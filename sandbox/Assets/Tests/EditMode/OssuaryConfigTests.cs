using NUnit.Framework;
using UnityEngine;

/// <summary>S5 config: boss tuning, phase switch, guaranteed Rare+ boss loot.</summary>
public class OssuaryConfigTests
{
    [Test]
    public void Boss_TunedForLevelSix()
    {
        Assert.AreEqual(150, BossController.BossMaxHP);
        Assert.AreEqual("Hollow Abbot", BossController.BossName);
        Assert.AreEqual(120, DiabloProgression.XpFor(LootTier.Boss));
    }

    [Test]
    public void Boss_PhaseTwoAtHalfHp()
    {
        var go = new GameObject("Boss");
        var health = go.AddComponent<EnemyHealth>();
        health.SetTier(BossController.BossMaxHP, LootTier.Boss);
        var boss = go.AddComponent<BossController>();
        int before = BossController.PhaseTwoEntries;
        // Above half the Update gate stays shut; at exactly half it opens.
        health.TakeDamage(74); // 76/150
        Assert.Greater(health.HPFraction(), BossController.PhaseTwoFraction);
        Assert.AreEqual(before, BossController.PhaseTwoEntries);
        health.TakeDamage(1); // 75/150 = 50 %
        Assert.LessOrEqual(health.HPFraction(), BossController.PhaseTwoFraction);
        boss.EnterPhaseTwo();
        Assert.AreEqual(before + 1, BossController.PhaseTwoEntries);
        boss.EnterPhaseTwo(); // idempotent: summons only once
        Assert.AreEqual(before + 1, BossController.PhaseTwoEntries);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Boss_LootIsAlwaysRarePlus()
    {
        LootTable.WeightsFor(LootTier.Boss, out int magic, out int rare, out int unique);
        Assert.AreEqual(0, magic);
        Assert.Greater(rare + unique, 0);
        var rng = new System.Random(7);
        for (int i = 0; i < 50; i++)
        {
            var roll = LootTable.Roll(LootTier.Boss, rng);
            Assert.IsTrue(roll.drops);
            Assert.AreNotEqual(ItemRarity.Magic, roll.rarity);
        }
    }

    [Test]
    public void Boss_GoldPurseIs150To250()
    {
        DroptableDatabase.GetGoldTable(LootTier.Boss, out int chance, out int min, out int max);
        Assert.AreEqual(100, chance);
        Assert.AreEqual(150, min);
        Assert.AreEqual(250, max);
        var rng = new System.Random(7);
        for (int i = 0; i < 50; i++)
        {
            int amount = GoldDrop.RollAmount(LootTier.Boss, rng);
            Assert.GreaterOrEqual(amount, 150);
            Assert.LessOrEqual(amount, 250);
        }
    }

    [Test]
    public void Caster_HoldsSixToEightMeters()
    {
        Assert.AreEqual(6f, RangedCaster.HoldMin);
        Assert.AreEqual(8f, RangedCaster.HoldMax);
        Assert.Greater(RangedCaster.Telegraph, 0f);
    }
}
