using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// cinderreach-akt3-finale S2 (Wacht): H10 (letzte Nacht, vier Gespraeche plus
/// Scherbe ans Licht), N6 (Jorins Einschmelzen), N7 (Ilsabes letztes Angebot)
/// und die N6/N7-Folgen auf h10.done (x|nK-Muster wie h9.talk.merle.7).
///
/// Das Modell gehoert S3: H10/N6/N7 stehen noch nicht im StoryCatalog
/// (Akt3ModellTests.Kennungen_BleibenReserviert). Darum laufen die Reisen auf
/// Dialogebene (Get/Sichtbarkeit/Loc) plus Plan-Prioritaet und Surfacing mit
/// Bestand-Quests. Keine neuen Platzierungen (kein neuer Ort), keine Barks.
/// </summary>
public class Akt3WachtTests
{
    GameObject hero;

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

    static void AssertDialog(string id, int lines)
    {
        var d = StoryDialogs.Get(id);
        Assert.IsNotNull(d, id + " exists");
        Assert.AreEqual(lines, d.speakers.Length, id + " line count");
        for (int i = 0; i < lines; i++)
        {
            string key = d.Key(i);
            Assert.IsTrue(Loc.Has(key), "loc " + key);
            Assert.AreNotEqual(key, Loc.T(key), "de text " + key);
        }
    }

    [UnityTest]
    public IEnumerator H10_Dialogkette_AngebotBisAbgabe()
    {
        AssertDialog("h10.offer", 4);
        AssertDialog("h10.hint", 1);
        AssertDialog("h10.done", 6);
        AssertDialog("h10.talk.ottilie", 3);
        AssertDialog("h10.talk.merle", 3);
        AssertDialog("h10.talk.ilsabe", 2);
        AssertDialog("h10.talk.jorin", 2);
        // Talk-Ids folgen der Plan-Konvention quest.talk.npc (S3 haengt sie an H9).
        Assert.AreEqual("h10.talk." + StoryIds.Ottilie, StoryDialogs.Get("h10.talk.ottilie").id);
        Assert.AreEqual("h10.talk." + StoryIds.Jorin, StoryDialogs.Get("h10.talk.jorin").id);
        var offer = StoryDialogs.Get("h10.offer");
        foreach (var s in offer.speakers) Assert.AreEqual(StoryIds.Ottilie, StoryDialog.Speaker(s));
        var done = StoryDialogs.Get("h10.done");
        Assert.AreEqual("n", StoryDialog.Speaker(done.speakers[0]), "h10.done eroeffnet der Erzaehler");
        yield break;
    }

    [UnityTest]
    public IEnumerator N6_JorinKette_AngebotBisAbgabe()
    {
        AssertDialog("n6.offer", 2);
        AssertDialog("n6.hint", 1);
        AssertDialog("n6.done", 3);
        var done = StoryDialogs.Get("n6.done");
        foreach (var s in done.speakers) Assert.AreEqual(StoryIds.Jorin, StoryDialog.Speaker(s));
        Assert.IsTrue(StoryDialogs.IsShop(StoryIds.Jorin), "Jorin bleibt Laden-NPC");
        yield break;
    }

    [UnityTest]
    public IEnumerator N7_IlsabeKette_AngebotBisAbgabe()
    {
        AssertDialog("n7.offer", 2);
        AssertDialog("n7.hint", 1);
        AssertDialog("n7.done", 3);
        var offer = StoryDialogs.Get("n7.offer");
        foreach (var s in offer.speakers) Assert.AreEqual(StoryIds.Ilsabe, StoryDialog.Speaker(s));
        Assert.IsTrue(StoryDialogs.IsShop(StoryIds.Ilsabe), "Ilsabe bleibt Laden-NPC");
        yield break;
    }

    [UnityTest]
    public IEnumerator NPC_Folgen_N6N7_AufH10Done()
    {
        var done = StoryDialogs.Get("h10.done");
        Assert.AreEqual(6, done.speakers.Length);
        Assert.AreEqual(StoryIds.Jorin + "|n6", done.speakers[4]);
        Assert.AreEqual(StoryIds.Ilsabe + "|n7", done.speakers[5]);
        Assert.IsTrue(StoryDialog.Visible(done.speakers[4], new HashSet<string> { "n6" }), "n6 zeigt die Tiegelzeile");
        Assert.IsFalse(StoryDialog.Visible(done.speakers[4], new HashSet<string>()), "ohne n6 keine Tiegelzeile");
        Assert.IsTrue(StoryDialog.Visible(done.speakers[5], new HashSet<string> { "n7" }), "n7 zeigt die Absagezeile");
        Assert.IsFalse(StoryDialog.Visible(done.speakers[5], new HashSet<string>()), "ohne n7 keine Absagezeile");
        // Basiszeilen sind unbedingt sichtbar.
        for (int i = 0; i < 4; i++)
            Assert.IsTrue(StoryDialog.Visible(done.speakers[i], new HashSet<string>()), "h10.done." + (i + 1));
        yield break;
    }

    [UnityTest]
    public IEnumerator Wacht_Surfacing_TrackerFuhrbuchKartenUndTexte()
    {
        S().Refresh(RunProfile.Act);
        // Angebot annehmen ueber den echten Gespraechspfad (Bestand-Quest als Traeger).
        var plan = StoryDialogs.Plan(S(), StoryIds.Ottilie);
        Assert.AreEqual(StoryTalkKind.Offer, plan.kind);
        Assert.AreEqual("h1", plan.questId);
        Assert.IsNotNull(plan.dialog);
        StoryDirector.BookTalk(StoryIds.Ottilie, hero);
        Assert.AreEqual(StoryStatus.Active, S().Status("h1"));
        // Fuhrbuch: Aufgabe mit Zielen.
        bool found = false;
        foreach (var line in StoryQuestUI.Lines(S()))
            if (line.Contains(Loc.T(StoryCatalog.Get("h1").TitleKey))) found = true;
        Assert.IsTrue(found, "h1 surfaces in the log");
        // Tracker: verfolgt, umschaltbar.
        Assert.IsTrue(StoryTracking.IsTracked(S(), "h1"));
        Assert.IsFalse(StoryTracking.Toggle(S(), "h1"), "toggle hides");
        Assert.IsTrue(StoryTracking.Toggle(S(), "h1"), "toggle tracks again");
        // Karten: Wacht-NPCs stehen, Trackertext nennt die Aufgabe.
        var world = new FakeWorld();
        world.npcs[StoryIds.Ottilie] = new Vector3(600f, 0f, 40f);
        world.npcs[StoryIds.Merle] = new Vector3(601f, 0f, 41f);
        world.npcs[StoryIds.Ilsabe] = new Vector3(602f, 0f, 42f);
        world.npcs[StoryIds.Jorin] = new Vector3(603f, 0f, 43f);
        world.pickups[StoryIds.Fuhrbrief] = new List<Vector3> { new Vector3(604f, 0f, 44f) };
        world.enemies[StoryIds.FenBrute] = new List<Vector3> { new Vector3(605f, 0f, 45f) };
        var marks = new List<QuestMark>();
        QuestDestinations.Collect(S(), world, new Vector3(600f, 0f, 44f), marks);
        Assert.IsTrue(marks.Exists(m => m.questId == "h1"), "h1 marks the map");
        string text = StoryDirector.TrackerText(S(), world, marks, new Vector3(600f, 0f, 44f));
        StringAssert.Contains(Loc.T("quest.h1.title"), text);
        // Plan-Prioritaet (TurnIn > Talk > Offer > Hint): Ilsabe bietet zuerst
        // das Lieferbuch an (Katalogreihenfolge, N1-Pass-through), Jorin hat
        // ohne Ofen nichts anhaengig und oeffnet seinen Laden.
        var ilsabe = StoryDialogs.Plan(S(), StoryIds.Ilsabe);
        Assert.AreEqual(StoryTalkKind.Offer, ilsabe.kind);
        Assert.AreEqual("n1", ilsabe.questId);
        var jorin = StoryDialogs.Plan(S(), StoryIds.Jorin);
        Assert.IsFalse(jorin.IsPending, "Jorin ohne Ofen: Laden statt Gespraech");
        // Alle neuen Wacht-Zeilen haben Katalogtext (Clips rendert dieser Commit mit).
        var keys = new List<string>();
        foreach (var id in new[] { "h10.offer", "h10.hint", "h10.done", "h10.talk.ottilie",
            "h10.talk.merle", "h10.talk.ilsabe", "h10.talk.jorin",
            "n6.offer", "n6.hint", "n6.done", "n7.offer", "n7.hint", "n7.done" })
        {
            var d = StoryDialogs.Get(id);
            Assert.IsNotNull(d, id);
            for (int i = 0; i < d.speakers.Length; i++) keys.Add(d.Key(i));
        }
        Assert.AreEqual(33, keys.Count);
        var missing = new List<string>();
        foreach (var k in keys) if (!Loc.Has(k)) missing.Add(k);
        Assert.IsEmpty(missing, string.Join("\n", missing));
        yield break;
    }

    [UnityTest]
    public IEnumerator H9_Anker_Unangetastet()
    {
        // S2 fasst Akt II nicht an: H9-Anker und Varianten bleiben wie in S1.
        var h9 = StoryCatalog.Get("h9");
        Assert.IsNotNull(h9);
        Assert.AreEqual("h8", h9.prereq);
        var merle = StoryDialogs.Get("h9.talk.merle");
        Assert.AreEqual(7, merle.speakers.Length);
        var ottilie = StoryDialogs.Get("h9.talk.ottilie");
        Assert.AreEqual(8, ottilie.speakers.Length);
        // Modell-Reservierung steht weiter: S3 haengt H10/N6/N7 an H9.
        Assert.IsNull(StoryCatalog.Get("h10"));
        Assert.IsNull(StoryCatalog.Get("n6"));
        Assert.IsNull(StoryCatalog.Get("n7"));
        yield break;
    }
}
