using NUnit.Framework;
using UnityEngine;

/// <summary>
/// cinderreach-team-autoplay K2/K3: dedicated-server story authority (forwarded
/// collects and zone entries), the party difficulty a server adopts, the Kindled
/// follow-up campaign and the lead/follower phase contract.
/// </summary>
public class TeamCampaignTests
{
    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        SessionComplete.ResetRun();
    }

    [TearDown]
    public void Teardown()
    {
        RunProfile.Reset();
        SessionComplete.ResetRun();
        ActBossGate.ResetRun();
    }

    static void ReachMoortor()
    {
        RunProfile.Act.Unlock(ActProgress.Kiln);
        RunProfile.Act.CompleteAct();
        RunProfile.Act.DefeatZoneBoss();
        RunProfile.Finale.OpenMoortor(RunProfile.Act);
    }

    [Test]
    public void PartyDifficulty_KindledOnlyWhenTheHeroUnlockedIt()
    {
        Assert.AreEqual(RunProfile.Kindled, DedicatedServer.PartyDifficulty("Kindled", true));
        Assert.AreEqual(RunProfile.Kindled, DedicatedServer.PartyDifficulty("kindled", true));
        Assert.AreEqual(RunProfile.Normal, DedicatedServer.PartyDifficulty("Kindled", false), "a locked save cannot force Kindled");
        Assert.AreEqual(RunProfile.Normal, DedicatedServer.PartyDifficulty("Normal", true));
        Assert.AreEqual(RunProfile.Normal, DedicatedServer.PartyDifficulty(null, true));
    }

    [Test]
    public void StartCampaign_RestartsStoryAndActs_KeepsHeroAndUnlock()
    {
        RunProfile.DifficultyUnlocked = true;
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        RunProfile.SmithingMaterial = 4;
        ReachMoortor();
        RunProfile.Finale.DefeatFinalBoss();
        var oldStory = RunProfile.Story;
        int seed = RunProfile.RunSeed;

        Assert.AreEqual(RunProfile.Kindled, RunProfile.StartCampaign(RunProfile.Kindled));

        Assert.AreSame(DifficultyProfile.Kindled, DifficultyProfile.Current);
        Assert.IsFalse(RunProfile.Act.ActOneComplete);
        Assert.IsFalse(RunProfile.Act.ZoneBossDefeated);
        Assert.IsFalse(RunProfile.Finale.FinalBossDefeated);
        Assert.AreNotSame(oldStory, RunProfile.Story);
        Assert.AreEqual(StoryStatus.Locked, RunProfile.Story.Status("h12"));
        Assert.AreNotEqual(seed, RunProfile.RunSeed);
        Assert.IsTrue(RunProfile.DifficultyUnlocked);
        Assert.AreEqual(ClassCatalog.LanternbearerId, RunProfile.ClassId);
        Assert.AreEqual(4, RunProfile.SmithingMaterial);
    }

    [Test]
    public void StartCampaign_LockedKindled_FallsBackToNormal()
    {
        Assert.AreEqual(RunProfile.Normal, RunProfile.StartCampaign(RunProfile.Kindled));
    }

    [Test]
    public void PrepareKindledCampaign_OnlyAfterThewesOnNormal()
    {
        Assert.IsFalse(AutoplayResult.PrepareKindledCampaign(), "nothing unlocked yet");
        ReachMoortor();
        StoryDirector.BookKill(StoryIds.Thewes);
        Assert.IsTrue(RunProfile.DifficultyUnlocked);

        Assert.IsTrue(AutoplayResult.PrepareKindledCampaign());
        Assert.AreEqual(RunProfile.Kindled, RunProfile.Difficulty);
        Assert.IsFalse(RunProfile.Finale.FinalBossDefeated, "the Kindled run starts the campaign over");
        Assert.IsFalse(AutoplayResult.PrepareKindledCampaign(), "a Kindled run does not restart again");
    }

    [Test]
    public void BookForwarded_UnknownKindsAndItemsBookNothing()
    {
        Assert.IsFalse(StoryDirector.BookForwarded(99, "x"));
        Assert.IsFalse(StoryDirector.BookForwarded(StoryDirector.StoryEventCollect, "no-such-item"));
    }

    [Test]
    public void ForwardToHost_IsANoOpOffline()
    {
        var hero = new GameObject("Player");
        Assert.DoesNotThrow(() => StoryDirector.ForwardToHost(StoryDirector.StoryEventCollect, "x", hero));
        Assert.DoesNotThrow(() => StoryDirector.ForwardToHost(StoryDirector.StoryEventCollect, "x", null));
        Object.DestroyImmediate(hero);
    }

    [Test]
    public void LeadPlaysTheFullRoute_FollowersNeedTheFinalBoss()
    {
        CollectionAssert.Contains(AutoplayResult.FollowPhases, AutoplayResult.PhaseFinalBoss);
        CollectionAssert.AreEqual(AutoplayResult.RequiredPhases, AutoplayResult.RequiredFor(AutoplayResult.RoleLead));
    }

    [Test]
    public void SnapEnemy_MovesTheClientCopyToTheServerPosition()
    {
        var husk = new GameObject("Fen Brute");
        husk.AddComponent<CharacterController>();
        DiabloNetworkPlayer.SnapEnemy(husk, new Vector3(9f, 0f, -15f));
        Assert.AreEqual(new Vector3(9f, 0f, -15f), husk.transform.position);
        Assert.IsTrue(husk.GetComponent<CharacterController>().enabled);
        Assert.DoesNotThrow(() => DiabloNetworkPlayer.SnapEnemy(null, Vector3.zero));
        Object.DestroyImmediate(husk);
    }

    [Test]
    public void ClientRefresh_LeavesCatchUpToTheHost()
    {
        // H19 catches up once the Glasswright fell; a co-op client must not close it itself.
        ReachMoortor();
        var client = new StoryState();
        client.Refresh(RunProfile.Act, catchUp: false);
        Assert.AreNotEqual(StoryStatus.Done, client.Status("h19"));
        var host = new StoryState();
        host.Refresh(RunProfile.Act);
        Assert.AreEqual(StoryStatus.Done, host.Status("h19"));
        Assert.IsTrue(StoryDirector.CatchUpHere, "offline the local state counts");
    }
}
