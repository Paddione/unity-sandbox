using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>S2 persist: SaveGame roundtrip, version/corrupt paths, wallet logic.</summary>
public class SaveGameTests
{
    string path;
    string corruptPath;
    GameObject hero;

    [SetUp]
    public void Setup()
    {
        path = Path.Combine(Path.GetTempPath(), $"cinderreach-test-{System.Guid.NewGuid():N}.json");
        corruptPath = path + ".corrupt";
    }

    [TearDown]
    public void Teardown()
    {
        if (hero != null) Object.DestroyImmediate(hero);
        Wallet.ResetRun();
        SaveService.DeleteSave(path);
        if (File.Exists(corruptPath)) File.Delete(corruptPath);
        if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
    }

    static GameObject HeroWithAll()
    {
        var go = new GameObject("Player");
        go.AddComponent<Equipment>();
        go.AddComponent<Inventory>();
        go.AddComponent<Wallet>();
        go.AddComponent<DiabloProgression>();
        return go;
    }

    [Test]
    public void Roundtrip_PreservesAllFields()
    {
        hero = HeroWithAll();
        var prog = hero.GetComponent<DiabloProgression>();
        prog.SetState(7, 123);
        hero.GetComponent<Wallet>().SetGold(456);
        var eq = hero.GetComponent<Equipment>();
        eq.Equip(ItemDatabase.CreateUnique("Emberbrand Blade"));
        var inv = hero.GetComponent<Inventory>();
        Assert.IsTrue(inv.Add(ItemDatabase.CreateStarterSword()));
        Assert.IsTrue(inv.Add(ItemDatabase.CreateUnique("Seer's Circlet")));

        var save = SaveService.CaptureState(hero, zoneReached: 2);
        save.freeAttributePoints = 3;
        save.strength = 2; save.vitality = 1; save.focus = 4;
        save.skillRanks.Add(new SkillRankEntry { skill = "Fireball", rank = 3 });
        save.skillRanks.Add(new SkillRankEntry { skill = "Arrow", rank = 1 });
        SaveService.WriteSave(save, path);

        var fresh = new GameObject("Player");
        Assert.IsTrue(SaveService.TryLoad(fresh, path));
        hero = fresh; // teardown destroys it
        var fprog = fresh.GetComponent<DiabloProgression>();
        fprog.GetState(out int level, out int xp);
        Assert.AreEqual(7, level);
        Assert.AreEqual(123, xp);
        Assert.AreEqual(456, fresh.GetComponent<Wallet>().Gold);
        var feq = fresh.GetComponent<Equipment>();
        Assert.AreEqual("Emberbrand Blade", feq.weapon.uniqueId);
        var finv = fresh.GetComponent<Inventory>();
        Assert.AreEqual(2, finv.Count);
        // Bag order is stable through the roundtrip.
        Assert.AreEqual(ItemSlot.Weapon, finv.Get(0).slot);
        Assert.AreEqual("Seer's Circlet", finv.Get(1).uniqueId);
        Assert.AreEqual(2, SaveService.LoadedZoneReached);
    }

    [Test]
    public void CorruptFile_BackupAndFreshStart()
    {
        hero = HeroWithAll();
        hero.GetComponent<Wallet>().SetGold(10);
        File.WriteAllText(path, "{ not valid json !!!");
        Assert.IsFalse(SaveService.TryLoad(hero, path));
        Assert.IsTrue(File.Exists(corruptPath), "Corrupt save must be backed up, never silently lost.");
        Assert.IsFalse(File.Exists(path), "Corrupt save must not stay in place.");
        // Hero untouched: fresh start, no partial apply.
        Assert.AreEqual(10, hero.GetComponent<Wallet>().Gold);
        hero.GetComponent<DiabloProgression>().GetState(out int level, out _);
        Assert.AreEqual(1, level);
    }

    [Test]
    public void VersionMismatch_TreatedAsCorrupt()
    {
        hero = HeroWithAll();
        File.WriteAllText(path, "{\"v\":999,\"level\":9,\"gold\":999}");
        Assert.IsFalse(SaveService.TryLoad(hero, path));
        Assert.IsTrue(File.Exists(corruptPath));
        hero.GetComponent<DiabloProgression>().GetState(out int level, out _);
        Assert.AreEqual(1, level, "Future-version save must not apply.");
    }

    [Test]
    public void MissingFile_LoadReturnsFalse()
    {
        hero = HeroWithAll();
        Assert.IsFalse(File.Exists(path));
        Assert.IsFalse(SaveService.TryLoad(hero, path));
    }

    [Test]
    public void Wallet_AddAndTrySpend()
    {
        hero = HeroWithAll();
        var wallet = hero.GetComponent<Wallet>();
        Assert.AreEqual(0, wallet.Gold);
        wallet.Add(0);
        wallet.Add(-5);
        Assert.AreEqual(0, wallet.Gold, "Non-positive adds are ignored.");
        wallet.Add(50);
        Assert.AreEqual(50, wallet.Gold);
        Assert.IsFalse(wallet.TrySpend(60), "Overspend must fail without deducting.");
        Assert.AreEqual(50, wallet.Gold);
        Assert.IsTrue(wallet.TrySpend(30));
        Assert.AreEqual(20, wallet.Gold);
        Assert.IsTrue(wallet.TrySpend(0), "Free costs always succeed.");
    }

    [Test]
    public void GoldTable_ConstantsSane()
    {
        foreach (LootTier tier in System.Enum.GetValues(typeof(LootTier)))
        {
            DroptableDatabase.GetGoldTable(tier, out int chance, out int min, out int max);
            Assert.That(chance, Is.InRange(0, 100), $"Tier {tier}: chance must be a percent.");
            Assert.Greater(max, 0, $"Tier {tier}: must drop gold.");
            Assert.LessOrEqual(min, max, $"Tier {tier}: bounds inverted.");
        }
        DroptableDatabase.GetGoldTable(LootTier.Normal, out int nc, out int nmin, out int nmax);
        Assert.AreEqual(70, nc);
        Assert.AreEqual(3, nmin);
        Assert.AreEqual(8, nmax);
        DroptableDatabase.GetGoldTable(LootTier.Elite, out int ec, out int emin, out int emax);
        Assert.AreEqual(100, ec);
        Assert.AreEqual(15, emin);
        Assert.AreEqual(30, emax);
        DroptableDatabase.GetGoldTable(LootTier.Chest, out int cc, out int cmin, out int cmax);
        Assert.AreEqual(100, cc);
        Assert.AreEqual(40, cmin);
        Assert.AreEqual(60, cmax);
    }

    [Test]
    public void GoldRolls_HonorChanceAndBounds()
    {
        var rng = new System.Random(2026);
        int drops = 0;
        const int rolls = 2000;
        for (int i = 0; i < rolls; i++)
        {
            if (GoldDrop.RollDrops(LootTier.Normal, rng)) drops++;
            int amount = GoldDrop.RollAmount(LootTier.Normal, rng);
            Assert.That(amount, Is.InRange(3, 8));
            Assert.That(GoldDrop.RollAmount(LootTier.Elite, rng), Is.InRange(15, 30));
            Assert.That(GoldDrop.RollAmount(LootTier.Chest, rng), Is.InRange(40, 60));
        }
        Assert.That((float)drops / rolls, Is.InRange(0.62f, 0.78f), "Normal gold rate must sit near 70 %.");
        for (int i = 0; i < 200; i++)
            Assert.IsTrue(GoldDrop.RollDrops(LootTier.Elite, rng), "Elite gold must always drop.");
    }
}
