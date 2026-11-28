using UnityEngine;

/// <summary>
/// Split-body locomotion for KayKit Rig_Medium characters (heroes, husks,
/// remote co-op mirrors). The root keeps its gameplay facing (heroes: mouse
/// aim, husks: the hero); after the Animator has posed the rig, the hips
/// yaw toward the measured ground velocity so the legs run where the
/// character actually goes, and spine/chest turn back so the upper body keeps
/// the root's facing. Moving more than <see cref="BackpedalAngle"/> away from
/// the facing backpedals instead: legs face the facing side and the run clip
/// plays in reverse through the <see cref="MoveSpeedParam"/> state multiplier.
/// </summary>
[RequireComponent(typeof(Animator))]
public sealed class LocomotionTwist : MonoBehaviour
{
    /// <summary>Float state-speed multiplier on the run/walk state (+1 forward, -1 backpedal).</summary>
    public const string MoveSpeedParam = "MoveSpeed";
    /// <summary>Beyond this angle between facing and travel the legs backpedal.</summary>
    public const float BackpedalAngle = 100f;
    /// <summary>Hysteresis: backpedal ends only below this angle (no flicker at the boundary).</summary>
    public const float BackpedalExitAngle = 80f;
    /// <summary>Planar speed (m/s) above which the character counts as moving.</summary>
    public const float MoveThreshold = 0.4f;
    /// <summary>Per-frame jump above this is a teleport, not locomotion.</summary>
    public const float TeleportDistance = 3f;
    /// <summary>Largest hip twist; the spine takes back the same amount.</summary>
    public const float MaxTwist = 100f;
    const float TwistRate = 12f;
    const float VelocityRate = 15f;
    const float SpineShare = 0.6f;

    Animator animator;
    Transform hips, spine, chest;
    Quaternion hipsBase, spineBase, chestBase, hipsApplied, spineApplied, chestApplied;
    bool applied;
    bool hasSpeedParam;
    Vector3 lastPos;
    Vector3 velocity;
    float twist;

    /// <summary>Smoothed planar velocity measured from the transform (works for mirrors and scripted movers).</summary>
    public Vector3 Velocity => velocity;
    /// <summary>True while the measured planar speed exceeds <see cref="MoveThreshold"/>.</summary>
    public bool MovingMeasured => velocity.sqrMagnitude > MoveThreshold * MoveThreshold;
    /// <summary>True while the legs backpedal (run clip reversed).</summary>
    public bool Backpedaling { get; private set; }
    /// <summary>Current hip yaw relative to the root facing, degrees.</summary>
    public float Twist => twist;

    /// <summary>
    /// Leg yaw (signed degrees, relative to <paramref name="facing"/>) for
    /// travel direction <paramref name="move"/>. Pure, so EditMode tests pin it.
    /// </summary>
    public static float SolveLegYaw(Vector3 facing, Vector3 move, bool wasBackpedaling, out bool backpedal)
    {
        facing.y = 0f;
        move.y = 0f;
        backpedal = false;
        if (facing.sqrMagnitude < 1e-6f || move.sqrMagnitude < 1e-6f) return 0f;
        float angle = Vector3.SignedAngle(facing, move, Vector3.up);
        float limit = wasBackpedaling ? BackpedalExitAngle : BackpedalAngle;
        if (Mathf.Abs(angle) <= limit) return angle;
        backpedal = true;
        return Vector3.SignedAngle(facing, -move, Vector3.up);
    }

    void Awake()
    {
        animator = GetComponent<Animator>();
        ResolveBones();
        lastPos = transform.position;
    }

    /// <summary>
    /// (Re-)resolve the split-body bones. KayKit Adventurers share one rig,
    /// but import variants and skin swaps differ in case; companions are
    /// re-skinned after Awake, so a lazy re-resolve keeps every class footed.
    /// </summary>
    public bool ResolveBones()
    {
        hips = FindBone(transform, HipNames);
        spine = hips != null ? FindBone(hips, SpineNames) : null;
        chest = spine != null ? FindBone(spine, ChestNames) : null;
        return hips != null && spine != null;
    }

    static readonly string[] HipNames = { "hips", "hip", "pelvis", "pelvis.l", "root_hips" };
    static readonly string[] SpineNames = { "spine", "spine1", "spine01", "torso", "chest1" };
    static readonly string[] ChestNames = { "chest", "chest1", "upper_chest", "spine2", "spine02" };

    /// <summary>True once hips+spine resolved (chest is optional); false = twist parks safely.</summary>
    public bool HasFooting => hips != null && spine != null;

    /// <summary>
    /// Ensure split-body footwork on a hero, companion, mirror or husk:
    /// adds <see cref="LocomotionTwist"/> whenever an Animator stands.
    /// Idempotent; returns the twist or null when no Animator exists.
    /// </summary>
    public static LocomotionTwist Ensure(GameObject go)
    {
        if (go == null) return null;
        if (go.GetComponent<Animator>() == null) return null;
        var twist = go.GetComponent<LocomotionTwist>();
        if (twist == null) twist = go.AddComponent<LocomotionTwist>();
        else twist.ResolveBones();
        return twist;
    }

    void Start()
    {
        hasSpeedParam = false;
        if (animator != null && animator.runtimeAnimatorController != null)
            foreach (var p in animator.parameters)
                if (p.name == MoveSpeedParam && p.type == AnimatorControllerParameterType.Float) hasSpeedParam = true;
    }

    void LateUpdate()
    {
        if (hips == null || spine == null) ResolveBones();
        float dt = Time.deltaTime;
        var pos = transform.position;
        var delta = pos - lastPos;
        lastPos = pos;
        delta.y = 0f;
        if (dt > 0f)
        {
            if (delta.magnitude > TeleportDistance) velocity = Vector3.zero;
            else velocity = Vector3.Lerp(velocity, delta / dt, 1f - Mathf.Exp(-VelocityRate * dt));
        }

        bool moving = MovingMeasured;
        float target = 0f;
        bool back = false;
        if (moving)
            target = SolveLegYaw(transform.forward, velocity, Backpedaling, out back);
        Backpedaling = moving && back;
        target = Mathf.Clamp(target, -MaxTwist, MaxTwist);
        twist = Mathf.LerpAngle(twist, target, dt > 0f ? 1f - Mathf.Exp(-TwistRate * dt) : 1f);

        if (hasSpeedParam && animator != null && animator.isActiveAndEnabled)
            animator.SetFloat(MoveSpeedParam, Backpedaling ? -1f : 1f);
        ApplyTwist();
    }

    void ApplyTwist()
    {
        if (hips == null || spine == null) return;
        // Culled or paused Animators leave the bones alone: undo last frame's twist
        // instead of stacking a new one on top of it.
        if (applied)
        {
            if (hips.localRotation == hipsApplied) hips.localRotation = hipsBase;
            if (spine.localRotation == spineApplied) spine.localRotation = spineBase;
            if (chest != null && chest.localRotation == chestApplied) chest.localRotation = chestBase;
        }
        hipsBase = hips.localRotation;
        spineBase = spine.localRotation;
        if (chest != null) chestBase = chest.localRotation;

        if (Mathf.Abs(twist) > 0.01f && animator != null && animator.isActiveAndEnabled)
        {
            var up = Vector3.up;
            hips.rotation = Quaternion.AngleAxis(twist, up) * hips.rotation;
            float spineBack = chest != null ? twist * SpineShare : twist;
            spine.rotation = Quaternion.AngleAxis(-spineBack, up) * spine.rotation;
            if (chest != null)
                chest.rotation = Quaternion.AngleAxis(-(twist - spineBack), up) * chest.rotation;
        }
        hipsApplied = hips.localRotation;
        spineApplied = spine.localRotation;
        if (chest != null) chestApplied = chest.localRotation;
        applied = true;
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (string.Equals(t.name, name, System.StringComparison.OrdinalIgnoreCase)) return t;
        foreach (Transform c in t)
        {
            var hit = FindDeep(c, name);
            if (hit != null) return hit;
        }
        return null;
    }

    static Transform FindBone(Transform root, string[] names)
    {
        foreach (var n in names)
        {
            var hit = FindDeep(root, n);
            if (hit != null) return hit;
        }
        return null;
    }
}
