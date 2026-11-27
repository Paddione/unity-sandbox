using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// cinderreach-akt2-vertiefung S2 (Brache): R4 (Sehgläser), R5 (Scherben),
/// R6-Entscheidung (Ilsabe statt Merle), Tamm-/Ilsabe-Folgen und H9-Varianten
/// laufen über echte Fundstück-Trigger (StoryPickup, dieselben Positionen wie
/// TownBuilder.SpawnStoryPickups) und echte Gespräche (StoryDirector.BookTalk).
/// N12 hängt an N4 (Tamm), N13/N15 an N5 (Ilsabe); H9 zeigt je eine eigene
/// Zeile (x|nK über StoryDialog.Visible).
///
/// Jede Quest (quest:h18, quest:h19, quest:n12, quest:n13, quest:n14, quest:n15)
/// und jede neue Platzierung (placement:DiabloZone:<Name>) ist explizit markiert.
/// </summary>
public class Akt2BrachenTests
{
    GameObject hero;

    static readonly Vector3 B = BarrensLayout.BarrensOffset;
    // Dieselben Positionen wie TownBuilder.SpawnStoryPickups (S2-Anteil).
    static readonly Vector3 Sehglas0Pos = B + new Vector3(-5f, 0.7f, 11f);
    static readonly Vector3 Sehglas1Pos = B + new Vector3(5f, 0.7f, 11f);
    static readonly Vector3 Sehglas2Pos = B + new Vector3(11.5f, 0.7f, -6f);
    static Vector3 ScherbePos(int i) => B + new Vector3(
        i == 0 ? -2f : (i == 1 ? 5f : (i == 2 ? -6f : -8f)), 0.7f,
        i == 0 ? 7f : (i == 1 ? -2f : (i == 2 ? 5f : -11f)));
    static readonly Vector3 RadPos = B + new Vector3(11f, 0.7f, -10f);
    static readonly Vector3 SeitePos = B + new Vector3(-12f, 0.7f, -1f);

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
        Wallet.ResetRun();
        DiabloProgression.ResetRun();
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        ZoneGate.ResetRun();
        DiabloClickController.ExternalDrive = false;

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
        Wallet.ResetRun();
        DiabloProgression.ResetRun();
        ZoneGate.ResetRun();
        DiabloClickController.ExternalDrive = false;
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    static StoryState S() => RunProfile.Story;

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

    static void TeleportNear(Vector3 target, float east = 1f)
    {
        DiabloTestFixture.TeleportHero(GameObject.Find(DiabloNetworkPlayer.HeroName),
            target + new Vector3(east, 0f, 0f));
    }

    /// <summary>Nimmt ein Angebot über den echten Gesprächspfad an.</summary>
    static void AcceptViaTalk(string npc, string questId)
    {
        var plan = StoryDialogs.Plan(S(), npc);
        Assert.AreEqual(StoryTalkKind.Offer, plan.kind, $"offer for {questId}");
        Assert.AreEqual(questId, plan.questId);
        Assert.IsNotNull(plan.dialog, $"{questId}.offer dialog exists");
        StoryDirector.BookTalk(npc, GameObject.Find(DiabloNetworkPlayer.HeroName));
        Assert.AreEqual(StoryStatus.Active, S().Status(questId));
    }

    static IEnumerator RunDirectorSettled()
    {
        var go = GameObject.Find(DiabloNetworkPlayer.HeroName);
        if (go.GetComponent<StoryDirector>() == null) go.AddComponent<StoryDirector>();
        yield return DiabloTestFixture.WaitSeconds(0.6f);
    }

    /// <summary>Produktiver Toast-Pfad (StoryDirector.DrainToasts, privat) synchron:
    /// TickBarks im Update würde die Queue sonst vor dem Assert fressen.</summary>
    static void DrainProductionToasts()
    {
        var go = GameObject.Find(DiabloNetworkPlayer.HeroName);
        var director = go.GetComponent<StoryDirector>();
        if (director == null) director = go.AddComponent<StoryDirector>();
        var m = typeof(StoryDirector).GetMethod("DrainToasts",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, "StoryDirector.DrainToasts must exist");
        m.Invoke(director, null);
    }

    static bool LogShowsDone(string questId)
    {
        string title = Loc.T(StoryCatalog.Get(questId).TitleKey);
        foreach (var line in StoryQuestUI.Lines(S()))
            if (line.Contains(title) && line.Contains("✓")) return true;
        return false;
    }

    /// <summary>Akt II aufschließen (Tor + Aktabschluss), ohne Akt-I-Kette.</summary>
    static void OpenActTwo()
    {
        Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
        Assert.IsTrue(RunProfile.Act.CompleteAct());
        Assert.IsTrue(S().Refresh(RunProfile.Act));
    }

    /// <summary>H5 kurz abwickeln (Vorlauf für N5): Angebot, Ilsabe, Ottilie.</summary>
    static void PlayH5Quick(GameObject heroGo)
    {
        AcceptViaTalk(StoryIds.Ottilie, "h5");
        StoryDirector.BookTalk(StoryIds.Ilsabe, heroGo);
        Assert.AreEqual(StoryStatus.Ready, S().Status("h5"));
        StoryDirector.BookTalk(StoryIds.Ottilie, heroGo);
        Assert.AreEqual(StoryStatus.Done, S().Status("h5"));
    }

    /// <summary>H6/H7 über den echten Pfad (Vorlauf für H18/H19/N12).</summary>
    static void PlayH6H7()
    {
        AcceptViaTalk(StoryIds.Merle, "h6");
        StoryDirector.BookEnter(StoryIds.ZoneBarrens);
        Assert.AreEqual(StoryStatus.Done, S().Status("h6"));
        Assert.AreEqual(StoryStatus.Active, S().Status("h7"), "h7 auto-accepts");
        for (int i = 0; i < 3; i++) StoryDirector.BookKill("Dune Augur");
        Assert.AreEqual(StoryStatus.Done, S().Status("h7"));
    }

    static List<string> DrainBarks()
    {
        var ids = new List<string>();
        while (StoryBarks.TryTake(out var b)) ids.Add(b.id);
        return ids;
    }

    [UnityTest]
    public IEnumerator H18_R4_ZweiSehglasTalkToDone()
    // quest:h18
    // placement:DiabloZone:StoryPickup_sehglas_0
    // placement:DiabloZone:StoryPickup_sehglas_1
    {
        OpenActTwo();
        PlayH6H7();
        StoryBarks.ResetForTests();
        S().Refresh(RunProfile.Act); // Kette oeffnen (ggf. no-op)
        Assert.AreEqual(StoryStatus.Active, S().Status("h18"), "h18 auto-accepts ab h7-aktiv");
        DrainProductionToasts();
        Assert.Contains("sehglas.find", DrainBarks(), "R4-Fund spricht beim Aufwachen");
        var glas0 = MakePickup(StoryIds.Sehglas, 0, Sehglas0Pos);
        var glas1 = MakePickup(StoryIds.Sehglas, 1, Sehglas1Pos);
        try
        {
            TeleportNear(Sehglas0Pos, 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.AreEqual(1, S().Count("h18", 0));
            Assert.IsFalse(glas0.activeSelf, "counted find despawns");
            TeleportNear(Sehglas1Pos, 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.AreEqual(2, S().Count("h18", 0));
            Assert.IsFalse(glas1.activeSelf);
            StoryDirector.BookTalk(StoryIds.Merle, hero);
            Assert.AreEqual(StoryStatus.Done, S().Status("h18"), "h18 hat kein turn-in");
            Assert.IsTrue(LogShowsDone("h18"), string.Join("\n", StoryQuestUI.Lines(S())));
        }
        finally
        {
            Object.DestroyImmediate(glas0);
            Object.DestroyImmediate(glas1);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator H19_R5_DreiScherbenToDone()
    // quest:h19
    // placement:DiabloZone:StoryPickup_glasscherbe_0
    // placement:DiabloZone:StoryPickup_glasscherbe_1
    // placement:DiabloZone:StoryPickup_glasscherbe_2
    {
        OpenActTwo();
        PlayH6H7();
        StoryDirector.BookKill("The Glasswright");
        Assert.AreEqual(StoryStatus.Done, S().Status("h8"));
        StoryBarks.ResetForTests();
        S().Refresh(RunProfile.Act); // Kette oeffnen (ggf. no-op)
        Assert.AreEqual(StoryStatus.Active, S().Status("h19"), "h19 auto-accepts ab h8-done");
        DrainProductionToasts();
        Assert.Contains("scherben.find", DrainBarks(), "R5-Fund spricht beim Aufwachen");
        var scherben = new GameObject[3];
        for (int i = 0; i < 3; i++) scherben[i] = MakePickup(StoryIds.Glasscherbe, i, ScherbePos(i));
        try
        {
            for (int i = 0; i < 3; i++)
            {
                TeleportNear(ScherbePos(i), 0.2f);
                yield return DiabloTestFixture.WaitSeconds(0.5f);
                Assert.IsFalse(scherben[i].activeSelf, $"scherbe {i} despawns");
            }
            Assert.AreEqual(StoryStatus.Done, S().Status("h19"), "h19 hat kein turn-in");
            Assert.IsTrue(LogShowsDone("h19"), string.Join("\n", StoryQuestUI.Lines(S())));
        }
        finally
        {
            foreach (var g in scherben) Object.DestroyImmediate(g);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator N14_TammsScherbeToDoneUndH19Folge()
    // quest:n14
    // placement:DiabloZone:StoryPickup_glasscherbe_3
    {
        OpenActTwo();
        PlayH6H7();
        // H18-Gespraech vorab klaeren, damit Merle Angebote macht (Talk vor Offer).
        StoryDirector.BookTalk(StoryIds.Merle, hero);
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Merle, "n4");
        StoryDirector.BookKill("Glass Elite_0");
        Assert.AreEqual(StoryStatus.Ready, S().Status("n4"));
        StoryDirector.BookTalk(StoryIds.Merle, hero);
        Assert.AreEqual(StoryStatus.Done, S().Status("n4"));
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Merle, "n12");
        var rad = MakePickup(StoryIds.Speichenrad, 0, RadPos);
        TeleportNear(RadPos, 0.2f);
        yield return DiabloTestFixture.WaitSeconds(0.5f);
        StoryDirector.BookTalk(StoryIds.Merle, hero);
        Assert.AreEqual(StoryStatus.Ready, S().Status("n12"));
        StoryDirector.BookTalk(StoryIds.Merle, hero);
        Assert.AreEqual(StoryStatus.Done, S().Status("n12"));
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Merle, "n14");
        var scherbe = MakePickup(StoryIds.Glasscherbe, 3, ScherbePos(3));
        try
        {
            // Tamms Scherbe zählt für N14 (Buchungen zählen für jede aktive Aufgabe).
            TeleportNear(ScherbePos(3), 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.AreEqual(StoryStatus.Ready, S().Status("n14"));
            var turnIn = StoryDialogs.Plan(S(), StoryIds.Merle);
            Assert.AreEqual("n14", turnIn.questId);
            Assert.IsNotNull(turnIn.dialog, "n14.done dialog exists");
            StoryDirector.BookTalk(StoryIds.Merle, hero);
            Assert.AreEqual(StoryStatus.Done, S().Status("n14"));
            yield return RunDirectorSettled();
            Assert.AreEqual(500, hero.GetComponent<Wallet>().Gold, "n4 (250) + n12 (150) + n14 (100)");
            Assert.IsTrue(LogShowsDone("n14"), string.Join("\n", StoryQuestUI.Lines(S())));
            // N14-Folge: im H19-Gespräch nennt Merle Tamms Namen nur mit der Scherbe.
            var talk = StoryDialogs.Get("h18.talk.merle");
            Assert.AreEqual(4, talk.speakers.Length);
            Assert.IsTrue(StoryDialog.Visible(talk.speakers[3], new HashSet<string> { "n14" }), "n14 zeigt die Namenszeile");
            Assert.IsFalse(StoryDialog.Visible(talk.speakers[3], new HashSet<string>()));
        }
        finally
        {
            Object.DestroyImmediate(rad);
            Object.DestroyImmediate(scherbe);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator N12_TammsRadToDoneUndH9MerleFolge()
    // quest:n12
    // placement:DiabloZone:StoryPickup_speichenrad_0
    {
        OpenActTwo();
        PlayH6H7();
        // H18-Gespraech vorab klaeren, damit Merle Angebote macht (Talk vor Offer).
        StoryDirector.BookTalk(StoryIds.Merle, hero);
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Merle, "n4");
        StoryDirector.BookKill("Glass Elite_0");
        Assert.AreEqual(StoryStatus.Ready, S().Status("n4"));
        var turnInN4 = StoryDialogs.Plan(S(), StoryIds.Merle);
        Assert.AreEqual("n4", turnInN4.questId);
        StoryDirector.BookTalk(StoryIds.Merle, hero);
        Assert.AreEqual(StoryStatus.Done, S().Status("n4"));
        yield return RunDirectorSettled();
        Assert.AreEqual(250, hero.GetComponent<Wallet>().Gold);
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Merle, "n12");
        var rad = MakePickup(StoryIds.Speichenrad, 0, RadPos);
        try
        {
            TeleportNear(RadPos, 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.AreEqual(1, S().Count("n12", 0));
            Assert.IsFalse(rad.activeSelf);
            StoryDirector.BookTalk(StoryIds.Merle, hero);
            Assert.AreEqual(StoryStatus.Ready, S().Status("n12"));
            var turnIn = StoryDialogs.Plan(S(), StoryIds.Merle);
            Assert.AreEqual("n12", turnIn.questId);
            Assert.IsNotNull(turnIn.dialog, "n12.done dialog exists");
            StoryDirector.BookTalk(StoryIds.Merle, hero);
            Assert.AreEqual(StoryStatus.Done, S().Status("n12"));
            yield return RunDirectorSettled();
            Assert.AreEqual(400, hero.GetComponent<Wallet>().Gold, "n4 (250) + n12 (150)");
            Assert.IsTrue(LogShowsDone("n12"), string.Join("\n", StoryQuestUI.Lines(S())));
            // N12-Folge: H9-Merle vergräbt das Glas nur mit Tamms Rad.
            var merle = StoryDialogs.Get("h9.talk.merle");
            Assert.AreEqual(7, merle.speakers.Length);
            Assert.IsTrue(StoryDialog.Visible(merle.speakers[6], new HashSet<string> { "n12" }), "n12 zeigt die Vergrabungszeile");
            Assert.IsFalse(StoryDialog.Visible(merle.speakers[6], new HashSet<string>()));
        }
        finally
        {
            Object.DestroyImmediate(rad);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator N13N15_R6_IlsabeStattMerleUndH9Folgen()
    // quest:n13 quest:n15
    // placement:DiabloZone:StoryPickup_sehglas_2
    // placement:DiabloZone:StoryPickup_spendenseite_0
    {
        OpenActTwo();
        PlayH5Quick(hero);
        S().Refresh(RunProfile.Act);
        // Ilsabe bietet erst das Lieferbuch an (Katalogreihenfolge), dann N5.
        AcceptViaTalk(StoryIds.Ilsabe, "n1");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ilsabe, "n5");
        for (int i = 0; i < 4; i++) StoryDirector.BookKill("Dune Augur");
        Assert.AreEqual(StoryStatus.Ready, S().Status("n5"));
        var turnInN5 = StoryDialogs.Plan(S(), StoryIds.Ilsabe);
        Assert.AreEqual("n5", turnInN5.questId);
        StoryDirector.BookTalk(StoryIds.Ilsabe, hero);
        Assert.AreEqual(StoryStatus.Done, S().Status("n5"));
        S().Refresh(RunProfile.Act);
        // N11 (Catch-up über H3) steht im Katalog vor N13 — als Pass-through annehmen.
        AcceptViaTalk(StoryIds.Ilsabe, "n11");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ilsabe, "n13");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ilsabe, "n15");
        var glas = MakePickup(StoryIds.Sehglas, 2, Sehglas2Pos);
        var seite = MakePickup(StoryIds.Spendenseite, 0, SeitePos);
        try
        {
            TeleportNear(Sehglas2Pos, 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.AreEqual(StoryStatus.Ready, S().Status("n13"));
            var turnIn13 = StoryDialogs.Plan(S(), StoryIds.Ilsabe);
            Assert.AreEqual("n13", turnIn13.questId);
            Assert.IsNotNull(turnIn13.dialog, "n13.done dialog exists");
            StoryDirector.BookTalk(StoryIds.Ilsabe, hero);
            Assert.AreEqual(StoryStatus.Done, S().Status("n13"));
            TeleportNear(SeitePos, 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.AreEqual(StoryStatus.Ready, S().Status("n15"));
            var turnIn15 = StoryDialogs.Plan(S(), StoryIds.Ilsabe);
            Assert.AreEqual("n15", turnIn15.questId);
            StoryDirector.BookTalk(StoryIds.Ilsabe, hero);
            Assert.AreEqual(StoryStatus.Done, S().Status("n15"));
            yield return RunDirectorSettled();
            Assert.AreEqual(350, hero.GetComponent<Wallet>().Gold, "n13 (200) + n15 (150)");
            Assert.IsTrue(LogShowsDone("n13"), string.Join("\n", StoryQuestUI.Lines(S())));
            Assert.IsTrue(LogShowsDone("n15"), string.Join("\n", StoryQuestUI.Lines(S())));
            // R6: die Gläser gingen an Ilsabe, nicht an Merle — H18 bleibt unberührt.
            Assert.AreEqual(StoryStatus.Locked, S().Status("h18"), "R6-Entscheidung startet H18 gar nicht erst");
            // H9-Folgen: Ottilie liest mit fünf Gläsern und nennt den Preis.
            var ottilie = StoryDialogs.Get("h9.talk.ottilie");
            Assert.AreEqual(8, ottilie.speakers.Length);
            Assert.IsTrue(StoryDialog.Visible(ottilie.speakers[6], new HashSet<string> { "n13" }), "n13 zeigt die Fünf-Gläser-Zeile");
            Assert.IsFalse(StoryDialog.Visible(ottilie.speakers[6], new HashSet<string>()));
            Assert.IsTrue(StoryDialog.Visible(ottilie.speakers[7], new HashSet<string> { "n15" }), "n15 zeigt die Preiszeile");
            Assert.IsFalse(StoryDialog.Visible(ottilie.speakers[7], new HashSet<string>()));
        }
        finally
        {
            Object.DestroyImmediate(glas);
            Object.DestroyImmediate(seite);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator S2_Surfacing_TrackerFuhrbuchKartenUndTexte()
    // quest:h18 (Stellvertreter: alle S2-Quests laufen über dieselben generischen Pfade)
    // placement:DiabloZone:StoryPickup_sehglas_0
    // placement:DiabloZone:StoryPickup_sehglas_1
    // placement:DiabloZone:StoryPickup_sehglas_2
    // placement:DiabloZone:StoryPickup_glasscherbe_0
    // placement:DiabloZone:StoryPickup_glasscherbe_1
    // placement:DiabloZone:StoryPickup_glasscherbe_2
    // placement:DiabloZone:StoryPickup_glasscherbe_3
    // placement:DiabloZone:StoryPickup_speichenrad_0
    // placement:DiabloZone:StoryPickup_spendenseite_0
    {
        OpenActTwo();
        PlayH6H7();
        S().Refresh(RunProfile.Act); // Kette oeffnen (ggf. no-op)
        Assert.AreEqual(StoryStatus.Active, S().Status("h18"));
        // Fuhrbuch: neue Aufgabe mit Zielen.
        bool found = false;
        foreach (var line in StoryQuestUI.Lines(S()))
            if (line.Contains(Loc.T(StoryCatalog.Get("h18").TitleKey))) found = true;
        Assert.IsTrue(found, "h18 surfaces in the log");
        // Tracker: verfolgt, umschaltbar, mit Zielen.
        Assert.IsTrue(StoryTracking.IsTracked(S(), "h18"));
        Assert.IsFalse(StoryTracking.Toggle(S(), "h18"), "toggle hides");
        Assert.IsTrue(StoryTracking.Toggle(S(), "h18"), "toggle tracks again");
        // Karten: ein Zielschritt für die Gläser plus Trackertext.
        var world = new FakeWorld();
        var heroPos = B + new Vector3(0f, 0f, 13f);
        world.npcs[StoryIds.Merle] = new Vector3(600f, 0f, 40f);
        world.pickups[StoryIds.Sehglas] = new List<Vector3> { Sehglas0Pos, Sehglas1Pos };
        var marks = new List<QuestMark>();
        QuestDestinations.Collect(S(), world, heroPos, marks);
        Assert.IsTrue(marks.Exists(m => m.questId == "h18"), "h18 marks the map");
        string text = StoryDirector.TrackerText(S(), world, marks, heroPos);
        StringAssert.Contains(Loc.T("quest.h18.title"), text);
        StringAssert.Contains("Sehgläser", text);
        // Alle neuen S2-Zeilen haben deutschen Text (Clips rendert dieser Commit mit).
        var keys = new List<string>();
        foreach (var d in StoryDialogs.All)
        {
            if (d.id == "h9.talk.merle" || d.id == "h9.talk.ottilie"
                || d.id == "h18.hint" || d.id == "h18.talk.merle" || d.id == "h19.hint"
                || d.id == "n12.offer" || d.id == "n12.hint" || d.id == "n12.done"
                || d.id == "n13.offer" || d.id == "n13.hint" || d.id == "n13.done"
                || d.id == "n14.offer" || d.id == "n14.hint" || d.id == "n14.done"
                || d.id == "n15.offer" || d.id == "n15.hint" || d.id == "n15.done")
                for (int i = 0; i < d.speakers.Length; i++) keys.Add(d.Key(i));
        }
        keys.Add("bark.sehglas.find");
        keys.Add("bark.scherben.find");
        Assert.AreEqual(43, keys.Count);
        var missing = new List<string>();
        foreach (var k in keys) if (!Loc.Has(k)) missing.Add(k);
        Assert.IsEmpty(missing, string.Join("\n", missing));
        yield break;
    }
}
