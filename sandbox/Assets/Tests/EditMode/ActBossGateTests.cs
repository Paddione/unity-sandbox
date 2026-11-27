using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// cinderreach-act-boss-gating: act bosses wait for every other quest of their
/// act, wake once with a spawn cutscene that frames them without captions,
/// and the required sets never deadlock.
/// </summary>
public class ActBossGateTests
{
    GameObject hero;
    GameObject boss;
    GameObject camGo;
    bool ownsCam;
    Vector3 savedPos;
    Quaternion savedRot;
    float savedAspect, savedFov;

    [SetUp]
    public void Setup()
    {
        CutsceneDirector.ResetRun();
        RunProfile.Reset();
        RunProfile.Story.Refresh(RunProfile.Act);
        hero = new GameObject(DiabloNetworkPlayer.HeroName);
        hero.AddComponent<PlayerHealth>();
        hero.transform.position = new Vector3(-200f, 0f, 0f); // far from the arena: remote cut
        // CutsceneDirector drives Camera.main: reuse a scene main camera when one exists.
        ownsCam = Camera.main == null;
        camGo = ownsCam ? new GameObject("TestMainCamera") : Camera.main.gameObject;
        if (ownsCam) { camGo.tag = "MainCamera"; camGo.AddComponent<Camera>(); }
        var testCam = camGo.GetComponent<Camera>();
        savedPos = camGo.transform.position;
        savedRot = camGo.transform.rotation;
        savedAspect = testCam.aspect;
        savedFov = testCam.fieldOfView;
        testCam.aspect = 16f / 9f; // batchmode has no screen: pin the 1080p aspect
        testCam.fieldOfView = 60f;
        camGo.transform.SetPositionAndRotation(hero.transform.position + new Vector3(0f, 12f, -10f),
            Quaternion.Euler(50f, 0f, 0f));
    }

    [TearDown]
    public void Teardown()
    {
        ActBossGate.ResetRun();
        CutsceneDirector.ResetRun();
        Object.DestroyImmediate(hero);
        if (ownsCam) Object.DestroyImmediate(camGo);
        else if (camGo != null)
        {
            camGo.transform.SetPositionAndRotation(savedPos, savedRot);
            var c = camGo.GetComponent<Camera>();
            c.aspect = savedAspect;
            c.fieldOfView = savedFov;
        }
        if (boss != null) Object.DestroyImmediate(boss);
        RunProfile.Reset();
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    static void FinishAll(IEnumerable<string> ids)
    {
        var parts = new List<string>();
        foreach (var id in ids) parts.Add(id + ":" + StoryStatus.Done + ":");
        RunProfile.Story.Merge(string.Join(";", parts));
    }

    [Test]
    public void RequiredQuests_AreEveryOtherQuestOfTheAct()
    {
        CollectionAssert.AreEquivalent(
            new[] { "h1", "h2", "h3", "n1", "n2", "n3", "h14", "h15", "h16", "n8", "n9", "n10", "n11" },
            ActBossGate.RequiredQuests(1));
        CollectionAssert.AreEquivalent(
            new[] { "h6", "h7", "n4", "n5", "h18", "n12", "n13", "n14", "n15" },
            ActBossGate.RequiredQuests(2));
        CollectionAssert.AreEquivalent(new[] { "h10", "n6", "n7", "h11" }, ActBossGate.RequiredQuests(3));
    }

    [Test]
    public void RequiredQuests_NeverWaitOnTheirOwnOrALaterBoss()
    {
        // Act state a gate needs, as the act whose boss sets it (Abbot = 0: not gated).
        int GateAct(StoryGate g) => g == StoryGate.ActOneComplete ? 1 : g == StoryGate.ZoneBossDefeated ? 2 : 0;
        foreach (var b in ActBossGate.Bosses)
        {
            foreach (var id in ActBossGate.RequiredQuests(b.act))
            {
                var q = StoryCatalog.Get(id);
                Assert.Less(GateAct(q.gate), b.act, $"{id}: gate {q.gate} needs a boss at or after act {b.act}");
                foreach (var other in ActBossGate.Bosses)
                    if (other.act >= b.act)
                        Assert.IsFalse(ActBossGate.DependsOn(q, other.quest), $"{id} waits on {other.quest}");
            }
        }
    }

    [Test]
    public void Boss_StaysDormantUntilTheLastQuest_ThenWakesOnceWithCutscene()
    {
        boss = new GameObject(KilnLayout.KilnmotherName);
        boss.transform.position = new Vector3(80f, 0f, 40f);
        var required = new List<string>(ActBossGate.RequiredQuests(1));
        ActBossGate.Tick(1f);
        Assert.IsFalse(boss.activeSelf, "fresh run: no Kilnmother");
        FinishAll(required.GetRange(0, required.Count - 1));
        ActBossGate.Tick(2f);
        Assert.IsFalse(boss.activeSelf, "one quest still open");
        Assert.IsFalse(ActBossGate.Ready(RunProfile.Story, 1));
        FinishAll(required);
        ActBossGate.Tick(3f);
        Assert.IsTrue(boss.activeSelf);
        Assert.AreEqual(1, ActBossGate.Spawns[0]);
        Assert.AreEqual("intro.kilnmother", CutsceneDirector.ActiveId);
        Assert.IsTrue(CutsceneDirector.SpawnMode);
        ActBossGate.Tick(4f);
        Assert.AreEqual(1, ActBossGate.Spawns[0], "wakes exactly once");
    }

    [Test]
    public void SpawnCutscene_FramesTheBossDuringEveryShowcaseAndReturnsWithoutCaptions()
    {
        boss = new GameObject(KilnLayout.KilnmotherName);
        boss.transform.position = new Vector3(80f, 0f, 40f);
        var cam = camGo.GetComponent<Camera>();
        var before = camGo.transform.position;
        FinishAll(ActBossGate.RequiredQuests(1));
        ActBossGate.Tick(10f);
        Assert.AreEqual("intro.kilnmother", CutsceneDirector.ActiveId);
        string banner = Loc.T("name.enemy.kilnmother");
        for (float t = 10f; t <= 18.9f; t += 0.5f)
        {
            CutsceneDirector.Tick(t);
            var v = cam.WorldToViewportPoint(boss.transform.position + Vector3.up);
            Assert.Greater(v.z, 0f, $"boss in front of the camera at {t}");
            Assert.That(v.x, Is.InRange(0.1f, 0.9f), $"boss on screen (x) at {t} main={(Camera.main != null ? Camera.main.name : "null")} cam={camGo.transform.position} spawn={CutsceneDirector.SpawnMode}");
            Assert.That(v.y, Is.InRange(0.1f, 0.9f), $"boss on screen (y) at {t}");
            Assert.AreEqual(banner, CutsceneDirector.BannerText, $"only the boss name shows at {t}");
        }
        Assert.AreEqual(1, CutsceneDirector.ShowcaseCastCount("cutscene.ability.kilnmother.wave"), "phase-1 ability shown");
        CutsceneDirector.Tick(19.5f);
        Assert.IsFalse(CutsceneDirector.Active);
        Assert.AreEqual(before, camGo.transform.position, "camera back on the hero");
        Assert.IsTrue(RunProfile.Story.Flags.Contains(ActBossGate.SpawnFlag(1)));
    }

    [Test]
    public void PlayedSpawnCutscene_DoesNotReplayAfterLoad()
    {
        boss = new GameObject(GlasswrightController.BossName);
        FinishAll(ActBossGate.RequiredQuests(2));
        RunProfile.Story.Flags.Add(ActBossGate.SpawnFlag(2));
        ActBossGate.Tick(1f);
        Assert.IsTrue(boss.activeSelf, "boss stands after load");
        Assert.IsFalse(CutsceneDirector.Active, "cutscene was already played");
    }

    [Test]
    public void BusyCutscene_DefersTheSpawnIntroUntilFree()
    {
        boss = new GameObject(KilnLayout.KilnmotherName);
        var abbot = new GameObject(BossController.BossName);
        try
        {
            Assert.IsTrue(CutsceneDirector.TryStartBossIntro("intro.abbot", 0f) || CutsceneDirector.Active);
            FinishAll(ActBossGate.RequiredQuests(1));
            ActBossGate.Tick(1f);
            Assert.IsTrue(boss.activeSelf);
            Assert.AreNotEqual("intro.kilnmother", CutsceneDirector.ActiveId);
            Assert.IsFalse(RunProfile.Story.Flags.Contains(ActBossGate.SpawnFlag(1)));
            CutsceneDirector.Skip();
            ActBossGate.Tick(2f);
            Assert.AreEqual("intro.kilnmother", CutsceneDirector.ActiveId, "plays once the other sequence ended");
        }
        finally { Object.DestroyImmediate(abbot); }
    }

    [Test]
    public void DefeatedAct_NeverHidesTheBoss()
    {
        boss = new GameObject(KilnLayout.KilnmotherName);
        RunProfile.Act.Unlock(ActProgress.Kiln);
        RunProfile.Act.CompleteAct();
        ActBossGate.Tick(1f);
        Assert.IsTrue(boss.activeSelf);
        Assert.IsFalse(CutsceneDirector.Active);
    }

    [Test]
    public void ArenaEntry_NoLongerTriggersActBossIntros()
    {
        boss = new GameObject(KilnLayout.KilnmotherName);
        boss.transform.position = hero.transform.position + new Vector3(3f, 0f, 0f);
        CutsceneDirector.Tick(1f);
        Assert.IsFalse(CutsceneDirector.Active, "act bosses introduce themselves on spawn only");
    }

    [Test]
    public void KillLog_RecordsOpenQuestsAtTheKill()
    {
        StoryDirector.BookKill(KilnLayout.KilnmotherName);
        StringAssert.StartsWith("1:Kilnmother:h1,", ActBossGate.KillLog[0]);
        FinishAll(ActBossGate.RequiredQuests(2));
        StoryDirector.BookKill(GlasswrightController.BossName);
        Assert.AreEqual("2:Glasswright:-", ActBossGate.KillLog[1]);
    }

    [Test]
    public void ReleasedMask_HasOneBitPerReadyAct()
    {
        Assert.AreEqual(0, ActBossGate.ReleasedMask(RunProfile.Story));
        FinishAll(ActBossGate.RequiredQuests(1));
        FinishAll(ActBossGate.RequiredQuests(3));
        Assert.AreEqual(0b101, ActBossGate.ReleasedMask(RunProfile.Story));
    }
}
