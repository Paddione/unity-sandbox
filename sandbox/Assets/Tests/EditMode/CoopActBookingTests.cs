using NUnit.Framework;

/// <summary>
/// coop-trio: a co-op client books the host's boss kills into its own act
/// state (Abbot → Kiln, Kilnmother → Act I + Barrens, Thewes → Kindled), so a
/// client save after a group run carries the same progression as a solo run.
/// </summary>
public class CoopActBookingTests
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

    [Test]
    public void AbbotKill_OpensKiln_ButNotKindled()
    {
        Assert.IsFalse(RunProfile.Act.IsUnlocked(ActProgress.Kiln));
        DiabloNetworkPlayer.BookAbbotKill();
        Assert.IsTrue(RunProfile.Act.IsUnlocked(ActProgress.Kiln));
        Assert.IsFalse(RunProfile.DifficultyUnlocked, "cinderreach-team-autoplay K1: the Abbot no longer unlocks Kindled");
        Assert.IsFalse(SessionComplete.KindledUnlockedThisRun);
        DiabloNetworkPlayer.BookAbbotKill();
        Assert.AreEqual(ActProgress.Kiln, RunProfile.Act.HighestUnlocked, "a repeat broadcast changes nothing");
    }

    [Test]
    public void KilnmotherKill_CompletesActOne_EvenWithoutTheAbbotBroadcast()
    {
        DiabloNetworkPlayer.BookKilnmotherKill();
        Assert.IsTrue(RunProfile.Act.ActOneComplete);
        Assert.IsTrue(RunProfile.Act.IsUnlocked(ActProgress.Barrens));
        Assert.IsFalse(RunProfile.Act.ZoneBossDefeated);
    }

    [Test]
    public void AbbotKillBooking_DoesNotUnlockKindled()
    {
        StoryDirector.BookKill(BossController.BossName);
        Assert.IsFalse(RunProfile.DifficultyUnlocked);
    }

    [Test]
    public void ThewesKill_UnlocksKindled_Once()
    {
        StoryDirector.BookKill(StoryIds.Thewes);
        Assert.IsTrue(RunProfile.DifficultyUnlocked);
        Assert.IsTrue(SessionComplete.KindledUnlockedThisRun);
        Assert.IsFalse(StoryDirector.BookFinalBossKill(), "a repeat kill or broadcast unlocks nothing new");
    }

    [Test]
    public void ClientBooksHostThewesKill_UnlocksKindledAndFinale()
    {
        RunProfile.Act.Unlock(ActProgress.Kiln);
        RunProfile.Act.CompleteAct();
        RunProfile.Act.DefeatZoneBoss();
        RunProfile.Finale.OpenMoortor(RunProfile.Act);
        Assert.IsTrue(StoryDirector.BookFinalBossKill());
        Assert.IsTrue(RunProfile.DifficultyUnlocked);
        Assert.IsTrue(RunProfile.Finale.FinalBossDefeated);
    }
}
