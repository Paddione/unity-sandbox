using System.Collections.Generic;

/// <summary>One scripted conversation: line i is catalog key <c>dlg.&lt;id&gt;.&lt;i+1&gt;</c>.</summary>
public sealed class StoryDialog
{
    public readonly string id;
    /// <summary>Speaker per line: an NPC id or "n" for narration; "o|flag" shows the line only with that story flag, "o|!flag" only without.</summary>
    public readonly string[] speakers;

    public StoryDialog(string id, params string[] speakers)
    {
        this.id = id;
        this.speakers = speakers;
    }

    public string Key(int i) => "dlg." + id + "." + (i + 1);

    public static string Speaker(string code)
    {
        int bar = code.IndexOf('|');
        return bar < 0 ? code : code.Substring(0, bar);
    }

    public static string Flag(string code)
    {
        int bar = code.IndexOf('|');
        return bar < 0 ? null : code.Substring(bar + 1);
    }

    /// <summary>Line filter: "o|flag" needs the flag, "o|!flag" needs it absent.</summary>
    public static bool Visible(string code, ICollection<string> flags)
    {
        string flag = Flag(code);
        if (string.IsNullOrEmpty(flag)) return true;
        bool negate = flag[0] == '!';
        if (negate) flag = flag.Substring(1);
        bool has = flags != null && flags.Contains(flag);
        return negate ? !has : has;
    }
}

public enum StoryTalkKind { None, TurnIn, Talk, Offer, Hint, Idle }

/// <summary>What talking to an NPC does right now: which script, and what closing it books.</summary>
public struct StoryTalkPlan
{
    public StoryTalkKind kind;
    public string questId;
    public StoryDialog dialog;
    public bool IsPending => kind == StoryTalkKind.TurnIn || kind == StoryTalkKind.Talk || kind == StoryTalkKind.Offer;
}

/// <summary>
/// The scripts (docs/story-bible.md voices) and the rule which one an NPC
/// speaks: turn-in, then a talk objective, then a new offer, then a reminder,
/// then idle chatter. Shops (Ilsabe, Jorin) speak only the first three and
/// otherwise open their window as before.
/// </summary>
public static class StoryDialogs
{
    const string O = StoryIds.Ottilie, I = StoryIds.Ilsabe, J = StoryIds.Jorin, M = StoryIds.Merle, N = "n";

    public static readonly StoryDialog[] All =
    {
        new StoryDialog("h1.offer", O, O, O, O),
        new StoryDialog("h1.hint", O),
        new StoryDialog("h1.done", O, N, O, O, O),
        new StoryDialog("h2.offer", O, O, O, O, O + "|n8"),
        new StoryDialog("h2.hint", O),
        new StoryDialog("h2.done", O, O),
        new StoryDialog("h3.offer", N, O, O, O, O, O + "|n2", O, O + "|n9"),
        new StoryDialog("h3.hint", O),
        new StoryDialog("h3.talk.jorin", J, J, J, J, J),
        new StoryDialog("h4.hint", O),
        new StoryDialog("h4.done", O, O, O + "|n3", O, O + "|n10"),
        new StoryDialog("h5.offer", N, O, O, O, O + "|n1", O, O + "|n11"),
        new StoryDialog("h5.hint", O),
        new StoryDialog("h5.talk.ilsabe", I, I, I, I),
        new StoryDialog("h5.done", O, N, O, O, O + "|n11", O + "|!n11"),
        new StoryDialog("h6.offer", N, M, M, M, M, M),
        new StoryDialog("h6.hint", M),
        new StoryDialog("h7.hint", M),
        new StoryDialog("h8.hint", M),
        new StoryDialog("h9.talk.merle", M, M, M, M + "|n4", M + "|!n4", M),
        new StoryDialog("h9.talk.ottilie", N, O, O, O + "|n5", N, O),
        new StoryDialog("n1.offer", I, I, I),
        new StoryDialog("n1.hint", I),
        new StoryDialog("n1.done", I, I, I),
        new StoryDialog("n2.offer", O, O),
        new StoryDialog("n2.hint", O),
        new StoryDialog("n2.done", O, O, O),
        new StoryDialog("n3.offer", J, J),
        new StoryDialog("n3.hint", J),
        new StoryDialog("n3.done", J, J, J),
        new StoryDialog("n4.offer", M, M, M),
        new StoryDialog("n4.hint", M),
        new StoryDialog("n4.done", N, M, M),
        new StoryDialog("n5.offer", I, I, I),
        new StoryDialog("n5.hint", I),
        new StoryDialog("n5.done", I, I),
        // ---- Akt I Vertiefung S2: Moor/Beinhaus (R1 Fuhrfetzen, R2 siebte Tafel) ----
        // R1/R2 nutzen Bestandssysteme: Fundstuecke ueber StoryPickup, Gespraeche
        // ueber Folgezeilen (x|nK). N8 aendert H2-Angebot und H14-Abschluss,
        // N9 aendert H3-Angebot; die siebte Tafel liest sich erst mit Grablicht.
        new StoryDialog("h14.offer", O, O, O, O),
        new StoryDialog("h14.hint", O),
        new StoryDialog("h14.done", O, O, O + "|n8"),
        new StoryDialog("h15.offer", O, O, O),
        new StoryDialog("h15.hint", O),
        new StoryDialog("h15.talk.ottilie", O, O, O),
        new StoryDialog("n8.offer", O, O),
        new StoryDialog("n8.hint", O),
        new StoryDialog("n8.done", O, O),
        new StoryDialog("n9.offer", O, O),
        new StoryDialog("n9.hint", O),
        new StoryDialog("n9.done", O, O, O),
        // ---- Akt I Vertiefung S3: Ofen/Wacht (R3 Siegel-Entscheidung, H5-Finale) ----
        // R3 nutzt Bestandssysteme: Fundstuecke ueber StoryPickup, Entscheidungen
        // ueber Gespraeche mit Folgezeilen (x|nK). N10 aendert den H4-Abschluss,
        // N11 das H5-Angebot; das H5-Finale steht je nach N11 in zwei Fassungen.
        new StoryDialog("h16.offer", J, J, J, J),
        new StoryDialog("h16.hint", J),
        new StoryDialog("h16.done", J, J, J, J),
        new StoryDialog("h17.offer", O, O, O),
        new StoryDialog("h17.talk.ilsabe", I, I, I, I),
        new StoryDialog("h17.talk.ottilie", O, O, O, O),
        new StoryDialog("n10.offer", J, J),
        new StoryDialog("n10.hint", J),
        new StoryDialog("n10.done", J, J),
        new StoryDialog("n11.offer", I, I, I),
        new StoryDialog("n11.hint", I),
        new StoryDialog("n11.done", I, I),
        new StoryDialog("idle.ottilie.1", O),
        new StoryDialog("idle.ottilie.2", O),
        new StoryDialog("idle.ottilie.3", O),
        new StoryDialog("idle.merle.1", M),
    };

    static Dictionary<string, StoryDialog> byId;

    public static StoryDialog Get(string id)
    {
        if (byId == null)
        {
            byId = new Dictionary<string, StoryDialog>();
            foreach (var d in All) byId[d.id] = d;
        }
        return id != null && byId.TryGetValue(id, out var dlg) ? dlg : null;
    }

    /// <summary>NPCs whose world click opens a shop when no story talk is pending.</summary>
    public static bool IsShop(string npc) => npc == StoryIds.Ilsabe || npc == StoryIds.Jorin;

    /// <summary>The conversation <paramref name="npc"/> holds for <paramref name="s"/> right now.</summary>
    public static StoryTalkPlan Plan(StoryState s, string npc)
    {
        if (s == null) return default;
        foreach (var q in StoryCatalog.All)
            if (q.giver == npc && s.Status(q.id) == StoryStatus.Ready)
                return Make(StoryTalkKind.TurnIn, q.id, q.id + ".done");
        foreach (var q in StoryCatalog.All)
        {
            if (s.Status(q.id) != StoryStatus.Active) continue;
            for (int i = 0; i < q.objectives.Length; i++)
                if (q.objectives[i].type == StoryObjectiveType.Talk && q.objectives[i].target == npc && !s.ObjectiveDone(q, i))
                    return Make(StoryTalkKind.Talk, q.id, q.id + ".talk." + npc);
        }
        foreach (var q in StoryCatalog.All)
            if (q.giver == npc && s.Status(q.id) == StoryStatus.Available)
                return Make(StoryTalkKind.Offer, q.id, q.id + ".offer");
        foreach (var q in StoryCatalog.All)
            if (q.giver == npc && s.Status(q.id) == StoryStatus.Active && Get(q.id + ".hint") != null)
                return Make(StoryTalkKind.Hint, q.id, q.id + ".hint");
        // Merle relays the Act II chain even though Ottilie closes it.
        if (npc == StoryIds.Merle)
            foreach (var id in new[] { "h7", "h8" })
                if (s.Status(id) == StoryStatus.Active) return Make(StoryTalkKind.Hint, id, id + ".hint");
        if (IsShop(npc)) return default;
        return Make(StoryTalkKind.Idle, null, IdleFor(s, npc));
    }

    static string IdleFor(StoryState s, string npc)
    {
        if (npc == StoryIds.Merle) return "idle.merle.1";
        if (s.Status("h9") == StoryStatus.Done) return "idle.ottilie.3";
        if (s.Status("h5") == StoryStatus.Done) return "idle.ottilie.2";
        return "idle.ottilie.1";
    }

    static StoryTalkPlan Make(StoryTalkKind kind, string quest, string dialogId) =>
        new StoryTalkPlan { kind = kind, questId = quest, dialog = Get(dialogId) };
}
