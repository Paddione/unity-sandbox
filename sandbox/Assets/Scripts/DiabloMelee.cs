using UnityEngine;

/// <summary>
/// Melee swing for the Diablo hero (Q5). Range arc + cooldown; damage goes
/// through <see cref="EnemyHealth"/>. Driven by
/// <see cref="DiabloClickController"/> attack-move (or tests directly).
/// </summary>
public sealed class DiabloMelee : MonoBehaviour
{
    public float attackRange = 2.2f;
    public int damage = 1;
    public float cooldown = 0.5f;

    public static int Swings;

    float lastSwing = float.NegativeInfinity;

    /// <summary>
    /// Damage for one swing: equipped weapon stats when the attacker gears
    /// through <see cref="Equipment"/>, else the flat <see cref="damage"/>.
    /// </summary>
    public int CurrentDamage(Transform attacker)
    {
        var eq = attacker != null ? attacker.GetComponent<Equipment>() : null;
        if (eq == null) return damage;
        return Mathf.Max(1, Mathf.RoundToInt(eq.GetDamage()));
    }

    /// <summary>
    /// Knockback distance by tier (D2 pop, not punt: combos must stay in the
    /// 2.2 m arc — 1.2 m broke the arc-kill contract, see Strike_HitsHuskInFacingArc).
    /// </summary>
    public const float KnockbackNormal = 0.6f;
    public const float KnockbackElite = 0.2f;

    /// <summary>Last husk actually hit (target bar follows it; null when none).</summary>
    public static GameObject LastStruck;
    public static float LastStruckTime;

    /// <summary>Attempt one swing at target. Returns true on a landed hit.</summary>
    public bool TryAttack(GameObject target, Transform attacker)
    {
        if (target == null || attacker == null) return false;
        if (Time.time - lastSwing < cooldown) return false;
        var toTarget = target.transform.position - attacker.position;
        toTarget.y = 0f;
        if (toTarget.magnitude > attackRange) return false;
        var health = target.GetComponent<EnemyHealth>();
        if (health == null) return false;
        lastSwing = Time.time;
        Swings++;
        if (toTarget.sqrMagnitude > 0.0001f)
            attacker.rotation = Quaternion.LookRotation(toTarget);
        int blow = DiabloJuice.RollCrit(attacker, CurrentDamage(attacker), out bool crit);
        health.TakeDamage(blow, crit);
        health.ApplyKnockback(attacker.position,
            health.tier == LootTier.Normal ? KnockbackNormal : KnockbackElite);
        DiabloAudio.PlayHit();
        HitStop.Trigger(crit ? HitStop.Duration * 1.6f : HitStop.Duration);
        LastStruck = target;
        LastStruckTime = Time.time;
        Debug.Log($"[Diablo] Melee swing #{Swings} hit {target.name} ({blow} dmg).");
        return true;
    }

    /// <summary>Frontal cone for cursor strikes (half-angle).</summary>
    public const float ArcHalfAngle = 60f;

    /// <summary>Nearest husk inside the facing arc, or null.</summary>
    public static GameObject FindArcTarget(Transform attacker, Vector3 dir, float range)
    {
        if (attacker == null) return null;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return null;
        dir.Normalize();
        GameObject best = null;
        float bestDist = float.MaxValue;
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk.GetComponent<EnemyHealth>() == null) continue;
            var to = husk.transform.position - attacker.position;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist > range) continue;
            if (dist > 0.001f && Vector3.Angle(dir, to / dist) > ArcHalfAngle) continue;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = husk;
            }
        }
        return best;
    }

    /// <summary>Swing toward a direction; hits the nearest husk in the arc.</summary>
    public bool TryAttackDirection(Transform attacker, Vector3 dir)
    {
        var target = FindArcTarget(attacker, dir, attackRange);
        return target != null && TryAttack(target, attacker);
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        Swings = 0;
        LastStruck = null;
        LastStruckTime = 0f;
    }
}
