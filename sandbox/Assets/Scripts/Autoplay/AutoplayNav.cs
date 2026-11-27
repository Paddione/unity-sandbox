using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Autoplay-only terrain navigation (the game itself has no NavMesh: the
/// hero walks straight lines with a CharacterController). Walkability of a
/// 0.5 m cell is probed lazily with a hero-sized capsule against static
/// colliders (triggers, characters and loose rigidbodies ignored) and cached
/// per run; <see cref="GridAStar"/> plans around walls, pillars and props.
/// Cells the hero actually stuck on are learned as blocked.
/// </summary>
public sealed class AutoplayNav
{
    public const float CellSize = 0.5f;
    /// <summary>Hero CharacterController radius 0.35 m plus clearance.</summary>
    public const float ProbeRadius = 0.42f;
    const float ProbeLow = 0.8f, ProbeHigh = 1.5f;

    readonly Dictionary<Vector2Int, bool> cache = new Dictionary<Vector2Int, bool>();
    readonly HashSet<Vector2Int> learned = new HashSet<Vector2Int>();
    readonly Collider[] hits = new Collider[16];
    readonly Transform self;

    public int Plans { get; private set; }
    public int Failures { get; private set; }

    public AutoplayNav(Transform hero) { self = hero; }

    public static Vector2Int CellOf(Vector3 p) =>
        new Vector2Int(Mathf.RoundToInt(p.x / CellSize), Mathf.RoundToInt(p.z / CellSize));

    public static Vector3 PosOf(Vector2Int c, float y = 0f) => new Vector3(c.x * CellSize, y, c.y * CellSize);

    public bool Walkable(Vector2Int c)
    {
        if (learned.Contains(c)) return false;
        if (cache.TryGetValue(c, out bool ok)) return ok;
        ok = Probe(PosOf(c));
        cache[c] = ok;
        return ok;
    }

    public bool Walkable(Vector3 p) => Walkable(CellOf(p));

    bool Probe(Vector3 p)
    {
        if (!WorldBounds.IsWalkable(p, 0.3f)) return false;
        int n = Physics.OverlapCapsuleNonAlloc(new Vector3(p.x, ProbeLow, p.z), new Vector3(p.x, ProbeHigh, p.z),
            ProbeRadius, hits, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
            if (IsStatic(hits[i])) return false;
        return true;
    }

    bool IsStatic(Collider c)
    {
        if (c == null) return false;
        if (self != null && c.transform.IsChildOf(self)) return false;
        var rb = c.attachedRigidbody;
        if (rb != null && !rb.isKinematic) return false;
        if (c is CharacterController) return false;
        if (c.GetComponentInParent<EnemyHealth>() != null) return false;
        if (c.GetComponentInParent<PlayerHealth>() != null) return false;
        if (c.GetComponentInParent<CharacterController>() != null) return false;
        return true;
    }

    /// <summary>Marks the cell ahead of a stuck hero as blocked (geometry the probe missed).</summary>
    public void LearnBlocked(Vector3 hero, Vector3 towards)
    {
        var d = towards - hero;
        d.y = 0f;
        if (d.sqrMagnitude < 0.01f) return;
        var ahead = CellOf(hero + d.normalized * (CellSize * 1.5f));
        if (ahead != CellOf(hero)) learned.Add(ahead);
    }

    public bool LineClear(Vector3 a, Vector3 b) =>
        GridAStar.LineClear(new Vector2(a.x, a.z) / CellSize, new Vector2(b.x, b.z) / CellSize, Walkable);

    /// <summary>
    /// Waypoints (world, y of <paramref name="from"/>) ending within
    /// <paramref name="accept"/> metres of <paramref name="to"/>; null when no path exists.
    /// </summary>
    public List<Vector3> FindPath(Vector3 from, Vector3 to, float accept)
    {
        Plans++;
        var start = CellOf(from);
        var goal = CellOf(to);
        float acceptCells = Mathf.Max(0.5f, accept / CellSize);
        bool Goal(Vector2Int c) => (c - goal).sqrMagnitude <= acceptCells * acceptCells;
        bool Free(Vector2Int c) => c == start || Walkable(c);
        var cells = GridAStar.Find(start, goal, Goal, Free);
        if (cells == null)
        {
            Failures++;
            return null;
        }
        cells = GridAStar.Smooth(cells, (a, b) => GridAStar.LineClear(a, b, Free));
        var path = new List<Vector3>(cells.Count);
        for (int i = 1; i < cells.Count; i++) path.Add(PosOf(cells[i], from.y));
        // Finish on the real target when it is reachable in a straight line.
        if (path.Count > 0 && LineClear(path[path.Count - 1], to)) path[path.Count - 1] = new Vector3(to.x, from.y, to.z);
        if (path.Count == 0) path.Add(new Vector3(to.x, from.y, to.z));
        return path;
    }

    /// <summary>Nearest walkable cell centre around <paramref name="p"/> (spiral, up to 4 m), else p.</summary>
    public Vector3 NearestWalkable(Vector3 p)
    {
        var c = CellOf(p);
        if (Walkable(c)) return PosOf(c, p.y);
        for (int r = 1; r <= 8; r++)
            for (int dx = -r; dx <= r; dx++)
                for (int dz = -r; dz <= r; dz++)
                {
                    if (Mathf.Abs(dx) != r && Mathf.Abs(dz) != r) continue;
                    var n = new Vector2Int(c.x + dx, c.y + dz);
                    if (Walkable(n)) return PosOf(n, p.y);
                }
        return p;
    }
}

/// <summary>
/// Steers the autoplay hero along <see cref="AutoplayNav"/> paths and gets it
/// unstuck. Replans when the goal moves or progress stalls; after repeated
/// stalls it learns the blocking cell, and as a last resort warps the hero
/// onto the next validated waypoint (never blindly into geometry).
/// </summary>
public sealed class AutoplayMover
{
    const float WaypointReach = 0.45f;
    const float StallWindow = 1.5f;
    const float StallDistance = 0.35f;

    readonly AutoplayNav nav;
    List<Vector3> path;
    int index;
    Vector3 plannedGoal;
    float replanAt;
    Vector3 stallPos;
    float stallT;
    int stalls;
    float lastTick;

    public int Warps { get; private set; }
    public int Replans { get; private set; }
    public AutoplayNav Nav => nav;

    public AutoplayMover(AutoplayNav nav) { this.nav = nav; }

    /// <summary>Forget the current path (new leg, teleport).</summary>
    public void Reset()
    {
        path = null;
        stalls = 0;
        stallT = Time.realtimeSinceStartup;
    }

    /// <summary>
    /// One frame toward <paramref name="goal"/>. With <paramref name="target"/>
    /// set, the last leg is handed to <see cref="DiabloClickController.AttackTarget"/>
    /// (approach + swing). True once within <paramref name="arrive"/> metres.
    /// <paramref name="planAccept"/> (default: arrive) widens the planner's goal
    /// to free cells around a target that sits on or in a prop (story finds).
    /// </summary>
    public bool Tick(DiabloClickController hero, Vector3 goal, float arrive, GameObject target = null,
        float planAccept = 0f)
    {
        var pos = hero.transform.position;
        if (Planar(goal - pos) <= arrive)
        {
            stalls = 0;
            return true;
        }
        float now = Time.realtimeSinceStartup;
        if (now - lastTick > 0.5f)
        {
            // Resumed after a pause (in-range swings, dialog): restart the stall window.
            stallPos = pos;
            stallT = now;
        }
        lastTick = now;
        float accept = Mathf.Max(arrive, planAccept);
        if (path == null || Planar(goal - plannedGoal) > 1.5f || now >= replanAt) Plan(pos, goal, accept, now);

        while (index < path.Count - 1 && (Planar(path[index] - pos) <= WaypointReach || nav.LineClear(pos, path[index + 1])))
            index++;
        bool lastLeg = index >= path.Count - 1;
        if (lastLeg && target != null) hero.AttackTarget(target);
        else hero.SetDestination(path[index]);

        if (now - stallT >= StallWindow)
        {
            float moved = Planar(pos - stallPos);
            stallPos = pos;
            stallT = now;
            if (moved >= StallDistance) stalls = 0;
            else Unstick(hero, pos, goal, accept, now);
        }
        return false;
    }

    void Plan(Vector3 pos, Vector3 goal, float arrive, float now)
    {
        plannedGoal = goal;
        replanAt = now + 2f;
        index = 0;
        if (nav.LineClear(pos, goal)) path = new List<Vector3> { goal };
        else path = nav.FindPath(pos, goal, arrive) ?? new List<Vector3> { goal };
        Replans++;
    }

    void Unstick(DiabloClickController hero, Vector3 pos, Vector3 goal, float arrive, float now)
    {
        stalls++;
        var next = path != null && index < path.Count ? path[index] : goal;
        nav.LearnBlocked(pos, next);
        if (stalls < 3)
        {
            Plan(pos, goal, arrive, now);
            Debug.Log($"[Autoplay] nav stall #{stalls}: replanned ({path.Count} legs)");
            return;
        }
        // Last resort: step onto the next validated waypoint (or the nearest free cell toward it).
        var dir = next - pos;
        dir.y = 0f;
        var hop = nav.NearestWalkable(dir.magnitude > 2.5f ? pos + dir.normalized * 2.5f : next);
        hop.y = pos.y;
        var cc = hero.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        hero.transform.position = hop;
        if (cc != null) cc.enabled = true;
        Warps++;
        stalls = 0;
        Plan(hop, goal, arrive, now);
        Debug.Log($"[Autoplay] nav warp #{Warps} to ({hop.x:0.0},{hop.z:0.0})");
    }

    static float Planar(Vector3 d) => new Vector2(d.x, d.z).magnitude;
}
