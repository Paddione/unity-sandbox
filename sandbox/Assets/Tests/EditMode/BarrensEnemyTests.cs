using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>act-two-opening S2: Shardback guard, Dune Augur shard field, Barrens anchors, plaza and the built roster.</summary>
public class BarrensEnemyTests
{
    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        ShardGuard.ResetRun();
        ShardField.ResetRun();
        DuneAugur.ResetRun();
        BarrensLayout.ResetPlaza();
    }

    [TearDown]
    public void Teardown()
    {
        foreach (var f in Object.FindObjectsByType<ShardField>(FindObjectsSortMode.None)) Object.DestroyImmediate(f.gameObject);
        BarrensLayout.ResetPlaza();
        RunProfile.Reset();
    }

    static GameObject Shardback(float arc = ShardGuard.DefaultArc)
    {
        var go = new GameObject("Shardback_test");
        go.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        go.AddComponent<EnemyHealth>().SetTier(14, LootTier.Normal);
        go.AddComponent<ShardGuard>().arcDegrees = arc;
        return go;
    }

    [Test]
    public void Guard_CatchesFrontHits_FlankAndBackLandInFull()
    {
        var go = Shardback();
        var hp = go.GetComponent<EnemyHealth>();
        hp.TakeDamage(4, false, new Vector3(0f, 0f, 3f)); // front
        Assert.AreEqual(12, hp.CurrentHP, "4 × 40 % = 2");
        Assert.AreEqual(1, ShardGuard.Blocks);
        hp.TakeDamage(4, false, new Vector3(3f, 0f, -0.5f)); // flank, outside 110°
        Assert.AreEqual(8, hp.CurrentHP);
        hp.TakeDamage(4, false, new Vector3(0f, 0f, -3f)); // back
        Assert.AreEqual(4, hp.CurrentHP);
        hp.TakeDamage(1, false, new Vector3(0f, 0f, 2f)); // 1-damage poke from the front
        Assert.AreEqual(3, hp.CurrentHP, "a poke still chips the glass");
        hp.TakeDamage(2); // no origin (ticks, thorns, burns): unfiltered
        Assert.AreEqual(1, hp.CurrentHP);
        Assert.AreEqual(2, ShardGuard.Blocks);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Guard_ArcRule_AndEliteArcIsWider()
    {
        var at = Vector3.zero;
        var fwd = Vector3.forward;
        var side60 = Quaternion.Euler(0f, 60f, 0f) * Vector3.forward * 3f;
        Assert.IsTrue(ShardGuard.InArc(at, fwd, new Vector3(0f, 0f, 3f), ShardGuard.DefaultArc));
        Assert.IsFalse(ShardGuard.InArc(at, fwd, side60, ShardGuard.DefaultArc), "60° off-axis is past 55°");
        Assert.IsTrue(ShardGuard.InArc(at, fwd, side60, ShardGuard.EliteArc), "elites cover 75° each side");
        Assert.AreEqual(1, ShardGuard.Guarded(1));
        Assert.AreEqual(1, ShardGuard.Guarded(2));
        Assert.AreEqual(3, ShardGuard.Guarded(8));
        Assert.AreEqual(0, ShardGuard.Guarded(0));
    }

    [Test]
    public void Guard_DropsWhileStunned()
    {
        var go = Shardback();
        go.AddComponent<EnemyAttack>();
        var hp = go.GetComponent<EnemyHealth>();
        go.GetComponent<EnemyAttack>().Stun(2f);
        hp.TakeDamage(4, false, new Vector3(0f, 0f, 3f));
        Assert.AreEqual(10, hp.CurrentHP, "a slammed Shardback takes the full hit");
        Assert.AreEqual(0, ShardGuard.Blocks);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ShardField_WarnsThenCutsOnAnInterval()
    {
        var hero = new GameObject(DiabloNetworkPlayer.HeroName);
        var hp = hero.AddComponent<PlayerHealth>();
        float start = hp.CurrentHP;
        var field = ShardField.Spawn(Vector3.zero);
        float t0 = Time.time;
        Assert.IsFalse(field.Tick(t0 + 0.5f, hero.transform));
        Assert.AreEqual(0, ShardField.HeroHits, "telegraph never hurts");
        field.Tick(t0 + ShardField.Telegraph + 0.01f, hero.transform);
        Assert.AreEqual(1, ShardField.HeroHits);
        field.Tick(t0 + ShardField.Telegraph + 0.2f, hero.transform);
        Assert.AreEqual(1, ShardField.HeroHits, "interval holds");
        hero.transform.position = new Vector3(ShardField.Radius + 0.5f, 0f, 0f);
        field.Tick(t0 + ShardField.Telegraph + 1f, hero.transform);
        Assert.AreEqual(1, ShardField.HeroHits, "stepping out is the counter");
        Assert.Less(hp.CurrentHP, start);
        Assert.IsTrue(field.Tick(t0 + ShardField.Telegraph + ShardField.Active + 0.01f, hero.transform), "spent");
        Object.DestroyImmediate(hero);
    }

    [Test]
    public void Augur_CastsAShardFieldAtTheTarget()
    {
        var go = new GameObject("Dune Augur_test");
        var augur = go.AddComponent<DuneAugur>();
        augur.Cast(new Vector3(3f, 0f, 4f));
        Assert.AreEqual(1, DuneAugur.Casts);
        var field = Object.FindFirstObjectByType<ShardField>();
        Assert.IsNotNull(field);
        Assert.Less(Vector2.Distance(new Vector2(field.transform.position.x, field.transform.position.z), new Vector2(3f, 4f)), 0.01f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Anchors_StayClearOfBlockersAndArrival_InEveryVariant()
    {
        var arrival = BarrensLayout.BarrensArrival;
        for (int v = 0; v < BarrensLayout.Variants; v++)
        {
            void Check(string what, Vector3 p)
            {
                var local = new Vector2(p.x - BarrensLayout.BarrensOffset.x, p.z - BarrensLayout.BarrensOffset.z);
                Assert.Greater(Flat(p - arrival), 9f, $"v{v} {what} too close to the arrival");
                foreach (var d in BarrensLayout.DuneSpots) Assert.Greater(Vector2.Distance(local, d), 2.5f, $"v{v} {what} in a dune");
                foreach (var r in BarrensLayout.RuinSpots) Assert.Greater(Vector2.Distance(local, r), 1.9f, $"v{v} {what} in a ruin");
                for (int s = 0; s < BarrensLayout.ShardSpires; s++)
                    Assert.Greater(Flat(p - BarrensLayout.SpireAnchor(s)), 1.9f, $"v{v} {what} in spire {s}");
                Assert.AreEqual(ActProgress.Barrens, EncounterLayout.CurrentZone(p), $"v{v} {what} zone");
            }
            for (int i = 0; i < BarrensLayout.ShardbackCount; i++) Check($"shardback {i}", BarrensLayout.ShardbackAnchor(v, i));
            for (int i = 0; i < BarrensLayout.AugurCount; i++) Check($"augur {i}", BarrensLayout.AugurAnchor(v, i));
            for (int i = 0; i < BarrensLayout.EliteCount; i++) Check($"elite {i}", BarrensLayout.EliteAnchor(v, i));
        }
        Assert.AreNotEqual(BarrensLayout.ShardbackAnchor(0, 0), BarrensLayout.ShardbackAnchor(1, 0), "variants differ");
    }

    [Test]
    public void Plaza_HoldsUntilTheHeroLeaves_AndIgnoresOtherZones()
    {
        Assert.IsFalse(BarrensLayout.PlazaSafe(KilnLayout.KilnArrival), "Kiln is not the Barrens plaza");
        Assert.IsFalse(BarrensLayout.PlazaBreached, "other zones never latch");
        Assert.IsTrue(BarrensLayout.PlazaSafe(BarrensLayout.BarrensArrival));
        Assert.IsTrue(BarrensLayout.PlazaSafe(BarrensLayout.WaypointArrival));
        Assert.IsFalse(BarrensLayout.PlazaSafe(BarrensLayout.BarrensOffset));
        Assert.IsFalse(BarrensLayout.PlazaSafe(BarrensLayout.BarrensArrival), "latched: no re-arming by retreating");
        BarrensLayout.ResetPlaza();
        Assert.IsTrue(BarrensLayout.PlazaSafe(BarrensLayout.BarrensArrival));
    }

    [Test]
    public void KilnChoke_IgnoresHeroesInTheBarrens()
    {
        EncounterLayout.ResetEncounter();
        Assert.IsFalse(EncounterLayout.PassedChoke(BarrensLayout.BarrensOffset + new Vector3(0f, 0f, -10f)));
        Assert.IsTrue(EncounterLayout.PassedChoke(KilnLayout.KilnOffset + new Vector3(0f, 0f, -5f)));
    }

    [Test]
    public void Scene_HoldsTheBarrensRoster()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/DiabloZone.unity", OpenSceneMode.Single);
        int shardbacks = 0, augurs = 0, elites = 0;
        foreach (var h in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (h.name.StartsWith(BarrensLayout.ShardbackPrefix))
            {
                shardbacks++;
                Assert.IsNotNull(h.GetComponent<ShardGuard>(), h.name);
                Assert.AreEqual(ShardGuard.TurnRate, h.GetComponent<EnemyAttack>().turnRate, 0.001f, h.name);
                Assert.IsNotNull(h.GetComponent<Unity.Netcode.NetworkObject>(), h.name);
            }
            if (h.name.StartsWith(BarrensLayout.AugurPrefix))
            {
                augurs++;
                Assert.IsNotNull(h.GetComponent<RangedCaster>(), h.name);
                Assert.IsNotNull(h.GetComponent<DuneAugur>(), h.name);
            }
            if (h.name.StartsWith(BarrensLayout.ElitePrefix))
            {
                elites++;
                Assert.AreEqual(LootTier.Elite, h.tier, h.name);
                Assert.AreEqual(ShardGuard.EliteArc, h.GetComponent<ShardGuard>().arcDegrees, 0.001f, h.name);
            }
        }
        Assert.AreEqual(BarrensLayout.ShardbackCount + 18, shardbacks, "Nine pairs guard the empty Barrens pockets.");
        Assert.AreEqual(BarrensLayout.AugurCount, augurs);
        Assert.AreEqual(BarrensLayout.EliteCount, elites);
        Assert.AreEqual("Dünenseher", DiabloTargetBar.DisplayName("Dune Augur_3"));
        Assert.AreEqual("Glasvogt", DiabloTargetBar.DisplayName("Glass Elite_1"));
    }

    static float Flat(Vector3 v)
    {
        v.y = 0f;
        return v.magnitude;
    }
}
