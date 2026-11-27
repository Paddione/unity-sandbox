using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Headless EditMode tests for <see cref="CharacterStatBreakdown"/>,
/// <see cref="SkillBreakdown"/>, <see cref="InfoTooltip"/>, and enhanced
/// <see cref="ItemTooltip"/>.
/// </summary>
public sealed class CharacterStatBreakdownTests
{
    static ItemInstance MakeItem(ItemSlot slot, ItemRarity rarity, int ilvl, string baseType, params (string key, float val)[] affixes)
    {
        var item = new ItemInstance { slot = slot, rarity = rarity, itemLevel = ilvl, baseType = baseType };
        foreach (var (k, v) in affixes) item.affixes.Add(new Affix(k, v));
        return item;
    }

    [TearDown]
    public void TearDown()
    {
        Loc.Language = Loc.German;
        RunProfile.Reset();
        DifficultyProfile.Select(RunProfile.Normal);
        ItemTooltip.ShiftOverride = null;
    }

    [Test]
    public void EveryAffixInLibrary_IsCoveredByBreakdown()
    {
        var go = new GameObject("StatHero");
        var eq = go.AddComponent<Equipment>();
        var prog = go.AddComponent<DiabloProgression>();
        var snap = CharacterStatBreakdown.Build(eq, prog, ClassCatalog.Fenwalker);

        var covered = new HashSet<string>();
        foreach (var entry in snap.entries)
            foreach (var k in entry.coveredAffixKeys)
                covered.Add(k);

        var missing = new List<string>();
        foreach (var def in AffixLibrary.All)
            if (!covered.Contains(def.key)) missing.Add(def.key);

        Assert.IsEmpty(missing, "Affixes missing from CharacterStatBreakdown: " + string.Join(", ", missing));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void MaxHpAndMeleeDamage_MatchLiveCombatGetters()
    {
        var go = new GameObject("CombatStatHero");
        var eq = go.AddComponent<Equipment>();
        var prog = go.AddComponent<DiabloProgression>();
        var melee = go.AddComponent<DiabloMelee>();

        prog.SetState(5, 0, 0, 0, strength: 3, vitality: 2, focus: 0, ranks: null);
        eq.Equip(MakeItem(ItemSlot.Weapon, ItemRarity.Rare, 5, "ShortSword",
            (AffixLibrary.Damage, 6f), (AffixLibrary.FireDamage, 2f), (AffixLibrary.MaxHP, 50f)));
        eq.Equip(MakeItem(ItemSlot.Armor, ItemRarity.Rare, 5, "Breastplate", (AffixLibrary.MaxHP, 15f)));
        eq.Equip(MakeItem(ItemSlot.Belt, ItemRarity.Rare, 5, "WarBelt", (AffixLibrary.MaxHP, 10f)));

        var snap = CharacterStatBreakdown.Build(eq, prog, ClassCatalog.Lanternbearer);

        // MaxHP: 20 base + (5-1)*3 level (12) + 2*5 vit (10) + 15 armor + 10 belt = 67 (weapon MaxHP ignored).
        Assert.AreEqual(eq.GetMaxHP(), 67f, 0.001f);
        var hpEntry = snap.Find("max_hp");
        Assert.IsNotNull(hpEntry);
        Assert.AreEqual(Loc.Num(eq.GetMaxHP(), "0.#"), hpEntry.displayValue);
        StringAssert.Contains(Loc.Num(50f, "0.#"), hpEntry.capNote);

        // Melee: 1 base + 6 dmg + 2 fire + 3 str = 12 (strengthMeleeFactor 0.5 ignored per live DiabloMelee).
        int expectedDmg = melee.CurrentDamage(go.transform);
        Assert.AreEqual(12, expectedDmg);
        var dmgEntry = snap.Find("melee_damage");
        Assert.IsNotNull(dmgEntry);
        Assert.AreEqual(Loc.Num(expectedDmg, "0"), dmgEntry.displayValue);
        StringAssert.Contains("+3", dmgEntry.RenderTooltipBody());

        Object.DestroyImmediate(go);
    }

    [Test]
    public void CapsAndUniques_MatchLiveEquipmentGetters()
    {
        var go = new GameObject("UniqueStatHero");
        var eq = go.AddComponent<Equipment>();
        var prog = go.AddComponent<DiabloProgression>();
        prog.SetState(3, 0, 0, 0, strength: 0, vitality: 0, focus: 5, ranks: null);

        var bow = ItemDatabase.CreateUnique(ItemDatabase.GlassthornBowId);
        bow.affixes.Add(new Affix(AffixLibrary.Crit, 90f));
        bow.affixes.Add(new Affix(AffixLibrary.LifeSteal, 40f));
        eq.Equip(bow);

        var mail = ItemDatabase.CreateUnique(ItemDatabase.KilnheartMailId);
        mail.affixes.Add(new Affix(AffixLibrary.Thorns, 5f));
        eq.Equip(mail);

        var crown = ItemDatabase.CreateUnique(ItemDatabase.EmberwreathCrownId);
        crown.affixes.Add(new Affix(AffixLibrary.CooldownReduction, 48f));
        eq.Equip(crown);

        var hood = ItemDatabase.CreateUnique(ItemDatabase.ProspectorsHoodId);
        var gloves = MakeItem(ItemSlot.Gloves, ItemRarity.Rare, 5, "LeatherGloves", (AffixLibrary.GoldFind, 40f));
        eq.Equip(gloves);

        var armorSlot = MakeItem(ItemSlot.Armor, ItemRarity.Rare, 5, "Breastplate",
            (AffixLibrary.IncreasedArea, 130f), (AffixLibrary.Armor, 4f), (AffixLibrary.Thorns, 5f));
        eq.Equip(armorSlot);

        var belt = ItemDatabase.CreateUnique(ItemDatabase.VialSashId);
        belt.affixes.Add(new Affix(AffixLibrary.FlaskHealing, 20f));
        eq.Equip(belt);

        var snap = CharacterStatBreakdown.Build(eq, prog, ClassCatalog.Rogue);

        Assert.AreEqual(Equipment.MaxCritChance, eq.GetCrit(), 0.001f);
        StringAssert.Contains(Loc.Num(75f, "0.#"), snap.Find("crit_chance").displayValue);
        Assert.AreEqual(Loc.F("statbd.val.mul", 2.5f), snap.Find("crit_multiplier").displayValue);
        StringAssert.Contains(Loc.Num(25f, "0.#"), snap.Find("life_steal").displayValue);
        StringAssert.Contains("100", snap.Find("area_bonus").displayValue);

        // EmberwreathCrown raises gear CDR cap to 50%, combined multiplicatively with Focus (1 - 5*0.03 = 0.85).
        float expectedCdrPct = (1f - prog.Sheet.CooldownMultiplier() * eq.CooldownFactor()) * 100f;
        Assert.AreEqual(Loc.F("statbd.val.pct", expectedCdrPct).TrimStart(), snap.Find("cooldown_reduction").displayValue.TrimStart('-'));
        StringAssert.Contains("50", snap.Find("cooldown_reduction").capNote);

        // Flask shows 2x charges from VialSash.
        StringAssert.Contains("2×", snap.Find("flask_heal").displayValue);

        // KilnheartMail x2 thorns when equipped.
        eq.Equip(mail);
        var kilnSnap = CharacterStatBreakdown.Build(eq, prog, ClassCatalog.Fenwalker);
        Assert.AreEqual(eq.ThornsDamage().ToString(), kilnSnap.Find("thorns").displayValue);

        // ProspectorsHood adds +50% of GoldFind to MagicFind.
        eq.Equip(hood);
        var hoodSnap = CharacterStatBreakdown.Build(eq, prog, ClassCatalog.Rogue);
        Assert.AreEqual(Loc.F("statbd.val.plus_pct", eq.MagicFindPct()), hoodSnap.Find("magic_find").displayValue);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void SkillBreakdown_MatchesDiabloSkillsEffectiveValuesAcrossClasses()
    {
        var go = new GameObject("SkillStatHero");
        var eq = go.AddComponent<Equipment>();
        var prog = go.AddComponent<DiabloProgression>();
        var skills = go.AddComponent<DiabloSkills>();

        prog.SetState(6, 0, 0, 2, strength: 1, vitality: 1, focus: 4, ranks: null);
        prog.Sheet.SpendSkill(DiabloSkills.Skill.Cleave);
        prog.Sheet.SpendSkill(DiabloSkills.Skill.MendingLight);
        prog.Sheet.SpendSkill(DiabloSkills.Skill.BladeFlurry);

        eq.Equip(MakeItem(ItemSlot.Weapon, ItemRarity.Rare, 5, "ShortSword", (AffixLibrary.Damage, 4f)));
        eq.Equip(MakeItem(ItemSlot.Helmet, ItemRarity.Rare, 5, "Helm", (AffixLibrary.SkillRank, 1f), (AffixLibrary.CooldownReduction, 10f)));
        eq.Equip(MakeItem(ItemSlot.Armor, ItemRarity.Rare, 5, "Breastplate", (AffixLibrary.IncreasedArea, 25f)));

        // Fenwalker Cleave
        skills.classOverride = ClassCatalog.Fenwalker;
        var cleaveData = SkillBreakdown.Build(DiabloSkills.Skill.Cleave, skills, prog.Sheet, eq, ClassCatalog.Fenwalker);
        StringAssert.Contains(skills.EffectiveDamage(DiabloSkills.Skill.Cleave).ToString(), cleaveData.body);
        StringAssert.Contains(Loc.Num(skills.EffectiveCooldown(DiabloSkills.Skill.Cleave), "0.##"), cleaveData.body);

        // Lanternbearer MendingLight (focusSkillFactor 1.5, specialtyFactor 1.5, MendersTouch x1.2)
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.MendersTouchId));
        skills.classOverride = ClassCatalog.Lanternbearer;
        var mendData = SkillBreakdown.Build(DiabloSkills.Skill.MendingLight, skills, prog.Sheet, eq, ClassCatalog.Lanternbearer);
        StringAssert.Contains(skills.EffectiveHeal(DiabloSkills.Skill.MendingLight).ToString(), mendData.body);

        // Rogue BladeFlurry (focusSkillFactor 1.5, specialtyFactor 1.5)
        skills.classOverride = ClassCatalog.Rogue;
        var flurryData = SkillBreakdown.Build(DiabloSkills.Skill.BladeFlurry, skills, prog.Sheet, eq, ClassCatalog.Rogue);
        StringAssert.Contains(skills.EffectiveDamage(DiabloSkills.Skill.BladeFlurry).ToString(), flurryData.body);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void ItemTooltip_ShowsRollRangesAndCapHints_AndInfoTooltipUsesHdScaler()
    {
        var go = new GameObject("TipHero");
        var eq = go.AddComponent<Equipment>();
        var weapon = MakeItem(ItemSlot.Weapon, ItemRarity.Rare, 10, "ShortSword",
            (AffixLibrary.Damage, 5f), (AffixLibrary.Crit, 8f), (AffixLibrary.MaxHP, 12f));
        eq.Equip(MakeItem(ItemSlot.Gloves, ItemRarity.Rare, 10, "LeatherGloves", (AffixLibrary.Crit, 72f)));

        var itemTip = ItemTooltip.For(go);
        ItemTooltip.ShiftOverride = false;
        itemTip.Show(weapon, eq);

        // Roll range for Damage at ilvl 10 tier 1 -> Bracket 3 (x1.5) -> [2-8]
        var (minDmg, maxDmg) = AffixLibrary.GetRange(AffixLibrary.Damage, 10, 1);
        StringAssert.Contains($"[{Loc.Num(minDmg, "0")}–{Loc.Num(maxDmg, "0")}]", itemTip.HoverText);
        // Weapon MaxHP note and Crit cap hint present.
        StringAssert.Contains(AffixLibrary.StatName(AffixLibrary.Crit), itemTip.HoverText);
        StringAssert.Contains(Loc.F("statbd.item.weapon_hp_note", DiabloPalette.ToHex(DiabloPalette.ParchmentDim)), itemTip.HoverText);

        // English switch produces clean text without missing keys.
        Loc.Language = Loc.English;
        itemTip.Show(weapon, eq);
        StringAssert.Contains("Cap", itemTip.HoverText);

        // InfoTooltip uses 1920x1080 scaler without GraphicRaycaster.
        var infoTip = InfoTooltip.For(go);
        infoTip.Show("Title", "Formula", "Body");
        Assert.IsTrue(infoTip.IsVisible);
        Assert.IsFalse(itemTip.IsVisible, "Showing InfoTooltip hides ItemTooltip.");
        var canvasGo = go.transform.Find(InfoTooltip.CanvasName)?.gameObject;
        Assert.IsNotNull(canvasGo);
        Assert.IsNull(canvasGo.GetComponent<GraphicRaycaster>());
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        Assert.IsNotNull(scaler);
        Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);

        Object.DestroyImmediate(go);
    }
}
