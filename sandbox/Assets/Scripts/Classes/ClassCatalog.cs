using UnityEngine;

/// <summary>
/// The playable classes (cinderreach-second-class). Fenwalker is exactly the
/// pre-class hero (Fireball/Arrow/Cleave on Q/E/R, factors 1) and is the
/// fallback for missing or unknown ids, so old saves keep playing unchanged.
/// </summary>
public static class ClassCatalog
{
    public const string FenwalkerId = "fenwalker";
    public const string LanternbearerId = "lanternbearer";
    public const string RogueId = "rogue";

    public static readonly ClassDefinition Fenwalker = new ClassDefinition(
        FenwalkerId, "Fenwalker",
        "Sword, bow and fire. Cleaves through husk packs, holds the line.",
        new[] { DiabloSkills.Skill.Fireball, DiabloSkills.Skill.Arrow, DiabloSkills.Skill.Cleave, DiabloSkills.Skill.SeismicSlam },
        new[] { "Q", "E", "R", "Space" },
        1f, 1f, "Adventurers/Characters/Knight.fbx",
        ClassDefinition.PartyRole.Tank, 1f);

    public static readonly ClassDefinition Lanternbearer = new ClassDefinition(
        LanternbearerId, "Lanternbearer",
        "Carries Lantern Hold's light. Wisps fight beside her, the halo burns what comes close.",
        new[] { DiabloSkills.Skill.LanternBolt, DiabloSkills.Skill.Wisps, DiabloSkills.Skill.SearingHalo, DiabloSkills.Skill.VeilStep },
        new[] { "Q", "E", "R", "Space" },
        1.5f, 0.5f, "Adventurers/Characters/Mage.fbx",
        ClassDefinition.PartyRole.Healer, 1.5f);

    /// <summary>Rogue (party-roles S1): ranged/melee DPS. S1 casts the shared base set; S3 adds Rogue specialty skills.</summary>
    public static readonly ClassDefinition Rogue = new ClassDefinition(
        RogueId, "Rogue",
        "Strikes from range and from the shadows. Fast bow shots and quick blades.",
        new[] { DiabloSkills.Skill.Fireball, DiabloSkills.Skill.Arrow },
        new[] { "Q", "E" },
        1.5f, 1.5f, "Adventurers/Characters/Rogue.fbx",
        ClassDefinition.PartyRole.Dps, 1.5f);

    public static readonly ClassDefinition[] All = { Fenwalker, Lanternbearer, Rogue };

    /// <summary>Class by id; unknown or empty ids fall back to Fenwalker (warning for unknown).</summary>
    public static ClassDefinition Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return Fenwalker;
        foreach (var c in All)
            if (c.id == id) return c;
        Debug.LogWarning($"[Class] Unknown class id '{id}', falling back to {Fenwalker.displayName}.");
        return Fenwalker;
    }
}
