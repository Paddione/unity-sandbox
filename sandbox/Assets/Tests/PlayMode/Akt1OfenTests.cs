using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// cinderreach-akt1-vertiefung S3 (Ofen/Wacht): R3 (Siegel-Entscheidung),
/// Jorins Liste mit Einschmelz-Entscheidung, Ilsabes letztes Angebot, das
/// H5-Finale und die N10/N11-Folgezeilen (x|nK wie N8/N9 in S2).
///
/// Fundstücke laufen über echte Fundstück-Trigger (StoryPickup, dieselben
/// Kiln-Positionen wie für die echte Platzierung vorgesehen), Kills und
/// Gespräche über dieselben Einstiegspunkte wie zur Laufzeit
/// (StoryDirector.BookKill/BookTalk). Jede Quest (quest:h16, quest:h17,
/// quest:n10, quest:n11) ist explizit markiert.
/// </summary>
public class Akt1OfenTests
{
    GameObject hero;

    // Vorgeschlagene echte Platzierung im Glutofen (KilnOffset + lokal):
    // Fuhrliste am Aufgang, Rußseite auf dem Weg zur Ofenmutter.
    static readonly Vector3 ListePos = KilnLayout.KilnOffset + new Vector3(10f, 0.7f, 8f);
    static readonly Vector3 SeitePos = KilnLayout.KilnOffset + new Vector3(3f, 0.7f, -5f);

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
        StoryBarks.ResetForTests();

        hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = DiabloNetworkPlayer.HeroName;
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
        StoryBarks.ResetForTests();
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    static StoryState S() => RunProfile.Story;

    static GameObject MakePickup(string itemId, Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"StoryPickup_{itemId}_test";
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

    static IEnumerator RunDirector()
    {
        var go = GameObject.Find(DiabloNetworkPlayer.HeroName);
        if (go.GetComponent<StoryDirector>() == null) go.AddComponent<StoryDirector>();
        yield return null;
        yield return null;
    }

    static bool LogShowsDone(string questId)
    {
        string title = Loc.T(StoryCatalog.Get(questId).TitleKey);
        foreach (var line in StoryQuestUI.Lines(S()))
            if (line.Contains(title) && line.Contains("✓")) return true;
        return false;
    }

    /// <summary>H1+h2 über den echten Pfad bis in den Ofen (Vorlauf für H16).</summary>
    static void PlayH1H2()
    {
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Ottilie, "h1");
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Fuhrbrief));
        StoryDirector.BookKill("The Fen Brute(Clone)");
        StoryDirector.BookTalk(StoryIds.Ottilie, GameObject.Find(DiabloNetworkPlayer.HeroName));
        Assert.AreEqual(StoryStatus.Done, S().Status("h1"));
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "h2");
        StoryDirector.BookEnter(StoryIds.ZoneOssuary);
        for (int i = 0; i < 4; i++) StoryDirector.BookKill("Bone Warden");
        StoryDirector.BookKill("The Hollow Abbot");
        StoryDirector.BookTalk(StoryIds.Ottilie, GameObject.Find(DiabloNetworkPlayer.HeroName));
        Assert.AreEqual(StoryStatus.Done, S().Status("h2"));
    }

    [UnityTest]
    public IEnumerator H16N11_OfenFunde_ListeSeiteLancersToDone()
    // quest:h16 quest:n11
    // placement:DiabloZone:StoryPickup_fuhrliste_0
    // placement:DiabloZone:StoryPickup_russseite_0
    {
        var liste = MakePickup(StoryIds.Fuhrliste, ListePos);
        var seite = MakePickup(StoryIds.Russseite, SeitePos);
        try
        {
            PlayH1H2();
            S().Refresh(RunProfile.Act);
            AcceptViaTalk(StoryIds.Ottilie, "h3");
            StoryDirector.BookTalk(StoryIds.Jorin, hero);
            StoryDirector.BookEnter(StoryIds.ZoneKiln);
            Assert.AreEqual(StoryStatus.Done, S().Status("h3"));
            Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
            S().Refresh(RunProfile.Act);
            // Jorin bietet erst die Spitzen an, dann die Liste.
            AcceptViaTalk(StoryIds.Jorin, "n3");
            for (int i = 0; i < 6; i++) StoryDirector.BookKill("Ash Lancer_0");
            // Jorins Zuruf: ein Lanzer-Tod ruft, die restlichen drosselt das Fenster.
            int lancerHeard = 0;
            while (StoryBarks.TryTake(out var queued))
                if (queued.id == "lancer.fall") lancerHeard++;
            Assert.AreEqual(1, lancerHeard, "six lancer kills bark lancer.fall exactly once");
            var turnInN3 = StoryDialogs.Plan(S(), StoryIds.Jorin);
            Assert.AreEqual("n3", turnInN3.questId);
            Assert.IsNotNull(turnInN3.dialog, "n3.done dialog exists");
            StoryDirector.BookTalk(StoryIds.Jorin, hero);
            S().Refresh(RunProfile.Act);
            AcceptViaTalk(StoryIds.Jorin, "h16");
            // Die Rußseite wartet, bis N11 angenommen ist.
            TeleportNear(SeitePos, 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.IsTrue(seite.activeSelf, "early find waits instead of being lost");
            Assert.AreEqual(0, S().Count("n11", 0));
            // Ilsabe bietet erst das Lieferbuch an, dann die Seite.
            AcceptViaTalk(StoryIds.Ilsabe, "n1");
            Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Lieferbuch));
            StoryDirector.BookTalk(StoryIds.Ilsabe, hero);
            S().Refresh(RunProfile.Act);
            AcceptViaTalk(StoryIds.Ilsabe, "n11");
            // Beide Ofen-Fundstücke über echte Trigger.
            TeleportNear(ListePos, 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.AreEqual(1, S().Count("h16", 0));
            Assert.IsFalse(liste.activeSelf, "counted find despawns");
            TeleportNear(SeitePos, 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.AreEqual(1, S().Count("n11", 0));
            Assert.IsFalse(seite.activeSelf);
            Assert.AreEqual(StoryStatus.Ready, S().Status("n11"));
            var turnInN11 = StoryDialogs.Plan(S(), StoryIds.Ilsabe);
            Assert.AreEqual("n11", turnInN11.questId);
            Assert.IsNotNull(turnInN11.dialog, "n11.done dialog exists");
            StoryDirector.BookTalk(StoryIds.Ilsabe, hero);
            Assert.AreEqual(StoryStatus.Done, S().Status("n11"));
            yield return RunDirector();
            Assert.AreEqual(320, hero.GetComponent<Wallet>().Gold, "n3 (50) + n1 (150) + n11 (120)");
            // Jorins Entscheidung: zwei Lanzer, dann das Einschmelzen.
            StoryDirector.BookKill("Ash Lancer_0");
            StoryDirector.BookKill("Ash Lancer_0");
            Assert.AreEqual(StoryStatus.Ready, S().Status("h16"));
            var turnInH16 = StoryDialogs.Plan(S(), StoryIds.Jorin);
            Assert.AreEqual("h16", turnInH16.questId);
            Assert.IsNotNull(turnInH16.dialog, "h16.done dialog exists");
            var done = StoryDialogs.Get("h16.done");
            Assert.AreEqual(4, done.speakers.Length);
            StoryDirector.BookTalk(StoryIds.Jorin, hero);
            Assert.AreEqual(StoryStatus.Done, S().Status("h16"));
            Assert.IsTrue(LogShowsDone("h16"), string.Join("\n", StoryQuestUI.Lines(S())));
            Assert.IsTrue(LogShowsDone("n11"), string.Join("\n", StoryQuestUI.Lines(S())));
        }
        finally
        {
            Object.DestroyImmediate(liste);
            Object.DestroyImmediate(seite);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator H17_R3_IlsabeDannOttilie_EntscheidungUndH5Finale()
    // quest:h17
    {
        Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
        Assert.IsTrue(RunProfile.Act.CompleteAct());
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Ottilie, "h5");
        // H5 zuerst: Ilsabes Gespräch und Ottilies Abgabe, damit R3 danach
        // beide Gespraeche mit ihren eigenen Dialogen erreicht.
        StoryDirector.BookTalk(StoryIds.Ilsabe, hero);
        Assert.AreEqual(StoryStatus.Ready, S().Status("h5"));
        var turnInH5First = StoryDialogs.Plan(S(), StoryIds.Ottilie);
        Assert.AreEqual(StoryTalkKind.TurnIn, turnInH5First.kind);
        Assert.AreEqual("h5", turnInH5First.questId);
        StoryDirector.BookTalk(StoryIds.Ottilie, hero);
        Assert.AreEqual(StoryStatus.Done, S().Status("h5"));
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "n2");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "h17");
        // R3: erst Ilsabes letztes Angebot, dann Ottilies Abwägung.
        var talkIlsabe = StoryDialogs.Plan(S(), StoryIds.Ilsabe);
        Assert.AreEqual(StoryTalkKind.Talk, talkIlsabe.kind);
        Assert.AreEqual("h17", talkIlsabe.questId);
        Assert.AreEqual(StoryDialogs.Get("h17.talk.ilsabe"), talkIlsabe.dialog);
        StoryDirector.BookTalk(StoryIds.Ilsabe, hero);
        Assert.AreEqual(StoryStatus.Active, S().Status("h17"), "ein Gespräch reicht nicht (R3)");
        var talkOttilie = StoryDialogs.Plan(S(), StoryIds.Ottilie);
        Assert.AreEqual(StoryTalkKind.Talk, talkOttilie.kind);
        Assert.AreEqual("h17", talkOttilie.questId);
        Assert.AreEqual(StoryDialogs.Get("h17.talk.ottilie"), talkOttilie.dialog);
        StoryDirector.BookTalk(StoryIds.Ottilie, hero);
        Assert.AreEqual(StoryStatus.Done, S().Status("h17"));
        Assert.IsTrue(LogShowsDone("h17"), string.Join("\n", StoryQuestUI.Lines(S())));
        // Ottilies Entscheidung steht im Buch, nicht bei Ilsabe.
        Assert.AreEqual("Ich glaube dem Buch. Nicht ihr, nicht ihm. Dem Buch.",
            Loc.T("dlg.h17.talk.ottilie.3"));
        // N11-Folge im H5-Angebot: ohne die Seite keine Preiszeile.
        var offer = StoryDialogs.Get("h5.offer");
        Assert.AreEqual(7, offer.speakers.Length);
        Assert.IsTrue(StoryDialog.Visible(offer.speakers[6], new HashSet<string> { "n11" }), "n11 zeigt die Preiszeile");
        Assert.IsFalse(StoryDialog.Visible(offer.speakers[6], new HashSet<string>()));
        // Das H5-Finale steht in zwei Fassungen je nach N11.
        var finale = StoryDialogs.Get("h5.done");
        Assert.AreEqual(6, finale.speakers.Length);
        var ohne = new HashSet<string>();
        var mit = new HashSet<string> { "n11" };
        Assert.IsFalse(StoryDialog.Visible(finale.speakers[4], ohne));
        Assert.IsTrue(StoryDialog.Visible(finale.speakers[4], mit));
        Assert.IsTrue(StoryDialog.Visible(finale.speakers[5], ohne));
        Assert.IsFalse(StoryDialog.Visible(finale.speakers[5], mit));
        // H5-Finale: Ottilie hat die Laterne gelöscht.
        Assert.IsTrue(LogShowsDone("h5"), string.Join("\n", StoryQuestUI.Lines(S())));
        yield break;
    }

    [UnityTest]
    public IEnumerator N10_Schlackeglut_ThrallsToDoneUndH4Folge()
    // quest:n10
    {
        Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Jorin, "n3");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Jorin, "n10");
        StoryBarks.ResetForTests();
        for (int i = 0; i < 4; i++) StoryDirector.BookKill("Cinder Thrall_0");
        // Die Knechte rufen den Glut-Zuruf — einmal, dann gedrosselt.
        Assert.IsTrue(StoryBarks.TryTake(out var first));
        Assert.AreEqual("thrall.fall", first.id);
        Assert.IsFalse(StoryBarks.TryTake(out _), "repeat window keeps one line");
        Assert.AreEqual(StoryStatus.Ready, S().Status("n10"));
        var turnIn = StoryDialogs.Plan(S(), StoryIds.Jorin);
        Assert.AreEqual("n10", turnIn.questId);
        Assert.IsNotNull(turnIn.dialog, "n10.done dialog exists");
        StoryDirector.BookTalk(StoryIds.Jorin, hero);
        Assert.AreEqual(StoryStatus.Done, S().Status("n10"));
        yield return RunDirector();
        Assert.AreEqual(80, hero.GetComponent<Wallet>().Gold);
        Assert.IsTrue(LogShowsDone("n10"), string.Join("\n", StoryQuestUI.Lines(S())));
        // N10-Folge: der H4-Abschluss nennt die Wacht als Absender.
        var h4done = StoryDialogs.Get("h4.done");
        Assert.AreEqual(5, h4done.speakers.Length);
        Assert.IsTrue(StoryDialog.Visible(h4done.speakers[4], new HashSet<string> { "n10" }), "n10 ändert H4-Abschluss");
        Assert.IsFalse(StoryDialog.Visible(h4done.speakers[4], new HashSet<string>()));
        yield break;
    }

    [UnityTest]
    public IEnumerator S3_Surfacing_TrackerFuhrbuchKartenUndTexte()
    // quest:n10 (Stellvertreter: alle S3-Quests laufen über dieselben generischen Pfade)
    {
        Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Jorin, "n3");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Jorin, "n10");
        // Fuhrbuch: neue Aufgabe mit Zielen.
        bool found = false;
        foreach (var line in StoryQuestUI.Lines(S()))
            if (line.Contains(Loc.T(StoryCatalog.Get("n10").TitleKey))) found = true;
        Assert.IsTrue(found, "n10 surfaces in the log");
        // Tracker: verfolgt, umschaltbar, mit Zielen.
        Assert.IsTrue(StoryTracking.IsTracked(S(), "n10"));
        Assert.IsFalse(StoryTracking.Toggle(S(), "n10"), "toggle hides");
        Assert.IsTrue(StoryTracking.Toggle(S(), "n10"), "toggle tracks again");
        // Karten: ein Zielschritt für die Knechte plus Trackertext.
        var world = new FakeWorld();
        var heroPos = KilnLayout.KilnOffset + new Vector3(0f, 0f, 10f);
        world.npcs[StoryIds.Jorin] = new Vector3(-3.5f, 0f, 11f);
        world.enemies[StoryIds.CinderThrall] = new List<Vector3>
            { KilnLayout.KilnOffset + new Vector3(2f, 0f, 6f) };
        var marks = new List<QuestMark>();
        QuestDestinations.Collect(S(), world, heroPos, marks);
        Assert.IsTrue(marks.Exists(m => m.questId == "n10"), "n10 marks the map");
        string text = StoryDirector.TrackerText(S(), world, marks, heroPos);
        StringAssert.Contains(Loc.T("quest.n10.title"), text);
        StringAssert.Contains("Schlackenknechte", text);
        // Alle neuen S3-Zeilen haben deutschen Text (Clips rendert S4).
        var keys = new List<string>();
        foreach (var d in StoryDialogs.All)
        {
            if (d.id == "h16.offer" || d.id == "h16.hint" || d.id == "h16.done"
                || d.id == "h17.offer" || d.id == "h17.talk.ilsabe" || d.id == "h17.talk.ottilie"
                || d.id == "n10.offer" || d.id == "n10.hint" || d.id == "n10.done"
                || d.id == "n11.offer" || d.id == "n11.hint" || d.id == "n11.done")
                for (int i = 0; i < d.speakers.Length; i++) keys.Add(d.Key(i));
        }
        keys.Add("dlg.h4.done.5");
        keys.Add("dlg.h5.offer.7");
        keys.Add("dlg.h5.done.5");
        keys.Add("dlg.h5.done.6");
        keys.Add("bark.lancer.fall");
        keys.Add("bark.thrall.fall");
        Assert.AreEqual(37, keys.Count);
        var missing = new List<string>();
        foreach (var k in keys) if (!Loc.Has(k)) missing.Add(k);
        Assert.IsEmpty(missing, string.Join("\n", missing));
        yield break;
    }
}
