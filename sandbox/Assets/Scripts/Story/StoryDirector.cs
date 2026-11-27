using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Runtime story wiring (cinderreach-deutsch-story S4): polls the act state,
/// books zone entries, grants quest rewards once, shows toasts and the HUD
/// tracker, autosaves on change and lets the co-op host broadcast its story
/// snapshot. Pure quest logic lives in <see cref="StoryState"/>.
/// </summary>
public sealed class StoryDirector : MonoBehaviour
{
    public static StoryState State => RunProfile.Story;

    float poll;
    string lastZone;
    int lastRevision = -1;
    readonly Queue<string> toasts = new Queue<string>();
    float toastLeft;
    TextMesh trackerLabel;
    Canvas canvas;
    UnityEngine.UI.Text toastText;
    UnityEngine.UI.Text trackerText;
    UnityEngine.UI.Text barkText;
    float barkLeft;

    /// <summary>Places the tracked quests point at, refreshed with the 0.25 s poll (maps and tracker read them).</summary>
    public readonly List<QuestMark> Marks = new List<QuestMark>();
    /// <summary>Scene lookups behind <see cref="Marks"/>.</summary>
    public readonly SceneQuestWorld World = new SceneQuestWorld();

    void Awake()
    {
        if (GetComponent<StoryDialogUI>() == null) gameObject.AddComponent<StoryDialogUI>();
        if (GetComponent<StoryQuestUI>() == null) gameObject.AddComponent<StoryQuestUI>();
        if (GetComponent<UseKey>() == null) gameObject.AddComponent<UseKey>();
        // cinderreach-group-quest-map: minimap (top right) and zone map (M).
        if (GetComponent<MinimapHud>() == null) gameObject.AddComponent<MinimapHud>();
        if (GetComponent<WorldMapUI>() == null) gameObject.AddComponent<WorldMapUI>();
    }

    void Start()
    {
        BuildHud();
        lastZone = WorldBounds.ZoneAt(transform.position).name;
        State.Refresh(RunProfile.Act);
        State.TakeChanges();
        SyncDoneFlags();
        lastRevision = State.Revision;
    }

    void BuildHud()
    {
        var canvasGo = new GameObject("StoryCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 25, raycaster: false);
        trackerText = DiabloUIFactory.MakeText(canvasGo.transform, "StoryTracker", "", 16,
            DiabloPalette.GoldBright, TextAnchor.UpperLeft);
        var rt = trackerText.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(18f, -14f); rt.sizeDelta = new Vector2(560f, 320f);
        trackerText.supportRichText = true;
        toastText = DiabloUIFactory.MakeText(canvasGo.transform, "StoryToast", "", 19,
            DiabloPalette.Parchment, TextAnchor.MiddleCenter);
        var tr = toastText.rectTransform;
        tr.anchorMin = new Vector2(0.5f, 1f); tr.anchorMax = new Vector2(0.5f, 1f); tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0f, -64f); tr.sizeDelta = new Vector2(760f, 36f);
        toastText.enabled = false;
        barkText = DiabloUIFactory.MakeText(canvasGo.transform, "StoryBark", "", 18,
            DiabloPalette.Parchment, TextAnchor.MiddleCenter);
        barkText.horizontalOverflow = HorizontalWrapMode.Wrap;
        var br = barkText.rectTransform;
        br.anchorMin = new Vector2(0.5f, 0f); br.anchorMax = new Vector2(0.5f, 0f); br.pivot = new Vector2(0.5f, 0f);
        br.anchoredPosition = new Vector2(0f, 190f); br.sizeDelta = new Vector2(860f, 60f);
        barkText.enabled = false;
    }

    /// <summary>Shows queued barks as a subtitle with their voice clip, one at a time.</summary>
    void TickBarks()
    {
        if (barkText == null) return;
        if (barkLeft > 0f)
        {
            barkLeft -= Time.unscaledDeltaTime;
            if (barkLeft > 0f) return;
            barkText.enabled = false;
        }
        if (StoryDialogUI.AnyOpen || StoryDialogUI.AnyShared || !StoryBarks.TryTake(out var bark)) return;
        string who = bark.speaker == "n" ? "" : Loc.T("npc." + bark.speaker + ".name");
        string line = Loc.T(bark.Key);
        barkText.text = who.Length > 0 ? "<color=#d8b25a>" + who + ":</color> " + line : "<i>" + line + "</i>";
        barkText.supportRichText = true;
        barkText.enabled = true;
        float len = StoryVoice.Play(bark.Key);
        barkLeft = Mathf.Max(3.5f, len + 1f);
    }

    void Update()
    {
        if (DiabloUIFactory.IsRemoteMirror(this)) return;
        TickBarks();
        CutsceneDirector.Tick(Time.unscaledTime);
        if (toastLeft > 0f)
        {
            toastLeft -= Time.unscaledDeltaTime;
            if (toastLeft <= 0f) { toastText.enabled = false; ShowNextToast(); }
        }
        poll -= Time.unscaledDeltaTime;
        if (poll > 0f) return;
        poll = 0.25f;

        State.Refresh(RunProfile.Act);
        // Akt III Finale S3: the Barrens boss opens the Moortor (Abbot→Kiln
        // pattern, idempotent; persists via the generic story lists, v16 stays).
        RunProfile.Finale.OpenMoortor(RunProfile.Act);
        // Akt III Finale S3c: H11-Kante — Thewes tritt erst mit H11 am
        // Moortor auf (Respawn, genau 1x pro Lauf).
        MaybeSpawnThewesAtH11();
        var zone = WorldBounds.ZoneAt(transform.position).name;
        if (lastZone != null && zone != lastZone) BookEnter(zone);
        lastZone = zone;
        SyncDoneFlags();
        GrantRewards();
        DrainToasts();
        QuestDestinations.Collect(State, World, transform.position, Marks);
        RefreshTracker();

        if (lastRevision >= 0 && State.Revision != lastRevision)
        {
            var relay = GetComponent<DiabloNetworkPlayer>();
            bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            if (!networked || (relay != null && relay.IsServer))
                SaveService.SaveNow(gameObject);
            if (relay != null && relay.IsSpawned && relay.IsServer)
                relay.BroadcastStory(State.Snapshot());
        }
        lastRevision = State.Revision;
    }

    void SyncDoneFlags()
    {
        foreach (var q in StoryCatalog.All)
            if (State.Status(q.id) == StoryStatus.Done) State.Flags.Add(q.id);
    }

    void GrantRewards()
    {
        foreach (var q in StoryCatalog.All)
        {
            if (State.Status(q.id) != StoryStatus.Done || q.reward == StoryReward.None) continue;
            string flag = "reward:" + q.id;
            if (State.Flags.Contains(flag)) continue;
            if (q.rewardGold > 0) Wallet.EnsureWallet(gameObject)?.Add(q.rewardGold);
            bool granted = true;
            if (q.reward == StoryReward.SmithKit)
            {
                RunProfile.SmithingMaterial += 3;
                Toast(Loc.F("story.reward.item", Loc.T("story.reward.material").Replace("{0}", "3")));
            }
            else if (q.reward == StoryReward.RareHelm || q.reward == StoryReward.SeerCirclet)
            {
                var item = q.reward == StoryReward.SeerCirclet
                    ? ItemDatabase.CreateUnique("Seer's Circlet")
                    : RollRareHelm();
                var inv = GetComponent<Inventory>();
                granted = inv != null && inv.Add(item);
                if (granted) Toast(Loc.F("story.reward.item", LootDrop.PrettyName(
                    item.rarity == ItemRarity.Unique ? item.uniqueId : item.baseType)));
            }
            if (granted) State.Flags.Add(flag);
        }
    }

    static ItemInstance RollRareHelm()
    {
        var rng = new System.Random(RunProfile.RunSeed ^ 0x51ab);
        var item = new ItemInstance
        {
            slot = ItemSlot.Helmet,
            rarity = ItemRarity.Rare,
            baseType = "Helm",
            itemLevel = 5,
            affixTier = 1,
        };
        item.affixes = AffixLibrary.RollAffixes(item.slot, item.rarity, rng, item.affixTier, item.itemLevel);
        item.EnsureIdentity();
        return item;
    }

    void DrainToasts()
    {
        foreach (var c in State.TakeChanges())
        {
            var q = StoryCatalog.Get(c.questId);
            if (q == null) continue;
            if (c.objective < 0 && c.status == StoryStatus.Active && q.id == "h4") StoryBarks.Say("h4.start");
            if (c.objective < 0 && c.status == StoryStatus.Done && q.id == "h7") StoryBarks.Say("h7.done");
            // Akt II Vertiefung S2: R4/R5-Funde sprechen beim Aufwachen der Aufgabe.
            if (c.objective < 0 && c.status == StoryStatus.Active && q.id == "h18") StoryBarks.Say("sehglas.find");
            if (c.objective < 0 && c.status == StoryStatus.Active && q.id == "h19") StoryBarks.Say("scherben.find");
            if (c.objective < 0 && c.status == StoryStatus.Active) Toast(Loc.F("story.quest.new", Loc.T(q.TitleKey)));
            else if (c.objective < 0 && c.status == StoryStatus.Ready) Toast(Loc.F("story.quest.ready", Loc.T(q.TitleKey)));
            else if (c.objective < 0 && c.status == StoryStatus.Done) Toast(Loc.F("story.quest.done", Loc.T(q.TitleKey)));
            else if (c.objective >= 0 && c.objective < q.objectives.Length) Toast(ProgressToast(q, c));
        }
        ShowNextToast();
    }

    void ShowNextToast()
    {
        if (toastText == null || toastText.enabled || toasts.Count == 0) return;
        toastText.text = toasts.Dequeue();
        toastText.enabled = true;
        toastLeft = 3.5f;
    }

    /// <summary>Queue a line on the story toast (pickups, rewards).</summary>
    public void Toast(string text)
    {
        if (!string.IsNullOrEmpty(text)) toasts.Enqueue(text);
        ShowNextToast();
    }

    /// <summary>Objective progress line: "Knochenwächter besiegen  2/4", or "… ✓" once done.</summary>
    public static string ProgressToast(StoryQuest q, StoryChange c)
    {
        var o = q.objectives[c.objective];
        string text = Loc.T(o.textKey);
        if (c.count >= o.count) return o.count > 1 ? text + "  " + o.count + "/" + o.count + " ✓" : text + " ✓";
        return text + "  " + c.count + "/" + o.count;
    }

    void RefreshTracker()
    {
        if (trackerText == null) return;
        trackerText.text = TrackerText(State, World, Marks, transform.position);
    }

    /// <summary>
    /// HUD tracker (cinderreach-group-quest-map): every tracked quest with its
    /// open objectives and counts, finished ones ticked, hand-ins naming the
    /// giver, and the way to the next place: metres in this zone, else the zone
    /// it lies in. Rich text; test seam.
    /// </summary>
    public static string TrackerText(StoryState s, IQuestWorld world, List<QuestMark> marks, Vector3 hero)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var q in StoryTracking.Tracked(s))
        {
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(q.main ? "<color=#f9cc47><b>" : "<color=#dbd5c4><b>").Append(Loc.T(q.TitleKey)).Append("</b></color>");
            if (marks != null && QuestDestinations.NearestStep(world, marks, q.id, hero, out var step, out int zone))
            {
                int here = ZoneMap.IndexAt(hero);
                string way = zone == here
                    ? Loc.F("story.tracker.meters", Mathf.RoundToInt(Vector3.Distance(Flat(step), Flat(hero))))
                    : Loc.F("story.tracker.zone", ActProgress.ZoneName(zone));
                sb.Append("  <color=#a69985>").Append(way).Append("</color>");
            }
            if (s.Status(q.id) == StoryStatus.Ready)
            {
                sb.Append("\n   <color=#e8d9be>• ").Append(Loc.F("story.tracker.handin", Loc.T("npc." + q.giver + ".name"))).Append("</color>");
                continue;
            }
            for (int i = 0; i < q.objectives.Length; i++)
            {
                bool done = s.ObjectiveDone(q, i);
                sb.Append(done ? "\n   <color=#8a7f6e>✓ " : "\n   <color=#e8d9be>• ")
                  .Append(StoryTracking.ObjectiveRow(s, q, i)).Append("</color>");
            }
        }
        return sb.ToString();
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    // ---- booking (static so kills, pickups, tests and the network can call) ----

    public static void BookKill(string objectName)
    {
        string target = StoryIds.KillTarget(objectName ?? "");
        StoryBarks.OnKill(target);
        State.Apply(StoryObjectiveType.Kill, target);
        // Akt III Finale S3: Thewes down ends H12 and arms the H13 decision
        // (no new boss system: EnemyHealth books every kill through here).
        if (target == StoryIds.Thewes) RunProfile.Finale.DefeatFinalBoss();
        State.Refresh(RunProfile.Act);
    }

    public static bool BookCollect(string itemId)
    {
        bool counted = State.Apply(StoryObjectiveType.Collect, itemId);
        if (counted) State.Refresh(RunProfile.Act);
        return counted;
    }

    public static void BookEnter(string zoneName)
    {
        State.Apply(StoryObjectiveType.Enter, zoneName);
        State.Refresh(RunProfile.Act);
    }

    /// <summary>Books the pending talk with <paramref name="npc"/> after its dialog ran. Co-op clients forward it to the host.</summary>
    public static StoryTalkPlan BookTalk(string npc, GameObject hero)
    {
        var plan = StoryDialogs.Plan(State, npc);
        if (!plan.IsPending) return plan;
        var relay = hero != null ? hero.GetComponent<DiabloNetworkPlayer>() : null;
        bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (networked && relay != null && relay.IsSpawned && !relay.IsServer)
        {
            relay.SubmitStoryTalkServerRpc(npc);
            return plan;
        }
        BookTalkLocal(npc);
        return plan;
    }

    /// <summary>Host/solo talk booking (also the ServerRpc target on the host).</summary>
    public static void BookTalkLocal(string npc)
    {
        var plan = StoryDialogs.Plan(State, npc);
        if (!plan.IsPending) return;
        switch (plan.kind)
        {
            case StoryTalkKind.Offer: State.Accept(plan.questId); break;
            case StoryTalkKind.TurnIn: if (State.TurnIn(plan.questId)) DiabloAudio.PlayQuest(); break;
            case StoryTalkKind.Talk: State.Apply(StoryObjectiveType.Talk, npc); break;
        }
        State.Refresh(RunProfile.Act);
    }

    // ---- Akt III Finale S3 (Moortor: H11-Raetsel, H12-Boss, H13-Entscheidung) ----

    /// <summary>Final-boss phase-2 entries this run (test seam, boss precedent).</summary>
    public static int FinalBossPhaseTwoEntries;
    static bool finalBossPhaseTwo;

    /// <summary>
    /// Akt III Finale S3: Thewes phase 2 at/below 50 % HP (boss pattern, once
    /// per run: roar + boss music). The phase-2 showcase reuses the existing
    /// <see cref="CutsceneDirector"/> path as soon as a Thewes intro exists;
    /// until then that step is a no-op, never a new system.
    /// </summary>
    public static bool BookFinalBossPhase(float hpFraction)
    {
        if (finalBossPhaseTwo || hpFraction > 0.5f) return false;
        finalBossPhaseTwo = true;
        FinalBossPhaseTwoEntries++;
        Debug.Log("[Finale] Thewes enters phase 2.");
        try { DiabloAudio.PlayBossRoar(); } catch { }
        try { MusicDirector.Ensure().PlayBoss(); } catch { }
        foreach (var intro in CutsceneDirector.BossIntros)
            if (intro.anchorName == StoryIds.Thewes)
            {
                CutsceneDirector.TryStartPhaseTwoShowcase(intro.id, Time.unscaledTime);
                break;
            }
        return true;
    }

    /// <summary>Run state: H11 edge fired (test seam, exactly once per run).</summary>
    public static int ThewesEntranceEntries;
    static bool thewesEntranceDone;

    /// <summary>
    /// Akt III Finale S3c (H11-Auftritt): Thewes tritt erst mit H11 am
    /// Moortor auf. Feuert genau einmal pro Lauf, sobald h11 Active ist:
    /// lebt ein Thewes, passiert nichts (kein Doppel-Spawn); fehlt er oder
    /// liegt er tot da (Akt-II-Sweep vor H9), spawnt er neu mit vollem HP —
    /// Laufzeit-Pfad wie der Kilnmother-Summon (HuskModelLibrary, Boss-Tier,
    /// EnemyAttack + Husk-Rig, generisches BookKill). Position: am Moortor
    /// (MoortorGate zur Laufzeit, sonst BarrensLayout-Fallback).
    /// </summary>
    public static void MaybeSpawnThewesAtH11()
    {
        if (thewesEntranceDone) return;
        if (State.Status("h11") != StoryStatus.Active) return;
        thewesEntranceDone = true;
        ThewesEntranceEntries++;
        var existing = GameObject.Find(StoryIds.Thewes);
        var health = existing != null ? existing.GetComponent<EnemyHealth>() : null;
        if (health != null && health.IsAlive) return;
        if (existing != null) Object.Destroy(existing);
        Vector3 pos = BarrensLayout.BarrensOffset + new Vector3(0f, 0.05f, -19f);
        var gate = GameObject.Find(ZoneGate.MoortorGateName);
        if (gate != null) pos = gate.transform.position + new Vector3(-4f, -0.95f, -3.5f);
        var go = HuskModelLibrary.SpawnHusk(StoryIds.Thewes, pos, 2.2f,
            GlasswrightController.MaxHP, LootTier.Boss, 3, model: null, netServer: false);
        var attack = go.GetComponent<EnemyAttack>();
        if (attack != null) { attack.damage = 3; attack.encounterGroup = EncounterLayout.FrontGroup; }
        Debug.Log("[Finale] Thewes tritt am Moortor auf.");
    }

    /// <summary>
    /// Akt III Finale S3 (H13): books the one closing line. Exactly once per
    /// run (<see cref="ActProgressAkt3.Decide"/>); the outcome flag rides the
    /// generic story flags, so the save (v16, no migration) and the log keep
    /// it without a new field or new network code.
    /// </summary>
    public static bool BookDecision(bool shatter)
    {
        if (!RunProfile.Finale.Decide(shatter)) return false;
        State.Flags.Add(shatter ? "h13.shatter" : "h13.hold");
        return true;
    }

    /// <summary>Reset finale run state (scene load / headless tests).</summary>
    public static void ResetFinaleForTests()
    {
        FinalBossPhaseTwoEntries = 0;
        finalBossPhaseTwo = false;
        ThewesEntranceEntries = 0;
        thewesEntranceDone = false;
    }

    /// <summary>Opens the dialog for <paramref name="npc"/> when one is scripted; shops only for pending talks.</summary>
    public static bool TryOpenTalk(GameObject hero, string npc)
    {
        if (hero == null) return false;
        var plan = StoryDialogs.Plan(State, npc);
        if (plan.dialog == null) return false;
        if (StoryDialogs.IsShop(npc) && !plan.IsPending) return false;
        var ui = hero.GetComponent<StoryDialogUI>();
        if (ui == null) ui = hero.AddComponent<StoryDialogUI>();
        if (ui.IsOpen) return true;
        ui.Show(plan, npc);
        if (plan.questId != null && StoryCatalog.Get(plan.questId) is StoryQuest quest && quest.main)
            CutsceneDirector.PlayDialog(ui, npc);
        return true;
    }
}
