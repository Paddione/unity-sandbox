using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>replayability S2: affix table, tiers, effect hooks, compare tooltip.</summary>
public class AffixTableTests
{
    readonly List<GameObject> spawned = new List<GameObject>();

    [TearDown]
    public void Teardown()
    {
        foreach (var go in spawned)
            if (go != null) Object.DestroyImmediate(go);
        spawned.Clear();
    }

    GameObject Hero()
    {
        var go = new GameObject("AffixHero");
        go.AddComponent<Equipment>();
        go.AddComponent<PlayerHealth>();
        spawned.Add(go);
        return go;
    }

    static ItemInstance Item(ItemSlot slot, params (string key, float value)[] affixes)
    {
        var item = new ItemInstance { slot = slot, rarity = ItemRarity.Rare, baseType = "Test" };
        foreach (var (k, v) in affixes) item.affixes.Add(new Affix(k, v));
        return item;
    }

    [Test]
    public void VendorEquipment_CarriesItsSourceLevel()
    {
        var gear = VendorStock.Roll(12).offers.FindAll(offer => offer.item.slot != ItemSlot.Consumable);
        Assert.IsNotEmpty(gear);
        foreach (var offer in gear)
        {
            Assert.AreEqual(ItemLevelRules.VendorLevel, offer.item.itemLevel);
            Assert.IsNotEmpty(offer.item.instanceId);
        }
    }

    [TestCase(0, 0)]
    [TestCase(1, 1)]
    [TestCase(4, 1)]
    [TestCase(5, 2)]
    [TestCase(9, 2)]
    [TestCase(10, 3)]
    [TestCase(14, 3)]
    [TestCase(15, 4)]
    [TestCase(40, 4)]
    public void BracketBoundaries(int level, int bracket)
    {
        Assert.AreEqual(bracket, ItemLevelRules.GetBracket(level));
    }

    [Test]
    public void DropAffixes_UseSourceLevelAndFrozenDifficultyTier()
    {
        var rng = new System.Random(27);
        foreach (int level in new[] { 1, 5, 10 })
        foreach (int tier in new[] { 1, 2 })
        foreach (ItemSlot slot in new[] { ItemSlot.Weapon, ItemSlot.Armor, ItemSlot.Helmet })
        {
            var roll = new LootRoll { drops = true, slot = slot, rarity = ItemRarity.Rare, affixTier = tier };
            for (int i = 0; i < 50; i++)
            {
                var item = LootDrop.BuildItem(roll, rng, level);
                Assert.AreEqual(level, item.itemLevel);
                Assert.AreEqual(tier, item.affixTier);
                foreach (var affix in item.affixes)
                {
                    var range = AffixLibrary.GetRange(affix.key, level, tier);
                    Assert.IsTrue(AffixLibrary.Get(affix.key).AllowedOn(slot));
                    Assert.That(affix.value, Is.InRange(range.min, range.max), affix.key);
                }
            }
        }
    }

    [Test]
    public void SourceLevel_ComesFromEncounterZoneAndTier()
    {
        Assert.AreEqual(1, ItemLevelRules.ForDrop(Vector3.zero, LootTier.Normal));
        Assert.AreEqual(2, ItemLevelRules.ForDrop(Vector3.zero, LootTier.Chest));
        Assert.AreEqual(5, ItemLevelRules.ForDrop(KilnLayout.OssuaryOffset, LootTier.Normal));
        Assert.AreEqual(6, ItemLevelRules.ForDrop(KilnLayout.OssuaryOffset, LootTier.Elite));
        Assert.AreEqual(12, ItemLevelRules.ForDrop(KilnLayout.KilnOffset, LootTier.ActBoss));
        Assert.AreEqual(ItemLevelRules.BarrensLevel, ItemLevelRules.ForDrop(BarrensLayout.BarrensOffset, LootTier.Normal));
        Assert.AreEqual(ItemLevelRules.BarrensLevel + 2, ItemLevelRules.ForDrop(BarrensLayout.GlasswrightPos, LootTier.Boss));
    }

    [Test]
    public void SameSourceAndSeed_KeepsRollsAfterHeroLevels()
    {
        var hero = Hero();
        var progression = hero.AddComponent<DiabloProgression>();
        var roll = new LootRoll { drops = true, slot = ItemSlot.Armor, rarity = ItemRarity.Rare, affixTier = 1 };
        int level = ItemLevelRules.ForDrop(KilnLayout.OssuaryOffset, LootTier.Elite);
        var before = LootDrop.BuildItem(roll, new System.Random(11), level);
        progression.SetState(20, 0);
        var after = LootDrop.BuildItem(roll, new System.Random(11), level);

        Assert.AreEqual(before.itemLevel, after.itemLevel);
        Assert.AreEqual(before.baseType, after.baseType);
        Assert.AreEqual(before.affixes.Count, after.affixes.Count);
        for (int i = 0; i < before.affixes.Count; i++)
        {
            Assert.AreEqual(before.affixes[i].key, after.affixes[i].key);
            Assert.AreEqual(before.affixes[i].value, after.affixes[i].value);
        }
        Assert.AreNotEqual(before.instanceId, after.instanceId);
    }

    [Test]
    public void UniqueDrop_HasMaximumValuesInItsBracket()
    {
        var roll = new LootRoll { drops = true, slot = ItemSlot.Helmet, rarity = ItemRarity.Unique, affixTier = 2 };
        var item = LootDrop.BuildItem(roll, new System.Random(0), 10);
        Assert.AreEqual("Seer's Circlet", item.uniqueId);
        Assert.IsNotEmpty(item.instanceId);
        foreach (var affix in item.affixes)
            Assert.AreEqual(AffixLibrary.GetRange(affix.key, 10, 2).max, affix.value);
    }

    [Test]
    public void Table_HasAtLeastTwelveUniqueAffixes()
    {
        Assert.GreaterOrEqual(AffixLibrary.All.Length, 12);
        var keys = new HashSet<string>();
        foreach (var d in AffixLibrary.All) Assert.IsTrue(keys.Add(d.key), $"duplicate {d.key}");
        foreach (var k in new[] { "LifeSteal", "GoldFind", "SkillRank", "CooldownReduction", "FireDamage", "Thorns", "MagicFind" })
            Assert.IsNotNull(AffixLibrary.Get(k), k);
    }

    [Test]
    public void Spans_AreOrderedAndTierTwoNotWorse()
    {
        foreach (var d in AffixLibrary.All)
        {
            Assert.LessOrEqual(d.t1Min, d.t1Max, d.key);
            Assert.LessOrEqual(d.t2Min, d.t2Max, d.key);
            Assert.GreaterOrEqual(d.t2Min, d.t1Min, d.key);
            Assert.GreaterOrEqual(d.t2Max, d.t1Max, d.key);
            Assert.Greater(d.slots.Length, 0, d.key);
        }
    }

    [Test]
    public void TierOne_KeepsP2Ranges()
    {
        void Span(string key, float min, float max)
        {
            var d = AffixLibrary.Get(key);
            Assert.AreEqual(min, d.t1Min, 0.0001f, key);
            Assert.AreEqual(max, d.t1Max, 0.0001f, key);
        }
        Span("Damage", 1, 5);
        Span("AttackSpeed", 5, 15);
        Span("Crit", 2, 10);
        Span("MaxHP", 5, 20);
        Span("Armor", 1, 3);
        Span("MoveSpeed", 0.1f, 0.3f);
        Span("XPMul", 5, 15);
    }

    [Test]
    public void Rolls_StayInsideEveryTierSpan()
    {
        var rng = new System.Random(7);
        foreach (var d in AffixLibrary.All)
            for (int tier = 1; tier <= 2; tier++)
            {
                d.Range(tier, out float min, out float max);
                for (int i = 0; i < 300; i++)
                {
                    float v = d.Roll(tier, rng);
                    Assert.GreaterOrEqual(v, min, $"{d.key} t{tier}");
                    Assert.LessOrEqual(v, max, $"{d.key} t{tier}");
                    if (d.integer) Assert.AreEqual(Mathf.Round(v), v, $"{d.key} integer");
                }
            }
    }

    [Test]
    public void Pools_CoverEverySlotAndKeepP2Keys()
    {
        CollectionAssert.IsSubsetOf(new[] { "Damage", "AttackSpeed", "Crit" }, AffixLibrary.PoolFor(ItemSlot.Weapon));
        CollectionAssert.IsSubsetOf(new[] { "MaxHP", "Armor", "MoveSpeed" }, AffixLibrary.PoolFor(ItemSlot.Armor));
        CollectionAssert.IsSubsetOf(new[] { "XPMul", "Crit", "MaxHP" }, AffixLibrary.PoolFor(ItemSlot.Helmet));
        int covered = 0;
        foreach (ItemSlot slot in new[] { ItemSlot.Weapon, ItemSlot.Armor, ItemSlot.Helmet })
            covered += AffixLibrary.PoolFor(slot).Count;
        Assert.GreaterOrEqual(covered, AffixLibrary.All.Length, "every affix rolls on at least one slot");
    }

    [Test]
    public void RareAndMagicCounts_AndUniqueHasNone()
    {
        var rng = new System.Random(11);
        for (int i = 0; i < 200; i++)
        {
            int rare = AffixLibrary.RollAffixes(ItemSlot.Weapon, ItemRarity.Rare, rng, 2).Count;
            Assert.That(rare, Is.InRange(3, 6));
            int magic = AffixLibrary.RollAffixes(ItemSlot.Armor, ItemRarity.Magic, rng).Count;
            Assert.That(magic, Is.InRange(1, 2));
        }
        Assert.AreEqual(0, AffixLibrary.RollAffixes(ItemSlot.Helmet, ItemRarity.Unique, rng).Count);
    }

    [Test]
    public void Equipment_AggregatesAcrossSlots()
    {
        var eq = Hero().GetComponent<Equipment>();
        eq.Equip(Item(ItemSlot.Armor, ("GoldFind", 20), ("MagicFind", 10)));
        eq.Equip(Item(ItemSlot.Helmet, ("GoldFind", 15), ("MagicFind", 5), ("SkillRank", 1)));
        eq.Equip(Item(ItemSlot.Weapon, ("Damage", 3), ("FireDamage", 2)));
        Assert.AreEqual(35f, eq.GoldFindPct(), 0.001f);
        Assert.AreEqual(15f, eq.MagicFindPct(), 0.001f);
        Assert.AreEqual(1, eq.SkillRankBonus());
        Assert.AreEqual(Equipment.BaseDamage + 5f, eq.GetDamage(), 0.001f, "FireDamage adds to weapon damage");
    }

    [Test]
    public void LifeSteal_HealsShareOfDamageAndIsCapped()
    {
        var hero = Hero();
        var eq = hero.GetComponent<Equipment>();
        var hp = hero.GetComponent<PlayerHealth>();
        hp.TakeDamage(10f);
        Assert.AreEqual(0f, Equipment.ApplyLifeSteal(hero.transform, 10), "no affix, no heal");
        eq.Equip(Item(ItemSlot.Weapon, ("LifeSteal", 10)));
        float before = hp.CurrentHP;
        Assert.AreEqual(2f, Equipment.ApplyLifeSteal(hero.transform, 20), 0.001f);
        Assert.AreEqual(before + 2f, hp.CurrentHP, 0.001f);
        eq.Equip(Item(ItemSlot.Weapon, ("LifeSteal", 90)));
        Assert.AreEqual(Equipment.MaxLifeStealPct, eq.LifeStealPct(), 0.001f);
    }

    [Test]
    public void SkillRank_RaisesSkillDamageMultiplier()
    {
        var sheet = new CharacterSheet { fireballRank = 2 };
        Assert.AreEqual(2, sheet.EffectiveRank(DiabloSkills.Skill.Fireball));
        Assert.AreEqual(3, sheet.EffectiveRank(DiabloSkills.Skill.Fireball, 1));
        Assert.AreEqual(1f + 3 * CharacterSheet.RankDamageBonus,
            sheet.RankMultiplier(DiabloSkills.Skill.Fireball, 1), 0.001f);
        Assert.AreEqual(1f + 1 * CharacterSheet.RankDamageBonus,
            sheet.RankMultiplier(DiabloSkills.Skill.Arrow, 1), 0.001f, "+1 to all skills also lifts unranked skills");
    }

    [Test]
    public void CooldownReduction_ScalesAndCaps()
    {
        var eq = Hero().GetComponent<Equipment>();
        Assert.AreEqual(1f, eq.CooldownFactor(), 0.0001f);
        eq.Equip(Item(ItemSlot.Helmet, ("CooldownReduction", 10)));
        Assert.AreEqual(0.9f, eq.CooldownFactor(), 0.0001f);
        eq.Equip(Item(ItemSlot.Helmet, ("CooldownReduction", 80)));
        Assert.AreEqual(1f - Equipment.MaxCooldownReductionPct / 100f, eq.CooldownFactor(), 0.0001f);
    }

    [Test]
    public void GoldFind_ScalesPickups()
    {
        Assert.AreEqual(10, Equipment.ApplyGoldFind(10, 0f));
        Assert.AreEqual(15, Equipment.ApplyGoldFind(10, 50f));
        Assert.AreEqual(3, Equipment.ApplyGoldFind(3, 10f), "rounding never drops below the base");
    }

    [Test]
    public void Thorns_SumFromArmor()
    {
        var eq = Hero().GetComponent<Equipment>();
        eq.Equip(Item(ItemSlot.Armor, ("Thorns", 2), ("Thorns", 1)));
        Assert.AreEqual(3, eq.ThornsDamage());
    }

    [Test]
    public void MagicFind_ShiftsWeightsAndZeroIsP2Table()
    {
        LootTable.WeightsWithMagicFind(LootTier.Normal, 0f, out float m0, out float r0, out float u0);
        Assert.AreEqual(100f, m0 + r0 + u0, 0.001f, "mf 0 keeps the pinned 100-sum table");
        LootTable.WeightsWithMagicFind(LootTier.Normal, 100f, out float m1, out float r1, out float u1);
        Assert.AreEqual(m0, m1, 0.001f);
        Assert.AreEqual(r0 * 1.5f, r1, 0.001f);
        Assert.AreEqual(u0 * 2f, u1, 0.001f);

        int Uniques(float mf)
        {
            var rng = new System.Random(3);
            int n = 0;
            for (int i = 0; i < 4000; i++)
            {
                var roll = LootTable.Roll(LootTier.Elite, rng, mf);
                if (roll.drops && roll.rarity == ItemRarity.Unique) n++;
            }
            return n;
        }
        Assert.Greater(Uniques(100f), Uniques(0f) * 1.4f);
    }

    [Test]
    public void CompareSummary_ColorsDeltasAgainstEquipped()
    {
        var equipped = Item(ItemSlot.Weapon, ("Damage", 3), ("Crit", 5));
        var candidate = Item(ItemSlot.Weapon, ("Damage", 5), ("LifeSteal", 4));
        string text = DiabloInventoryUI.CompareSummary(candidate, equipped);
        StringAssert.Contains("+5 Damage <color=" + DiabloInventoryUI.BetterColor + ">(+2)</color>", text);
        StringAssert.Contains("4% Life Steal <color=" + DiabloInventoryUI.BetterColor + ">(+4)</color>", text);
        StringAssert.Contains("+0% Crit <color=" + DiabloInventoryUI.WorseColor + ">(-5)</color>", text);
        StringAssert.DoesNotContain("<color", DiabloInventoryUI.CompareSummary(equipped, equipped));
        Assert.AreEqual("no bonuses", DiabloInventoryUI.CompareSummary(Item(ItemSlot.Helmet), null));
    }
}
