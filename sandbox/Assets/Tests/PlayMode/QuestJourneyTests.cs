using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// cinderreach-quest-interaktionen, PlayMode: Quest-Reisen.
///
/// Jede Quest (quest:h1..h9, quest:n1..n5, Vertiefung quest:h14..h17,
/// quest:n8..n11) laeuft vom echten Ausloeser
/// (<see cref="StoryDirector.BookTalk"/>, <see cref="StoryDirector.BookCollect"/>,
/// <see cref="StoryDirector.BookKill"/>, <see cref="StoryDirector.BookEnter"/> —
/// dieselben Einstiegspunkte, die NPC-Klick, Pickup-Trigger und Zonenwechsel
/// zur Laufzeit rufen) bis zum fuer Spieler sichtbaren Abschluss
/// (<see cref="StoryQuestUI.Lines"/>-Fuhrbuch, <see cref="StoryDialogs.Plan"/>.
/// Keine direkte <see cref="StoryState"/>-Mutation nach Teststart: ein
/// defekter Ausloeser wuerde sonst gruen bleiben. Belohnungsquests lassen den
/// echten <see cref="StoryDirector"/> laufen (Frames), statt die Vergabe
/// nachzubauen.
/// </summary>
public class QuestJourneyTests
{
    GameObject hero;

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
        hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = DiabloNetworkPlayer.HeroName;
        hero.transform.position = new Vector3(0f, 1f, 40f);
        hero.AddComponent<DiabloProgression>();
        hero.AddComponent<Equipment>();
        hero.AddComponent<Inventory>();
        hero.AddComponent<Wallet>();
        hero.AddComponent<PlayerHealth>();
    }

    [TearDown]
    public void TearDown()
    {
        DiabloTestFixture.CleanupTestScene(hero);
        RunProfile.Reset();
        Wallet.ResetRun();
        DiabloProgression.ResetRun();
        ZoneGate.ResetRun();
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    static StoryState S() => RunProfile.Story;

    /// <summary>Nimmt ein Angebot ueber den echten Gespraechspfad an.</summary>
    static void AcceptViaTalk(string npc, string questId)
    {
        var plan = StoryDialogs.Plan(S(), npc);
        Assert.AreEqual(StoryTalkKind.Offer, plan.kind, $"offer for {questId}");
        Assert.AreEqual(questId, plan.questId);
        StoryDirector.BookTalk(npc, GameObject.Find(DiabloNetworkPlayer.HeroName));
        Assert.AreEqual(StoryStatus.Active, S().Status(questId));
    }

    /// <summary>Gibt eine fertige Quest ueber den echten Gespraechspfad ab.</summary>
    static void TurnInViaTalk(string npc, string questId)
    {
        var plan = StoryDialogs.Plan(S(), npc);
        Assert.AreEqual(StoryTalkKind.TurnIn, plan.kind, $"turn-in for {questId}");
        Assert.AreEqual(questId, plan.questId);
        Assert.IsNotNull(plan.dialog, $"{questId}.done dialog exists");
        StoryDirector.BookTalk(npc, GameObject.Find(DiabloNetworkPlayer.HeroName));
        Assert.AreEqual(StoryStatus.Done, S().Status(questId));
    }

    /// <summary>Gibt eine neue Aufgabe ueber den echten Gespraechspfad ab (Dialoge kommen in S2/S3).</summary>
    static void TurnInNewViaTalk(string npc, string questId)
    {
        var plan = StoryDialogs.Plan(S(), npc);
        Assert.AreEqual(StoryTalkKind.TurnIn, plan.kind, $"turn-in for {questId}");
        Assert.AreEqual(questId, plan.questId);
        StoryDirector.BookTalk(npc, GameObject.Find(DiabloNetworkPlayer.HeroName));
        Assert.AreEqual(StoryStatus.Done, S().Status(questId));
    }

    static bool LogShowsDone(string questId)
    {
        string title = Loc.T(StoryCatalog.Get(questId).TitleKey);
        foreach (var line in StoryQuestUI.Lines(S()))
            if (line.Contains(title) && line.Contains("✓")) return true;
        return false;
    }

    static void PlayH1()
    {
        // quest:h1
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Ottilie, "h1");
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Fuhrbrief));
        StoryDirector.BookKill("The Fen Brute(Clone)");
        Assert.AreEqual(StoryStatus.Ready, S().Status("h1"));
        TurnInViaTalk(StoryIds.Ottilie, "h1");
    }

    static void PlayH2()
    {
        PlayH1();
        // quest:h2
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "h2");
        StoryDirector.BookEnter(StoryIds.ZoneOssuary);
        for (int i = 0; i < 4; i++) StoryDirector.BookKill("Bone Warden");
        Assert.AreEqual(StoryStatus.Active, S().Status("h2"));
        StoryDirector.BookKill("The Hollow Abbot");
        Assert.AreEqual(StoryStatus.Ready, S().Status("h2"));
        TurnInViaTalk(StoryIds.Ottilie, "h2");
    }

    static void PlayH3()
    {
        PlayH2();
        // quest:h3
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "h3");
        var talk = StoryDialogs.Plan(S(), StoryIds.Jorin);
        Assert.AreEqual(StoryTalkKind.Talk, talk.kind);
        StoryDirector.BookTalk(StoryIds.Jorin, GameObject.Find(DiabloNetworkPlayer.HeroName));
        StoryDirector.BookEnter(StoryIds.ZoneKiln);
        Assert.AreEqual(StoryStatus.Done, S().Status("h3"));
    }

    static void PlayH4()
    {
        PlayH3();
        // quest:h4
        S().Refresh(RunProfile.Act);
        Assert.AreEqual(StoryStatus.Active, S().Status("h4"), "h4 auto-accepts far from the giver");
        StoryDirector.BookKill("The Kilnmother(Clone)");
        Assert.AreEqual(StoryStatus.Ready, S().Status("h4"));
        TurnInViaTalk(StoryIds.Ottilie, "h4");
    }

    static void OpenActTwo()
    {
        Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
        Assert.IsTrue(RunProfile.Act.CompleteAct());
    }

    /// <summary>Laesst den echten Director zwei Frames laufen (Vergabe + Autosave).</summary>
    static IEnumerator RunDirector()
    {
        var go = GameObject.Find(DiabloNetworkPlayer.HeroName);
        if (go.GetComponent<StoryDirector>() == null) go.AddComponent<StoryDirector>();
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator H1_Journey_OfferToDoneShowsInLog() // quest:h1
    {
        PlayH1();
        Assert.IsTrue(LogShowsDone("h1"), string.Join("\n", StoryQuestUI.Lines(S())));
        Assert.AreEqual("h2", StoryDialogs.Plan(S(), StoryIds.Ottilie).questId);
        yield break;
    }

    [UnityTest]
    public IEnumerator H2_Journey_EnterWardensAbbotToDone() // quest:h2
    {
        PlayH2();
        Assert.IsTrue(LogShowsDone("h2"), string.Join("\n", StoryQuestUI.Lines(S())));
        Assert.AreEqual("h3", StoryDialogs.Plan(S(), StoryIds.Ottilie).questId);
        yield break;
    }

    [UnityTest]
    public IEnumerator H3_Journey_JorinTalkKilnEntryCompletes() // quest:h3
    {
        PlayH3();
        Assert.IsTrue(LogShowsDone("h3"), string.Join("\n", StoryQuestUI.Lines(S())));
        yield break;
    }

    [UnityTest]
    public IEnumerator H4_Journey_KilnmotherKillToDone() // quest:h4
    {
        PlayH4();
        Assert.IsTrue(LogShowsDone("h4"), string.Join("\n", StoryQuestUI.Lines(S())));
        var next = StoryDialogs.Plan(S(), StoryIds.Ottilie);
        Assert.AreEqual(StoryTalkKind.Offer, next.kind);
        Assert.AreEqual("h5", next.questId);
        yield break;
    }

    [UnityTest]
    public IEnumerator H5_Journey_IlsabeTalkToDone() // quest:h5
    {
        PlayH4();
        // quest:h5
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "h5");
        var talk = StoryDialogs.Plan(S(), StoryIds.Ilsabe);
        Assert.AreEqual(StoryTalkKind.Talk, talk.kind);
        Assert.AreEqual("h5.talk.ilsabe", talk.dialog.id);
        StoryDirector.BookTalk(StoryIds.Ilsabe, hero);
        Assert.AreEqual(StoryStatus.Ready, S().Status("h5"));
        TurnInViaTalk(StoryIds.Ottilie, "h5");
        Assert.IsTrue(LogShowsDone("h5"), string.Join("\n", StoryQuestUI.Lines(S())));
        yield break;
    }

    [UnityTest]
    public IEnumerator N1_Journey_LedgerToDoneAnd150Gold() // quest:n1
    {
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Ottilie, "h1");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ilsabe, "n1");
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Lieferbuch));
        Assert.AreEqual(StoryStatus.Ready, S().Status("n1"));
        TurnInViaTalk(StoryIds.Ilsabe, "n1");
        yield return RunDirector();
        Assert.AreEqual(150, hero.GetComponent<Wallet>().Gold);
        Assert.IsTrue(LogShowsDone("n1"), string.Join("\n", StoryQuestUI.Lines(S())));
        int gold = hero.GetComponent<Wallet>().Gold;
        yield return RunDirector();
        Assert.AreEqual(gold, hero.GetComponent<Wallet>().Gold, "reward stays one-shot");
    }

    [UnityTest]
    public IEnumerator N2_Journey_ThreeTabletsToDoneAndHelm() // quest:n2
    {
        PlayH1();
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "h2");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "n2");
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Grabtafel));
        Assert.AreEqual(StoryStatus.Active, S().Status("n2"));
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Grabtafel));
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Grabtafel));
        Assert.AreEqual(StoryStatus.Ready, S().Status("n2"));
        TurnInViaTalk(StoryIds.Ottilie, "n2");
        yield return RunDirector();
        var inv = hero.GetComponent<Inventory>();
        Assert.AreEqual(1, inv.Count);
        Assert.AreEqual(ItemSlot.Helmet, inv.Get(0).slot);
        Assert.IsTrue(LogShowsDone("n2"), string.Join("\n", StoryQuestUI.Lines(S())));
        yield return RunDirector();
        Assert.AreEqual(1, inv.Count, "reward stays one-shot");
    }

    [UnityTest]
    public IEnumerator N3_Journey_SixLancersToDoneAndMaterial() // quest:n3
    {
        Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Jorin, "n3");
        for (int i = 0; i < 6; i++) StoryDirector.BookKill("Ash Lancer_0");
        Assert.AreEqual(StoryStatus.Ready, S().Status("n3"));
        TurnInViaTalk(StoryIds.Jorin, "n3");
        yield return RunDirector();
        Assert.AreEqual(50, hero.GetComponent<Wallet>().Gold);
        Assert.AreEqual(3, RunProfile.SmithingMaterial);
        Assert.IsTrue(LogShowsDone("n3"), string.Join("\n", StoryQuestUI.Lines(S())));
        yield return RunDirector();
        Assert.AreEqual(3, RunProfile.SmithingMaterial, "reward stays one-shot");
    }

    // ---- Akt-I-Vertiefung (cinderreach-akt1-vertiefung S1): H14-H17 + N8-N11 ----
    // Die Dialoge (offer/hint/done) und Fundorte kommen in S2/S3; hier laeuft
    // jede neue Aufgabe ueber dieselben Einstiegspunkte (BookTalk/BookCollect/
    // BookKill) bis zum sichtbaren Abschluss im Fuhrbuch.

    [UnityTest]
    public IEnumerator H14_Journey_FetzenBruteToDone() // quest:h14
    {
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Ottilie, "h1");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "h14");
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Fuhrfetzen));
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Fuhrfetzen));
        StoryDirector.BookKill("The Fen Brute(Clone)");
        Assert.AreEqual(StoryStatus.Ready, S().Status("h14"));
        TurnInNewViaTalk(StoryIds.Ottilie, "h14");
        Assert.IsTrue(LogShowsDone("h14"), string.Join("\n", StoryQuestUI.Lines(S())));
        yield break;
    }

    [UnityTest]
    public IEnumerator N8_Journey_BandToDoneAnd100Gold() // quest:n8
    {
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Ottilie, "h1");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "h14");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "n8");
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Siegelband));
        Assert.AreEqual(StoryStatus.Ready, S().Status("n8"));
        TurnInNewViaTalk(StoryIds.Ottilie, "n8");
        yield return RunDirector();
        Assert.AreEqual(100, hero.GetComponent<Wallet>().Gold);
        Assert.IsTrue(LogShowsDone("n8"), string.Join("\n", StoryQuestUI.Lines(S())));
        yield return RunDirector();
        Assert.AreEqual(100, hero.GetComponent<Wallet>().Gold, "reward stays one-shot");
    }

    [UnityTest]
    public IEnumerator H15_Journey_BruchTalkToDone() // quest:h15
    {
        PlayH2();
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "h3");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "n2");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "h14");
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Fuhrfetzen));
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Fuhrfetzen));
        StoryDirector.BookKill("The Fen Brute(Clone)");
        TurnInNewViaTalk(StoryIds.Ottilie, "h14");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "h15");
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Tafelbruch));
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Tafelbruch));
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Tafelbruch));
        var talk = StoryDialogs.Plan(S(), StoryIds.Ottilie);
        Assert.AreEqual(StoryTalkKind.Talk, talk.kind);
        StoryDirector.BookTalk(StoryIds.Ottilie, hero);
        Assert.AreEqual(StoryStatus.Done, S().Status("h15"));
        Assert.IsTrue(LogShowsDone("h15"), string.Join("\n", StoryQuestUI.Lines(S())));
        yield break;
    }

    [UnityTest]
    public IEnumerator N9_Journey_LichtToDoneAnd60Gold() // quest:n9
    {
        PlayH2();
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "h3");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "n2");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "h14");
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Fuhrfetzen));
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Fuhrfetzen));
        StoryDirector.BookKill("The Fen Brute(Clone)");
        TurnInNewViaTalk(StoryIds.Ottilie, "h14");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "h15");
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Tafelbruch));
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Tafelbruch));
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Tafelbruch));
        StoryDirector.BookTalk(StoryIds.Ottilie, hero);
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "n8");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "n9");
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Grablicht));
        Assert.AreEqual(StoryStatus.Ready, S().Status("n9"));
        TurnInNewViaTalk(StoryIds.Ottilie, "n9");
        yield return RunDirector();
        Assert.AreEqual(60, hero.GetComponent<Wallet>().Gold);
        Assert.IsTrue(LogShowsDone("n9"), string.Join("\n", StoryQuestUI.Lines(S())));
        yield return RunDirector();
        Assert.AreEqual(60, hero.GetComponent<Wallet>().Gold, "reward stays one-shot");
    }

    [UnityTest]
    public IEnumerator H16_Journey_ListeLancersToDone() // quest:h16
    {
        PlayH2();
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "h3");
        var talkH3 = StoryDialogs.Plan(S(), StoryIds.Jorin);
        Assert.AreEqual(StoryTalkKind.Talk, talkH3.kind);
        StoryDirector.BookTalk(StoryIds.Jorin, hero);
        StoryDirector.BookEnter(StoryIds.ZoneKiln);
        Assert.AreEqual(StoryStatus.Done, S().Status("h3"));
        Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Jorin, "n3");
        for (int i = 0; i < 6; i++) StoryDirector.BookKill("Ash Lancer_0");
        TurnInViaTalk(StoryIds.Jorin, "n3");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Jorin, "h16");
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Fuhrliste));
        StoryDirector.BookKill("Ash Lancer_0");
        StoryDirector.BookKill("Ash Lancer_0");
        Assert.AreEqual(StoryStatus.Ready, S().Status("h16"));
        TurnInNewViaTalk(StoryIds.Jorin, "h16");
        Assert.IsTrue(LogShowsDone("h16"), string.Join("\n", StoryQuestUI.Lines(S())));
        yield break;
    }

    [UnityTest]
    public IEnumerator N10_Journey_ThrallsToDoneAnd80Gold() // quest:n10
    {
        Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Jorin, "n3");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Jorin, "n10");
        for (int i = 0; i < 4; i++) StoryDirector.BookKill("Cinder Thrall_0");
        Assert.AreEqual(StoryStatus.Ready, S().Status("n10"));
        TurnInNewViaTalk(StoryIds.Jorin, "n10");
        yield return RunDirector();
        Assert.AreEqual(80, hero.GetComponent<Wallet>().Gold);
        Assert.IsTrue(LogShowsDone("n10"), string.Join("\n", StoryQuestUI.Lines(S())));
        yield return RunDirector();
        Assert.AreEqual(80, hero.GetComponent<Wallet>().Gold, "reward stays one-shot");
    }

    [UnityTest]
    public IEnumerator N11_Journey_SeiteToDoneAnd270Gold() // quest:n11
    {
        Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Ottilie, "h3");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ilsabe, "n1");
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Lieferbuch));
        TurnInViaTalk(StoryIds.Ilsabe, "n1");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ilsabe, "n11");
        Assert.IsTrue(StoryDirector.BookCollect(StoryIds.Russseite));
        Assert.AreEqual(StoryStatus.Ready, S().Status("n11"));
        TurnInNewViaTalk(StoryIds.Ilsabe, "n11");
        yield return RunDirector();
        Assert.AreEqual(270, hero.GetComponent<Wallet>().Gold, "n1 (150) + n11 (120)");
        Assert.IsTrue(LogShowsDone("n11"), string.Join("\n", StoryQuestUI.Lines(S())));
        yield return RunDirector();
        Assert.AreEqual(270, hero.GetComponent<Wallet>().Gold, "reward stays one-shot");
    }

    [UnityTest]
    public IEnumerator H17_Journey_TwoTalksToDone() // quest:h17
    {
        Assert.IsTrue(RunProfile.Act.Unlock(ActProgress.Kiln));
        Assert.IsTrue(RunProfile.Act.CompleteAct());
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Ottilie, "h5");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "n2");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ottilie, "h17");
        StoryDirector.BookTalk(StoryIds.Ottilie, hero);
        Assert.AreEqual(StoryStatus.Active, S().Status("h17"), "ein Gespräch reicht nicht (R3)");
        StoryDirector.BookTalk(StoryIds.Ilsabe, hero);
        Assert.AreEqual(StoryStatus.Done, S().Status("h17"));
        Assert.IsTrue(LogShowsDone("h17"), string.Join("\n", StoryQuestUI.Lines(S())));
        yield break;
    }

    [UnityTest]
    public IEnumerator H6_Journey_BarrensEntryCompletes() // quest:h6
    {
        PlayH4();
        OpenActTwo();
        // quest:h6
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Merle, "h6");
        StoryDirector.BookEnter(StoryIds.ZoneBarrens);
        Assert.AreEqual(StoryStatus.Done, S().Status("h6"));
        Assert.IsTrue(LogShowsDone("h6"), string.Join("\n", StoryQuestUI.Lines(S())));
        Assert.AreEqual(StoryStatus.Active, S().Status("h7"));
        yield break;
    }

    [UnityTest]
    public IEnumerator H7_Journey_ThreeAugursCompletes() // quest:h7
    {
        PlayH4();
        OpenActTwo();
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Merle, "h6");
        StoryDirector.BookEnter(StoryIds.ZoneBarrens);
        // quest:h7
        Assert.AreEqual(StoryStatus.Active, S().Status("h7"));
        for (int i = 0; i < 3; i++) StoryDirector.BookKill("Dune Augur");
        Assert.AreEqual(StoryStatus.Done, S().Status("h7"));
        Assert.IsTrue(LogShowsDone("h7"), string.Join("\n", StoryQuestUI.Lines(S())));
        Assert.AreEqual(StoryStatus.Active, S().Status("h8"));
        yield break;
    }

    [UnityTest]
    public IEnumerator H8_Journey_GlasswrightCompletes() // quest:h8
    {
        PlayH4();
        OpenActTwo();
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Merle, "h6");
        StoryDirector.BookEnter(StoryIds.ZoneBarrens);
        for (int i = 0; i < 3; i++) StoryDirector.BookKill("Dune Augur");
        // quest:h8
        Assert.AreEqual(StoryStatus.Active, S().Status("h8"));
        StoryDirector.BookKill("The Glasswright");
        Assert.AreEqual(StoryStatus.Done, S().Status("h8"));
        Assert.IsTrue(LogShowsDone("h8"), string.Join("\n", StoryQuestUI.Lines(S())));
        Assert.AreEqual(StoryStatus.Active, S().Status("h9"));
        yield break;
    }

    [UnityTest]
    public IEnumerator H9_Journey_TwoTalksComplete() // quest:h9
    {
        PlayH4();
        OpenActTwo();
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Merle, "h6");
        StoryDirector.BookEnter(StoryIds.ZoneBarrens);
        for (int i = 0; i < 3; i++) StoryDirector.BookKill("Dune Augur");
        StoryDirector.BookKill("The Glasswright");
        // quest:h9
        Assert.AreEqual(StoryStatus.Active, S().Status("h9"));
        var first = StoryDialogs.Plan(S(), StoryIds.Merle);
        Assert.AreEqual(StoryTalkKind.Talk, first.kind);
        StoryDirector.BookTalk(StoryIds.Merle, hero);
        var second = StoryDialogs.Plan(S(), StoryIds.Ottilie);
        Assert.AreEqual(StoryTalkKind.Talk, second.kind);
        StoryDirector.BookTalk(StoryIds.Ottilie, hero);
        Assert.AreEqual(StoryStatus.Done, S().Status("h9"));
        Assert.IsTrue(LogShowsDone("h9"), string.Join("\n", StoryQuestUI.Lines(S())));
        yield break;
    }

    [UnityTest]
    public IEnumerator N4_Journey_EliteToDoneAnd250Gold() // quest:n4
    {
        PlayH4();
        OpenActTwo();
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Merle, "h6");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Merle, "n4");
        StoryDirector.BookKill("Glass Elite");
        Assert.AreEqual(StoryStatus.Ready, S().Status("n4"));
        TurnInViaTalk(StoryIds.Merle, "n4");
        yield return RunDirector();
        Assert.AreEqual(250, hero.GetComponent<Wallet>().Gold);
        Assert.IsTrue(LogShowsDone("n4"), string.Join("\n", StoryQuestUI.Lines(S())));
        yield return RunDirector();
        Assert.AreEqual(250, hero.GetComponent<Wallet>().Gold, "reward stays one-shot");
    }

    [UnityTest]
    public IEnumerator N5_Journey_FourAugursToDoneAndCirclet() // quest:n5
    {
        PlayH4();
        OpenActTwo();
        Assert.IsTrue(S().Refresh(RunProfile.Act));
        AcceptViaTalk(StoryIds.Ottilie, "h5");
        StoryDirector.BookTalk(StoryIds.Ilsabe, hero);
        TurnInViaTalk(StoryIds.Ottilie, "h5");
        S().Refresh(RunProfile.Act);
        AcceptViaTalk(StoryIds.Ilsabe, "n1");
        StoryDirector.BookCollect(StoryIds.Lieferbuch);
        TurnInViaTalk(StoryIds.Ilsabe, "n1");
        AcceptViaTalk(StoryIds.Ilsabe, "n5");
        for (int i = 0; i < 4; i++) StoryDirector.BookKill("Dune Augur_1");
        Assert.AreEqual(StoryStatus.Ready, S().Status("n5"));
        TurnInViaTalk(StoryIds.Ilsabe, "n5");
        yield return RunDirector();
        var inv = hero.GetComponent<Inventory>();
        Assert.AreEqual(1, inv.Count);
        Assert.AreEqual("Seer's Circlet", inv.Get(0).uniqueId);
        Assert.IsTrue(LogShowsDone("n5"), string.Join("\n", StoryQuestUI.Lines(S())));
        yield return RunDirector();
        Assert.AreEqual(1, inv.Count, "reward stays one-shot");
    }
}
