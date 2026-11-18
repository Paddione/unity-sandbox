using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// risk-shrines S2 loop: heal path, challenge path, reload before the kill,
/// hero death keeping the challenge, full bag holding the claim, double
/// activation, and a new run reopening the choice.
/// </summary>
public class RiskShrineLoopTests
{
    GameObject player;
    GameObject shrineGo;
    RiskShrine shrine;

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
        RunProfile.Reset();
        RunProfile.RunSeed = 4242;
        DiabloClickController.ExternalDrive = false;

        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(6.5f, 1.05f, 7.5f);
        player.AddComponent<CharacterController>();
        player.AddComponent<DiabloProgression>();
        var eq = player.AddComponent<Equipment>();
        player.AddComponent<Inventory>();
        player.AddComponent<Wallet>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<DiabloClickController>();
        eq.Equip(ItemDatabase.CreateStarterSword());

        var listener = new GameObject("TestAudioListener");
        listener.AddComponent<AudioListener>();

        shrineGo = new GameObject(RiskShrine.ShrineObjectName);
        shrineGo.transform.position = RiskShrine.ShrineAnchor;
        var col = shrineGo.AddComponent<BoxCollider>();
        col.size = new Vector3(1.2f, 1.6f, 1.2f);
        shrine = shrineGo.AddComponent<RiskShrine>();
    }

    [TearDown]
    public void TearDown()
    {
        DiabloTestFixture.CleanupTestScene(player);
        if (shrineGo != null) Object.DestroyImmediate(shrineGo);
        var elite = GameObject.Find(RiskShrine.EliteName);
        if (elite != null) Object.DestroyImmediate(elite);
        RunProfile.Reset();
        DiabloClickController.ExternalDrive = false;
        GoldDrop.ResetRun();
        Wallet.ResetRun();
        DiabloProgression.ResetRun();
        EnemyHealth.ResetRun();
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    static void KillElite()
    {
        var elite = GameObject.Find(RiskShrine.EliteName);
        Assert.IsNotNull(elite, "challenge elite must be spawned");
        elite.GetComponent<EnemyHealth>().TakeDamage(9999);
    }

    [UnityTest]
    public IEnumerator HealPath_RestoresToMax_AndLocksChoice()
    {
        var health = player.GetComponent<PlayerHealth>();
        health.TakeDamage(health.MaxHP / 2f);
        Assert.Less(health.CurrentHP, health.MaxHP);
        Assert.IsTrue(shrine.ChooseHeal(player));
        Assert.AreEqual(health.MaxHP, health.CurrentHP, 0.001f);
        Assert.AreEqual(RiskShrineState.Healed, RunProfile.RiskShrine.State);
        Assert.IsFalse(shrine.ChooseChallenge(player), "choice is one-shot");
        Assert.IsFalse(shrine.ChooseHeal(player), "double activation is a no-op");
        yield return null;
    }

    [UnityTest]
    public IEnumerator ChallengePath_KillThenClaim_PersistsClaimed()
    {
        Assert.IsTrue(shrine.ChooseChallenge(player));
        Assert.AreEqual(RiskShrineState.ChallengeActive, RunProfile.RiskShrine.State);
        var elite = GameObject.Find(RiskShrine.EliteName);
        Assert.IsNotNull(elite);
        Assert.AreEqual(RiskShrine.EliteHP,
            Mathf.CeilToInt(RiskShrine.EliteBaseHP * RiskShrine.EliteHpBonus));
        KillElite();
        yield return null; // Destroy applies at frame end
        Assert.AreEqual(RiskShrineState.RewardAvailable, RunProfile.RiskShrine.State);
        Assert.IsTrue(shrine.ClaimReward(player), "first pickup takes the rare");
        Assert.AreEqual(RiskShrineState.RewardClaimed, RunProfile.RiskShrine.State);
        Assert.AreEqual(1, player.GetComponent<Inventory>().Count);
        Assert.IsFalse(shrine.ClaimReward(player), "double-claim is a no-op");
        Assert.IsFalse(shrine.ChooseHeal(player));
        yield return null;
    }

    [UnityTest]
    public IEnumerator ReloadBeforeKill_RespawnsElite_AndStillPaysOut()
    {
        Assert.IsTrue(shrine.ChooseChallenge(player));
        var elite = GameObject.Find(RiskShrine.EliteName);
        Object.DestroyImmediate(elite); // scene torn down, save reloaded
        SaveService.SaveNow(player);
        RunProfile.Reset();
        Assert.IsTrue(SaveService.TryLoad(player));
        Assert.AreEqual(RiskShrineState.ChallengeActive, RunProfile.RiskShrine.State);
        shrine.EnsureChallengeSpawn();
        Assert.IsNotNull(GameObject.Find(RiskShrine.EliteName), "reload must respawn the active challenge");
        KillElite();
        yield return null;
        Assert.AreEqual(RiskShrineState.RewardAvailable, RunProfile.RiskShrine.State);
        Assert.IsTrue(shrine.ClaimReward(player));
        yield return null;
    }

    [UnityTest]
    public IEnumerator HeroDeath_KeepsChallenge()
    {
        Assert.IsTrue(shrine.ChooseChallenge(player));
        player.GetComponent<PlayerHealth>().DieAndRespawn();
        Assert.AreEqual(RiskShrineState.ChallengeActive, RunProfile.RiskShrine.State,
            "dying must not consume the challenge");
        Assert.IsNotNull(GameObject.Find(RiskShrine.EliteName));
        KillElite();
        yield return null;
        Assert.AreEqual(RiskShrineState.RewardAvailable, RunProfile.RiskShrine.State);
        yield return null;
    }

    [UnityTest]
    public IEnumerator FullBag_HoldsClaim_ThenClaimsAfterSpace()
    {
        var inv = player.GetComponent<Inventory>();
        for (int i = 0; i < Inventory.Capacity; i++)
            Assert.IsTrue(inv.Add(ItemDatabase.CreateStarterSword()));
        Assert.IsTrue(shrine.ChooseChallenge(player));
        KillElite();
        yield return null;
        Assert.AreEqual(RiskShrineState.RewardAvailable, RunProfile.RiskShrine.State);
        Assert.IsFalse(shrine.ClaimReward(player), "full bag keeps the claim open");
        Assert.IsTrue(inv.RemoveAt(0));
        Assert.IsTrue(shrine.ClaimReward(player), "freed slot lets the claim through");
        Assert.AreEqual(RiskShrineState.RewardClaimed, RunProfile.RiskShrine.State);
        yield return null;
    }

    [UnityTest]
    public IEnumerator NewRun_ReopensChoice()
    {
        Assert.IsTrue(shrine.ChooseHeal(player));
        RunProfile.Reset(); // New Game
        SaveService.DeleteSave();
        Assert.AreEqual(RiskShrineState.Unchosen, RunProfile.RiskShrine.State);
        Assert.IsTrue(shrine.ChooseChallenge(player), "a fresh run chooses again");
        yield return null;
    }
}
