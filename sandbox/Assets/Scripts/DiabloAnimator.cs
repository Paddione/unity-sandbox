using UnityEngine;

/// <summary>
/// Drives the Mecanim locomotion controller (KayKit Rig_Medium takes,
/// retargeted onto the Adventurers hero) from the click-to-move state:
/// sets the "Moving" bool while the hero has an order.
/// </summary>
[RequireComponent(typeof(Animator))]
public sealed class DiabloAnimator : MonoBehaviour
{
    public string moveParam = "Moving";
    public string strikeParam = "Strike";

    /// <summary>Swings triggered (hits + whiffs) — the visible feedback counter.</summary>
    public int strikeCount;

    Animator animator;
    DiabloClickController controller;

    /// <summary>True while the hero works an order (click or WASD).</summary>
    public static bool IsMoving(DiabloClickController c) =>
        c != null && (c.HasDestination || c.CurrentTarget != null
            || c.MoveInput.sqrMagnitude > 0.0001f);

    void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<DiabloClickController>();
    }

    void Update()
    {
        if (animator == null || controller == null) return;
        animator.SetBool(moveParam, IsMoving(controller));
    }

    /// <summary>Swing once (hit or whiff) — called by every Strike.</summary>
    public void OnStrike()
    {
        strikeCount++;
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetTrigger(strikeParam);
    }
}
