using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lantern Hold vendor assortment (S3 town). Six seeded offers per zone
/// visit, rarity prices (Magic 25, Rare 90, Unique 300), 25 % resale.
/// Pure data + rules, no GameObjects, so EditMode tests pin the economy
/// without a scene. Potions are always in stock beside the 6 offers.
/// </summary>
public sealed class VendorStock
{
    /// <summary>One trade offer: the item plus its fixed buy price.</summary>
    public struct Offer
    {
        public ItemInstance item;
        public int price;
    }

    public const int OfferCount = 6;

    public readonly List<Offer> offers = new List<Offer>();

    public int seed;

    /// <summary>Buy price by rarity (S3 table: 25 / 90 / 300).</summary>
    public static int BuyPrice(ItemInstance item)
    {
        if (item == null) return 0;
        if (item.slot == ItemSlot.Consumable) return ItemDatabase.PotionPrice;
        switch (item.rarity)
        {
            case ItemRarity.Rare: return 90;
            case ItemRarity.Unique: return 300;
            default: return 25;
        }
    }

    /// <summary>Resale is 25 % of the buy price (min 1, never free).</summary>
    public static int SellPrice(ItemInstance item) => Mathf.Max(1, BuyPrice(item) / 4);

    /// <summary>
    /// Roll the 6-offer assortment for a zone visit: 3 Magic, 2 Rare,
    /// 1 Unique (slots/bases/affixes from the seed). Same seed, same stock.
    /// </summary>
    public static VendorStock Roll(int seed)
    {
        var stock = new VendorStock { seed = seed };
        var rng = new System.Random(seed);
        var rarities = new[]
        {
            ItemRarity.Magic, ItemRarity.Magic, ItemRarity.Magic,
            ItemRarity.Rare, ItemRarity.Rare, ItemRarity.Unique,
        };
        var slots = new[] { ItemSlot.Weapon, ItemSlot.Armor, ItemSlot.Helmet };
        foreach (var rarity in rarities)
        {
            ItemInstance item;
            if (rarity == ItemRarity.Unique)
            {
                var uniques = ItemDatabase.GetUniques();
                item = uniques[rng.Next(uniques.Count)];
            }
            else
            {
                var slot = slots[rng.Next(slots.Length)];
                var bases = ItemDatabase.GetBaseTypes(slot);
                item = new ItemInstance
                {
                    slot = slot,
                    rarity = rarity,
                    baseType = bases[rng.Next(bases.Count)],
                    affixes = AffixLibrary.RollAffixes(slot, rarity, rng),
                };
            }
            stock.offers.Add(new Offer { item = item, price = BuyPrice(item) });
        }
        return stock;
    }

    /// <summary>
    /// Buy offers[index] into the hero's bag. False (nothing spent) when the
    /// purse is short, the offer is gone, or the bag is full.
    /// </summary>
    public bool TryBuy(int index, Wallet wallet, Inventory inv)
    {
        if (wallet == null || inv == null) return false;
        if (index < 0 || index >= offers.Count) return false;
        var offer = offers[index];
        if (offer.item == null || inv.FreeSlots <= 0) return false;
        if (!wallet.TrySpend(offer.price)) return false;
        inv.Add(offer.item);
        offers.RemoveAt(index);
        Debug.Log($"[Vendor] Bought {LootDrop.ItemLabel(offer.item)} for {offer.price} gold.");
        return true;
    }

    /// <summary>Buy one healing potion into the bag (always in stock).</summary>
    public static bool BuyPotion(Wallet wallet, Inventory inv)
    {
        if (wallet == null || inv == null || inv.FreeSlots <= 0) return false;
        if (!wallet.TrySpend(ItemDatabase.PotionPrice)) return false;
        inv.Add(ItemDatabase.CreateHealingPotion());
        Debug.Log($"[Vendor] Bought healing potion for {ItemDatabase.PotionPrice} gold.");
        return true;
    }

    /// <summary>
    /// Sell bag[bagIndex] back for 25 % of its buy price. False when the slot
    /// is empty (nothing credited).
    /// </summary>
    public static bool TrySell(int bagIndex, Wallet wallet, Inventory inv)
    {
        if (wallet == null || inv == null) return false;
        var item = inv.Get(bagIndex);
        if (item == null) return false;
        int price = SellPrice(item);
        if (!inv.RemoveAt(bagIndex)) return false;
        wallet.Add(price);
        Debug.Log($"[Vendor] Sold {LootDrop.ItemLabel(item)} for {price} gold.");
        return true;
    }
}
