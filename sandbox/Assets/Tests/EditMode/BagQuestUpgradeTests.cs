using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// cinderreach-bag-quest-upgrades: four side quests (quest:n1, quest:n9,
/// quest:n4, quest:n6) each add 20 bag slots, 20 → 100, never shrinking;
/// slots persist in save v17 and travel with the co-op item snapshot.
/// </summary>
public sealed class BagQuestUpgradeTests
{
    GameObject hero;

    [SetUp]
    public void SetUp()
    {
        RunProfile.Reset();
        hero = new GameObject("BagHero");
    }

    [TearDown]
    public void TearDown()
    {
        if (hero != null) Object.DestroyImmediate(hero);
        RunProfile.Reset();
    }

    [Test]
    public void Catalog_FourSideQuestsAcrossActs_CarryTheBagReward()
    {
        var bagQuests = StoryCatalog.All.Where(q => q.bagUpgrade).ToList();
        CollectionAssert.AreEquivalent(new[] { "n1", "n9", "n4", "n6" }, bagQuests.Select(q => q.id));
        Assert.IsTrue(bagQuests.All(q => !q.main), "only side quests");
        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, bagQuests.Select(q => q.act).Distinct());
        Assert.AreEqual(Inventory.MaxSlots, Inventory.SlotsFor(bagQuests.Count), "all four reach exactly 100");
    }

    [Test]
    public void Slots_GrowBy20_CapAt100_NeverShrink()
    {
        var inv = hero.AddComponent<Inventory>();
        Assert.AreEqual(20, inv.Slots);
        Assert.AreEqual(20, Inventory.SlotsFor(0));
        Assert.AreEqual(40, Inventory.SlotsFor(1));
        Assert.AreEqual(100, Inventory.SlotsFor(9), "cap");
        Assert.IsTrue(inv.RaiseSlots(Inventory.SlotsFor(2)));
        Assert.AreEqual(60, inv.Slots);
        Assert.IsFalse(inv.RaiseSlots(40), "never shrinks");
        Assert.IsFalse(inv.RaiseSlots(60), "same step twice grants nothing");
        Assert.AreEqual(60, inv.Slots);
        for (int i = 0; i < 60; i++) Assert.IsTrue(inv.Add(ItemDatabase.CreateStarterSword()));
        Assert.IsFalse(inv.Add(ItemDatabase.CreateStarterSword()), "61st item does not fit");
        inv.RaiseSlots(500);
        Assert.AreEqual(100, inv.Slots);
    }

    static StoryState StateWithDone(params string[] ids) =>
        StoryState.FromSave(ids.Select(id => new StoryQuestEntry { id = id, status = StoryStatus.Done }).ToList(), new List<string>());

    [Test]
    public void BagQuestsDone_CountsOnlyFinishedBagQuests()
    {
        Assert.AreEqual(0, StoryDirector.BagQuestsDone(StateWithDone("h1", "n2", "n3")));
        Assert.AreEqual(1, StoryDirector.BagQuestsDone(StateWithDone("n1", "n2")));
        Assert.AreEqual(4, StoryDirector.BagQuestsDone(StateWithDone("n1", "n9", "n4", "n6")));
        Assert.AreEqual(100, Inventory.SlotsFor(StoryDirector.BagQuestsDone(StateWithDone("n1", "n9", "n4", "n6"))));
    }

    [Test]
    public void Save_V16MigratesTo20Slots_V17RoundTripKeepsSlotsAndItems()
    {
        var v16 = new SaveGame { v = 16 };
        string json = JsonUtility.ToJson(v16).Replace("\"bagSlots\":20,", "").Replace(",\"bagSlots\":20", "");
        StringAssert.DoesNotContain("bagSlots", json, "simulated pre-v17 file");
        Assert.AreEqual(SaveMigrations.Outcome.Migrated, SaveMigrations.Migrate(json, out var migrated, out int from));
        Assert.AreEqual(16, from);
        Assert.AreEqual(17, migrated.v);
        Assert.AreEqual(Inventory.Capacity, migrated.bagSlots);

        hero.AddComponent<DiabloProgression>();
        hero.AddComponent<Equipment>();
        hero.AddComponent<Wallet>();
        var inv = hero.AddComponent<Inventory>();
        inv.RaiseSlots(60);
        for (int i = 0; i < 45; i++) inv.Add(ItemDatabase.CreateStarterSword());
        var save = SaveService.CaptureState(hero);
        Assert.AreEqual(60, save.bagSlots);
        var restored = JsonUtility.FromJson<SaveGame>(JsonUtility.ToJson(save));
        var other = new GameObject("BagHeroLoaded");
        try
        {
            other.AddComponent<Inventory>();
            SaveService.ApplyState(other, restored);
            var loaded = other.GetComponent<Inventory>();
            Assert.AreEqual(60, loaded.Slots, "slots survive save/load");
            Assert.AreEqual(45, loaded.Count, "items beyond 20 survive save/load");
        }
        finally { Object.DestroyImmediate(other); }
    }

    [Test]
    public void NetworkSnapshot_CarriesBagSlots()
    {
        var inv = hero.AddComponent<Inventory>();
        hero.AddComponent<Equipment>();
        hero.AddComponent<Wallet>();
        inv.RaiseSlots(80);
        var state = NetworkItemOwnership.Capture(hero, new Stash());
        Assert.AreEqual(80, state.bagSlots);
        var roundTrip = JsonUtility.FromJson<NetworkItemOwnership.State>(JsonUtility.ToJson(state));
        var joined = new GameObject("Joined").AddComponent<Inventory>();
        try
        {
            joined.RaiseSlots(roundTrip.bagSlots);
            Assert.AreEqual(80, joined.Slots);
            joined.RaiseSlots(0); // old clients send 0: stays
            Assert.AreEqual(80, joined.Slots);
        }
        finally { Object.DestroyImmediate(joined.gameObject); }
    }
}
