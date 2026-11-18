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
    public static readonly Vector3 KilnArrival = KilnOffset + new Vector3(0f, 1f, 2f);
    public static readonly Vector3 KilnReturnPos = KilnOffset + new Vector3(0f, 1f, 10f);
    /// <summary>Back in the Ossuary 8 m clear of the Kiln gate (no bounce).</summary>
    public static readonly Vector3 OssuaryReturnArrival = OssuaryOffset + new Vector3(6f, 1f, -5f);
    public const string LockedHint = "Die Glut ruht noch";
}
