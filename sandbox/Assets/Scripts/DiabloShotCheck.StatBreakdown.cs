using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>Shotcheck part: Character stat breakdown panels (-stat-breakdown-shots).</summary>
public sealed partial class DiabloShotCheck
{
    IEnumerator StatBreakdownShots(DiabloClickController hero)
    {
        var eq = hero.GetComponent<Equipment>();
        var bag = hero.GetComponent<Inventory>();
        var prog = hero.GetComponent<DiabloProgression>();
        if (prog != null && prog.Sheet != null)
        {
            // cinderreach-levelup-empower: attributes come from the class template,
            // only skill ranks are still spent manually (D2/D3).
            for (int i = 0; i < 4; i++) prog.Sheet.AwardLevel(RunProfile.Class);
            prog.Sheet.SpendSkill(RunProfile.Class.skills[0]);
            prog.Sheet.SpendSkill(RunProfile.Class.skills[2]);
        }

        var prevWeapon = eq != null ? eq.weapon : null;
        var prevArmor = eq != null ? eq.armor : null;
        var prevBelt = eq != null ? eq.belt : null;

        var weapon = GalleryItem(ItemDatabase.StarterSwordBase, ItemRarity.Rare,
            (AffixLibrary.Damage, 5f), (AffixLibrary.FireDamage, 3f), (AffixLibrary.Crit, 8f));
        var armor = new ItemInstance { slot = ItemSlot.Armor, rarity = ItemRarity.Rare, baseType = "Breastplate", itemLevel = 10 };
        armor.affixes.Add(new Affix(AffixLibrary.MaxHP, 24f));
        armor.affixes.Add(new Affix(AffixLibrary.Armor, 4f));
        var belt = new ItemInstance { slot = ItemSlot.Belt, rarity = ItemRarity.Rare, baseType = "WarBelt", itemLevel = 10 };
        belt.affixes.Add(new Affix(AffixLibrary.CooldownReduction, 10f));
        belt.affixes.Add(new Affix(AffixLibrary.FlaskHealing, 18f));
        var rival = GalleryItem(ItemDatabase.StarterSwordBase, ItemRarity.Rare,
            (AffixLibrary.Damage, 7f), (AffixLibrary.Crit, 4f), (AffixLibrary.LifeSteal, 5f));

        if (eq != null)
        {
            eq.Equip(weapon);
            eq.Equip(armor);
            eq.Equip(belt);
        }
        bool addedRival = bag != null && bag.Add(rival);

        var invUi = hero.GetComponent<DiabloInventoryUI>() ?? hero.gameObject.AddComponent<DiabloInventoryUI>();
        invUi.SetOpen(true);
        invUi.Refresh();
        yield return Wait(0.45f);
        yield return Shot("stat_breakdown_inventory");

        // Parameter tooltip pinned next to the Stats tab breakdown row.
        var infoTip = InfoTooltip.For(hero.gameObject);
        var snap = CharacterStatBreakdown.BuildFor(hero.gameObject);
        var cdrEntry = snap.Find("cooldown_reduction") ?? snap.Find("max_hp");
        var paramRow = hero.transform.Find("InventoryCanvas/Panel/StatsPage/StatsCard/StatsScroll/Viewport/StatsContent/ParamRow_" + cdrEntry.id) as RectTransform;
        infoTip.PinnedScreen = paramRow != null ? (Vector2)paramRow.position : new Vector2(Screen.width * 0.50f, Screen.height * 0.55f);
        infoTip.Show(cdrEntry.name, cdrEntry.formula, cdrEntry.RenderTooltipBody(), paramRow != null ? paramRow.gameObject : null, cdrEntry.capNote);
        yield return Wait(0.35f);
        yield return Shot("stat_breakdown_param_tip");
        infoTip.Hide();
        infoTip.PinnedScreen = null;

        // Extended ItemTooltip with roll spans + Shift-compare.
        if (addedRival)
        {
            yield return CompareShot(hero.gameObject, "InventoryCanvas", rival, "stat_breakdown_item_tip", $"BagButton{bag.Count - 1}");
            bag.RemoveAt(bag.Count - 1);
        }
        invUi.SetOpen(false);
        yield return Wait(0.2f);

        // Character sheet skill tooltip pinned.
        var charUi = hero.GetComponent<DiabloCharacterUI>() ?? hero.gameObject.AddComponent<DiabloCharacterUI>();
        charUi.SetOpen(true);
        charUi.Refresh();
        yield return Wait(0.35f);
        var skillData = SkillBreakdown.BuildFor(hero.gameObject, RunProfile.Class.skills[2]);
        var skillRow = hero.transform.Find("CharacterCanvas/Panel/SkillRowsContainer/Row" + RunProfile.Class.keys[2]) as RectTransform;
        infoTip.PinnedScreen = skillRow != null ? (Vector2)skillRow.position - new Vector2(390f, -60f) : new Vector2(Screen.width * 0.45f, Screen.height * 0.55f);
        infoTip.Show(skillData.title, skillData.meta, skillData.body, skillRow != null ? skillRow.gameObject : null);
        yield return Wait(0.35f);
        yield return Shot("stat_breakdown_skill_tip");
        infoTip.Hide();
        infoTip.PinnedScreen = null;
        charUi.SetOpen(false);

        if (eq != null)
        {
            eq.SetSlot(ItemSlot.Weapon, prevWeapon);
            eq.SetSlot(ItemSlot.Armor, prevArmor);
            eq.SetSlot(ItemSlot.Belt, prevBelt);
        }
        invUi.Refresh();
    }
}
