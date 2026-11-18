using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

/// <summary>
/// Isometric hero controller for the Diablo prototype (Q5 click-to-move,
/// Cinderreach rework): WASD/arrows move camera-relative with priority, the
/// hero always faces the mouse cursor, left-click strikes a stationary
/// frontal cone toward the cursor (no step-in, no chase), right-click thrusts
/// past husk melee range, Shift blocks frontally. Bindings are code-defined
/// (Input System package, no .inputactions asset), like Q1.
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
    DiabloBlock block;
    float verticalVelocity;
    float lastNetSwing = float.NegativeInfinity;
    float lastNetThrust = float.NegativeInfinity;
    Vector3 destination;
    bool hasDestination;
    GameObject attackTarget;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            // Primitive Capsule test heroes carry a sibling CapsuleCollider alongside CharacterController;
            // disable it and sync PhysX transforms so CharacterController.Move doesn't self-depenetrate.
            var cap = GetComponent<CapsuleCollider>();
            if (cap != null && !cap.isTrigger) cap.enabled = false;
            Physics.SyncTransforms();
        }
        melee = GetComponent<DiabloMelee>();
        netPlayer = GetComponent<DiabloNetworkPlayer>();
        block = GetComponent<DiabloBlock>();
        if (block == null) block = gameObject.AddComponent<DiabloBlock>();
        var fwd = transform.forward;
        fwd.y = 0f;
        AimDirection = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;
        // Ranged skills (Q fireball, E bow) ride on every hero, scene or prefab.
        if (GetComponent<DiabloSkills>() == null) gameObject.AddComponent<DiabloSkills>();
        if (GetComponent<DiabloProgression>() == null) gameObject.AddComponent<DiabloProgression>();
        // S4 character sheet rides along too (runtime-built UI, C to open).
        if (GetComponent<DiabloCharacterUI>() == null) gameObject.AddComponent<DiabloCharacterUI>();
        // P2 zone scenes run netcode: the solo hero would double-drive clicks
        // and fight the network player over the camera. Yield in that case.
        if (ShouldYieldToNetcode(netPlayer != null, NetworkManager.Singleton != null))
            enabled = false;
    }

    /// <summary>
    /// True when a scene-local click controller must stand down: the scene
    /// runs netcode and this object is not the network player.
    /// Pure rule so EditMode tests can pin it without spawning UI.
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

    /// <summary>
    /// Driver/autoplay seam (bots, scripted runs): approach and swing at a
    /// husk. Player clicks never set this since the rework (LMB is
    /// stationary); kept so autoplay and existing driver tests keep working.
    /// </summary>
    public void AttackTarget(GameObject target)
    {
        attackTarget = target;
        hasDestination = false;
    }

    public bool HasDestination => hasDestination;
    public GameObject CurrentTarget => attackTarget;

    /// <summary>
    /// Scripted driver owns the hero (shotcheck): keyboard and mouse are
    /// ignored so aim and facing stay where the script put them.
    /// </summary>
    public static bool ExternalDrive;

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

    /// <summary>
    /// Legacy step-in distance (pre-rework D2 commitment). Kept as a constant
    /// so old tuning references compile; strikes no longer move the hero.
    /// </summary>
    public const float StrikeStep = 0.5f;

    /// <summary>True while the Shift block is held.</summary>
    public bool IsBlocking()
    {
        if (block == null) block = GetComponent<DiabloBlock>();
        return block != null && block.Held;
    }

    /// <summary>
    /// Stationary LMB cone toward the cursor: damages every husk in the
    /// frontal arc, never moves the hero. Relays per-target via netcode.
    /// Blocked while blocking. Returns true when at least one husk is hit.
    /// </summary>
    public bool Strike()
    {
        if (IsBlocking()) return false;
        if (melee == null) melee = GetComponent<DiabloMelee>();
        if (netPlayer == null) netPlayer = GetComponent<DiabloNetworkPlayer>();
        var driver = GetComponent<DiabloAnimator>();
        if (driver != null) driver.OnStrike();
        SheatheBow();
        DiabloAudio.PlaySwing();
        var targets = melee != null
            ? DiabloMelee.FindArcTargets(transform, AimDirection, melee.attackRange, DiabloMelee.ArcHalfAngle)
            : new System.Collections.Generic.List<GameObject>();
        if (targets.Count == 0) return false;
        var nm = NetworkManager.Singleton;
        if (netPlayer != null && nm != null && nm.IsListening && netPlayer.IsSpawned)
        {
            float cd = melee != null ? melee.cooldown : 0.5f;
            if (Time.time - lastNetSwing < cd) return false;
            lastNetSwing = Time.time;
            foreach (var t in targets) netPlayer.Attack(t, melee, transform);
            return true;
        }
        return melee != null && melee.TryConeAttack(transform, AimDirection) > 0;
    }

    /// <summary>
    /// RMB outreach thrust toward the cursor: narrow cone past husk melee
    /// range. Blocked while blocking. Returns true on a landed hit.
    /// </summary>
    public bool Thrust()
    {
        if (IsBlocking()) return false;
        if (melee == null) melee = GetComponent<DiabloMelee>();
        if (netPlayer == null) netPlayer = GetComponent<DiabloNetworkPlayer>();
        var driver = GetComponent<DiabloAnimator>();
        if (driver != null) driver.OnStrike();
        SheatheBow();
        DiabloAudio.PlaySwing();
        var targets = DiabloMelee.FindArcTargets(
            transform, AimDirection, DiabloMelee.OutreachRange, DiabloMelee.OutreachHalfAngle);
        if (targets.Count == 0) return false;
        var nm = NetworkManager.Singleton;
        if (netPlayer != null && nm != null && nm.IsListening && netPlayer.IsSpawned)
        {
            if (Time.time - lastNetThrust < DiabloMelee.OutreachCooldown) return false;
            lastNetThrust = Time.time;
            foreach (var t in targets) netPlayer.Attack(t, melee, transform);
            return true;
        }
        return melee != null && melee.TryThrust(transform, AimDirection) > 0;
    }

    void Update()
    {
        // P2 co-op: remote mirrors are driven by NetworkTransform, not input.
        if (IsRemoteMirror) return;
        if (!ExternalDrive) PollKeyboard();
        UpdateBlock();

        var mouse = ExternalDrive ? null : Mouse.current;
        if (mouse != null && Camera.main != null)
        {
            // Aim tracks the cursor every frame (twin-stick feel).
            var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out var aimHit, 200f))
                SetAimPoint(aimHit.point);
            if (!ShouldBlockClicks(GetComponent<DiabloInventoryUI>())
                && !DiabloUIFactory.IsWorldInputBlocked(gameObject))
            {
                if (mouse.leftButton.wasPressedThisFrame)
                    Strike();
                else if (mouse.rightButton.wasPressedThisFrame)
                    Thrust();
            }
        }

        float speed = walkSpeed * (IsBlocking() ? DiabloBlock.BlockMoveMultiplier : 1f);
        // WASD/arrows win over click orders; facing stays on the cursor.
        if (MoveInput.sqrMagnitude > 0.0001f)
        {
            Move(MoveInput * speed);
        }
        // Driver/autoplay seam: approach until in range, then swing (cooldown
        // gates repeats). Unity fake-null: a destroyed husk reads as null
        // here and clears the order. Player clicks never set this.
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
                var driver = GetComponent<DiabloAnimator>();
                var nm = NetworkManager.Singleton;
                if (netPlayer != null && nm != null && nm.IsListening && netPlayer.IsSpawned)
                {
                    // Same cooldown as local swings: the server does not gate
                    // client attack rate, so gate it here (also stops anim spam).
                    float cd = melee != null ? melee.cooldown : 0.5f;
                    if (Time.time - lastNetSwing >= cd)
                    {
                        lastNetSwing = Time.time;
                        if (driver != null) driver.OnStrike();
                        SheatheBow();
                        netPlayer.Attack(attackTarget, melee, transform);
                    }
                }
                else if (melee != null && melee.TryAttack(attackTarget, transform))
                {
                    // Visible swing: chase-kills used to die silently ("despawn").
                    if (driver != null) driver.OnStrike();
                    SheatheBow();
                }
                if (attackTarget == null) attackTarget = null;
            }
            else
            {
                Move(toTarget.normalized * speed);
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
                Move(toDest.normalized * speed);
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

    /// <summary>Shift hold drives the block (programmatic Held survives headless).</summary>
    void UpdateBlock()
    {
        if (block == null) block = GetComponent<DiabloBlock>();
        if (block == null) return;
        if (ExternalDrive) return;
        var kb = Keyboard.current;
        if (kb == null) return; // headless/tests: API only
        block.Held = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
    }

    /// <summary>Melee swings put the bow away so the sword shows again.</summary>
    void SheatheBow()
    {
        var skills = GetComponent<DiabloSkills>();
        if (skills != null && skills.BowDrawn) skills.ExitBowStance();
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
        cam.transform.position = pivot - rot * Vector3.forward * distance + DiabloJuice.ShakeOffset;
        cam.transform.rotation = rot;
    }
}
