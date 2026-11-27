using System.Collections.Generic;

/// <summary>
/// Story tasks as data (cinderreach-deutsch-story S4). Ids, NPCs, objectives
/// and dialog keys follow docs/story-bible.md. Texts live in the catalog
/// (LocCatalog.Story.cs, LocCatalog.Dialog.cs); this file only wires them.
/// Quest ids and objective order are a save contract: append, never reorder.
/// </summary>
public static class StoryIds
{
    // NPCs (Vendor and Smith are the existing scene stations).
    public const string Ottilie = "ottilie";
    public const string Ilsabe = "ilsabe";
    public const string Jorin = "jorin";
    public const string Merle = "merle";

    // Story items (pickups in the world or found on a boss).
    public const string Fuhrbrief = "fuhrbrief";
    public const string Lieferbuch = "lieferbuch";
    public const string Grabtafel = "grabtafel";
    // Akt-I-Vertiefung (S1): R1 Fuhrbrief-Kombination, R2 siebte Tafel, R3 Siegel-Strang.
    public const string Fuhrfetzen = "fuhrfetzen";
    public const string Siegelband = "siegelband";
    public const string Tafelbruch = "tafelbruch";
    public const string Grablicht = "grablicht";
    public const string Fuhrliste = "fuhrliste";
    public const string Russseite = "russseite";
    // Akt-II-Vertiefung (S1): R4 Sehglas-Kombination, R5 Scherben-Ordnung,
    // R6 behalten/abliefern-Entscheidung, Tamm-/Ilsabe-Folgen.
    public const string Sehglas = "sehglas";
    public const string Glasscherbe = "glasscherbe";
    public const string Speichenrad = "speichenrad";
    public const string Spendenseite = "spendenseite";

    // Zones as WorldBounds names.
    public const string ZoneOssuary = "Ossuary";
    public const string ZoneKiln = "Kiln";
    public const string ZoneBarrens = "Barrens";

    // Kill targets: scene names without index suffix and leading "The ".
    public const string FenBrute = "Fen Brute";
    public const string BoneWarden = "Bone Warden";
    public const string Abbot = "Hollow Abbot";
    public const string AshLancer = "Ash Lancer";
    public const string CinderThrall = "Cinder Thrall";
    public const string Kilnmother = "Kilnmother";
    public const string DuneAugur = "Dune Augur";
    public const string GlassElite = "Glass Elite";
    public const string Glasswright = "Glasswright";

    /// <summary>Kill-target form of a scene object name: "The Kilnmother(Clone)" → "Kilnmother", "Ash Lancer_3" → "Ash Lancer".</summary>
    public static string KillTarget(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return "";
        string n = objectName.Replace("(Clone)", "").Trim();
        int us = n.IndexOf('_');
        if (us > 0) n = n.Substring(0, us).Trim();
        if (n.StartsWith("The ")) n = n.Substring(4);
        if (n == "Kiln Summon") n = "Cinder Thrall";
        return n;
    }
}

public enum StoryObjectiveType { Talk, Collect, Kill, Enter }

/// <summary>Quest lifecycle; values are persisted (save v13) and merged by max in co-op.</summary>
public static class StoryStatus
{
    public const int Locked = 0;
    public const int Available = 1;
    public const int Active = 2;
    public const int Ready = 3;   // objectives done, waiting for the turn-in talk
    public const int Done = 4;
}

/// <summary>Act state a quest waits for (besides its prerequisite).</summary>
public enum StoryGate { None, KilnOpen, ActOneComplete, ZoneBossDefeated }

public enum StoryReward { None, Gold, SmithKit, RareHelm, SeerCirclet }

public sealed class StoryObjective
{
    public readonly StoryObjectiveType type;
    public readonly string target;
    public readonly int count;
    /// <summary>Catalog key of the objective line in the quest log.</summary>
    public readonly string textKey;

    public StoryObjective(StoryObjectiveType type, string target, int count, string textKey)
    {
        this.type = type;
        this.target = target;
        this.count = count < 1 ? 1 : count;
        this.textKey = textKey;
    }
}

public sealed class StoryQuest
{
    public readonly string id;
    public readonly int act;
    public readonly bool main;
    public readonly string giver;
    public readonly StoryObjective[] objectives;
    /// <summary>Quest that must reach <see cref="prereqStatus"/> first (null: none).</summary>
    public readonly string prereq;
    public readonly int prereqStatus;
    public readonly StoryGate gate;
    /// <summary>Starts by itself once available (the giver is far away).</summary>
    public readonly bool autoAccept;
    /// <summary>Needs a turn-in talk with the giver once the objectives are done.</summary>
    public readonly bool turnIn;
    /// <summary>Act state that completes this quest retroactively while it is not yet started.</summary>
    public readonly StoryGate catchUp;
    public readonly StoryReward reward;
    public readonly int rewardGold;

    public StoryQuest(string id, int act, bool main, string giver, StoryObjective[] objectives,
        string prereq = null, int prereqStatus = StoryStatus.Done, StoryGate gate = StoryGate.None,
        bool autoAccept = false, bool turnIn = true, StoryGate catchUp = StoryGate.None,
        StoryReward reward = StoryReward.None, int rewardGold = 0)
    {
        this.id = id;
        this.act = act;
        this.main = main;
        this.giver = giver;
        this.objectives = objectives;
        this.prereq = prereq;
        this.prereqStatus = prereqStatus;
        this.gate = gate;
        this.autoAccept = autoAccept;
        this.turnIn = turnIn;
        this.catchUp = catchUp;
        this.reward = reward;
        this.rewardGold = rewardGold;
    }

    public string TitleKey => "quest." + id + ".title";
    public string SummaryKey => "quest." + id + ".summary";
}

/// <summary>All story tasks in narrative order (docs/story-bible.md, Akt I mit Vertiefung H14-H17/N8-N11, Akt II mit Vertiefung H18-H19/N12-N15).</summary>
public static class StoryCatalog
{
    static StoryObjective Talk(string npc, string key) => new StoryObjective(StoryObjectiveType.Talk, npc, 1, key);
    static StoryObjective Collect(string item, int n, string key) => new StoryObjective(StoryObjectiveType.Collect, item, n, key);
    static StoryObjective Kill(string target, int n, string key) => new StoryObjective(StoryObjectiveType.Kill, target, n, key);
    static StoryObjective Enter(string zone, string key) => new StoryObjective(StoryObjectiveType.Enter, zone, 1, key);

    public static readonly StoryQuest[] All =
    {
        // ---- Akt I: Wer das Licht bezahlt ----
        new StoryQuest("h1", 1, true, StoryIds.Ottilie, new[]
        {
            Collect(StoryIds.Fuhrbrief, 1, "quest.h1.obj.letter"),
            Kill(StoryIds.FenBrute, 1, "quest.h1.obj.brute"),
        }, catchUp: StoryGate.KilnOpen),
        new StoryQuest("h2", 1, true, StoryIds.Ottilie, new[]
        {
            Enter(StoryIds.ZoneOssuary, "quest.h2.obj.enter"),
            Kill(StoryIds.BoneWarden, 4, "quest.h2.obj.wardens"),
            Kill(StoryIds.Abbot, 1, "quest.h2.obj.abbot"),
        }, prereq: "h1", catchUp: StoryGate.KilnOpen),
        new StoryQuest("h3", 1, true, StoryIds.Ottilie, new[]
        {
            Talk(StoryIds.Jorin, "quest.h3.obj.jorin"),
            Enter(StoryIds.ZoneKiln, "quest.h3.obj.enter"),
        }, prereq: "h2", turnIn: false, catchUp: StoryGate.ActOneComplete),
        new StoryQuest("h4", 1, true, StoryIds.Ottilie, new[]
        {
            Kill(StoryIds.Kilnmother, 1, "quest.h4.obj.kilnmother"),
        }, prereq: "h3", autoAccept: true, catchUp: StoryGate.ActOneComplete),
        new StoryQuest("h5", 1, true, StoryIds.Ottilie, new[]
        {
            Talk(StoryIds.Ilsabe, "quest.h5.obj.ilsabe"),
        }, prereq: "h4"),
        new StoryQuest("n1", 1, false, StoryIds.Ilsabe, new[]
        {
            Collect(StoryIds.Lieferbuch, 1, "quest.n1.obj.ledger"),
        }, prereq: "h1", prereqStatus: StoryStatus.Active, reward: StoryReward.Gold, rewardGold: 150),
        new StoryQuest("n2", 1, false, StoryIds.Ottilie, new[]
        {
            Collect(StoryIds.Grabtafel, 3, "quest.n2.obj.tablets"),
        }, prereq: "h2", prereqStatus: StoryStatus.Active, reward: StoryReward.RareHelm),
        new StoryQuest("n3", 1, false, StoryIds.Jorin, new[]
        {
            Kill(StoryIds.AshLancer, 6, "quest.n3.obj.lancers"),
        }, gate: StoryGate.KilnOpen, reward: StoryReward.SmithKit, rewardGold: 50),

        // ---- Akt II: Was im Glas wohnt ----
        new StoryQuest("h6", 2, true, StoryIds.Merle, new[]
        {
            Enter(StoryIds.ZoneBarrens, "quest.h6.obj.enter"),
        }, gate: StoryGate.ActOneComplete, turnIn: false, catchUp: StoryGate.ZoneBossDefeated),
        new StoryQuest("h7", 2, true, StoryIds.Merle, new[]
        {
            Kill(StoryIds.DuneAugur, 3, "quest.h7.obj.augurs"),
        }, prereq: "h6", autoAccept: true, turnIn: false, catchUp: StoryGate.ZoneBossDefeated),
        new StoryQuest("h8", 2, true, StoryIds.Merle, new[]
        {
            Kill(StoryIds.Glasswright, 1, "quest.h8.obj.glasswright"),
        }, prereq: "h7", autoAccept: true, turnIn: false, catchUp: StoryGate.ZoneBossDefeated),
        new StoryQuest("h9", 2, true, StoryIds.Ottilie, new[]
        {
            Talk(StoryIds.Merle, "quest.h9.obj.merle"),
            Talk(StoryIds.Ottilie, "quest.h9.obj.ottilie"),
        }, prereq: "h8", autoAccept: true, turnIn: false),
        new StoryQuest("n4", 2, false, StoryIds.Merle, new[]
        {
            Kill(StoryIds.GlassElite, 1, "quest.n4.obj.tamm"),
        }, prereq: "h6", prereqStatus: StoryStatus.Active, reward: StoryReward.Gold, rewardGold: 250),
        new StoryQuest("n5", 2, false, StoryIds.Ilsabe, new[]
        {
            Kill(StoryIds.DuneAugur, 4, "quest.n5.obj.augurs"),
        }, prereq: "h5", gate: StoryGate.ActOneComplete, reward: StoryReward.SeerCirclet),

        // ---- Akt I Vertiefung (cinderreach-akt1-vertiefung S1): H14-H17 + N8-N11 ----
        // am Ende halten die Gespraechsplaene des Bestands stabil
        // (erstes Angebot je NPC bleibt h1/h2/h3/h5/n1/n5). Neue Kennungen meiden
        // h10-h13/n6-n7 (Akt III) und bleiben coverage-faehig ([a-z]+\d+).
        new StoryQuest("h14", 1, true, StoryIds.Ottilie, new[]
        {
            Collect(StoryIds.Fuhrfetzen, 2, "quest.h14.obj.fetzen"),
            Kill(StoryIds.FenBrute, 1, "quest.h14.obj.brute"),
        }, prereq: "h1", prereqStatus: StoryStatus.Active, catchUp: StoryGate.KilnOpen),
        new StoryQuest("h15", 1, true, StoryIds.Ottilie, new[]
        {
            Collect(StoryIds.Tafelbruch, 3, "quest.h15.obj.bruch"),
            Talk(StoryIds.Ottilie, "quest.h15.obj.ottilie"),
        }, prereq: "h2", prereqStatus: StoryStatus.Active, turnIn: false, catchUp: StoryGate.KilnOpen),
        new StoryQuest("h16", 1, true, StoryIds.Jorin, new[]
        {
            Collect(StoryIds.Fuhrliste, 1, "quest.h16.obj.liste"),
            Kill(StoryIds.AshLancer, 2, "quest.h16.obj.lancers"),
        }, prereq: "h3", prereqStatus: StoryStatus.Active, gate: StoryGate.KilnOpen, catchUp: StoryGate.ActOneComplete),
        new StoryQuest("h17", 1, true, StoryIds.Ottilie, new[]
        {
            Talk(StoryIds.Ilsabe, "quest.h17.obj.ilsabe"),
            Talk(StoryIds.Ottilie, "quest.h17.obj.ottilie"),
        }, prereq: "h4", turnIn: false),
        new StoryQuest("n8", 1, false, StoryIds.Ottilie, new[]
        {
            Collect(StoryIds.Siegelband, 1, "quest.n8.obj.band"),
        }, prereq: "h1", prereqStatus: StoryStatus.Active, reward: StoryReward.Gold, rewardGold: 100),
        new StoryQuest("n9", 1, false, StoryIds.Ottilie, new[]
        {
            Collect(StoryIds.Grablicht, 1, "quest.n9.obj.licht"),
        }, prereq: "h2", prereqStatus: StoryStatus.Active, reward: StoryReward.Gold, rewardGold: 60),
        new StoryQuest("n10", 1, false, StoryIds.Jorin, new[]
        {
            Kill(StoryIds.CinderThrall, 4, "quest.n10.obj.thralls"),
        }, gate: StoryGate.KilnOpen, reward: StoryReward.Gold, rewardGold: 80),
        new StoryQuest("n11", 1, false, StoryIds.Ilsabe, new[]
        {
            Collect(StoryIds.Russseite, 1, "quest.n11.obj.seite"),
        }, prereq: "h3", prereqStatus: StoryStatus.Active, gate: StoryGate.KilnOpen, reward: StoryReward.Gold, rewardGold: 120),

        // ---- Akt II Vertiefung (cinderreach-akt2-vertiefung S1): H18-H19 + N12-N15 ----
        // haengt wie H14-H17 neben den Ankern: H6 (Merle-Angebot), H8
        // (Glasmacher-Kill mit Prisma) und H9 (Rueckkehr mit Scherben-Zeile)
        // bleiben unangetastet. Neue Kennungen meiden h10-h13/n6-n7
        // (Akt III) und bleiben coverage-faehig ([a-z]+\d+).
        new StoryQuest("h18", 2, true, StoryIds.Merle, new[]
        {
            Collect(StoryIds.Sehglas, 2, "quest.h18.obj.sehglas"),
            Talk(StoryIds.Merle, "quest.h18.obj.merle"),
        }, prereq: "h7", prereqStatus: StoryStatus.Active, autoAccept: true, turnIn: false, catchUp: StoryGate.ZoneBossDefeated),
        new StoryQuest("h19", 2, true, StoryIds.Merle, new[]
        {
            Collect(StoryIds.Glasscherbe, 3, "quest.h19.obj.scherben"),
        }, prereq: "h8", prereqStatus: StoryStatus.Done, autoAccept: true, turnIn: false, catchUp: StoryGate.ZoneBossDefeated),
        new StoryQuest("n12", 2, false, StoryIds.Merle, new[]
        {
            Collect(StoryIds.Speichenrad, 1, "quest.n12.obj.rad"),
            Talk(StoryIds.Merle, "quest.n12.obj.merle"),
        }, prereq: "n4", prereqStatus: StoryStatus.Done, reward: StoryReward.Gold, rewardGold: 150),
        new StoryQuest("n13", 2, false, StoryIds.Ilsabe, new[]
        {
            Collect(StoryIds.Sehglas, 1, "quest.n13.obj.sehglas"),
        }, prereq: "n5", prereqStatus: StoryStatus.Done, reward: StoryReward.Gold, rewardGold: 200),
        new StoryQuest("n14", 2, false, StoryIds.Merle, new[]
        {
            Collect(StoryIds.Glasscherbe, 1, "quest.n14.obj.scherbe"),
        }, prereq: "h18", prereqStatus: StoryStatus.Active, reward: StoryReward.Gold, rewardGold: 100),
        new StoryQuest("n15", 2, false, StoryIds.Ilsabe, new[]
        {
            Collect(StoryIds.Spendenseite, 1, "quest.n15.obj.seite"),
        }, prereq: "n5", prereqStatus: StoryStatus.Done, reward: StoryReward.Gold, rewardGold: 150),

    };

    static Dictionary<string, StoryQuest> byId;

    public static StoryQuest Get(string id)
    {
        if (byId == null)
        {
            byId = new Dictionary<string, StoryQuest>();
            foreach (var q in All) byId[q.id] = q;
        }
        return id != null && byId.TryGetValue(id, out var quest) ? quest : null;
    }
}
