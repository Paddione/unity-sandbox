using NUnit.Framework;
using UnityEngine;

/// <summary>Blood-moor tabbed menu: art, tabs, bag grid, settings store.</summary>
public class DiabloMenuTests
{
    static GameObject HeroWithBag()
    {
        var go = new GameObject("Player");
        go.AddComponent<Equipment>();
        go.AddComponent<Inventory>();
        return go;
    }

    [Test]
    public void Palette_RarityColorsDistinct()
    {
        var magic = DiabloPalette.Rarity(ItemRarity.Magic);
        var rare = DiabloPalette.Rarity(ItemRarity.Rare);
        var unique = DiabloPalette.Rarity(ItemRarity.Unique);
        Assert.AreNotEqual(magic, rare);
        Assert.AreNotEqual(rare, unique);
        Assert.AreNotEqual(magic, unique);
    }

    [Test]
    public void MenuArt_SpritesBuildHeadless()
    {
        Assert.IsNotNull(DiabloMenuArt.PanelBackground());
        Assert.IsNotNull(DiabloMenuArt.HeaderBanner());
        Assert.IsNotNull(DiabloMenuArt.TabBackground(true));
        Assert.IsNotNull(DiabloMenuArt.TabBackground(false));
        Assert.IsNotNull(DiabloMenuArt.SlotBackground(null));
        Assert.IsNotNull(DiabloMenuArt.SlotBackground(ItemRarity.Unique));
        foreach (var b in new[] { "ShortSword", "HeavyAxe", "Breastplate", "Helm", "Circlet" })
            Assert.IsNotNull(DiabloMenuArt.ItemIcon(b, ItemRarity.Rare), b);
        Assert.Greater(DiabloMenuArt.TestCacheCount, 0);
    }

    [Test]
    public void Settings_ClampsAndNames()
    {
        DiabloSettings.MasterVol = 2f;
        DiabloSettings.QualityLevel = 9;
        DiabloSettings.Clamp();
        Assert.AreEqual(1f, DiabloSettings.MasterVol);
        Assert.AreEqual(2, DiabloSettings.QualityLevel);
        Assert.AreEqual("High", DiabloSettings.QualityName);
        DiabloSettings.QualityLevel = 0;
        Assert.AreEqual("Low", DiabloSettings.QualityName);
        DiabloSettings.QualityLevel = 2; // restore default
        DiabloSettings.MasterVol = 0.8f;
    }

    [Test]
    public void Menu_TabsSwitchAndBagGridFills()
    {
        var go = HeroWithBag();
        var inv = go.GetComponent<Inventory>();
        inv.Add(ItemDatabase.CreateStarterSword());
        var ui = go.AddComponent<DiabloInventoryUI>();
        ui.SetOpen(true);
        Assert.IsTrue(ui.IsOpen);
        Assert.AreEqual(DiabloInventoryUI.MenuTab.Inventory, ui.CurrentTab);
        ui.ShowTab(DiabloInventoryUI.MenuTab.Bag);
        Assert.AreEqual(DiabloInventoryUI.MenuTab.Bag, ui.CurrentTab);
        ui.ShowTab(DiabloInventoryUI.MenuTab.Stats);
        ui.ShowTab(DiabloInventoryUI.MenuTab.Settings);
        Assert.AreEqual(DiabloInventoryUI.MenuTab.Settings, ui.CurrentTab);
        // Bag grid always holds all 20 cells (filled + empty frames).
        int cells = 0;
        foreach (Transform c in go.GetComponentsInChildren<Transform>(true))
            if (c.name.StartsWith("BagButton") || c.name == "BagEmpty") cells++;
        Assert.AreEqual(Inventory.Capacity, cells, "Bag grid must render all 20 cells.");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Menu_UnequipReturnsToBag()
    {
        var go = HeroWithBag();
        var inv = go.GetComponent<Inventory>();
        var eq = go.GetComponent<Equipment>();
        eq.Equip(ItemDatabase.CreateStarterSword());
        var ui = go.AddComponent<DiabloInventoryUI>();
        ui.SetOpen(true);
        ui.UnequipSlot((int)ItemSlot.Weapon);
        Assert.IsNull(eq.weapon);
        Assert.AreEqual(1, inv.Count);
        ui.EquipIndex(0);
        Assert.IsNotNull(eq.weapon);
        Object.DestroyImmediate(go);
    }
}
