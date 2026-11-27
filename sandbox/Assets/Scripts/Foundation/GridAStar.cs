using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Autoplay-only grid A* (8-neighbour, no corner cutting). Pure: the caller
/// supplies walkability, so EditMode tests pin it without physics. The start
/// cell always counts as walkable (the hero may overlap a wall edge).
/// </summary>
public static class GridAStar
{
    static readonly Vector2Int[] Steps =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
        new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
    };

    /// <summary>
    /// Cells from <paramref name="start"/> to the first cell that satisfies
    /// <paramref name="isGoal"/> (both inclusive), or null when none is found
    /// within <paramref name="maxExpanded"/> expansions.
    /// </summary>
    public static List<Vector2Int> Find(Vector2Int start, Vector2Int target, Func<Vector2Int, bool> isGoal,
        Func<Vector2Int, bool> walkable, int maxExpanded = 20000)
    {
        var open = new SortedSet<(float f, int seq, Vector2Int c)>(Comparer<(float f, int seq, Vector2Int c)>.Create(
            (a, b) => a.f != b.f ? a.f.CompareTo(b.f) : a.seq.CompareTo(b.seq)));
        var g = new Dictionary<Vector2Int, float> { [start] = 0f };
        var from = new Dictionary<Vector2Int, Vector2Int>();
        var closed = new HashSet<Vector2Int>();
        int seq = 0;
        open.Add((H(start, target), seq++, start));
        int expanded = 0;
        while (open.Count > 0 && expanded < maxExpanded)
        {
            var cur = open.Min;
            open.Remove(cur);
            var c = cur.c;
            if (!closed.Add(c)) continue;
            if (isGoal(c)) return Rebuild(from, c);
            expanded++;
            foreach (var s in Steps)
            {
                var n = c + s;
                if (closed.Contains(n) || !walkable(n)) continue;
                // Diagonal only when both orthogonal neighbours are free (no clipping wall corners).
                if (s.x != 0 && s.y != 0
                    && (!walkable(new Vector2Int(c.x + s.x, c.y)) || !walkable(new Vector2Int(c.x, c.y + s.y))))
                    continue;
                float ng = g[c] + (s.x != 0 && s.y != 0 ? 1.41421356f : 1f);
                if (g.TryGetValue(n, out float old) && old <= ng) continue;
                g[n] = ng;
                from[n] = c;
                open.Add((ng + H(n, target), seq++, n));
            }
        }
        return null;
    }

    static float H(Vector2Int a, Vector2Int b)
    {
        float dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) + 0.41421356f * Mathf.Min(dx, dy);
    }

    static List<Vector2Int> Rebuild(Dictionary<Vector2Int, Vector2Int> from, Vector2Int end)
    {
        var path = new List<Vector2Int> { end };
        while (from.TryGetValue(path[path.Count - 1], out var prev)) path.Add(prev);
        path.Reverse();
        return path;
    }

    /// <summary>
    /// String-pulls a cell path: keeps a cell only where the straight line
    /// from the last kept cell to the next one is not clear.
    /// </summary>
    public static List<Vector2Int> Smooth(List<Vector2Int> path, Func<Vector2Int, Vector2Int, bool> clear)
    {
        if (path == null || path.Count <= 2) return path;
        var outPath = new List<Vector2Int> { path[0] };
        int anchor = 0;
        for (int i = 2; i < path.Count; i++)
        {
            if (clear(path[anchor], path[i])) continue;
            outPath.Add(path[i - 1]);
            anchor = i - 1;
        }
        outPath.Add(path[path.Count - 1]);
        return outPath;
    }

    /// <summary>True when every cell sampled along a→b (quarter-cell steps) is walkable.</summary>
    public static bool LineClear(Vector2 a, Vector2 b, Func<Vector2Int, bool> walkable)
    {
        float len = Vector2.Distance(a, b);
        int n = Mathf.Max(1, Mathf.CeilToInt(len * 4f));
        for (int i = 1; i <= n; i++)
        {
            var p = Vector2.Lerp(a, b, i / (float)n);
            if (!walkable(new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y)))) return false;
        }
        return true;
    }
}
