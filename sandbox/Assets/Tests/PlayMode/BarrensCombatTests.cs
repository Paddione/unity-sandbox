using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// act-two-opening S2 (PlayMode): Glass Barrens enemies hold at the arrival
/// plaza, then a Shardback turns slowly and strikes, its guard catches a front
/// swing but not a back swing, and a Dune Augur calls a shard field under the hero.
/// </summary>
public class BarrensCombatTests
{
    GameObject hero;

    [SetUp]
    public void Setup()
    {
        DiabloJuice.ForceCrit = false;
        RunProfile.Reset(); // re-arms both plaza latches
        EnemyHealth.ResetRun();
        ShardGuard.ResetRun();
        ShardField.ResetRun();
        DuneAugur.ResetRun();
        RangedCaster.ResetRun();
        RunProfile.Act.Unlock(ActProgress.Kiln);
        RunProfile.Act.CompleteAct();

        hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = "Player";
        hero.AddComponent<CharacterController>();
        hero.AddComponent<PlayerHealth>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var f in Object.FindObjectsOfType<ShardField>())
            Object.DestroyImmediate(f.gameObject);
        DiabloTestFixture.CleanupTestScene(hero);
        EnemyHealth.ResetRun();
        ShardGuard.ResetRun();
        ShardField.ResetRun();
        DuneAugur.ResetRun();
        RangedCaster.ResetRun();
        RunProfile.Reset();
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    static GameObject SpawnShardback(Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = BarrensLayout.ShardbackPrefix + "t";
        go.tag = "Husk";
        go.transform.position = pos;
        go.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        go.AddComponent<EnemyHealth>().SetTier(14, LootTier.Normal);
        var a = go.AddComponent<EnemyAttack>();
        a.turnRate = ShardGuard.TurnRate;
        go.AddComponent<ShardGuard>();
        return go;
    }

    static GameObject SpawnAugur(Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = BarrensLayout.AugurPrefix + "t";
        go.tag = "Husk";
        go.transform.position = pos;
        go.AddComponent<EnemyHealth>().SetTier(9, LootTier.Normal);
        go.AddComponent<RangedCaster>();
        go.AddComponent<DuneAugur>();
        return go;
    }

    [UnityTest]
    public IEnumerator Shardback_HoldsAtPlaza_ThenStrikes()
    {
        hero.transform.position = BarrensLayout.BarrensArrival;
        var sb = SpawnShardback(BarrensLayout.BarrensArrival + new Vector3(0f, -0.95f, -1.5f));
        var hp = hero.GetComponent<PlayerHealth>();
        float full = hp.CurrentHP;
        yield return new WaitForSeconds(2.2f);
        Assert.AreEqual(full, hp.CurrentHP, "plaza: no strike while the hero orients");

        // Step off the plaza: the latch breaks and the Shardback engages.
        hero.transform.position = BarrensLayout.BarrensOffset + new Vector3(0f, 1f, 4f);
        sb.transform.position = BarrensLayout.BarrensOffset + new Vector3(0f, 0.05f, 2.5f);
        yield return new WaitForSeconds(2.5f);
        Assert.IsTrue(BarrensLayout.PlazaBreached);
        Assert.Less(hp.CurrentHP, full, "the Shardback struck after the hero advanced");
    }

    [UnityTest]
    public IEnumerator Shardback_FrontSwingBlocked_BackSwingLands()
    {
        hero.transform.position = BarrensLayout.BarrensOffset + new Vector3(0f, 1f, -10f); // off-plaza, out of aggro
        BarrensLayout.PlazaSafe(hero.transform.position);
        var sb = SpawnShardback(BarrensLayout.BarrensOffset + new Vector3(0f, 0.05f, 0f));
        var hp = sb.GetComponent<EnemyHealth>();
        yield return null;
        hp.TakeDamage(4, false, sb.transform.position + sb.transform.forward * 2f);
        Assert.AreEqual(13, hp.CurrentHP);
        hp.TakeDamage(4, false, sb.transform.position - sb.transform.forward * 2f);
        Assert.AreEqual(9, hp.CurrentHP);
        Assert.AreEqual(1, ShardGuard.Blocks);
    }

    [UnityTest]
    public IEnumerator Augur_CallsShardFieldUnderTheHero()
    {
        hero.transform.position = BarrensLayout.BarrensOffset + new Vector3(0f, 1f, -2f);
        BarrensLayout.PlazaSafe(hero.transform.position); // breach: off the arrival plaza
        SpawnAugur(BarrensLayout.BarrensOffset + new Vector3(0f, 0.05f, -9f));
        float until = Time.time + 6f;
        while (Time.time < until && DuneAugur.Casts == 0) yield return null;
        Assert.GreaterOrEqual(DuneAugur.Casts, 1, "the Augur cast within its first cycle");
        var field = Object.FindObjectOfType<ShardField>();
        Assert.IsNotNull(field);
        var d = field.transform.position - hero.transform.position;
        d.y = 0f;
        Assert.Less(d.magnitude, ShardField.Radius, "field lands under the hero");
        yield return new WaitForSeconds(ShardField.Telegraph + 0.3f);
        Assert.GreaterOrEqual(ShardField.HeroHits, 1, "standing in the field cuts");
    }
}
