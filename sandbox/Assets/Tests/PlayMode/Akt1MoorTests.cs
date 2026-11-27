using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// cinderreach-akt1-vertiefung S2 (Moor/Beinhaus): R1 (zweiter Karren) und R2
/// (siebte Tafel) laufen ueber echte Fundstueck-Trigger (StoryPickup, dieselben
/// Positionen wie TownBuilder.SpawnStoryPickups) und echte Gespraeche
/// (StoryDirector.BookTalk). N8 aendert das H2-Angebot und den H14-Abschluss,
/// N9 aendert das H3-Angebot (Folgezeilen x|nK ueber StoryDialog.Visible).
///
/// Jede Quest (quest:h14, quest:h15, quest:n8, quest:n9) und jede neue
/// Platzierung (placement:DiabloZone:<Name>) ist explizit markiert.
/// </summary>
public class Akt1MoorTests
{
    GameObject hero;

    // Dieselben Positionen wie TownBuilder.SpawnStoryPickups (S2-Anteil).
    static readonly Vector3 Fetzen0Pos = new Vector3(-8.5f, 0.5f, 3.5f);
    static readonly Vector3 Fetzen1Pos = new Vector3(-4.0f, 0.5f, 8.0f);
    static readonly Vector3 BandPos = new Vector3(-5.0f, 0.5f, 4.5f);
    static Vector3 BruchPos(int i) => KilnLayout.OssuaryOffset + new Vector3(
        i == 1 ? 8.0f : -5.6f, 0.7f, i == 1 ? -3f : (i == 0 ? -6f : 0f));
    static Vector3 LichtPos => KilnLayout.OssuaryOffset + new Vector3(10.4f, 0.7f, -2f);

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

    /// <summary>Nimmt ein Angebot ueber den echten Gespraechspfad an.</summary>
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

    [UnityTest]
    public IEnumerator H14N8_ZweiterKarren_FetzenBandBruteToDone()
    // quest:h14 quest:n8
    // placement:DiabloZone:StoryPickup_fuhrfetzen_0
    // placement:DiabloZone:StoryPickup_fuhrfetzen_1
    // placement:DiabloZone:StoryPickup_siegelband_0
    {
        var fetzen0 = MakePickup(StoryIds.Fuhrfetzen, 0, Fetzen0Pos);
        var fetzen1 = MakePickup(StoryIds.Fuhrfetzen, 1, Fetzen1Pos);
        var band = MakePickup(StoryIds.Siegelband, 0, BandPos);
        try
        {
            Assert.IsTrue(S().Refresh(RunProfile.Act));
            AcceptViaTalk(StoryIds.Ottilie, "h1");
            AcceptViaTalk(StoryIds.Ottilie, "h14");
            // R1: das Band wartet, bis N8 angenommen ist (quest:h14 bleibt unberuehrt).
            TeleportNear(BandPos, 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.IsTrue(band.activeSelf, "early find waits instead of being lost");
            Assert.AreEqual(0, S().Count("n8", 0));
            AcceptViaTalk(StoryIds.Ottilie, "n8");
            // Zwei Fetzen am zweiten Karren ueber echte Trigger.
            TeleportNear(Fetzen0Pos, 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.AreEqual(1, S().Count("h14", 0));
            Assert.IsFalse(fetzen0.activeSelf, "counted find despawns");
            TeleportNear(Fetzen1Pos, 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.AreEqual(2, S().Count("h14", 0));
            Assert.IsFalse(fetzen1.activeSelf);
            // Band einsammeln, dann N8 vor H14 abgeben (volle Lesung in H14).
            DiabloTestFixture.TeleportHero(hero, BandPos + new Vector3(10f, 0f, 0f));
            Physics.SyncTransforms();
            yield return null;
            TeleportNear(BandPos, 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.AreEqual(1, S().Count("n8", 0));
            Assert.AreEqual(StoryStatus.Ready, S().Status("n8"));
            var turnInN8 = StoryDialogs.Plan(S(), StoryIds.Ottilie);
            Assert.AreEqual(StoryTalkKind.TurnIn, turnInN8.kind);
            Assert.AreEqual("n8", turnInN8.questId);
            Assert.IsNotNull(turnInN8.dialog, "n8.done dialog exists");
            StoryDirector.BookTalk(StoryIds.Ottilie, hero);
            Assert.AreEqual(StoryStatus.Done, S().Status("n8"));
            yield return RunDirector();
            Assert.AreEqual(100, hero.GetComponent<Wallet>().Gold);
            // Der Moorschlaeger zaehlt fuer H1 und H14 zugleich.
            StoryDirector.BookKill("The Fen Brute(Clone)");
            Assert.AreEqual(StoryStatus.Ready, S().Status("h14"));
            var turnInH14 = StoryDialogs.Plan(S(), StoryIds.Ottilie);
            Assert.AreEqual("h14", turnInH14.questId);
            Assert.IsNotNull(turnInH14.dialog, "h14.done dialog exists");
            // R1-Vollsatz: mit Vogts Band liest sich der Satz zu Ende.
            var done = StoryDialogs.Get("h14.done");
            Assert.IsTrue(StoryDialog.Visible(done.speakers[2], S().Flags), "n8 zeigt die dritte Abschlusszeile");
            StoryDirector.BookTalk(StoryIds.Ottilie, hero);
            Assert.AreEqual(StoryStatus.Done, S().Status("h14"));
            Assert.IsTrue(LogShowsDone("h14"), string.Join("\n", StoryQuestUI.Lines(S())));
            // N8-Folge: das H2-Angebot nennt das Siegel des Vogts.
            var h2offer = StoryDialogs.Get("h2.offer");
            Assert.IsTrue(StoryDialog.Visible(h2offer.speakers[4], S().Flags), "n8 aendert das H2-Angebot");
        }
        finally
        {
            Object.DestroyImmediate(fetzen0);
            Object.DestroyImmediate(fetzen1);
            Object.DestroyImmediate(band);
        }
        yield break;
    }

    [UnityTest]
    public IEnumerator H15N9_SiebteTafel_BruchLichtTalkToDone()
    // quest:h15 quest:n9
    // placement:DiabloZone:StoryPickup_tafelbruch_0
    // placement:DiabloZone:StoryPickup_tafelbruch_1
    // placement:DiabloZone:StoryPickup_tafelbruch_2
    // placement:DiabloZone:StoryPickup_grablicht_0
    {
        var bruch0 = MakePickup(StoryIds.Tafelbruch, 0, BruchPos(0));
        var bruch1 = MakePickup(StoryIds.Tafelbruch, 1, BruchPos(1));
        var bruch2 = MakePickup(StoryIds.Tafelbruch, 2, BruchPos(2));
        var licht = MakePickup(StoryIds.Grablicht, 0, LichtPos);
        try
        {
            Assert.IsTrue(S().Refresh(RunProfile.Act));
            AcceptViaTalk(StoryIds.Ottilie, "h1");
            Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Fuhrbrief));
            StoryDirector.BookKill("The Fen Brute(Clone)");
            var turnInH1 = StoryDialogs.Plan(S(), StoryIds.Ottilie);
            Assert.AreEqual("h1", turnInH1.questId);
            StoryDirector.BookTalk(StoryIds.Ottilie, hero);
            AcceptViaTalk(StoryIds.Ottilie, "h2");
            // Angebote in Katalogreihenfolge abarbeiten, bis H15 ansteht.
            AcceptViaTalk(StoryIds.Ottilie, "n2");
            AcceptViaTalk(StoryIds.Ottilie, "h14");
            AcceptViaTalk(StoryIds.Ottilie, "h15");
            // R2: drei Bruchstuecke ueber echte Trigger, Gespraech bleibt faellig.
            foreach (var pos in new[] { BruchPos(0), BruchPos(1), BruchPos(2) })
            {
                TeleportNear(pos, 0.2f);
                yield return DiabloTestFixture.WaitSeconds(0.5f);
            }
            Assert.AreEqual(3, S().Count("h15", 0));
            Assert.IsFalse(bruch0.activeSelf);
            Assert.IsFalse(bruch1.activeSelf);
            Assert.IsFalse(bruch2.activeSelf);
            var talk = StoryDialogs.Plan(S(), StoryIds.Ottilie);
            Assert.AreEqual(StoryTalkKind.Talk, talk.kind);
            Assert.AreEqual("h15", talk.questId);
            Assert.IsNotNull(talk.dialog, "h15.talk.ottilie dialog exists");
            StoryDirector.BookTalk(StoryIds.Ottilie, hero);
            Assert.AreEqual(StoryStatus.Done, S().Status("h15"));
            Assert.IsTrue(LogShowsDone("h15"), string.Join("\n", StoryQuestUI.Lines(S())));
            // N9 hinterher: Licht bergen, abgeben, H3-Angebot aendert sich.
            AcceptViaTalk(StoryIds.Ottilie, "n8");
            AcceptViaTalk(StoryIds.Ottilie, "n9");
            TeleportNear(licht.transform.position, 0.2f);
            yield return DiabloTestFixture.WaitSeconds(0.5f);
            Assert.AreEqual(1, S().Count("n9", 0));
            Assert.IsFalse(licht.activeSelf);
            var turnInN9 = StoryDialogs.Plan(S(), StoryIds.Ottilie);
            Assert.AreEqual("n9", turnInN9.questId);
            Assert.IsNotNull(turnInN9.dialog, "n9.done dialog exists");
            StoryDirector.BookTalk(StoryIds.Ottilie, hero);
            Assert.AreEqual(StoryStatus.Done, S().Status("n9"));
            yield return RunDirector();
            Assert.AreEqual(60, hero.GetComponent<Wallet>().Gold);
            Assert.IsTrue(LogShowsDone("n9"), string.Join("\n", StoryQuestUI.Lines(S())));
            var h3offer = StoryDialogs.Get("h3.offer");
            Assert.IsTrue(StoryDialog.Visible(h3offer.speakers[7], S().Flags), "n9 aendert das H3-Angebot");
        }
        finally
        {
            Object.DestroyImmediate(bruch0);
            Object.DestroyImmediate(bruch1);
            Object.DestroyImmediate(bruch2);
            Object.DestroyImmediate(licht);
        }
        yield break;
    }
}
