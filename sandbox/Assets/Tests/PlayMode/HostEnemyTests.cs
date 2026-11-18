using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// S2 host-authoritative enemies (cinderreach-coop-session): HitValidator
/// gates (range / cooldown / dead / damage), server TakeDamage lowers HP,
/// boss phase entry summons once, targeting picks the nearest living hero.
/// ServerRpc replication itself is proven by the 2-process pair run
/// (run-coop-pair.sh); these PlayMode tests pin the validation logic on the
/// same code path the server uses.
/// </summary>
public class HostEnemyTests
{
    GameObject heroNear;
    GameObject heroFar;
    GameObject husk;

    [SetUp]
    public void Setup()
    {
        EnemyHealth.ResetRun();
        BossController.ResetRun();
        RangedCaster.ResetRun();
        DiabloMelee.ResetRun();

        heroNear = NewHero("Player", new Vector3(0f, 1f, 0f));
        heroFar = NewHero("HeroFar", new Vector3(10f, 1f, 0f));
        husk = NewHusk(new Vector3(2f, 0f, 0f), 5);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var g in GameObject.FindGameObjectsWithTag("Husk"))
            Object.DestroyImmediate(g);
        if (heroNear != null) Object.DestroyImmediate(heroNear);
        if (heroFar != null) Object.DestroyImmediate(heroFar);
        foreach (var p in GameObject.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            if (p != null) Object.DestroyImmediate(p.gameObject);
        EnemyHealth.ResetRun();
        BossController.ResetRun();
        RangedCaster.ResetRun();
    }

    static GameObject NewHero(string name, Vector3 at)
    {
        var go = new GameObject(name);
        go.transform.position = at;
        go.AddComponent<PlayerHealth>();
        go.AddComponent<CharacterController>();
        go.AddComponent<Equipment>();
        return go;
    }

    static GameObject NewHusk(Vector3 at, int hp)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.tag = "Husk";
        go.transform.position = at;
        go.AddComponent<EnemyHealth>().SetTier(hp, LootTier.Normal);
        go.AddComponent<EnemyAttack>().damage = 1;
        return go;
    }

    [UnityTest]
    public IEnumerator Validator_AcceptsCloseReadyLiveHit()
    {
        var health = husk.GetComponent<EnemyHealth>();
        string reason = HitValidator.Validate(
            heroNear.transform.position, husk.transform.position,
            DiabloNetworkPlayer.MeleeValidateRange, float.NegativeInfinity, 0.5f,
            10f, health.IsAlive, 2);
        Assert.IsNull(reason, $"valid hit rejected: {reason}");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Validator_RejectsTooFarCooldownDeadAndDamage()
    {
        var health = husk.GetComponent<EnemyHealth>();
        // Too far: 100 m with a 4 m melee gate.
        string far = HitValidator.Validate(
            new Vector3(100f, 0f, 0f), husk.transform.position,
            DiabloNetworkPlayer.MeleeValidateRange, float.NegativeInfinity, 0.5f,
            10f, true, 2);
        StringAssert.Contains("range", far);

        // Cooldown: 0.1 s after the last hit with a 0.5 s gate.
        string cd = HitValidator.Validate(
            heroNear.transform.position, husk.transform.position,
            DiabloNetworkPlayer.MeleeValidateRange, 10f, 0.5f,
            10.1f, true, 2);
        StringAssert.Contains("cooldown", cd);

        // Dead target.
        string dead = HitValidator.Validate(
            heroNear.transform.position, husk.transform.position,
            DiabloNetworkPlayer.MeleeValidateRange, float.NegativeInfinity, 0.5f,
            10f, false, 2);
        StringAssert.Contains("dead", dead);

        // Fake damage.
        string dmg = HitValidator.Validate(
            heroNear.transform.position, husk.transform.position,
            DiabloNetworkPlayer.MeleeValidateRange, float.NegativeInfinity, 0.5f,
            10f, true, 9999);
        StringAssert.Contains("damage", dmg);
        Assert.IsFalse(health == null);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ServerHit_LowersHPAndReplicatesLocally()
    {
        var health = husk.GetComponent<EnemyHealth>();
        int before = health.CurrentHP;
        health.TakeDamage(2);
        Assert.AreEqual(before - 2, health.CurrentHP, "server TakeDamage must lower HP");
        yield return null;
        // Kill: drops from the kill path without exceptions (loot may spawn).
        health.TakeDamage(health.CurrentHP);
        Assert.IsFalse(health.IsAlive);
        Assert.AreEqual(1, EnemyHealth.HusksKilled);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Boss_PhaseTwoSummonsOnce()
    {
        var boss = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        boss.tag = "Husk";
        boss.transform.position = new Vector3(200f, 0f, -10f);
        boss.AddComponent<EnemyHealth>().SetTier(BossController.BossMaxHP, LootTier.Boss);
        var ctrl = boss.AddComponent<BossController>();
        int before = BossController.PhaseTwoEntries;
        ctrl.EnterPhaseTwo();
        Assert.AreEqual(before + 1, BossController.PhaseTwoEntries);
        // Second call is a no-op (summons once).
        ctrl.EnterPhaseTwo();
        Assert.AreEqual(before + 1, BossController.PhaseTwoEntries);
        // Cleanup summons (offline spawn, no NetworkObject).
        foreach (var g in GameObject.FindGameObjectsWithTag("Husk"))
            if (g.name.StartsWith("Husk_Summon_")) Object.DestroyImmediate(g);
        Object.DestroyImmediate(boss);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Targeting_PicksNearestLiving()
    {
        // Near hero alive: must win over the far one.
        var pick = HitValidator.NearestLivingPlayer(husk.transform.position);
        Assert.IsNotNull(pick);
        Assert.AreEqual(heroNear.transform, pick);

        // Kill the near hero: the far one takes over.
        heroNear.GetComponent<PlayerHealth>().TakeDamage(9999f);
        var pick2 = HitValidator.NearestLivingPlayer(husk.transform.position);
        Assert.IsNotNull(pick2);
        Assert.AreEqual(heroFar.transform, pick2);
        yield return null;
    }
}
