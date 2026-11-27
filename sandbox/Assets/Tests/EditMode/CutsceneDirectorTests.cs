using NUnit.Framework;
using UnityEngine;

/// <summary>Cutscene pipeline: boss intros trigger once, hold the boss, skip cleanly; main-quest talks play boxed.</summary>
public class CutsceneDirectorTests
{
    GameObject hero;
    GameObject boss;

    [SetUp]
    public void Setup()
    {
        CutsceneDirector.ResetRun();
        RunProfile.Reset();
        RunProfile.Story.Refresh(RunProfile.Act);
        hero = new GameObject("Player");
        hero.AddComponent<PlayerHealth>();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(hero);
        if (boss != null) Object.DestroyImmediate(boss);
        boss = null;
        CutsceneDirector.ResetRun();
        RunProfile.Reset();
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    GameObject SpawnBoss(string name, Vector3 at)
    {
        boss = new GameObject(name);
        boss.transform.position = at;
        return boss;
    }

    [Test]
    public void NearBoss_StartsIntroOnceAndRearmsAfterReset()
    {
        SpawnBoss(BossController.BossName, new Vector3(10f, 0f, 0f));
        hero.transform.position = new Vector3(5f, 0f, 0f);
        CutsceneDirector.Tick(10f);
        Assert.IsTrue(CutsceneDirector.Active);
        Assert.AreEqual("intro.abbot", CutsceneDirector.ActiveId);
        Assert.IsTrue(CutsceneDirector.HoldBosses);
        Assert.AreEqual(1, CutsceneDirector.Starts);
        CutsceneDirector.Tick(10f + 9f + 0.5f);
        Assert.IsFalse(CutsceneDirector.Active);
        Assert.AreEqual(1, CutsceneDirector.Completes);
        CutsceneDirector.Tick(30f);
        Assert.AreEqual(1, CutsceneDirector.Starts, "once per run");
        CutsceneDirector.ResetRun();
        CutsceneDirector.Tick(31f);
        Assert.AreEqual(1, CutsceneDirector.Starts, "re-armed after reset");
        Assert.IsTrue(CutsceneDirector.Active);
    }

    [Test]
    public void FarHero_DoesNotTrigger()
    {
        SpawnBoss(BossController.BossName, new Vector3(10f, 0f, 0f));
        hero.transform.position = new Vector3(100f, 0f, 0f);
        CutsceneDirector.Tick(10f);
        Assert.IsFalse(CutsceneDirector.Active);
        Assert.AreEqual(0, CutsceneDirector.Starts);
    }

    [Test]
    public void DeadBoss_DoesNotTrigger()
    {
        var go = SpawnBoss(BossController.BossName, new Vector3(10f, 0f, 0f));
        go.AddComponent<EnemyHealth>().SetTier(10, LootTier.Normal);
        UnityEngine.TestTools.LogAssert.Expect(LogType.Error,
            new System.Text.RegularExpressions.Regex("Destroy may not be called from edit mode"));
        go.GetComponent<EnemyHealth>().TakeDamage(1000);
        hero.transform.position = new Vector3(5f, 0f, 0f);
        CutsceneDirector.Tick(10f);
        Assert.IsFalse(CutsceneDirector.Active);
        var doomed = new System.Collections.Generic.List<GameObject>();
        foreach (var left in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            var n = left.name;
            if (n.StartsWith("Corpse_") || n.StartsWith("DiabloOrb_")) doomed.Add(left);
        }
        foreach (var d in doomed)
            if (d != null) Object.DestroyImmediate(d);
        EnemyHealth.ResetRun();
    }

    [Test]
    public void Skip_EndsIntroAndRestoresControl()
    {
        SpawnBoss(BossController.BossName, new Vector3(10f, 0f, 0f));
        hero.transform.position = new Vector3(5f, 0f, 0f);
        var tpc = hero.AddComponent<ThirdPersonController>();
        Assert.IsTrue(CutsceneDirector.TryStartBossIntro("intro.abbot", 10f));
        Assert.IsFalse(tpc.enabled, "movement frozen during the intro");
        Assert.IsTrue(CutsceneDirector.Skip());
        Assert.IsFalse(CutsceneDirector.Active);
        Assert.IsFalse(CutsceneDirector.HoldBosses);
        Assert.IsTrue(tpc.enabled);
        Assert.AreEqual(1, CutsceneDirector.Skips);
        Assert.IsFalse(CutsceneDirector.Skip(), "idle skip is a no-op");
    }

    [Test]
    public void ShowcaseData_EveryBossHasAbilitiesAndResolvedCaptions()
    {
        Assert.AreEqual(3, CutsceneDirector.BossIntros.Length);
        foreach (var intro in CutsceneDirector.BossIntros)
        {
            Assert.GreaterOrEqual(intro.duration, 8f, intro.id);
            Assert.LessOrEqual(intro.duration, 12f, intro.id);
            Assert.GreaterOrEqual(intro.phaseTwoDuration, 3f, intro.id);
            Assert.LessOrEqual(intro.phaseTwoDuration, 5f, intro.id);
            Assert.IsFalse(string.IsNullOrEmpty(intro.phaseTwoId), intro.id);
            Assert.IsNotNull(intro.phaseOneAbilities, intro.id);
            Assert.IsNotNull(intro.phaseTwoAbilities, intro.id);
            Assert.GreaterOrEqual(intro.phaseOneAbilities.Length, 1, intro.id);
            Assert.GreaterOrEqual(intro.phaseTwoAbilities.Length, 1, intro.id);
            foreach (var key in intro.phaseOneAbilities)
                Assert.AreNotEqual(key, Loc.T(key), key);
            foreach (var key in intro.phaseTwoAbilities)
                Assert.AreNotEqual(key, Loc.T(key), key);
        }
    }

    [Test]
    public void Intro_FiresEachPhaseOneShowcaseOnceAndHarmless()
    {
        SpawnBoss(BossController.BossName, new Vector3(10f, 0f, 0f));
        hero.transform.position = new Vector3(5f, 0f, 0f);
        float hpBefore = hero.GetComponent<PlayerHealth>().CurrentHP;
        Assert.IsTrue(CutsceneDirector.TryStartBossIntro("intro.abbot", 10f));
        CutsceneDirector.Tick(13f); // first step at 9 * 1/3 = 3 s
        Assert.AreEqual(1, CutsceneDirector.ShowcaseCastCount("cutscene.ability.abbot.slam"));
        Assert.AreEqual("cutscene.ability.abbot.slam", CutsceneDirector.CurrentAbilityKey);
        CutsceneDirector.Tick(16f); // second step at 9 * 2/3 = 6 s
        Assert.AreEqual(1, CutsceneDirector.ShowcaseCastCount("cutscene.ability.abbot.combo"));
        Assert.AreEqual(2, CutsceneDirector.ShowcaseCasts);
        CutsceneDirector.Tick(19.5f); // intro ends at 9 s
        Assert.IsFalse(CutsceneDirector.Active);
        Assert.AreEqual(hpBefore, hero.GetComponent<PlayerHealth>().CurrentHP, "demo casts never damage");
        Assert.AreEqual(2, CutsceneDirector.ShowcaseCasts, "no cast repeats after the intro");
    }

    [Test]
    public void GlasswrightIntro_SpawnsLanceTelegraphsAwayFromHero()
    {
        SpawnBoss(GlasswrightController.BossName, new Vector3(10f, 0f, 0f));
        hero.transform.position = new Vector3(5f, 0f, 0f);
        float hpBefore = hero.GetComponent<PlayerHealth>().CurrentHP;
        Assert.IsTrue(CutsceneDirector.TryStartBossIntro("intro.glasswright", 10f));
        CutsceneDirector.Tick(14.5f); // only step at 9 * 1/2 = 4.5 s
        Assert.AreEqual(1, CutsceneDirector.ShowcaseCastCount("cutscene.ability.glasswright.lances"));
        int lances = 0;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go == null) continue;
            string n;
            try { n = go.name; } catch { continue; } // destroyed by an earlier test in the full suite
            if (n == "ShardLance") { lances++; Object.DestroyImmediate(go); }
        }
        Assert.AreEqual(3, lances, "fan of three telegraphs");
        Assert.AreEqual(hpBefore, hero.GetComponent<PlayerHealth>().CurrentHP, "demo lances never damage");
    }

    [Test]
    public void PhaseTwoShowcase_HoldsBossKeepsHeroControlAndRunsOnce()
    {
        SpawnBoss(BossController.BossName, new Vector3(10f, 0f, 0f));
        hero.transform.position = new Vector3(5f, 0f, 0f);
        var tpc = hero.AddComponent<ThirdPersonController>();
        Assert.IsTrue(CutsceneDirector.TryStartPhaseTwoShowcase("intro.abbot", 20f));
        Assert.AreEqual("phase2.abbot", CutsceneDirector.ActiveId);
        Assert.IsTrue(CutsceneDirector.HoldBosses);
        Assert.IsTrue(tpc.enabled, "hero keeps control during the phase-2 showcase");
        CutsceneDirector.Tick(21.4f); // first step at 4 * 1/3
        Assert.AreEqual(1, CutsceneDirector.ShowcaseCastCount("cutscene.ability.abbot.summon"));
        CutsceneDirector.Tick(22.7f); // second step at 4 * 2/3
        Assert.AreEqual(1, CutsceneDirector.ShowcaseCastCount("cutscene.ability.abbot.firefan"));
        CutsceneDirector.Tick(24.5f);
        Assert.IsFalse(CutsceneDirector.Active);
        Assert.IsFalse(CutsceneDirector.HoldBosses);
        Assert.IsFalse(CutsceneDirector.TryStartPhaseTwoShowcase("intro.abbot", 30f), "once per run");
        CutsceneDirector.ResetRun();
        Assert.IsTrue(CutsceneDirector.TryStartPhaseTwoShowcase("intro.abbot", 40f), "re-armed after reset");
    }

    [Test]
    public void PhaseTwoShowcase_SkipEndsAndRestores()
    {
        SpawnBoss(KilnLayout.KilnmotherName, new Vector3(10f, 0f, 0f));
        hero.transform.position = new Vector3(5f, 0f, 0f);
        Assert.IsTrue(CutsceneDirector.TryStartPhaseTwoShowcase("intro.kilnmother", 10f));
        Assert.IsTrue(CutsceneDirector.Skip());
        Assert.IsFalse(CutsceneDirector.Active);
        Assert.IsFalse(CutsceneDirector.HoldBosses);
        Assert.AreEqual(1, CutsceneDirector.Skips);
    }

    [Test]
    public void UnknownIntro_DoesNotStart()
    {
        SpawnBoss(BossController.BossName, new Vector3(10f, 0f, 0f));
        hero.transform.position = new Vector3(5f, 0f, 0f);
        Assert.IsFalse(CutsceneDirector.TryStartBossIntro("intro.nope", 10f));
        Assert.IsFalse(CutsceneDirector.Active);
    }

    [Test]
    public void DialogHighlight_AutoAdvancesAndSkipsWithoutBooking()
    {
        var s = new StoryState();
        s.Refresh(new ActProgress());
        var plan = StoryDialogs.Plan(s, StoryIds.Ottilie);
        Assert.AreEqual("h1", plan.questId, "fresh run offers the main quest");
        var ui = hero.AddComponent<StoryDialogUI>();
        ui.Show(plan, StoryIds.Ottilie);
        Assert.IsTrue(ui.IsOpen);
        Assert.IsTrue(CutsceneDirector.PlayDialog(ui, StoryIds.Ottilie));
        Assert.IsTrue(CutsceneDirector.Active);
        CutsceneDirector.Tick(Time.unscaledTime + 100f);
        Assert.AreEqual(1, ui.LineIndex, "advanced on the voice length");
        Assert.IsTrue(ui.IsOpen);
        Assert.IsTrue(CutsceneDirector.Skip());
        Assert.IsFalse(ui.IsOpen);
        Assert.AreEqual(StoryStatus.Available, RunProfile.Story.Status("h1"), "skip books nothing");
    }

    [Test]
    public void MainQuestTalk_OpensCutscene()
    {
        Assert.IsTrue(StoryDirector.TryOpenTalk(hero, StoryIds.Ottilie));
        Assert.IsTrue(CutsceneDirector.Active, "main-quest talk plays boxed");
        Assert.AreEqual("dialog." + StoryIds.Ottilie, CutsceneDirector.ActiveId);
    }

    [Test]
    public void SideQuestTalk_StaysPlainWindow()
    {
        Assert.IsTrue(StoryDirector.TryOpenTalk(hero, StoryIds.Ottilie));
        var ui = hero.GetComponent<StoryDialogUI>();
        for (int i = 0; i < 4; i++) ui.Advance(); // h1.offer has 4 lines; close books the offer
        Assert.IsFalse(ui.IsOpen);
        Assert.AreEqual(StoryStatus.Active, RunProfile.Story.Status("h1"));
        CutsceneDirector.ResetRun();
        RunProfile.Story.Refresh(RunProfile.Act);
        Assert.IsTrue(StoryDirector.TryOpenTalk(hero, StoryIds.Ilsabe), "h1 opens the n1 offer");
        Assert.AreEqual("n1", StoryDialogs.Plan(RunProfile.Story, StoryIds.Ilsabe).questId);
        Assert.IsTrue(ui.IsOpen);
        Assert.IsFalse(CutsceneDirector.Active, "side quests keep the plain window");
        ui.SetOpen(false);
    }
}
