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
    // Lanternbearer (second-class S3). Ranks of the other class stay stored, they just never apply.
    public int lanternBoltRank;
    public int wispsRank;
    public int haloRank;
    public int veilRank;
    // Tank slam (party-roles S2).
    public int slamRank;
    // Rogue + Healer specialties (party-roles S3).
    public int quickRank;
    public int flurryRank;
    public int mendRank;
    public int renewRank;

    /// <summary>True while any point is unspent (drives the UI pulse).</summary>
    /// <summary>Ranks of the class-kits-parity skills (MireHook and later), keyed by skill.</summary>
    readonly Dictionary<DiabloSkills.Skill, int> kitRanks = new Dictionary<DiabloSkills.Skill, int>();

    static bool IsKitSkill(DiabloSkills.Skill skill) => skill >= DiabloSkills.Skill.MireHook;

    /// <summary>
    /// class-kits-parity: ranks on skills outside the hero's kit (Fenwalker and
    /// Rogue lost Fireball/Arrow) go back as free skill points. Idempotent:
    /// refunded ranks are zero afterwards. Returns the points refunded.
    /// </summary>
    public int RefundForeignRanks(ClassDefinition cls)
    {
        if (cls == null) return 0;
        int refunded = 0;
        foreach (DiabloSkills.Skill skill in System.Enum.GetValues(typeof(DiabloSkills.Skill)))
        {
            if (cls.Has(skill)) continue;
            int r = GetRank(skill);
            if (r <= 0) continue;
            SetRank(skill, 0);
            refunded += r;
        }
        freeSkillPoints += refunded;
        return refunded;
    }

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
            case DiabloSkills.Skill.LanternBolt: return lanternBoltRank;
            case DiabloSkills.Skill.Wisps: return wispsRank;
            case DiabloSkills.Skill.SearingHalo: return haloRank;
            case DiabloSkills.Skill.VeilStep: return veilRank;
            case DiabloSkills.Skill.SeismicSlam: return slamRank;
            case DiabloSkills.Skill.QuickShot: return quickRank;
            case DiabloSkills.Skill.BladeFlurry: return flurryRank;
            case DiabloSkills.Skill.MendingLight: return mendRank;
            case DiabloSkills.Skill.RenewingCircle: return renewRank;
            case var k when IsKitSkill(k): return kitRanks.TryGetValue(k, out int kr) ? kr : 0;
            default: return 0;
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
            case DiabloSkills.Skill.LanternBolt: lanternBoltRank++; break;
            case DiabloSkills.Skill.Wisps: wispsRank++; break;
            case DiabloSkills.Skill.SearingHalo: haloRank++; break;
            case DiabloSkills.Skill.VeilStep: veilRank++; break;
            case DiabloSkills.Skill.SeismicSlam: slamRank++; break;
            case DiabloSkills.Skill.QuickShot: quickRank++; break;
            case DiabloSkills.Skill.BladeFlurry: flurryRank++; break;
            case DiabloSkills.Skill.MendingLight: mendRank++; break;
            case DiabloSkills.Skill.RenewingCircle: renewRank++; break;
            case var k when IsKitSkill(k): kitRanks[k] = GetRank(k) + 1; break;
            default: return false;
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

    /// <summary>Focus bonus scaled by the class (Lanternbearer 1.5, Fenwalker 1 = unchanged).</summary>
    public float FocusSkillMultiplier(float classFocusFactor) =>
        1f + FocusSkillDamagePerPoint * focus * classFocusFactor;

    /// <summary>Total skill-damage multiplier: rank × focus.</summary>
    public float SkillMultiplier(DiabloSkills.Skill skill, int gearRankBonus = 0, float classFocusFactor = 1f) =>
        RankMultiplier(skill, gearRankBonus) * FocusSkillMultiplier(classFocusFactor);

    /// <summary>
    /// Party specialty multiplier (party-roles S1, pure formula, wired into damage in S3):
    /// rank × focus(class) × specialtyFactor for specialty skills, rank × focus(class) for base skills.
    /// </summary>
    public float SpecialtySkillMultiplier(DiabloSkills.Skill skill, ClassDefinition cls, int gearRankBonus = 0)
    {
        float baseMul = SkillMultiplier(skill, gearRankBonus, cls != null ? cls.focusSkillFactor : 1f);
        if (cls != null && cls.IsSpecialty(skill)) baseMul *= cls.specialtyFactor;
        return baseMul;
    }

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

    void SetRank(DiabloSkills.Skill skill, int rank)
    {
        switch (skill)
        {
            case DiabloSkills.Skill.Fireball: fireballRank = rank; break;
            case DiabloSkills.Skill.Arrow: arrowRank = rank; break;
            case DiabloSkills.Skill.Cleave: cleaveRank = rank; break;
            case DiabloSkills.Skill.LanternBolt: lanternBoltRank = rank; break;
            case DiabloSkills.Skill.Wisps: wispsRank = rank; break;
            case DiabloSkills.Skill.SearingHalo: haloRank = rank; break;
            case DiabloSkills.Skill.VeilStep: veilRank = rank; break;
            case DiabloSkills.Skill.SeismicSlam: slamRank = rank; break;
            case DiabloSkills.Skill.QuickShot: quickRank = rank; break;
            case DiabloSkills.Skill.BladeFlurry: flurryRank = rank; break;
            case DiabloSkills.Skill.MendingLight: mendRank = rank; break;
            case DiabloSkills.Skill.RenewingCircle: renewRank = rank; break;
            default:
                if (rank <= 0) kitRanks.Remove(skill); else kitRanks[skill] = rank;
                break;
        }
    }

    /// <summary>Export ranks for <see cref="SaveGame.skillRanks"/>.</summary>
    public List<SkillRankEntry> ToEntries()
    {
        var list = LegacyEntries();
        foreach (var kv in kitRanks)
            if (kv.Value > 0) list.Add(new SkillRankEntry { skill = kv.Key.ToString(), rank = kv.Value });
        return list;
    }

    List<SkillRankEntry> LegacyEntries() => new List<SkillRankEntry>
    {
        new SkillRankEntry { skill = DiabloSkills.Skill.Fireball.ToString(), rank = fireballRank },
        new SkillRankEntry { skill = DiabloSkills.Skill.Arrow.ToString(), rank = arrowRank },
        new SkillRankEntry { skill = DiabloSkills.Skill.Cleave.ToString(), rank = cleaveRank },
        new SkillRankEntry { skill = DiabloSkills.Skill.LanternBolt.ToString(), rank = lanternBoltRank },
        new SkillRankEntry { skill = DiabloSkills.Skill.Wisps.ToString(), rank = wispsRank },
        new SkillRankEntry { skill = DiabloSkills.Skill.SearingHalo.ToString(), rank = haloRank },
        new SkillRankEntry { skill = DiabloSkills.Skill.VeilStep.ToString(), rank = veilRank },
        new SkillRankEntry { skill = DiabloSkills.Skill.SeismicSlam.ToString(), rank = slamRank },
        new SkillRankEntry { skill = DiabloSkills.Skill.QuickShot.ToString(), rank = quickRank },
        new SkillRankEntry { skill = DiabloSkills.Skill.BladeFlurry.ToString(), rank = flurryRank },
        new SkillRankEntry { skill = DiabloSkills.Skill.MendingLight.ToString(), rank = mendRank },
        new SkillRankEntry { skill = DiabloSkills.Skill.RenewingCircle.ToString(), rank = renewRank },
    };

    /// <summary>Import ranks from a save (unknown/missing skills stay 0).</summary>
    public void FromEntries(List<SkillRankEntry> entries)
    {
        fireballRank = arrowRank = cleaveRank = 0;
        lanternBoltRank = wispsRank = haloRank = veilRank = slamRank = 0;
        quickRank = flurryRank = mendRank = renewRank = 0;
        kitRanks.Clear();
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
                case "LanternBolt": lanternBoltRank = rank; break;
                case "Wisps": wispsRank = rank; break;
                case "SearingHalo": haloRank = rank; break;
                case "VeilStep": veilRank = rank; break;
                case "SeismicSlam": slamRank = rank; break;
                case "QuickShot": quickRank = rank; break;
                case "BladeFlurry": flurryRank = rank; break;
                case "MendingLight": mendRank = rank; break;
                case "RenewingCircle": renewRank = rank; break;
                default:
                    if (System.Enum.TryParse(e.skill, out DiabloSkills.Skill k) && IsKitSkill(k) && rank > 0)
                        kitRanks[k] = rank;
                    break;
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
