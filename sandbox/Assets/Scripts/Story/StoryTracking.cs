using System.Collections.Generic;

/// <summary>
/// Which quests the HUD tracker and the maps follow (cinderreach-group-quest-map).
/// Every active or hand-in-ready quest is tracked unless the player hid it in
/// the expedition log; the choice lives in the per-hero story flags
/// (<c>untrack:&lt;id&gt;</c>), so it is saved without a save migration and never
/// travels in the co-op snapshot. Main quests come first, then side quests,
/// both in catalog order.
/// </summary>
public static class StoryTracking
{
    public const string HiddenPrefix = "untrack:";

    /// <summary>Most quests the HUD tracker lists at once.</summary>
    public const int MaxTracked = 4;

    public static bool IsOpen(StoryState s, string id)
    {
        int st = s.Status(id);
        return st == StoryStatus.Active || st == StoryStatus.Ready;
    }

    public static bool IsHidden(StoryState s, string id) => s != null && s.Flags.Contains(HiddenPrefix + id);

    public static bool IsTracked(StoryState s, string id) => s != null && IsOpen(s, id) && !IsHidden(s, id);

    /// <summary>Flips tracking of <paramref name="id"/>; returns the new tracked state.</summary>
    public static bool Toggle(StoryState s, string id)
    {
        if (s == null || StoryCatalog.Get(id) == null) return false;
        if (s.Flags.Remove(HiddenPrefix + id)) return IsOpen(s, id);
        s.Flags.Add(HiddenPrefix + id);
        return false;
    }

    /// <summary>Tracked quests, main first, at most <paramref name="max"/>.</summary>
    public static List<StoryQuest> Tracked(StoryState s, int max = MaxTracked)
    {
        var list = new List<StoryQuest>();
        if (s == null) return list;
        foreach (bool main in new[] { true, false })
            foreach (var q in StoryCatalog.All)
            {
                if (list.Count >= max) return list;
                if (q.main == main && IsTracked(s, q.id)) list.Add(q);
            }
        return list;
    }

    /// <summary>Quests a giver offers right now (log section and "!" map marks).</summary>
    public static List<StoryQuest> Offers(StoryState s)
    {
        var list = new List<StoryQuest>();
        if (s == null) return list;
        foreach (var q in StoryCatalog.All)
            if (s.Status(q.id) == StoryStatus.Available) list.Add(q);
        return list;
    }

    /// <summary>
    /// Open objective indices of <paramref name="q"/>. A hand-in-ready quest has
    /// none (the giver is the target), a done or locked quest has none either.
    /// </summary>
    public static List<int> OpenObjectives(StoryState s, StoryQuest q)
    {
        var list = new List<int>();
        if (s == null || q == null || s.Status(q.id) != StoryStatus.Active) return list;
        for (int i = 0; i < q.objectives.Length; i++)
            if (!s.ObjectiveDone(q, i)) list.Add(i);
        return list;
    }

    /// <summary>One tracker row for objective <paramref name="i"/>: text plus "n/m" for counted goals.</summary>
    public static string ObjectiveRow(StoryState s, StoryQuest q, int i)
    {
        var o = q.objectives[i];
        string row = Loc.T(o.textKey);
        if (o.count > 1) row += "  " + System.Math.Min(o.count, s.Count(q.id, i)) + "/" + o.count;
        return row;
    }
}
