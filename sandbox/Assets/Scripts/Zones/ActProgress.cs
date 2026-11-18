using System.Collections.Generic;

/// <summary>
/// Act I progression (cinderreach-act-one S1). Zones: 1 Ashen Fen with
/// Lantern Hold, 2 Sunken Ossuary (open from the start, as before), 3 Cinder
/// Kiln (unlocked by the first Hollow Abbot kill). The act completes with the
/// Kilnmother. Waypoints remember discovered shrines (S5). Pure C#, persisted
/// through save v4 via <see cref="RunProfile.Act"/>.
/// </summary>
public sealed class ActProgress
{
    public const int Town = 1;
    public const int Ossuary = 2;
    public const int Kiln = 3;
    public const int FirstLockedZone = Kiln;

    public int HighestUnlocked { get; private set; } = Ossuary;
    public bool ActOneComplete { get; private set; }
    public readonly List<string> Waypoints = new List<string>();

    public bool IsUnlocked(int zone) => zone >= Town && zone <= HighestUnlocked;

    /// <summary>Open a zone. Zones open in order; true only when it was locked.</summary>
    public bool Unlock(int zone)
    {
        if (zone <= HighestUnlocked || zone > Kiln) return false;
        if (zone != HighestUnlocked + 1) return false; // no skipping ahead
        HighestUnlocked = zone;
        return true;
    }

    /// <summary>Kilnmother down: the act is done. Only valid with the Kiln open. True the first time.</summary>
    public bool CompleteAct()
    {
        if (ActOneComplete || !IsUnlocked(Kiln)) return false;
        ActOneComplete = true;
        return true;
    }

    public bool Discover(string waypoint)
    {
        if (string.IsNullOrEmpty(waypoint) || Waypoints.Contains(waypoint)) return false;
        Waypoints.Add(waypoint);
        return true;
    }

    public static ActProgress FromSave(int highestUnlocked, bool actOneComplete, List<string> waypoints)
    {
        var a = new ActProgress();
        a.HighestUnlocked = System.Math.Max(Ossuary, System.Math.Min(Kiln, highestUnlocked));
        a.ActOneComplete = actOneComplete && a.HighestUnlocked >= Kiln;
        if (waypoints != null)
            foreach (var w in waypoints) a.Discover(w);
        return a;
    }
}
