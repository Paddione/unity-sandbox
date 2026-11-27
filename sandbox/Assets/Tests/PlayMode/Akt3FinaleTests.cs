using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// cinderreach-akt3-finale S3 (Moortor): H11-Raetsel (R7-Glutspuren),
/// H12-Boss (Phase 2 ab 50 %, Balken, vorhandenes Cutscene-Muster),
/// H13-Entscheidung (Decide genau 1x, je 1 Schlusszeile zerschlagen/halten),
/// Moortor-Gate, Save-Roundtrip (v16, generische Listen) und Koop-Snapshot.
///
/// quest:h10 quest:h11 quest:h12 quest:h13 quest:n6 quest:n7
///
/// Kein neuer Ort, kein neues Boss-System, keine Voice (P6 S2 glaettet nach
/// dem S4-Drop): Reisen laufen auf Buchungsebene (Accept/Talk/Collect/Kill),
/// H13-Talk ohne Dialogfenster (dlg.h13.* pflegt P6).
/// </summary>
public class Akt3FinaleTests
{
    GameObject hero;
    string savePath;

    sealed class FakeWorld : IQuestWorld
    {
        public readonly Dictionary<string, Vector3> npcs = new Dictionary<string, Vector3>();
        public readonly Dictionary<string, List<Vector3>> pickups = new Dictionary<string, List<Vector3>>();
        public readonly Dictionary<string, List<Vector3>> enemies = new Dictionary<string, List<Vector3>>();
        public bool TryNpc(string npcId, out Vector3 position) => npcs.TryGetValue(npcId, out position);
        public void Pickups(string itemId, List<Vector3> into) { if (pickups.TryGetValue(itemId, out var l)) into.AddRange(l); }
        public void Enemies(string killTarget, List<Vector3> into) { if (enemies.TryGetValue(killTarget, out var l)) into.AddRange(l); }
        public bool TryGate(int from, int to, out Vector3 position) { position = default; return false; }
    }

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        StoryDirector.ResetFinaleForTests();
        StoryBarks.ResetForTests();
        CutsceneDirector.ResetRun();
        Wallet.ResetRun();
        DiabloProgression.ResetRun();
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        ZoneGate.ResetRun();
        DiabloClickController.ExternalDrive = false;
        savePath = Path.Combine(Path.GetTempPath(), "cinderreach-finale-test.json");
        if (File.Exists(savePath)) File.Delete(savePath);

        hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = DiabloNetworkPlayer.HeroName;
        hero.transform.position = new Vector3(600f, 1f, 40f);
        hero.AddComponent<CharacterController>();
        hero.AddComponent<DiabloProgression>();
        hero.AddComponent<Equipment>();
        hero.AddComponent<Inventory>();
        hero.AddComponent<Wallet>();
        hero.AddComponent<PlayerHealth>();

        var eventSys = new GameObject("EventSystem");
        eventSys.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSys.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        var listener = new GameObject("TestAudioListener");
        listener.AddComponent<AudioListener>();
    }

    [TearDown]
    public void TearDown()
    {
        DiabloTestFixture.CleanupTestScene(hero);
        var listener = GameObject.Find("TestAudioListener");
        if (listener != null) Object.DestroyImmediate(listener);
        var eventSys = GameObject.Find("EventSystem");
        if (eventSys != null) Object.DestroyImmediate(eventSys);
        RunProfile.Reset();
        StoryDirector.ResetFinaleForTests();
        StoryBarks.ResetForTests();
        CutsceneDirector.ResetRun();
        Wallet.ResetRun();
        DiabloProgression.ResetRun();
        ZoneGate.ResetRun();
        DiabloClickController.ExternalDrive = false;
        if (savePath != null && File.Exists(savePath)) File.Delete(savePath);
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    static StoryState S() => RunProfile.Story;

    static readonly Vector3 B = BarrensLayout.BarrensOffset;
    // Dieselben Stellen wie TownBuilder.SpawnStoryPickups (S3b-Anteil) und
    // BarrensBuilder.SpawnMoortor: die Reise belegt die Szene ueber Trigger.
    static Vector3 GlutPos(int i) => B + new Vector3(
        i == 0 ? -3f : (i == 1 ? 1f : 5f), 0.7f,
        i == 0 ? -16f : (i == 1 ? -17f : -15.5f));
    static Vector3 LanzenPos(int i) => B + new Vector3(
        i == 0 ? 8f : (i == 1 ? 8.5f : (i == 2 ? 4f : 8.5f)), 0.7f,
        i == 0 ? 2f : (i == 1 ? -2.5f : (i == 2 ? -13f : -13.5f)));
    static readonly Vector3 Seite1Pos = B + new Vector3(12f, 0.7f, 2f);
    static readonly Vector3 MoortorGatePos = B + new Vector3(4f, 1f, -15.5f);
    static readonly Vector3 MoortorArrival = B + new Vector3(0f, 1f, -18.5f);
    static readonly Vector3 MoortorReturnPos = B + new Vector3(-4f, 1f, -18f);
    static readonly Vector3 MoortorReturnArrival = B + new Vector3(4f, 1f, -13.5f);
    static readonly Vector3 ThewesPos = B + new Vector3(0f, 0.05f, -19f);

    static GameObject MakePickup(string itemId, int index, Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"StoryPickup_{itemId}_{index}";
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.45f, 0.12f, 0.6f);
        var col = go.GetComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(2.4f, 2f, 2.4f);
        go.AddComponent<StoryPickup>().itemId = itemId;
        return go;
    }

    static GameObject MakeGate(string name, Vector3 pos, Vector3 dest, int zone)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.GetComponent<BoxCollider>().isTrigger = true;
        var gate = go.AddComponent<ZoneGate>();
        gate.destination = dest;
        gate.targetZone = zone;
        return go;
    }

    static GameObject MakeThewes(Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = StoryIds.Thewes;
        go.tag = "Husk";
        go.transform.position = pos;
        var health = go.AddComponent<EnemyHealth>();
        health.SetTier(GlasswrightController.MaxHP, LootTier.Boss);
        var attack = go.AddComponent<EnemyAttack>();
        attack.damage = 3;
        attack.encounterGroup = EncounterLayout.FrontGroup;
        return go;
    }

    /// <summary>Act II fertig + Moortor offen: H9 wartet aktiv (TurnIn-frei).</summary>
    static void DriveAct()
    {
        Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
        Assert.IsTrue(RunProfile.Act.CompleteAct());
        Assert.IsTrue(RunProfile.Act.DefeatZoneBoss());
        Assert.IsTrue(RunProfile.Finale.OpenMoortor(RunProfile.Act));
        S().Refresh(RunProfile.Act);
        Assert.AreEqual(StoryStatus.Active, S().Status("h9"));
    }

    /// <summary>H9-Gespraeche (Merle + Ottilie) auf dem echten Gespraechspfad.</summary>
    void FinishH9()
    {
        StoryDirector.BookTalk(StoryIds.Merle, hero);
        StoryDirector.BookTalk(StoryIds.Ottilie, hero);
        Assert.AreEqual(StoryStatus.Done, S().Status("h9"));
        S().Refresh(RunProfile.Act);
    }

    /// <summary>H10: Angebot annehmen, vier Wacht-Gespraeche, Abgabe bei Ottilie.</summary>
    void FinishH10()
    {
        // h5 bietet frueher an (Katalogreihenfolge): h10 direkt annehmen.
        Assert.AreEqual(StoryStatus.Available, S().Status("h10"));
        Assert.IsTrue(S().Accept("h10"));
        StoryDirector.BookTalk(StoryIds.Ottilie, hero);
        StoryDirector.BookTalk(StoryIds.Merle, hero);
        StoryDirector.BookTalk(StoryIds.Ilsabe, hero);
        StoryDirector.BookTalk(StoryIds.Jorin, hero);
        Assert.AreEqual(StoryStatus.Ready, S().Status("h10"));
        StoryDirector.BookTalk(StoryIds.Ottilie, hero);
        Assert.AreEqual(StoryStatus.Done, S().Status("h10"));
        S().Refresh(RunProfile.Act);
    }

    void DriveToH11()
    {
        DriveAct();
        FinishH9();
        FinishH10();
        Assert.AreEqual(StoryStatus.Active, S().Status("h11"));
        Assert.AreEqual(StoryStatus.Available, S().Status("n6"));
        Assert.AreEqual(StoryStatus.Available, S().Status("n7"));
    }

    void DriveToH13()
    {
        DriveToH11();
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Glutspur));
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Glutspur));
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Glutspur));
        Assert.AreEqual(StoryStatus.Done, S().Status("h11"));
        Assert.AreEqual(StoryStatus.Active, S().Status("h12"));
        StoryDirector.BookKill(StoryIds.Thewes);
        Assert.AreEqual(StoryStatus.Done, S().Status("h12"));
        Assert.AreEqual(StoryStatus.Active, S().Status("h13"));
        StoryDirector.BookTalk(StoryIds.Ottilie, hero);
        Assert.AreEqual(StoryStatus.Done, S().Status("h13"));
    }

    [UnityTest]
    public IEnumerator H10_Kette_AngebotBisAbgabe()
    {
        DriveAct();
        FinishH9();
        var h10 = StoryCatalog.Get("h10");
        Assert.IsNotNull(h10);
        Assert.AreEqual("h9", h10.prereq);
        Assert.AreEqual(StoryIds.Ottilie, h10.giver);
        Assert.AreEqual(3, h10.act);
        FinishH10();
        Assert.AreEqual(StoryStatus.Active, S().Status("h11"), "h11 startet von allein");
        Assert.AreEqual(StoryStatus.Available, S().Status("n6"), "n6 wartet auf Jorin");
        Assert.AreEqual(StoryStatus.Available, S().Status("n7"), "n7 wartet auf Ilsabe");
        yield break;
    }

    [UnityTest]
    public IEnumerator N6N7_Ketten_SammelnUndAbgeben()
    {
        DriveToH11();
        Assert.IsTrue(S().Accept("n6"));
        for (int i = 0; i < 4; i++) Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Lanzenspitze));
        Assert.AreEqual(StoryStatus.Ready, S().Status("n6"));
        StoryDirector.BookTalk(StoryIds.Jorin, hero);
        Assert.AreEqual(StoryStatus.Done, S().Status("n6"));
        Assert.IsTrue(S().Accept("n7"));
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Spendenseite));
        Assert.AreEqual(StoryStatus.Ready, S().Status("n7"));
        StoryDirector.BookTalk(StoryIds.Ilsabe, hero);
        Assert.AreEqual(StoryStatus.Done, S().Status("n7"));
        Assert.AreEqual(StoryStatus.Active, S().Status("h11"), "n6/n7 aendern h11 nicht");
        yield break;
    }

    [UnityTest]
    public IEnumerator H11_Raetsel_DreiGlutspuren()
    {
        DriveToH11();
        var h11 = StoryCatalog.Get("h11");
        Assert.AreEqual("h10", h11.prereq);
        Assert.AreEqual(StoryIds.Merle, h11.giver);
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Glutspur));
        Assert.AreEqual(StoryStatus.Active, S().Status("h11"), "1/3 reicht nicht");
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Glutspur));
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Glutspur));
        Assert.AreEqual(StoryStatus.Done, S().Status("h11"), "R7: drei Spuren nennen Thewes");
        Assert.AreEqual(StoryStatus.Active, S().Status("h12"));
        yield break;
    }

    [UnityTest]
    public IEnumerator H12_Boss_Phase2UndKill()
    {
        DriveToH11();
        StoryDirector.BookCollect(StoryIds.Glutspur);
        StoryDirector.BookCollect(StoryIds.Glutspur);
        StoryDirector.BookCollect(StoryIds.Glutspur);
        Assert.AreEqual(StoryStatus.Active, S().Status("h12"));
        // Phase 2: erst ab 50 %, genau einmal (Boss-Muster).
        Assert.IsFalse(StoryDirector.BookFinalBossPhase(0.6f));
        Assert.AreEqual(0, StoryDirector.FinalBossPhaseTwoEntries);
        Assert.IsTrue(StoryDirector.BookFinalBossPhase(0.5f));
        Assert.AreEqual(1, StoryDirector.FinalBossPhaseTwoEntries);
        Assert.IsFalse(StoryDirector.BookFinalBossPhase(0.2f), "Phase 2 bleibt einmalig");
        // Balken: jeder Boss-Rang zeigt breit; Thewes faellt generisch durch.
        Assert.AreEqual("Thewes", DiabloTargetBar.DisplayName("Thewes"));
        Assert.IsTrue(DiabloTargetBar.IsBossTier(LootTier.Boss));
        Assert.AreEqual("", DiabloTargetBar.PhaseLabel(null), "ohne Boss kein Phasenlabel");
        StoryDirector.BookKill("Thewes_0");
        Assert.AreEqual(StoryStatus.Done, S().Status("h12"), "Thewes_0 zaehlt wie jeder Boss");
        Assert.IsTrue(RunProfile.Finale.FinalBossDefeated);
        Assert.AreEqual(StoryStatus.Active, S().Status("h13"));
        yield break;
    }

    [UnityTest]
    public IEnumerator H13_Entscheidung_Zerschlagen()
    {
        DriveToH13();
        Assert.AreEqual(ActProgressAkt3.NoDecision, RunProfile.Finale.Decision);
        Assert.IsTrue(StoryDirector.BookDecision(true));
        Assert.AreEqual(ActProgressAkt3.Shatter, RunProfile.Finale.Decision);
        Assert.IsTrue(S().Flags.Contains("h13.shatter"), "Ausgang reitet das Flag");
        Assert.IsFalse(S().Flags.Contains("h13.hold"));
        Assert.IsFalse(StoryDirector.BookDecision(false), "genau eine Schlusszeile");
        Assert.AreEqual(ActProgressAkt3.Shatter, RunProfile.Finale.Decision);
        yield break;
    }

    [UnityTest]
    public IEnumerator H13_Entscheidung_Halten()
    {
        DriveToH13();
        Assert.IsTrue(StoryDirector.BookDecision(false));
        Assert.AreEqual(ActProgressAkt3.Hold, RunProfile.Finale.Decision);
        Assert.IsTrue(S().Flags.Contains("h13.hold"));
        Assert.IsFalse(StoryDirector.BookDecision(true), "genau eine Schlusszeile");
        Assert.AreEqual(ActProgressAkt3.Hold, RunProfile.Finale.Decision);
        yield break;
    }

    [UnityTest]
    public IEnumerator Moortor_Gate_OeffnetMitBarrensboss()
    {
        var gateGo = new GameObject(ZoneGate.MoortorGateName);
        var gate = gateGo.AddComponent<ZoneGate>();
        gate.targetZone = ActProgress.Barrens;
        gate.destination = new Vector3(10f, 0f, 10f);
        Assert.IsTrue(gate.IsMoortorGate);
        Assert.IsTrue(gate.Locked, "ohne Barrens-Boss kein Moortor");
        gate.Teleport(hero);
        Assert.AreEqual(1, ZoneGate.Refusals);
        DriveAct();
        Assert.IsFalse(gate.Locked);
        gate.Teleport(hero);
        Assert.AreEqual(1, ZoneGate.Teleports);
        Assert.AreEqual(new Vector3(10f, 0f, 10f), hero.transform.position);
        yield break;
    }

    [UnityTest]
    public IEnumerator Save_Roundtrip_FinaleBleibtBestehen()
    {
        DriveToH13();
        Assert.IsTrue(StoryDirector.BookDecision(true));
        SaveService.SaveNow(hero, path: savePath);
        Assert.AreEqual(16, SaveMigrations.ReadVersion(savePath), "v16 bleibt (generische Listen)");
        RunProfile.Reset();
        Assert.IsTrue(SaveService.TryLoad(hero, savePath));
        Assert.AreEqual(StoryStatus.Done, S().Status("h10"));
        Assert.AreEqual(StoryStatus.Done, S().Status("h11"));
        Assert.AreEqual(StoryStatus.Done, S().Status("h12"));
        Assert.AreEqual(StoryStatus.Done, S().Status("h13"));
        Assert.IsTrue(RunProfile.Finale.MoortorOpen, "Moortor aus Akt + h10");
        Assert.IsTrue(RunProfile.Finale.FinalBossDefeated);
        Assert.AreEqual(ActProgressAkt3.Shatter, RunProfile.Finale.Decision, "Flag h13.shatter");
        yield break;
    }

    [UnityTest]
    public IEnumerator Koop_Snapshot_TraegtFinaleSchritte()
    {
        // P2-S3-Praezedenz: neue Schritte ueber den generischen Pfad
        // (Host-Snapshot Max-Merge), kein neuer Netzwerkcode.
        DriveToH11();
        StoryDirector.BookCollect(StoryIds.Glutspur);
        StoryDirector.BookCollect(StoryIds.Glutspur);
        StoryDirector.BookCollect(StoryIds.Glutspur);
        StoryDirector.BookKill(StoryIds.Thewes);
        string snap = S().Snapshot();
        var client = new StoryState();
        Assert.IsTrue(client.Merge(snap), "Host-Snapshot bewegt den Client");
        client.Refresh(RunProfile.Act);
        Assert.AreEqual(StoryStatus.Done, client.Status("h11"));
        Assert.AreEqual(StoryStatus.Done, client.Status("h12"));
        Assert.AreEqual(StoryStatus.Active, client.Status("h13"));
        yield break;
    }

    [UnityTest]
    public IEnumerator Moortor_Szene_ToreFindsThewes()
    // quest:h11 quest:h12 quest:n6 quest:n7
    // placement:DiabloZone:MoortorGate
    // placement:DiabloZone:MoortorReturnGate
    // placement:DiabloZone:StoryPickup_glutspur_0
    // placement:DiabloZone:StoryPickup_glutspur_1
    // placement:DiabloZone:StoryPickup_glutspur_2
    // placement:DiabloZone:StoryPickup_lanzenspitze_0
    // placement:DiabloZone:StoryPickup_lanzenspitze_1
    // placement:DiabloZone:StoryPickup_lanzenspitze_2
    // placement:DiabloZone:StoryPickup_lanzenspitze_3
    // placement:DiabloZone:StoryPickup_spendenseite_1
    {
        DriveToH11();
        var gate = MakeGate(ZoneGate.MoortorGateName, MoortorGatePos, MoortorArrival, ActProgress.Barrens);
        var ret = MakeGate(ZoneGate.MoortorReturnGateName, MoortorReturnPos, MoortorReturnArrival, ActProgress.Barrens);
        var thewes = MakeThewes(ThewesPos);
        var picks = new GameObject[8];
        for (int i = 0; i < 3; i++) picks[i] = MakePickup(StoryIds.Glutspur, i, GlutPos(i));
        for (int i = 0; i < 4; i++) picks[3 + i] = MakePickup(StoryIds.Lanzenspitze, i, LanzenPos(i));
        picks[7] = MakePickup(StoryIds.Spendenseite, 1, Seite1Pos);
        try
        {
            // Tore: exakte Builder-Namen, Moortor-Finale schaltet beide.
            Assert.AreEqual(ZoneGate.MoortorGateName, GameObject.Find(ZoneGate.MoortorGateName).name);
            Assert.AreEqual(ZoneGate.MoortorReturnGateName, GameObject.Find(ZoneGate.MoortorReturnGateName).name);
            Assert.IsTrue(gate.GetComponent<ZoneGate>().IsMoortorGate);
            Assert.IsTrue(ret.GetComponent<ZoneGate>().IsMoortorGate);
            Assert.IsFalse(gate.GetComponent<ZoneGate>().Locked, "Barrens-Boss offen");
            DiabloTestFixture.TeleportHero(hero, MoortorGatePos);
            gate.GetComponent<ZoneGate>().BeginDwell(hero, 80f);
            Assert.IsTrue(gate.GetComponent<ZoneGate>().TickDwell(80f + ZoneGate.DwellSeconds + 0.1f, -100f));
            Assert.AreEqual(MoortorArrival, hero.transform.position);
            // Thewes: exakter Name, Boss-Tier fuer den Balken.
            var foe = GameObject.Find(StoryIds.Thewes);
            Assert.IsNotNull(foe);
            Assert.AreEqual(LootTier.Boss, foe.GetComponent<EnemyHealth>().tier);
            Assert.IsTrue(DiabloTargetBar.IsBossTier(foe.GetComponent<EnemyHealth>().tier));
            Assert.AreEqual(StoryIds.Thewes, DiabloTargetBar.DisplayName(StoryIds.Thewes));
            // N6/N7 annehmen, dann alle 8 Funde ueber echte Trigger einsammeln.
            Assert.IsTrue(S().Accept("n6"));
            Assert.IsTrue(S().Accept("n7"));
            foreach (var pk in picks)
            {
                DiabloTestFixture.TeleportHero(hero, pk.transform.position + new Vector3(0.2f, 0f, 0f));
                yield return DiabloTestFixture.WaitSeconds(0.5f);
            }
            for (int i = 0; i < 8; i++) Assert.IsFalse(picks[i].activeSelf, $"fund {picks[i].name} despawns");
            Assert.AreEqual(StoryStatus.Ready, S().Status("n6"));
            Assert.AreEqual(StoryStatus.Ready, S().Status("n7"));
            Assert.AreEqual(StoryStatus.Done, S().Status("h11"), "R7: drei Spuren nennen Thewes");
            // H12: Thewes faellt wie jeder Boss, H13 wartet.
            StoryDirector.BookKill(foe.name);
            Assert.AreEqual(StoryStatus.Done, S().Status("h12"));
            Assert.IsTrue(RunProfile.Finale.FinalBossDefeated);
            Assert.AreEqual(StoryStatus.Active, S().Status("h13"));
        }
        finally
        {
            Object.DestroyImmediate(gate);
            Object.DestroyImmediate(ret);
            Object.DestroyImmediate(thewes);
            foreach (var pk in picks) if (pk != null) Object.DestroyImmediate(pk);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator Thewes_ErscheintErstNachAllenAkt3Quests()
    // quest:h11 quest:h12 quest:h13 quest:n6 quest:n7
    // cinderreach-act-boss-gating: Thewes ruht, bis H10/H11/N6/N7 erledigt
    // sind (ersetzt den H11-Auftritt aus S3c), erscheint genau 1x mit
    // Spawn-Cutscene und faellt als letzte Akt-III-Aufgabe vor H13.
    {
        ActBossGate.ResetRun();
        DriveToH11();
        CollectionAssert.AreEqual(new[] { "h10", "n6", "n7", "h11" }, ActBossGate.RequiredQuests(3));
        var thewes = MakeThewes(ThewesPos);
        try
        {
            ActBossGate.Tick(10f);
            Assert.IsFalse(thewes.activeSelf, "ruht, solange Akt-III-Quests offen sind");
            Assert.IsNull(GameObject.Find(StoryIds.Thewes));
            for (int i = 0; i < 3; i++) Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Glutspur));
            Assert.AreEqual(StoryStatus.Done, S().Status("h11"));
            Assert.AreEqual(StoryStatus.Active, S().Status("h12"));
            ActBossGate.Tick(11f);
            Assert.IsFalse(thewes.activeSelf, "N6/N7 offen: weiter kein Thewes");
            Assert.IsTrue(S().Accept("n6"));
            for (int i = 0; i < 4; i++) Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Lanzenspitze));
            StoryDirector.BookTalk(StoryIds.Jorin, hero);
            Assert.IsTrue(S().Accept("n7"));
            Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Spendenseite));
            StoryDirector.BookTalk(StoryIds.Ilsabe, hero);
            Assert.AreEqual(StoryStatus.Done, S().Status("n6"));
            Assert.AreEqual(StoryStatus.Done, S().Status("n7"));
            ActBossGate.Tick(12f);
            Assert.IsTrue(thewes.activeSelf, "alle anderen Akt-III-Quests erledigt");
            Assert.AreEqual(1, ActBossGate.Spawns[2]);
            Assert.AreEqual("intro.thewes", CutsceneDirector.ActiveId, "Spawn-Cutscene startet sofort");
            Assert.IsTrue(S().Flags.Contains(ActBossGate.SpawnFlag(3)), "Cutscene gilt als gespielt (Save)");
            ActBossGate.Tick(13f);
            Assert.AreEqual(1, ActBossGate.Spawns[2], "genau 1x");
            CutsceneDirector.Skip();
            StoryDirector.BookKill(thewes.name);
            Assert.AreEqual(StoryStatus.Done, S().Status("h12"));
            CollectionAssert.AreEqual(new[] { "3:Thewes:-" }, ActBossGate.KillLog, "Thewes faellt zuletzt");
            Assert.AreEqual(StoryStatus.Active, S().Status("h13"));
            Assert.IsTrue(StoryDirector.BookDecision(true));
        }
        finally
        {
            Object.DestroyImmediate(thewes);
            ActBossGate.ResetRun();
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator Finale_Surfacing_LogTrackerKarte()
    {
        DriveToH11();
        bool found = false;
        foreach (var line in StoryQuestUI.Lines(S()))
            if (line.Contains(Loc.T(StoryCatalog.Get("h11").TitleKey))) found = true;
        Assert.IsTrue(found, "h11 steht im Fuhrbuch");
        var world = new FakeWorld();
        world.npcs[StoryIds.Merle] = new Vector3(601f, 0f, 41f);
        world.npcs[StoryIds.Ottilie] = new Vector3(600f, 0f, 40f);
        world.pickups[StoryIds.Glutspur] = new List<Vector3> { new Vector3(604f, 0f, 44f) };
        var marks = new List<QuestMark>();
        QuestDestinations.Collect(S(), world, new Vector3(600f, 0f, 44f), marks);
        Assert.IsTrue(marks.Exists(m => m.questId == "h11"), "R7-Spuren stehen auf der Karte");
        string text = StoryDirector.TrackerText(S(), world, marks, new Vector3(600f, 0f, 44f));
        StringAssert.Contains(Loc.T("quest.h11.title"), text);
        yield break;
    }
}
