/// <summary>
/// Co-op economy gate (cinderreach-coop-session S4). Pure C#, no scene:
/// every gold/vendor intent is checked here before it applies, whether the
/// check runs on the host (ServerRpc) or locally (solo prediction +
/// <see cref="VendorStock.TryBuy"/> apply step). Null means approved, else a
/// short reason ("gold", "bag", "range", "taken", "price") for logs/tests.
/// Split of duties: the host owns range, existence and once-only (shared
/// world); funds and bag space are enforced at apply time by TryBuy/TrySell
/// on the owner's own purse and bag (per-player state, never synced).
/// </summary>
public static class TradeValidator
{
    /// <summary>Buy/sell reach around the stall (plan S4).</summary>
    public const float VendorRange = 4f;

    /// <summary>Gold/item pickup reach around the pile (plan S4).</summary>
    public const float PickupRange = 2.5f;

    /// <summary>Highest plausible resale (25 % of the Unique buy price).</summary>
    public const int MaxSellPrice = 75;

    /// <summary>Full buy gate: funds, bag space and stall reach.</summary>
    public static string CanBuy(int gold, int price, int freeSlots, float distToVendor)
    {
        if (price <= 0) return $"price {price} not for sale";
        if (gold < price) return $"gold {gold} < price {price}";
        if (freeSlots <= 0) return "bag full";
        return CanBuyRange(distToVendor);
    }

    /// <summary>Server-side buy part: stall reach only (funds/bag live per player).</summary>
    public static string CanBuyRange(float distToVendor) =>
        distToVendor <= VendorRange ? null : $"range {distToVendor:0.00}m > {VendorRange:0.00}m";

    /// <summary>Pickup gate: pile reach and once-only (double-pickup race).</summary>
    public static string CanPickup(float distToPile, bool alreadyCollected)
    {
        if (alreadyCollected) return "taken";
        if (distToPile > PickupRange) return $"range {distToPile:0.00}m > {PickupRange:0.00}m";
        return null;
    }

    /// <summary>Sell gate: stall reach and a sane resale price.</summary>
    public static string CanSell(float distToVendor, int price)
    {
        if (price <= 0 || price > MaxSellPrice) return $"price {price} outside 1..{MaxSellPrice}";
        if (distToVendor > VendorRange) return $"range {distToVendor:0.00}m > {VendorRange:0.00}m";
        return null;
    }
}
