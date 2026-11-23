using NUnit.Framework;
using UnityEngine;

/// <summary>replayability S4: Kindled profile, unlock/selection rules, loot and gold scaling.</summary>
public class DifficultyTests
{
    [SetUp]
    public void Setup() => RunProfile.Reset();

    [TearDown]
    public void Teardown() => RunProfile.Reset();

    [Test]
    public void Profiles_HaveThePlannedMultipliers()
    {
        var n = DifficultyProfile.Normal;
        Assert.AreEqual(1f, n.hpMul);
        Assert.AreEqual(1f, n.damageMul);
        Assert.AreEqual(1, n.affixTier);
        Assert.AreEqual(1f, n.uniqueMul);
        Assert.AreEqual(1f, n.goldMul);
        var k = DifficultyProfile.Kindled;
        Assert.AreEqual(1.8f, k.hpMul, 0.0001f);
        Assert.AreEqual(1.8f, k.damageMul, 0.0001f);
        Assert.AreEqual(2, k.affixTier);
        Assert.AreEqual(2f, k.uniqueMul, 0.0001f);
        Assert.AreEqual(1.5f, k.goldMul, 0.0001f);
    }

    [Test]
    public void Scaling_RoundsAndKeepsFloors()
    {
        var k = DifficultyProfile.Kindled;
        Assert.AreEqual(9, k.ScaleHP(5));
        Assert.AreEqual(270, k.ScaleHP(BossController.BossMaxHP));
        Assert.AreEqual(2, k.ScaleDamage(1));
        Assert.AreEqual(4, k.ScaleDamage(2));
        Assert.AreEqual(5, k.ScaleDamage(3));
        Assert.AreEqual(0, k.ScaleDamage(0));
        Assert.AreEqual(15, k.ScaleGold(10));
        Assert.AreEqual(0, k.ScaleGold(0));
        Assert.AreEqual(5, DifficultyProfile.Normal.ScaleHP(5));
        Assert.AreEqual(1, DifficultyProfile.Normal.ScaleDamage(1));
    }

    [Test]
    public void Kindled_IsLockedUntilUnlocked()
    {
        Assert.AreEqual(RunProfile.Normal, DifficultyProfile.Select(RunProfile.Kindled), "locked selection falls back");
        Assert.AreSame(DifficultyProfile.Normal, DifficultyProfile.Current);
        RunProfile.Difficulty = RunProfile.Kindled; // stale save value without unlock
        Assert.AreSame(DifficultyProfile.Normal, DifficultyProfile.Current);

        Assert.IsTrue(DifficultyProfile.UnlockKindled());
        Assert.IsFalse(DifficultyProfile.UnlockKindled(), "second kill unlocks nothing new");
        Assert.AreEqual(RunProfile.Kindled, DifficultyProfile.Select(RunProfile.Kindled));
        Assert.AreSame(DifficultyProfile.Kindled, DifficultyProfile.Current);
        Assert.AreEqual(RunProfile.Normal, DifficultyProfile.Select(RunProfile.Normal));
        Assert.AreSame(DifficultyProfile.Normal, DifficultyProfile.Current);
    }

    [Test]
    public void UniqueMultiplier_DoublesUniqueShare()
    {
        int Uniques(float mul)
        {
            var rng = new System.Random(5);
            int u = 0;
            for (int i = 0; i < 6000; i++)
            {
                var r = LootTable.Roll(LootTier.Elite, rng, 0f, mul);
                if (r.drops && r.rarity == ItemRarity.Unique) u++;
            }
            return u;
        }
        int normal = Uniques(1f), kindled = Uniques(2f);
        // Elite: 10/100 unique vs 20/110 ≈ 18.2 %.
        Assert.That(normal / 6000f, Is.InRange(0.08f, 0.12f));
        Assert.That(kindled / 6000f, Is.InRange(0.16f, 0.205f));
    }

    [Test]
    public void KindledDrops_RollTierTwoAffixes()
    {
        RunProfile.DifficultyUnlocked = true;
        RunProfile.Difficulty = RunProfile.Kindled;
        var rng = new System.Random(9);
        for (int i = 0; i < 50; i++)
        {
            var item = LootDrop.BuildItem(new LootRoll { drops = true, rarity = ItemRarity.Rare, slot = (ItemSlot)(i % 3) }, rng);
            foreach (var a in item.affixes)
            {
                AffixLibrary.Get(a.key).Range(2, out float min, out float max);
                Assert.That(a.value, Is.InRange(min, max), a.key);
            }
        }
    }

    [Test]
    public void DifficultyText_ShowsLockHint()
    {
        StringAssert.Contains("Entfacht gesperrt", DiabloInventoryUI.DifficultyText());
        DifficultyProfile.UnlockKindled();
        DifficultyProfile.Select(RunProfile.Kindled);
        Assert.AreEqual("Schwierigkeit: Entfacht", DiabloInventoryUI.DifficultyText());
    }
}
