using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// cinderreach-quest-interaktionen, EditMode: Questregeln und Speicherung.
///
/// Deckt jede Quest (quest:h1..h9, quest:n1..n5, Vertiefung quest:h14..h17,
/// quest:n8..n11) ueber die reine
/// <see cref="StoryState"/>-Logik ab: vorgesehener Verlauf, gesperrte Schritte
/// (Voraussetzung, Akt-Gate, falsches Ziel), einmalige Belohnungen ueber den
/// produktiven <see cref="StoryDirector"/>-Vergabepfad (private
/// GrantRewards, per Reflection gerufen — kein Nachbau) und die
/// Wiederherstellung persistenter Zwischen- und Endzustaende nach Speichern
/// und Laden (ToSave/FromSave plus produktiver <see cref="SaveService"/>-
/// Dateipfad in eine frische Sitzung).
/// </summary>
public class QuestRuleTests
{
    GameObject hero;
    string savePath;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        Wallet.ResetRun();
        savePath = Path.Combine(Path.GetTempPath(), $"cinderreach-quest-{System.Guid.NewGuid():N}.json");
        hero = new GameObject("Player");
        hero.AddComponent<Equipment>();
        hero.AddComponent<Inventory>();
        hero.AddComponent<Wallet>();
        hero.AddComponent<DiabloProgression>();
    }

    [TearDown]
    public void TearDown()
    {
        if (hero != null) Object.DestroyImmediate(hero);
        Wallet.ResetRun();
        RunProfile.Reset();
        foreach (var f in new[] { savePath, savePath + ".tmp", savePath + ".corrupt" })
            if (f != null && File.Exists(f)) File.Delete(f);
    }

    static StoryState S() => RunProfile.Story;
    static ActProgress Act() => RunProfile.Act;

    static void TurnIn(string id)
    {
        Assert.IsTrue(S().TurnIn(id), $"TurnIn({id})");
    }

    /// <summary>Produktiver Vergabepfad (StoryDirector.GrantRewards, privat).</summary>
    static void GrantRewards(GameObject heroGo)
    {
        var director = heroGo.GetComponent<StoryDirector>();
        if (director == null) director = heroGo.AddComponent<StoryDirector>();
        var m = typeof(StoryDirector).GetMethod("GrantRewards",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(m, "StoryDirector.GrantRewards must exist");
        m.Invoke(director, null);
    }

    static void CompleteH1()
    {
        // quest:h1
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h1"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Fuhrbrief));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.FenBrute));
        Assert.AreEqual(StoryStatus.Ready, S().Status("h1"));
        TurnIn("h1");
    }

    static void CompleteH2()
    {
        // quest:h1
        CompleteH1();
        // quest:h2
        S().Refresh(Act()); // h2/n1 stehen teils schon offen: kein Umschalten nötig
        Assert.IsTrue(S().Accept("h2"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Enter, StoryIds.ZoneOssuary));
        for (int i = 0; i < 4; i++)
            Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.BoneWarden));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.Abbot));
        Assert.AreEqual(StoryStatus.Ready, S().Status("h2"));
        TurnIn("h2");
    }

    static void CompleteH3()
    {
        // quest:h2
        CompleteH2();
        // quest:h3
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h3"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Talk, StoryIds.Jorin));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Enter, StoryIds.ZoneKiln));
        Assert.AreEqual(StoryStatus.Done, S().Status("h3"), "h3 has no turn-in");
    }

    static void CompleteH4()
    {
        // quest:h3
        CompleteH3();
        // quest:h4
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Active, S().Status("h4"), "h4 auto-accepts");
        Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.Kilnmother));
        Assert.AreEqual(StoryStatus.Ready, S().Status("h4"));
        TurnIn("h4");
    }

    static void OpenActTwo()
    {
        Assert.IsTrue(Act().Unlock(ActProgress.Kiln));
        Assert.IsTrue(Act().CompleteAct());
    }

    // ---- vorgesehene Verlaeufe ----

    [Test]
    public void H1_OfferCollectKillTurnIn_Completes() // quest:h1
    {
        CompleteH1();
        Assert.AreEqual(StoryStatus.Done, S().Status("h1"));
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Available, S().Status("h2"));
    }

    [Test]
    public void H2_EnterWardensAbbotTurnIn_Completes() // quest:h2
    {
        CompleteH2();
        Assert.AreEqual(StoryStatus.Done, S().Status("h2"));
    }

    [Test]
    public void H3_TalkJorinEnterKiln_CompletesWithoutTurnIn() // quest:h3
    {
        CompleteH3();
        Assert.IsFalse(S().TurnIn("h3"), "no turn-in without Ready");
    }

    [Test]
    public void H4_AutoAcceptKillKilnmotherTurnIn_Completes() // quest:h4
    {
        CompleteH4();
        Assert.AreEqual(StoryStatus.Done, S().Status("h4"));
    }

    [Test]
    public void H5_TalkIlsabeTurnIn_Completes() // quest:h5
    {
        // quest:h4
        CompleteH4();
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h5"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Talk, StoryIds.Ilsabe));
        Assert.AreEqual(StoryStatus.Ready, S().Status("h5"));
        TurnIn("h5");
        Assert.AreEqual(StoryStatus.Done, S().Status("h5"));
    }

    [Test]
    public void N1_CollectLedgerTurnIn_Completes() // quest:n1
    {
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h1"));
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Available, S().Status("n1"), "n1 opens with h1 active-or-later");
        Assert.IsTrue(S().Accept("n1"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Lieferbuch));
        Assert.AreEqual(StoryStatus.Ready, S().Status("n1"));
        TurnIn("n1");
        Assert.AreEqual(StoryStatus.Done, S().Status("n1"));
    }

    [Test]
    public void N2_CollectThreeTabletsTurnIn_Completes() // quest:n2
    {
        CompleteH1();
        S().Refresh(Act());
        Assert.IsTrue(S().Accept("h2"));
        S().Refresh(Act());
        Assert.IsTrue(S().Accept("n2"));
        Assert.AreEqual(StoryStatus.Active, S().Status("n2"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Grabtafel));
        Assert.AreEqual(StoryStatus.Active, S().Status("n2"), "1/3 is not done");
        Assert.AreEqual(1, S().Count("n2", 0));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Grabtafel, 2));
        Assert.AreEqual(StoryStatus.Ready, S().Status("n2"));
        TurnIn("n2");
        Assert.AreEqual(StoryStatus.Done, S().Status("n2"));
    }

    [Test]
    public void N3_SixLancersAfterKilnOpen_Completes() // quest:n3
    {
        Assert.IsTrue(Act().Unlock(ActProgress.Kiln));
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Available, S().Status("n3"));
        Assert.IsTrue(S().Accept("n3"));
        for (int i = 0; i < 6; i++)
            Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.AshLancer));
        Assert.AreEqual(StoryStatus.Ready, S().Status("n3"));
        TurnIn("n3");
        Assert.AreEqual(StoryStatus.Done, S().Status("n3"));
    }

    // ---- Akt-I-Vertiefung (cinderreach-akt1-vertiefung S1): H14-H17 + N8-N11 ----

    [Test]
    public void NeueAngebote_StellenAlteErstangeboteNichtUm()
    {
        var s = S();
        Assert.IsTrue(s.Refresh(Act()));
        Assert.AreEqual("h1", StoryDialogs.Plan(s, StoryIds.Ottilie).questId);
        Assert.IsTrue(s.Accept("h1"));
        Assert.IsTrue(s.Apply(StoryObjectiveType.Collect, StoryIds.Fuhrbrief));
        Assert.IsTrue(s.Apply(StoryObjectiveType.Kill, StoryIds.FenBrute));
        s.TurnIn("h1"); // quest:h14 quest:n8 stehen bereit, h2 bleibt erstes Angebot
        Assert.IsTrue(s.Refresh(Act()));
        Assert.AreEqual("h2", StoryDialogs.Plan(s, StoryIds.Ottilie).questId);
        Assert.IsTrue(S().Accept("h2"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Enter, StoryIds.ZoneOssuary));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.BoneWarden, 4));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.Abbot));
        TurnIn("h2"); // quest:h15 quest:n9 stehen bereit, h3 bleibt erstes Angebot
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual("h3", StoryDialogs.Plan(s, StoryIds.Ottilie).questId);
    }

    [Test]
    public void H14_FetzenBruteTurnIn_Completes() // quest:h14
    {
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h1"));
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Available, S().Status("h14"));
        Assert.IsTrue(S().Accept("h14"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Fuhrfetzen));
        Assert.AreEqual(StoryStatus.Active, S().Status("h14"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Fuhrfetzen));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.FenBrute));
        Assert.AreEqual(StoryStatus.Ready, S().Status("h14"));
        TurnIn("h14");
        Assert.AreEqual(StoryStatus.Done, S().Status("h14"));
    }

    [Test]
    public void N8_BandTurnIn_Completes() // quest:n8
    {
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h1"));
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Available, S().Status("n8"));
        Assert.IsTrue(S().Accept("n8"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Siegelband));
        Assert.AreEqual(StoryStatus.Ready, S().Status("n8"));
        TurnIn("n8");
        Assert.AreEqual(StoryStatus.Done, S().Status("n8"));
    }

    [Test]
    public void H15_BruchTalk_CompletesWithoutTurnIn() // quest:h15
    {
        CompleteH1();
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h2"));
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Available, S().Status("h15"));
        Assert.IsTrue(S().Accept("h15"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Tafelbruch, 3));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Talk, StoryIds.Ottilie));
        Assert.AreEqual(StoryStatus.Done, S().Status("h15"), "h15 has no turn-in");
        Assert.IsFalse(S().TurnIn("h15"), "no turn-in without Ready");
    }

    [Test]
    public void N9_LichtTurnIn_Completes() // quest:n9
    {
        CompleteH1();
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h2"));
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Available, S().Status("n9"));
        Assert.IsTrue(S().Accept("n9"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Grablicht));
        Assert.AreEqual(StoryStatus.Ready, S().Status("n9"));
        TurnIn("n9");
        Assert.AreEqual(StoryStatus.Done, S().Status("n9"));
    }

    // ---- Akt-II-Vertiefung (cinderreach-akt2-vertiefung S2): Brache ----

    [Test]
    public void H18_SehglasTalk_CompletesWithoutTurnIn() // quest:h18
    {
        CompleteH4();
        OpenActTwo();
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h6"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Enter, StoryIds.ZoneBarrens));
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Active, S().Status("h7"), "h7 auto-accepts");
        Assert.AreEqual(StoryStatus.Active, S().Status("h18"), "h18 auto-accepts ab h7-aktiv");
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Sehglas, 2));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Talk, StoryIds.Merle));
        Assert.AreEqual(StoryStatus.Done, S().Status("h18"), "h18 has no turn-in");
        Assert.IsFalse(S().TurnIn("h18"), "no turn-in without Ready");
    }

    [Test]
    public void N12_NeedsN4_Reward150Once() // quest:n12
    {
        CompleteH4();
        OpenActTwo();
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h6"));
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Locked, S().Status("n12"), "ohne N4 gesperrt");
        Assert.IsFalse(S().Accept("n12"));
        Assert.IsTrue(S().Accept("n4"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.GlassElite));
        TurnIn("n4");
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Available, S().Status("n12"));
        Assert.IsTrue(S().Accept("n12"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Speichenrad));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Talk, StoryIds.Merle));
        Assert.AreEqual(StoryStatus.Ready, S().Status("n12"));
        var q = StoryCatalog.Get("n12");
        Assert.AreEqual(StoryReward.Gold, q.reward);
        Assert.AreEqual(150, q.rewardGold);
        TurnIn("n12");
        var wallet = hero.GetComponent<Wallet>();
        GrantRewards(hero);
        Assert.AreEqual(400, wallet.Gold, "n4 (250) + n12 (150)");
        GrantRewards(hero);
        Assert.AreEqual(400, wallet.Gold, "second grant is a no-op");
    }

    [Test]
    public void R6_PfadeUnabhaengig_MerleVsIlsabe() // quest:h18 quest:n13
    {
        CompleteH4();
        OpenActTwo();
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h5"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Talk, StoryIds.Ilsabe));
        TurnIn("h5");
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h6"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Enter, StoryIds.ZoneBarrens));
        S().Refresh(Act()); // Kette oeffnen
        Assert.AreEqual(StoryStatus.Active, S().Status("h18"), "h18 auto-accepts ab h7-aktiv");
        S().Refresh(Act()); // Kette oeffnen (ggf. no-op)
        Assert.IsTrue(S().Accept("n5"));
        for (int i = 0; i < 4; i++)
            Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.DuneAugur));
        TurnIn("n5");
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("n13"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Sehglas));
        TurnIn("n13");
        Assert.AreEqual(StoryStatus.Done, S().Status("n13"));
        // R6: Ilsabes Glas berührt Merles Aufgabe nicht — und umgekehrt.
        Assert.AreNotEqual(StoryStatus.Done, S().Status("h18"));
        Assert.AreEqual(1, S().Count("h18", 0), "Buchungen zählen für jede aktive Aufgabe");
    }

    [Test]
    public void Reward_N13_GoldOnce() // quest:n13
    {
        var q = StoryCatalog.Get("n13");
        Assert.AreEqual(StoryReward.Gold, q.reward);
        Assert.AreEqual(200, q.rewardGold);
        CompleteH4();
        OpenActTwo();
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h5"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Talk, StoryIds.Ilsabe));
        TurnIn("h5");
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("n5"));
        for (int i = 0; i < 4; i++)
            Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.DuneAugur));
        TurnIn("n5");
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("n13"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Sehglas));
        TurnIn("n13");
        var wallet = hero.GetComponent<Wallet>();
        GrantRewards(hero);
        Assert.AreEqual(200, wallet.Gold);
        GrantRewards(hero);
        Assert.AreEqual(200, wallet.Gold, "second grant is a no-op");
    }

    // ---- Akt-I-Vertiefung (cinderreach-akt1-vertiefung S2): Moor/Beinhaus-Dialoge ----

    [Test]
    public void MoorDialoge_AngebotUndAbschlussHabenSkript() // quest:h14 quest:n8
    {
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h1"));
        Assert.IsTrue(S().Refresh(Act()));
        var offer = StoryDialogs.Plan(S(), StoryIds.Ottilie);
        Assert.AreEqual(StoryTalkKind.Offer, offer.kind);
        Assert.AreEqual("h14", offer.questId);
        Assert.AreEqual(StoryDialogs.Get("h14.offer"), offer.dialog);
        Assert.IsTrue(S().Accept("h14"));
        var offerN8 = StoryDialogs.Plan(S(), StoryIds.Ottilie);
        Assert.AreEqual(StoryTalkKind.Offer, offerN8.kind);
        Assert.AreEqual("n8", offerN8.questId);
        Assert.AreEqual(StoryDialogs.Get("n8.offer"), offerN8.dialog);
        Assert.IsTrue(S().Accept("n8"));
        // Hinweise existieren als Rueckfall (Plan zeigt die frueheste aktive Quest).
        Assert.IsNotNull(StoryDialogs.Get("h14.hint"));
        Assert.IsNotNull(StoryDialogs.Get("n8.hint"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Fuhrfetzen, 2));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.FenBrute));
        var done = StoryDialogs.Plan(S(), StoryIds.Ottilie);
        Assert.AreEqual(StoryTalkKind.TurnIn, done.kind);
        Assert.AreEqual("h14", done.questId);
        Assert.AreEqual(StoryDialogs.Get("h14.done"), done.dialog);
        TurnIn("h14");
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Siegelband));
        var doneN8 = StoryDialogs.Plan(S(), StoryIds.Ottilie);
        Assert.AreEqual(StoryTalkKind.TurnIn, doneN8.kind);
        Assert.AreEqual("n8", doneN8.questId);
        Assert.AreEqual(StoryDialogs.Get("n8.done"), doneN8.dialog);
    }

    [Test]
    public void MoorFolgezeile_N8AendertH2AngebotUndH14Abschluss() // quest:h14 quest:n8
    {
        var s = S();
        Assert.IsTrue(s.Refresh(Act()));
        Assert.IsTrue(s.Accept("h1"));
        Assert.IsTrue(s.Refresh(Act()));
        Assert.IsTrue(s.Accept("h14"));
        Assert.IsTrue(s.Accept("n8"));
        Assert.IsTrue(s.Apply(StoryObjectiveType.Collect, StoryIds.Siegelband));
        TurnIn("n8");
        s.Flags.Add("n8"); // StoryDirector.SyncDoneFlags im Spiel
        var h2 = StoryDialogs.Get("h2.offer");
        Assert.AreEqual(5, h2.speakers.Length);
        Assert.IsTrue(StoryDialog.Visible(h2.speakers[4], s.Flags), "n8 aendert H2-Angebot");
        Assert.IsFalse(StoryDialog.Visible(h2.speakers[4], new HashSet<string>()));
        // R1: der H14-Abschluss liest sich erst mit Vogts Band zu Ende.
        Assert.IsTrue(s.Apply(StoryObjectiveType.Collect, StoryIds.Fuhrfetzen, 2));
        Assert.IsTrue(s.Apply(StoryObjectiveType.Kill, StoryIds.FenBrute));
        var done = StoryDialogs.Plan(s, StoryIds.Ottilie);
        Assert.AreEqual("h14", done.questId);
        var h14done = StoryDialogs.Get("h14.done");
        Assert.AreEqual(3, h14done.speakers.Length);
        Assert.IsTrue(StoryDialog.Visible(h14done.speakers[2], s.Flags), "n8 vollendet R1");
    }

    [Test]
    public void BeinhausFolgezeile_N9AendertH3Angebot() // quest:h15 quest:n9
    {
        CompleteH1();
        var s = S();
        Assert.IsTrue(s.Refresh(Act()));
        Assert.IsTrue(s.Accept("h2"));
        Assert.IsTrue(s.Refresh(Act()));
        Assert.IsTrue(s.Accept("h15"));
        Assert.IsTrue(s.Accept("n9"));
        Assert.IsTrue(s.Apply(StoryObjectiveType.Collect, StoryIds.Grablicht));
        TurnIn("n9");
        s.Flags.Add("n9"); // StoryDirector.SyncDoneFlags im Spiel
        var h3 = StoryDialogs.Get("h3.offer");
        Assert.AreEqual(8, h3.speakers.Length);
        Assert.IsTrue(StoryDialog.Visible(h3.speakers[7], s.Flags), "n9 aendert H3-Angebot");
        Assert.IsFalse(StoryDialog.Visible(h3.speakers[7], new HashSet<string>()));
        // R2: drei Brueche, dann das Gespraech mit Ottilie.
        Assert.IsTrue(s.Apply(StoryObjectiveType.Collect, StoryIds.Tafelbruch, 3));
        var talk = StoryDialogs.Plan(s, StoryIds.Ottilie);
        Assert.AreEqual(StoryTalkKind.Talk, talk.kind);
        Assert.AreEqual("h15", talk.questId);
        Assert.AreEqual(StoryDialogs.Get("h15.talk.ottilie"), talk.dialog);
    }

    [Test]
    public void H16_ListeLancersTurnIn_Completes() // quest:h16
    {
        CompleteH2();
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h3"));
        Assert.IsTrue(Act().Unlock(ActProgress.Kiln));
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Available, S().Status("h16"));
        Assert.IsTrue(S().Accept("h16"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Fuhrliste));
        Assert.AreEqual(StoryStatus.Active, S().Status("h16"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.AshLancer, 2));
        Assert.AreEqual(StoryStatus.Ready, S().Status("h16"));
        TurnIn("h16");
        Assert.AreEqual(StoryStatus.Done, S().Status("h16"));
    }

    [Test]
    public void N10_ThrallsTurnIn_Completes() // quest:n10
    {
        Assert.IsTrue(Act().Unlock(ActProgress.Kiln));
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Available, S().Status("n10"));
        Assert.IsTrue(S().Accept("n10"));
        for (int i = 0; i < 4; i++)
            Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.CinderThrall));
        Assert.AreEqual(StoryStatus.Ready, S().Status("n10"));
        TurnIn("n10");
        Assert.AreEqual(StoryStatus.Done, S().Status("n10"));
    }

    [Test]
    public void N11_SeiteTurnIn_Completes() // quest:n11
    {
        CompleteH2();
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h3"));
        Assert.IsTrue(Act().Unlock(ActProgress.Kiln));
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Available, S().Status("n11"));
        Assert.IsTrue(S().Accept("n11"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Russseite));
        Assert.AreEqual(StoryStatus.Ready, S().Status("n11"));
        TurnIn("n11");
        Assert.AreEqual(StoryStatus.Done, S().Status("n11"));
    }

    [Test]
    public void H17_TwoTalks_CompletesWithoutTurnIn() // quest:h17
    {
        CompleteH4();
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Available, S().Status("h17"));
        Assert.IsTrue(S().Accept("h17"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Talk, StoryIds.Ilsabe));
        Assert.AreEqual(StoryStatus.Active, S().Status("h17"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Talk, StoryIds.Ottilie));
        Assert.AreEqual(StoryStatus.Done, S().Status("h17"), "h17 has no turn-in");
    }

    [Test]
    public void H6_EnterBarrens_CompletesWithoutTurnIn() // quest:h6
    {
        CompleteH4();
        OpenActTwo();
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Available, S().Status("h6"));
        Assert.IsTrue(S().Accept("h6"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Enter, StoryIds.ZoneBarrens));
        Assert.AreEqual(StoryStatus.Done, S().Status("h6"), "h6 has no turn-in");
        S().Refresh(Act()); // Kette oeffnen (Produktivpfad: BookEnter ruft Refresh)
    }

    [Test]
    public void H7_ThreeAugurs_CompletesWithoutTurnIn() // quest:h7
    {
        // quest:h6
        CompleteH4();
        OpenActTwo();
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h6"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Enter, StoryIds.ZoneBarrens));
        S().Refresh(Act()); // Kette oeffnen (Produktivpfad: BookEnter ruft Refresh)
        Assert.AreEqual(StoryStatus.Active, S().Status("h7"), "h7 auto-accepts");
        for (int i = 0; i < 3; i++)
            Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.DuneAugur));
        Assert.AreEqual(StoryStatus.Done, S().Status("h7"), "h7 has no turn-in");
    }

    [Test]
    public void H8_Glasswright_CompletesWithoutTurnIn() // quest:h8
    {
        // quest:h7
        H7_ThreeAugurs_CompletesWithoutTurnIn();
        S().Refresh(Act()); // Kette oeffnen
        Assert.AreEqual(StoryStatus.Active, S().Status("h8"), "h8 auto-accepts");
        Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.Glasswright));
        Assert.AreEqual(StoryStatus.Done, S().Status("h8"), "h8 has no turn-in");
    }

    [Test]
    public void H9_TwoTalks_CompletesWithoutTurnIn() // quest:h9
    {
        // quest:h8
        H8_Glasswright_CompletesWithoutTurnIn();
        S().Refresh(Act()); // Kette oeffnen
        Assert.AreEqual(StoryStatus.Active, S().Status("h9"), "h9 auto-accepts");
        Assert.IsTrue(S().Apply(StoryObjectiveType.Talk, StoryIds.Merle));
        Assert.AreEqual(StoryStatus.Active, S().Status("h9"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Talk, StoryIds.Ottilie));
        Assert.AreEqual(StoryStatus.Done, S().Status("h9"), "h9 has no turn-in");
    }

    [Test]
    public void N4_EliteAfterH6_Completes() // quest:n4
    {
        CompleteH4();
        OpenActTwo();
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h6"));
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Available, S().Status("n4"));
        Assert.IsTrue(S().Accept("n4"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.GlassElite));
        Assert.AreEqual(StoryStatus.Ready, S().Status("n4"));
        TurnIn("n4");
        Assert.AreEqual(StoryStatus.Done, S().Status("n4"));
    }

    [Test]
    public void N5_FourAugursAfterH5AndActOne_Completes() // quest:n5
    {
        CompleteH4();
        OpenActTwo();
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h5"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Talk, StoryIds.Ilsabe));
        TurnIn("h5");
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Available, S().Status("n5"));
        Assert.IsTrue(S().Accept("n5"));
        for (int i = 0; i < 4; i++)
            Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.DuneAugur));
        Assert.AreEqual(StoryStatus.Ready, S().Status("n5"));
        TurnIn("n5");
        Assert.AreEqual(StoryStatus.Done, S().Status("n5"));
    }

    // ---- gesperrte Schritte ----

    [Test]
    public void Locked_AcceptTurnInApply_Rejected() // quest:h2
    {
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Locked, S().Status("h2"));
        Assert.IsFalse(S().Accept("h2"));
        Assert.IsFalse(S().TurnIn("h2"));
        Assert.IsFalse(S().Apply(StoryObjectiveType.Kill, StoryIds.BoneWarden));
        Assert.IsFalse(S().TurnIn("h1"), "no turn-in before Ready");
        Assert.IsFalse(S().Apply(StoryObjectiveType.Kill, StoryIds.AshLancer), "wrong target counts nothing");
    }

    [Test]
    public void Locked_GateSealsActTwoQuests() // quest:h6
    {
        // quest:n3
        Assert.IsTrue(S().Refresh(Act()));
        Assert.AreEqual(StoryStatus.Locked, S().Status("n3"), "Kiln gate closed");
        Assert.IsFalse(S().Accept("n3"));
        Assert.AreEqual(StoryStatus.Locked, S().Status("h6"), "Act I gate closed");
        Assert.IsFalse(S().Accept("h6"));
    }

    [Test]
    public void Locked_DoubleAcceptAndDoubleTurnIn_Rejected() // quest:h1
    {
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h1"));
        Assert.IsFalse(S().Accept("h1"), "already active");
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Fuhrbrief));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.FenBrute));
        TurnIn("h1");
        Assert.IsFalse(S().TurnIn("h1"), "already done");
        Assert.IsFalse(S().Apply(StoryObjectiveType.Collect, StoryIds.Fuhrbrief), "done quest ignores events");
    }

    [Test]
    public void CatchUp_ActStateClosesSkippedChains() // quest:h4
    {
        var act = new ActProgress();
        Assert.IsTrue(act.Unlock(ActProgress.Kiln));
        Assert.IsTrue(S().Refresh(act));
        // quest:h1, quest:h2
        Assert.AreEqual(StoryStatus.Done, S().Status("h1"));
        Assert.AreEqual(StoryStatus.Done, S().Status("h2"));
        Assert.IsTrue(act.CompleteAct());
        Assert.IsTrue(S().Refresh(act));
        // quest:h3
        Assert.AreEqual(StoryStatus.Done, S().Status("h3"));
        Assert.AreEqual(StoryStatus.Done, S().Status("h4"));
        Assert.AreEqual(StoryStatus.Available, S().Status("h5"));
        // quest:h5
        Assert.AreEqual(StoryStatus.Available, S().Status("h6"));
    }

    // ---- einmalige Belohnungen (produktiver Vergabepfad) ----

    [Test]
    public void Reward_N1_GoldOnce() // quest:n1
    {
        var q = StoryCatalog.Get("n1");
        Assert.AreEqual(StoryReward.Gold, q.reward);
        Assert.AreEqual(150, q.rewardGold);
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h1"));
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("n1"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Lieferbuch));
        TurnIn("n1");
        var wallet = hero.GetComponent<Wallet>();
        GrantRewards(hero);
        Assert.AreEqual(150, wallet.Gold);
        Assert.IsTrue(S().Flags.Contains("reward:n1"));
        GrantRewards(hero);
        Assert.AreEqual(150, wallet.Gold, "second grant is a no-op");
    }

    [Test]
    public void Reward_N2_RareHelmOnce() // quest:n2
    {
        var q = StoryCatalog.Get("n2");
        Assert.AreEqual(StoryReward.RareHelm, q.reward);
        CompleteH1();
        S().Refresh(Act());
        Assert.IsTrue(S().Accept("h2"));
        S().Refresh(Act());
        Assert.IsTrue(S().Accept("n2"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Grabtafel, 3));
        TurnIn("n2");
        var inv = hero.GetComponent<Inventory>();
        GrantRewards(hero);
        Assert.AreEqual(1, inv.Count);
        Assert.AreEqual(ItemSlot.Helmet, inv.Get(0).slot);
        GrantRewards(hero);
        Assert.AreEqual(1, inv.Count, "second grant is a no-op");
    }

    [Test]
    public void Reward_N3_MaterialAndGoldOnce() // quest:n3
    {
        var q = StoryCatalog.Get("n3");
        Assert.AreEqual(StoryReward.SmithKit, q.reward);
        Assert.AreEqual(50, q.rewardGold);
        Assert.IsTrue(Act().Unlock(ActProgress.Kiln));
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("n3"));
        for (int i = 0; i < 6; i++)
            Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.AshLancer));
        TurnIn("n3");
        var wallet = hero.GetComponent<Wallet>();
        GrantRewards(hero);
        Assert.AreEqual(50, wallet.Gold);
        Assert.AreEqual(3, RunProfile.SmithingMaterial);
        GrantRewards(hero);
        Assert.AreEqual(50, wallet.Gold);
        Assert.AreEqual(3, RunProfile.SmithingMaterial, "second grant is a no-op");
    }

    [Test]
    public void Reward_N4_GoldOnce() // quest:n4
    {
        var q = StoryCatalog.Get("n4");
        Assert.AreEqual(StoryReward.Gold, q.reward);
        Assert.AreEqual(250, q.rewardGold);
        CompleteH4();
        OpenActTwo();
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h6"));
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("n4"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.GlassElite));
        TurnIn("n4");
        var wallet = hero.GetComponent<Wallet>();
        GrantRewards(hero);
        Assert.AreEqual(250, wallet.Gold);
        GrantRewards(hero);
        Assert.AreEqual(250, wallet.Gold, "second grant is a no-op");
    }

    [Test]
    public void Reward_N5_SeersCircletOnce() // quest:n5
    {
        var q = StoryCatalog.Get("n5");
        Assert.AreEqual(StoryReward.SeerCirclet, q.reward);
        CompleteH4();
        OpenActTwo();
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h5"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Talk, StoryIds.Ilsabe));
        TurnIn("h5");
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("n5"));
        for (int i = 0; i < 4; i++)
            Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.DuneAugur));
        TurnIn("n5");
        var inv = hero.GetComponent<Inventory>();
        GrantRewards(hero);
        Assert.AreEqual(1, inv.Count);
        Assert.AreEqual("Seer's Circlet", inv.Get(0).uniqueId);
        GrantRewards(hero);
        Assert.AreEqual(1, inv.Count, "second grant is a no-op");
    }

    // ---- persistente Zustaende nach Speichern und Laden ----

    [Test]
    public void SaveLoad_MidQuestProgress_Restored()
    {
        // quest:h1
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h1"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Fuhrbrief));
        var restored = StoryState.FromSave(S().ToSave(), new System.Collections.Generic.List<string>(S().Flags));
        Assert.AreEqual(StoryStatus.Active, restored.Status("h1"));
        Assert.AreEqual(1, restored.Count("h1", 0));
        Assert.AreEqual(0, restored.Count("h1", 1));
    }

    [Test]
    public void SaveLoad_DoneQuestAndRewardFlag_Restored()
    {
        // quest:n1
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("h1"));
        Assert.IsTrue(S().Refresh(Act()));
        Assert.IsTrue(S().Accept("n1"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Lieferbuch));
        TurnIn("n1");
        GrantRewards(hero);
        var restored = StoryState.FromSave(S().ToSave(), new System.Collections.Generic.List<string>(S().Flags));
        Assert.AreEqual(StoryStatus.Done, restored.Status("n1"));
        Assert.IsTrue(restored.Flags.Contains("reward:n1"), "once-only flag persists");
    }

    [Test]
    public void SaveLoad_ViaProductivePath_RestoresMidAndEndState()
    {
        // quest:h2 (Zwischenstand) + quest:n1 (Endstand mit Einmal-Belohnung)
        CompleteH1();
        S().Refresh(Act());
        Assert.IsTrue(S().Accept("h2"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Kill, StoryIds.BoneWarden));
        Assert.IsTrue(S().Accept("n1"));
        Assert.IsTrue(S().Apply(StoryObjectiveType.Collect, StoryIds.Lieferbuch));
        TurnIn("n1");
        GrantRewards(hero);
        int goldBefore = hero.GetComponent<Wallet>().Gold;
        Assert.AreEqual(150, goldBefore);

        SaveService.SaveNow(hero, -1, savePath);
        Assert.IsTrue(File.Exists(savePath));
        RunProfile.Reset();
        var fresh = new GameObject("FreshHero");
        fresh.AddComponent<Equipment>();
        fresh.AddComponent<Inventory>();
        fresh.AddComponent<Wallet>();
        fresh.AddComponent<DiabloProgression>();
        try
        {
            Assert.IsTrue(SaveService.TryLoad(fresh, savePath));
            Assert.AreEqual(StoryStatus.Active, RunProfile.Story.Status("h2"), "Zwischenstand wiederhergestellt");
            Assert.AreEqual(1, RunProfile.Story.Count("h2", 1), "Wardenzahl wiederhergestellt");
            Assert.AreEqual(StoryStatus.Done, RunProfile.Story.Status("n1"), "Endstand wiederhergestellt");
            Assert.IsTrue(RunProfile.Story.Flags.Contains("reward:n1"), "Einmal-Flag wiederhergestellt");
            Assert.AreEqual(150, fresh.GetComponent<Wallet>().Gold, "Gold wiederhergestellt");
            int invBefore = fresh.GetComponent<Inventory>().Count;
            var director = fresh.AddComponent<StoryDirector>();
            var m = typeof(StoryDirector).GetMethod("GrantRewards",
                BindingFlags.NonPublic | BindingFlags.Instance);
            m.Invoke(director, null);
            Assert.AreEqual(150, fresh.GetComponent<Wallet>().Gold, "keine Zweitvergabe nach Laden");
            Assert.AreEqual(invBefore, fresh.GetComponent<Inventory>().Count);
        }
        finally
        {
            Object.DestroyImmediate(fresh);
        }
    }
}
