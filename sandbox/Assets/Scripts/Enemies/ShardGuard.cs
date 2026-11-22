using UnityEngine;

/// <summary>
/// Hit filter seam (act-two-opening S2): components on an enemy that shape
/// incoming damage from a known origin. <see cref="EnemyHealth.TakeDamage(int, bool, Vector3)"/>
/// runs every filter; hits without an origin (ticks, thorns, burns) skip them.
/// </summary>
public interface IDamageFilter
{
    int Filter(int amount, Vector3 from);
}

/// <summary>
/// Shardback glass shield (act-two-opening S2): hits from inside the frontal
/// arc lose <see cref="Reduction"/> of their damage (rounded, never below 1,
/// so weak hits still chip through; S5 matrix showed 0-damage pokes stalled
/// a Kindled Lanternbearer). Flank and back hits land in full, and a
/// stunned Shardback (tank slam) drops its guard. The Shardback turns slowly
/// (<see cref="EnemyAttack.turnRate"/>), so a party that holds its attention
/// on the tank can hit it from behind.
/// </summary>
public sealed class ShardGuard : MonoBehaviour, IDamageFilter
{
    public const float DefaultArc = 110f;
    public const float EliteArc = 150f;
    public const float Reduction = 0.6f;
    public const float TurnRate = 2.5f;

    /// <summary>Full frontal arc in degrees.</summary>
    public float arcDegrees = DefaultArc;

    /// <summary>Hits the guard caught this run (test seam).</summary>
    public static int Blocks;

    /// <summary>Pure rule: is <paramref name="from"/> inside the frontal arc of a body at <paramref name="pos"/> facing <paramref name="forward"/>?</summary>
    public static bool InArc(Vector3 pos, Vector3 forward, Vector3 from, float arc)
    {
        var to = from - pos;
        to.y = 0f;
        forward.y = 0f;
        if (to.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f) return true; // point blank: shield
        return Vector3.Angle(forward, to) <= arc * 0.5f;
    }

    /// <summary>Damage left after a guarded hit.</summary>
    public static int Guarded(int amount) =>
        amount <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(amount * (1f - Reduction)));

    public int Filter(int amount, Vector3 from)
    {
        var attack = GetComponent<EnemyAttack>();
        if (attack != null && attack.IsStunned) return amount;
        if (!InArc(transform.position, transform.forward, from, arcDegrees)) return amount;
        Blocks++;
        return Guarded(amount);
    }

    public static void ResetRun() => Blocks = 0;
}
