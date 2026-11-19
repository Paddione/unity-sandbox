using UnityEngine;

/// <summary>
/// Party threat per enemy (cinderreach-party-roles S2). Damage books threat
/// 1:1 from the attacker, the tank slam adds <see cref="SlamThreatBonus"/>
/// per enemy on top (healing books in S3). <see cref="EnemyAttack"/>
/// prefers the highest-threat living source and falls back to nearest-living
/// when the table is empty, so solo play without any threat behaves exactly
/// like before. No decay in v1: threat is deterministic and test-pinnable.
/// </summary>
public sealed class ThreatTable : MonoBehaviour
{
    /// <summary>Threat per point of damage dealt to this enemy.</summary>
    public const float DamageThreatPerPoint = 1f;
    /// <summary>Extra threat per enemy hit by the tank slam, on top of its damage.</summary>
    public const float SlamThreatBonus = 20f;

    readonly System.Collections.Generic.Dictionary<Transform, float> threat =
        new System.Collections.Generic.Dictionary<Transform, float>();

    /// <summary>Add threat from a source. Null sources and non-positive amounts are ignored.</summary>
    public void AddThreat(Transform source, float amount)
    {
        if (source == null || amount <= 0f) return;
        threat.TryGetValue(source, out float cur);
        threat[source] = cur + amount;
    }

    /// <summary>Current threat of a source (0 when unknown).</summary>
    public float ThreatOf(Transform source)
    {
        if (source == null) return 0f;
        return threat.TryGetValue(source, out float cur) ? cur : 0f;
    }

    /// <summary>Highest-threat living source, or null when nobody qualifies.</summary>
    public Transform TopSource()
    {
        Prune();
        Transform best = null;
        float bestThreat = 0f;
        foreach (var kv in threat)
        {
            if (!IsLiving(kv.Key)) continue;
            if (kv.Value > bestThreat)
            {
                bestThreat = kv.Value;
                best = kv.Key;
            }
        }
        return bestThreat > 0f ? best : null;
    }

    /// <summary>True while any source holds threat (the enemy is in a fight).</summary>
    public bool Engaged => threat.Count > 0;

    /// <summary>Forget all threat (death, zone change, tests).</summary>
    public void Clear() => threat.Clear();

    static bool IsLiving(Transform t)
    {
        if (t == null || !t.gameObject.activeInHierarchy) return false;
        var hp = t.GetComponent<PlayerHealth>();
        if (hp != null && hp.CurrentHP <= 0f) return false;
        return true;
    }

    void Prune()
    {
        // Only destroyed sources are forgotten (one per read: dictionaries
        // dislike removal during iteration). Inactive heroes keep their
        // threat and simply lose aggro until they return (see IsLiving).
        Transform stale = null;
        bool hasStale = false;
        foreach (var k in threat.Keys)
        {
            if (k == null) { stale = k; hasStale = true; break; }
        }
        if (hasStale) threat.Remove(stale);
    }

    /// <summary>Book damage threat on a target, creating its table on first hit.</summary>
    public static void BookDamage(GameObject target, Transform attacker, float damage)
    {
        if (target == null || attacker == null || damage <= 0f) return;
        var table = target.GetComponent<ThreatTable>();
        if (table == null) table = target.AddComponent<ThreatTable>();
        table.AddThreat(attacker, damage * DamageThreatPerPoint);
    }

    /// <summary>Book the tank-slam bonus on a target, creating its table on first hit.</summary>
    public static void BookStun(GameObject target, Transform attacker)
    {
        if (target == null || attacker == null) return;
        var table = target.GetComponent<ThreatTable>();
        if (table == null) table = target.AddComponent<ThreatTable>();
        table.AddThreat(attacker, SlamThreatBonus);
    }

    /// <summary>
    /// Book healing threat (party-roles S3): every engaged enemy near the
    /// healer notices the mending. Amount is the HP actually restored.
    /// </summary>
    public static void BookHeal(Transform healer, float amount, float radius = 30f)
    {
        if (healer == null || amount <= 0f) return;
        foreach (var table in Object.FindObjectsByType<ThreatTable>(FindObjectsSortMode.None))
        {
            if (table == null || !table.Engaged) continue;
            var to = table.transform.position - healer.position;
            to.y = 0f;
            if (to.magnitude > radius) continue;
            table.AddThreat(healer, amount);
        }
    }
}
