using UnityEngine;

/// <summary>
/// Skill variants with noticeable trade-offs (cinderreach-skill-variants S1).
/// Each variant-bearing class skill (Fenwalker Cleave, Lanternbearer Searing
/// Halo, Rogue Quick Shot since class-kits-parity) has exactly two variants with a stable string id. The first id is
/// the legacy behavior and the fallback for missing, unknown or
/// class-foreign ids, so old saves keep playing unchanged. All tuning lives
/// here so the character sheet preview and the runtime use the same values.
/// </summary>
public static class SkillVariantCatalog
{
    // ---- stable ids ----
    public const string CleaveRoundId = "cleave_round";
    public const string CleaveFrontalId = "cleave_frontal";
    public const string HaloFollowId = "halo_follow";
    public const string HaloWardId = "halo_ward";
    public const string QuickSingleId = "quick_single";
    public const string QuickFanId = "quick_fan";

    // ---- shared tuning (preview == runtime) ----
    /// <summary>Frontal cleave: total cone angle in degrees (planar).</summary>
    public const float FrontalArcDegrees = 90f;
    /// <summary>Frontal cleave reaches this much further than the round swing.</summary>
    public const float FrontalRangeFactor = 1.5f;
    /// <summary>Halo ticks keep their interval on both variants.</summary>
    public const float HaloTickInterval = 1.0f;
    /// <summary>Base halo effect window; the ward doubles it.</summary>
    public const float HaloBaseDuration = 4.0f;
    /// <summary>Stationary ward duration (double the base window).</summary>
    public static float HaloWardDuration => HaloBaseDuration * 2f;
    /// <summary>Fan Quick Shot: bolts per cast.</summary>
    public const int FanBolts = 3;
    /// <summary>Fan Quick Shot: yaw between neighbouring bolts (degrees).</summary>
    public const float FanSpreadDegrees = 15f;
    /// <summary>Fan Quick Shot: damage share per bolt.</summary>
    public const float FanDamageFactor = 0.6f;

    /// <summary>Per-bolt damage of the fan (never below 1).</summary>
    public static int FanBoltDamage(int single) => Mathf.Max(1, Mathf.RoundToInt(single * FanDamageFactor));

    // ---- Lantern Hold town boundary (spatial, not act-based) ----
    /// <summary>Palisade ring center (TownBuilder.BuildCamp).</summary>
    public static readonly Vector3 TownCenter = new Vector3(0f, 0f, 11f);
    /// <summary>Palisade radius 6 plus a step of slack; the Ashen Fen shrine (-15,-4) stays far outside.</summary>
    public const float TownRadius = 6.5f;

    /// <summary>Planar check: is this world position inside the Lantern Hold camp?</summary>
    public static bool IsInLanternHold(Vector3 pos)
    {
        var d = pos - TownCenter;
        d.y = 0f;
        return d.sqrMagnitude <= TownRadius * TownRadius;
    }

    /// <summary>Variant switch only solo inside the Hold (Fen counts as another area despite sharing the act).</summary>
    public static bool CanChangeAt(Vector3 heroPos, bool solo) => solo && IsInLanternHold(heroPos);

    /// <summary>The two variant ids of a skill, or empty when the skill has none.</summary>
    public static string[] VariantIds(string classId, DiabloSkills.Skill skill)
    {
        if (classId == ClassCatalog.FenwalkerId && skill == DiabloSkills.Skill.Cleave)
            return new[] { CleaveRoundId, CleaveFrontalId };
        if (classId == ClassCatalog.LanternbearerId && skill == DiabloSkills.Skill.SearingHalo)
            return new[] { HaloFollowId, HaloWardId };
        if (classId == ClassCatalog.RogueId && skill == DiabloSkills.Skill.QuickShot)
            return new[] { QuickSingleId, QuickFanId };
        return new string[0];
    }

    /// <summary>Legacy behavior: first variant, null when the skill has none.</summary>
    public static string DefaultVariantId(string classId, DiabloSkills.Skill skill)
    {
        var ids = VariantIds(classId, skill);
        return ids.Length > 0 ? ids[0] : null;
    }

    /// <summary>True when id is one of the skill's two variants.</summary>
    public static bool IsKnownVariant(string classId, DiabloSkills.Skill skill, string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        foreach (var v in VariantIds(classId, skill))
            if (v == id) return true;
        return false;
    }

    /// <summary>Requested id when known and class-owned, else the legacy default (null without variants).</summary>
    public static string Resolve(string classId, DiabloSkills.Skill skill, string requested) =>
        IsKnownVariant(classId, skill, requested) ? requested : DefaultVariantId(classId, skill);

    /// <summary>Display name of a variant (resolved first, so never blank for variant skills).</summary>
    public static string DisplayName(string classId, DiabloSkills.Skill skill, string variantId)
    {
        string id = Resolve(classId, skill, variantId);
        switch (id)
        {
            case CleaveRoundId: return Loc.T("char.variant.whirl");
            case CleaveFrontalId: return Loc.T("char.variant.frontal");
            case HaloFollowId: return Loc.T("char.variant.aura");
            case HaloWardId: return Loc.T("char.variant.ward");
            case QuickSingleId: return Loc.T("char.variant.single");
            case QuickFanId: return Loc.T("char.variant.fan");
            default: return DiabloSkills.SkillName(skill);
        }
    }

    /// <summary>Effective cleave radius for a variant (rank bonus included).</summary>
    public static float CleaveRadiusFor(DiabloSkills skills, string variantId)
    {
        float baseR = skills != null ? skills.CleaveRadius : 2.6f;
        return variantId == CleaveFrontalId ? baseR * FrontalRangeFactor : baseR;
    }

    /// <summary>Total cone angle for a cleave variant (360 = rundum).</summary>
    public static float CleaveArcFor(string variantId) =>
        variantId == CleaveFrontalId ? FrontalArcDegrees : 360f;

    /// <summary>Halo effect duration in seconds (0 = instant, mitlaufend).</summary>
    public static float HaloDurationFor(string variantId) =>
        variantId == HaloWardId ? HaloWardDuration : 0f;

    /// <summary>One-line sheet preview; values match the runtime above.</summary>
    public static string Describe(string classId, DiabloSkills.Skill skill, string variantId, DiabloSkills skills)
    {
        string id = Resolve(classId, skill, variantId);
        switch (id)
        {
            case CleaveRoundId:
                return Loc.F("char.variant.whirl.desc", CleaveRadiusFor(skills, id));
            case CleaveFrontalId:
                return Loc.F("char.variant.frontal.desc", FrontalArcDegrees, CleaveRadiusFor(skills, id), FrontalRangeFactor);
            case HaloFollowId:
                return Loc.T("char.variant.aura.desc");
            case HaloWardId:
                return Loc.F("char.variant.ward.desc", HaloWardDuration, HaloTickInterval);
            case QuickSingleId:
                return Loc.F("char.variant.single.desc", skills != null ? skills.EffectiveDamage(DiabloSkills.Skill.QuickShot) : 3);
            case QuickFanId:
                return Loc.F("char.variant.fan.desc", FanBolts, FanSpreadDegrees,
                    FanBoltDamage(skills != null ? skills.EffectiveDamage(DiabloSkills.Skill.QuickShot) : 3));
            default:
                return string.Empty;
        }
    }
}
