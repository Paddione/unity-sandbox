using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>cinderreach-vendor-sell-all: one call sells every Rare in the bag; Uniques, consumables and worn gear stay.</summary>
public sealed class VendorSellAllTests
{
    GameObject hero;
    Inventory inv;
    Wallet wallet;

    [SetUp]
    public void SetUp()
    {
        hero = new GameObject("SellAllHero");
        inv = hero.AddComponent<Inventory>();
        wallet = hero.AddComponent<Wallet>();
        hero.AddComponent<Equipment>();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(hero);

    static ItemInstance Rare(ItemSlot slot) => new ItemInstance
    {
        slot = slot, rarity = ItemRarity.Rare, baseType = ItemDatabase.GetBaseTypes(slot)[0],
        affixes = new List<Affix> { new Affix("Damage", 3f) }, itemLevel = 5,
    };

    [Test]
    public void SellAllRares_SellsOnlyRares_GoldIsSumOfSinglePrices()
    {
        var r1 = Rare(ItemSlot.Weapon);
        var r2 = Rare(ItemSlot.Helmet);
        var magic = Rare(ItemSlot.Boots); magic.rarity = ItemRarity.Magic;
        var unique = ItemDatabase.CreateUnique(ItemDatabase.ShardbackAegisId);
        var worn = Rare(ItemSlot.Armor);
        hero.GetComponent<Equipment>().Equip(worn);
        foreach (var it in new[] { r1, unique, r2, magic }) Assert.IsTrue(inv.Add(it));
        int expected = VendorStock.SellPrice(r1) + VendorStock.SellPrice(r2) + VendorStock.SellPrice(magic);

        Assert.AreEqual(expected, VendorStock.PreviewSellAllRares(inv, out int previewCount));
        Assert.AreEqual(3, previewCount);
        Assert.AreEqual(4, inv.Count, "preview sells nothing");

        int gold = VendorStock.SellAllRares(wallet, inv, out int sold);
        Assert.AreEqual(3, sold);
        Assert.AreEqual(expected, gold);
        Assert.AreEqual(expected, wallet.Gold);
        Assert.AreEqual(1, inv.Count);
        Assert.AreSame(unique, inv.Get(0), "the Unique stays");
        Assert.AreSame(worn, hero.GetComponent<Equipment>().armor, "worn gear is never sold");
        Assert.AreEqual(0, VendorStock.SellAllRares(wallet, inv, out sold), "second run sells nothing");
        Assert.AreEqual(0, sold);
    }
}
