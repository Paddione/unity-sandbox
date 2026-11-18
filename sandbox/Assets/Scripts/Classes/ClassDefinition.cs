/// <summary>
/// One playable class as data (cinderreach-second-class S1). Skills, key
/// labels, attribute scaling and the KayKit model live here so the hero,
/// the character sheet and the class select read one source.
/// </summary>
public sealed class ClassDefinition
{
    public readonly string id;
    public readonly string displayName;
    public readonly string blurb;
    public readonly DiabloSkills.Skill[] skills;
    /// <summary>Key label per skill (same order as <see cref="skills"/>).</summary>
    public readonly string[] keys;
    /// <summary>Multiplier on the Focus skill-damage bonus (Fenwalker 1).</summary>
    public readonly float focusSkillFactor;
    /// <summary>Multiplier on the Strength melee bonus (Fenwalker 1).</summary>
    public readonly float strengthMeleeFactor;
    /// <summary>Model under Assets/ThirdParty/KayKit/.</summary>
    public readonly string modelPath;

    public ClassDefinition(string id, string displayName, string blurb, DiabloSkills.Skill[] skills,
        string[] keys, float focusSkillFactor, float strengthMeleeFactor, string modelPath)
    {
        this.id = id;
        this.displayName = displayName;
        this.blurb = blurb;
        this.skills = skills;
        this.keys = keys;
        this.focusSkillFactor = focusSkillFactor;
        this.strengthMeleeFactor = strengthMeleeFactor;
        this.modelPath = modelPath;
    }

    public bool Has(DiabloSkills.Skill skill) => System.Array.IndexOf(skills, skill) >= 0;
}
