using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>S2 PlayMode: first-Kindled-victory claim lifecycle.</summary>
public class KindledFinaleRewardTests
{
    GameObject hero;
    string path;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        RunProfile.DifficultyUnlocked = true;
        RunProfile.Difficulty = RunProfile.Kindled;
        SessionComplete.ResetRun();
        KindledFinaleReward.ResetRun();
        hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = DiabloNetworkPlayer.HeroName;
        hero.AddComponent<DiabloProgression>();
        hero.AddComponent<Equipment>();
        hero.AddComponent<Inventory>();
        hero.AddComponent<Wallet>();
        hero.AddComponent<PlayerHealth>();
        path = Path.Combine(Path.GetTempPath(), $"kindled-reward-{System.Guid.NewGuid():N}.json");
    }

    [TearDown]
    public void TearDown()
    {
        DiabloTestFixture.CleanupTestScene(hero);
        foreach (var f in new[] { path, path + ".tmp" }) if (File.Exists(f)) File.Delete(f);
        if (SaveService.HasSave()) SaveService.DeleteSave();
        RunProfile.Reset();
        SessionComplete.ResetRun();
        KindledFinaleReward.ResetRun();
    }

    [UnityTest]
    public IEnumerator Fenwalker_GetsCompatibleRareWeapon()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        Assert.IsTrue(KindledFinaleReward.OnKilnmotherKill(hero, path));
        var inv = hero.GetComponent<Inventory>();
        Assert.AreEqual(1, inv.Count);
        var w = inv.Get(0);
        Assert.AreEqual(ItemSlot.Weapon, w.slot);
        Assert.AreEqual(ItemRarity.Rare, w.rarity);
        Assert.IsTrue(Equipment.CanEquip(w, ClassCatalog.FenwalkerId));
        Assert.Greater(w.GetAffix("Damage"), 0f, "Kindled affixes present");
        Assert.IsTrue(RunProfile.KindledRewardGranted);
        Assert.IsFalse(RunProfile.KindledRewardPending);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Lanternbearer_GetsCompatibleRareWeapon()
    {
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        Assert.IsTrue(KindledFinaleReward.OnKilnmotherKill(hero, path));
        var w = hero.GetComponent<Inventory>().Get(0);
        Assert.AreEqual(ItemRarity.Rare, w.rarity);
        Assert.IsTrue(Equipment.CanEquip(w, ClassCatalog.LanternbearerId));
        Assert.IsFalse(Equipment.CanEquip(w, ClassCatalog.FenwalkerId), "not the other class weapon");
        yield return null;
    }

    [UnityTest]
    public IEnumerator FullBag_KeepsClaimOpen_ThenDelivers()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        var inv = hero.GetComponent<Inventory>();
        for (int i = 0; i < Inventory.Capacity; i++) inv.Add(ItemDatabase.CreateStarterSword());
        Assert.IsFalse(KindledFinaleReward.OnKilnmotherKill(hero, path), "full bag: claim stays open");
        Assert.IsTrue(RunProfile.KindledRewardPending);
        Assert.IsFalse(RunProfile.KindledRewardGranted);
        inv.RemoveAt(0);
        KindledFinaleReward.OfferOnSessionScreen(hero, path);
        Assert.IsFalse(RunProfile.KindledRewardPending);
        Assert.IsTrue(RunProfile.KindledRewardGranted);
        Assert.AreEqual(Inventory.Capacity, inv.Count);
        Assert.AreEqual(ItemRarity.Rare, inv.Get(inv.Count - 1).rarity);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SaveFailure_RollsBack()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        KindledFinaleReward.ForceSaveFailure = true;
        Assert.IsFalse(KindledFinaleReward.OnKilnmotherKill(hero, path));
        Assert.IsTrue(RunProfile.KindledRewardPending, "claim preserved");
        Assert.IsFalse(RunProfile.KindledRewardGranted);
        Assert.AreEqual(0, hero.GetComponent<Inventory>().Count, "payout rolled back");
        KindledFinaleReward.ForceSaveFailure = false;
        Assert.IsTrue(KindledFinaleReward.TryDeliver(hero, path), "retry after failure works");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Reload_OffersAgain()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        var inv = hero.GetComponent<Inventory>();
        for (int i = 0; i < Inventory.Capacity; i++) inv.Add(ItemDatabase.CreateStarterSword());
        KindledFinaleReward.OnKilnmotherKill(hero, path);
        Assert.IsTrue(RunProfile.KindledRewardPending);
        // Reload: pending claim survives, bag is full again after load.
        RunProfile.Reset();
        var fresh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        fresh.name = "Fresh";
        fresh.AddComponent<DiabloProgression>();
        fresh.AddComponent<Equipment>();
        fresh.AddComponent<Inventory>();
        fresh.AddComponent<Wallet>();
        Assert.IsTrue(SaveService.TryLoad(fresh, path));
        Object.DestroyImmediate(fresh);
        RunProfile.DifficultyUnlocked = true;
        RunProfile.Difficulty = RunProfile.Kindled;
        Assert.IsTrue(RunProfile.KindledRewardPending, "reload keeps the claim");
        inv.RemoveAt(0);
        KindledFinaleReward.OfferOnSessionScreen(hero, path);
        Assert.IsTrue(RunProfile.KindledRewardGranted);
        yield return null;
    }

    [UnityTest]
    public IEnumerator RepeatedBossKill_NoDuplicate()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        Assert.IsTrue(KindledFinaleReward.OnKilnmotherKill(hero, path));
        Assert.IsFalse(KindledFinaleReward.OnKilnmotherKill(hero, path), "second kill grants nothing");
        Assert.AreEqual(1, hero.GetComponent<Inventory>().Count);
        Assert.IsTrue(RunProfile.KindledRewardGranted);
        yield return null;
    }

    [UnityTest]
    public IEnumerator OldCompletedRun_NoRetroactiveClaim()
    {
        RunProfile.Difficulty = RunProfile.Normal;
        RunProfile.DifficultyUnlocked = true;
        RunProfile.Act.Unlock(ActProgress.Kiln);
        RunProfile.Act.CompleteAct();
        Assert.IsFalse(KindledFinaleReward.OnKilnmotherKill(hero, path));
        Assert.IsFalse(RunProfile.KindledRewardPending);
        Assert.IsFalse(RunProfile.KindledRewardGranted);
        yield return null;
    }
}
