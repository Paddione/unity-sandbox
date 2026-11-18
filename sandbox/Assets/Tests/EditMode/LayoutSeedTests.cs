using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// replayability S3: seeded Ashen Fen layouts. Reachability runs on the real
/// zone scene: a 0.5 m grid of capsule checks (hero size) and a flood fill from
/// the spawn must reach the Fen Gate, the chest and every enemy anchor for all
/// 81 variant combinations.
/// </summary>
public class LayoutSeedTests
{
    const string ZonePath = "Assets/Scenes/DiabloZone.unity";
    const float Step = 0.5f;
    const float Half = 17.5f;
    const float HeroRadius = 0.35f;

    [Test]
    public void ForSeed_IsDeterministicAndCoversEveryVariant()
    {
        Assert.AreEqual(LayoutSeeder.ForSeed(42).ToString(), LayoutSeeder.ForSeed(42).ToString());
        var seen = new HashSet<string>();
        for (int seed = 0; seed < 200; seed++)
        {
            var l = LayoutSeeder.ForSeed(seed);
            seen.Add($"h{l.husks}");
            seen.Add($"e{l.elite}");
            seen.Add($"b{l.brute}");
            seen.Add($"p{l.props}");
        }
        foreach (var cat in new[] { "h", "e", "b", "p" })
            for (int v = 0; v < LayoutSeeder.VariantCount; v++)
                Assert.IsTrue(seen.Contains(cat + v), $"{cat}{v} never chosen");
    }

    [Test]
    public void Seeds_ZeroToTwo_GiveDistinctLayouts()
    {
        var a = LayoutSeeder.ForSeed(0).ToString();
        var b = LayoutSeeder.ForSeed(1).ToString();
        var c = LayoutSeeder.ForSeed(2).ToString();
        Assert.AreEqual(3, new HashSet<string> { a, b, c }.Count, $"{a} | {b} | {c}");
    }

    [Test]
    public void Scene_BaselineMatchesBuilderAndHasThreePropRoots()
    {
        EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        for (int i = 0; i < LayoutSeeder.HuskCount; i++)
        {
            var husk = GameObject.Find($"Husk_{i}");
            Assert.IsNotNull(husk, $"Husk_{i}");
            Assert.Less(Vector3.Distance(husk.transform.position, LayoutSeeder.HuskSets[0][i]), 0.01f, $"Husk_{i}");
        }
        Assert.Less(Vector3.Distance(GameObject.Find(LayoutSeeder.EliteName).transform.position, LayoutSeeder.EliteSpots[0]), 0.01f);
        Assert.Less(Vector3.Distance(GameObject.Find(LayoutSeeder.BruteName).transform.position, LayoutSeeder.BruteSpots[0]), 0.01f);
        for (int v = 0; v < LayoutSeeder.VariantCount; v++)
        {
            var root = LayoutSeeder.FindIncludingInactive(LayoutSeeder.PropRootPrefix + v);
            Assert.IsNotNull(root, $"prop root {v}");
            Assert.AreEqual(v == 0, root.activeSelf, $"only variant 0 starts active ({v})");
            Assert.AreEqual(LayoutSeeder.PropSets[v].Length, root.transform.childCount);
        }
    }

    [Test]
    public void Apply_MovesEnemiesAndTogglesProps()
    {
        EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        var layout = new LayoutSeeder.Layout { husks = 2, elite = 1, brute = 2, props = 1 };
        int kilnEnemies = KilnLayout.ThrallCount + KilnLayout.LancerCount + KilnLayout.EliteCount;
        Assert.AreEqual(LayoutSeeder.HuskCount + 2 + kilnEnemies, LayoutSeeder.Apply(layout));
        Assert.Less(Vector3.Distance(GameObject.Find("Husk_3").transform.position, LayoutSeeder.HuskSets[2][3]), 0.01f);
        Assert.Less(Vector3.Distance(GameObject.Find(LayoutSeeder.EliteName).transform.position, LayoutSeeder.EliteSpots[1]), 0.01f);
        Assert.IsTrue(LayoutSeeder.FindIncludingInactive(LayoutSeeder.PropRootPrefix + "1").activeSelf);
        Assert.IsFalse(LayoutSeeder.FindIncludingInactive(LayoutSeeder.PropRootPrefix + "0").activeSelf);
    }

    [Test]
    public void EveryLayout_ReachesGateChestAndAllEnemies()
    {
        EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        var player = GameObject.Find("Player");
        var gate = GameObject.Find(ZoneGate.FenGateName);
        var chest = GameObject.Find("DChestZone");
        Assert.IsNotNull(player);
        Assert.IsNotNull(gate);
        Assert.IsNotNull(chest);

        // Actors are not obstacles for the walk check.
        foreach (var go in GameObject.FindGameObjectsWithTag("Husk"))
            foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var c in player.GetComponentsInChildren<Collider>()) c.enabled = false;

        var failures = new List<string>();
        for (int h = 0; h < 3; h++)
        for (int e = 0; e < 3; e++)
        for (int b = 0; b < 3; b++)
        for (int p = 0; p < 3; p++)
        {
            var layout = new LayoutSeeder.Layout { husks = h, elite = e, brute = b, props = p };
            LayoutSeeder.Apply(layout);
            Physics.SyncTransforms();
            var reach = FloodFrom(player.transform.position);
            void Need(string what, Vector3 pos)
            {
                if (!Reachable(reach, pos)) failures.Add($"{layout}: {what} @ {pos}");
            }
            Need("FenGate", gate.transform.position);
            Need("chest", chest.transform.position);
            for (int i = 0; i < LayoutSeeder.HuskCount; i++) Need($"Husk_{i}", LayoutSeeder.HuskSets[h][i]);
            Need("elite", LayoutSeeder.EliteSpots[e]);
            Need("brute", LayoutSeeder.BruteSpots[b]);
        }
        Assert.IsEmpty(failures, string.Join("\n", failures.GetRange(0, Mathf.Min(10, failures.Count))));
    }

    static int Cells => Mathf.RoundToInt(Half * 2f / Step) + 1;

    static Vector2Int ToCell(Vector3 p) => new Vector2Int(
        Mathf.Clamp(Mathf.RoundToInt((p.x + Half) / Step), 0, Cells - 1),
        Mathf.Clamp(Mathf.RoundToInt((p.z + Half) / Step), 0, Cells - 1));

    static Vector3 ToWorld(int x, int z) => new Vector3(-Half + x * Step, 0f, -Half + z * Step);

    /// <summary>
    /// Capsule bottom sits at 0.4 m: low steps (ZStep_*, 0.3 m) are climbed by the
    /// CharacterController step offset and must not count as walls.
    /// </summary>
    static bool Walkable(int x, int z)
    {
        var p = ToWorld(x, z);
        return !Physics.CheckCapsule(p + Vector3.up * (0.4f + HeroRadius), p + Vector3.up * 1.6f, HeroRadius,
            ~0, QueryTriggerInteraction.Ignore);
    }

    static bool[,] FloodFrom(Vector3 start)
    {
        int n = Cells;
        var seen = new bool[n, n];
        var walk = new bool[n, n];
        for (int x = 0; x < n; x++)
            for (int z = 0; z < n; z++)
                walk[x, z] = Walkable(x, z);
        var s = NearestWalkable(walk, ToCell(start), Mathf.CeilToInt(2f / Step));
        var queue = new Queue<Vector2Int>();
        seen[s.x, s.y] = true;
        queue.Enqueue(s);
        var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            foreach (var d in dirs)
            {
                var nb = c + d;
                if (nb.x < 0 || nb.y < 0 || nb.x >= n || nb.y >= n) continue;
                if (seen[nb.x, nb.y] || !walk[nb.x, nb.y]) continue;
                seen[nb.x, nb.y] = true;
                queue.Enqueue(nb);
            }
        }
        return seen;
    }

    static Vector2Int NearestWalkable(bool[,] walk, Vector2Int c, int radius)
    {
        var best = c;
        int bestD = int.MaxValue;
        for (int dx = -radius; dx <= radius; dx++)
            for (int dz = -radius; dz <= radius; dz++)
            {
                int x = c.x + dx, z = c.y + dz;
                if (x < 0 || z < 0 || x >= Cells || z >= Cells || !walk[x, z]) continue;
                int d = dx * dx + dz * dz;
                if (d < bestD) { bestD = d; best = new Vector2Int(x, z); }
            }
        return best;
    }

    /// <summary>A target counts as reached when a flooded cell lies within 1.5 m (melee/trigger reach).</summary>
    static bool Reachable(bool[,] reach, Vector3 target)
    {
        var c = ToCell(target);
        int r = Mathf.CeilToInt(1.5f / Step);
        for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
            {
                int x = c.x + dx, z = c.y + dz;
                if (x < 0 || z < 0 || x >= Cells || z >= Cells) continue;
                if (reach[x, z] && Vector3.Distance(ToWorld(x, z), new Vector3(target.x, 0f, target.z)) <= 1.5f)
                    return true;
            }
        return false;
    }
}
