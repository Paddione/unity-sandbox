using UnityEngine;

/// <summary>
/// Glass ward on the Glasswright: while any of its pillars stands, directed
/// hits lose half their damage (rounded up, never below 1).
/// </summary>
public sealed class GlassWard : MonoBehaviour, IDamageFilter
{
    public static int Warded;

    public int Filter(int amount, Vector3 from)
    {
        var boss = GetComponent<GlasswrightController>();
        if (boss == null || boss.LivePillars == 0 || amount <= 0) return amount;
        Warded++;
        return Mathf.Max(1, Mathf.CeilToInt(amount * 0.5f));
    }

    public static void ResetRun() => Warded = 0;
}
