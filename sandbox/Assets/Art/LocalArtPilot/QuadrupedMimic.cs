using UnityEngine;

/// <summary>
/// Local-art pilot: mirrors the host husk's combat state onto the quadruped
/// visual child (own Animator, no parameters, direct clip playback, no root
/// motion). Reads only public state (HuskAnimator.moving/attackCount,
/// EnemyHealth.IsAlive); combat components on the host root are untouched.
/// </summary>
public sealed class QuadrupedMimic : MonoBehaviour
{
    const string RunClip = "Run";
    const string AttackClip = "Attack";

    Animator animator;
    HuskAnimator husk;
    EnemyHealth health;
    int lastAttackCount = -1;

    void Awake()
    {
        animator = GetComponent<Animator>();
        husk = GetComponentInParent<HuskAnimator>();
        health = GetComponentInParent<EnemyHealth>();
    }

    void Update()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        if (health != null && !health.IsAlive)
        {
            animator.enabled = false;
            return;
        }
        if (husk != null && husk.attackCount != lastAttackCount)
        {
            lastAttackCount = husk.attackCount;
            animator.speed = 1f;
            animator.CrossFadeInFixedTime(AttackClip, 0.08f);
            return;
        }
        bool moving = husk != null && husk.moving;
        var state = animator.GetCurrentAnimatorStateInfo(0);
        if (moving)
        {
            animator.speed = 1f;
            if (!state.IsName(RunClip)) animator.Play(RunClip);
        }
        else if (!state.IsName(AttackClip) || state.normalizedTime >= 1f)
        {
            // Idle stance: freeze the trot on its neutral first frame.
            animator.Play(RunClip, 0, 0f);
            animator.speed = 0f;
        }
    }
}
