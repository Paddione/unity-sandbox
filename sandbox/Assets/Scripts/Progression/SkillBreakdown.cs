using System.Text;
using UnityEngine;

/// <summary>
/// Read-only live breakdown for one skill's effective damage/healing,
/// rank + gear bonus, focus/specialty multipliers, cooldown, and scaled AoE radius.
/// </summary>
public static class SkillBreakdown
{
    public readonly struct SkillTooltipData
    {
        public readonly string title;
        public readonly string meta;
        public readonly string body;

        public SkillTooltipData(string title, string meta, string body)
        {
            this.title = title;
            this.meta = meta;
            this.body = body;
        }
    }

    public static SkillTooltipData BuildFor(GameObject hero, DiabloSkills.Skill skill)
    {
        var skills = hero != null ? hero.GetComponent<DiabloSkills>() : null;
        var eq = hero != null ? hero.GetComponent<Equipment>() : null;
        var prog = hero != null ? hero.GetComponent<DiabloProgression>() : null;
        var sheet = prog != null ? prog.Sheet : new CharacterSheet();
        var cls = skills != null ? skills.ActiveClass : RunProfile.Class;
        return Build(skill, skills, sheet, eq, cls);
    }

    public static SkillTooltipData Build(DiabloSkills.Skill skill, DiabloSkills skills,
        CharacterSheet sheet, Equipment eq, ClassDefinition cls)
    {
        cls = cls ?? RunProfile.Class;
        sheet = sheet ?? new CharacterSheet();

        string name = DiabloSkills.SkillName(skill);
        string keyLabel = KeyLabelFor(cls, skill);
        int rank = sheet.GetRank(skill);
        int gearRanks = eq != null ? eq.SkillRankBonus() : 0;
        int effRank = sheet.EffectiveRank(skill, gearRanks);

        string meta = gearRanks > 0
            ? Loc.F("statbd.skill.meta_gear", keyLabel, cls.displayName, rank, CharacterSheet.MaxSkillRank, gearRanks)
            : Loc.F("statbd.skill.meta", keyLabel, cls.displayName, rank, CharacterSheet.MaxSkillRank);

        var sb = new StringBuilder();
        sb.AppendLine(DiabloSkills.SkillDescription(skill));

        string variantId = skills != null
            ? skills.ActiveVariantId(skill)
            : RunProfile.GetSkillVariant(cls.id, skill);
        if (!string.IsNullOrEmpty(variantId))
        {
            string variantName = SkillVariantCatalog.DisplayName(cls.id, skill, variantId);
            sb.AppendLine(Loc.F("statbd.skill.variant", variantName));
        }

        sb.AppendLine();

        bool isHeal = skill == DiabloSkills.Skill.MendingLight || skill == DiabloSkills.Skill.RenewingCircle;
        int baseDmg = BaseDamageFor(skill, skills, eq);
        float rankMul = sheet.RankMultiplier(skill, gearRanks);
        float focusMul = sheet.FocusSkillMultiplier(cls.focusSkillFactor);
        bool hasSpecialty = DiabloSkills.IsPartySkill(skill) && cls.IsSpecialty(skill) && !Mathf.Approximately(cls.specialtyFactor, 1f);

        if (isHeal)
        {
            int baseHeal = skill == DiabloSkills.Skill.MendingLight
                ? (skills != null ? skills.mendHeal : 8)
                : (skills != null ? skills.renewHeal : 5);
            float healSpec = cls.IsSpecialty(skill) && !Mathf.Approximately(cls.specialtyFactor, 1f) ? cls.specialtyFactor : 1f;
            float uniqueHeal = GearUniqueEffects.HealMultiplier(eq);
            int effHeal = skills != null
                ? skills.EffectiveHeal(skill)
                : Mathf.Max(1, Mathf.RoundToInt(baseHeal * sheet.SpecialtySkillMultiplier(skill, cls, gearRanks) * uniqueHeal));

            sb.AppendLine(Loc.F("statbd.skill.base_heal", baseHeal));
            sb.AppendLine(Loc.F("statbd.skill.rank_mul", effRank, rankMul));
            sb.AppendLine(Loc.F("statbd.skill.focus_mul", sheet.focus, cls.focusSkillFactor, focusMul));
            if (!Mathf.Approximately(healSpec, 1f))
                sb.AppendLine(Loc.F("statbd.skill.spec_mul", healSpec));
            if (!Mathf.Approximately(uniqueHeal, 1f))
                sb.AppendLine(Loc.F("statbd.skill.unique_heal", uniqueHeal));
            sb.AppendLine(Loc.F("statbd.skill.eff_heal", effHeal));
        }
        else if (baseDmg > 0)
        {
            int weaponBonus = (skill == DiabloSkills.Skill.Arrow || skill == DiabloSkills.Skill.Cleave) && eq != null
                ? Mathf.Max(0, Mathf.RoundToInt(eq.GetDamage() - Equipment.BaseDamage))
                : 0;
            int effDmg = skills != null
                ? skills.EffectiveDamage(skill)
                : Mathf.Max(1, Mathf.RoundToInt(baseDmg * rankMul * focusMul * (hasSpecialty ? cls.specialtyFactor : 1f)));

            sb.AppendLine(weaponBonus > 0
                ? Loc.F("statbd.skill.base_dmg", baseDmg, weaponBonus)
                : Loc.F("statbd.skill.base_dmg_plain", baseDmg));
            sb.AppendLine(Loc.F("statbd.skill.rank_mul", effRank, rankMul));
            sb.AppendLine(Loc.F("statbd.skill.focus_mul", sheet.focus, cls.focusSkillFactor, focusMul));
            if (hasSpecialty)
                sb.AppendLine(Loc.F("statbd.skill.spec_mul", cls.specialtyFactor));
            sb.AppendLine(Loc.F("statbd.skill.eff_dmg", effDmg));
        }
        else
        {
            sb.AppendLine(Loc.T("statbd.skill.utility"));
        }

        float baseCd = BaseCooldownFor(skill, skills);
        float focusCd = sheet.CooldownMultiplier();
        float gearCd = eq != null ? eq.CooldownFactor() : 1f;
        float effCd = skills != null ? skills.EffectiveCooldown(skill) : baseCd * focusCd * gearCd;
        sb.AppendLine(Loc.F("statbd.skill.cd", effCd, baseCd, focusCd, gearCd));

        float baseRadius = BaseRadiusFor(skill, skills, sheet, variantId);
        if (baseRadius > 0f)
        {
            float areaMul = eq != null ? eq.ScaleArea(1f) : 1f;
            float scaled = eq != null ? eq.ScaleArea(baseRadius) : baseRadius;
            sb.AppendLine(Loc.F("statbd.skill.area", scaled, baseRadius, areaMul));
        }

        return new SkillTooltipData(name, meta, sb.ToString().TrimEnd());
    }

    static string KeyLabelFor(ClassDefinition cls, DiabloSkills.Skill skill)
    {
        if (cls != null && cls.skills != null)
        {
            for (int i = 0; i < cls.skills.Length; i++)
                if (cls.skills[i] == skill) return cls.KeyLabel(i);
        }
        return "-";
    }

    static int BaseDamageFor(DiabloSkills.Skill skill, DiabloSkills skills, Equipment eq)
    {
        if (skills != null) return skills.BaseDamage(skill);
        int w = eq != null ? Mathf.Max(0, Mathf.RoundToInt(eq.GetDamage() - Equipment.BaseDamage)) : 0;
        switch (skill)
        {
            case DiabloSkills.Skill.Fireball: return 3;
            case DiabloSkills.Skill.Arrow: return 2 + w;
            case DiabloSkills.Skill.Cleave: return 4 + w;
            case DiabloSkills.Skill.LanternBolt: return 4;
            case DiabloSkills.Skill.Wisps: return 1;
            case DiabloSkills.Skill.SearingHalo: return 3;
            case DiabloSkills.Skill.SeismicSlam: return 3;
            case DiabloSkills.Skill.QuickShot: return 2;
            case DiabloSkills.Skill.BladeFlurry: return 2;
            case DiabloSkills.Skill.MireHook: return ClassCombos.MireHookDamage;
            case DiabloSkills.Skill.BogCharge: return ClassCombos.BogChargeDamage;
            case DiabloSkills.Skill.MireGround: return ClassCombos.MireGroundDamage;
            case DiabloSkills.Skill.VenomVial: return ClassCombos.VenomImpactDamage;
            case DiabloSkills.Skill.PowderKeg: return ClassCombos.PowderKegDamage;
            default: return 0;
        }
    }

    static float BaseCooldownFor(DiabloSkills.Skill skill, DiabloSkills skills)
    {
        if (skills != null) return skills.BaseCooldown(skill);
        switch (skill)
        {
            case DiabloSkills.Skill.Fireball: return 1.0f;
            case DiabloSkills.Skill.Arrow: return 0.35f;
            case DiabloSkills.Skill.LanternBolt: return 1.2f;
            case DiabloSkills.Skill.Wisps: return 14f;
            case DiabloSkills.Skill.SearingHalo: return 7f;
            case DiabloSkills.Skill.VeilStep: return 5f;
            case DiabloSkills.Skill.SeismicSlam: return 8f;
            case DiabloSkills.Skill.QuickShot: return 0.3f;
            case DiabloSkills.Skill.BladeFlurry: return 4f;
            case DiabloSkills.Skill.MendingLight: return DiabloSkills.MendBaseCooldown;
            case DiabloSkills.Skill.RenewingCircle: return DiabloSkills.RenewBaseCooldown;
            case DiabloSkills.Skill.MireHook: return ClassCombos.MireHookCooldown;
            case DiabloSkills.Skill.BogCharge: return ClassCombos.BogChargeCooldown;
            case DiabloSkills.Skill.MireGround: return ClassCombos.MireGroundCooldown;
            case DiabloSkills.Skill.IronVow: return ClassCombos.IronVowCooldown;
            case DiabloSkills.Skill.SmokePot: return ClassCombos.SmokeCooldown;
            case DiabloSkills.Skill.ShadowRoll: return ClassCombos.ShadowRollCooldown;
            case DiabloSkills.Skill.VenomVial: return ClassCombos.VenomCooldown;
            case DiabloSkills.Skill.PowderKeg: return ClassCombos.PowderKegCooldown;
            default: return 6f;
        }
    }

    static float BaseRadiusFor(DiabloSkills.Skill skill, DiabloSkills skills, CharacterSheet sheet, string variantId)
    {
        switch (skill)
        {
            case DiabloSkills.Skill.Fireball: return skills != null ? skills.fireballBlastRadius : 2.5f;
            case DiabloSkills.Skill.Cleave:
            {
                float r = skills != null ? skills.CleaveRadius : sheet.CleaveRadius(2.6f, 0.4f);
                return variantId == SkillVariantCatalog.CleaveFrontalId ? r * SkillVariantCatalog.FrontalRangeFactor : r;
            }
            case DiabloSkills.Skill.SearingHalo: return skills != null ? skills.haloRadius : 3.5f;
            case DiabloSkills.Skill.SeismicSlam: return skills != null ? skills.slamRadius : 3.5f;
            case DiabloSkills.Skill.BladeFlurry: return skills != null ? skills.flurryRadius : 2.4f;
            case DiabloSkills.Skill.RenewingCircle: return skills != null ? skills.groupHealRadius : DiabloSkills.GroupHealBaseRange;
            case DiabloSkills.Skill.MireGround: return ClassCombos.MireGroundRadius;
            case DiabloSkills.Skill.IronVow: return ClassCombos.IronVowTauntRadius;
            case DiabloSkills.Skill.SmokePot: return ClassCombos.SmokeRadius;
            case DiabloSkills.Skill.VenomVial: return ClassCombos.VenomRadius;
            case DiabloSkills.Skill.PowderKeg: return ClassCombos.PowderKegRadius;
            default: return 0f;
        }
    }
}
