using UnityEngine;

/// <summary>
/// Glass Barrens layout constants (act-two-opening S1), shared by the
/// editor-side BarrensBuilder and runtime/tests. Zone 4 — the first zone of
/// Act II — sits at +600 m X: a vitrified ash plain the Kiln's fire turned to
/// glass. The gate leaves the Kiln south of the Kilnmother's ring.
/// </summary>
public static class BarrensLayout
{
    public static readonly Vector3 BarrensOffset = new Vector3(600f, 0f, 0f);
    /// <summary>Kiln offset, mirrored here: <see cref="KilnLayout.Shrines"/> reads this class, so
    /// reading KilnLayout back from a static initializer would cycle (zeroed fields).</summary>
    static readonly Vector3 KilnOffset = new Vector3(400f, 0f, 0f);
    public const string RootName = "BarrensRoot";
    public const string DisplayName = "Glass Barrens";
    public const int ShardSpires = 10;
    public const int GlassLights = 6;
    public const int Dunes = 5;
    /// <summary>Half-extent of the Barrens floor (plane scale 4 = 40 m).</summary>
    public const float Extent = 20f;

    /// <summary>Kiln-side gate, south of the dome ring and west of the Kilnmother's dais.</summary>
    public static readonly Vector3 BarrensGatePos = KilnOffset + new Vector3(-6f, 1f, -13.5f);
    /// <summary>Arrival at the north edge of the Barrens.</summary>
    public static readonly Vector3 BarrensArrival = BarrensOffset + new Vector3(0f, 1f, 13f);
    /// <summary>Return gate in the north-west corner of the Barrens.</summary>
    public static readonly Vector3 BarrensReturnPos = BarrensOffset + new Vector3(-6f, 1f, 17f);
    /// <summary>Back in the Kiln 5 m clear of the Barrens gate (no bounce).</summary>
    public static readonly Vector3 KilnReturnArrival = KilnOffset + new Vector3(-6f, 1f, -8.5f);
    public const string LockedHint = "The glass stays sealed";

    /// <summary>Waypoint shrine position (also listed in <see cref="KilnLayout.Shrines"/>).</summary>
    public static readonly Vector3 ShrinePos = BarrensOffset + new Vector3(4.5f, 0f, 12f);

    /// <summary>Shard spire anchors: an outer ring broken into east and west arcs, leaving the north and south lanes open.</summary>
    public static Vector3 SpireAnchor(int i)
    {
        int arc = i / (ShardSpires / 2), k = i % (ShardSpires / 2);
        float deg = (arc == 0 ? -50f : 130f) + k * 25f;
        float a = deg * Mathf.Deg2Rad;
        float r = 14f + (k % 2) * 1.5f;
        return BarrensOffset + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
    }
}
