using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Playtest feedback 2026-11-23: every zone floor is walled, nothing the
/// player must reach lies outside the walls, and companions clamp to floors.
/// </summary>
public class WorldEdgeTests
{
    const string ZonePath = "Assets/Scenes/DiabloZone.unity";

    [Test]
    public void EveryZone_HasFourSolidEdgeWallsOnItsRim()
    {
        EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        foreach (var zone in new[] { "Fen", "Ossuary", "Kiln", "Barrens" })
        {
            var z = Zone(zone);
            foreach (var side in new[] { "N", "S", "E", "W" })
            {
                var wall = GameObject.Find(zone == "Fen" ? $"MoorEdge_{side}" : $"ZoneEdge_{zone}_{side}");
                Assert.IsNotNull(wall, $"{zone} needs its {side} edge wall.");
                var col = wall.GetComponent<BoxCollider>();
                Assert.IsNotNull(col, $"{wall.name} blocks with a collider.");
                Assert.IsFalse(col.isTrigger);
                var b = col.bounds;
                Assert.GreaterOrEqual(b.size.y, 1.2f, $"{wall.name} is too low to stop a hero.");
                // The wall's inner face sits on the walkable rim and spans the whole side.
                bool alongX = b.size.x > b.size.z;
                float inner = alongX
                    ? Mathf.Min(Mathf.Abs(b.min.z - z.center.z), Mathf.Abs(b.max.z - z.center.z))
                    : Mathf.Min(Mathf.Abs(b.min.x - z.center.x), Mathf.Abs(b.max.x - z.center.x));
                Assert.AreEqual(z.halfExtent, inner, 0.05f, $"{wall.name} inner face off the rim.");
                Assert.GreaterOrEqual(alongX ? b.size.x : b.size.z, 2f * z.halfExtent, $"{wall.name} leaves a gap.");
            }
        }

    }

    [TestCase("Fen", "Ground", 35f, 16)]
    [TestCase("Ossuary", "OssuaryFloor", 28f, 18)]
    [TestCase("Kiln", "KilnFloor", 28f, 18)]
    [TestCase("Barrens", "BarrensFloor", 38f, 18)]
    public void ExpandedFloor_CoversFourTimesTheAreaWithSafeEncounters(string name, string floorName, float oldWidth, int count)
    {
        EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        var zone = Zone(name);
        Assert.AreEqual(oldWidth * 2f, zone.halfExtent * 2f);
        var ground = GameObject.Find(floorName).GetComponent<Collider>().bounds;
        Assert.LessOrEqual(ground.min.x, zone.center.x - zone.halfExtent);
        Assert.GreaterOrEqual(ground.max.x, zone.center.x + zone.halfExtent);
        Assert.LessOrEqual(ground.min.z, zone.center.z - zone.halfExtent);
        Assert.GreaterOrEqual(ground.max.z, zone.center.z + zone.halfExtent);

        var safe = new List<Vector3>();
        foreach (var gate in Object.FindObjectsByType<ZoneGate>(FindObjectsSortMode.None))
        {
            safe.Add(gate.transform.position);
            safe.Add(gate.destination);
        }
        foreach (var shrine in Object.FindObjectsByType<WaypointShrine>(FindObjectsSortMode.None))
            safe.Add(shrine.Arrival);
        foreach (var npc in Object.FindObjectsByType<StoryNpc>(FindObjectsSortMode.None)) safe.Add(npc.transform.position);
        safe.Add(GameObject.Find(Vendor.VendorObjectName).transform.position);
        for (int i = 0; i < 4; i++) safe.Add(GameObject.Find($"CoopSpawn_{i}").transform.position);

        int added = 0, inner = 0;
        var quadrants = new HashSet<int>();
        foreach (var enemy in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            var p = enemy.transform.position;
            if (!zone.Contains(p)) continue;
            var d = p - zone.center;
            bool patrol = name == "Ossuary"
                ? enemy.name == "Bone Warden" && d.sqrMagnitude > 100f
                : AfterRoster(enemy.name, "Husk_", LayoutSeeder.HuskCount) ||
                  AfterRoster(enemy.name, KilnLayout.ThrallPrefix, KilnLayout.ThrallCount) ||
                  AfterRoster(enemy.name, BarrensLayout.ShardbackPrefix, BarrensLayout.ShardbackCount);
            if (!patrol) continue;
            added++;
            if (Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.z)) <= oldWidth / 2f) inner++;
            else quadrants.Add((d.x >= 0f ? 1 : 0) + (d.z >= 0f ? 2 : 0));
            float range = enemy.GetComponent<RangedCaster>() ? RangedCaster.AggroRange : EnemyAttack.AggroRange;
            foreach (var site in safe)
            {
                var delta = p - site;
                Assert.Greater(new Vector2(delta.x, delta.z).magnitude, range, $"{enemy.name} engages a safe site at {site}");
            }
            if (name == "Fen")
                Assert.Greater(Vector2.Distance(new Vector2(p.x, p.z), new Vector2(0f, 17f)), 12f);
        }
        Assert.AreEqual(count, added);
        Assert.GreaterOrEqual(inner, 2, "Previously empty pockets inside the old edge get a pair.");
        Assert.AreEqual(4, quadrants.Count, "Each quarter of the added ground has an encounter.");
    }

    static bool AfterRoster(string name, string prefix, int seededCount) =>
        name.StartsWith(prefix, System.StringComparison.Ordinal)
        && int.TryParse(name.Substring(prefix.Length), out int index) && index >= seededCount;

    [Test]
    public void GatesShrinesSpawnsAndArrivals_AreInsideTheWalls()
    {
        EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        foreach (var gate in Object.FindObjectsByType<ZoneGate>(FindObjectsSortMode.None))
        {
            Assert.IsTrue(WorldBounds.IsWalkable(gate.transform.position, 0.5f), $"{gate.name} sits outside the walls.");
            Assert.IsTrue(WorldBounds.IsWalkable(gate.destination, 0.5f), $"{gate.name} lands outside the walls.");
        }
        foreach (var shrine in Object.FindObjectsByType<WaypointShrine>(FindObjectsSortMode.None))
            Assert.IsTrue(WorldBounds.IsWalkable(shrine.Arrival, 0.5f), $"{shrine.name} arrival outside the walls.");
        for (int i = 0; i < 4; i++)
            Assert.IsTrue(WorldBounds.IsWalkable(GameObject.Find($"CoopSpawn_{i}").transform.position, 0.5f));
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
            Assert.IsTrue(WorldBounds.IsWalkable(husk.transform.position), $"{husk.name} spawns outside the walls.");
        Assert.IsTrue(WorldBounds.IsWalkable(KilnLayout.KilnArrival, 0.5f));
        Assert.IsTrue(WorldBounds.IsWalkable(BarrensLayout.BarrensArrival, 0.5f));
        Assert.IsTrue(WorldBounds.IsWalkable(BarrensLayout.KilnReturnArrival, 0.5f));
        Assert.IsTrue(WorldBounds.IsWalkable(KilnLayout.OssuaryReturnArrival, 0.5f));
    }

    [Test]
    public void Clamp_KeepsPointsOnTheNearestFloor()
    {
        var kiln = Zone("Kiln");
        var outside = kiln.center + new Vector3(kiln.halfExtent + 10f, 0f, -3f);
        var c = WorldBounds.Clamp(outside);
        Assert.IsTrue(kiln.Contains(c), "a point east of the Kiln clamps onto the Kiln floor");
        Assert.AreEqual(-3f, c.z - kiln.center.z, 0.0001f, "clamping keeps the free axis");
        var inside = kiln.center + new Vector3(3f, 0.7f, 4f);
        Assert.AreEqual(inside, WorldBounds.Clamp(inside), "points on the floor stay put");
        Assert.IsFalse(WorldBounds.IsWalkable(new Vector3(100f, 0f, 0f)), "between zones is no floor");
    }

    static WorldBounds.Zone Zone(string name)
    {
        foreach (var z in WorldBounds.Zones)
            if (z.name == name) return z;
        Assert.Fail($"no zone {name}");
        return default;
    }
}
