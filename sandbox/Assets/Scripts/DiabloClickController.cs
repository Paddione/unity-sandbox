using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

/// <summary>
/// Isometric click-to-move controller for the Diablo prototype (Q5).
/// Fixed-angle follow camera, left-click raycasts against the ground for
/// movement and against tagged husks for attack-move. Bindings are
/// code-defined (Input System package, no .inputactions asset), like Q1.
/// Solo P1: no NetworkObject gate (Diablo scene carries no netcode).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public sealed class DiabloClickController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float stoppingDistance = 0.25f;
    public float gravity = -22f;

    [Header("Camera (fixed isometric)")]
    public float yaw = 45f;
    public float pitch = 55f;
    public float distance = 11f;

    [Header("Combat")]
    public float attackRange = 2.2f;

    CharacterController controller;
    DiabloMelee melee;
    DiabloNetworkPlayer netPlayer;
    float verticalVelocity;
    Vector3 destination;
    bool hasDestination;
    GameObject attackTarget;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        melee = GetComponent<DiabloMelee>();
        netPlayer = GetComponent<DiabloNetworkPlayer>();
        // P2 zone scenes run netcode: the solo hero would double-drive clicks
        // and fight the network player over the camera. Yield in that case.
        if (ShouldYieldToNetcode(netPlayer != null, NetworkManager.Singleton != null))
            enabled = false;
    }

    /// <summary>
    /// True when a scene-local click controller must stand down: the scene
    /// runs netcode and this object is not the network player.
    /// Pure rule so EditMode tests can pin it.
    /// </summary>
    public static bool ShouldYieldToNetcode(bool hasNetworkPlayer, bool netcodeInScene) =>
        !hasNetworkPlayer && netcodeInScene;

    /// <summary>True for a replicated mirror owned by another client.</summary>
    bool IsRemoteMirror =>
        netPlayer != null && netPlayer.IsSpawned && !netPlayer.IsOwner;

    /// <summary>Order movement to a ground point (clears attack target).</summary>
    public void SetDestination(Vector3 point)
    {
        destination = point;
        hasDestination = true;
        attackTarget = null;
    }

    /// <summary>Order attack-move against a husk (re-paths into melee range).</summary>
    public void AttackTarget(GameObject target)
    {
        attackTarget = target;
        hasDestination = false;
    }

    public bool HasDestination => hasDestination;
    public GameObject CurrentTarget => attackTarget;

    void Update()
    {
        // P2 co-op: remote mirrors are driven by NetworkTransform, not input.
        if (IsRemoteMirror) return;
        var mouse = Mouse.current;
        if (mouse != null && Camera.main != null && mouse.leftButton.wasPressedThisFrame)
            HandleClick(mouse);

        // Attack-move: approach until in range, then swing (cooldown gates repeats).
        // Unity fake-null: a destroyed husk reads as null here and clears the order.
        if (attackTarget != null)
        {
            var toTarget = attackTarget.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude <= attackRange)
            {
                Face(toTarget);
                // Lazy: Awake order vs. sibling AddComponent is not guaranteed.
                if (melee == null) melee = GetComponent<DiabloMelee>();
                if (netPlayer == null) netPlayer = GetComponent<DiabloNetworkPlayer>();
                var nm = NetworkManager.Singleton;
                if (netPlayer != null && nm != null && nm.IsListening && netPlayer.IsSpawned)
                    netPlayer.Attack(attackTarget, melee, transform);
                else if (melee != null) melee.TryAttack(attackTarget, transform);
                if (attackTarget == null) attackTarget = null;
            }
            else
            {
                Move(toTarget.normalized * walkSpeed);
                Face(toTarget);
            }
        }
        else if (hasDestination)
        {
            var toDest = destination - transform.position;
            toDest.y = 0f;
            if (toDest.magnitude <= stoppingDistance)
            {
                hasDestination = false;
            }
            else
            {
                Move(toDest.normalized * walkSpeed);
                Face(toDest);
            }
        }

        // Gravity (flat dungeon, no jump in P1).
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);

        UpdateCamera();
    }

    void HandleClick(Mouse mouse)
    {
        var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out var hit, 200f)) return;
        if (hit.collider.CompareTag("Husk"))
            AttackTarget(hit.collider.gameObject);
        else
            SetDestination(hit.point);
    }

    void Move(Vector3 planar)
    {
        controller.Move(planar * Time.deltaTime);
    }

    void Face(Vector3 planar)
    {
        if (planar.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(planar), 14f * Time.deltaTime);
    }

    void UpdateCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var pivot = transform.position + Vector3.up * 1f;
        var rot = Quaternion.Euler(pitch, yaw, 0f);
        cam.transform.position = pivot - rot * Vector3.forward * distance;
        cam.transform.rotation = rot;
    }
}
