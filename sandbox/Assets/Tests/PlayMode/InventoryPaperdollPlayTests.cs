using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Combined inventory (cinderreach-inventory-paperdoll): live 3D hero mirrors
/// worn gear, right click swaps instantly, drag & drop equips into the
/// matching slot and refuses a wrong slot without changing state.
/// </summary>
public sealed class InventoryPaperdollPlayTests
{
    GameObject hero;
    GameObject eventSys;
    Equipment equipment;
    Inventory inventory;
    DiabloInventoryUI ui;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        RunProfile.Reset();
        if (EventSystem.current == null)
            eventSys = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        var source = Resources.Load<HeroArtSource>("CinderreachHeroArt");
        Assert.IsNotNull(source);
        hero = Object.Instantiate(source.LightClothing);
        hero.name = "PaperdollHero";
        equipment = hero.AddComponent<Equipment>();
        inventory = hero.AddComponent<Inventory>();
        hero.AddComponent<PlayerHealth>();
        var look = hero.AddComponent<ClassAppearance>();
        look.Apply(ClassCatalog.Fenwalker);
        ui = hero.AddComponent<DiabloInventoryUI>();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (ui != null) ui.SetOpen(false);
        var rig = ui != null && ui.Paperdoll != null ? ui.Paperdoll.Rig : null;
        if (hero != null) Object.Destroy(hero);
        if (rig != null) Object.Destroy(rig);
        if (eventSys != null) Object.Destroy(eventSys);
        RunProfile.Reset();
        yield return null;
    }

    static ItemInstance Item(ItemSlot slot, float damage)
    {
        var item = new ItemInstance { slot = slot, rarity = ItemRarity.Rare, baseType = "Test", itemLevel = 4 };
        item.affixes.Add(new Affix(AffixLibrary.Damage, damage));
        return item;
    }

    GameObject BagCell(int index) => hero.GetComponentsInChildren<InventoryCell>(true)
        .First(c => c.kind == InventoryCell.Kind.Bag && c.index == index && c.name == $"BagButton{index}").gameObject;

    GameObject SlotCell(ItemSlot slot) => hero.GetComponentsInChildren<InventoryCell>(true)
        .First(c => c.kind == InventoryCell.Kind.Equip && c.Slot == slot).gameObject;

    static PointerEventData Pointer(PointerEventData.InputButton button) =>
        new PointerEventData(EventSystem.current) { button = button };

    [UnityTest]
    public IEnumerator Paperdoll_RendersLiveHeroAndMirrorsWornUnique()
    {
        ui.SetOpen(true);
        yield return null;
        var doll = ui.Paperdoll;
        Assert.IsNotNull(doll);
        Assert.IsTrue(doll.IsLive, "Opening the inventory stands the hero clone on the stage.");
        Assert.IsNotNull(doll.Texture);
        Assert.AreSame(doll.Texture, doll.GetComponent<RawImage>().texture);
        Assert.AreNotSame(hero, doll.Rig);
        Assert.IsNull(doll.Rig.GetComponent<Equipment>(), "The clone carries no gameplay state.");

        var unique = ItemDatabase.UniqueIds.Select(ItemDatabase.CreateUnique).First(i => i.slot == ItemSlot.Helmet);
        inventory.Add(unique);
        ui.RightClickBag(0);
        Assert.AreSame(unique, equipment.helmet);
        yield return null;
        yield return null;
        Assert.IsTrue(doll.Rig.GetComponentsInChildren<Transform>().Any(t => t.name == unique.uniqueId),
            "The 3D hero wears the newly equipped unique.");

        ui.RightClickSlot(ItemSlot.Helmet);
        yield return null;
        yield return null;
        Assert.IsFalse(doll.Rig.GetComponentsInChildren<Transform>().Any(t => t.name == unique.uniqueId));

        ui.SetOpen(false);
        Assert.IsFalse(doll.IsLive, "Closing parks the rig and stops its camera.");
    }

    [UnityTest]
    public IEnumerator RightClick_SwapsBagItemWithWornAndBack()
    {
        var old = Item(ItemSlot.Weapon, 2);
        var better = Item(ItemSlot.Weapon, 6);
        equipment.Equip(old);
        inventory.Add(better);
        ui.SetOpen(true);
        yield return null;

        ExecuteEvents.Execute(BagCell(0), Pointer(PointerEventData.InputButton.Right), ExecuteEvents.pointerClickHandler);
        Assert.AreSame(better, equipment.weapon, "Right click wields the bag weapon.");
        Assert.AreEqual(1, inventory.Count);
        Assert.AreSame(old, inventory.Get(0), "The worn weapon swaps into the bag.");

        ExecuteEvents.Execute(SlotCell(ItemSlot.Weapon), Pointer(PointerEventData.InputButton.Right), ExecuteEvents.pointerClickHandler);
        Assert.IsNull(equipment.weapon, "Right click on a worn item puts it into the bag.");
        Assert.AreEqual(2, inventory.Count);

        inventory.Add(new ItemInstance { slot = ItemSlot.Consumable, rarity = ItemRarity.Rare, baseType = "Healing Potion" });
        ui.Refresh();
        yield return null;
        ExecuteEvents.Execute(BagCell(2), Pointer(PointerEventData.InputButton.Right), ExecuteEvents.pointerClickHandler);
        Assert.AreEqual(3, inventory.Count, "gear-slots S2: right click never drinks a bag consumable.");
        Assert.IsNull(equipment.helmet);
    }

    [UnityTest]
    public IEnumerator DragDrop_EquipsMatchingSlotAndRefusesWrongSlot()
    {
        var old = Item(ItemSlot.Armor, 1);
        var plate = Item(ItemSlot.Armor, 3);
        equipment.Equip(old);
        inventory.Add(plate);
        ui.SetOpen(true);
        yield return null;

        // Wrong slot: armor onto the weapon slot is refused, nothing moves.
        var cell = BagCell(0);
        ExecuteEvents.Execute(cell, Pointer(PointerEventData.InputButton.Left), ExecuteEvents.beginDragHandler);
        Assert.IsTrue(ui.IsDragging);
        ExecuteEvents.Execute(SlotCell(ItemSlot.Weapon), Pointer(PointerEventData.InputButton.Left), ExecuteEvents.dropHandler);
        ExecuteEvents.Execute(cell, Pointer(PointerEventData.InputButton.Left), ExecuteEvents.endDragHandler);
        Assert.IsFalse(ui.IsDragging);
        Assert.AreEqual(ItemSlot.Weapon, ui.LastRejectedSlot);
        Assert.IsNull(equipment.weapon);
        Assert.AreSame(old, equipment.armor);
        Assert.AreSame(plate, inventory.Get(0));
        yield return null;
        var weaponFrame = SlotCell(ItemSlot.Weapon).GetComponent<Image>();
        Assert.Greater(weaponFrame.color.r, weaponFrame.color.g + 0.3f, "The refused slot flashes red.");

        // Matching slot: equips and swaps the worn armor into the bag.
        cell = BagCell(0);
        ExecuteEvents.Execute(cell, Pointer(PointerEventData.InputButton.Left), ExecuteEvents.beginDragHandler);
        ExecuteEvents.Execute(SlotCell(ItemSlot.Armor), Pointer(PointerEventData.InputButton.Left), ExecuteEvents.dropHandler);
        ExecuteEvents.Execute(cell, Pointer(PointerEventData.InputButton.Left), ExecuteEvents.endDragHandler);
        Assert.AreSame(plate, equipment.armor);
        Assert.AreSame(old, inventory.Get(0));
        Assert.IsNull(ui.LastRejectedSlot);
        yield return null;

        // Worn item dragged onto the bag grid goes back into the bag.
        var slot = SlotCell(ItemSlot.Armor);
        var grid = hero.GetComponentsInChildren<InventoryCell>(true).First(c => c.name == "BagGrid").gameObject;
        ExecuteEvents.Execute(slot, Pointer(PointerEventData.InputButton.Left), ExecuteEvents.beginDragHandler);
        ExecuteEvents.Execute(grid, Pointer(PointerEventData.InputButton.Left), ExecuteEvents.dropHandler);
        ExecuteEvents.Execute(slot, Pointer(PointerEventData.InputButton.Left), ExecuteEvents.endDragHandler);
        Assert.IsNull(equipment.armor);
        Assert.AreEqual(2, inventory.Count);
    }

    [UnityTest]
    public IEnumerator Hover_ShowsTooltipAndShiftCompares()
    {
        equipment.Equip(Item(ItemSlot.Weapon, 2));
        inventory.Add(Item(ItemSlot.Weapon, 5));
        ui.SetOpen(true);
        yield return null;
        try
        {
            ItemTooltip.ShiftOverride = true;
            ExecuteEvents.Execute(BagCell(0), Pointer(PointerEventData.InputButton.Left), ExecuteEvents.pointerEnterHandler);
            var tip = hero.GetComponent<ItemTooltip>();
            Assert.IsNotNull(tip);
            Assert.IsTrue(tip.IsVisible);
            Assert.IsTrue(tip.IsComparing);
            StringAssert.Contains($"<color={DiabloInventoryUI.BetterColor}>{ItemCompare.UpArrow}", tip.HoverText);
            ExecuteEvents.Execute(BagCell(0), Pointer(PointerEventData.InputButton.Left), ExecuteEvents.pointerExitHandler);
            Assert.IsFalse(tip.IsVisible);
        }
        finally { ItemTooltip.ShiftOverride = null; }
    }
}
