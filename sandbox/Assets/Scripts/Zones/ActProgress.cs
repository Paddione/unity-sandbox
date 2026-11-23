using System.Collections.Generic;

/// <summary>
/// Act progression (cinderreach-act-one S1, act-two-opening S1). Zones: 1 Ashen
/// Fen with Lantern Hold, 2 Sunken Ossuary (open from the start, as before),
/// 3 Cinder Kiln (unlocked by the first Hollow Abbot kill), 4 Glass Barrens
/// (Act II, opened by the Kilnmother, which completes Act I). The Barrens'
/// zone boss sets <see cref="ZoneBossDefeated"/>. Waypoints remember discovered
/// shrines. Pure C#, persisted through save v4/v11 via <see cref="RunProfile.Act"/>.
/// </summary>
public sealed class ActProgress
{
    public const int Town = 1;
    public const int Ossuary = 2;
    public const int Kiln = 3;
    public const int Barrens = 4;
    public const int FirstLockedZone = Kiln;
    public const int LastZone = Barrens;

    /// <summary>Display name of a zone index (Town covers Lantern Hold and the Ashen Fen, cinderreach-deutsch-story).</summary>
    public static string ZoneName(int zone)
    {
        switch (zone)
        {
            case Ossuary: return Loc.T("name.zone.ossuary");
            case Kiln: return Loc.T("name.zone.kiln");
            case Barrens: return Loc.T("name.zone.barrens");
            default: return Loc.T("name.zone.fen");
        }
    }

    public int HighestUnlocked { get; private set; } = Ossuary;
    public bool ActOneComplete { get; private set; }
    /// <summary>act-two-opening: the Glass Barrens' zone boss has fallen.</summary>
    public bool ZoneBossDefeated { get; private set; }
    public readonly List<string> Waypoints = new List<string>();

    public bool IsUnlocked(int zone) => zone >= Town && zone <= HighestUnlocked;

    /// <summary>
    /// Open a zone. Zones open in order; true only when it was locked. The
    /// Barrens additionally need Act I completed (the Kilnmother's door).
    /// </summary>
    public bool Unlock(int zone)
    {
        if (zone <= HighestUnlocked || zone > LastZone) return false;
        if (zone != HighestUnlocked + 1) return false; // no skipping ahead
        if (zone == Barrens && !ActOneComplete) return false;
        HighestUnlocked = zone;
        return true;
    }

    /// <summary>
    /// Kilnmother down: Act I is done and the Glass Barrens open. Only valid
    /// with the Kiln open. True the first time.
    /// </summary>
    public bool CompleteAct()
    {
        if (ActOneComplete || !IsUnlocked(Kiln)) return false;
        ActOneComplete = true;
        Unlock(Barrens);
        return true;
    }

    /// <summary>Barrens boss down. Only valid with the Barrens open. True the first time.</summary>
    public bool DefeatZoneBoss()
    {
        if (ZoneBossDefeated || !IsUnlocked(Barrens)) return false;
        ZoneBossDefeated = true;
        return true;
    }

    public bool Discover(string waypoint)
    {
        if (string.IsNullOrEmpty(waypoint) || Waypoints.Contains(waypoint)) return false;
        Waypoints.Add(waypoint);
        return true;
    }

    public static ActProgress FromSave(int highestUnlocked, bool actOneComplete, List<string> waypoints) =>
        FromSave(highestUnlocked, actOneComplete, false, waypoints);

    public static ActProgress FromSave(int highestUnlocked, bool actOneComplete, bool zoneBossDefeated, List<string> waypoints)
    {
        var a = new ActProgress();
        a.HighestUnlocked = System.Math.Max(Ossuary, System.Math.Min(LastZone, highestUnlocked));
        a.ActOneComplete = actOneComplete && a.HighestUnlocked >= Kiln;
        // The Barrens never stand open without the Kilnmother down (no skipping via a hand-edited save).
        if (a.HighestUnlocked >= Barrens && !a.ActOneComplete) a.HighestUnlocked = Kiln;
        a.ZoneBossDefeated = zoneBossDefeated && a.HighestUnlocked >= Barrens;
        if (waypoints != null)
            foreach (var w in waypoints) a.Discover(w);
        return a;
    }
}
