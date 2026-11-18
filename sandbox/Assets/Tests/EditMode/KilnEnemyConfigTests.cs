using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>act-one S3/S4: Kiln enemy inventory, seeded anchors, reachability, enemy and boss rules.</summary>
public class KilnEnemyConfigTests
{
    const string ZonePath = "Assets/Scenes/DiabloZone.unity";

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        ThrallBlast.ResetRun();
        AshLancer.ResetRun();
        BurningGround.ResetRun();
    }

    [TearDown]
    public void Teardown() => RunProfile.Reset();

    [Test]
    public void Scene_HasPacksLancersElitesAndKilnmother()
    {
        EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        for (int i = 0; i < KilnLayout.ThrallCount; i++)
        {
            var t = GameObject.Find(KilnLayout.ThrallPrefix + i);
            Assert.IsNotNull(t, $"thrall {i}");
            Assert.IsNotNull(t.GetComponent<CinderThrall>());
            Assert.IsNotNull(t.GetComponent<EnemyAttack>());
            Assert.Less(Vector3.Distance(t.transform.position, KilnLayout.ThrallAnchor(0, i)), 0.01f);
        }
        for (int i = 0; i < KilnLayout.LancerCount; i++)
            Assert.IsNotNull(GameObject.Find(KilnLayout.LancerPrefix + i).GetComponent<AshLancer>());
        for (int i = 0; i < KilnLayout.EliteCount; i++)
            Assert.AreEqual(LootTier.Elite, GameObject.Find(KilnLayout.ElitePrefix + i).GetComponent<EnemyHealth>().tier);
        var mother = GameObject.Find(KilnLayout.KilnmotherName);
        Assert.IsNotNull(mother.GetComponent<KilnmotherController>());
        var hp = mother.GetComponent<EnemyHealth>();
        Assert.AreEqual(KilnmotherController.MaxHP, hp.maxHP);
        Assert.AreEqual(LootTier.ActBoss, hp.tier);
    }

    [Test]
    public void KilnDraw_KeepsTheFenPicksOfEarlierSeeds()
    {
        Assert.AreEqual("husks=2 elite=2 brute=2 props=1", Fen(LayoutSeeder.ForSeed(0)));
        Assert.AreEqual("husks=0 elite=0 brute=1 props=2", Fen(LayoutSeeder.ForSeed(1)));
        Assert.AreEqual("husks=2 elite=1 brute=0 props=2", Fen(LayoutSeeder.ForSeed(2)));
        var seen = new HashSet<int>();
        for (int s = 0; s < 100; s++) seen.Add(LayoutSeeder.ForSeed(s).kiln);
        Assert.AreEqual(3, seen.Count);
    }

    static string Fen(LayoutSeeder.Layout l) => $"husks={l.husks} elite={l.elite} brute={l.brute} props={l.props}";

    [Test]
    public void EveryKilnVariant_ReachesAllAnchorsTheBossAndTheReturnGate()
    {
        EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        foreach (var go in GameObject.FindGameObjectsWithTag("Husk"))
            foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
        Physics.SyncTransforms();
        const float step = 0.5f, half = 14.5f;
        int n = Mathf.RoundToInt(half * 2f / step) + 1;
        var o = KilnLayout.KilnOffset;
        var walk = new bool[n, n];
        for (int x = 0; x < n; x++)
            for (int z = 0; z < n; z++)
            {
                var p = o + new Vector3(-half + x * step, 0f, -half + z * step);
                walk[x, z] = !Physics.CheckCapsule(p + Vector3.up * 0.75f, p + Vector3.up * 1.6f, 0.35f, ~0, QueryTriggerInteraction.Ignore);
            }
        Vector2Int Cell(Vector3 w) => new Vector2Int(
            Mathf.Clamp(Mathf.RoundToInt((w.x - o.x + half) / step), 0, n - 1),
            Mathf.Clamp(Mathf.RoundToInt((w.z - o.z + half) / step), 0, n - 1));
        var seen = new bool[n, n];
        var q = new Queue<Vector2Int>();
        var s = Cell(KilnLayout.KilnArrival);
        Assert.IsTrue(walk[s.x, s.y], "arrival is free");
        seen[s.x, s.y] = true;
        q.Enqueue(s);
        var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (q.Count > 0)
        {
            var c = q.Dequeue();
            foreach (var d in dirs)
            {
                var nb = c + d;
                if (nb.x < 0 || nb.y < 0 || nb.x >= n || nb.y >= n || seen[nb.x, nb.y] || !walk[nb.x, nb.y]) continue;
                seen[nb.x, nb.y] = true;
                q.Enqueue(nb);
            }
        }
        bool Reach(Vector3 w)
        {
            var c = Cell(w);
            for (int dx = -3; dx <= 3; dx++)
                for (int dz = -3; dz <= 3; dz++)
                {
                    int x = c.x + dx, z = c.y + dz;
                    if (x >= 0 && z >= 0 && x < n && z < n && seen[x, z]) return true;
                }
            return false;
        }
        var fails = new List<string>();
        for (int v = 0; v < KilnLayout.Variants; v++)
        {
            for (int i = 0; i < KilnLayout.ThrallCount; i++) if (!Reach(KilnLayout.ThrallAnchor(v, i))) fails.Add($"v{v} thrall {i}");
            for (int i = 0; i < KilnLayout.LancerCount; i++) if (!Reach(KilnLayout.LancerAnchor(v, i))) fails.Add($"v{v} lancer {i}");
            for (int i = 0; i < KilnLayout.EliteCount; i++) if (!Reach(KilnLayout.EliteAnchor(v, i))) fails.Add($"v{v} elite {i}");
        }
        if (!Reach(KilnLayout.KilnmotherPos)) fails.Add("kilnmother");
        if (!Reach(KilnLayout.KilnReturnPos)) fails.Add("return gate");
        Assert.IsEmpty(fails, string.Join(", ", fails));
    }

    [Test]
    public void ThrallBlast_WaitsThenHitsOnlyTheHeroInRadius()
    {
        // Own scene: an open zone scene from another suite has its own "Player".
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var hero = new GameObject(DiabloNetworkPlayer.HeroName);
        var hp = hero.AddComponent<PlayerHealth>();
        hero.transform.position = new Vector3(1.5f, 0f, 0f);
        var blast = ThrallBlast.Spawn(Vector3.zero);
        float t0 = Time.time;
        Assert.IsFalse(blast.Tick(t0 + CinderThrall.BlastDelay * 0.5f), "telegraph first");
        Assert.AreEqual(Equipment.BaseMaxHP, hp.CurrentHP, 0.001f);
        Assert.IsTrue(blast.Tick(t0 + CinderThrall.BlastDelay));
        Assert.AreEqual(Equipment.BaseMaxHP - CinderThrall.BlastDamage, hp.CurrentHP, 0.001f);
        Assert.IsTrue(ThrallBlast.Hits(Vector3.zero, new Vector3(2f, 0f, 0f)));
        Assert.IsFalse(ThrallBlast.Hits(Vector3.zero, new Vector3(2.1f, 0f, 0f)));
        Object.DestroyImmediate(hero);
    }

    [Test]
    public void Lancer_TelegraphsThenChargesAndHitsOnce()
    {
        var hero = new GameObject(DiabloNetworkPlayer.HeroName);
        var hp = hero.AddComponent<PlayerHealth>();
        hero.transform.position = new Vector3(0f, 0f, 8f);
        var lancerGo = new GameObject("Lancer");
        var lancer = lancerGo.AddComponent<AshLancer>();
        float t = 100f;
        lancer.Tick(t, 0.02f, hero.transform);
        Assert.AreEqual(AshLancer.State.Telegraph, lancer.Current, "already at hold distance");
        lancer.Tick(t + AshLancer.Telegraph * 0.5f, 0.02f, hero.transform);
        Assert.AreEqual(Equipment.BaseMaxHP, hp.CurrentHP, 0.001f, "no damage during the telegraph");
        lancer.Tick(t + AshLancer.Telegraph + 0.01f, 0.02f, hero.transform);
        Assert.AreEqual(AshLancer.State.Charge, lancer.Current);
        float now = t + AshLancer.Telegraph + 0.01f;
        for (int i = 0; i < 60 && lancer.Current == AshLancer.State.Charge; i++)
        {
            now += 0.02f;
            lancer.Tick(now, 0.02f, hero.transform);
        }
        Assert.AreEqual(AshLancer.State.Exhausted, lancer.Current);
        Assert.AreEqual(1, AshLancer.HeroHits);
        Assert.AreEqual(Equipment.BaseMaxHP - AshLancer.ChargeDamage, hp.CurrentHP, 0.001f);
        Object.DestroyImmediate(lancerGo);
        Object.DestroyImmediate(hero);
    }

    [Test]
    public void Lancer_StaysIdleBeyondAggro()
    {
        var hero = new GameObject(DiabloNetworkPlayer.HeroName);
        hero.AddComponent<PlayerHealth>();
        hero.transform.position = new Vector3(0f, 0f, AshLancer.AggroRange + 5f);
        var lancerGo = new GameObject("Lancer");
        var lancer = lancerGo.AddComponent<AshLancer>();
        lancer.Tick(1f, 0.5f, hero.transform);
        Assert.AreEqual(AshLancer.State.Idle, lancer.Current);
        Assert.AreEqual(Vector3.zero, lancerGo.transform.position);
        Object.DestroyImmediate(lancerGo);
        Object.DestroyImmediate(hero);
    }

    [Test]
    public void FireWave_HitsInTheRingButNotInTheGap()
    {
        var w = new FireWave(Vector3.zero, 0f, 0f); // gap points along +X
        float tAt5 = 5f / KilnmotherController.WaveSpeed;
        Assert.IsTrue(w.Overlaps(tAt5, new Vector3(0f, 0f, 5f)), "ring at 5 m, outside the gap");
        Assert.IsFalse(w.Overlaps(tAt5, new Vector3(5f, 0f, 0f)), "inside the 60° gap");
        Assert.IsFalse(w.Overlaps(tAt5, new Vector3(0f, 0f, 8f)), "ring not there yet");
    }

    [Test]
    public void BurningGround_DamagesOnlyInsideAfterTelegraph()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var hero = new GameObject(DiabloNetworkPlayer.HeroName);
        var hp = hero.AddComponent<PlayerHealth>();
        var field = BurningGround.Spawn(Vector3.zero);
        float t0 = Time.time;
        field.Tick(t0 + 0.5f, hero.transform);
        Assert.AreEqual(0, BurningGround.Ticks, "telegraph");
        field.Tick(t0 + BurningGround.Telegraph + 1.01f, hero.transform);
        Assert.AreEqual(1, BurningGround.Ticks);
        Assert.AreEqual(Equipment.BaseMaxHP - 1f, hp.CurrentHP, 0.001f);
        hero.transform.position = new Vector3(3f, 0f, 0f);
        field.Tick(t0 + BurningGround.Telegraph + 2.02f, hero.transform);
        Assert.AreEqual(1, BurningGround.Ticks, "outside: no burn");
        Object.DestroyImmediate(field.gameObject);
        Object.DestroyImmediate(hero);
    }

    [Test]
    public void ActBossLoot_IsAlwaysRarePlus()
    {
        LootTable.WeightsFor(LootTier.ActBoss, out int m, out int r, out int u);
        Assert.AreEqual(100, m + r + u);
        Assert.AreEqual(0, m);
        var rng = new System.Random(4);
        for (int i = 0; i < 200; i++)
        {
            var roll = LootTable.Roll(LootTier.ActBoss, rng);
            Assert.IsTrue(roll.drops);
            Assert.AreNotEqual(ItemRarity.Magic, roll.rarity);
        }
        DroptableDatabase.GetGoldTable(LootTier.ActBoss, out int chance, out int min, out int max);
        Assert.AreEqual(100, chance);
        Assert.AreEqual(300, min);
        Assert.AreEqual(450, max);
    }
}
