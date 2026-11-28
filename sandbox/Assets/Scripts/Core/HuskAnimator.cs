using UnityEngine;

/// <summary>
/// Drives the husk Mecanim controller (KayKit Rig_Medium takes on the
/// Skeletons rig): "Moving" while approaching, "Attack" on the wind-up,
/// "Hit" when struck. Null-safe: primitive test husks have no Animator and
/// only bump the counters, so feedback is provable headless. Animated husks
/// also walk while measurably moving (scripted movers such as lancers and
/// casters never call <see cref="SetMoving"/>) and get <see cref="LocomotionTwist"/>, so their legs
/// follow detours and sidesteps while the body keeps facing the hero.
/// </summary>
public sealed class HuskAnimator : MonoBehaviour
{
    public const string MoveParam = "Moving";
    public const string AttackParam = "Attack";
    public const string HitParam = "Hit";
    public const string DeathState = "Death";

    public int attackCount;
    public int hitCount;
    public bool moving;

    Animator animator;
    LocomotionTwist twist;

    void Awake()
    {
        animator = GetComponent<Animator>();
        twist = LocomotionTwist.Ensure(gameObject);
    }

    void LateUpdate()
    {
        if (Driven) animator.SetBool(MoveParam, moving || (twist != null && twist.MovingMeasured));
    }

    bool Driven => animator != null && animator.runtimeAnimatorController != null;

    public void SetMoving(bool value)
    {
        moving = value;
        if (Driven) animator.SetBool(MoveParam, value);
    }

    public void OnAttack()
    {
        attackCount++;
        if (Driven) animator.SetTrigger(AttackParam);
    }

    public void OnHit()
    {
        hitCount++;
        if (Driven) animator.SetTrigger(HitParam);
    }
}
