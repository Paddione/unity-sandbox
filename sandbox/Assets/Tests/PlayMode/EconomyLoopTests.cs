using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>S2 economy loop: gold pickup balance, reload restores level/gold.</summary>
public class EconomyLoopTests
{
    GameObject player;
    string path;

    [SetUp]
    public void Setup()
    {
        DiabloJuice.ForceCrit = false;
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        GoldDrop.ResetRun();
        Wallet.ResetRun();
        DiabloProgression.ResetRun();

        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.AddComponent<CharacterController>();
        player.AddComponent<DiabloProgression>();
        player.AddComponent<Equipment>();
        player.AddComponent<Inventory>();
        player.AddComponent<Wallet>();
        // Earlier suites' kills drop gold piles their teardowns predate:
        // clear them so pickup asserts start from zero.
        foreach (var gold in GameObject.FindObjectsOfType<GoldDrop>())
            if (gold != null) Object.DestroyImmediate(gold.gameObject);
        path = Path.Combine(Path.GetTempPath(), $"cinderreach-test-{System.Guid.NewGuid():N}.json");
    }

    [TearDown]
    public void TearDown()
    {
        DiabloTestFixture.CleanupTestScene(player);
        var ambience = GameObject.Find("DiabloAmbience");
        if (ambience != null) Object.DestroyImmediate(ambience);
        foreach (var gold in GameObject.FindObjectsOfType<GoldDrop>())
            if (gold != null) Object.DestroyImmediate(gold.gameObject);
        GoldDrop.ResetRun();
        Wallet.ResetRun();
        DiabloProgression.ResetRun();
        SaveService.DeleteSave(path);
        if (File.Exists(path + ".corrupt")) File.Delete(path + ".corrupt");
        // Level-up autosave writes the default path: never leak it between tests.
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    [UnityTest]
    public IEnumerator GoldPickup_AddsPileAmountToWallet()
    {
        var wallet = player.GetComponent<Wallet>();
        Assert.AreEqual(0, wallet.Gold);
        var pile = GoldDrop.SpawnPile(new Vector3(0f, 0.5f, 0f), 7);
        Assert.IsNotNull(pile);
        // Walk the hero onto the pile until the trigger fires (proven pattern).
        DiabloTestFixture.TeleportHero(player, pile.transform.position);
        float wait = 0f;
        while (wallet.Gold == 0 && wait < 2f)
        {
            yield return null;
            wait += Time.deltaTime;
        }
        Assert.AreEqual(7, wallet.Gold, "Walking over a 7-gold pile must credit exactly 7.");
        Assert.AreEqual(7, Wallet.GoldEarned);
    }

    [UnityTest]
    public IEnumerator Kill_DropsGoldBesideItemOrb()
    {
        int pilesBefore = GoldDrop.PilesDropped;
        var rng = new System.Random(99);
        int goldSpawns = 0;
        const int kills = 40;
        for (int i = 0; i < kills; i++)
            if (GoldDrop.SpawnGold(Vector3.zero, LootTier.Normal, rng) != null) goldSpawns++;
        Assert.Greater(GoldDrop.PilesDropped - pilesBefore, 0, "40 normal kills must drop some gold.");
        Assert.That((float)goldSpawns / kills, Is.InRange(0.5f, 0.9f),
            "Normal gold rate over 40 rolls must sit near 70 %.");
        foreach (var gold in GameObject.FindObjectsOfType<GoldDrop>())
            Object.DestroyImmediate(gold.gameObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Reload_RestoresLevelAndGold()
    {
        var prog = player.GetComponent<DiabloProgression>();
        var wallet = player.GetComponent<Wallet>();
        prog.AddXp(DiabloProgression.XpToNext(1)); // exactly one level-up
        wallet.Add(123);
        int savedLevel = prog.Level;
        Assert.AreEqual(2, savedLevel);
        SaveService.SaveNow(player, zoneReached: 1, path: path);
        Assert.IsTrue(SaveService.HasSave(path));

        // Wipe the run state, then reload.
        prog.SetState(1, 0);
        wallet.SetGold(0);
        Assert.AreEqual(0, wallet.Gold);
        Assert.IsTrue(SaveService.TryLoad(player, path));
        prog.GetState(out int level, out int xp);
        Assert.AreEqual(savedLevel, level, "Reload must restore the saved level.");
        Assert.AreEqual(123, player.GetComponent<Wallet>().Gold, "Reload must restore the saved gold.");
        yield return null;
    }
}
