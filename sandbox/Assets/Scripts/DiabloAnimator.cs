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

    Animator animator;
    DiabloClickController controller;

    /// <summary>True while the hero works an order (move or attack-move).</summary>
    public static bool IsMoving(DiabloClickController c) =>
        c != null && (c.HasDestination || c.CurrentTarget != null);

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
}
