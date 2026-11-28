using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// cinderreach-inventory-redesign: the fixed tooltip column (Kriterium 1) shows
/// name, rarity, slot, affixes, unique effect and comparison for hover and click,
/// and its Equip/Sell/Discard actions run on the authoritative equip path.
/// </summary>
public sealed class InventoryTooltipColumnPlayTests
{
    GameObject hero;
    GameObject eventSys;
    Equipment equipment;
    Inventory inventory;
    Wallet wallet;
    DiabloInventoryUI ui;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        RunProfile.Reset();
        if (EventSystem.current == null)
            eventSys = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        hero = new GameObject(DiabloNetworkPlayer.HeroName);
        hero.AddComponent<CharacterController>();
        var ctrl = hero.AddComponent<DiabloClickController>();
        ctrl.enabled = false;
        equipment = hero.AddComponent<Equipment>();
        inventory = hero.AddComponent<Inventory>();
        wallet = hero.AddComponent<Wallet>();
        hero.AddComponent<PlayerHealth>();
        hero.AddComponent<HealingFlask>();
        hero.AddComponent<DiabloProgression>();
        hero.AddComponent<DiabloSkills>();
        ui = hero.AddComponent<DiabloInventoryUI>();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (ui != null) ui.SetOpen(false);
        if (hero != null) Object.Destroy(hero);
        if (eventSys != null) Object.Destroy(eventSys);
        RunProfile.Reset();
        yield return null;
    }

    static ItemInstance Sword(float damage, ItemRarity rarity = ItemRarity.Rare, string uniqueId = null) =>
        new ItemInstance
        {
            slot = ItemSlot.Weapon,
            rarity = rarity,
            baseType = ItemDatabase.StarterSwordBase,
            itemLevel = 5,
            uniqueId = uniqueId,
            affixes = new System.Collections.Generic.List<Affix> { new Affix(AffixLibrary.Damage, damage) }
        };

    GameObject BagCell(int index) => hero.GetComponentsInChildren<InventoryCell>(true)
        .First(c => c.kind == InventoryCell.Kind.Bag && c.index == index && c.name == $"BagButton{index}").gameObject;

    GameObject Column() => hero.transform.Find("InventoryCanvas/Panel/InventoryPage/TooltipColumn/TooltipCard")?.gameObject;

    static PointerEventData Pointer(PointerEventData.InputButton button) =>
        new PointerEventData(EventSystem.current) { button = button };

    static void Click(GameObject go) =>
        ExecuteEvents.Execute(go, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left },
            ExecuteEvents.pointerClickHandler);

    [UnityTest]
    public IEnumerator HoverFillsColumnWithNameRarityAffixesAndComparison()
    {
        equipment.Equip(Sword(2f));
        var better = Sword(5f);
        inventory.Add(better);
        ui.SetOpen(true);
        yield return null;
        Canvas.ForceUpdateCanvases();

        ExecuteEvents.Execute(BagCell(0), Pointer(PointerEventData.InputButton.Left), ExecuteEvents.pointerEnterHandler);
        var col = Column();
        Assert.IsNotNull(col, "Fixed tooltip column exists.");
        var title = col.transform.Find("TooltipTitle")?.GetComponent<Text>();
        Assert.IsNotNull(title);
        StringAssert.Contains(LootDrop.ItemLabel(better), title.text);
        var meta = col.transform.Find("TooltipMeta")?.GetComponent<Text>();
        Assert.IsNotNull(meta);
        StringAssert.Contains(ItemText.Rarity(ItemRarity.Rare), meta.text);
        StringAssert.Contains(ItemText.Slot(ItemSlot.Weapon), meta.text);
        var body = col.transform.Find("TooltipScroll/Viewport/TooltipContent/TooltipBody")?.GetComponent<Text>();
        Assert.IsNotNull(body, "Scrollable detail body exists.");
        Assert.IsNotEmpty(body.text);
        var compare = col.transform.Find("TooltipCompare")?.GetComponent<Text>();
        Assert.IsNotNull(compare);
        StringAssert.Contains(DiabloInventoryUI.BetterColor, compare.text);
        StringAssert.Contains(Loc.T("inv.tc.compare"), compare.text);

        // Action buttons are real buttons, large enough to tap.
        foreach (var name in new[] { "TooltipActions/TooltipEquipButton", "TooltipActions/TooltipSellButton", "TooltipActions/TooltipDiscardButton" })
        {
            var btn = col.transform.Find(name)?.GetComponent<Button>();
            Assert.IsNotNull(btn, name + " exists.");
            Assert.GreaterOrEqual(((RectTransform)btn.transform).rect.height, 44f, name + " is tappable.");
        }
    }

    [UnityTest]
    public IEnumerator ClickEquipsAndEquippedSlotShowsUnequip()
    {
        var sword = Sword(5f);
        inventory.Add(sword);
        ui.SetOpen(true);
        yield return null;

        Click(BagCell(0));
        Assert.AreSame(sword, equipment.weapon, "Click equips on the authoritative path.");
        yield return null;

        // Hovering the worn slot shows it with an Unequip action.
        var slot = hero.GetComponentsInChildren<InventoryCell>(true)
            .First(c => c.kind == InventoryCell.Kind.Equip && c.Slot == ItemSlot.Weapon).gameObject;
        ExecuteEvents.Execute(slot, Pointer(PointerEventData.InputButton.Left), ExecuteEvents.pointerEnterHandler);
        var col = Column();
        var title = col.transform.Find("TooltipTitle")?.GetComponent<Text>();
        StringAssert.Contains(LootDrop.ItemLabel(sword), title.text);
        var equipBtn = col.transform.Find("TooltipActions/TooltipEquipButton")?.GetComponent<Button>();
        var label = equipBtn.GetComponentInChildren<Text>();
        StringAssert.Contains(Loc.T("inv.tc.unequip"), label.text);
        Click(equipBtn.gameObject);
        Assert.IsNull(equipment.weapon, "Unequip moves the item back into the bag.");
        Assert.AreEqual(1, inventory.Count);
    }

    [UnityTest]
    public IEnumerator SellButtonSellsForGold()
    {
        var sword = Sword(5f);
        inventory.Add(sword);
        ui.SetOpen(true);
        yield return null;
        int before = wallet.Gold;

        ExecuteEvents.Execute(BagCell(0), Pointer(PointerEventData.InputButton.Left), ExecuteEvents.pointerEnterHandler);
        var sell = Column().transform.Find("TooltipActions/TooltipSellButton")?.gameObject;
        Assert.IsNotNull(sell);
        Click(sell);
        Assert.AreEqual(0, inventory.Count, "Sold item leaves the bag.");
        Assert.Greater(wallet.Gold, before, "Selling credits gold.");
        StringAssert.Contains("Gold", ui.Notice);
    }

    [UnityTest]
    public IEnumerator UniqueShowsEffectText()
    {
        var unique = Sword(5f, ItemRarity.Unique, ItemDatabase.LancerWardId);
        inventory.Add(unique);
        ui.SetOpen(true);
        yield return null;

        ExecuteEvents.Execute(BagCell(0), Pointer(PointerEventData.InputButton.Left), ExecuteEvents.pointerEnterHandler);
        var body = Column().transform.Find("TooltipScroll/Viewport/TooltipContent/TooltipBody")?.GetComponent<Text>();
        Assert.IsNotNull(body);
        string effect = BuildUniqueEffects.EffectText(ItemDatabase.LancerWardId);
        Assert.IsNotEmpty(effect, "Test premise: lancer-ward has an effect text.");
        StringAssert.Contains(effect, body.text, "Unique effect renders in the column.");
    }
}
