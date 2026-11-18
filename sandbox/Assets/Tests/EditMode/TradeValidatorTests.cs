using NUnit.Framework;

/// <summary>
/// S4 economy gate (cinderreach-coop-session): the host and the solo path
/// share these rules, so every rejection below must stay rejected in game.
/// </summary>
public class TradeValidatorTests
{
    [Test]
    public void Buy_AcceptsFundedBaggedInReach()
    {
        Assert.IsNull(TradeValidator.CanBuy(gold: 100, price: 25, freeSlots: 3, distToVendor: 2f));
    }

    [Test]
    public void Buy_RejectsShortGoldFullBagAndFarStall()
    {
        StringAssert.Contains("gold", TradeValidator.CanBuy(10, 25, 3, 2f));
        StringAssert.Contains("bag", TradeValidator.CanBuy(100, 25, 0, 2f));
        StringAssert.Contains("range", TradeValidator.CanBuy(100, 25, 3, 9f));
        StringAssert.Contains("range", TradeValidator.CanBuyRange(4.5f));
        Assert.IsNull(TradeValidator.CanBuyRange(4f));
    }

    [Test]
    public void Pickup_AcceptsNearFreshPile()
    {
        Assert.IsNull(TradeValidator.CanPickup(distToPile: 1f, alreadyCollected: false));
    }

    [Test]
    public void Pickup_RejectsFarPileAndDoublePickup()
    {
        StringAssert.Contains("range", TradeValidator.CanPickup(9f, false));
        StringAssert.Contains("taken", TradeValidator.CanPickup(1f, true));
    }

    [Test]
    public void Sell_AcceptsNearSanePrice()
    {
        Assert.IsNull(TradeValidator.CanSell(distToVendor: 2f, price: 22));
    }

    [Test]
    public void Sell_RejectsFarStallAndAbsurdPrice()
    {
        StringAssert.Contains("range", TradeValidator.CanSell(9f, 22));
        StringAssert.Contains("price", TradeValidator.CanSell(2f, 9999));
        StringAssert.Contains("price", TradeValidator.CanSell(2f, 0));
    }

    [Test]
    public void PerPlayerStock_HostKeepsZoneSeed_ClientDerives()
    {
        Assert.AreEqual(7, Vendor.StockSeed(7, 0), "host (id 0) keeps the zone stock");
        int clientSeed = Vendor.StockSeed(7, 1);
        Assert.AreEqual(Vendor.StockSeed(7, 1), clientSeed, "same client, same stock");
        var hostStock = VendorStock.Roll(7);
        var clientStock = VendorStock.Roll(clientSeed);
        Assert.AreEqual(hostStock.offers.Count, clientStock.offers.Count);
        for (int i = 0; i < hostStock.offers.Count; i++)
            Assert.AreEqual(LootDrop.ItemLabel(hostStock.offers[i].item),
                LootDrop.ItemLabel(VendorStock.Roll(7).offers[i].item),
                "host stock is the plain zone roll");
        Assert.AreNotEqual(LootDrop.ItemLabel(hostStock.offers[0].item),
            LootDrop.ItemLabel(clientStock.offers[0].item),
            "client 1 sees a different assortment");
    }
}
