/// <summary>
/// One playable class as data (cinderreach-second-class S1). Skills, key
/// labels, attribute scaling and the KayKit model live here so the hero,
/// the character sheet and the class select read one source.
/// </summary>
public sealed class ClassDefinition
{
    /// <summary>Party role (cinderreach-party-roles S1): Tank holds aggro, Healer restores, Dps kills.</summary>
    public enum PartyRole { Tank, Healer, Dps }

    public readonly string id;
    /// <summary>Shown name from the catalog (<c>name.class.&lt;id&gt;</c>, cinderreach-deutsch-story).</summary>
    public string displayName => Loc.T("name.class." + id);
    /// <summary>Class-select lore line from the catalog (<c>class.blurb.&lt;id&gt;</c>).</summary>
    public string blurb => Loc.T("class.blurb." + id);
    public readonly DiabloSkills.Skill[] skills;
    /// <summary>Key label per skill (same order as <see cref="skills"/>).</summary>
    public readonly string[] keys;
    /// <summary>Multiplier on the Focus skill-damage bonus (Fenwalker 1).</summary>
    public readonly float focusSkillFactor;
    /// <summary>Multiplier on the Strength melee bonus (Fenwalker 1).</summary>
    public readonly float strengthMeleeFactor;
    /// <summary>Model under Assets/ThirdParty/KayKit/.</summary>
    public readonly string modelPath;
    /// <summary>Party role of the class (Fenwalker Tank, Lanternbearer Healer, Rogue Dps).</summary>
    public readonly PartyRole role;
    /// <summary>Damage/heal factor on specialty (non-base) skills: 1.5 for Rogue/Healer, 1 for the Tank (control, not damage).</summary>
    public readonly float specialtyFactor;
    /// <summary>Strength points auto-assigned per level-up (cinderreach-levelup-empower).</summary>
    public readonly int attrStrengthPerLevel;
    /// <summary>Vitality points auto-assigned per level-up (cinderreach-levelup-empower).</summary>
    public readonly int attrVitalityPerLevel;
    /// <summary>Focus points auto-assigned per level-up (cinderreach-levelup-empower).</summary>
    public readonly int attrFocusPerLevel;

    /// <summary>Base set every class can cast (cinderreach-party-roles S1): Fireball + Arrow. Melee runs through DiabloMelee for all classes.</summary>
    public static readonly DiabloSkills.Skill[] BaseSkills = { DiabloSkills.Skill.Fireball, DiabloSkills.Skill.Arrow };

    public ClassDefinition(string id, DiabloSkills.Skill[] skills,
        string[] keys, float focusSkillFactor, float strengthMeleeFactor, string modelPath,
        PartyRole role, float specialtyFactor,
        int attrStrengthPerLevel, int attrVitalityPerLevel, int attrFocusPerLevel)
    {
        this.id = id;
        this.skills = skills;
        this.keys = keys;
        this.focusSkillFactor = focusSkillFactor;
        this.strengthMeleeFactor = strengthMeleeFactor;
        this.modelPath = modelPath;
        this.role = role;
        this.specialtyFactor = specialtyFactor;
        this.attrStrengthPerLevel = attrStrengthPerLevel;
        this.attrVitalityPerLevel = attrVitalityPerLevel;
        this.attrFocusPerLevel = attrFocusPerLevel;
    }

    /// <summary>
    /// Deterministic spend order of the auto-assigned attribute points:
    /// Strength first, then Vitality, then Focus (cinderreach-levelup-empower).
    /// Used by <see cref="CharacterSheet.AwardLevel(ClassDefinition)"/> and for
    /// distributing leftover free points of old saves.
    /// </summary>
    public CharacterSheet.Attribute[] LevelUpSequence()
    {
        var seq = new CharacterSheet.Attribute[attrStrengthPerLevel + attrVitalityPerLevel + attrFocusPerLevel];
        int i = 0;
        for (int s = 0; s < attrStrengthPerLevel; s++) seq[i++] = CharacterSheet.Attribute.Strength;
        for (int v = 0; v < attrVitalityPerLevel; v++) seq[i++] = CharacterSheet.Attribute.Vitality;
        for (int f = 0; f < attrFocusPerLevel; f++) seq[i++] = CharacterSheet.Attribute.Focus;
        return seq;
    }

    /// <summary>Shown key of skill slot <paramref name="i"/> ("Space" → "Leertaste").</summary>
    public string KeyLabel(int i) => keys[i] == "Space" ? Loc.T("key.space") : keys[i];

    public bool Has(DiabloSkills.Skill skill) => System.Array.IndexOf(skills, skill) >= 0;

    /// <summary>True for the shared base set (castable by every class).</summary>
    public static bool IsBaseSkill(DiabloSkills.Skill skill) => System.Array.IndexOf(BaseSkills, skill) >= 0;

    /// <summary>Cast permission: own skills plus the shared base set (party-roles S1 opens the old class gate).</summary>
    public bool CanCast(DiabloSkills.Skill skill) => Has(skill) || IsBaseSkill(skill);

    /// <summary>True for class-special (non-base) skills: they scale with <see cref="specialtyFactor"/>.</summary>
    public bool IsSpecialty(DiabloSkills.Skill skill) => Has(skill) && !IsBaseSkill(skill);
}
