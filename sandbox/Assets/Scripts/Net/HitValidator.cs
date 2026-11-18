using UnityEngine;

/// <summary>
/// Co-op hit validation (cinderreach-coop-session S2). Pure C# + Unity math,
/// no netcode: the server calls <see cref="Validate"/> for every client hit
/// intent (melee + ranged). Range uses planar (XZ) distance like the rest of
/// the combat code (queries use distance math, never triggers).
/// Targeting helper <see cref="NearestLivingPlayer"/> replaces the
/// name-based "Player" lookup for co-op (nearest living hero).
/// </summary>
public static class HitValidator
{
    /// <summary>Slack on top of the skill range (lag + cursor aim).</summary>
    public const float RangeTolerance = 0.5f;

    /// <summary>Upper bound for a single plausible hit (blocks 9999 fakes).</summary>
    public const int MaxDamage = 50;

    /// <summary>Planar (XZ) distance, like EnemyAttack/DiabloProjectile.</summary>
    public static float PlanarDistance(Vector3 a, Vector3 b)
    {
        var d = a - b;
        d.y = 0f;
        return d.magnitude;
    }

    /// <summary>True when the target is within skill range + tolerance.</summary>
    public static bool InRange(Vector3 attackerPos, Vector3 targetPos, float skillRange) =>
        PlanarDistance(attackerPos, targetPos) <= skillRange + RangeTolerance;

    /// <summary>True when enough time passed since the last validated hit.</summary>
    public static bool CooldownReady(float lastTime, float cooldown, float now) =>
        now - lastTime >= cooldown;

    /// <summary>True for a plausible single-hit damage roll.</summary>
    public static bool DamagePlausible(int damage) => damage >= 1 && damage <= MaxDamage;

    /// <summary>
    /// Full gate: null when the hit is valid, else a short reason
    /// ("range", "cooldown", "dead", "damage") for logs/tests.
    /// </summary>
    public static string Validate(Vector3 attackerPos, Vector3 targetPos, float skillRange,
        float lastTime, float cooldown, float now, bool targetAlive, int damage)
    {
        if (!DamagePlausible(damage)) return $"damage {damage} outside 1..{MaxDamage}";
        if (!targetAlive) return "dead target";
        if (!CooldownReady(lastTime, cooldown, now)) return $"cooldown {now - lastTime:0.00}s < {cooldown:0.00}s";
        if (!InRange(attackerPos, targetPos, skillRange))
            return $"range {PlanarDistance(attackerPos, targetPos):0.00}m > {skillRange + RangeTolerance:0.00}m";
        return null;
    }

    /// <summary>
    /// Nearest living hero (CurrentHP &gt; 0, active) to <paramref name="from"/>.
    /// Null when nobody is alive. Used by husks, wardens and the boss so co-op
    /// targeting no longer depends on the scene object named "Player".
    /// </summary>
    public static Transform NearestLivingPlayer(Vector3 from)
    {
        Transform best = null;
        float bestD = float.MaxValue;
        foreach (var h in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            if (h == null || !h.gameObject.activeInHierarchy) continue;
            if (h.CurrentHP <= 0f) continue;
            float d = PlanarDistance(from, h.transform.position);
            if (d < bestD)
            {
                bestD = d;
                best = h.transform;
            }
        }
        return best;
    }
}
