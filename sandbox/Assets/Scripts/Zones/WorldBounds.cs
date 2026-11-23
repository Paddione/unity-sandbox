using UnityEngine;

/// <summary>
/// Walkable rectangle of every zone floor (playtest feedback 2026-11-23: no
/// falling off the world). The scene builder raises its edge walls on these
/// rectangles (<c>ZoneEdgeBuilder</c>; the Fen keeps its older
/// <c>MoorEdge_*</c> earthworks at the same line) and AI companions, which
/// move without a collider, clamp their steps to them.
/// </summary>
public static class WorldBounds
{
    /// <summary>One zone floor: centre on the ground and walkable half-extent (X and Z).</summary>
    public readonly struct Zone
    {
        public readonly string name;
        public readonly Vector3 center;
        public readonly float halfExtent;

        public Zone(string name, Vector3 center, float halfExtent)
        {
            this.name = name;
            this.center = center;
            this.halfExtent = halfExtent;
        }

        public bool Contains(Vector3 p, float margin = 0f) =>
            Mathf.Abs(p.x - center.x) <= halfExtent - margin && Mathf.Abs(p.z - center.z) <= halfExtent - margin;
    }

    /// <summary>Edge wall height and thickness (the Fen earthworks are 2 m × 1 m).</summary>
    public const float WallHeight = 1.6f;
    public const float WallThickness = 1f;

    /// <summary>
    /// Fen: inner face of the MoorEdge earthworks (centred ±18, 1 m thick) on
    /// the 40 m plane. Ossuary and Kiln: 30 m plates, walls 1 m inside the rim.
    /// Barrens: 40 m plate, walls 1 m inside the rim.
    /// </summary>
    public static readonly Zone[] Zones =
    {
        new Zone("Fen", Vector3.zero, 17.5f),
        new Zone("Ossuary", KilnLayout.OssuaryOffset, 14f),
        new Zone("Kiln", KilnLayout.KilnOffset, 14f),
        new Zone("Barrens", BarrensLayout.BarrensOffset, 19f),
    };

    /// <summary>Zone whose floor holds <paramref name="p"/>, else the one with the nearest centre.</summary>
    public static Zone ZoneAt(Vector3 p)
    {
        Zone best = Zones[0];
        float bestD = float.MaxValue;
        foreach (var z in Zones)
        {
            if (z.Contains(p)) return z;
            var d = p - z.center;
            d.y = 0f;
            if (d.sqrMagnitude < bestD) { bestD = d.sqrMagnitude; best = z; }
        }
        return best;
    }

    /// <summary>True when <paramref name="p"/> stands on some zone's walkable floor.</summary>
    public static bool IsWalkable(Vector3 p, float margin = 0f)
    {
        foreach (var z in Zones)
            if (z.Contains(p, margin)) return true;
        return false;
    }

    /// <summary>Clamp X/Z into the walkable rectangle of the zone around <paramref name="p"/>.</summary>
    public static Vector3 Clamp(Vector3 p, float margin = 0.4f)
    {
        var z = ZoneAt(p);
        float h = Mathf.Max(0f, z.halfExtent - margin);
        p.x = Mathf.Clamp(p.x, z.center.x - h, z.center.x + h);
        p.z = Mathf.Clamp(p.z, z.center.z - h, z.center.z + h);
        return p;
    }
}
