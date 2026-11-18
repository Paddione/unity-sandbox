using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable session snapshot (S2 persist). Solo-local JSON via
/// <see cref="SaveService"/>: level/XP, S4-forward-compat point fields
/// (unused until the CharacterSheet stage), equipment, bag, gold and the
/// furthest zone reached (consumed by the S5 gate).
/// </summary>
[System.Serializable]
public sealed class SaveGame
{
    public const int CurrentVersion = 7;

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

    // v2 (replayability S1): run profile, see RunProfile / SaveMigrations.
    public bool difficultyUnlocked;
    public string difficulty = RunProfile.Normal;
    public int runSeed;
    public List<ItemInstance> stash = new List<ItemInstance>();

    // v3 (second-class S1): playable class, see ClassCatalog.
    public string classId = ClassCatalog.FenwalkerId;

    // v4 (act-one S1): act progression, see ActProgress.
    public int highestUnlockedZone = ActProgress.Ossuary;
    public bool actOneComplete;
    public List<string> waypoints = new List<string>();

    // v5 (zone-contracts S1): frozen run contract, see ContractProgress.
    public ContractSnapshot contract = new ContractSnapshot();

    // v6 (smithing S1): reforge material.
    public int smithingMaterial;

    // v7 (skill-variants S1): variant picks by class+skill.
    public List<SkillVariantEntry> skillVariants = new List<SkillVariantEntry>();
}

/// <summary>Frozen board contract (zone-contracts S1, save v5): the accepted
/// offer plus its Active/Complete/Claimed status. Empty when the run holds no
/// contract; the active target position survives reloads verbatim.</summary>
[System.Serializable]
public sealed class ContractSnapshot
{
    public bool hasContract;
    public string offerId = string.Empty;
    public int type;
    public int zone = ActProgress.Town;
    public Vector3 position;
    public int gold;
    public string targetName = string.Empty;
    public int status;
}

/// <summary>One skill variant pick (class + skill key, stable variant id).</summary>
[System.Serializable]
public sealed class SkillVariantEntry
{
    public string classId;
    public string skill;
    public string variant;
}
}

/// <summary>One skill's rank (string key: JsonUtility has no dictionaries).</summary>
[System.Serializable]
public sealed class SkillRankEntry
{
    public string skill;
    public int rank;
}
