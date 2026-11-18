using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// risk-shrines S1: one-shot states, v4 → v8 migration with Unchosen default,
/// new-run reset, and the atomic payout (item + claim together, write-failure
/// rollback, full bag keeps the claim).
/// </summary>
public class RiskShrineProgressTests
{
    GameObject hero;
    string path;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        path = Path.Combine(Path.GetTempPath(), $"cinderreach-risk-{System.Guid.NewGuid():N}.json");
        hero = new GameObject("Player");
        hero.AddComponent<Equipment>();
        hero.AddComponent<Inventory>();
        hero.AddComponent<Wallet>();
        hero.AddComponent<DiabloProgression>();
        hero.AddComponent<PlayerHealth>();
    }

    [TearDown]
    public void Teardown()
    {
        if (hero != null) Object.DestroyImmediate(hero);
        Wallet.ResetRun();
        if (path != null && File.Exists(path)) File.Delete(path);
        RunProfile.Reset();
    }

    static void FillBag(GameObject h)
    {
        var inv = h.GetComponent<Inventory>();
        for (int i = 0; i < Inventory.Capacity; i++)
            Assert.IsTrue(inv.Add(ItemDatabase.CreateStarterSword()), $"slot {i}");
    }

    [Test]
    public void FreshRun_StartsUnchosen()
    {
        Assert.AreEqual(RiskShrineState.Unchosen, RunProfile.RiskShrine.State);
        Assert.IsTrue(RunProfile.RiskShrine.CanChoose);
    }

    [Test]
    public void V4Save_MigratesToV8_Unchosen()
    {
        const string v4 = @"{""v"":4,""level"":3,""xp"":10,""gold"":5,""zoneReached"":1,"
            + @"""difficulty"":""Normal"",""runSeed"":42,""classId"":""fenwalker"","
            + @"""highestUnlockedZone"":2,""actOneComplete"":false,""waypoints"":[]}";
        var outcome = SaveMigrations.Migrate(v4, out SaveGame save, out int from);
        Assert.AreEqual(SaveMigrations.Outcome.Migrated, outcome);
        Assert.AreEqual(4, from);
        Assert.AreEqual(8, save.v);
        Assert.AreEqual(SaveGame.CurrentVersion, save.v);
        Assert.AreEqual((int)RiskShrineState.Unchosen, save.riskShrineState);
        Assert.IsNull(save.riskReward);
        Assert.IsTrue(SaveService.TryLoad(hero, WriteTemp(v4)));
        Assert.AreEqual(RiskShrineState.Unchosen, RunProfile.RiskShrine.State);
    }

    string WriteTemp(string json)
    {
        File.WriteAllText(path, json);
        return path;
    }

    [Test]
    public void V1Save_LiftsStraightToV8()
    {
        const string v1 = @"{""v"":1,""level"":2,""xp"":5,""gold"":7,""zoneReached"":1}";
        var outcome = SaveMigrations.Migrate(v1, out SaveGame save, out _);
        Assert.AreEqual(SaveMigrations.Outcome.Migrated, outcome);
        Assert.AreEqual(8, save.v);
        Assert.AreEqual((int)RiskShrineState.Unchosen, save.riskShrineState);
    }

    [Test]
    public void Choice_IsOneShot()
    {
        Assert.IsTrue(RunProfile.RiskShrine.ChooseHeal());
        Assert.AreEqual(RiskShrineState.Healed, RunProfile.RiskShrine.State);
        Assert.IsFalse(RunProfile.RiskShrine.ChooseChallenge(1));
        Assert.IsFalse(RunProfile.RiskShrine.ChooseHeal());
    }

    [Test]
    public void ClaimRoundtrip_PersistsItemAndClaim()
    {
        Assert.IsTrue(RunProfile.RiskShrine.ChooseChallenge(99));
        RunProfile.RiskShrine.NotifyEliteKilled(RiskShrineProgress.RollRare(99));
        Assert.AreEqual(RiskShrineState.RewardAvailable, RunProfile.RiskShrine.State);
        Assert.IsTrue(RunProfile.RiskShrine.TryClaim(hero, () => SaveService.WriteSave(SaveService.CaptureState(hero), path)));
        Assert.AreEqual(RiskShrineState.RewardClaimed, RunProfile.RiskShrine.State);
        Assert.AreEqual(1, hero.GetComponent<Inventory>().Count);

        RunProfile.Reset();
        Assert.IsTrue(SaveService.TryLoad(hero, path));
        Assert.AreEqual(RiskShrineState.RewardClaimed, RunProfile.RiskShrine.State);
        Assert.AreEqual(1, hero.GetComponent<Inventory>().Count);
    }

    [Test]
    public void WriteFailure_RollsBackBagAndKeepsClaim()
    {
        Assert.IsTrue(RunProfile.RiskShrine.ChooseChallenge(7));
        RunProfile.RiskShrine.NotifyEliteKilled(RiskShrineProgress.RollRare(7));
        bool ok = RunProfile.RiskShrine.TryClaim(hero, () => throw new IOException("disk full"));
        Assert.IsFalse(ok, "failed write must not consume the claim");
        Assert.AreEqual(RiskShrineState.RewardAvailable, RunProfile.RiskShrine.State);
        Assert.AreEqual(0, hero.GetComponent<Inventory>().Count, "provisional insert must roll back");
        Assert.IsNotNull(RunProfile.RiskShrine.PendingReward);
    }

    [Test]
    public void FullBag_KeepsClaimOpen()
    {
        FillBag(hero);
        Assert.IsTrue(RunProfile.RiskShrine.ChooseChallenge(11));
        RunProfile.RiskShrine.NotifyEliteKilled(RiskShrineProgress.RollRare(11));
        Assert.IsFalse(RunProfile.RiskShrine.TryClaim(hero, () => SaveService.WriteSave(SaveService.CaptureState(hero), path)));
        Assert.AreEqual(RiskShrineState.RewardAvailable, RunProfile.RiskShrine.State);
        Assert.IsNotNull(RunProfile.RiskShrine.PendingReward);
    }

    [Test]
    public void RollRare_IsDeterministicAndRare()
    {
        var a = RiskShrineProgress.RollRare(1234);
        var b = RiskShrineProgress.RollRare(1234);
        Assert.AreEqual(ItemRarity.Rare, a.rarity);
        Assert.AreEqual(a.slot, b.slot);
        Assert.AreEqual(a.baseType, b.baseType);
        Assert.AreEqual(a.affixes.Count, b.affixes.Count);
    }

    [Test]
    public void NewRun_ResetsChoice()
    {
        RunProfile.RiskShrine.ChooseHeal();
        RunProfile.Reset();
        Assert.AreEqual(RiskShrineState.Unchosen, RunProfile.RiskShrine.State);
        Assert.IsNull(RunProfile.RiskShrine.PendingReward);
    }
}
