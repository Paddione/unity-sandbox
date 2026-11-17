using System.Collections.Generic;

/// <summary>
/// Serializable session snapshot (S2 persist). Solo-local JSON via
/// <see cref="SaveService"/>: level/XP, S4-forward-compat point fields
/// (unused until the CharacterSheet stage), equipment, bag, gold and the
/// furthest zone reached (consumed by the S5 gate).
/// </summary>
[System.Serializable]
public sealed class SaveGame
{
    public const int CurrentVersion = 1;

    public int v = CurrentVersion;
    public int level = 1;
    public int xp;

    // S4 points: unspent pools + allocations (filled by CharacterSheet stage).
    public int freeAttributePoints;
    public int freeSkillPoints;
    public int strength;
    public int vitality;
    public int focus;
    public List<SkillRankEntry> skillRanks = new List<SkillRankEntry>();

    public ItemInstance equipmentArmor;
    public ItemInstance equipmentWeapon;
    public ItemInstance equipmentHelmet;
    public List<ItemInstance> bag = new List<ItemInstance>();
    public int gold;
    public int zoneReached = 1;
}

/// <summary>One skill's rank (string key: JsonUtility has no dictionaries).</summary>
[System.Serializable]
public sealed class SkillRankEntry
{
    public string skill;
    public int rank;
}
