using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// S4 point-spend choices (pure C# logic, no MonoBehaviour): one attribute
/// point (Strength → melee damage, Vitality → life, Focus → skill damage and
/// cooldown) and one skill point (rank 1–5 per skill, +20 % damage per rank)
/// per level on top of the flat <c>+3 Life/Level</c> base. Owned by
/// <see cref="DiabloProgression"/> (one sheet per hero, solo-local like the
/// rest of the session state) and persisted through <see cref="SaveGame"/>
/// (S2 fields, filled by S4).
/// </summary>
public sealed class CharacterSheet
{
    /// <summary>Highest rank a single skill can reach.</summary>
    public const int MaxSkillRank = 5;
    /// <summary>Damage bonus per skill rank (multiplicative with focus).</summary>
    public const float RankDamageBonus = 0.20f;
    /// <summary>Melee damage per Strength point.</summary>
    public const int StrengthMeleePerPoint = 1;
    /// <summary>Max-life per Vitality point.</summary>
    public const float VitalityLifePerPoint = 5f;
    /// <summary>Skill-damage bonus per Focus point (multiplicative with rank).</summary>
    public const float FocusSkillDamagePerPoint = 0.08f;
    /// <summary>Cooldown reduction per Focus point (floored, see below).</summary>
    public const float FocusCooldownPerPoint = 0.03f;
    /// <summary>Cooldowns never drop below half their base.</summary>
    public const float MinCooldownFactor = 0.5f;

    public enum Attribute { Strength, Vitality, Focus }

    public int freeAttributePoints;
    public int freeSkillPoints;
    public int strength;
    public int vitality;
    public int focus;
    public int fireballRank;
    public int arrowRank;
    public int cleaveRank;

    /// <summary>True while any point is unspent (drives the UI pulse).</summary>
    public bool HasUnspent => freeAttributePoints > 0 || freeSkillPoints > 0;

    /// <summary>One level grants one attribute and one skill point.</summary>
    public void AwardLevel()
    {
        freeAttributePoints++;
        freeSkillPoints++;
    }

    /// <summary>Spend one attribute point. False when none is free.</summary>
    public bool SpendAttribute(Attribute attribute)
    {
        if (freeAttributePoints <= 0) return false;
        freeAttributePoints--;
        switch (attribute)
        {
            case Attribute.Strength: strength++; break;
            case Attribute.Vitality: vitality++; break;
            default: focus++; break;
        }
        return true;
    }

    /// <summary>Current rank of a skill (0 = unranked, still castable).</summary>
    public int GetRank(DiabloSkills.Skill skill)
    {
        switch (skill)
        {
            case DiabloSkills.Skill.Fireball: return fireballRank;
            case DiabloSkills.Skill.Arrow: return arrowRank;
            case DiabloSkills.Skill.Cleave: return cleaveRank;
            default: return 0; // Lanternbearer ranks arrive in second-class S3
        }
    }

    /// <summary>Spend one skill point on a rank. False when none is free or capped.</summary>
    public bool SpendSkill(DiabloSkills.Skill skill)
    {
        if (freeSkillPoints <= 0 || GetRank(skill) >= MaxSkillRank) return false;
        switch (skill)
        {
            case DiabloSkills.Skill.Fireball: fireballRank++; break;
            case DiabloSkills.Skill.Arrow: arrowRank++; break;
            case DiabloSkills.Skill.Cleave: cleaveRank++; break;
            default: return false; // Lanternbearer ranks arrive in second-class S3
        }
        freeSkillPoints--;
        return true;
    }

    /// <summary>Spent rank plus "+N to all Skills" from gear (replayability S2).</summary>
    public int EffectiveRank(DiabloSkills.Skill skill, int gearRankBonus = 0) =>
        GetRank(skill) + System.Math.Max(0, gearRankBonus);

    /// <summary>Rank multiplier for a skill (1 + 20 % per effective rank).</summary>
    public float RankMultiplier(DiabloSkills.Skill skill, int gearRankBonus = 0) =>
        1f + RankDamageBonus * EffectiveRank(skill, gearRankBonus);

    /// <summary>Focus skill-damage multiplier (1 + 8 % per point).</summary>
    public float FocusSkillMultiplier() => 1f + FocusSkillDamagePerPoint * focus;

    /// <summary>Total skill-damage multiplier: rank × focus.</summary>
    public float SkillMultiplier(DiabloSkills.Skill skill, int gearRankBonus = 0) =>
        RankMultiplier(skill, gearRankBonus) * FocusSkillMultiplier();

    /// <summary>Cooldown factor from focus (floored at half base).</summary>
    public float CooldownMultiplier() =>
        Mathf.Max(MinCooldownFactor, 1f - FocusCooldownPerPoint * focus);

    /// <summary>Flat melee-damage bonus from strength.</summary>
    public int MeleeBonus() => strength * StrengthMeleePerPoint;

    /// <summary>Max-life bonus from vitality (on top of the +3/level base).</summary>
    public float VitalityLife() => vitality * VitalityLifePerPoint;

    /// <summary>Cleave radius grows with rank (S4: Rang erweitert Radius).</summary>
    public float CleaveRadius(float baseRadius, float perRank) =>
        baseRadius + perRank * cleaveRank;

    /// <summary>Export ranks for <see cref="SaveGame.skillRanks"/>.</summary>
    public List<SkillRankEntry> ToEntries() => new List<SkillRankEntry>
    {
        new SkillRankEntry { skill = DiabloSkills.Skill.Fireball.ToString(), rank = fireballRank },
        new SkillRankEntry { skill = DiabloSkills.Skill.Arrow.ToString(), rank = arrowRank },
        new SkillRankEntry { skill = DiabloSkills.Skill.Cleave.ToString(), rank = cleaveRank },
    };

    /// <summary>Import ranks from a save (unknown/missing skills stay 0).</summary>
    public void FromEntries(List<SkillRankEntry> entries)
    {
        fireballRank = arrowRank = cleaveRank = 0;
        if (entries == null) return;
        foreach (var e in entries)
        {
            if (e == null || string.IsNullOrEmpty(e.skill)) continue;
            int rank = Mathf.Clamp(e.rank, 0, MaxSkillRank);
            switch (e.skill)
            {
                case "Fireball": fireballRank = rank; break;
                case "Arrow": arrowRank = rank; break;
                case "Cleave": cleaveRank = rank; break;
            }
        }
    }

    /// <summary>Sheet of a hero (null-safe; tests use the class directly).</summary>
    public static CharacterSheet For(GameObject hero)
    {
        var prog = hero != null ? hero.GetComponent<DiabloProgression>() : null;
        return prog != null ? prog.Sheet : null;
    }
}
