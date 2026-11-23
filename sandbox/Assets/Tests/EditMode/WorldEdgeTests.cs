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
    public void AnnexZones_HaveFourSolidEdgeWallsOnTheirRim()
    {
        EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        foreach (var zone in new[] { "Ossuary", "Kiln", "Barrens" })
        {
            var z = Zone(zone);
            foreach (var side in new[] { "N", "S", "E", "W" })
            {
                var wall = GameObject.Find($"ZoneEdge_{zone}_{side}");
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
        foreach (var side in new[] { "N", "S", "E", "W" })
            Assert.IsNotNull(GameObject.Find($"MoorEdge_{side}"), "The Fen keeps its earthwork edge.");
    }

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
        var outside = kiln.center + new Vector3(25f, 0f, -3f);
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
