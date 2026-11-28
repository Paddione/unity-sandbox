using UnityEngine;

/// <summary>
/// Drives the Mecanim locomotion controller (KayKit Rig_Medium takes,
/// retargeted onto the Adventurers hero) from the click-to-move state:
/// sets the "Moving" bool while the hero has an order or is measurably
/// moving (remote co-op mirrors carry no input, only synced positions).
/// <see cref="LocomotionTwist"/> points the legs along the travel direction
/// while the upper body keeps the mouse aim.
/// </summary>
[RequireComponent(typeof(Animator))]
public sealed class DiabloAnimator : MonoBehaviour
{
    public string moveParam = "Moving";
    public string strikeParam = "Strike";
    /// <summary>Bool: bow drawn (aiming pose while standing).</summary>
    public string bowParam = "Bow";
    /// <summary>Trigger: one bow release (Ranged_Bow_Release take).</summary>
    public string bowShootParam = "BowShoot";

    /// <summary>Bow releases triggered (arrows shot with a visible pose).</summary>
    public int bowShotCount;

    /// <summary>Swings triggered (hits + whiffs) — the visible feedback counter.</summary>
    public int strikeCount;

    Animator animator;
    DiabloClickController controller;
    DiabloSkills skills;
    LocomotionTwist twist;

    /// <summary>True while the hero works an order (click or WASD).</summary>
    public static bool IsMoving(DiabloClickController c) =>
        c != null && (c.HasDestination || c.CurrentTarget != null
            || c.MoveInput.sqrMagnitude > 0.0001f);

    void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<DiabloClickController>();
        skills = GetComponent<DiabloSkills>();
        twist = LocomotionTwist.Ensure(gameObject);
    }

    /// <summary>
    /// Ensure hero footwork on any hero-like body (hero, companion clone,
    /// co-op mirror): Animator driver + split-body twist. Idempotent.
    /// </summary>
    public static void EnsureFootwork(GameObject go)
    {
        if (go == null) return;
        if (go.GetComponent<Animator>() == null) return;
        if (go.GetComponent<DiabloAnimator>() == null) go.AddComponent<DiabloAnimator>();
        LocomotionTwist.Ensure(go);
    }

    /// <summary>Controllers built before the bow states lack these params: skip silently.</summary>
    bool HasParam(string name)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (var p in animator.parameters)
            if (p.name == name) return true;
        return false;
    }

    void Update()
    {
        if (animator == null || controller == null) return;
        animator.SetBool(moveParam, IsMoving(controller) || (twist != null && twist.MovingMeasured));
        if (skills == null) skills = GetComponent<DiabloSkills>();
        if (skills != null && HasParam(bowParam)) animator.SetBool(bowParam, skills.BowDrawn);
    }

    /// <summary>Bow shot: draw-and-release pose instead of the sword chop.</summary>
    public void OnBowShot()
    {
        strikeCount++;
        bowShotCount++;
        if (!HasParam(bowShootParam))
        {
            OnStrikeTriggerOnly();
            return;
        }
        if (HasParam(bowParam)) animator.SetBool(bowParam, true);
        animator.ResetTrigger(strikeParam);
        animator.SetTrigger(bowShootParam);
    }

    void OnStrikeTriggerOnly()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetTrigger(strikeParam);
    }

    /// <summary>Swing once (hit or whiff) — called by every Strike.</summary>
    public void OnStrike()
    {
        strikeCount++;
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetTrigger(strikeParam);
    }
}
