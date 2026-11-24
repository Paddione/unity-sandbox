using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// cinderreach-quest-interaktionen, EditMode: Questregeln und Speicherung.
///
/// Deckt jede Quest (quest:h1..h9, quest:n1..n5) ueber die reine
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
