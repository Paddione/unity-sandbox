using NUnit.Framework;
using UnityEngine;

/// <summary>
/// smithing S1: Verwertung und Umschmieden (Transaktionen, Validierung,
/// Rollback). Speichern ist per <see cref="SmithingService.SaveHook"/>
/// entkoppelt (default: Erfolg ohne Datei-IO); Roundtrip steht in den
/// Save- und PlayMode-Tests.
/// </summary>
public class SmithingServiceTests
{
    GameObject hero;
    Inventory inv;
    Wallet wallet;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        SmithingService.ResetSession();
        SmithingService.SaveHook = _ => true;
        hero = new GameObject("Player");
        inv = hero.AddComponent<Inventory>();
        wallet = hero.AddComponent<Wallet>();
        wallet.Add(1000);
    }

    [TearDown]
    public void Teardown()
    {
        if (hero != null) Object.DestroyImmediate(hero);
        SmithingService.SaveHook = null;
        SmithingService.ResetSession();
        Wallet.ResetRun();
        RunProfile.Reset();
    }

    static ItemInstance MagicWeapon()
    {
        return new ItemInstance
        {
            slot = ItemSlot.Weapon,
            rarity = ItemRarity.Magic,
            baseType = "ShortSword",
            affixTier = 1,
            affixes = new System.Collections.Generic.List<Affix>
            {
                new Affix(AffixLibrary.Damage, 3f),
                new Affix(AffixLibrary.AttackSpeed, 10f),
            },
        };
    }

    static ItemInstance RareArmor()
    {
        return new ItemInstance
        {
            slot = ItemSlot.Armor,
            rarity = ItemRarity.Rare,
            baseType = "LeatherVest",
            affixTier = 1,
            affixes = new System.Collections.Generic.List<Affix>
            {
                new Affix(AffixLibrary.MaxHP, 10f),
                new Affix(AffixLibrary.Armor, 2f),
                new Affix(AffixLibrary.GoldFind, 15f),
            },
        };
    }

    // ---------- Salvage ----------

    [Test]
    public void Salvage_MagicGrantsOneMaterialAndRemovesItem()
    {
        var item = MagicWeapon();
        Assert.IsTrue(inv.Add(item));
        Assert.IsTrue(SmithingService.TrySalvage(hero, 0, item));
        Assert.AreEqual(0, inv.Count);
        Assert.AreEqual(1, RunProfile.SmithingMaterial);
    }

    [Test]
    public void Salvage_RareGrantsThreeMaterial()
    {
        var item = RareArmor();
        Assert.IsTrue(inv.Add(item));
        Assert.IsTrue(SmithingService.TrySalvage(hero, 0, item));
        Assert.AreEqual(3, RunProfile.SmithingMaterial);
    }

    [Test]
    public void Salvage_UniqueRejected()
    {
        var unique = ItemDatabase.CreateUnique("Emberbrand Blade");
        Assert.IsTrue(inv.Add(unique));
        Assert.IsFalse(SmithingService.TrySalvage(hero, 0, unique));
        Assert.AreEqual(1, inv.Count);
        Assert.AreEqual(0, RunProfile.SmithingMaterial);
    }

    [Test]
    public void Salvage_StaleExpectedRejected()
    {
        var item = MagicWeapon();
        Assert.IsTrue(inv.Add(item));
        var stale = new ItemInstance
        {
            slot = item.slot, rarity = item.rarity, baseType = item.baseType,
            affixes = item.affixes, affixTier = 1,
        };
        Assert.IsFalse(SmithingService.TrySalvage(hero, 0, stale));
        Assert.AreEqual(1, inv.Count);
    }

    [Test]
    public void Salvage_DoubleConfirmGrantsOnlyOnce()
    {
        var item = MagicWeapon();
        Assert.IsTrue(inv.Add(item));
        Assert.IsTrue(SmithingService.TrySalvage(hero, 0, item));
        Assert.IsFalse(SmithingService.TrySalvage(hero, 0, item));
        Assert.AreEqual(1, RunProfile.SmithingMaterial);
    }

    [Test]
    public void Salvage_FailedSaveRestoresAll()
    {
        var item = MagicWeapon();
        Assert.IsTrue(inv.Add(item));
        SmithingService.SaveHook = _ => false;
        Assert.IsFalse(SmithingService.TrySalvage(hero, 0, item));
        Assert.AreEqual(1, inv.Count);
        Assert.IsTrue(ReferenceEquals(item, inv.Get(0)));
        Assert.AreEqual(0, RunProfile.SmithingMaterial);
        Assert.AreEqual(1000, wallet.Gold);
    }

    // ---------- Reforge ----------

    void FundMaterial()
    {
        RunProfile.SmithingMaterial = SmithingService.ReforgeMaterialCost;
    }

    [Test]
    public void Reforge_ReplacesOneAffixKeepsOthers()
    {
        var item = RareArmor();
        Assert.IsTrue(inv.Add(item));
        FundMaterial();
        float other0 = item.GetAffix(AffixLibrary.MaxHP);
        float other2 = item.GetAffix(AffixLibrary.GoldFind);
        Assert.IsTrue(SmithingService.PreviewReforge(hero, 0, item, 1, AffixLibrary.Thorns,
            out float min, out float max));
        Assert.IsTrue(SmithingService.TryReforge(hero, 0, item, 1, AffixLibrary.Thorns));
        Assert.AreEqual(AffixLibrary.Thorns, item.affixes[1].key);
        Assert.GreaterOrEqual(item.affixes[1].value, min);
        Assert.LessOrEqual(item.affixes[1].value, max);
        Assert.AreEqual(other0, item.GetAffix(AffixLibrary.MaxHP), 0.001f);
        Assert.AreEqual(other2, item.GetAffix(AffixLibrary.GoldFind), 0.001f);
        Assert.AreEqual(2, item.reforgeIndexPlusOne);
        SmithingService.ReforgeCost(out int mat, out int gold);
        Assert.AreEqual(1000 - gold, wallet.Gold);
        Assert.AreEqual(0, RunProfile.SmithingMaterial);
    }

    [Test]
    public void Reforge_BoundSlotRejectsEverySecondAttempt()
    {
        var item = RareArmor();
        Assert.IsTrue(inv.Add(item));
        FundMaterial();
        Assert.IsTrue(SmithingService.TryReforge(hero, 0, item, 1, AffixLibrary.Thorns));
        RunProfile.SmithingMaterial = SmithingService.ReforgeMaterialCost;
        Assert.IsFalse(SmithingService.TryReforge(hero, 0, item, 1, AffixLibrary.MoveSpeed));
        Assert.IsFalse(SmithingService.TryReforge(hero, 0, item, 0, AffixLibrary.MoveSpeed));
        Assert.AreEqual(AffixLibrary.Thorns, item.affixes[1].key);
    }

    [Test]
    public void Reforge_DuplicateKeyRejected()
    {
        var item = RareArmor();
        Assert.IsTrue(inv.Add(item));
        FundMaterial();
        Assert.IsFalse(SmithingService.TryReforge(hero, 0, item, 1, AffixLibrary.MaxHP));
        Assert.AreEqual(AffixLibrary.Armor, item.affixes[1].key);
        Assert.AreEqual(0, item.reforgeIndexPlusOne);
    }

    [Test]
    public void Reforge_WrongSlotKeyRejected()
    {
        var item = RareArmor();
        Assert.IsTrue(inv.Add(item));
        FundMaterial();
        Assert.IsFalse(SmithingService.TryReforge(hero, 0, item, 1, AffixLibrary.Damage));
    }

    [Test]
    public void Reforge_UniqueRejected()
    {
        var unique = ItemDatabase.CreateUnique("Emberbrand Blade");
        Assert.IsTrue(inv.Add(unique));
        FundMaterial();
        Assert.IsFalse(SmithingService.TryReforge(hero, 0, unique, 0, AffixLibrary.FireDamage));
    }

    [Test]
    public void Reforge_InsufficientFundsRejected()
    {
        var item = RareArmor();
        Assert.IsTrue(inv.Add(item));
        RunProfile.SmithingMaterial = 0;
        Assert.IsFalse(SmithingService.TryReforge(hero, 0, item, 1, AffixLibrary.Thorns));
        FundMaterial();
        wallet.SetGold(10);
        Assert.IsFalse(SmithingService.TryReforge(hero, 0, item, 1, AffixLibrary.Thorns));
        Assert.AreEqual(AffixLibrary.Armor, item.affixes[1].key);
    }

    [Test]
    public void Reforge_StaleOfferRejected()
    {
        var item = RareArmor();
        Assert.IsTrue(inv.Add(item));
        FundMaterial();
        var stale = new ItemInstance
        {
            slot = item.slot, rarity = item.rarity, baseType = item.baseType,
            affixes = item.affixes, affixTier = 1,
        };
        Assert.IsFalse(SmithingService.TryReforge(hero, 0, stale, 1, AffixLibrary.Thorns));
        Assert.AreEqual(AffixLibrary.Armor, item.affixes[1].key);
        Assert.AreEqual(SmithingService.ReforgeMaterialCost, RunProfile.SmithingMaterial);
    }

    [Test]
    public void Reforge_FailedSaveRestoresAllState()
    {
        var item = RareArmor();
        Assert.IsTrue(inv.Add(item));
        FundMaterial();
        SmithingService.SaveHook = _ => { throw new System.Exception("disk full"); };
        Assert.IsFalse(SmithingService.TryReforge(hero, 0, item, 1, AffixLibrary.Thorns));
        Assert.AreEqual(AffixLibrary.Armor, item.affixes[1].key);
        Assert.AreEqual(2f, item.affixes[1].value, 0.001f);
        Assert.AreEqual(0, item.reforgeIndexPlusOne);
        Assert.AreEqual(1000, wallet.Gold);
        Assert.AreEqual(SmithingService.ReforgeMaterialCost, RunProfile.SmithingMaterial);
    }

    [Test]
    public void Reforge_PreviewsAndRollsWithinStoredLevelBracket()
    {
        var item = RareArmor();
        item.itemLevel = 10;
        item.affixTier = 2;
        Assert.IsTrue(inv.Add(item));
        RunProfile.SmithingMaterial = SmithingService.ReforgeMaterialCost;
        Assert.IsTrue(SmithingService.PreviewReforge(hero, 0, item, 1, AffixLibrary.Thorns, out float min, out float max));
        Assert.AreEqual(3f, min);
        Assert.AreEqual(6f, max);
        Assert.IsTrue(SmithingService.TryReforge(hero, 0, item, 1, AffixLibrary.Thorns));
        Assert.That(item.affixes[1].value, Is.InRange(min, max));
        Assert.AreEqual(10, item.itemLevel);
    }

    [Test]
    public void Reforge_TierTwoRollsInKindledSpan()
    {
        var item = RareArmor();
        item.affixTier = 2;
        Assert.IsTrue(inv.Add(item));
        FundMaterial();
        var def = AffixLibrary.Get(AffixLibrary.Thorns);
        Assert.IsTrue(SmithingService.TryReforge(hero, 0, item, 1, AffixLibrary.Thorns));
        Assert.GreaterOrEqual(item.affixes[1].value, def.t2Min);
        Assert.LessOrEqual(item.affixes[1].value, def.t2Max);
    }

    [Test]
    public void Candidates_ExcludeOccupiedKeys()
    {
        var item = RareArmor();
        var cands = SmithingService.ReplacementCandidates(item);
        Assert.IsFalse(cands.Contains(AffixLibrary.MaxHP));
        Assert.IsFalse(cands.Contains(AffixLibrary.Armor));
        Assert.IsTrue(cands.Contains(AffixLibrary.Thorns));
        foreach (var key in cands)
            Assert.IsTrue(AffixLibrary.Get(key).AllowedOn(ItemSlot.Armor), key);
    }
}
