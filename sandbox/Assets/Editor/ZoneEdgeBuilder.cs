using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Edge walls for the annex zones (playtest feedback 2026-11-23: nobody falls
/// off the world). Ossuary, Kiln and Glass Barrens get four box walls each on
/// the <see cref="WorldBounds"/> rectangle, parented to the zone root.
/// <see cref="ZoneSurfaceBuilder"/> textures them per zone (crypt ashlar,
/// kiln brick, barrens stone). The Fen keeps its dressed MoorEdge earthworks.
/// </summary>
public static class ZoneEdgeBuilder
{
    public const string Prefix = "ZoneEdge_";
    public const int WallsPerZone = 4;

    /// <summary>Zones walled here (the Fen is walled by SandboxBuilder.BuildMoorField).</summary>
    public static readonly (string zone, string root)[] Walled =
    {
        ("Ossuary", OssuaryBuilder.RootName),
        ("Kiln", KilnBuilder.RootName),
        ("Barrens", BarrensLayout.RootName),
    };

    public static void BuildZoneEdges()
    {
        foreach (var (zoneName, rootName) in Walled)
        {
            var zone = Find(zoneName);
            var root = GameObject.Find(rootName);
            float h = zone.halfExtent + WorldBounds.WallThickness / 2f;
            float len = 2f * (zone.halfExtent + WorldBounds.WallThickness);
            var sides = new[]
            {
                ("N", new Vector3(0f, 0f, -h), new Vector3(len, WorldBounds.WallHeight, WorldBounds.WallThickness)),
                ("S", new Vector3(0f, 0f, h), new Vector3(len, WorldBounds.WallHeight, WorldBounds.WallThickness)),
                ("E", new Vector3(h, 0f, 0f), new Vector3(WorldBounds.WallThickness, WorldBounds.WallHeight, len)),
                ("W", new Vector3(-h, 0f, 0f), new Vector3(WorldBounds.WallThickness, WorldBounds.WallHeight, len)),
            };
            foreach (var (side, offset, scale) in sides)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"{Prefix}{zoneName}_{side}";
                wall.transform.position = zone.center + offset + Vector3.up * (WorldBounds.WallHeight / 2f);
                wall.transform.localScale = scale;
                if (root != null) wall.transform.SetParent(root.transform, true);
                wall.isStatic = true;
                var r = wall.GetComponent<Renderer>();
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (r != null && shader != null)
                    r.sharedMaterial = new Material(shader) { color = new Color(0.3f, 0.28f, 0.26f) };
            }
        }
    }

    /// <summary>Abnahme: 4 solid walls per annex zone, each on its zone's rim.</summary>
    public static void VerifyZoneEdges(UnityEngine.SceneManagement.Scene scene)
    {
        int found = 0;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith(Prefix)) continue;
                var col = t.GetComponent<BoxCollider>();
                if (col == null || col.isTrigger)
                    throw new System.Exception($"ZoneEdge verify failed: {t.name} needs a solid BoxCollider.");
                found++;
            }
        if (found != Walled.Length * WallsPerZone)
            throw new System.Exception($"ZoneEdge verify failed: {found} edge walls, want {Walled.Length * WallsPerZone}.");
        Debug.Log($"[ZoneEdgeBuilder] VerifyZoneEdges: OK ({found} walls).");
    }

    static WorldBounds.Zone Find(string name)
    {
        foreach (var z in WorldBounds.Zones)
            if (z.name == name) return z;
        throw new System.Exception($"WorldBounds has no zone {name}.");
    }
}
