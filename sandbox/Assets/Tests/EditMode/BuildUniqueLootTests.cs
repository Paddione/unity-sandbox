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
                if (item.uniqueId == ItemDatabase.LancerWardId || item.uniqueId == ItemDatabase.WickOfHaloId
                    || item.uniqueId == ItemDatabase.NightfangId)
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
            string ownBuild = cls.id == ClassCatalog.LanternbearerId ? ItemDatabase.WickOfHaloId
                : cls.id == ClassCatalog.RogueId ? ItemDatabase.NightfangId : ItemDatabase.LancerWardId;
            for (int i = 0; i < 100; i++)
            {
                var item = LootDrop.BuildItem(new LootRoll { drops = true, rarity = ItemRarity.Unique, slot = ItemSlot.Weapon }, rng);
                Assert.AreEqual(ItemRarity.Unique, item.rarity);
                // Cross-class rolls (30 % other-class share) exist by design and
                // simply don't fit the finder; all classes' pools must be valid.
                Assert.That(item.uniqueId, Is.EqualTo(ItemDatabase.LancerWardId)
                    .Or.EqualTo(ItemDatabase.WickOfHaloId)
                    .Or.EqualTo(ItemDatabase.NightfangId)
                    .Or.EqualTo(ItemDatabase.MoorvogtAnchorId)
                    .Or.EqualTo(ItemDatabase.SmugglersFuseId)
                    .Or.EqualTo("Emberbrand Blade")
                    .Or.EqualTo("Lantern of the Hold")
                    .Or.EqualTo(ItemDatabase.RogueKeepsakeId));
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

    // ---------------- S4 role uniques (cinderreach-itemization S4) ----------------

    [Test]
    public void Matrix_MapsNineCombos_ToSlotCorrectUniques()
    {
        var roles = new[] {
            ClassDefinition.PartyRole.Tank,
            ClassDefinition.PartyRole.Healer,
            ClassDefinition.PartyRole.Dps,
        };
        var seen = new System.Collections.Generic.HashSet<string>();
        foreach (var role in roles)
            foreach (var slot in new[] { ItemSlot.Weapon, ItemSlot.Armor, ItemSlot.Helmet })
            {
                string id = ItemDatabase.UniqueForMatrix(role, slot);
                Assert.IsTrue(ItemDatabase.IsMatrixUnique(id), $"{role}/{slot} maps to a matrix id");
                Assert.IsTrue(seen.Add(id), $"{id} used twice in the matrix");
                var item = ItemDatabase.CreateUnique(id);
                Assert.AreEqual(ItemRarity.Unique, item.rarity);
                Assert.AreEqual(slot, item.slot, $"{id} must drop in its matrix slot");
            }
        Assert.AreEqual(9, seen.Count, "3 roles x weapon/armor/helmet");
        Assert.AreEqual(ItemDatabase.LancerWardId,
            ItemDatabase.UniqueForMatrix(ClassDefinition.PartyRole.Tank, ItemSlot.Weapon));
        Assert.AreEqual(ItemDatabase.WickOfHaloId,
            ItemDatabase.UniqueForMatrix(ClassDefinition.PartyRole.Healer, ItemSlot.Weapon));
        Assert.AreEqual(ItemDatabase.NightfangId,
            ItemDatabase.UniqueForMatrix(ClassDefinition.PartyRole.Dps, ItemSlot.Weapon),
            "NightfangId is the Rogue matrix weapon (RogueKeepsake stays legacy)");
    }

    [Test]
    public void Matrix_ArmorAndHelms_AreFreelyWearable()
    {
        foreach (var id in new[] {
            ItemDatabase.BulwarkCarrierId, ItemDatabase.MercyShroudId, ItemDatabase.ShadowstrideVestId,
            ItemDatabase.SeismicCrownId, ItemDatabase.DawnCircleId, ItemDatabase.KeenCadenceId })
        {
            var item = ItemDatabase.CreateUnique(id);
            Assert.AreEqual("", ItemDatabase.ClassRestriction(item.baseType), $"{id} on a shared base");
            foreach (var cls in ClassCatalog.All)
                Assert.IsTrue(Equipment.CanEquip(item, cls.id), $"{id} wearable by {cls.id}");
        }
    }

    [Test]
    public void Matrix_Weapons_KeepTheirClassBinding()
    {
        // Pinned by BuildUniques_AreClassBoundWeapons: matrix weapons reuse the
        // class-bound build uniques, so the binding stays by design.
        Assert.AreEqual(ClassCatalog.FenwalkerId,
            ItemDatabase.ClassRestriction(ItemDatabase.CreateUnique(ItemDatabase.LancerWardId).baseType));
        Assert.AreEqual(ClassCatalog.LanternbearerId,
            ItemDatabase.ClassRestriction(ItemDatabase.CreateUnique(ItemDatabase.WickOfHaloId).baseType));
        Assert.AreEqual(ClassCatalog.RogueId,
            ItemDatabase.ClassRestriction(ItemDatabase.CreateUnique(ItemDatabase.NightfangId).baseType));
    }

    [Test]
    public void LootDrop_BuildItem_RoutesEveryMatrixId_WithoutTouchingRarityWeights()
    {
        var seen = new System.Collections.Generic.HashSet<string>();
        for (int seed = 0; seed < 60; seed++)
        {
            var rng = new System.Random(seed);
            foreach (var slot in new[] { ItemSlot.Weapon, ItemSlot.Armor, ItemSlot.Helmet })
            {
                RunProfile.ClassId = ClassCatalog.All[seed % ClassCatalog.All.Length].id;
                var item = LootDrop.BuildItem(
                    new LootRoll { drops = true, rarity = ItemRarity.Unique, slot = slot }, rng);
                if (ItemDatabase.IsMatrixUnique(item.uniqueId)) seen.Add(item.uniqueId);
            }
        }
        RunProfile.Reset();
        foreach (var id in ItemDatabase.MatrixUniqueIds)
            Assert.IsTrue(seen.Contains(id), $"{id} reachable through the regular unique pool");
        // Legacy armor/helmets still drop (within-slot split, aggregate untouched).
        bool sawPlate = false, sawCirclet = false;
        for (int seed = 0; seed < 200; seed++)
        {
            var rng = new System.Random(seed);
            var armor = LootDrop.BuildItem(
                new LootRoll { drops = true, rarity = ItemRarity.Unique, slot = ItemSlot.Armor }, rng);
            var helm = LootDrop.BuildItem(
                new LootRoll { drops = true, rarity = ItemRarity.Unique, slot = ItemSlot.Helmet }, rng);
            if (armor.uniqueId == "Cryptwarden Plate") sawPlate = true;
            if (helm.uniqueId == "Seer's Circlet") sawCirclet = true;
        }
        Assert.IsTrue(sawPlate && sawCirclet, "legacy armor/helmet still in the pool");
        Assert.That(DroptableDatabase.MatrixNonWeaponShare,
            Is.InRange(0f, 1f).And.Not.EqualTo(0f).And.Not.EqualTo(1f));
    }

    [Test]
    public void Matrix_UniqueDrops_CarryTopBracketValues()
    {
        foreach (var id in ItemDatabase.MatrixUniqueIds)
        {
            var item = ItemDatabase.RollUnique(ItemDatabase.CreateUnique(id), 10, 2);
            Assert.AreEqual(10, item.itemLevel);
            Assert.AreEqual(2, item.affixTier);
            Assert.IsNotEmpty(item.instanceId);
            foreach (var affix in item.affixes)
                Assert.AreEqual(AffixLibrary.GetRange(affix.key, 10, 2).max, affix.value,
                    $"{id} affix {affix.key} at the top of its bracket");
        }
    }

    [Test]
    public void Matrix_Persistence_Roundtrips_AllNine()
    {
        var stash = new Stash();
        foreach (var id in ItemDatabase.MatrixUniqueIds)
            Assert.IsTrue(stash.Add(ItemDatabase.CreateUnique(id)), $"{id} fits the stash");
        var json = JsonUtility.ToJson(new StashSaveWrapper { items = stash.ToList() });
        var back = Stash.FromList(JsonUtility.FromJson<StashSaveWrapper>(json).items);
        Assert.AreEqual(9, back.Count);
        for (int i = 0; i < 9; i++)
        {
            Assert.AreEqual(ItemDatabase.MatrixUniqueIds[i], back.Get(i).uniqueId);
            var viaJson = JsonUtility.FromJson<ItemInstance>(JsonUtility.ToJson(back.Get(i)));
            Assert.AreEqual(ItemDatabase.MatrixUniqueIds[i], viaJson.uniqueId);
            Assert.AreEqual(ItemRarity.Unique, viaJson.rarity);
        }
    }

    [Test]
    public void Matrix_Smithing_StaysExcluded()
    {
        // Standing rarity rule (uniques are never reforged/salvaged) covers the
        // six new pieces exactly like the legacy three.
        SmithingService.SaveHook = _ => true;
        var hero = new GameObject("Player");
        try
        {
            var inv = hero.AddComponent<Inventory>();
            var wallet = hero.AddComponent<Wallet>();
            wallet.Add(1000);
            foreach (var id in ItemDatabase.MatrixUniqueIds)
            {
                var item = ItemDatabase.CreateUnique(id);
                Assert.IsTrue(inv.Add(item));
                int idx = inv.Count - 1;
                Assert.IsFalse(SmithingService.TrySalvage(hero, idx, item), $"{id} not salvageable");
                float min, max;
                string pool = AffixLibrary.PoolFor(item.slot).Count > 0
                    ? AffixLibrary.PoolFor(item.slot)[0] : AffixLibrary.Damage;
                Assert.IsFalse(SmithingService.PreviewReforge(hero, idx, item, 0, pool, out min, out max),
                    $"{id} not reforgeable");
                Assert.IsTrue(inv.RemoveAt(idx));
            }
        }
        finally
        {
            Object.DestroyImmediate(hero);
            SmithingService.SaveHook = null;
            SmithingService.ResetSession();
            Wallet.ResetRun();
            RunProfile.Reset();
        }
    }

    [Test]
    public void HostValidation_KnownUniquesPass_UnknownIdsRejected()
    {
        foreach (var id in ItemDatabase.MatrixUniqueIds)
            Assert.IsTrue(ItemDatabase.IsKnownUniqueId(id), $"{id} known to the host");
        foreach (var id in new[] {
            ItemDatabase.LancerWardId, ItemDatabase.WickOfHaloId, ItemDatabase.NightfangId,
            ItemDatabase.RogueKeepsakeId, "Emberbrand Blade", "Lantern of the Hold",
            "Cryptwarden Plate", "Seer's Circlet" })
            Assert.IsTrue(ItemDatabase.IsKnownUniqueId(id), $"{id} known to the host");
        Assert.IsFalse(ItemDatabase.IsKnownUniqueId("godmode-blade"));
        Assert.IsFalse(ItemDatabase.IsKnownUniqueId(""));
        Assert.IsFalse(ItemDatabase.IsKnownUniqueId(null));
    }

    [Test]
    public void HostJoinReport_StripsUnknownUnique_KeepsMatrix()
    {
        var report = new DiabloNetworkPlayer.JoinReport();
        report.weapon = ItemDatabase.CreateUnique(ItemDatabase.NightfangId);
        report.armor = new ItemInstance
        {
            slot = ItemSlot.Armor, rarity = ItemRarity.Unique,
            baseType = "Breastplate", uniqueId = "godmode-plate",
        };
        report.helmet = ItemDatabase.CreateUnique(ItemDatabase.DawnCircleId);
        var clean = DiabloNetworkPlayer.SanitizeJoinReport(report);
        Assert.AreEqual(ItemDatabase.NightfangId, clean.weapon.uniqueId);
        Assert.IsNull(clean.armor, "unknown unique ids never reach a mirror");
        Assert.AreEqual(ItemDatabase.DawnCircleId, clean.helmet.uniqueId);
    }

    [Test]
    public void Matrix_EffectTexts_CoverAllNine()
    {
        foreach (var id in ItemDatabase.MatrixUniqueIds)
            Assert.IsNotEmpty(BuildUniqueEffects.EffectText(id), $"{id} has tooltip text");
        Assert.IsNotEmpty(BuildUniqueEffects.EffectTextFor(
            ItemDatabase.CreateUnique(ItemDatabase.BulwarkCarrierId)));
    }
}
