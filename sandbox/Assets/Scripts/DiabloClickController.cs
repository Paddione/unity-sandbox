using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

/// <summary>
/// Isometric hero controller for the Diablo prototype (Q5 click-to-move,
/// P2+ hybrid): WASD/arrows move camera-relative with priority, the hero
/// always faces the mouse cursor, left-click strikes toward the cursor
/// (a clicked husk keeps the Q5 attack-move chase). Bindings are
/// code-defined (Input System package, no .inputactions asset), like Q1.
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
        var fwd = transform.forward;
        fwd.y = 0f;
        AimDirection = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;
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

    /// <summary>
    /// Clicks go to the open inventory panel, never to strikes (pure rule so
    /// EditMode tests can pin it without spawning UI).
    /// </summary>
    public static bool ShouldBlockClicks(DiabloInventoryUI ui) =>
        ui != null && ui.IsOpen;

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

    /// <summary>Active WASD/arrows world-space intent (zero when idle).</summary>
    public Vector3 MoveInput { get; private set; }

    /// <summary>Flattened facing direction (mouse aim).</summary>
    public Vector3 AimDirection { get; private set; }

    /// <summary>Camera-relative WASD mapping (pure, fixed iso yaw).</summary>
    public static Vector3 MapInput(Vector2 xy, float yaw)
    {
        if (xy.sqrMagnitude > 1f) xy.Normalize();
        var dir = Quaternion.Euler(0f, yaw, 0f) * new Vector3(xy.x, 0f, xy.y);
        dir.y = 0f;
        return dir;
    }

    /// <summary>Drive movement directly (tests + external drivers).</summary>
    public void SetMoveInput(Vector3 worldDir)
    {
        MoveInput = worldDir;
    }

    /// <summary>Face a ground point (mouse aim); snaps for tight tracking.</summary>
    public void SetAimPoint(Vector3 groundPoint)
    {
        var d = groundPoint - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.0001f) return;
        AimDirection = d.normalized;
        transform.rotation = Quaternion.LookRotation(AimDirection);
    }

    /// <summary>Swing toward the cursor; relays via netcode when spawned.</summary>
    public bool Strike()
    {
        if (melee == null) melee = GetComponent<DiabloMelee>();
        if (netPlayer == null) netPlayer = GetComponent<DiabloNetworkPlayer>();
        var driver = GetComponent<DiabloAnimator>();
        if (driver != null) driver.OnStrike();
        var target = melee != null
            ? DiabloMelee.FindArcTarget(transform, AimDirection, melee.attackRange)
            : null;
        if (target == null) return false;
        var nm = NetworkManager.Singleton;
        if (netPlayer != null && nm != null && nm.IsListening && netPlayer.IsSpawned)
        {
            netPlayer.Attack(target, melee, transform);
            return true;
        }
        return melee != null && melee.TryAttack(target, transform);
    }

    void Update()
    {
        // P2 co-op: remote mirrors are driven by NetworkTransform, not input.
        if (IsRemoteMirror) return;
        PollKeyboard();

        var mouse = Mouse.current;
        if (mouse != null && Camera.main != null)
        {
            // Aim tracks the cursor every frame (twin-stick feel).
            var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out var aimHit, 200f))
                SetAimPoint(aimHit.point);
            if (mouse.leftButton.wasPressedThisFrame
                && !ShouldBlockClicks(GetComponent<DiabloInventoryUI>()))
                HandleClick(aimHit);
        }

        // WASD/arrows win over click orders; facing stays on the cursor.
        if (MoveInput.sqrMagnitude > 0.0001f)
        {
            Move(MoveInput * walkSpeed);
        }
        // Attack-move: approach until in range, then swing (cooldown gates repeats).
        // Unity fake-null: a destroyed husk reads as null here and clears the order.
        else if (attackTarget != null)
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

    /// <summary>WASD/arrows drive MoveInput; held keys cancel click orders.</summary>
    public void PollKeyboard()
    {
        var kb = Keyboard.current;
        if (kb == null) return; // headless/tests: keep programmatic input
        MoveInput = Vector3.zero;
        float ix = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                 - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
        float iy = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                 - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
        if (ix == 0f && iy == 0f) return;
        MoveInput = MapInput(new Vector2(ix, iy), yaw);
        hasDestination = false;
        attackTarget = null;
    }

    void HandleClick(RaycastHit hit)
    {
        if (hit.collider != null && hit.collider.CompareTag("Husk"))
            AttackTarget(hit.collider.gameObject);
        else
            Strike();
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
