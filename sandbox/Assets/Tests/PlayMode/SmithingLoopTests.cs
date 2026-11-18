using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// smithing S2: Spielpfad-Verträge (Salvage/Reforge, stale/insufficient,
/// Save-Roundtrip und Reload-Sperre) plus je ein Fund auf Normal und Kindled
/// bis zur Verbesserung. Speichern läuft über einen Temp-Pfad-Hook.
/// </summary>
public class SmithingLoopTests
{
    GameObject player;
    Inventory inv;
    Wallet wallet;
    string path;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        SmithingService.ResetSession();
        Wallet.ResetRun();
        path = Path.Combine(Path.GetTempPath(), $"smith-loop-{System.Guid.NewGuid():N}.json");
        SmithingService.SaveHook = hero => { SaveService.SaveNow(hero, -1, path); return true; };

        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 11f);
        player.AddComponent<DiabloProgression>();
        player.AddComponent<Equipment>();
        inv = player.AddComponent<Inventory>();
        wallet = player.AddComponent<Wallet>();
        wallet.Add(500);
    }

    [TearDown]
    public void TearDown()
    {
        SmithingService.SaveHook = null;
        SmithingService.ResetSession();
        Wallet.ResetRun();
        RunProfile.Reset();
        if (player != null) Object.DestroyImmediate(player);
        DiabloTestFixture.CleanupTestScene(null);
        foreach (var f in new[] { path, path + ".tmp", path + ".corrupt" })
            if (f != null && File.Exists(f)) File.Delete(f);
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    static ItemInstance Fund(ItemSlot slot, ItemRarity rarity, int tier)
    {
        return new ItemInstance
        {
            slot = slot,
            rarity = rarity,
            baseType = slot == ItemSlot.Weapon ? "ShortSword" : "LeatherVest",
            affixTier = tier,
            affixes = new List<Affix>
            {
                new Affix(slot == ItemSlot.Weapon ? AffixLibrary.AttackSpeed : AffixLibrary.MaxHP, 5f),
                new Affix(slot == ItemSlot.Weapon ? AffixLibrary.Crit : AffixLibrary.Armor, 2f),
            },
        };
    }

    static ItemInstance Fodder()
    {
        return new ItemInstance
        {
            slot = ItemSlot.Armor,
            rarity = ItemRarity.Magic,
            baseType = "ClothTunic",
            affixTier = 1,
            affixes = new List<Affix> { new Affix(AffixLibrary.Armor, 1f) },
        };
    }

    [UnityTest]
    public IEnumerator Salvage_RemovesItemAndGrantsMaterialOnce()
    {
        var item = Fodder();
        Assert.IsTrue(inv.Add(item));
        Assert.IsTrue(SmithingService.TrySalvage(player, 0, item));
        Assert.AreEqual(0, inv.Count);
        Assert.AreEqual(1, RunProfile.SmithingMaterial);
        Assert.IsFalse(SmithingService.TrySalvage(player, 0, item));
        Assert.AreEqual(1, RunProfile.SmithingMaterial);
        yield break;
    }

    [UnityTest]
    public IEnumerator Reforge_KeepsOtherAffixesAndLocksSlotAfterReload()
    {
        var item = Fund(ItemSlot.Weapon, ItemRarity.Magic, 1);
        Assert.IsTrue(inv.Add(item));
        RunProfile.SmithingMaterial = SmithingService.ReforgeMaterialCost;

        string keepKey = item.affixes[1].key;
        float keepValue = item.affixes[1].value;
        Assert.IsTrue(SmithingService.TryReforge(player, 0, item, 0, AffixLibrary.Damage));
        Assert.AreEqual(keepKey, item.affixes[1].key);
        Assert.AreEqual(keepValue, item.affixes[1].value, 0.001f);
        Assert.AreEqual(1, item.reforgeIndexPlusOne);
        Assert.IsTrue(File.Exists(path), "Reforge speichert Wallet+Inventar+Material in einem Snapshot.");

        var fresh = new GameObject("Player2");
        fresh.AddComponent<DiabloProgression>();
        fresh.AddComponent<Equipment>();
        fresh.AddComponent<Inventory>();
        fresh.AddComponent<Wallet>();
        try
        {
            Assert.IsTrue(SaveService.TryLoad(fresh, path));
            var loaded = fresh.GetComponent<Inventory>().Get(0);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(1, loaded.reforgeIndexPlusOne);
            Assert.AreEqual(item.affixes[0].key, loaded.affixes[0].key);
            Assert.AreEqual(item.affixes[0].value, loaded.affixes[0].value, 0.001f);
            // Slot bleibt nach Reload gebunden.
            Assert.IsFalse(SmithingService.TryReforge(fresh, 0, loaded, 1, AffixLibrary.FireDamage));
            Assert.IsFalse(SmithingService.TryReforge(fresh, 0, loaded, 0, AffixLibrary.FireDamage));
        }
        finally { Object.DestroyImmediate(fresh); }
        yield break;
    }

    [UnityTest]
    public IEnumerator Reforge_RejectsStaleOfferAndInsufficientFunds()
    {
        var item = Fund(ItemSlot.Armor, ItemRarity.Rare, 1);
        Assert.IsTrue(inv.Add(item));
        RunProfile.SmithingMaterial = 0;
        Assert.IsFalse(SmithingService.TryReforge(player, 0, item, 0, AffixLibrary.Thorns));
        RunProfile.SmithingMaterial = SmithingService.ReforgeMaterialCost;
        wallet.SetGold(10);
        Assert.IsFalse(SmithingService.TryReforge(player, 0, item, 0, AffixLibrary.Thorns));
        wallet.SetGold(500);
        var stale = new ItemInstance
        {
            slot = item.slot, rarity = item.rarity, baseType = item.baseType,
            affixes = item.affixes, affixTier = 1,
        };
        Assert.IsFalse(SmithingService.TryReforge(player, 0, stale, 0, AffixLibrary.Thorns));
        Assert.AreEqual(0, item.reforgeIndexPlusOne);
        yield break;
    }

    [UnityTest]
    public IEnumerator Reforge_FailedSaveRestoresAllState()
    {
        var item = Fund(ItemSlot.Armor, ItemRarity.Rare, 1);
        Assert.IsTrue(inv.Add(item));
        RunProfile.SmithingMaterial = SmithingService.ReforgeMaterialCost;
        SmithingService.SaveHook = _ => false;
        Assert.IsFalse(SmithingService.TryReforge(player, 0, item, 0, AffixLibrary.Thorns));
        Assert.AreEqual(AffixLibrary.MaxHP, item.affixes[0].key);
        Assert.AreEqual(5f, item.affixes[0].value, 0.001f);
        Assert.AreEqual(0, item.reforgeIndexPlusOne);
        Assert.AreEqual(500, wallet.Gold);
        Assert.AreEqual(SmithingService.ReforgeMaterialCost, RunProfile.SmithingMaterial);
        Assert.IsTrue(ReferenceEquals(item, inv.Get(0)));
        yield break;
    }

    [UnityTest]
    public IEnumerator NormalFund_PlayedThroughToImprovement()
    {
        RunProfile.Difficulty = RunProfile.Normal;
        var rng = new System.Random(7);
        var roll = LootTable.Roll(LootTier.Elite, rng);
        Assert.IsTrue(roll.drops);
        var fund = LootDrop.BuildItem(roll, rng);
        if (fund.rarity == ItemRarity.Unique || fund.affixes.Count == 0)
        {
            fund = Fund(ItemSlot.Weapon, ItemRarity.Magic, 1);
        }
        Assert.AreEqual(1, fund.affixTier);
        Assert.IsTrue(inv.Add(fund));
        int need = SmithingService.ReforgeMaterialCost;
        while (RunProfile.SmithingMaterial < need)
        {
            var f = Fodder();
            Assert.IsTrue(inv.Add(f));
            Assert.IsTrue(SmithingService.TrySalvage(player, inv.Count - 1, f));
        }
        var cands = SmithingService.ReplacementCandidates(fund);
        Assert.IsNotEmpty(cands);
        string before = fund.affixes[0].key;
        Assert.IsTrue(SmithingService.PreviewReforge(player, 0, fund, 0, cands[0],
            out float min, out float max));
        Assert.IsTrue(SmithingService.TryReforge(player, 0, fund, 0, cands[0]));
        Assert.AreNotEqual(before, fund.affixes[0].key);
        Assert.GreaterOrEqual(fund.affixes[0].value, min);
        Assert.LessOrEqual(fund.affixes[0].value, max);
        Assert.IsTrue(File.Exists(path));
        yield break;
    }

    [UnityTest]
    public IEnumerator KindledFund_PlayedThroughToImprovement()
    {
        RunProfile.DifficultyUnlocked = true;
        RunProfile.Difficulty = RunProfile.Kindled;
        Assert.AreEqual(2, DifficultyProfile.Current.affixTier);
        var rng = new System.Random(21);
        var roll = LootTable.Roll(LootTier.Elite, rng);
        var fund = LootDrop.BuildItem(roll, rng);
        if (fund.rarity == ItemRarity.Unique || fund.affixes.Count == 0)
        {
            fund = Fund(ItemSlot.Weapon, ItemRarity.Magic, 2);
        }
        Assert.AreEqual(2, fund.affixTier);
        Assert.IsTrue(inv.Add(fund));
        RunProfile.SmithingMaterial = SmithingService.ReforgeMaterialCost;
        var cands = SmithingService.ReplacementCandidates(fund);
        Assert.IsNotEmpty(cands);
        var def = AffixLibrary.Get(cands[0]);
        Assert.IsTrue(SmithingService.TryReforge(player, 0, fund, 0, cands[0]));
        Assert.AreEqual(cands[0], fund.affixes[0].key);
        Assert.GreaterOrEqual(fund.affixes[0].value, def.t2Min);
        Assert.LessOrEqual(fund.affixes[0].value, def.t2Max);
        yield break;
    }

    [UnityTest]
    public IEnumerator Station_OpensSmithUIWithSameReachAsVendor()
    {
        Assert.IsTrue(SmithStation.CanInteract(
            new Vector3(0f, 0f, 11f), new Vector3(1f, 0f, 11f)));
        Assert.IsFalse(SmithStation.CanInteract(
            new Vector3(0f, 0f, 11f), new Vector3(10f, 0f, 11f)));
        var ui = player.GetComponent<DiabloSmithUI>();
        if (ui == null) ui = player.AddComponent<DiabloSmithUI>();
        ui.SetOpen(true);
        Assert.IsTrue(ui.IsOpen);
        ui.SetOpen(false);
        Assert.IsFalse(ui.IsOpen);
        yield break;
    }
}
