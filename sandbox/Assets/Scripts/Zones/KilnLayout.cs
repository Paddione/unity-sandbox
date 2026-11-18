using UnityEngine;

/// <summary>
/// Cinder Kiln layout constants (act-one S2), shared by the editor-side
/// KilnBuilder and runtime/tests. Zone 3 sits at +400 m X like the Ossuary
/// at +200 m.
/// </summary>
public static class KilnLayout
{
    public static readonly Vector3 OssuaryOffset = new Vector3(200f, 0f, 0f);
    public static readonly Vector3 KilnOffset = new Vector3(400f, 0f, 0f);
    public const string RootName = "KilnRoot";
    public const int KilnDomes = 6;
    public const int EmberLights = 6;
    /// <summary>Ossuary-side gate, east of the apse and clear of the pillar ring.</summary>
    public static readonly Vector3 KilnGatePos = OssuaryOffset + new Vector3(6f, 1f, -13f);
    /// <summary>Arrival in the north-east corner, at least 9 m from every thrall and elite anchor in all variants (was between two packs).</summary>
    public static readonly Vector3 KilnArrival = KilnOffset + new Vector3(13f, 1f, 13f);
    public static readonly Vector3 KilnReturnPos = KilnOffset + new Vector3(0f, 1f, 10f);
    /// <summary>Back in the Ossuary 8 m clear of the Kiln gate (no bounce).</summary>
    public static readonly Vector3 OssuaryReturnArrival = OssuaryOffset + new Vector3(6f, 1f, -5f);
    public const string LockedHint = "Die Glut ruht noch";

    // ---- S3 enemies: three anchor variants, picked by LayoutSeeder ----
    public const int ThrallCount = 18;
    public const int LancerCount = 6;
    public const int EliteCount = 2;
    public const string ThrallPrefix = "Cinder Thrall_";
    public const string LancerPrefix = "Ash Lancer_";
    public const string ElitePrefix = "Kiln Elite_";
    public const int Variants = 3;
    const float Y = 0.05f;

    /// <summary>Six packs of three around the ring; variants rotate and widen the packs.</summary>
    public static Vector3 ThrallAnchor(int variant, int i)
    {
        int group = i / 3, member = i % 3;
        float a = (group * 60f + variant * 20f) * Mathf.Deg2Rad;
        float r = 6f + (variant == 2 ? 1.2f : 0f);
        var center = new Vector3(Mathf.Cos(a) * r, Y, Mathf.Sin(a) * r);
        float ma = member * 120f * Mathf.Deg2Rad + a;
        return KilnOffset + center + new Vector3(Mathf.Cos(ma) * 1.3f, 0f, Mathf.Sin(ma) * 1.3f);
    }

    public static Vector3 LancerAnchor(int variant, int i)
    {
        float a = (i * 60f + 30f + variant * 25f) * Mathf.Deg2Rad;
        float r = 8.6f - variant * 0.8f;
        return KilnOffset + new Vector3(Mathf.Cos(a) * r, Y, Mathf.Sin(a) * r);
    }

    public static Vector3 EliteAnchor(int variant, int i) =>
        KilnOffset + new Vector3((i == 0 ? -1f : 1f) * (3f + variant), Y, -3.5f + variant * 2f);

    // ---- S5 waypoint shrines: id, name, zone, position ----
    public static readonly (string id, string name, int zone, Vector3 pos)[] Shrines =
    {
        ("Town", "Lantern Hold", ActProgress.Town, new Vector3(1.5f, 0f, 14.5f)),
        ("Fen", "Ashen Fen", ActProgress.Town, new Vector3(-15f, 0f, -4f)),
        ("Ossuary", "Sunken Ossuary", ActProgress.Ossuary, OssuaryOffset + new Vector3(-5f, 0f, 10f)),
        ("Kiln", "Cinder Kiln", ActProgress.Kiln, KilnOffset + new Vector3(3.5f, 0f, 9.5f)),
    };

    // ---- S4 boss ----
    public const string KilnmotherName = "The Kilnmother";
    public static readonly Vector3 KilnmotherPos = KilnOffset + new Vector3(0f, 0.05f, -7.5f);
}
