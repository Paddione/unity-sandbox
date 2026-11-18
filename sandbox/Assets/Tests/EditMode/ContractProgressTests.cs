using NUnit.Framework;
using UnityEngine;

/// <summary>zone-contracts S1: catalog determinism, progress states, save v5 reload.</summary>
public class ContractProgressTests
{
    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        ContractProgress.Reset();
    }

    [TearDown]
    public void Teardown()
    {
        RunProfile.Reset();
        ContractProgress.Reset();
    }

    [Test]
    public void SameSeedSameUnlocks_SameOffers()
    {
        var act = new ActProgress();
        var a = ContractCatalog.Offers(42, act);
        var b = ContractCatalog.Offers(42, act);
        Assert.AreEqual(2, a.Length);
        Assert.AreEqual(2, b.Length);
        for (int i = 0; i < 2; i++)
        {
            Assert.AreEqual(a[i].id, b[i].id);
            Assert.AreEqual(a[i].type, b[i].type);
            Assert.AreEqual(a[i].zone, b[i].zone);
            Assert.AreEqual(a[i].position, b[i].position);
            Assert.AreEqual(a[i].gold, b[i].gold);
        }
    }

    [Test]
    public void Offers_OneHuntOneRecovery_InUnlockedZones()
    {
        var act = new ActProgress();
        var offers = ContractCatalog.Offers(7, act);
        Assert.AreEqual(2, offers.Length);
        Assert.AreNotEqual(offers[0].type, offers[1].type);
        Assert.IsTrue(act.IsUnlocked(offers[0].zone));
        Assert.IsTrue(act.IsUnlocked(offers[1].zone));
        Assert.IsTrue(ContractCatalog.IsValidTarget(offers[0].position, offers[0].zone));
        Assert.Greater(offers[0].gold, 0);
    }

    [Test]
    public void LockedZones_NeverOffered()
    {
        var act = new ActProgress(); // only Town + Ossuary open
        for (int seed = 0; seed < 25; seed++)
            foreach (var o in ContractCatalog.Offers(seed, act))
                Assert.IsTrue(act.IsUnlocked(o.zone), $"seed {seed} offered locked zone {o.zone}");
    }

    [Test]
    public void Accept_TwiceFails_OnePerRun()
    {
        var offers = ContractCatalog.Offers(3, new ActProgress());
        Assert.IsTrue(ContractProgress.TryAccept(offers[0]));
        Assert.IsFalse(ContractProgress.TryAccept(offers[1]));
        Assert.AreEqual(offers[0].id, ContractProgress.ActiveOffer.id);
    }

    [Test]
    public void Complete_WrongIdFails_RightIdCompletes()
    {
        var offers = ContractCatalog.Offers(3, new ActProgress());
        Assert.IsTrue(ContractProgress.TryAccept(offers[0]));
        Assert.IsFalse(ContractProgress.TryComplete("contract-3-nope"));
        Assert.AreEqual(ContractStatus.Active, ContractProgress.Status);
        Assert.IsTrue(ContractProgress.TryComplete(offers[0].id));
        Assert.AreEqual(ContractStatus.Complete, ContractProgress.Status);
        Assert.IsFalse(ContractProgress.TryComplete(offers[0].id));
    }

    [Test]
    public void ClaimEndsBoardForRun_NoSecondAccept()
    {
        var offers = ContractCatalog.Offers(3, new ActProgress());
        Assert.IsTrue(ContractProgress.TryAccept(offers[0]));
        Assert.IsTrue(ContractProgress.TryComplete(offers[0].id));
        Assert.IsTrue(ContractProgress.MarkClaimed());
        Assert.AreEqual(ContractStatus.Claimed, ContractProgress.Status);
        Assert.IsFalse(ContractProgress.TryAccept(offers[1]));
    }

    [Test]
    public void Reload_RestoresActivePosition_UnlockChangeKeepsOffer()
    {
        RunProfile.RunSeed = 99;
        var offers = ContractCatalog.Offers(RunProfile.RunSeed, RunProfile.Act);
        Assert.IsTrue(ContractProgress.TryAccept(offers[0]));
        var snap = new ContractSnapshot();
        ContractProgress.ToSnapshot(snap);

        // Simulate reload with a changed unlock stand: the frozen offer survives verbatim.
        RunProfile.Reset();
        ContractProgress.Reset();
        ContractProgress.FromSnapshot(snap);
        Assert.IsTrue(ContractProgress.HasContract);
        Assert.AreEqual(offers[0].id, ContractProgress.ActiveOffer.id);
        Assert.AreEqual(offers[0].position, ContractProgress.ActiveOffer.position);
        Assert.AreEqual(offers[0].gold, ContractProgress.ActiveOffer.gold);
        Assert.AreEqual(ContractStatus.Active, ContractProgress.Status);
    }

    [Test]
    public void Snapshot_EmptyWhenNoContract()
    {
        var snap = new ContractSnapshot();
        ContractProgress.ToSnapshot(snap);
        Assert.IsFalse(snap.hasContract);
        ContractProgress.FromSnapshot(snap);
        Assert.IsFalse(ContractProgress.HasContract);
    }
}
