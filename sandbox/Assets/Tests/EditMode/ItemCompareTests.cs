using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Shift-compare model and tooltip (cinderreach-inventory-paperdoll): per-stat
/// trends against the worn item, arrows and colors, empty slots and potions.
/// </summary>
public class ItemCompareTests
{
    static ItemInstance Item(ItemSlot slot, params (string key, float value)[] affixes)
    {
        var item = new ItemInstance { slot = slot, rarity = ItemRarity.Rare, baseType = "Test", itemLevel = 3 };
        foreach (var (k, v) in affixes) item.affixes.Add(new Affix(k, v));
        return item;
    }

    [TearDown]
    public void TearDown() => ItemTooltip.ShiftOverride = null;

    [Test]
    public void Diff_MarksBetterWorseAndSamePerStat()
    {
        var worn = Item(ItemSlot.Weapon, (AffixLibrary.Damage, 3), (AffixLibrary.Crit, 5), (AffixLibrary.MaxHP, 10));
        var candidate = Item(ItemSlot.Weapon, (AffixLibrary.Damage, 5), (AffixLibrary.LifeSteal, 4), (AffixLibrary.MaxHP, 10));
        var diff = ItemCompare.Diff(candidate, worn);
        StatTrend TrendOf(string key) => diff.Find(d => d.key == key).Trend;
        Assert.AreEqual(4, diff.Count, "Union of both items' affix keys.");
        Assert.AreEqual(StatTrend.Better, TrendOf(AffixLibrary.Damage));
        Assert.AreEqual(StatTrend.Better, TrendOf(AffixLibrary.LifeSteal));
        Assert.AreEqual(StatTrend.Worse, TrendOf(AffixLibrary.Crit));
        Assert.AreEqual(StatTrend.Same, TrendOf(AffixLibrary.MaxHP));
        Assert.AreEqual(2f, diff.Find(d => d.key == AffixLibrary.Damage).Delta, 0.001f);
        Assert.AreEqual(1, ItemCompare.Score(candidate, worn), "Two gains, one loss.");
    }

    [Test]
    public void Diff_EmptySlotMakesEveryRolledStatBetter()
    {
        var candidate = Item(ItemSlot.Helmet, (AffixLibrary.Armor, 6), (AffixLibrary.GoldFind, 12));
        foreach (var d in ItemCompare.Diff(candidate, null))
            Assert.AreEqual(StatTrend.Better, d.Trend, d.key);
    }

    [Test]
    public void ArrowLines_GreenUpForGainsRedDownForLossesNoArrowWhenEqual()
    {
        var worn = Item(ItemSlot.Weapon, (AffixLibrary.Damage, 3), (AffixLibrary.Crit, 5), (AffixLibrary.MaxHP, 10));
        var candidate = Item(ItemSlot.Weapon, (AffixLibrary.Damage, 5), (AffixLibrary.MaxHP, 10));
        var lines = ItemCompare.ArrowLines(candidate, worn).Split('\n');
        Assert.AreEqual(3, lines.Length);
        string Line(string stat) => System.Array.Find(lines, l => l.Contains(AffixLibrary.StatName(stat)));
        StringAssert.StartsWith($"<color={DiabloInventoryUI.BetterColor}>{ItemCompare.UpArrow}", Line(AffixLibrary.Damage));
        StringAssert.Contains("(+2)", Line(AffixLibrary.Damage));
        StringAssert.StartsWith($"<color={DiabloInventoryUI.WorseColor}>{ItemCompare.DownArrow}", Line(AffixLibrary.Crit));
        StringAssert.Contains("(-5)", Line(AffixLibrary.Crit));
        StringAssert.DoesNotContain(ItemCompare.UpArrow, Line(AffixLibrary.MaxHP));
        StringAssert.DoesNotContain(ItemCompare.DownArrow, Line(AffixLibrary.MaxHP));
        Assert.AreEqual(Loc.T("inv.nobonus"), ItemCompare.ArrowLines(Item(ItemSlot.Helmet), null));
    }

    [Test]
    public void Tooltip_ShiftAddsEquippedCardAndArrows()
    {
        var hero = new GameObject("HeroTip");
        var eq = hero.AddComponent<Equipment>();
        var worn = Item(ItemSlot.Weapon, (AffixLibrary.Damage, 3), (AffixLibrary.Crit, 5));
        eq.Equip(worn);
        var candidate = Item(ItemSlot.Weapon, (AffixLibrary.Damage, 7));
        var tip = ItemTooltip.For(hero);

        ItemTooltip.ShiftOverride = false;
        tip.Show(candidate, eq);
        Assert.IsTrue(tip.IsVisible);
        Assert.IsFalse(tip.IsComparing);
        Assert.AreEqual("", tip.EquippedText, "No equipped card without Shift.");
        StringAssert.DoesNotContain(ItemCompare.UpArrow, tip.HoverText);

        ItemTooltip.ShiftOverride = true;
        tip.Render();
        Assert.IsTrue(tip.IsComparing);
        StringAssert.Contains(AffixLibrary.Describe(new Affix(AffixLibrary.Crit, 5)), tip.EquippedText);
        StringAssert.Contains(ItemCompare.UpArrow, tip.HoverText);
        StringAssert.Contains(ItemCompare.DownArrow, tip.HoverText);

        // Hovering the worn item itself never compares against itself.
        tip.Show(worn, eq);
        Assert.IsFalse(tip.IsComparing);

        // Potions have no slot to compare against.
        tip.Show(ItemDatabase.CreateHealingPotion(), eq);
        Assert.IsFalse(tip.IsComparing);

        // Empty slot: the equipped card explains it, every stat is a gain.
        var helm = Item(ItemSlot.Helmet, (AffixLibrary.Armor, 4));
        tip.Show(helm, eq);
        Assert.IsTrue(tip.IsComparing);
        Assert.AreEqual(Loc.T("inv.tip.emptyslot"), tip.EquippedText.Substring(tip.EquippedText.Length - Loc.T("inv.tip.emptyslot").Length));
        StringAssert.Contains(ItemCompare.UpArrow, tip.HoverText);

        tip.Hide();
        Assert.IsFalse(tip.IsVisible);
        Object.DestroyImmediate(hero);
    }

    [Test]
    public void InventoryUI_ShowsGearAndBagOnOnePage()
    {
        var go = new GameObject("HeroCombined");
        go.AddComponent<Equipment>();
        go.AddComponent<Inventory>();
        var ui = go.AddComponent<DiabloInventoryUI>();
        ui.SetOpen(true);
        foreach (var tab in new[] { DiabloInventoryUI.MenuTab.Inventory, DiabloInventoryUI.MenuTab.Bag })
        {
            ui.ShowTab(tab);
            var page = go.transform.Find("InventoryCanvas/Panel/InventoryPage");
            Assert.IsNotNull(page);
            Assert.IsTrue(page.Find("CharacterColumn/Paperdoll/EquipFrame0").gameObject.activeInHierarchy, "Slots visible on " + tab);
            Assert.IsTrue(page.Find("BagColumn/BagGrid").gameObject.activeInHierarchy, "Bag visible on " + tab);
            Assert.IsNotNull(page.Find("CharacterColumn/Paperdoll/PaperdollImage").GetComponent<HeroPaperdollPreview>());
        }
        ui.ShowTab(DiabloInventoryUI.MenuTab.Stats);
        Assert.IsFalse(go.transform.Find("InventoryCanvas/Panel/InventoryPage").gameObject.activeSelf);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void InventoryUI_WrongSlotDropIsRefusedWithoutStateChange()
    {
        var go = new GameObject("HeroDrop");
        var eq = go.AddComponent<Equipment>();
        var inv = go.AddComponent<Inventory>();
        var ui = go.AddComponent<DiabloInventoryUI>();
        ui.SetOpen(true);
        var helm = Item(ItemSlot.Helmet, (AffixLibrary.Armor, 4));
        inv.Add(helm);
        Assert.IsFalse(ui.DropBagOnSlot(0, ItemSlot.Weapon));
        Assert.AreEqual(ItemSlot.Weapon, ui.LastRejectedSlot);
        Assert.IsNull(eq.weapon);
        Assert.IsNull(eq.helmet);
        Assert.AreSame(helm, inv.Get(0));
        StringAssert.Contains(ItemText.Slot(ItemSlot.Weapon), ui.Notice);

        Assert.IsTrue(ui.DropBagOnSlot(0, ItemSlot.Helmet));
        Assert.AreSame(helm, eq.helmet);
        Assert.IsNull(ui.LastRejectedSlot);
        Assert.AreEqual(0, inv.Count);
        Object.DestroyImmediate(go);
    }
}
