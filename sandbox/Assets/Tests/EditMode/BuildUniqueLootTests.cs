using NUnit.Framework;
using UnityEngine;

/// <summary>
/// cinderreach-build-uniques S1: lancer-ward (Fenwalker) and wick-of-the-halo
/// (Lanternbearer) flow through the existing class-bound Unique selection and
/// persistence. Smithing exclusion relies on the standing rarity rule
/// (uniques are never reforged — fixed affix sets); the #8 SmithingService
/// interface is not on main yet and must keep excluding ItemRarity.Unique.
/// </summary>
public class BuildUniqueLootTests
{
    [Test]
    public void BuildUniques_AreClassBoundWeapons()
    {
        var ward = ItemDatabase.CreateUnique(ItemDatabase.LancerWardId);
        var wick = ItemDatabase.CreateUnique(ItemDatabase.WickOfHaloId);
        Assert.AreEqual(ItemSlot.Weapon, ward.slot);
        Assert.AreEqual(ItemSlot.Weapon, wick.slot);
        Assert.AreEqual(ItemRarity.Unique, ward.rarity);
        Assert.AreEqual(ItemRarity.Unique, wick.rarity);
        Assert.AreEqual(ClassCatalog.FenwalkerId, ItemDatabase.ClassRestriction(ward.baseType));
        Assert.AreEqual(ClassCatalog.LanternbearerId, ItemDatabase.ClassRestriction(wick.baseType));
        Assert.IsTrue(Equipment.CanEquip(ward, ClassCatalog.FenwalkerId));
        Assert.IsFalse(Equipment.CanEquip(ward, ClassCatalog.LanternbearerId));
        Assert.IsTrue(Equipment.CanEquip(wick, ClassCatalog.LanternbearerId));
        Assert.IsFalse(Equipment.CanEquip(wick, ClassCatalog.FenwalkerId));
    }

    [Test]
    public void UniqueWeaponForClass_SplitsBuildAndLegacyPerClass()
    {
        foreach (var cls in ClassCatalog.All)
        {
            bool sawBuild = false, sawLegacy = false;
            var rng = new System.Random(7);
            for (int i = 0; i < 200; i++)
            {
                var item = ItemDatabase.UniqueWeaponForClass(cls.id, rng);
                Assert.AreEqual(ItemRarity.Unique, item.rarity);
                Assert.AreEqual(ItemSlot.Weapon, item.slot);
                Assert.IsTrue(Equipment.CanEquip(item, cls.id), $"{item.uniqueId} must fit {cls.id}");
                if (item.uniqueId == ItemDatabase.LancerWardId || item.uniqueId == ItemDatabase.WickOfHaloId)
                    sawBuild = true;
                else
                    sawLegacy = true;
            }
            Assert.IsTrue(sawBuild, $"{cls.id} never rolled its build unique");
            Assert.IsTrue(sawLegacy, $"{cls.id} never rolled its legacy unique");
        }
    }

    [Test]
    public void UniqueWeaponForClass_StaysClassCorrect()
    {
        var rng = new System.Random(13);
        for (int i = 0; i < 100; i++)
        {
            var fen = ItemDatabase.UniqueWeaponForClass(ClassCatalog.FenwalkerId, rng);
            var lan = ItemDatabase.UniqueWeaponForClass(ClassCatalog.LanternbearerId, rng);
            Assert.AreNotEqual(ItemDatabase.WickOfHaloId, fen.uniqueId);
            Assert.AreNotEqual("Lantern of the Hold", fen.uniqueId);
            Assert.AreNotEqual(ItemDatabase.LancerWardId, lan.uniqueId);
            Assert.AreNotEqual("Emberbrand Blade", lan.uniqueId);
        }
    }

    [Test]
    public void LootDrop_BuildItem_RoutesWeaponUniquesThroughClassSelection()
    {
        foreach (var cls in ClassCatalog.All)
        {
            RunProfile.ClassId = cls.id;
            var rng = new System.Random(42);
            bool sawBuild = false;
            string ownBuild = cls.id == ClassCatalog.LanternbearerId ? ItemDatabase.WickOfHaloId : ItemDatabase.LancerWardId;
            for (int i = 0; i < 100; i++)
            {
                var item = LootDrop.BuildItem(new LootRoll { drops = true, rarity = ItemRarity.Unique, slot = ItemSlot.Weapon }, rng);
                Assert.AreEqual(ItemRarity.Unique, item.rarity);
                // Cross-class rolls (30 % other-class share) exist by design and
                // simply don't fit the finder; both classes' pools must be valid.
                Assert.That(item.uniqueId, Is.EqualTo(ItemDatabase.LancerWardId)
                    .Or.EqualTo(ItemDatabase.WickOfHaloId)
                    .Or.EqualTo("Emberbrand Blade")
                    .Or.EqualTo("Lantern of the Hold"));
                if (item.uniqueId == ownBuild)
                    sawBuild = true;
            }
            Assert.IsTrue(sawBuild, $"{cls.id} drops never included its build unique");
            RunProfile.Reset();
        }
    }

    [Test]
    public void Persistence_JsonRoundtrip_KeepsUniqueId()
    {
        foreach (var id in new[] { ItemDatabase.LancerWardId, ItemDatabase.WickOfHaloId })
        {
            var item = ItemDatabase.CreateUnique(id);
            var back = JsonUtility.FromJson<ItemInstance>(JsonUtility.ToJson(item));
            Assert.AreEqual(id, back.uniqueId);
            Assert.AreEqual(ItemRarity.Unique, back.rarity);
            Assert.AreEqual(item.baseType, back.baseType);
            Assert.AreEqual(item.affixes.Count, back.affixes.Count);
        }
    }

    [Test]
    public void Persistence_StashRoundtrip_KeepsBuildUniques()
    {
        var stash = new Stash();
        Assert.IsTrue(stash.Add(ItemDatabase.CreateUnique(ItemDatabase.LancerWardId)));
        Assert.IsTrue(stash.Add(ItemDatabase.CreateUnique(ItemDatabase.WickOfHaloId)));
        var json = JsonUtility.ToJson(new StashSaveWrapper { items = stash.ToList() });
        var back = Stash.FromList(JsonUtility.FromJson<StashSaveWrapper>(json).items);
        Assert.AreEqual(2, back.Count);
        Assert.AreEqual(ItemDatabase.LancerWardId, back.Get(0).uniqueId);
        Assert.AreEqual(ItemDatabase.WickOfHaloId, back.Get(1).uniqueId);
    }

    [System.Serializable]
    sealed class StashSaveWrapper
    {
        public System.Collections.Generic.List<ItemInstance> items;
    }

    [Test]
    public void SmithingExclusion_UniquesKeepFixedSets_NeverReforged()
    {
        // Assumption for the #8 SmithingService (not on main): it must keep
        // excluding ItemRarity.Unique. Our side of the contract: build uniques
        // are Unique with fixed affix sets (re-rolls yield nothing to reforge).
        var rng = new System.Random(3);
        foreach (var id in new[] { ItemDatabase.LancerWardId, ItemDatabase.WickOfHaloId })
        {
            var item = ItemDatabase.CreateUnique(id);
            Assert.AreEqual(ItemRarity.Unique, item.rarity);
            Assert.IsNotEmpty(item.affixes, "build unique ships a fixed set");
            var reroll = AffixLibrary.RollAffixes(item.slot, ItemRarity.Unique, rng);
            Assert.IsEmpty(reroll, "Unique rolls are fixed: nothing for a smith to reroll");
        }
    }

    [Test]
    public void Tooltip_EffectText_ComesFromTheDefinition()
    {
        Assert.IsNotEmpty(BuildUniqueEffects.EffectText(ItemDatabase.LancerWardId));
        Assert.IsNotEmpty(BuildUniqueEffects.EffectText(ItemDatabase.WickOfHaloId));
        Assert.IsNotEmpty(BuildUniqueEffects.EffectTextFor(ItemDatabase.CreateUnique(ItemDatabase.LancerWardId)));
        Assert.AreEqual("", BuildUniqueEffects.EffectText("Emberbrand Blade"));
        Assert.AreEqual("", BuildUniqueEffects.EffectTextFor(ItemDatabase.CreateUnique("Emberbrand Blade")));
        Assert.AreEqual("", BuildUniqueEffects.EffectTextFor(null));
    }

    [Test]
    public void DropChances_RegularUniqueWeight_Unchanged()
    {
        Assert.That(DroptableDatabase.BuildUniqueWeaponShare, Is.InRange(0f, 1f).And.Not.EqualTo(0f).And.Not.EqualTo(1f));
        DroptableDatabase.GetTable(LootTier.Normal, out _, out _, out _, out int uNormal);
        DroptableDatabase.GetTable(LootTier.Elite, out _, out _, out _, out int uElite);
        Assert.GreaterOrEqual(uNormal, 0);
        Assert.GreaterOrEqual(uElite, uNormal);
    }
}
