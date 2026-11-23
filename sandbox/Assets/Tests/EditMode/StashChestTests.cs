using NUnit.Framework;
using UnityEngine;
using UnityEditor.SceneManagement;

/// <summary>replayability S5: Lantern Hold stash chest, stash window, result difficulty.</summary>
public class StashChestTests
{
    GameObject hero;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        hero = new GameObject("StashHero");
        hero.AddComponent<Inventory>();
    }

    [TearDown]
    public void Teardown()
    {
        if (hero != null) Object.DestroyImmediate(hero);
        DiabloClickController.ExternalDrive = false;
        RunProfile.Reset();
    }

    [Test]
    public void StoreAndTake_MoveItemsBetweenBagAndStash()
    {
        var bag = hero.GetComponent<Inventory>();
        var stash = new Stash();
        Assert.IsTrue(bag.Add(ItemDatabase.CreateUnique("Seer's Circlet")));
        Assert.IsTrue(bag.Add(ItemDatabase.CreateStarterSword()));

        Assert.IsTrue(StashChest.Store(bag, stash, 0));
        Assert.AreEqual(1, bag.Count);
        Assert.AreEqual(1, stash.Count);
        Assert.AreEqual("Seer's Circlet", stash.Get(0).uniqueId);

        Assert.IsTrue(StashChest.Take(stash, bag, 0));
        Assert.AreEqual(2, bag.Count);
        Assert.AreEqual(0, stash.Count);
        Assert.IsFalse(StashChest.Store(bag, stash, 9), "empty bag index");
        Assert.IsFalse(StashChest.Take(stash, bag, 0), "empty stash index");
    }

    [Test]
    public void Store_RefusesWhenStashFull_TakeRefusesWhenBagFull()
    {
        var bag = hero.GetComponent<Inventory>();
        var stash = new Stash();
        for (int i = 0; i < Stash.Capacity; i++) stash.Add(ItemDatabase.CreateStarterSword());
        bag.Add(ItemDatabase.CreateStarterSword());
        Assert.IsFalse(StashChest.Store(bag, stash, 0));
        Assert.AreEqual(1, bag.Count, "item stays in the bag");

        for (int i = bag.Count; i < Inventory.Capacity; i++) bag.Add(ItemDatabase.CreateStarterSword());
        Assert.IsFalse(StashChest.Take(stash, bag, 0));
        Assert.AreEqual(Stash.Capacity, stash.Count, "item stays in the stash");
    }

    [Test]
    public void InteractRadius_MatchesVendorReach()
    {
        Assert.IsTrue(StashChest.CanInteract(new Vector3(0f, 0f, 0f), new Vector3(3f, 0f, 0f)));
        Assert.IsFalse(StashChest.CanInteract(new Vector3(0f, 0f, 0f), new Vector3(4f, 0f, 0f)));
    }

    [Test]
    public void StashWindow_UsesRunProfileStash()
    {
        var bag = hero.GetComponent<Inventory>();
        bag.Add(ItemDatabase.CreateUnique("Emberbrand Blade"));
        var ui = hero.AddComponent<DiabloStashUI>();
        Assert.IsTrue(ui.StoreBagIndex(0));
        Assert.AreEqual(1, RunProfile.Stash.Count);
        StringAssert.Contains("Truhe 1 von 40", ui.CountLine());
        Assert.IsTrue(ui.TakeStashIndex(0));
        Assert.AreEqual(0, RunProfile.Stash.Count);
        Assert.AreEqual(1, bag.Count);
    }

    [Test]
    public void Scene_HasStashChestInLanternHold()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/DiabloZone.unity", OpenSceneMode.Single);
        var chest = GameObject.Find(StashChest.ObjectName);
        Assert.IsNotNull(chest);
        Assert.IsNotNull(chest.GetComponent<StashChest>());
        Assert.IsNotNull(chest.GetComponent<Collider>());
        var vendor = GameObject.Find(Vendor.VendorObjectName);
        Assert.Less(Vector3.Distance(chest.transform.position, vendor.transform.position), 6f, "chest stands in the camp");
    }

    [Test]
    public void AutoplayResult_CarriesDifficulty()
    {
        var r = new AutoplayResult();
        Assert.AreEqual("Normal", r.difficulty);
        r.difficulty = DifficultyProfile.Kindled.name;
        StringAssert.Contains("\"difficulty\": \"Kindled\"", r.ToJson());
    }
}
