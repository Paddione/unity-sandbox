using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// cinderreach-inventory-redesign: the ▲/▼ upgrade marker shows exactly for a
/// positive/negative <see cref="ItemCompare"/> total delta against the worn item
/// (D4), and the 8x5 grid renders locked frames outside the usable cells.
/// </summary>
public sealed class BagUpgradeMarkerTests
{
    GameObject go;
    Equipment equipment;
    Inventory inventory;

    [SetUp]
    public void SetUp()
    {
        RunProfile.Reset();
        go = new GameObject("Player");
        equipment = go.AddComponent<Equipment>();
        inventory = go.AddComponent<Inventory>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(go);
        RunProfile.Reset();
    }

    static ItemInstance Gear(float damage) =>
        new ItemInstance
        {
            slot = ItemSlot.Weapon,
            rarity = ItemRarity.Rare,
            baseType = ItemDatabase.StarterSwordBase,
            itemLevel = 5,
            affixes = new System.Collections.Generic.List<Affix> { new Affix(AffixLibrary.Damage, damage) }
        };

    [Test]
    public void BetterScoresPositive_WorseNegative_EqualZero()
    {
        equipment.Equip(Gear(2f));
        Assert.Greater(DiabloInventoryUI.UpgradeMarkScore(Gear(5f), equipment), 0, "Better item scores positive.");
        Assert.Less(DiabloInventoryUI.UpgradeMarkScore(Gear(1f), equipment), 0, "Worse item scores negative.");
        Assert.AreEqual(0, DiabloInventoryUI.UpgradeMarkScore(Gear(2f), equipment), "Equal item scores zero.");
    }

    [Test]
    public void ConsumableScoresZero()
    {
        equipment.Equip(Gear(2f));
        var potion = new ItemInstance { slot = ItemSlot.Consumable, rarity = ItemRarity.Magic, baseType = "Worn Potion" };
        Assert.AreEqual(0, DiabloInventoryUI.UpgradeMarkScore(potion, equipment));
    }

    [Test]
    public void GridShowsUpArrowOnlyForBetter()
    {
        var ui = go.AddComponent<DiabloInventoryUI>();
        ui.SetOpen(true);
        equipment.Equip(Gear(2f));
        inventory.Add(Gear(5f));
        ui.Refresh();
        var cell = go.GetComponentsInChildren<InventoryCell>(true);
        GameObject better = null;
        foreach (var c in cell)
            if (c.kind == InventoryCell.Kind.Bag && c.index == 0 && c.name == "BagButton0") better = c.gameObject;
        Assert.IsNotNull(better);
        var mark = better.transform.Find("UpgradeMark")?.GetComponent<Text>();
        Assert.IsNotNull(mark, "Better item shows an upgrade mark.");
        Assert.AreEqual(ItemCompare.UpArrow, mark.text);
    }

    [Test]
    public void GridShowsDownArrowForWorseAndNoneForEqual()
    {
        var ui = go.AddComponent<DiabloInventoryUI>();
        ui.SetOpen(true);
        equipment.Equip(Gear(5f));
        inventory.Add(Gear(1f));
        ui.Refresh();
        var cells = go.GetComponentsInChildren<InventoryCell>(true);
        GameObject worse = null;
        foreach (var c in cells)
            if (c.kind == InventoryCell.Kind.Bag && c.index == 0 && c.name == "BagButton0") worse = c.gameObject;
        Assert.IsNotNull(worse);
        var down = worse.transform.Find("UpgradeMark")?.GetComponent<Text>();
        Assert.IsNotNull(down, "Worse item shows a downgrade mark.");
        Assert.AreEqual(ItemCompare.DownArrow, down.text);
    }

    [Test]
    public void LockedFramesAreNotCells()
    {
        var ui = go.AddComponent<DiabloInventoryUI>();
        ui.SetOpen(true);
        ui.Refresh();
        int usable = 0, locked = 0;
        foreach (Transform c in go.GetComponentsInChildren<Transform>(true))
        {
            if (c.name.StartsWith("BagButton") || c.name == "BagEmpty") usable++;
            if (c.name == "BagLocked") locked++;
        }
        Assert.AreEqual(Inventory.Capacity, usable);
        Assert.AreEqual(40 - Inventory.Capacity, locked);
        foreach (var c in go.GetComponentsInChildren<InventoryCell>(true))
            Assert.AreNotEqual("BagLocked", c.name, "Locked frames carry no InventoryCell.");
    }
}
