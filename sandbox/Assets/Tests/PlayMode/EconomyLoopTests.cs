using System.Collections;
using System.IO;
using System.Linq;
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
        path = Path.Combine(Path.GetTempPath(), $"cinderreach-test-{System.Guid.NewGuid():N}.json");
    }

    [TearDown]
    public void TearDown()
    {
        DiabloTestFixture.CleanupTestScene(player);
        var ambience = GameObject.Find("DiabloAmbience");
        if (ambience != null) Object.DestroyImmediate(ambience);
        GoldDrop.ResetRun();
        Wallet.ResetRun();
        DiabloProgression.ResetRun();
        SaveService.DeleteSave(path);
        if (File.Exists(path + ".corrupt")) File.Delete(path + ".corrupt");
        // Level-up autosave writes the default path: never leak it between tests.
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    [UnityTest]
    public IEnumerator KillGold_CreditsWalletDirectly_NoGroundPile()
    {
        var wallet = player.GetComponent<Wallet>();
        Assert.AreEqual(0, wallet.Gold);
        Assert.IsTrue(GoldDrop.Credit(player, 7, Vector3.zero));
        Assert.AreEqual(7, wallet.Gold, "Kill gold must land in the purse without a pickup.");
        Assert.AreEqual(7, Wallet.GoldEarned);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Kill_AwardsGoldToKiller_AtNormalRate()
    {
        var wallet = player.GetComponent<Wallet>();
        var rng = new System.Random(99);
        int awards = 0;
        const int kills = 40;
        for (int i = 0; i < kills; i++)
            if (GoldDrop.AwardKillGold(Vector3.zero, LootTier.Normal, rng, player.transform) > 0) awards++;
        Assert.That((float)awards / kills, Is.InRange(0.5f, 0.9f),
            "Normal gold rate over 40 kills must sit near 70 %.");
        Assert.AreEqual(GoldDrop.AwardedRaw, wallet.Gold, "Every awarded coin lands in the killer's purse (no Gold Find gear).");
        Assert.AreEqual(0, Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
            .Count(c => c != null && c.name.StartsWith("GoldPile")), "No gold piles on the ground.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator KillGold_AppliesGoldFind()
    {
        var eq = player.GetComponent<Equipment>();
        int expected = Equipment.ApplyGoldFind(100, eq.GoldFindPct());
        GoldDrop.Credit(player, 100, Vector3.zero);
        Assert.AreEqual(expected, player.GetComponent<Wallet>().Gold);
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
