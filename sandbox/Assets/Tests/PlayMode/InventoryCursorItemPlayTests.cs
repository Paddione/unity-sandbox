using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;

/// <summary>
/// cinderreach-inventory-cursor-item: a click lifts an item onto the cursor,
/// the next click places it (bag move/swap, equip, unequip); clicking outside,
/// Esc-equivalent release or closing the panel puts it back. Nothing is lost.
/// </summary>
public sealed class InventoryCursorItemPlayTests
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
        hero = new GameObject("CursorHero");
        equipment = hero.AddComponent<Equipment>();
        inventory = hero.AddComponent<Inventory>();
        hero.AddComponent<PlayerHealth>();
        hero.AddComponent<Wallet>();
        ui = hero.AddComponent<DiabloInventoryUI>();
        yield return null;
        ui.SetOpen(true);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (hero != null) Object.Destroy(hero);
        if (eventSys != null) Object.Destroy(eventSys);
        RunProfile.Reset();
        yield return null;
    }

    static ItemInstance Gear(ItemSlot slot, string baseType = null)
    {
        var item = new ItemInstance
        {
            slot = slot,
            rarity = ItemRarity.Rare,
            baseType = baseType ?? ItemDatabase.GetBaseTypes(slot)[0],
            affixes = new List<Affix>(),
        };
        item.EnsureIdentity();
        return item;
    }

    int TotalItems()
    {
        int n = inventory.Count;
        foreach (ItemSlot s in System.Enum.GetValues(typeof(ItemSlot)))
            if (Equipment.IsGearSlot(s) && equipment.GetEquipped(s) != null) n++;
        return n;
    }

    [UnityTest]
    public IEnumerator Click_LiftsItem_GhostFollows_ClickEmptyCellMovesToEnd()
    {
        var a = Gear(ItemSlot.Helmet);
        var b = Gear(ItemSlot.Gloves);
        inventory.Add(a);
        inventory.Add(b);
        ui.Refresh();
        ui.ClickBag(0);
        Assert.IsTrue(ui.IsHolding, "click lifts the item");
        Assert.AreSame(a, ui.HeldItem);
        var ghost = hero.transform.Find("InventoryCanvas/DragGhost");
        Assert.IsNotNull(ghost);
        Assert.IsTrue(ghost.gameObject.activeSelf, "the icon rides the cursor");
        Assert.AreEqual(2, inventory.Count, "item stays in the bag while held");
        yield return null;
        ui.ClickBag(-1); // empty cell
        Assert.IsFalse(ui.IsHolding);
        Assert.AreSame(b, inventory.Get(0));
        Assert.AreSame(a, inventory.Get(1), "placed on an empty cell: moves behind the last item");
        Assert.IsFalse(ghost.gameObject.activeSelf);
    }

    [UnityTest]
    public IEnumerator ClickOccupiedCell_Swaps_DisplacedItemRidesCursor()
    {
        var a = Gear(ItemSlot.Helmet);
        var b = Gear(ItemSlot.Gloves);
        var c = Gear(ItemSlot.Boots);
        inventory.Add(a); inventory.Add(b); inventory.Add(c);
        ui.Refresh();
        ui.ClickBag(0);
        ui.ClickBag(2);
        Assert.AreSame(c, inventory.Get(0));
        Assert.AreSame(a, inventory.Get(2));
        Assert.IsTrue(ui.IsHolding, "the displaced item is now on the cursor");
        Assert.AreSame(c, ui.HeldItem);
        ui.ClickBag(0); // put it back where it is
        Assert.IsFalse(ui.IsHolding);
        Assert.AreEqual(3, TotalItems());
        yield return null;
    }

    [UnityTest]
    public IEnumerator HeldBagItem_OnMatchingSlot_Equips_WrongSlotRejects()
    {
        var helm = Gear(ItemSlot.Helmet);
        inventory.Add(helm);
        ui.Refresh();
        ui.ClickBag(0);
        ui.ClickSlot(ItemSlot.Boots);
        Assert.AreEqual(ItemSlot.Boots, ui.LastRejectedSlot, "wrong slot flashes red");
        Assert.IsTrue(ui.IsHolding, "rejected item stays on the cursor");
        ui.ClickSlot(ItemSlot.Helmet);
        Assert.AreSame(helm, equipment.GetEquipped(ItemSlot.Helmet), "click on the right slot equips");
        Assert.IsFalse(ui.IsHolding);
        Assert.AreEqual(0, inventory.Count);
        yield return null;
    }

    [UnityTest]
    public IEnumerator HeldBagItem_OnHero_EquipsIntoOwnSlot_SwappedItemRidesCursor()
    {
        var oldHelm = Gear(ItemSlot.Helmet);
        equipment.Equip(oldHelm);
        var newHelm = Gear(ItemSlot.Helmet);
        inventory.Add(newHelm);
        ui.Refresh();
        ui.ClickBag(0);
        ui.ClickDoll();
        Assert.AreSame(newHelm, equipment.GetEquipped(ItemSlot.Helmet));
        Assert.IsTrue(ui.IsHolding, "the previously worn helmet rides the cursor");
        Assert.AreSame(oldHelm, ui.HeldItem);
        ui.ClickOutside();
        Assert.IsFalse(ui.IsHolding);
        Assert.AreEqual(2, TotalItems());
        yield return null;
    }

    [UnityTest]
    public IEnumerator WornItem_LiftAndClickBag_Unequips()
    {
        var gloves = Gear(ItemSlot.Gloves);
        equipment.Equip(gloves);
        ui.Refresh();
        ui.ClickSlot(ItemSlot.Gloves);
        Assert.IsTrue(ui.IsHolding);
        Assert.AreSame(gloves, ui.HeldItem);
        ui.ClickBag(-1);
        Assert.IsNull(equipment.GetEquipped(ItemSlot.Gloves));
        Assert.AreSame(gloves, inventory.Get(0));
        Assert.IsFalse(ui.IsHolding);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ClickOutside_AndClosing_PutItemBack_NothingLost()
    {
        var a = Gear(ItemSlot.Armor);
        var worn = Gear(ItemSlot.Belt);
        inventory.Add(a);
        equipment.Equip(worn);
        ui.Refresh();
        int before = TotalItems();

        ui.ClickBag(0);
        ui.ClickOutside();
        Assert.IsFalse(ui.IsHolding);
        Assert.AreSame(a, inventory.Get(0), "outside click: back to its cell");

        ui.ClickSlot(ItemSlot.Belt);
        Assert.IsTrue(ui.IsHolding);
        ui.SetOpen(false);
        Assert.IsFalse(ui.IsHolding, "closing the panel releases the item");
        Assert.AreSame(worn, equipment.GetEquipped(ItemSlot.Belt));
        Assert.AreEqual(before, TotalItems());
        yield return null;
    }

    [UnityTest]
    public IEnumerator OutsideBackdrop_OnlyActiveWhileHolding()
    {
        inventory.Add(Gear(ItemSlot.Helmet));
        ui.Refresh();
        ui.ClickBag(0);
        var backdrop = hero.transform.Find("InventoryCanvas/CursorBackdrop");
        Assert.IsNotNull(backdrop, "outside-click catcher exists");
        Assert.IsTrue(backdrop.gameObject.activeSelf);
        ui.ClickOutside();
        Assert.IsFalse(backdrop.gameObject.activeSelf, "no input blocking without a held item");
        yield return null;
    }
}
