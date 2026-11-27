using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Runtime PlayMode tests for the 3-column inventory stat breakdown, live updates,
/// parameter hover, character sheet & skillbar hover, and item/info tooltip mutual exclusion.
/// </summary>
public sealed class InventoryStatBreakdownPlayTests
{
    GameObject hero;
    GameObject eventSys;
    Equipment equipment;
    Inventory inventory;
    DiabloProgression prog;
    DiabloInventoryUI invUi;

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
        prog = hero.AddComponent<DiabloProgression>();
        hero.AddComponent<PlayerHealth>();
        hero.AddComponent<HealingFlask>();
        hero.AddComponent<DiabloSkills>();
        invUi = hero.AddComponent<DiabloInventoryUI>();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        DiabloClickController.ExternalDrive = false;
        if (invUi != null) invUi.SetOpen(false);
        var rig = invUi != null && invUi.Paperdoll != null ? invUi.Paperdoll.Rig : null;
        if (hero != null) Object.Destroy(hero);
        if (rig != null) Object.Destroy(rig);
        if (eventSys != null) Object.Destroy(eventSys);
        RunProfile.Reset();
        yield return null;
    }

    static PointerEventData Pointer() => new PointerEventData(EventSystem.current);

    [UnityTest]
    public IEnumerator Inventory_ThreeColumnsFitWithoutOverlap_AndRefreshOnEquip()
    {
        var sword = new ItemInstance { slot = ItemSlot.Weapon, rarity = ItemRarity.Rare, baseType = "ShortSword", itemLevel = 5 };
        sword.affixes.Add(new Affix(AffixLibrary.Damage, 7f));
        inventory.Add(sword);

        invUi.SetOpen(true);
        yield return null;
        Canvas.ForceUpdateCanvases();
        var page = hero.transform.Find("InventoryCanvas/Panel/InventoryPage") as RectTransform;
        Assert.IsNotNull(page);
        LayoutRebuilder.ForceRebuildLayoutImmediate(page);

        var charCol = page.Find("CharacterColumn") as RectTransform;
        var paramCol = page.Find("ParametersColumn") as RectTransform;
        var bagCol = page.Find("BagColumn") as RectTransform;
        Assert.IsNotNull(charCol);
        Assert.IsNotNull(paramCol);
        Assert.IsNotNull(bagCol);
        Assert.IsTrue(charCol.gameObject.activeInHierarchy);
        Assert.IsTrue(paramCol.gameObject.activeInHierarchy);
        Assert.IsTrue(bagCol.gameObject.activeInHierarchy);

        Vector3[] cCorners = new Vector3[4], pCorners = new Vector3[4], bCorners = new Vector3[4];
        charCol.GetWorldCorners(cCorners);
        paramCol.GetWorldCorners(pCorners);
        bagCol.GetWorldCorners(bCorners);
        Assert.Greater(charCol.rect.width, 400f);
        Assert.Greater(paramCol.rect.width, 380f);
        Assert.Greater(bagCol.rect.width, 500f);
        Assert.LessOrEqual(cCorners[2].x, pCorners[0].x + 1f, "CharacterColumn must not overlap ParametersColumn.");
        Assert.LessOrEqual(pCorners[2].x, bCorners[0].x + 1f, "ParametersColumn must not overlap BagColumn.");

        // Check live update on EquipIndex and UnequipSlot.
        var dmgVal = paramCol.Find("ParametersCard/ParametersScroll/Viewport/ParametersContent/ParamRow_melee_damage/Value")?.GetComponent<Text>();
        Assert.IsNotNull(dmgVal);
        Assert.AreEqual("1", dmgVal.text);

        invUi.EquipIndex(0);
        yield return null;
        dmgVal = paramCol.Find("ParametersCard/ParametersScroll/Viewport/ParametersContent/ParamRow_melee_damage/Value")?.GetComponent<Text>();
        Assert.AreEqual("8", dmgVal.text, "Equipping +7 Damage weapon updates parameter row immediately.");

        invUi.UnequipSlot((int)ItemSlot.Weapon);
        yield return null;
        dmgVal = paramCol.Find("ParametersCard/ParametersScroll/Viewport/ParametersContent/ParamRow_melee_damage/Value")?.GetComponent<Text>();
        Assert.AreEqual("1", dmgVal.text, "Unequipping reverts parameter row immediately.");
    }

    [UnityTest]
    public IEnumerator ParameterAndBagHover_ShowAndHideTooltipsCleanly()
    {
        var sword = new ItemInstance { slot = ItemSlot.Weapon, rarity = ItemRarity.Rare, baseType = "ShortSword", itemLevel = 5 };
        sword.affixes.Add(new Affix(AffixLibrary.Damage, 4f));
        inventory.Add(sword);

        invUi.SetOpen(true);
        yield return null;

        var row = hero.transform.Find("InventoryCanvas/Panel/InventoryPage/ParametersColumn/ParametersCard/ParametersScroll/Viewport/ParametersContent/ParamRow_max_hp")?.gameObject;
        Assert.IsNotNull(row);
        ExecuteEvents.Execute(row, Pointer(), ExecuteEvents.pointerEnterHandler);
        var infoTip = hero.GetComponent<InfoTooltip>();
        Assert.IsNotNull(infoTip);
        Assert.IsTrue(infoTip.IsVisible);
        StringAssert.Contains(Loc.T("statbd.src.base"), infoTip.BodyText);

        // Hovering a bag item opens ItemTooltip and closes InfoTooltip.
        var bagCell = hero.GetComponentsInChildren<InventoryCell>(true)
            .First(c => c.kind == InventoryCell.Kind.Bag && c.index == 0 && c.name == "BagButton0").gameObject;
        ExecuteEvents.Execute(bagCell, Pointer(), ExecuteEvents.pointerEnterHandler);
        var itemTip = hero.GetComponent<ItemTooltip>();
        Assert.IsNotNull(itemTip);
        Assert.IsTrue(itemTip.IsVisible);
        Assert.IsFalse(infoTip.IsVisible);

        // Stats tab parameter row hover also works.
        invUi.ShowTab(DiabloInventoryUI.MenuTab.Stats);
        yield return null;
        var statsRow = hero.transform.Find("InventoryCanvas/Panel/StatsPage/StatsCard/StatsScroll/Viewport/StatsContent/ParamRow_melee_damage")?.gameObject;
        Assert.IsNotNull(statsRow);
        ExecuteEvents.Execute(statsRow, Pointer(), ExecuteEvents.pointerEnterHandler);
        Assert.IsTrue(infoTip.IsVisible);

        invUi.SetOpen(false);
        Assert.IsFalse(infoTip.IsVisible);
    }

    [UnityTest]
    public IEnumerator CharacterSheetAndSkillBarHud_HoverShowSkillBreakdown()
    {
        prog.SetState(4, 0, 0, 1, strength: 1, vitality: 0, focus: 3, ranks: null);
        prog.Sheet.SpendSkill(DiabloSkills.Skill.Cleave);

        var charUi = hero.AddComponent<DiabloCharacterUI>();
        charUi.SetOpen(true);
        yield return null;

        var skillRow = hero.transform.Find("CharacterCanvas/Panel/SkillRowsContainer/RowQ")?.gameObject;
        Assert.IsNotNull(skillRow);
        ExecuteEvents.Execute(skillRow, Pointer(), ExecuteEvents.pointerEnterHandler);
        var infoTip = hero.GetComponent<InfoTooltip>();
        Assert.IsNotNull(infoTip);
        Assert.IsTrue(infoTip.IsVisible);
        StringAssert.Contains(DiabloSkills.SkillName(DiabloSkills.Skill.Cleave), infoTip.TitleText);
        StringAssert.Contains(hero.GetComponent<DiabloSkills>().EffectiveDamage(DiabloSkills.Skill.Cleave).ToString(), infoTip.BodyText);

        ExecuteEvents.Execute(skillRow, Pointer(), ExecuteEvents.pointerExitHandler);
        Assert.IsFalse(infoTip.IsVisible);
        charUi.SetOpen(false);

        // SkillBarHud hover via SkillSlot_Cleave
        hero.GetComponent<DiabloClickController>().enabled = true;
        DiabloClickController.ExternalDrive = true;
        yield return null;

        var hudSlot = hero.transform.Find("SkillBarCanvas/SkillSlotsRow/SkillSlot_Cleave")?.gameObject;
        Assert.IsNotNull(hudSlot);
        ExecuteEvents.Execute(hudSlot, Pointer(), ExecuteEvents.pointerEnterHandler);
        Assert.IsTrue(infoTip.IsVisible);
        StringAssert.Contains(DiabloSkills.SkillName(DiabloSkills.Skill.Cleave), infoTip.TitleText);
        ExecuteEvents.Execute(hudSlot, Pointer(), ExecuteEvents.pointerExitHandler);
        Assert.IsFalse(infoTip.IsVisible);
    }
}
