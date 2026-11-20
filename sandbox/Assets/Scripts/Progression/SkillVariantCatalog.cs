using UnityEngine;

/// <summary>
/// Skill variants with noticeable trade-offs (cinderreach-skill-variants S1).
/// Each variant-bearing class skill (Fenwalker Cleave, Lanternbearer Searing
/// Halo) has exactly two variants with a stable string id. The first id is
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
            case CleaveRoundId: return "Whirl";
            case CleaveFrontalId: return "Frontal Arc";
            case HaloFollowId: return "Moving Aura";
            case HaloWardId: return "Fire Ward";
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
                return $"Whirl: 360° around you, {CleaveRadiusFor(skills, id):0.0} m.";
            case CleaveFrontalId:
                return $"Frontal Arc: {FrontalArcDegrees:0}° cone, {CleaveRadiusFor(skills, id):0.0} m reach (x{FrontalRangeFactor:0.0}).";
            case HaloFollowId:
                return "Moving Aura: instant ring that travels with you.";
            case HaloWardId:
                return $"Fire Ward: stays where cast, {HaloWardDuration:0} s, ticks every {HaloTickInterval:0.0} s, same area.";
            default:
                return string.Empty;
        }
    }
}
