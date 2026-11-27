using System.Collections.Generic;

/// <summary>
/// Autoplay quest planning (pure, EditMode-testable): what the story state
/// wants from the hero right now. Talks come from <see cref="StoryDialogs.Plan"/>
/// (offers, talk objectives, hand-ins), finds and kills from the open
/// objectives of active quests. Boss kills are left to the route's boss phases.
/// </summary>
public static class AutoplayQuests
{
    public static readonly string[] Npcs = { StoryIds.Ottilie, StoryIds.Ilsabe, StoryIds.Jorin, StoryIds.Merle };

    static readonly HashSet<string> BossTargets = new HashSet<string>
        { StoryIds.Abbot, StoryIds.Kilnmother, StoryIds.Glasswright };

    public static bool IsBossTarget(string killTarget) => BossTargets.Contains(killTarget);

    /// <summary>NPCs with a pending talk (offer, talk objective or hand-in).</summary>
    public static List<string> PendingTalks(StoryState s)
    {
        var list = new List<string>();
        foreach (var npc in Npcs)
            if (StoryDialogs.Plan(s, npc).IsPending) list.Add(npc);
        return list;
    }

    /// <summary>Story items an active quest still needs.</summary>
    public static HashSet<string> WantedFinds(StoryState s) => Open(s, StoryObjectiveType.Collect, false);

    /// <summary>Kill targets an active quest still needs (bosses excluded unless asked).</summary>
    public static HashSet<string> WantedKills(StoryState s, bool includeBosses = false) =>
        Open(s, StoryObjectiveType.Kill, includeBosses);

    static HashSet<string> Open(StoryState s, StoryObjectiveType type, bool includeBosses)
    {
        var set = new HashSet<string>();
        if (s == null) return set;
        foreach (var q in StoryCatalog.All)
        {
            if (s.Status(q.id) != StoryStatus.Active) continue;
            for (int i = 0; i < q.objectives.Length; i++)
            {
                var o = q.objectives[i];
                if (o.type != type || s.ObjectiveDone(q, i)) continue;
                if (type == StoryObjectiveType.Kill && !includeBosses && IsBossTarget(o.target)) continue;
                set.Add(o.target);
            }
        }
        return set;
    }
}

/// <summary>
/// Watches quest statuses during the run and tells played quests apart from
/// ones the act state closed by catch-up (Done without ever being seen
/// Active or Ready, <see cref="StoryState.Refresh"/>).
/// </summary>
public sealed class AutoplayQuestLedger
{
    readonly HashSet<string> seenStarted = new HashSet<string>();
    readonly HashSet<string> doneAtStart = new HashSet<string>();

    public AutoplayQuestLedger(StoryState s)
    {
        foreach (var q in StoryCatalog.All)
            if (s != null && s.Status(q.id) == StoryStatus.Done) doneAtStart.Add(q.id);
        Observe(s);
    }

    public void Observe(StoryState s)
    {
        if (s == null) return;
        foreach (var q in StoryCatalog.All)
        {
            int st = s.Status(q.id);
            if (st == StoryStatus.Active || st == StoryStatus.Ready) seenStarted.Add(q.id);
        }
    }

    public int Total => StoryCatalog.All.Length;

    public List<string> Done(StoryState s) => Filter(s, st => st == StoryStatus.Done);

    public List<string> Open(StoryState s) => Filter(s, st => st != StoryStatus.Done);

    /// <summary>Done this run without being played (catch-up).</summary>
    public List<string> CaughtUp(StoryState s)
    {
        var list = new List<string>();
        foreach (var q in StoryCatalog.All)
            if (s.Status(q.id) == StoryStatus.Done && !seenStarted.Contains(q.id) && !doneAtStart.Contains(q.id))
                list.Add(q.id);
        return list;
    }

    List<string> Filter(StoryState s, System.Func<int, bool> keep)
    {
        var list = new List<string>();
        foreach (var q in StoryCatalog.All)
            if (keep(s.Status(q.id))) list.Add(q.id);
        return list;
    }
}
