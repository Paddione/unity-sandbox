using System.Collections.Generic;
using System.Text;

/// <summary>One quest's persisted state (save v13).</summary>
[System.Serializable]
public sealed class StoryQuestEntry
{
    public string id;
    public int status;
    public int[] counts = new int[0];
}

/// <summary>What changed after an event or talk, for toasts and the log.</summary>
public struct StoryChange
{
    public string questId;
    public int objective;      // -1 for status changes
    public int status;
    public int count;
}

/// <summary>
/// Story progress of one hero (cinderreach-deutsch-story S4). Pure logic, no
/// scene: <see cref="Apply"/> counts events, <see cref="Refresh"/> opens
/// quests whose prerequisite and act gate hold and books act-backed catch-up,
/// <see cref="Snapshot"/>/<see cref="Merge"/> carry the host's state to co-op
/// clients (status and counts merge by max, so nothing ever regresses).
/// </summary>
public sealed class StoryState
{
    readonly Dictionary<string, StoryQuestEntry> quests = new Dictionary<string, StoryQuestEntry>();
    public readonly HashSet<string> Flags = new HashSet<string>();

    /// <summary>Changes since the last <see cref="TakeChanges"/> (toasts read and clear them).</summary>
    readonly List<StoryChange> pending = new List<StoryChange>();

    /// <summary>Bumps on every change (co-op broadcast and HUD refresh compare it).</summary>
    public int Revision { get; private set; }

    public StoryState()
    {
        foreach (var q in StoryCatalog.All)
            quests[q.id] = new StoryQuestEntry { id = q.id, status = StoryStatus.Locked, counts = new int[q.objectives.Length] };
    }

    public int Status(string id) => quests.TryGetValue(id, out var e) ? e.status : StoryStatus.Locked;

    public int Count(string id, int objective)
    {
        if (!quests.TryGetValue(id, out var e) || objective < 0 || objective >= e.counts.Length) return 0;
        return e.counts[objective];
    }

    public bool ObjectiveDone(StoryQuest q, int i) => Count(q.id, i) >= q.objectives[i].count;

    public List<StoryChange> TakeChanges()
    {
        var copy = new List<StoryChange>(pending);
        pending.Clear();
        return copy;
    }

    void SetStatus(StoryQuestEntry e, int status)
    {
        if (e.status == status) return;
        e.status = status;
        pending.Add(new StoryChange { questId = e.id, objective = -1, status = status });
        Revision++;
    }

    static bool GateOpen(StoryGate gate, ActProgress act)
    {
        if (act == null) return gate == StoryGate.None;
        switch (gate)
        {
            case StoryGate.KilnOpen: return act.IsUnlocked(ActProgress.Kiln);
            case StoryGate.ActOneComplete: return act.ActOneComplete;
            case StoryGate.ZoneBossDefeated: return act.ZoneBossDefeated;
            default: return true;
        }
    }

    /// <summary>
    /// Opens quests whose prerequisite and gate hold, auto-starts the far-away
    /// ones and completes not-yet-started chain quests the act state already
    /// passed (old saves, late co-op joins, a boss killed before the talk).
    /// Repeats until stable; returns true when anything changed.
    /// </summary>
    public bool Refresh(ActProgress act)
    {
        bool any = false;
        for (int guard = 0; guard < 8; guard++)
        {
            bool changed = false;
            foreach (var q in StoryCatalog.All)
            {
                var e = quests[q.id];
                if (e.status < StoryStatus.Active && q.catchUp != StoryGate.None && GateOpen(q.catchUp, act))
                {
                    for (int i = 0; i < e.counts.Length; i++) e.counts[i] = q.objectives[i].count;
                    SetStatus(e, StoryStatus.Done);
                    changed = true;
                    continue;
                }
                if (e.status != StoryStatus.Locked) continue;
                if (q.prereq != null && Status(q.prereq) < q.prereqStatus) continue;
                if (!GateOpen(q.gate, act)) continue;
                SetStatus(e, q.autoAccept ? StoryStatus.Active : StoryStatus.Available);
                changed = true;
            }
            if (!changed) break;
            any = true;
        }
        return any;
    }

    /// <summary>Takes an offered quest. False unless it is <see cref="StoryStatus.Available"/>.</summary>
    public bool Accept(string id)
    {
        if (!quests.TryGetValue(id, out var e) || e.status != StoryStatus.Available) return false;
        SetStatus(e, StoryStatus.Active);
        return true;
    }

    /// <summary>Closes a <see cref="StoryStatus.Ready"/> quest after the turn-in talk.</summary>
    public bool TurnIn(string id)
    {
        if (!quests.TryGetValue(id, out var e) || e.status != StoryStatus.Ready) return false;
        SetStatus(e, StoryStatus.Done);
        return true;
    }

    /// <summary>Counts one event against every active quest. Returns true when a count moved.</summary>
    public bool Apply(StoryObjectiveType type, string target, int amount = 1)
    {
        bool any = false;
        foreach (var q in StoryCatalog.All)
        {
            var e = quests[q.id];
            if (e.status != StoryStatus.Active) continue;
            for (int i = 0; i < q.objectives.Length; i++)
            {
                var o = q.objectives[i];
                if (o.type != type || o.target != target || e.counts[i] >= o.count) continue;
                e.counts[i] = System.Math.Min(o.count, e.counts[i] + amount);
                pending.Add(new StoryChange { questId = q.id, objective = i, status = e.status, count = e.counts[i] });
                Revision++;
                any = true;
            }
            if (AllDone(q, e)) SetStatus(e, q.turnIn ? StoryStatus.Ready : StoryStatus.Done);
        }
        return any;
    }

    static bool AllDone(StoryQuest q, StoryQuestEntry e)
    {
        for (int i = 0; i < q.objectives.Length; i++)
            if (e.counts[i] < q.objectives[i].count) return false;
        return true;
    }

    /// <summary>First active main quest (the HUD tracker line), else the first active side quest, else null.</summary>
    public StoryQuest Tracked()
    {
        StoryQuest side = null;
        foreach (var q in StoryCatalog.All)
        {
            int s = Status(q.id);
            if (s != StoryStatus.Active && s != StoryStatus.Ready) continue;
            if (q.main) return q;
            if (side == null) side = q;
        }
        return side;
    }

    // ---- persistence (save v13) ----

    public List<StoryQuestEntry> ToSave()
    {
        var list = new List<StoryQuestEntry>();
        foreach (var q in StoryCatalog.All)
        {
            var e = quests[q.id];
            list.Add(new StoryQuestEntry { id = e.id, status = e.status, counts = (int[])e.counts.Clone() });
        }
        return list;
    }

    public static StoryState FromSave(List<StoryQuestEntry> entries, List<string> flags)
    {
        var s = new StoryState();
        if (entries != null)
            foreach (var src in entries)
            {
                if (src == null || src.id == null || !s.quests.TryGetValue(src.id, out var e)) continue;
                e.status = System.Math.Max(StoryStatus.Locked, System.Math.Min(StoryStatus.Done, src.status));
                if (src.counts != null)
                    for (int i = 0; i < e.counts.Length && i < src.counts.Length; i++)
                        e.counts[i] = System.Math.Max(0, src.counts[i]);
            }
        if (flags != null) foreach (var f in flags) if (!string.IsNullOrEmpty(f)) s.Flags.Add(f);
        return s;
    }

    // ---- co-op (host snapshot, max-merge on clients) ----

    /// <summary>Compact form "h1:4:1,1;h2:2:1,3,0" (only started quests, well under one packet).</summary>
    public string Snapshot()
    {
        var sb = new StringBuilder();
        foreach (var q in StoryCatalog.All)
        {
            var e = quests[q.id];
            if (e.status == StoryStatus.Locked) continue;
            if (sb.Length > 0) sb.Append(';');
            sb.Append(e.id).Append(':').Append(e.status).Append(':');
            for (int i = 0; i < e.counts.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(e.counts[i]);
            }
        }
        return sb.ToString();
    }

    /// <summary>Takes the max of every status and count in <paramref name="snapshot"/>. True when anything moved.</summary>
    public bool Merge(string snapshot)
    {
        if (string.IsNullOrEmpty(snapshot)) return false;
        bool any = false;
        foreach (var part in snapshot.Split(';'))
        {
            var f = part.Split(':');
            if (f.Length < 2 || !quests.TryGetValue(f[0], out var e)) continue;
            if (!int.TryParse(f[1], out int status)) continue;
            if (f.Length > 2 && f[2].Length > 0)
            {
                var c = f[2].Split(',');
                for (int i = 0; i < e.counts.Length && i < c.Length; i++)
                    if (int.TryParse(c[i], out int v) && v > e.counts[i])
                    {
                        e.counts[i] = v;
                        Revision++;
                        any = true;
                    }
            }
            status = System.Math.Min(StoryStatus.Done, status);
            if (status > e.status) { SetStatus(e, status); any = true; }
        }
        return any;
    }
}
