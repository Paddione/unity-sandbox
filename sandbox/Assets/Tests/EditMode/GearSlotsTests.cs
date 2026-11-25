using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// gear-slots goal (cinderreach-gear-slots-flask): new slots, save v15, flask,
/// the 18 uniques and the drop balance with pity.
/// </summary>
public class GearSlotsTests
{
    readonly List<GameObject> spawned = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (var go in spawned) if (go != null) Object.DestroyImmediate(go);
        spawned.Clear();
        UniquePity.Current = 0;
    }

    GameObject Hero(Vector3 at)
    {
        var go = new GameObject("GearSlotsHero");
        go.transform.position = at;
        go.AddComponent<Equipment>();
        go.AddComponent<Inventory>();
        var hp = go.AddComponent<PlayerHealth>();
        hp.Refresh();
        spawned.Add(go);
        return go;
    }

    static ItemInstance Rare(ItemSlot slot, string baseType, params (string key, float value)[] affixes) => new ItemInstance
    {
        slot = slot,
        rarity = ItemRarity.Rare,
        baseType = baseType,
        itemLevel = 1,
        affixTier = 1,
        instanceId = System.Guid.NewGuid().ToString("N"),
        affixes = affixes.Select(a => new Affix(a.key, a.value)).ToList(),
    };

    // ---- S1 slots ------------------------------------------------------------

    [Test]
    public void SlotEnum_KeepsOldValuesAndAppendsNewSlots()
    {
        Assert.AreEqual(0, (int)ItemSlot.Armor);
        Assert.AreEqual(1, (int)ItemSlot.Weapon);
        Assert.AreEqual(2, (int)ItemSlot.Helmet);
        Assert.AreEqual(3, (int)ItemSlot.Consumable);
        Assert.AreEqual(4, (int)ItemSlot.Gloves);
        Assert.AreEqual(5, (int)ItemSlot.Boots);
        Assert.AreEqual(6, (int)ItemSlot.Belt);
        CollectionAssert.AreEquivalent(
            new[] { ItemSlot.Armor, ItemSlot.Weapon, ItemSlot.Helmet, ItemSlot.Gloves, ItemSlot.Boots, ItemSlot.Belt },
            Equipment.GearSlots);
        Assert.IsFalse(Equipment.IsGearSlot(ItemSlot.Consumable));
    }

    [Test]
    public void NewSlots_EquipUnequipAndFeedStats()
    {
        var hero = Hero(new Vector3(200f, 0f, 0f));
        var eq = hero.GetComponent<Equipment>();
        var inv = hero.GetComponent<Inventory>();
        float baseHp = eq.GetMaxHP();
        var gloves = Rare(ItemSlot.Gloves, "LeatherGloves", (AffixLibrary.Crit, 6f));
        var boots = Rare(ItemSlot.Boots, "LeatherBoots", (AffixLibrary.MoveSpeed, 0.2f), (AffixLibrary.MaxHP, 10f));
        var belt = Rare(ItemSlot.Belt, "LeatherBelt", (AffixLibrary.MaxHP, 5f), (AffixLibrary.FlaskHealing, 10f));
        foreach (var item in new[] { gloves, boots, belt }) Assert.IsTrue(Equipment.CanEquip(item, ClassCatalog.RogueId));
        eq.Equip(gloves);
        eq.Equip(boots);
        eq.Equip(belt);
        Assert.AreSame(gloves, eq.GetEquipped(ItemSlot.Gloves));
        Assert.AreSame(boots, eq.GetEquipped(ItemSlot.Boots));
        Assert.AreSame(belt, eq.GetEquipped(ItemSlot.Belt));
        Assert.AreEqual(baseHp + 15f, eq.GetMaxHP(), 0.001f, "Boots and belt life count.");
        Assert.AreEqual(Equipment.BaseMoveSpeed + 0.2f, eq.GetMoveSpeed(), 0.001f);
        Assert.AreEqual(Equipment.BaseCritChance + 6f, eq.GetCrit(), 0.001f);
        Assert.AreEqual(10f, eq.GearTotal(AffixLibrary.FlaskHealing), 0.001f);
        Assert.IsTrue(eq.HasInstance(belt.instanceId));
        Assert.IsTrue(eq.UnequipTo(ItemSlot.Gloves, inv));
        Assert.IsNull(eq.GetEquipped(ItemSlot.Gloves));
        Assert.AreEqual(1, inv.Count);
    }

    [Test]
    public void NewSlots_HaveBasesAndAffixPools()
    {
        foreach (var slot in new[] { ItemSlot.Gloves, ItemSlot.Boots, ItemSlot.Belt })
        {
            Assert.AreEqual(3, ItemDatabase.GetBaseTypes(slot).Count, slot.ToString());
            Assert.GreaterOrEqual(AffixLibrary.PoolFor(slot).Count, 4, slot.ToString());
            foreach (var b in ItemDatabase.GetBaseTypes(slot))
            {
                Assert.AreEqual("", ItemDatabase.ClassRestriction(b), b + " is shared by every class.");
                Assert.IsTrue(Loc.Has(LootDrop.ItemNameKey(b)), b + " needs a display name.");
                StringAssert.StartsWith("base:", DiabloMenuArt.ArtKeyFor(Rare(slot, b)));
                CollectionAssert.Contains(DiabloMenuArt.AllArtBases, b);
            }
        }
        CollectionAssert.AreEqual(new[] { ItemSlot.Belt }, AffixLibrary.Get(AffixLibrary.FlaskHealing).slots);
        CollectionAssert.DoesNotContain(AffixLibrary.PoolFor(ItemSlot.Weapon), AffixLibrary.FlaskHealing);
        var rng = new System.Random(3);
        var rolled = AffixLibrary.RollAffixes(ItemSlot.Boots, ItemRarity.Rare, rng, 1, 5);
        Assert.That(rolled.Count, Is.InRange(3, 6));
        foreach (var a in rolled) Assert.IsTrue(AffixLibrary.Get(a.key).AllowedOn(ItemSlot.Boots));
    }

    [Test]
    public void HostValidation_AcceptsNewSlotsAndRejectsConsumables()
    {
        var ids = new HashSet<string>();
        Assert.IsNotNull(NetworkItemOwnership.Validate(Rare(ItemSlot.Belt, "RopeSash", (AffixLibrary.MaxHP, 5f)), ids));
        Assert.IsNull(NetworkItemOwnership.Validate(Rare(ItemSlot.Belt, "Hood"), ids), "Wrong base for the slot.");
        var potion = new ItemInstance { slot = ItemSlot.Consumable, rarity = ItemRarity.Rare, baseType = "Healing Potion" };
        Assert.IsNull(NetworkItemOwnership.Validate(potion, ids), "The vendor potion is retired.");
        foreach (var id in ItemDatabase.GearSlotUniqueIds)
            Assert.IsNotNull(NetworkItemOwnership.Validate(ItemDatabase.RollUnique(ItemDatabase.CreateUnique(id), 10, 1), ids), id);
    }

    // ---- S1 save v15 ---------------------------------------------------------

    [Test]
    public void Migration_V14ToV15_RefundsPotionsAndKeepsItems()
    {
        var sword = ItemDatabase.CreateStarterSword();
        sword.instanceId = System.Guid.NewGuid().ToString("N");
        var plate = ItemDatabase.RollUnique(ItemDatabase.CreateUnique("Cryptwarden Plate"), 5, 1);
        var potion = new ItemInstance { slot = ItemSlot.Consumable, rarity = ItemRarity.Rare, baseType = "Healing Potion" };
        var old = new SaveGame { v = 14, gold = 10, equipmentArmor = plate };
        old.bag.Add(potion);
        old.bag.Add(sword);
        old.bag.Add(potion);
        old.stash.Add(potion);
        string json = JsonUtility.ToJson(old).Replace("\"v\":15", "\"v\":14");
        var outcome = SaveMigrations.Migrate(json, out var save, out int from);
        Assert.AreEqual(SaveMigrations.Outcome.Migrated, outcome);
        Assert.AreEqual(14, from);
        Assert.AreEqual(15, save.v);
        Assert.AreEqual(10 + 3 * SaveMigrations.LegacyPotionRefund, save.gold);
        Assert.AreEqual(1, save.bag.Count);
        Assert.AreEqual(sword.instanceId, save.bag[0].instanceId);
        Assert.AreEqual(0, save.stash.Count);
        Assert.AreEqual(plate.instanceId, save.equipmentArmor.instanceId);
        Assert.AreEqual("Cryptwarden Plate", save.equipmentArmor.uniqueId);
        Assert.IsTrue(Equipment.IsBlank(save.equipmentGloves));
        Assert.IsTrue(Equipment.IsBlank(save.equipmentBoots));
        Assert.IsTrue(Equipment.IsBlank(save.equipmentBelt));
        Assert.AreEqual(0, save.uniquePity);
    }

    [Test]
    public void Save_RoundtripsNewSlotsAndPity()
    {
        var hero = Hero(new Vector3(200f, 0f, 0f));
        var eq = hero.GetComponent<Equipment>();
        var gloves = ItemDatabase.RollUnique(ItemDatabase.CreateUnique(ItemDatabase.CutpurseWrapsId), 5, 1);
        var boots = Rare(ItemSlot.Boots, "IronSabatons", (AffixLibrary.Armor, 2f));
        var belt = ItemDatabase.RollUnique(ItemDatabase.CreateUnique(ItemDatabase.VialSashId), 5, 1);
        eq.Equip(gloves);
        eq.Equip(boots);
        eq.Equip(belt);
        UniquePity.Current = 17;
        var save = SaveService.CaptureState(hero);
        string json = JsonUtility.ToJson(save);
        UniquePity.Current = 0;
        Assert.AreEqual(SaveMigrations.Outcome.Current, SaveMigrations.Migrate(json, out var back, out _));
        var fresh = Hero(new Vector3(200f, 0f, 0f));
        SaveService.ApplyState(fresh, back);
        var eq2 = fresh.GetComponent<Equipment>();
        Assert.AreEqual(gloves.instanceId, eq2.gloves.instanceId);
        Assert.AreEqual(ItemDatabase.CutpurseWrapsId, eq2.gloves.uniqueId);
        Assert.AreEqual(boots.instanceId, eq2.boots.instanceId);
        Assert.AreEqual(belt.affixes[0].value, eq2.belt.affixes[0].value, 0.001f);
        Assert.AreEqual(17, UniquePity.Current);
    }

    // ---- S2 flask --------------------------------------------------------------

    [Test]
    public void Flask_HealsFortyPercentOverFiveSecondsThenCoolsDown()
    {
        var hero = Hero(new Vector3(200f, 0f, 0f));
        var hp = hero.GetComponent<PlayerHealth>();
        var flask = HealingFlask.For(hero);
        hp.TakeDamage(15f);
        float hurt = hp.CurrentHP;
        float t0 = Time.time;
        Assert.AreEqual(1, flask.Charges);
        Assert.IsTrue(flask.TryDrink());
        Assert.AreEqual(0, flask.Charges);
        Assert.IsFalse(flask.TryDrink(), "No second drink while the flask is empty.");
        Assert.AreEqual(hurt, hp.CurrentHP, 0.001f, "The heal ticks, it is not instant.");
        float total = hp.MaxHP * HealingFlask.HealFraction;
        flask.Tick(t0 + HealingFlask.TickSeconds);
        Assert.AreEqual(hurt + total / HealingFlask.Ticks, hp.CurrentHP, 0.001f);
        hp.TakeDamage(1f);
        Assert.IsTrue(flask.IsHealing, "Damage never cancels the heal.");
        for (int i = 2; i <= HealingFlask.Ticks; i++) flask.Tick(t0 + i * HealingFlask.TickSeconds);
        Assert.IsFalse(flask.IsHealing);
        Assert.AreEqual(hurt + total - 1f, hp.CurrentHP, 0.001f);
        Assert.AreEqual(total, flask.HealedTotal, 0.001f);
        flask.Tick(t0 + HealingFlask.CooldownSeconds - 0.1f);
        Assert.AreEqual(0, flask.Charges);
        flask.Tick(t0 + HealingFlask.CooldownSeconds);
        Assert.AreEqual(1, flask.Charges, "The charge returns after 60 s.");
    }

    [Test]
    public void Flask_RefillsInLanternHoldAndAtShrines()
    {
        var hero = Hero(new Vector3(200f, 0f, 0f));
        var flask = HealingFlask.For(hero);
        Assert.IsTrue(flask.TryDrink());
        float t0 = Time.time;
        flask.Tick(t0 + 1f);
        Assert.AreEqual(0, flask.Charges, "The Ossuary is no refill point.");
        Assert.IsTrue(HealingFlask.AtRefillPoint(SkillVariantCatalog.TownCenter));
        Assert.IsFalse(HealingFlask.AtRefillPoint(new Vector3(-15f, 0f, -4f)), "The Fen around the hold does not refill.");
        hero.transform.position = SkillVariantCatalog.TownCenter;
        flask.Tick(t0 + 2f);
        Assert.AreEqual(1, flask.Charges);

        var shrineGo = new GameObject("TestShrine");
        spawned.Add(shrineGo);
        shrineGo.transform.position = new Vector3(400f, 0f, 0f);
        var shrine = shrineGo.AddComponent<RiskShrine>();
        if (!RiskShrine.Live.Contains(shrine)) RiskShrine.Live.Add(shrine);
        try
        {
            Assert.IsTrue(HealingFlask.AtRefillPoint(new Vector3(401f, 0f, 0f)));
        }
        finally
        {
            RiskShrine.Live.Remove(shrine);
        }
    }

    [Test]
    public void Flask_BeltAffixAndBeltUniquesShapeIt()
    {
        var hero = Hero(new Vector3(200f, 0f, 0f));
        var eq = hero.GetComponent<Equipment>();
        var hp = hero.GetComponent<PlayerHealth>();
        var flask = HealingFlask.For(hero);
        float plain = HealingFlask.HealAmount(hp, eq);
        eq.Equip(Rare(ItemSlot.Belt, "LeatherBelt", (AffixLibrary.FlaskHealing, 25f)));
        Assert.AreEqual(plain * 1.25f, HealingFlask.HealAmount(hp, eq), 0.001f);
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.VialSashId));
        Assert.AreEqual(2, flask.MaxCharges);
        Assert.AreEqual(2, flask.Charges);
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.QuickdrawBeltId));
        Assert.AreEqual(GearUniqueEffects.QuickdrawCooldown, flask.Cooldown);
        Assert.AreEqual(1, flask.MaxCharges);
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.WardensGirdleId));
        Assert.IsTrue(flask.TryDrink());
        Assert.AreEqual(GearUniqueEffects.WardenShieldAmount, hp.ShieldAmount, 0.001f);
    }

    // ---- S4/S5 uniques ----------------------------------------------------------

    [Test]
    public void Uniques_AllEighteenAreCompleteCatalogEntries()
    {
        Assert.AreEqual(18, ItemDatabase.GearSlotUniqueIds.Length);
        Assert.AreEqual(18, ItemDatabase.GearSlotUniqueIds.Distinct().Count());
        var perSlot = new Dictionary<ItemSlot, int>();
        foreach (var id in ItemDatabase.GearSlotUniqueIds)
        {
            Assert.IsTrue(ItemDatabase.IsKnownUniqueId(id), id);
            var u = ItemDatabase.CreateUnique(id);
            Assert.AreEqual(id, u.uniqueId);
            Assert.AreEqual(ItemRarity.Unique, u.rarity);
            perSlot[u.slot] = perSlot.TryGetValue(u.slot, out int n) ? n + 1 : 1;
            if (u.slot == ItemSlot.Weapon)
                Assert.AreNotEqual("", ItemDatabase.ClassRestriction(u.baseType), id + " stays class-bound.");
            else
                CollectionAssert.Contains(ItemDatabase.GetBaseTypes(u.slot), u.baseType, id);
            Assert.IsNotEmpty(BuildUniqueEffects.EffectText(id), id + " needs an effect text.");
            Assert.IsTrue(Loc.Has(LootDrop.ItemNameKey(id)), id + " needs a display name.");
            Assert.AreEqual("unique:" + id, DiabloMenuArt.ArtKeyFor(u));
            foreach (var a in u.affixes) Assert.IsNotNull(AffixLibrary.Get(a.key), id + " " + a.key);
            var rolled = ItemDatabase.RollUnique(ItemDatabase.CreateUnique(id), 10, 1);
            foreach (var a in rolled.affixes)
                Assert.AreEqual(AffixLibrary.GetRange(a.key, 10, 1).max, a.value, 0.001f, id + " rolls at the top.");
        }
        foreach (var slot in Equipment.GearSlots) Assert.AreEqual(3, perSlot[slot], slot + " gets three new uniques.");
        foreach (var slot in new[] { ItemSlot.Gloves, ItemSlot.Boots, ItemSlot.Belt })
            foreach (var id in ItemDatabase.RoleUniquesFor(slot))
                Assert.IsTrue(HeroGearModels.IsKnownLimb(id, slot), id + " has a worn look.");
    }

    [Test]
    public void Uniques_ZonePiecesUnlockByItemLevel()
    {
        CollectionAssert.AreEqual(new[] { ItemDatabase.FenWrapId }, ItemDatabase.ZoneUniquesFor(ItemSlot.Armor, 1));
        CollectionAssert.AreEqual(new[] { ItemDatabase.FenWrapId, ItemDatabase.AshenHauberkId }, ItemDatabase.ZoneUniquesFor(ItemSlot.Armor, 5));
        Assert.AreEqual(3, ItemDatabase.ZoneUniquesFor(ItemSlot.Helmet, 10).Count);
        Assert.IsEmpty(ItemDatabase.ZoneUniquesFor(ItemSlot.Weapon, 1, ClassCatalog.RogueId));
        CollectionAssert.AreEqual(new[] { ItemDatabase.GlassthornBowId }, ItemDatabase.ZoneUniquesFor(ItemSlot.Weapon, 5, ClassCatalog.RogueId));

        var seen = new HashSet<string>();
        var rng = new System.Random(11);
        for (int i = 0; i < 4000; i++)
        {
            foreach (var slot in new[] { ItemSlot.Armor, ItemSlot.Helmet, ItemSlot.Gloves, ItemSlot.Boots, ItemSlot.Belt })
            {
                string id = ItemDatabase.UniqueForSlot(slot, 1, rng);
                Assert.AreEqual(slot, ItemDatabase.CreateUnique(id).slot);
                Assert.LessOrEqual(ItemDatabase.MinZoneLevel(id), 1, id + " must not drop in the Fen.");
                seen.Add(id);
            }
            seen.Add(ItemDatabase.UniqueWeaponForClass(ClassCatalog.FenwalkerId, rng, 10).uniqueId);
            seen.Add(ItemDatabase.UniqueForSlot(ItemSlot.Armor, 10, rng));
            seen.Add(ItemDatabase.UniqueForSlot(ItemSlot.Helmet, 10, rng));
        }
        foreach (var id in ItemDatabase.GearSlotUniqueIds)
        {
            if (id == ItemDatabase.OssuaryThuribleId || id == ItemDatabase.GlassthornBowId) continue;
            CollectionAssert.Contains(seen, id, id + " is reachable through the drop path.");
        }
    }

    [Test]
    public void Uniques_PassiveAndHitEffectsReadLiveGear()
    {
        var hero = Hero(new Vector3(200f, 0f, 0f));
        var eq = hero.GetComponent<Equipment>();
        var hp = hero.GetComponent<PlayerHealth>();
        Assert.AreEqual(8f, GearUniqueEffects.ShapeIncoming(hp, 8f, null), 0.001f);
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.AshenHauberkId));
        Assert.AreEqual(6f, GearUniqueEffects.ShapeIncoming(hp, 8f, null), 0.001f);
        Assert.AreEqual(5f, GearUniqueEffects.ShapeIncoming(hp, 5f, null), 0.001f, "Hits up to 5 pass unchanged.");
        Assert.AreEqual(5f, GearUniqueEffects.ShapeIncoming(hp, 6f, null), 0.001f, "The cut never drops a heavy hit below 5.");
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.BonewatchVisorId));
        Assert.AreEqual(4f * GearUniqueEffects.BonewatchDamageMul,
            GearUniqueEffects.ShapeIncoming(hp, 4f, hero.transform.position + Vector3.right * 2f), 0.001f);
        Assert.AreEqual(4f, GearUniqueEffects.ShapeIncoming(hp, 4f, hero.transform.position + Vector3.right * 5f), 0.001f);

        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.GlassthornBowId));
        Assert.AreEqual(GearUniqueEffects.GlassthornCritMultiplier, GearUniqueEffects.CritMultiplier(eq, 2f));
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.MendersTouchId));
        Assert.AreEqual(GearUniqueEffects.MendersHealMul, GearUniqueEffects.HealMultiplier(eq));

        var kiln = ItemDatabase.CreateUnique(ItemDatabase.KilnheartMailId);
        eq.Equip(kiln);
        Assert.AreEqual(4, eq.ThornsDamage(), "2 Thorns doubled.");
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.ProspectorsHoodId));
        Assert.AreEqual(10f + 30f * GearUniqueEffects.ProspectorShare, eq.MagicFindPct(), 0.001f);
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.EmberwreathCrownId));
        eq.Equip(Rare(ItemSlot.Belt, "LeatherBelt", (AffixLibrary.CooldownReduction, 45f)));
        Assert.AreEqual(1f - 50f / 100f, eq.CooldownFactor(), 0.001f, "Cap rises to 50 % (10 crown + 45 belt).");

        Assert.IsTrue(eq.UnequipTo(ItemSlot.Armor, hero.GetComponent<Inventory>()));
        Assert.AreEqual(0, eq.ThornsDamage(), "Taking the mail off ends the effect at once.");
    }

    [Test]
    public void Uniques_IronboundGripsShieldOnHitWithInterval()
    {
        var hero = Hero(new Vector3(200f, 0f, 0f));
        var eq = hero.GetComponent<Equipment>();
        var hp = hero.GetComponent<PlayerHealth>();
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.IronboundGripsId));
        hp.TakeDamage(1f);
        Assert.AreEqual(GearUniqueEffects.GripShieldAmount, hp.ShieldAmount, 0.001f);
        hp.TakeDamage(1f);
        Assert.AreEqual(GearUniqueEffects.GripShieldAmount - 1f, hp.ShieldAmount, 0.001f, "No refresh inside the interval.");
    }

    // ---- S6 drops ----------------------------------------------------------------

    [Test]
    public void SlotWeights_SumToHundredWithTheAgreedSplit()
    {
        Assert.AreEqual(100, LootTable.SlotWeights.Sum(w => w.weight));
        var map = LootTable.SlotWeights.ToDictionary(w => w.slot, w => w.weight);
        Assert.AreEqual(25, map[ItemSlot.Weapon]);
        Assert.AreEqual(20, map[ItemSlot.Armor]);
        Assert.AreEqual(15, map[ItemSlot.Helmet]);
        Assert.AreEqual(15, map[ItemSlot.Gloves]);
        Assert.AreEqual(15, map[ItemSlot.Boots]);
        Assert.AreEqual(10, map[ItemSlot.Belt]);
    }

    [Test]
    public void Simulation_TenThousandDropsPerTierMatchRarityAndSlotWeights()
    {
        foreach (LootTier tier in System.Enum.GetValues(typeof(LootTier)))
        {
            var rng = new System.Random(1000 + (int)tier);
            LootTable.WeightsFor(tier, out _, out _, out int uniqueW);
            int drops = 0, uniques = 0;
            var slots = new Dictionary<ItemSlot, int>();
            while (drops < 10000)
            {
                var roll = LootTable.Roll(tier, rng);
                if (!roll.drops) continue;
                drops++;
                if (roll.rarity == ItemRarity.Unique) uniques++;
                Assert.AreNotEqual(ItemRarity.Magic, roll.rarity);
                slots[roll.slot] = slots.TryGetValue(roll.slot, out int n) ? n + 1 : 1;
            }
            Assert.AreEqual(uniqueW, 100.0 * uniques / drops, 1.5, $"{tier}: unique share stays at its weight.");
            foreach (var (slot, weight) in LootTable.SlotWeights)
                Assert.AreEqual(weight, 100.0 * slots[slot] / drops, 1.5, $"{tier} {slot}");
        }
    }

    [Test]
    public void Pity_ForcesUniqueAfterFortyRaresAndResets()
    {
        int streak = 0;
        var rare = new LootRoll { drops = true, rarity = ItemRarity.Rare, slot = ItemSlot.Boots };
        for (int i = 0; i < LootTable.PityThreshold; i++)
            Assert.AreEqual(ItemRarity.Rare, LootTable.ApplyPity(rare, ref streak).rarity);
        Assert.AreEqual(LootTable.PityThreshold, streak);
        var forced = LootTable.ApplyPity(rare, ref streak);
        Assert.AreEqual(ItemRarity.Unique, forced.rarity);
        Assert.AreEqual(ItemSlot.Boots, forced.slot, "Pity keeps the rolled slot.");
        Assert.AreEqual(0, streak);
        LootTable.ApplyPity(new LootRoll { drops = false }, ref streak);
        Assert.AreEqual(0, streak, "No-drop rolls leave the counter alone.");
        LootTable.ApplyPity(rare, ref streak);
        LootTable.ApplyPity(new LootRoll { drops = true, rarity = ItemRarity.Unique }, ref streak);
        Assert.AreEqual(0, streak, "A natural Unique resets it.");

        var rng = new System.Random(5);
        int run = 0, longest = 0;
        streak = 0;
        for (int i = 0; i < 10000; i++)
        {
            var roll = LootTable.ApplyPity(LootTable.Roll(LootTier.Normal, rng), ref streak);
            if (!roll.drops) continue;
            run = roll.rarity == ItemRarity.Unique ? 0 : run + 1;
            longest = Mathf.Max(longest, run);
        }
        Assert.LessOrEqual(longest, LootTable.PityThreshold, "No run without a Unique is longer than 40 drops.");
    }
}
