using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>act-two-opening S4: bracket 4, Barrens uniques, their drop rule, host validation and save roundtrip.</summary>
public class BarrensLootTests
{
    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        GlasswrightController.ResetRun();
        SessionComplete.ResetRun();
    }

    [TearDown]
    public void Teardown()
    {
        foreach (var s in Object.FindObjectsByType<SessionComplete>(FindObjectsSortMode.None)) Object.DestroyImmediate(s.gameObject);
        foreach (var g in Object.FindObjectsByType<GlasswrightController>(FindObjectsSortMode.None)) Object.DestroyImmediate(g.gameObject);
        SessionComplete.ResetRun();
        if (SaveService.HasSave()) SaveService.DeleteSave();
        RunProfile.Reset();
    }

    [Test]
    public void Bracket4_WidensRangesBeyondTheKiln()
    {
        var kiln = AffixLibrary.GetRange(AffixLibrary.MaxHP, 12, 1);
        var barrens = AffixLibrary.GetRange(AffixLibrary.MaxHP, ItemLevelRules.BarrensLevel, 1);
        Assert.Greater(barrens.max, kiln.max);
        Assert.AreEqual(1.75f, ItemLevelRules.RangeMultiplier(ItemLevelRules.BarrensLevel), 0.0001f);
        Assert.AreEqual(1.5f, ItemLevelRules.RangeMultiplier(ItemLevelRules.KindledFinaleRewardLevel), 0.0001f, "Kindled reward keeps bracket 3");
    }

    [Test]
    public void ZoneUniques_AreKnownFreelyWearableAndTopRolled()
    {
        foreach (var id in ItemDatabase.ZoneUniqueIds)
        {
            Assert.IsTrue(ItemDatabase.IsKnownUniqueId(id), id);
            var item = ItemDatabase.RollUnique(ItemDatabase.CreateUnique(id), ItemLevelRules.BarrensLevel + 2, 1);
            Assert.AreEqual(id, item.uniqueId);
            Assert.AreEqual(ItemRarity.Unique, item.rarity);
            Assert.AreEqual("", ItemDatabase.ClassRestriction(item.baseType), $"{id} fits every class");
            foreach (var a in item.affixes)
                Assert.AreEqual(AffixLibrary.GetRange(a.key, ItemLevelRules.BarrensLevel + 2, 1).max, a.value, 0.0001f, $"{id} {a.key}");
            Assert.IsNotEmpty(item.instanceId);
        }
        Assert.AreEqual(ItemSlot.Armor, ItemDatabase.CreateUnique(ItemDatabase.ShardbackAegisId).slot);
        Assert.AreEqual(ItemSlot.Helmet, ItemDatabase.CreateUnique(ItemDatabase.GlasswrightPrismId).slot);
        Assert.AreEqual("Scherbenpanzer", LootDrop.PrettyName(ItemDatabase.ShardbackAegisId));
        Assert.AreEqual("Prisma des Glasmachers", LootDrop.PrettyName(ItemDatabase.GlasswrightPrismId));
    }

    [Test]
    public void ZoneUniques_DropOnlyFromBracket4Sources()
    {
        var rng = new System.Random(5);
        int zoneArmor = 0, zoneHelm = 0;
        for (int i = 0; i < 400; i++)
        {
            string kilnPick = ItemDatabase.UniqueForSlot(i % 2 == 0 ? ItemSlot.Armor : ItemSlot.Helmet, 12, rng);
            CollectionAssert.DoesNotContain(ItemDatabase.ZoneUniqueIds, kilnPick, "never outside the Barrens");
            var slot = i % 2 == 0 ? ItemSlot.Armor : ItemSlot.Helmet;
            string pick = ItemDatabase.UniqueForSlot(slot, ItemLevelRules.BarrensLevel, rng);
            Assert.AreEqual(slot, ItemDatabase.CreateUnique(pick).slot, "slot stays the rolled slot");
            if (pick == ItemDatabase.ShardbackAegisId) zoneArmor++;
            if (pick == ItemDatabase.GlasswrightPrismId) zoneHelm++;
        }
        Assert.That(zoneArmor, Is.InRange(60, 140), "about half of 200 armor uniques");
        Assert.That(zoneHelm, Is.InRange(60, 140));
        var roll = new LootRoll { drops = true, slot = ItemSlot.Armor, rarity = ItemRarity.Unique, affixTier = 1 };
        bool seen = false;
        for (int i = 0; i < 40 && !seen; i++)
            seen = LootDrop.BuildItem(roll, rng, ItemLevelRules.BarrensLevel).uniqueId == ItemDatabase.ShardbackAegisId;
        Assert.IsTrue(seen, "BuildItem routes bracket-4 uniques through the zone pool");
    }

    [Test]
    public void FirstGlasswrightKill_GuaranteesAZoneUnique()
    {
        RunProfile.Act.Unlock(ActProgress.Kiln);
        RunProfile.Act.CompleteAct();
        var go = new GameObject(GlasswrightController.BossName);
        go.transform.position = BarrensLayout.GlasswrightPos;
        go.AddComponent<EnemyHealth>().SetTier(GlasswrightController.MaxHP, LootTier.Boss);
        var boss = go.AddComponent<GlasswrightController>();
        boss.OnEnemyDeath();
        var drop = GlasswrightController.LastZoneUnique;
        Assert.IsNotNull(drop);
        CollectionAssert.Contains(ItemDatabase.ZoneUniqueIds, drop.uniqueId);
        Assert.AreEqual(ItemLevelRules.BarrensLevel + 2, drop.itemLevel);
        StringAssert.Contains(LootDrop.PrettyName(drop.uniqueId), SessionComplete.BestDrop);
    }

    [Test]
    public void ZoneUnique_SurvivesSaveRoundtrip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"cinderreach-barrens-{System.Guid.NewGuid():N}.json");
        var hero = new GameObject("Player");
        var eq = hero.AddComponent<Equipment>();
        hero.AddComponent<Inventory>();
        hero.AddComponent<Wallet>();
        hero.AddComponent<DiabloProgression>();
        eq.helmet = ItemDatabase.RollUnique(ItemDatabase.CreateUnique(ItemDatabase.GlasswrightPrismId), 17, 1);
        SaveService.SaveNow(hero, path: path);
        eq.helmet = null;
        Assert.IsTrue(SaveService.TryLoad(hero, path));
        Assert.IsNotNull(hero.GetComponent<Equipment>().helmet);
        Assert.AreEqual(ItemDatabase.GlasswrightPrismId, hero.GetComponent<Equipment>().helmet.uniqueId);
        Object.DestroyImmediate(hero);
        foreach (var f in new[] { path, path + ".tmp" }) if (File.Exists(f)) File.Delete(f);
    }
}
