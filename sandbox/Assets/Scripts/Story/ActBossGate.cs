using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// cinderreach-act-boss-gating: the act bosses (Kilnmother, Glasswright,
/// Thewes) stay dormant (inactive scene object) until every other quest of
/// their act is <see cref="StoryStatus.Done"/>. The required set comes from
/// the catalog: same act, not the boss quest itself and not reachable only
/// through it (prereq chain over the boss quest or a gate on the boss's own
/// act state). When the set completes the boss wakes exactly once per run and
/// plays its spawn cutscene unless an earlier session already played it (the
/// story flag <c>bossspawn.&lt;act&gt;</c> rides the save, no new field).
/// Co-op: the host's quest state decides; clients follow
/// <see cref="HostMask"/> (sent with every story broadcast).
/// </summary>
public static class ActBossGate
{
    public struct Boss
    {
        public int act;
        /// <summary>Quest whose kill objective is this boss.</summary>
        public string quest;
        /// <summary>Scene object name (also the cutscene anchor).</summary>
        public string anchorName;
        public string introId;
        /// <summary>Kill-target form (<see cref="StoryIds.KillTarget"/>).</summary>
        public string killTarget;
        /// <summary>Act state the boss kill sets; quests gated on it come after the boss.</summary>
        public StoryGate after;
    }

    public static readonly Boss[] Bosses =
    {
        new Boss { act = 1, quest = "h4", anchorName = KilnLayout.KilnmotherName, introId = "intro.kilnmother", killTarget = StoryIds.Kilnmother, after = StoryGate.ActOneComplete },
        new Boss { act = 2, quest = "h8", anchorName = GlasswrightController.BossName, introId = "intro.glasswright", killTarget = StoryIds.Glasswright, after = StoryGate.ZoneBossDefeated },
        new Boss { act = 3, quest = "h12", anchorName = StoryIds.Thewes, introId = "intro.thewes", killTarget = StoryIds.Thewes, after = StoryGate.None },
    };

    /// <summary>Released-boss bitmask from the co-op host (bit act-1); -1 = none received yet.</summary>
    public static int HostMask = -1;
    /// <summary>Boss wake-ups this run, per act index 0..2 (test seam).</summary>
    public static readonly int[] Spawns = new int[3];
    /// <summary>
    /// Act-boss kills this run in order, "act:boss:open" where open lists the
    /// act's required quests not Done at the kill ("-" = none). Proof that
    /// the boss fell last (autoplay result <c>actBossKills</c>).
    /// </summary>
    public static readonly List<string> KillLog = new List<string>();

    /// <summary>Books an act-boss kill into <see cref="KillLog"/> (StoryDirector.BookKill).</summary>
    public static void NoteKill(string killTarget, StoryState s)
    {
        foreach (var b in Bosses)
        {
            if (b.killTarget != killTarget) continue;
            var open = new List<string>();
            foreach (var id in RequiredQuests(b.act))
                if (s == null || s.Status(id) != StoryStatus.Done) open.Add(id);
            KillLog.Add(b.act + ":" + b.killTarget + ":" + (open.Count == 0 ? "-" : string.Join(",", open)));
        }
    }

    static readonly Dictionary<int, List<string>> required = new Dictionary<int, List<string>>();
    static readonly Dictionary<string, GameObject> anchors = new Dictionary<string, GameObject>();
    static readonly HashSet<string> pendingIntro = new HashSet<string>();
    /// <summary>Bosses already released this run (a boss found released at scene start counts too).</summary>
    static readonly HashSet<string> woken = new HashSet<string>();

    public static string SpawnFlag(int act) => "bossspawn." + act;

    public static bool IsGated(string introId)
    {
        foreach (var b in Bosses) if (b.introId == introId) return true;
        return false;
    }

    /// <summary>Quests of <paramref name="act"/> that must be Done before its boss appears (catalog order).</summary>
    public static IReadOnlyList<string> RequiredQuests(int act)
    {
        if (required.TryGetValue(act, out var list)) return list;
        list = new List<string>();
        int bi = IndexOfAct(act);
        if (bi >= 0)
        {
            var boss = Bosses[bi];
            foreach (var q in StoryCatalog.All)
            {
                if (q.act != act || q.id == boss.quest) continue;
                if (boss.after != StoryGate.None && q.gate == boss.after) continue;
                if (DependsOn(q, boss.quest)) continue;
                list.Add(q.id);
            }
        }
        required[act] = list;
        return list;
    }

    /// <summary>True when <paramref name="q"/>'s prereq chain runs through <paramref name="questId"/>.</summary>
    public static bool DependsOn(StoryQuest q, string questId)
    {
        for (int guard = 0; q != null && q.prereq != null && guard < 64; guard++)
        {
            if (q.prereq == questId) return true;
            q = StoryCatalog.Get(q.prereq);
        }
        return false;
    }

    public static bool Ready(StoryState s, int act)
    {
        if (s == null) return false;
        foreach (var id in RequiredQuests(act))
            if (s.Status(id) != StoryStatus.Done) return false;
        return true;
    }

    /// <summary>The boss already fell in this save (act state or its quest done): never gated again.</summary>
    public static bool Defeated(Boss b)
    {
        if (RunProfile.Story != null && RunProfile.Story.Status(b.quest) == StoryStatus.Done) return true;
        switch (b.act)
        {
            case 1: return RunProfile.Act != null && RunProfile.Act.ActOneComplete;
            case 2: return RunProfile.Act != null && RunProfile.Act.ZoneBossDefeated;
            default: return RunProfile.Finale != null && RunProfile.Finale.FinalBossDefeated;
        }
    }

    /// <summary>Host/solo view: bosses whose required quests are done (bit act-1).</summary>
    public static int ReleasedMask(StoryState s)
    {
        int mask = 0;
        foreach (var b in Bosses)
            if (Ready(s, b.act)) mask |= 1 << (b.act - 1);
        return mask;
    }

    static bool Released(Boss b)
    {
        if (Diablo.IsLocalAuthority()) return Ready(RunProfile.Story, b.act);
        return HostMask >= 0 && (HostMask & (1 << (b.act - 1))) != 0;
    }

    /// <summary>Is the boss of <paramref name="act"/> currently held back?</summary>
    public static bool IsDormant(int act)
    {
        int i = IndexOfAct(act);
        if (i < 0) return false;
        return anchors.TryGetValue(Bosses[i].anchorName, out var go) && go != null && !go.activeSelf;
    }

    /// <summary>
    /// Per-poll hook (StoryDirector, also on scene start). Hides unreleased
    /// bosses, wakes released ones once and starts their spawn cutscene
    /// (retried while another sequence such as the turn-in talk still runs).
    /// </summary>
    public static void Tick(float now)
    {
        foreach (var b in Bosses)
        {
            var go = Anchor(b.anchorName);
            if (go == null) continue;
            if (Defeated(b))
            {
                if (!go.activeSelf) go.SetActive(true);
                pendingIntro.Remove(b.introId);
                continue;
            }
            if (!Released(b))
            {
                if (go.activeSelf) go.SetActive(false);
                woken.Remove(b.anchorName);
                continue;
            }
            if (!go.activeSelf) go.SetActive(true);
            if (woken.Add(b.anchorName))
            {
                Spawns[b.act - 1]++;
                Debug.Log($"[ActBoss] {b.anchorName} erscheint (Akt {b.act}).");
                if (RunProfile.Story == null || !RunProfile.Story.Flags.Contains(SpawnFlag(b.act)))
                    pendingIntro.Add(b.introId);
            }
            if (pendingIntro.Contains(b.introId) && CutsceneDirector.TryStartSpawnIntro(b.introId, now))
            {
                pendingIntro.Remove(b.introId);
                RunProfile.Story?.Flags.Add(SpawnFlag(b.act));
            }
        }
    }

    /// <summary>Scene object by name, cached so it stays reachable while inactive.</summary>
    public static GameObject Anchor(string name)
    {
        if (anchors.TryGetValue(name, out var go) && go != null) return go;
        go = GameObject.Find(name);
        if (go != null) anchors[name] = go;
        else anchors.Remove(name);
        return go;
    }

    static int IndexOfAct(int act)
    {
        for (int i = 0; i < Bosses.Length; i++) if (Bosses[i].act == act) return i;
        return -1;
    }

    /// <summary>Reset run state (scene load / headless tests). Wakes cached bosses so nothing stays hidden.</summary>
    public static void ResetRun()
    {
        foreach (var kv in anchors)
            if (kv.Value != null && !kv.Value.activeSelf) kv.Value.SetActive(true);
        anchors.Clear();
        pendingIntro.Clear();
        woken.Clear();
        HostMask = -1;
        KillLog.Clear();
        for (int i = 0; i < Spawns.Length; i++) Spawns[i] = 0;
    }
}
