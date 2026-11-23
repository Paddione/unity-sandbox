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
    public static string LockedHint => Loc.T("gate.locked.barrens");

    /// <summary>Waypoint shrine position (also listed in <see cref="KilnLayout.Shrines"/>).</summary>
    public static readonly Vector3 ShrinePos = BarrensOffset + new Vector3(4.5f, 0f, 12f);

    // ---- S2 enemies: Shardbacks (shield), Dune Augurs (area casters), Glass Elites ----
    public const int ShardbackCount = 8;
    public const int AugurCount = 6;
    public const int EliteCount = 2;
    public const string ShardbackPrefix = "Shardback_";
    public const string AugurPrefix = "Dune Augur_";
    public const string ElitePrefix = "Glass Elite_";
    public const int Variants = 3;
    const float Y = 0.05f;

    /// <summary>Three packs: north-centre (3 shields + 2 augurs), west (3 + 2), east (2 + 2); elites guard the south.</summary>
    static readonly Vector2[] ShardbackBase =
    {
        new Vector2(-1.5f, 2f), new Vector2(0f, 2.5f), new Vector2(1.5f, 2f),
        new Vector2(-7f, -3f), new Vector2(-5.5f, -4f), new Vector2(-8.5f, -4.5f),
        new Vector2(9f, -2f), new Vector2(10.5f, -3f),
    };
    static readonly Vector2[] AugurBase =
    {
        new Vector2(-2f, -3f), new Vector2(2f, -3f),
        new Vector2(-10.5f, -6.5f), new Vector2(-2.5f, -5.5f),
        new Vector2(9.5f, -4.5f), new Vector2(11f, -1f),
    };
    static readonly Vector2[] EliteBase = { new Vector2(-3f, -11f), new Vector2(3f, -11f) };

    /// <summary>Variants slide the whole encounter east or west (clear of dunes, ruins and spires in all three).</summary>
    public static Vector3 VariantShift(int variant) =>
        new Vector3(new[] { 0f, 1.25f, -1.25f }[Mathf.Clamp(variant, 0, Variants - 1)], 0f, 0f);

    static Vector3 At(Vector2 p, int variant) => BarrensOffset + new Vector3(p.x, Y, p.y) + VariantShift(variant);

    public static Vector3 ShardbackAnchor(int variant, int i) => At(ShardbackBase[i], variant);
    public static Vector3 AugurAnchor(int variant, int i) => At(AugurBase[i], variant);
    public static Vector3 EliteAnchor(int variant, int i) => At(EliteBase[i], variant);

    /// <summary>Static blockers the anchors keep clear of (dunes, KayKit ruins), local XZ.</summary>
    public static readonly Vector2[] DuneSpots =
        { new Vector2(-10f, 3f), new Vector2(9f, 6f), new Vector2(-6f, -9f), new Vector2(8f, -8f), new Vector2(-12.5f, 9.5f) };

    // ---- S3 zone boss: the Glasswright at the south end, its elites in front ----
    public static readonly Vector3 GlasswrightPos = BarrensOffset + new Vector3(0f, 0.05f, -14f);
    public static readonly Vector2[] RuinSpots =
        { new Vector2(-12f, -3f), new Vector2(12f, 1f), new Vector2(-4f, 7f), new Vector2(5f, -4f) };

    // ---- Fair-fights arrival plaza (like KilnLayout): safe until the hero leaves it once ----
    public const float ArrivalPlazaRadius = 5f;
    static bool plazaBreached;

    /// <summary>Waypoint arrival point (shrine + forward offset, like <see cref="WaypointShrine.Arrival"/>).</summary>
    public static Vector3 WaypointArrival => ShrinePos + Vector3.up * 0.55f + Vector3.forward * 2f;

    static bool InPlaza(Vector3 heroPos, Vector3 center)
    {
        var d = heroPos - center;
        d.y = 0f;
        return d.magnitude <= ArrivalPlazaRadius;
    }

    /// <summary>True while the hero may stand safely in the Barrens entry (headless-callable; latches on exit).</summary>
    public static bool PlazaSafe(Vector3 heroPos)
    {
        var local = heroPos - BarrensOffset;
        if (Mathf.Abs(local.x) > Extent + 5f || Mathf.Abs(local.z) > Extent + 5f) return false;
        if (InPlaza(heroPos, BarrensArrival) || InPlaza(heroPos, WaypointArrival))
            return !plazaBreached;
        plazaBreached = true;
        return false;
    }

    /// <summary>Re-arm the plaza (Barrens entry, run reset, headless tests).</summary>
    public static void ResetPlaza() => plazaBreached = false;

    public static bool PlazaBreached => plazaBreached;

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
