using UnityEngine;

/// <summary>
/// The playable classes (cinderreach-second-class). Fenwalker is the fallback
/// for missing or unknown ids. Since class-kits-parity every class carries six
/// own skills on Q/E/R/Space/F/G; Fireball and Arrow stay castable through the
/// API (<see cref="ClassDefinition.BaseSkills"/>) but sit in no kit, and save
/// ranks on skills outside the kit are refunded on load
/// (<see cref="CharacterSheet.RefundForeignRanks"/>).
/// </summary>
public static class ClassCatalog
{
    public const string FenwalkerId = "fenwalker";
    public const string LanternbearerId = "lanternbearer";
    public const string RogueId = "rogue";

    public static readonly ClassDefinition Fenwalker = new ClassDefinition(
        FenwalkerId,
        new[] { DiabloSkills.Skill.Cleave, DiabloSkills.Skill.MireHook, DiabloSkills.Skill.SeismicSlam, DiabloSkills.Skill.BogCharge, DiabloSkills.Skill.MireGround, DiabloSkills.Skill.IronVow },
        new[] { "Q", "E", "R", "Space", "F", "G" },
        1f, 1f, "Adventurers/Characters/Knight.fbx",
        ClassDefinition.PartyRole.Tank, 1f, 2, 2, 1);

    public static readonly ClassDefinition Lanternbearer = new ClassDefinition(
        LanternbearerId,
        new[] { DiabloSkills.Skill.LanternBolt, DiabloSkills.Skill.Wisps, DiabloSkills.Skill.SearingHalo, DiabloSkills.Skill.VeilStep, DiabloSkills.Skill.MendingLight, DiabloSkills.Skill.RenewingCircle },
        new[] { "Q", "E", "R", "Space", "F", "G" },
        1.5f, 0.5f, "Adventurers/Characters/Mage.fbx",
        ClassDefinition.PartyRole.Healer, 1.5f, 1, 1, 3);

    /// <summary>Rogue (party-roles S1/S3, class-kits-parity): ranged/melee DPS built on venom, smoke and powder.</summary>
    public static readonly ClassDefinition Rogue = new ClassDefinition(
        RogueId,
        new[] { DiabloSkills.Skill.QuickShot, DiabloSkills.Skill.BladeFlurry, DiabloSkills.Skill.SmokePot, DiabloSkills.Skill.ShadowRoll, DiabloSkills.Skill.VenomVial, DiabloSkills.Skill.PowderKeg },
        new[] { "Q", "E", "R", "Space", "F", "G" },
        1.5f, 1.5f, "Adventurers/Characters/Rogue.fbx",
        ClassDefinition.PartyRole.Dps, 1.5f, 2, 1, 2);

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
