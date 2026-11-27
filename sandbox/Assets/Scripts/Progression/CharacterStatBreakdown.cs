using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Read-only single source of truth for all character parameters and their
/// live calculation breakdown (base, level, attribute scaling, per-slot gear,
/// unique modifiers, and active caps). Pure C# data types for headless testing.
/// </summary>
public static class CharacterStatBreakdown
{
    public enum StatCategory { Attributes, Offense, Defense, Utility }

    public readonly struct StatSourceLine
    {
        public readonly string label;
        public readonly string value;
        public readonly bool highlight;

        public StatSourceLine(string label, string value, bool highlight = false)
        {
            this.label = label;
            this.value = value;
            this.highlight = highlight;
        }
    }

    public sealed class StatEntry
    {
        public readonly string id;
        public readonly StatCategory category;
        public readonly string name;
        public readonly string displayValue;
        public readonly string formula;
        public readonly string capNote;
        public readonly List<StatSourceLine> sources;
        public readonly string[] coveredAffixKeys;

        public StatEntry(string id, StatCategory category, string name, string displayValue,
            string formula, string capNote, List<StatSourceLine> sources, params string[] coveredAffixKeys)
        {
            this.id = id;
            this.category = category;
            this.name = name;
            this.displayValue = displayValue;
            this.formula = formula;
            this.capNote = capNote ?? "";
            this.sources = sources ?? new List<StatSourceLine>();
            this.coveredAffixKeys = coveredAffixKeys ?? System.Array.Empty<string>();
        }

        public string RenderTooltipBody()
        {
            var sb = new StringBuilder();
            if (sources.Count == 0)
            {
                sb.AppendLine(Loc.T("statbd.src.none"));
            }
            else
            {
            string goldHex = DiabloPalette.ToHex(DiabloPalette.TrimGold);
                foreach (var src in sources)
                {
                    if (src.highlight)
                        sb.AppendLine(Loc.F("statbd.rich.highlight", goldHex, src.label, src.value));
                    else
                        sb.AppendLine(string.Format("• {0}: {1}", src.label, src.value));
                }
            }
            sb.Append(Loc.F("statbd.src.total", displayValue));
            return sb.ToString();
        }
    }

    public sealed class CharacterStatSnapshot
    {
        public readonly List<StatEntry> entries;

        public CharacterStatSnapshot(List<StatEntry> entries)
        {
            this.entries = entries ?? new List<StatEntry>();
        }

        public StatEntry Find(string id)
        {
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].id == id) return entries[i];
            return null;
        }

        public string RenderPlainSummary()
        {
            var sb = new StringBuilder();
            string goldHex = DiabloPalette.ToHex(DiabloPalette.TrimGold);
            StatCategory? lastCat = null;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (!lastCat.HasValue || lastCat.Value != e.category)
                {
                    if (sb.Length > 0) sb.Append('\n');
                    sb.AppendLine(Loc.F("statbd.rich.cat", goldHex, CategoryTitle(e.category)));
                    lastCat = e.category;
                }
                sb.AppendLine(string.Format("• {0}: <b>{1}</b>", e.name, e.displayValue));
            }
            return sb.ToString().TrimEnd();
        }
    }

    public static string CategoryTitle(StatCategory cat)
    {
        switch (cat)
        {
            case StatCategory.Attributes: return Loc.T("statbd.cat.attributes");
            case StatCategory.Offense: return Loc.T("statbd.cat.offense");
            case StatCategory.Defense: return Loc.T("statbd.cat.defense");
            default: return Loc.T("statbd.cat.utility");
        }
    }

    public static CharacterStatSnapshot BuildFor(GameObject hero)
    {
        if (hero == null)
            return Build(null, null, RunProfile.Class, null, null, null);
        var eq = hero.GetComponent<Equipment>();
        var prog = hero.GetComponent<DiabloProgression>();
        var skills = hero.GetComponent<DiabloSkills>();
        var flask = hero.GetComponent<HealingFlask>();
        var gearState = hero.GetComponent<GearEffectState>();
        var cls = skills != null ? skills.ActiveClass : RunProfile.Class;
        return Build(eq, prog, cls, skills, flask, gearState);
    }

    public static CharacterStatSnapshot Build(Equipment eq, DiabloProgression prog,
        ClassDefinition cls = null, DiabloSkills skills = null,
        HealingFlask flask = null, GearEffectState gearState = null)
    {
        cls = cls ?? (skills != null ? skills.ActiveClass : RunProfile.Class);
        int level = prog != null ? prog.Level : 1;
        var sheet = prog != null ? prog.Sheet : new CharacterSheet();
        if (sheet == null) sheet = new CharacterSheet();

        var entries = new List<StatEntry>();
        BuildAttributeEntries(entries, sheet, cls);
        BuildOffenseEntries(entries, eq, sheet, cls);
        BuildDefenseEntries(entries, eq, level, sheet, flask);
        BuildUtilityEntries(entries, eq, sheet, gearState);
        return new CharacterStatSnapshot(entries);
    }

    static void BuildAttributeEntries(List<StatEntry> entries, CharacterSheet sheet, ClassDefinition cls)
    {
        int strBonus = sheet.MeleeBonus();
        string strNote = !Mathf.Approximately(cls.strengthMeleeFactor, 1f)
            ? Loc.F("statbd.note.str_melee_factor", cls.strengthMeleeFactor)
            : "";
        entries.Add(new StatEntry(
            "strength",
            StatCategory.Attributes,
            Loc.T("statbd.name.strength"),
            Loc.Num(sheet.strength, "0"),
            Loc.F("statbd.formula.strength", CharacterSheet.StrengthMeleePerPoint),
            strNote,
            new List<StatSourceLine>
            {
                new StatSourceLine(Loc.F("statbd.src.attr.str", sheet.strength, CharacterSheet.StrengthMeleePerPoint), $"+{strBonus}", strBonus > 0),
            }));

        float vitLife = sheet.VitalityLife();
        string vitFormatted = "+" + Loc.Num(vitLife, "0.#");
        entries.Add(new StatEntry(
            "vitality",
            StatCategory.Attributes,
            Loc.T("statbd.name.vitality"),
            Loc.Num(sheet.vitality, "0"),
            Loc.F("statbd.formula.vitality", CharacterSheet.VitalityLifePerPoint),
            "",
            new List<StatSourceLine>
            {
                new StatSourceLine(Loc.F("statbd.src.attr.vit", sheet.vitality, CharacterSheet.VitalityLifePerPoint), vitFormatted, vitLife > 0f),
            }));

        float focDmgPct = (sheet.FocusSkillMultiplier(cls.focusSkillFactor) - 1f) * 100f;
        float focCdPct = (1f - sheet.CooldownMultiplier()) * 100f;
        string focSummary = Loc.Num(sheet.focus, "0") + " (" + Loc.F("statbd.val.focus_summary", focDmgPct, focCdPct) + ")";
        string focCdStr = "-" + Loc.F("statbd.val.pct", focCdPct);
        entries.Add(new StatEntry(
            "focus",
            StatCategory.Attributes,
            Loc.T("statbd.name.focus"),
            focSummary,
            Loc.F("statbd.formula.focus", CharacterSheet.FocusSkillDamagePerPoint * 100f, cls.focusSkillFactor, CharacterSheet.FocusCooldownPerPoint * 100f),
            "",
            new List<StatSourceLine>
            {
                new StatSourceLine(
                    Loc.F("statbd.src.attr.foc_dmg", sheet.focus, CharacterSheet.FocusSkillDamagePerPoint * 100f, cls.focusSkillFactor),
                    Loc.F("statbd.val.plus_pct", focDmgPct),
                    focDmgPct > 0f),
                new StatSourceLine(
                    Loc.F("statbd.src.attr.foc_cd", sheet.focus, CharacterSheet.FocusCooldownPerPoint * 100f, CharacterSheet.MinCooldownFactor),
                    focCdStr,
                    focCdPct > 0f),
            }));
    }

    static void BuildOffenseEntries(List<StatEntry> entries, Equipment eq, CharacterSheet sheet, ClassDefinition cls)
    {
        int strBonus = sheet.MeleeBonus();
        float eqDamage = eq != null ? eq.GetDamage() : Equipment.BaseDamage;
        int meleeDmg = Mathf.Max(1, Mathf.RoundToInt(eqDamage) + strBonus);
        var meleeSources = new List<StatSourceLine>
        {
            new StatSourceLine(Loc.T("statbd.src.base"), Loc.Num(Equipment.BaseDamage, "0.#")),
        };
        if (strBonus > 0)
            meleeSources.Add(new StatSourceLine(Loc.F("statbd.src.attr.str", sheet.strength, CharacterSheet.StrengthMeleePerPoint), $"+{strBonus}", true));
        if (eq != null)
        {
            var weapon = eq.GetEquipped(ItemSlot.Weapon);
            if (weapon != null)
            {
                float wdmg = weapon.GetAffix(AffixLibrary.Damage);
                if (wdmg > 0f)
                    meleeSources.Add(new StatSourceLine(Loc.F("statbd.src.slot", ItemText.Slot(ItemSlot.Weapon), LootDrop.ItemLabel(weapon)), "+" + Loc.Num(wdmg, "0.#")));
                float wfire = weapon.GetAffix(AffixLibrary.FireDamage);
                if (wfire > 0f)
                    meleeSources.Add(new StatSourceLine(Loc.F("statbd.src.slot", AffixLibrary.StatName(AffixLibrary.FireDamage), LootDrop.ItemLabel(weapon)), "+" + Loc.Num(wfire, "0.#")));
            }
        }
        entries.Add(new StatEntry(
            "melee_damage",
            StatCategory.Offense,
            Loc.T("statbd.name.melee_damage"),
            Loc.Num(meleeDmg, "0"),
            Loc.F("statbd.formula.melee_damage", Equipment.BaseDamage, CharacterSheet.StrengthMeleePerPoint),
            !Mathf.Approximately(cls.strengthMeleeFactor, 1f) ? Loc.F("statbd.note.str_melee_factor", cls.strengthMeleeFactor) : "",
            meleeSources,
            AffixLibrary.Damage));

        float fireTotal = eq != null ? eq.Total(AffixLibrary.FireDamage) : 0f;
        var fireSources = CollectSlotSources(eq, AffixLibrary.FireDamage, includeWeapon: true);
        float weaponFire = eq != null && eq.GetEquipped(ItemSlot.Weapon) != null ? eq.GetEquipped(ItemSlot.Weapon).GetAffix(AffixLibrary.FireDamage) : 0f;
        entries.Add(new StatEntry(
            "fire_damage",
            StatCategory.Offense,
            Loc.T("statbd.name.fire_damage"),
            "+" + Loc.Num(fireTotal, "0.#"),
            Loc.T("statbd.formula.fire_damage"),
            weaponFire > 0f ? Loc.F("statbd.note.weapon_fire_included", weaponFire) : "",
            fireSources,
            AffixLibrary.FireDamage));

        int gearRanks = eq != null ? eq.SkillRankBonus() : 0;
        float rankMul = 1f + CharacterSheet.RankDamageBonus * gearRanks;
        float focusMul = sheet.FocusSkillMultiplier(cls.focusSkillFactor);
        float healUnique = GearUniqueEffects.HealMultiplier(eq);
        float skillMul = rankMul * focusMul;
        var skillSources = new List<StatSourceLine>
        {
            new StatSourceLine(Loc.T("statbd.src.base"), Loc.F("statbd.val.mul", 1f)),
            new StatSourceLine(Loc.F("statbd.src.attr.foc_dmg", sheet.focus, CharacterSheet.FocusSkillDamagePerPoint * 100f, cls.focusSkillFactor), Loc.F("statbd.val.mul", focusMul), focusMul > 1f),
        };
        if (gearRanks > 0)
            skillSources.Add(new StatSourceLine(Loc.F("statbd.src.gear_ranks", gearRanks), Loc.F("statbd.val.mul", rankMul), true));
        if (!Mathf.Approximately(cls.specialtyFactor, 1f))
            skillSources.Add(new StatSourceLine(Loc.F("statbd.src.specialty", cls.displayName), Loc.F("statbd.val.mul", cls.specialtyFactor), true));
        if (healUnique > 1f)
            skillSources.Add(new StatSourceLine(Loc.F("statbd.src.unique", LootDrop.PrettyName(ItemDatabase.MendersTouchId)), Loc.F("statbd.val.mul", healUnique), true));
        entries.Add(new StatEntry(
            "skill_damage_mul",
            StatCategory.Offense,
            Loc.T("statbd.name.skill_damage_mul"),
            Loc.F("statbd.val.mul", skillMul),
            Loc.F("statbd.formula.skill_damage_mul", cls.focusSkillFactor, cls.specialtyFactor),
            "",
            skillSources));

        float rawCrit = Equipment.BaseCritChance + (eq != null ? eq.Total(AffixLibrary.Crit) + eq.Total(AffixLibrary.CritRating) / 10f : 0f);
        float critChance = eq != null ? eq.GetCrit() : Equipment.BaseCritChance;
        var critSources = new List<StatSourceLine>
        {
            new StatSourceLine(Loc.T("statbd.src.base"), Loc.F("statbd.val.pct", Equipment.BaseCritChance)),
        };
        if (eq != null)
        {
            foreach (var slot in Equipment.GearSlots)
            {
                var item = eq.GetEquipped(slot);
                if (item == null) continue;
                float c = item.GetAffix(AffixLibrary.Crit);
                if (c > 0f)
                    critSources.Add(new StatSourceLine(Loc.F("statbd.src.slot", ItemText.Slot(slot), LootDrop.ItemLabel(item)), Loc.F("statbd.val.plus_pct", c)));
                float cr = item.GetAffix(AffixLibrary.CritRating);
                if (cr > 0f)
                    critSources.Add(new StatSourceLine(Loc.F("statbd.src.crit_rating", ItemText.Slot(slot), LootDrop.ItemLabel(item), cr), Loc.F("statbd.val.plus_pct", cr / 10f)));
            }
        }
        entries.Add(new StatEntry(
            "crit_chance",
            StatCategory.Offense,
            Loc.T("statbd.name.crit_chance"),
            Loc.F("statbd.val.pct", critChance),
            Loc.F("statbd.formula.crit_chance", Equipment.BaseCritChance, Equipment.MaxCritChance),
            CapNotePct(rawCrit, Equipment.MaxCritChance),
            critSources,
            AffixLibrary.Crit, AffixLibrary.CritRating));

        float critMul = GearUniqueEffects.CritMultiplier(eq, 2f);
        var critMulSources = new List<StatSourceLine>
        {
            new StatSourceLine(Loc.T("statbd.src.base"), Loc.F("statbd.val.mul", 2f)),
        };
        if (critMul > 2f)
            critMulSources.Add(new StatSourceLine(Loc.F("statbd.src.unique", LootDrop.PrettyName(ItemDatabase.GlassthornBowId)), Loc.F("statbd.val.mul", critMul), true));
        entries.Add(new StatEntry(
            "crit_multiplier",
            StatCategory.Offense,
            Loc.T("statbd.name.crit_multiplier"),
            Loc.F("statbd.val.mul", critMul),
            Loc.F("statbd.formula.crit_multiplier", 2f),
            "",
            critMulSources));

        float atkSpd = eq != null ? eq.Total(AffixLibrary.AttackSpeed) : 0f;
        entries.Add(new StatEntry(
            "attack_speed",
            StatCategory.Offense,
            Loc.T("statbd.name.attack_speed"),
            Loc.F("statbd.val.plus_pct", atkSpd),
            Loc.T("statbd.formula.attack_speed"),
            Loc.T("statbd.note.raw_only"),
            CollectSlotSources(eq, AffixLibrary.AttackSpeed, includeWeapon: true, percent: true),
            AffixLibrary.AttackSpeed));
    }

    static void BuildDefenseEntries(List<StatEntry> entries, Equipment eq, int level, CharacterSheet sheet, HealingFlask flask)
    {
        float levelLife = (level - 1) * DiabloProgression.LifePerLevel;
        float vitLife = sheet.VitalityLife();
        float gearLife = eq != null ? eq.GearTotal(AffixLibrary.MaxHP) : 0f;
        float maxHp = Equipment.BaseMaxHP + levelLife + vitLife + gearLife;
        var hpSources = new List<StatSourceLine>
        {
            new StatSourceLine(Loc.T("statbd.src.base"), Loc.Num(Equipment.BaseMaxHP, "0.#")),
            new StatSourceLine(Loc.F("statbd.src.level", level, DiabloProgression.LifePerLevel), "+" + Loc.Num(levelLife, "0.#"), levelLife > 0f),
        };
        if (vitLife > 0f)
            hpSources.Add(new StatSourceLine(Loc.F("statbd.src.attr.vit", sheet.vitality, CharacterSheet.VitalityLifePerPoint), "+" + Loc.Num(vitLife, "0.#"), true));
        hpSources.AddRange(CollectSlotSources(eq, AffixLibrary.MaxHP, includeWeapon: false));
        float weaponHp = eq != null && eq.GetEquipped(ItemSlot.Weapon) != null ? eq.GetEquipped(ItemSlot.Weapon).GetAffix(AffixLibrary.MaxHP) : 0f;
        entries.Add(new StatEntry(
            "max_hp",
            StatCategory.Defense,
            Loc.T("statbd.name.max_hp"),
            Loc.Num(maxHp, "0.#"),
            Loc.F("statbd.formula.max_hp", Equipment.BaseMaxHP, DiabloProgression.LifePerLevel, CharacterSheet.VitalityLifePerPoint),
            weaponHp > 0f ? Loc.F("statbd.note.weapon_hp_ignored", weaponHp) : "",
            hpSources,
            AffixLibrary.MaxHP));

        float armor = eq != null ? eq.Total(AffixLibrary.Armor) : 0f;
        entries.Add(new StatEntry(
            "armor",
            StatCategory.Defense,
            Loc.T("statbd.name.armor"),
            Loc.Num(armor, "0.#"),
            Loc.T("statbd.formula.armor"),
            Loc.T("statbd.note.raw_only"),
            CollectSlotSources(eq, AffixLibrary.Armor, includeWeapon: true),
            AffixLibrary.Armor));

        int thorns = eq != null ? eq.ThornsDamage() : 0;
        var thornsSources = CollectSlotSources(eq, AffixLibrary.Thorns, includeWeapon: true);
        float thornsMul = GearUniqueEffects.ThornsMultiplier(eq);
        if (thornsMul > 1f)
            thornsSources.Add(new StatSourceLine(Loc.F("statbd.src.unique", LootDrop.PrettyName(ItemDatabase.KilnheartMailId)), Loc.F("statbd.val.mul", thornsMul), true));
        entries.Add(new StatEntry(
            "thorns",
            StatCategory.Defense,
            Loc.T("statbd.name.thorns"),
            Loc.Num(thorns, "0"),
            Loc.T("statbd.formula.thorns"),
            "",
            thornsSources,
            AffixLibrary.Thorns));

        float rawLs = eq != null ? eq.Total(AffixLibrary.LifeSteal) : 0f;
        float ls = eq != null ? eq.LifeStealPct() : 0f;
        entries.Add(new StatEntry(
            "life_steal",
            StatCategory.Defense,
            Loc.T("statbd.name.life_steal"),
            Loc.F("statbd.val.pct", ls),
            Loc.F("statbd.formula.life_steal", Equipment.MaxLifeStealPct),
            CapNotePct(rawLs, Equipment.MaxLifeStealPct),
            CollectSlotSources(eq, AffixLibrary.LifeSteal, includeWeapon: true, percent: true),
            AffixLibrary.LifeSteal));

        float beltBonus = eq != null ? eq.GearTotal(AffixLibrary.FlaskHealing) : 0f;
        float flaskHeal = maxHp * HealingFlask.HealFraction * (1f + Mathf.Max(0f, beltBonus) / 100f);
        int charges = flask != null ? flask.MaxCharges : GearUniqueEffects.FlaskCharges(eq);
        float flaskCd = flask != null ? flask.Cooldown : GearUniqueEffects.FlaskCooldown(eq);
        var flaskSources = new List<StatSourceLine>
        {
            new StatSourceLine(Loc.F("statbd.src.flask_base", HealingFlask.HealFraction * 100f, maxHp), Loc.Num(maxHp * HealingFlask.HealFraction, "0.#")),
        };
        flaskSources.AddRange(CollectSlotSources(eq, AffixLibrary.FlaskHealing, includeWeapon: false, percent: true));
        flaskSources.Add(new StatSourceLine(Loc.T("statbd.src.flask_charges"), Loc.F("statbd.src.flask_charges_val", charges, flaskCd)));
        entries.Add(new StatEntry(
            "flask_heal",
            StatCategory.Defense,
            Loc.T("statbd.name.flask_heal"),
            Loc.F("statbd.val.flask", flaskHeal, charges),
            Loc.F("statbd.formula.flask_heal", HealingFlask.HealFraction * 100f),
            "",
            flaskSources,
            AffixLibrary.FlaskHealing));
    }

    static void BuildUtilityEntries(List<StatEntry> entries, Equipment eq, CharacterSheet sheet, GearEffectState gearState)
    {
        float focusCdMul = sheet.CooldownMultiplier();
        float rawGearCdr = eq != null ? eq.Total(AffixLibrary.CooldownReduction) : 0f;
        float gearCap = GearUniqueEffects.CooldownCap(eq, Equipment.MaxCooldownReductionPct);
        float gearCdFactor = eq != null ? eq.CooldownFactor() : 1f;
        float totalCdrPct = (1f - focusCdMul * gearCdFactor) * 100f;
        var cdrSources = new List<StatSourceLine>
        {
            new StatSourceLine(Loc.F("statbd.src.attr.foc_cd", sheet.focus, CharacterSheet.FocusCooldownPerPoint * 100f, CharacterSheet.MinCooldownFactor), Loc.F("statbd.val.mul", focusCdMul), focusCdMul < 1f),
        };
        cdrSources.AddRange(CollectSlotSources(eq, AffixLibrary.CooldownReduction, includeWeapon: true, percent: true));
        if (rawGearCdr > 0f || gearCap > Equipment.MaxCooldownReductionPct)
            cdrSources.Add(new StatSourceLine(Loc.F("statbd.src.gear_cdr", rawGearCdr, gearCap), Loc.F("statbd.val.mul", gearCdFactor), gearCdFactor < 1f));
        if (gearCap > Equipment.MaxCooldownReductionPct)
            cdrSources.Add(new StatSourceLine(Loc.F("statbd.src.unique", LootDrop.PrettyName(ItemDatabase.EmberwreathCrownId)), Loc.F("statbd.val.pct", gearCap), true));
        entries.Add(new StatEntry(
            "cooldown_reduction",
            StatCategory.Utility,
            Loc.T("statbd.name.cooldown_reduction"),
            "-" + Loc.F("statbd.val.pct", totalCdrPct),
            Loc.T("statbd.formula.cooldown_reduction"),
            CapNotePct(rawGearCdr, gearCap),
            cdrSources,
            AffixLibrary.CooldownReduction));

        bool hasted = gearState != null && gearState.Hasted;
        float baseSpeed = eq != null ? eq.GetMoveSpeed() : Equipment.BaseMoveSpeed;
        float moveSpeed = hasted ? baseSpeed * GearUniqueEffects.GhostwalkHasteFactor : baseSpeed;
        var speedSources = new List<StatSourceLine>
        {
            new StatSourceLine(Loc.T("statbd.src.base"), Loc.Num(Equipment.BaseMoveSpeed, "0.##")),
        };
        speedSources.AddRange(CollectSlotSources(eq, AffixLibrary.MoveSpeed, includeWeapon: false, format: "0.##"));
        if (hasted)
            speedSources.Add(new StatSourceLine(Loc.T("statbd.src.haste"), Loc.F("statbd.val.mul", GearUniqueEffects.GhostwalkHasteFactor), true));
        entries.Add(new StatEntry(
            "move_speed",
            StatCategory.Utility,
            Loc.T("statbd.name.move_speed"),
            Loc.Num(moveSpeed, "0.##"),
            Loc.F("statbd.formula.move_speed", Equipment.BaseMoveSpeed),
            "",
            speedSources,
            AffixLibrary.MoveSpeed));

        float rawArea = eq != null ? eq.Total(AffixLibrary.IncreasedArea) : 0f;
        float areaPct = eq != null ? eq.AreaBonusPct() : 0f;
        float areaMul = eq != null ? eq.ScaleArea(1f) : 1f;
        entries.Add(new StatEntry(
            "area_bonus",
            StatCategory.Utility,
            Loc.T("statbd.name.area_bonus"),
            Loc.F("statbd.val.area", areaPct, areaMul),
            Loc.F("statbd.formula.area_bonus", Equipment.MaxIncreasedAreaPct),
            CapNotePct(rawArea, Equipment.MaxIncreasedAreaPct),
            CollectSlotSources(eq, AffixLibrary.IncreasedArea, includeWeapon: true, percent: true),
            AffixLibrary.IncreasedArea));

        int skillRanks = eq != null ? eq.SkillRankBonus() : 0;
        entries.Add(new StatEntry(
            "skill_rank",
            StatCategory.Utility,
            Loc.T("statbd.name.skill_rank"),
            $"+{skillRanks}",
            Loc.T("statbd.formula.skill_rank"),
            "",
            CollectSlotSources(eq, AffixLibrary.SkillRank, includeWeapon: true),
            AffixLibrary.SkillRank));

        float xpMul = eq != null ? eq.GetXPMul() : 100f;
        var xpSources = new List<StatSourceLine>
        {
            new StatSourceLine(Loc.T("statbd.src.base"), Loc.F("statbd.val.pct", 100f)),
        };
        xpSources.AddRange(CollectSlotSources(eq, AffixLibrary.XPMul, includeWeapon: false, percent: true));
        entries.Add(new StatEntry(
            "xp_mul",
            StatCategory.Utility,
            Loc.T("statbd.name.xp_mul"),
            Loc.F("statbd.val.pct", xpMul),
            Loc.F("statbd.formula.xp_mul", 100f),
            "",
            xpSources,
            AffixLibrary.XPMul));

        float gf = eq != null ? eq.GoldFindPct() : 0f;
        entries.Add(new StatEntry(
            "gold_find",
            StatCategory.Utility,
            Loc.T("statbd.name.gold_find"),
            Loc.F("statbd.val.plus_pct", gf),
            Loc.T("statbd.formula.gold_find"),
            "",
            CollectSlotSources(eq, AffixLibrary.GoldFind, includeWeapon: true, percent: true),
            AffixLibrary.GoldFind));

        float mf = eq != null ? eq.MagicFindPct() : 0f;
        var mfSources = CollectSlotSources(eq, AffixLibrary.MagicFind, includeWeapon: true, percent: true);
        float extraMf = GearUniqueEffects.ExtraMagicFind(eq, gf);
        if (extraMf > 0f || GearUniqueEffects.Wears(eq, ItemDatabase.ProspectorsHoodId))
            mfSources.Add(new StatSourceLine(Loc.F("statbd.src.unique", LootDrop.PrettyName(ItemDatabase.ProspectorsHoodId)), Loc.F("statbd.val.plus_pct", extraMf), true));
        entries.Add(new StatEntry(
            "magic_find",
            StatCategory.Utility,
            Loc.T("statbd.name.magic_find"),
            Loc.F("statbd.val.plus_pct", mf),
            Loc.T("statbd.formula.magic_find"),
            "",
            mfSources,
            AffixLibrary.MagicFind));
    }

    static List<StatSourceLine> CollectSlotSources(Equipment eq, string key, bool includeWeapon, bool percent = false, string format = "0.#")
    {
        var list = new List<StatSourceLine>();
        if (eq == null) return list;
        foreach (var slot in Equipment.GearSlots)
        {
            if (!includeWeapon && slot == ItemSlot.Weapon) continue;
            var item = eq.GetEquipped(slot);
            if (item == null) continue;
            float val = item.GetAffix(key);
            if (Mathf.Abs(val) < 0.0001f) continue;
            string formatted = percent ? Loc.F("statbd.val.plus_pct", val) : $"+{Loc.Num(val, format)}";
            list.Add(new StatSourceLine(Loc.F("statbd.src.slot", ItemText.Slot(slot), LootDrop.ItemLabel(item)), formatted));
        }
        return list;
    }

    static string CapNotePct(float raw, float cap)
    {
        string capStr = Loc.F("statbd.val.pct", cap);
        string rawStr = Loc.F("statbd.val.pct", raw);
        return raw > cap
            ? Loc.F("statbd.note.cap_hit", capStr, rawStr)
            : Loc.F("statbd.note.cap_ok", capStr, rawStr);
    }
}
