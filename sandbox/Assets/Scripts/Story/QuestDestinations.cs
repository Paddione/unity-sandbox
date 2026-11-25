using System.Collections.Generic;
using UnityEngine;

public enum QuestMarkKind { Objective, TurnIn, Offer }

/// <summary>One place a tracked quest points at (map icon, tracker distance).</summary>
public struct QuestMark
{
    public string questId;
    public int objective;      // -1 for hand-ins and offers
    public bool main;
    public QuestMarkKind kind;
    public Vector3 position;
    /// <summary>Zone index (<see cref="ActProgress"/>) the place lies in.</summary>
    public int zone;
}

/// <summary>Scene lookups the resolver needs; tests pass a fake.</summary>
public interface IQuestWorld
{
    bool TryNpc(string npcId, out Vector3 position);
    void Pickups(string itemId, List<Vector3> into);
    void Enemies(string killTarget, List<Vector3> into);
    /// <summary>Gate standing in zone <paramref name="from"/> that leads into zone <paramref name="to"/>.</summary>
    bool TryGate(int from, int to, out Vector3 position);
}

/// <summary>
/// Where the tracked quests want the hero to go (cinderreach-group-quest-map).
/// Talk goals and hand-ins point at the NPC, finds at the remaining pickups,
/// kills at the nearest living targets, zone entries at the zone. A place in
/// another zone is routed to the gate in the hero's zone that leads toward it
/// (<see cref="Route"/>), so the tracker and the minimap always show the next step.
/// </summary>
public static class QuestDestinations
{
    /// <summary>Most kill targets marked per objective (nearest first).</summary>
    public const int MaxKillMarks = 3;

    static readonly List<Vector3> scratch = new List<Vector3>();

    /// <summary>Marks of every tracked quest plus every open offer.</summary>
    public static void Collect(StoryState s, IQuestWorld world, Vector3 hero, List<QuestMark> into)
    {
        into.Clear();
        if (s == null || world == null) return;
        foreach (var q in StoryTracking.Tracked(s, int.MaxValue))
        {
            if (s.Status(q.id) == StoryStatus.Ready)
            {
                if (world.TryNpc(q.giver, out var g)) into.Add(Mark(q, -1, QuestMarkKind.TurnIn, g));
                continue;
            }
            foreach (int i in StoryTracking.OpenObjectives(s, q))
                AddObjective(q, i, world, hero, into);
        }
        foreach (var q in StoryTracking.Offers(s))
            if (world.TryNpc(q.giver, out var p)) into.Add(Mark(q, -1, QuestMarkKind.Offer, p));
    }

    static void AddObjective(StoryQuest q, int i, IQuestWorld world, Vector3 hero, List<QuestMark> into)
    {
        var o = q.objectives[i];
        switch (o.type)
        {
            case StoryObjectiveType.Talk:
                if (world.TryNpc(o.target, out var npc)) into.Add(Mark(q, i, QuestMarkKind.Objective, npc));
                break;
            case StoryObjectiveType.Collect:
                scratch.Clear();
                world.Pickups(o.target, scratch);
                foreach (var p in scratch) into.Add(Mark(q, i, QuestMarkKind.Objective, p));
                break;
            case StoryObjectiveType.Kill:
                scratch.Clear();
                world.Enemies(o.target, scratch);
                scratch.Sort((a, b) => Flat(a - hero).CompareTo(Flat(b - hero)));
                for (int k = 0; k < scratch.Count && k < MaxKillMarks; k++)
                    into.Add(Mark(q, i, QuestMarkKind.Objective, scratch[k]));
                break;
            case StoryObjectiveType.Enter:
                int zone = ZoneMap.IndexOf(o.target);
                if (zone > 0) into.Add(Mark(q, i, QuestMarkKind.Objective, ZoneMap.Zone(zone).center, zone));
                break;
        }
    }

    static float Flat(Vector3 d) => d.x * d.x + d.z * d.z;

    static QuestMark Mark(StoryQuest q, int objective, QuestMarkKind kind, Vector3 at, int zone = 0) => new QuestMark
    {
        questId = q.id,
        objective = objective,
        main = q.main,
        kind = kind,
        position = at,
        zone = zone > 0 ? zone : ZoneMap.IndexAt(at),
    };

    /// <summary>
    /// Next step toward <paramref name="mark"/> for a hero in zone
    /// <paramref name="heroZone"/>: the mark itself in the same zone, else the
    /// gate toward it. False when no gate is known (the place is unreachable from here).
    /// </summary>
    public static bool Route(IQuestWorld world, int heroZone, QuestMark mark, out Vector3 step)
    {
        step = mark.position;
        if (mark.zone == heroZone) return true;
        int next = ZoneMap.NextHop(heroZone, mark.zone);
        return world != null && world.TryGate(heroZone, next, out step);
    }

    /// <summary>Nearest reachable step of quest <paramref name="questId"/> (tracker distance); false when none.</summary>
    public static bool NearestStep(IQuestWorld world, List<QuestMark> marks, string questId, Vector3 hero,
        out Vector3 step, out int targetZone)
    {
        step = default;
        targetZone = 0;
        int heroZone = ZoneMap.IndexAt(hero);
        float best = float.MaxValue;
        foreach (var m in marks)
        {
            if (m.questId != questId || m.kind == QuestMarkKind.Offer) continue;
            if (!Route(world, heroZone, m, out var s)) continue;
            // Same-zone places beat routed ones; within a class, the nearest wins.
            float d = Flat(s - hero) + (m.zone == heroZone ? 0f : 1e6f);
            if (d >= best) continue;
            best = d;
            step = s;
            targetZone = m.zone;
        }
        return best < float.MaxValue;
    }
}

/// <summary>Live scene answers for <see cref="QuestDestinations"/>, cached for half a second.</summary>
public sealed class SceneQuestWorld : IQuestWorld
{
    public const float CacheSeconds = 0.5f;

    float stamp = -1f;
    readonly List<StoryNpc> npcs = new List<StoryNpc>();
    readonly List<StoryPickup> pickups = new List<StoryPickup>();
    readonly List<EnemyHealth> enemies = new List<EnemyHealth>();
    readonly List<ZoneGate> gates = new List<ZoneGate>();

    void Refresh()
    {
        float now = Time.realtimeSinceStartup;
        if (stamp >= 0f && now - stamp < CacheSeconds) return;
        stamp = now;
        npcs.Clear(); npcs.AddRange(Object.FindObjectsByType<StoryNpc>(FindObjectsSortMode.None));
        pickups.Clear(); pickups.AddRange(Object.FindObjectsByType<StoryPickup>(FindObjectsSortMode.None));
        enemies.Clear(); enemies.AddRange(Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None));
        gates.Clear(); gates.AddRange(Object.FindObjectsByType<ZoneGate>(FindObjectsSortMode.None));
    }

    public bool TryNpc(string npcId, out Vector3 position)
    {
        Refresh();
        foreach (var n in npcs)
            if (n != null && n.npcId == npcId && n.isActiveAndEnabled)
            {
                // Merle only stands in the Hold after Act I (StoryNpc hides her body until then).
                if (npcId == StoryIds.Merle && !RunProfile.Act.ActOneComplete) break;
                position = n.transform.position;
                return true;
            }
        position = default;
        return false;
    }

    public void Pickups(string itemId, List<Vector3> into)
    {
        Refresh();
        foreach (var p in pickups)
            if (p != null && p.itemId == itemId && p.gameObject.activeInHierarchy) into.Add(p.transform.position);
    }

    public void Enemies(string killTarget, List<Vector3> into)
    {
        Refresh();
        foreach (var e in enemies)
            if (e != null && e.IsAlive && e.gameObject.activeInHierarchy
                && StoryIds.KillTarget(e.gameObject.name) == killTarget)
                into.Add(e.transform.position);
    }

    public bool TryGate(int from, int to, out Vector3 position)
    {
        Refresh();
        foreach (var g in gates)
            if (g != null && g.targetZone == to && ZoneMap.IndexAt(g.transform.position) == from)
            {
                position = g.transform.position;
                return true;
            }
        position = default;
        return false;
    }
}
