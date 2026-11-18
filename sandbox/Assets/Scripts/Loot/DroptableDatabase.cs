using UnityEngine;

/// <summary>
/// Rarity weights per enemy tier (P2 tuning). Chest guarantees Rare+.
/// S2 economy: independent gold roll per tier (chance + inclusive bounds).
/// </summary>
public static class DroptableDatabase
{
    // Gold chances (percent): Normal 70 %, Elite/Chest always.
    public const int NormalGoldChance = 70;
    public const int EliteGoldChance = 100;
    public const int ChestGoldChance = 100;

    // Gold bounds (inclusive): Normal 3–8, Elite 15–30, Chest 40–60.
    public const int NormalGoldMin = 3;
    public const int NormalGoldMax = 8;
    public const int EliteGoldMin = 15;
    public const int EliteGoldMax = 30;
    public const int ChestGoldMin = 40;
    public const int ChestGoldMax = 60;

    // Boss purse (guaranteed, S5): 150–250 gold.
    public const int BossGoldChance = 100;
    public const int BossGoldMin = 150;
    public const int BossGoldMax = 250;

    public static void GetTable(LootTier tier, out int dropChance, out int magic, out int rare, out int unique)
    {
        dropChance = LootTable.DropChanceFor(tier);
        LootTable.WeightsFor(tier, out magic, out rare, out unique);
    }

    /// <summary>Gold roll for the tier: drop chance plus inclusive amount bounds.</summary>
    public static void GetGoldTable(LootTier tier, out int chance, out int min, out int max)
    {
        switch (tier)
        {
            case LootTier.Elite:
                chance = EliteGoldChance; min = EliteGoldMin; max = EliteGoldMax; return;
            case LootTier.Chest:
                chance = ChestGoldChance; min = ChestGoldMin; max = ChestGoldMax; return;
            case LootTier.Boss:
                chance = BossGoldChance; min = BossGoldMin; max = BossGoldMax; return;
            default:
                chance = NormalGoldChance; min = NormalGoldMin; max = NormalGoldMax; return;
        }
    }
}
