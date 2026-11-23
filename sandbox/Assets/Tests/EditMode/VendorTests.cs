using NUnit.Framework;
using UnityEngine;

/// <summary>S3 vendor economy: rarity prices, buy/sell balance, bag-full block.</summary>
public class VendorTests
{
    GameObject hero;
    Wallet wallet;
    Inventory inv;

    [SetUp]
    public void Setup()
    {
        hero = new GameObject("Player");
        hero.AddComponent<Equipment>();
        hero.AddComponent<PlayerHealth>();
        inv = hero.AddComponent<Inventory>();
        wallet = hero.AddComponent<Wallet>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(hero);
    }

    static ItemInstance ItemOf(ItemSlot slot, ItemRarity rarity) =>
        new ItemInstance { slot = slot, rarity = rarity, baseType = "T" };

    [Test]
    public void Prices_FollowRarityTable()
    {
        Assert.AreEqual(25, VendorStock.BuyPrice(ItemOf(ItemSlot.Weapon, ItemRarity.Magic)));
        Assert.AreEqual(90, VendorStock.BuyPrice(ItemOf(ItemSlot.Armor, ItemRarity.Rare)));
        Assert.AreEqual(300, VendorStock.BuyPrice(ItemOf(ItemSlot.Helmet, ItemRarity.Unique)));
        Assert.AreEqual(ItemDatabase.PotionPrice,
            VendorStock.BuyPrice(ItemDatabase.CreateHealingPotion()));
    }

    [Test]
    public void SellPrice_IsQuarterOfBuyPrice()
    {
        Assert.AreEqual(6, VendorStock.SellPrice(ItemOf(ItemSlot.Weapon, ItemRarity.Magic))); // 25/4
        Assert.AreEqual(22, VendorStock.SellPrice(ItemOf(ItemSlot.Armor, ItemRarity.Rare))); // 90/4
        Assert.AreEqual(75, VendorStock.SellPrice(ItemOf(ItemSlot.Helmet, ItemRarity.Unique))); // 300/4
    }

    [Test]
    public void Stock_HasSixSeededOffers()
    {
        var a = VendorStock.Roll(7);
        var b = VendorStock.Roll(7);
        Assert.AreEqual(VendorStock.OfferCount, a.offers.Count);
        for (int i = 0; i < a.offers.Count; i++)
            Assert.AreEqual(LootDrop.ItemLabel(a.offers[i].item),
                LootDrop.ItemLabel(b.offers[i].item), "Same seed must roll the same stock.");
        Assert.AreNotEqual(LootDrop.ItemLabel(VendorStock.Roll(8).offers[0].item),
            LootDrop.ItemLabel(a.offers[0].item), "Different seeds should differ (first offer).");
    }

    [Test]
    public void Stock_CarriesOnlyRareAndUnique()
    {
        for (int seed = 0; seed < 20; seed++)
            foreach (var offer in VendorStock.Roll(seed).offers)
                Assert.AreNotEqual(ItemRarity.Magic, offer.item.rarity, $"Seed {seed}: no blue stock (playtest 2026-11-23).");
        Assert.AreNotEqual(ItemRarity.Magic, ItemDatabase.CreateHealingPotion().rarity);
        Assert.AreNotEqual(ItemRarity.Magic, ItemDatabase.CreateStarterBow().rarity);
        Assert.AreNotEqual(ItemRarity.Magic, ItemDatabase.CreateStarterLantern().rarity);
    }

    [Test]
    public void Buy_WithoutGold_FailsWithoutSpending()
    {
        var stock = VendorStock.Roll(7);
        int price = stock.offers[0].price;
        Assert.Greater(price, wallet.Gold);
        Assert.IsFalse(stock.TryBuy(0, wallet, inv));
        Assert.AreEqual(0, wallet.Gold);
        Assert.AreEqual(0, inv.Count);
        Assert.AreEqual(VendorStock.OfferCount, stock.offers.Count);
    }

    [Test]
    public void BuySell_Balance()
    {
        var stock = VendorStock.Roll(7);
        wallet.Add(200);
        int price = stock.offers[0].price;
        Assert.IsTrue(stock.TryBuy(0, wallet, inv));
        Assert.AreEqual(200 - price, wallet.Gold);
        Assert.AreEqual(1, inv.Count);
        int expectBack = VendorStock.SellPrice(inv.Get(0));
        Assert.IsTrue(VendorStock.TrySell(0, wallet, inv));
        Assert.AreEqual(200 - price + expectBack, wallet.Gold);
        Assert.AreEqual(0, inv.Count);
    }

    [Test]
    public void BagFull_BlocksBuyWithoutSpending()
    {
        var stock = VendorStock.Roll(7);
        wallet.Add(1000);
        for (int i = 0; i < Inventory.Capacity; i++)
            Assert.IsTrue(inv.Add(ItemDatabase.CreateStarterSword()));
        Assert.AreEqual(0, inv.FreeSlots);
        Assert.IsFalse(stock.TryBuy(0, wallet, inv));
        Assert.AreEqual(1000, wallet.Gold);
        Assert.AreEqual(Inventory.Capacity, inv.Count);
    }

    [Test]
    public void Sell_EmptySlot_FailsWithoutCredit()
    {
        Assert.IsFalse(VendorStock.TrySell(0, wallet, inv));
        Assert.AreEqual(0, wallet.Gold);
    }

    [Test]
    public void Potion_Drink_HealsAndConsumesOne()
    {
        var health = hero.GetComponent<PlayerHealth>();
        wallet.Add(100);
        Assert.IsTrue(VendorStock.BuyPotion(wallet, inv));
        Assert.IsTrue(VendorStock.BuyPotion(wallet, inv));
        Assert.AreEqual(100 - 2 * ItemDatabase.PotionPrice, wallet.Gold);
        health.TakeDamage(15f);
        float hurt = health.CurrentHP;
        Assert.IsTrue(Vendor.DrinkPotion(hero));
        Assert.AreEqual(1, inv.Count, "Drinking consumes exactly one potion.");
        Assert.AreEqual(hurt + ItemDatabase.PotionHeal, health.CurrentHP, 0.001f);
        Assert.AreEqual(1, inv.Count, "Exactly one of the two potions is gone.");
    }

    [Test]
    public void Drink_WithoutPotion_Fails()
    {
        inv.Add(ItemDatabase.CreateStarterSword());
        Assert.IsFalse(Vendor.DrinkPotion(hero));
        Assert.AreEqual(1, inv.Count);
    }

    [Test]
    public void CanInteract_RespectsRadius()
    {
        var vendorPos = new Vector3(-3.5f, 0f, 11f);
        Assert.IsTrue(Vendor.CanInteract(new Vector3(-3.5f, 0f, 12f), vendorPos));
        Assert.IsFalse(Vendor.CanInteract(new Vector3(3.5f, 0f, 11f), vendorPos),
            "Campfire (7 m east) is out of trade reach.");
    }
}
