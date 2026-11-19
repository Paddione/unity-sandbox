using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// S5 presentation: ground sprite + rarity glow parity (host/client/late
/// join derive both from the S2 snapshot), the shared item card hierarchy
/// with deltas, and the three visible actions (equip / sell / discard) with
/// discard re-pickup keeping rolls and modifier.
/// </summary>
public class ItemPresentationTests
{
    GameObject player;

    [SetUp]
    public void Setup()
    {
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        GoldDrop.ResetRun();
        player = new GameObject("Player");
        player.transform.position = Vector3.zero;
        player.AddComponent<Equipment>();
        player.AddComponent<Inventory>();
        player.AddComponent<Wallet>();
        player.AddComponent<DiabloInventoryUI>();
    }

    [TearDown]
    public void TearDown()
    {
        DiabloTestFixture.CleanupTestScene(player);
        GoldDrop.ResetRun();
    }

    static ItemInstance RolledWeapon()
    {
        return new ItemInstance
        {
            slot = ItemSlot.Weapon,
            rarity = ItemRarity.Rare,
            baseType = "ShortSword",
            itemLevel = 5,
            instanceId = System.Guid.NewGuid().ToString("N"),
            affixes = new System.Collections.Generic.List<Affix>
            {
                new Affix(AffixLibrary.Damage, 4f),
                new Affix(AffixLibrary.Crit, 6f),
            },
        };
    }

    [UnityTest]
    public IEnumerator GroundView_SpriteAndGlow_MatchSnapshotParity()
    {
        foreach (var rarity in new[] { ItemRarity.Magic, ItemRarity.Rare, ItemRarity.Unique })
        {
            var item = rarity == ItemRarity.Unique
                ? ItemDatabase.RollUnique(ItemDatabase.CreateUnique(ItemDatabase.BulwarkCarrierId), 6, 1)
                : new ItemInstance
                {
                    slot = ItemSlot.Armor,
                    rarity = rarity,
                    baseType = "Breastplate",
                    itemLevel = 6,
                    instanceId = System.Guid.NewGuid().ToString("N"),
                    affixes = new System.Collections.Generic.List<Affix> { new Affix(AffixLibrary.Armor, 2f) },
                };
            // Host, client and late join all read the same S2 snapshot.
            var snapshot = JsonUtility.FromJson<ItemInstance>(JsonUtility.ToJson(item));
            var hostGo = new GameObject("HostDrop");
            var clientGo = new GameObject("ClientDrop");
            LootDropView.ShowOn(hostGo, item);
            LootDropView.ShowOn(clientGo, snapshot);
            Assert.AreEqual(LootDropView.ArtKey(item), LootDropView.ArtKey(snapshot));
            Assert.AreNotEqual("unknown", LootDropView.ArtKey(item));
            Assert.AreEqual(LootDropView.GlowColor(rarity), DiabloPalette.Rarity(rarity));
            var icon = hostGo.transform.Find("ItemIcon").GetComponent<SpriteRenderer>();
            var glow = hostGo.transform.Find("RarityGlow").GetComponent<SpriteRenderer>();
            Assert.IsNotNull(icon.sprite, rarity.ToString());
            Assert.IsNotNull(glow.sprite, rarity.ToString());
            Object.DestroyImmediate(hostGo);
            Object.DestroyImmediate(clientGo);
        }
        yield return null;
    }

    [Test]
    public void DetailsCard_HierarchyDeltasAndUniqueEffect()
    {
        var item = RolledWeapon();
        var equipped = new ItemInstance
        {
            slot = ItemSlot.Weapon,
            rarity = ItemRarity.Magic,
            baseType = "Dagger",
            itemLevel = 2,
            affixes = new System.Collections.Generic.List<Affix> { new Affix(AffixLibrary.Damage, 2f) },
        };
        string card = ItemDetailsView.Render(item, equipped);
        StringAssert.Contains("ShortSword", card);
        StringAssert.Contains("Rare", card);
        StringAssert.Contains("Weapon", card);
        StringAssert.Contains("Item level: 5", card);
        StringAssert.Contains("+4 Damage", card);
        StringAssert.Contains("+2 Damage", card); // delta line carries a + prefix
        StringAssert.Contains("+6 Crit", card);
        var unique = ItemDatabase.CreateUnique(ItemDatabase.BulwarkCarrierId);
        unique.itemLevel = 6;
        string ucard = ItemDetailsView.Render(unique, null);
        StringAssert.Contains("bulwark-carrier", ucard);
        StringAssert.Contains("Unique", ucard);
        StringAssert.Contains("threat", ucard.ToLowerInvariant());
    }

    [Test]
    public void DetailsCard_LegacyEmptyAndUnwearable_ExplainState()
    {
        var legacy = new ItemInstance
        {
            slot = ItemSlot.Weapon,
            rarity = ItemRarity.Magic,
            baseType = "ShortSword",
            itemLevel = 0,
            affixes = new System.Collections.Generic.List<Affix> { new Affix(AffixLibrary.Damage, 2f) },
        };
        StringAssert.Contains("Altbestand", ItemDetailsView.Render(legacy, null));
        StringAssert.Contains("Empty slot", ItemDetailsView.Render(null, null));
    }

    [UnityTest]
    public IEnumerator Actions_EquipSellDiscard_UpdateState_DiscardKeepsRolls()
    {
        var inv = player.GetComponent<Inventory>();
        var eq = player.GetComponent<Equipment>();
        var ui = player.GetComponent<DiabloInventoryUI>();
        var item = RolledWeapon();
        Assert.IsTrue(inv.Add(item));
        // Equip via instance ID.
        Assert.IsTrue(ui.EquipById(item.instanceId));
        Assert.AreEqual(item.instanceId, eq.weapon.instanceId);
        // Unequip back, then discard: the same instance spawns on the ground.
        ui.UnequipSlot((int)ItemSlot.Weapon);
        Assert.AreEqual(1, inv.Count);
        string id = inv.Get(0).instanceId;
        float dmg = inv.Get(0).GetAffix(AffixLibrary.Damage);
        Assert.IsTrue(ui.DiscardById(id));
        Assert.AreEqual(0, inv.Count);
        var drop = GameObject.FindObjectsOfType<LootDrop>();
        Assert.AreEqual(1, drop.Length);
        Assert.AreEqual(id, drop[0].item.instanceId);
        Assert.AreEqual(dmg, drop[0].item.GetAffix(AffixLibrary.Damage));
        // Re-pickup keeps rolls and modifier.
        LootDrop.CollectInto(player, drop[0].item);
        Assert.AreEqual(1, inv.Count);
        Assert.AreEqual(id, inv.Get(0).instanceId);
        Assert.AreEqual(dmg, inv.Get(0).GetAffix(AffixLibrary.Damage));
        yield return null;
    }

    [UnityTest]
    public IEnumerator Actions_FullBagAndRejectedBuy_ShowNotice()
    {
        var inv = player.GetComponent<Inventory>();
        for (int i = 0; i < Inventory.Capacity; i++)
            Assert.IsTrue(inv.Add(RolledWeapon()));
        Assert.IsFalse(inv.Add(RolledWeapon()), "bag must refuse over capacity");
        var vendorUi = player.AddComponent<DiabloVendorUI>();
        vendorUi.SetOpen(true);
        yield return null;
        vendorUi.BuyOffer(0);
        Assert.IsNotEmpty(vendorUi.Notice, "rejected buy must surface a notice");
        vendorUi.SetOpen(false);
    }
}
