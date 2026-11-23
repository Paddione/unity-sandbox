using NUnit.Framework;

/// <summary>
/// coop-trio: a co-op client books the host's boss kills into its own act
/// state (Abbot → Kindled + Kiln, Kilnmother → Act I + Barrens), so a client
/// save after a group run carries the same progression as a solo run.
/// </summary>
public class CoopActBookingTests
{
    [SetUp]
    public void Setup() => RunProfile.Reset();

    [TearDown]
    public void Teardown() => RunProfile.Reset();

    [Test]
    public void AbbotKill_OpensKilnAndKindled_Once()
    {
        Assert.IsFalse(RunProfile.Act.IsUnlocked(ActProgress.Kiln));
        DiabloNetworkPlayer.BookAbbotKill();
        Assert.IsTrue(RunProfile.Act.IsUnlocked(ActProgress.Kiln));
        Assert.IsTrue(RunProfile.DifficultyUnlocked);
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
}
