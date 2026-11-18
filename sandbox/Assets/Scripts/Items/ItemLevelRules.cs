using UnityEngine;

/// <summary>Item progression follows the source's authored encounter zone.</summary>
public static class ItemLevelRules
{
    public const int VendorLevel = 1;
    public const int RiskShrineRewardLevel = 5;
    public const int KindledFinaleRewardLevel = 12;

    public static int ForDrop(Vector3 sourcePosition, LootTier tier)
    {
        int zone = EncounterLayout.CurrentZone(sourcePosition);
        int level = zone == ActProgress.Kiln ? 10 : zone == ActProgress.Ossuary ? 5 : 1;
        switch (tier)
        {
            case LootTier.Elite:
            case LootTier.Chest: return level + 1;
            case LootTier.Boss:
            case LootTier.ActBoss: return level + 2;
            default: return level;
        }
    }

    /// <summary>Zero is the historical bracket and keeps its original tier range.</summary>
    public static int GetBracket(int itemLevel) =>
        itemLevel < 1 ? 0 : itemLevel < 5 ? 1 : itemLevel < 10 ? 2 : 3;

    public static float RangeMultiplier(int itemLevel)
    {
        switch (GetBracket(itemLevel))
        {
            case 2: return 1.25f;
            case 3: return 1.5f;
            default: return 1f;
        }
    }
}
