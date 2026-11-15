using UnityEngine;

/// <summary>
/// Rarity weights per enemy tier (P2 tuning). Chest guarantees Rare+.
/// </summary>
public static class DroptableDatabase
{
    public static void GetTable(LootTier tier, out int dropChance, out int magic, out int rare, out int unique)
    {
        dropChance = LootTable.DropChanceFor(tier);
        LootTable.WeightsFor(tier, out magic, out rare, out unique);
    }
}
