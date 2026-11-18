using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>zone-contracts S2: full loop — accept, objective, return, claim — plus reload and write-failure guards.</summary>
public class ContractLoopTests
{
    GameObject hero;
    GameObject boardGo;
    ContractBoard board;
    string path;

    static ContractOffer HuntOffer(string id, int gold) => new ContractOffer
    {
        id = id, type = ContractType.Hunt, zone = ActProgress.Town,
        position = new Vector3(5f, 0.05f, 2f), gold = gold, targetName = "Ash Elite",
    };

    static ContractOffer RelicOffer(string id, int gold) => new ContractOffer
    {
        id = id, type = ContractType.Recovery, zone = ActProgress.Town,
        position = new Vector3(-4f, 0.05f, 2f), gold = gold, targetName = "Fen Cache",
    };

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        ContractProgress.Reset();
        Wallet.ResetRun();
        ContractBoard.WriteHook = null;
        path = Path.Combine(Path.GetTempPath(), $"cinderreach-contract-{System.Guid.NewGuid():N}.json");
        hero = new GameObject("Player");
        hero.AddComponent<Equipment>();
        hero.AddComponent<Inventory>();
        hero.AddComponent<Wallet>();
        hero.AddComponent<DiabloProgression>();
        DiabloTestFixture.TeleportHero(hero, Vector3.zero);
        boardGo = new GameObject(ContractBoard.BoardObjectName);
        boardGo.transform.position = new Vector3(0f, 0f, 2f);
        board = boardGo.AddComponent<ContractBoard>();
    }

    [TearDown]
    public void TearDown()
    {
        ContractBoard.WriteHook = null;
        foreach (var o in GameObject.FindObjectsOfType<ContractObjective>())
            Object.DestroyImmediate(o.gameObject);
        if (boardGo != null) Object.DestroyImmediate(boardGo);
        if (hero != null) Object.DestroyImmediate(hero);
        Wallet.ResetRun();
        RunProfile.Reset();
        ContractProgress.Reset();
        foreach (var f in new[] { path, path + ".tmp", path + ".corrupt" })
            if (File.Exists(f)) File.Delete(f);
    }

    static void TeleportNear(GameObject hero, Vector3 target)
    {
        DiabloTestFixture.TeleportHero(hero, target + new Vector3(1f, 0f, 0f));
    }

    [UnityTest]
    public IEnumerator Hunt_KillReturnClaim()
    {
        var offer = HuntOffer("contract-1-ash-elite", 130);
        Assert.IsTrue(ContractProgress.TryAccept(offer));
        var objective = ContractObjective.SpawnFor(offer);
        Assert.IsNotNull(objective);
        // Elite dies at the frozen target position.
        TeleportNear(hero, offer.position);
        objective.OnEnemyDeath();
        Assert.AreEqual(ContractStatus.Complete, ContractProgress.Status);
        // Return to the board and claim.
        TeleportNear(hero, boardGo.transform.position);
        yield return null;
        Assert.IsTrue(board.TryClaim(hero, path));
        Assert.AreEqual(130, hero.GetComponent<Wallet>().Gold);
        Assert.AreEqual(ContractStatus.Claimed, ContractProgress.Status);
    }

    [UnityTest]
    public IEnumerator Recovery_ReachCollectReturnClaim()
    {
        var offer = RelicOffer("contract-1-fen-cache", 95);
        Assert.IsTrue(ContractProgress.TryAccept(offer));
        var objective = ContractObjective.SpawnFor(offer);
        Assert.IsNotNull(objective);
        // Walk into collect reach: the objective completes on hero proximity.
        TeleportNear(hero, objective.transform.position);
        yield return null;
        Assert.AreEqual(ContractStatus.Complete, ContractProgress.Status);
        TeleportNear(hero, boardGo.transform.position);
        yield return null;
        Assert.IsTrue(board.TryClaim(hero, path));
        Assert.AreEqual(95, hero.GetComponent<Wallet>().Gold);
        Assert.AreEqual(ContractStatus.Claimed, ContractProgress.Status);
    }

    [UnityTest]
    public IEnumerator Claim_ReloadCannotPayAgain()
    {
        var offer = HuntOffer("contract-2-ash-elite", 140);
        Assert.IsTrue(ContractProgress.TryAccept(offer));
        Assert.IsTrue(ContractProgress.TryComplete(offer.id));
        TeleportNear(hero, boardGo.transform.position);
        yield return null;
        Assert.IsTrue(board.TryClaim(hero, path));
        Assert.AreEqual(140, hero.GetComponent<Wallet>().Gold);
        // Reload: the claim persisted, a second claim pays nothing.
        int goldBefore = hero.GetComponent<Wallet>().Gold;
        Assert.IsTrue(SaveService.TryLoad(hero, path));
        Assert.AreEqual(ContractStatus.Claimed, ContractProgress.Status);
        Assert.IsFalse(board.TryClaim(hero, path));
        Assert.AreEqual(goldBefore, hero.GetComponent<Wallet>().Gold);
    }

    [UnityTest]
    public IEnumerator Claim_WriteFailureKeepsRewardAvailable()
    {
        var offer = RelicOffer("contract-3-fen-cache", 100);
        Assert.IsTrue(ContractProgress.TryAccept(offer));
        Assert.IsTrue(ContractProgress.TryComplete(offer.id));
        TeleportNear(hero, boardGo.transform.position);
        yield return null;
        ContractBoard.WriteHook = (save, p) => throw new IOException("disk full");
        Assert.IsFalse(board.TryClaim(hero, path));
        // Live values rolled back: no gold, still complete, reward still available.
        Assert.AreEqual(0, hero.GetComponent<Wallet>().Gold);
        Assert.AreEqual(ContractStatus.Complete, ContractProgress.Status);
        ContractBoard.WriteHook = null;
        Assert.IsTrue(board.TryClaim(hero, path));
        Assert.AreEqual(100, hero.GetComponent<Wallet>().Gold);
        Assert.AreEqual(ContractStatus.Claimed, ContractProgress.Status);
    }
}
