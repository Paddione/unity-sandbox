using System.Collections.Generic;
using UnityEngine;

/// <summary>One voiced line outside the dialog window: boss words in a fight, narration on a story beat.</summary>
public sealed class StoryBark
{
    public readonly string id;
    /// <summary>Speaker id (npc.&lt;id&gt;.name) or "n" for narration.</summary>
    public readonly string speaker;

    public StoryBark(string id, string speaker)
    {
        this.id = id;
        this.speaker = speaker;
    }

    public string Key => "bark." + id;
}

/// <summary>
/// Barks (cinderreach-story-voice): the bosses speak the lines the story bible
/// gives them, and two story beats without a talk get a narrated line. Callers
/// fire <see cref="Say"/>; the local <see cref="StoryDirector"/> shows the
/// subtitle and plays the clip. The same bark repeats at most every
/// <see cref="RepeatSeconds"/>, so a phase change replicated twice stays one line.
/// </summary>
public static class StoryBarks
{
    public const string Abbot = "abbot", Kilnmother = "kilnmother", Glasswright = "glasswright";
    public const float RepeatSeconds = 20f;

    public static readonly StoryBark[] All =
    {
        new StoryBark("abbot.phase2", Abbot),
        new StoryBark("abbot.death", Abbot),
        new StoryBark("kilnmother.phase2", Kilnmother),
        new StoryBark("kilnmother.death", Kilnmother),
        new StoryBark("glasswright.phase2", Glasswright),
        new StoryBark("glasswright.death", Glasswright),
        new StoryBark("h4.start", "n"),
        new StoryBark("h7.done", "n"),
        // Akt I Vertiefung S3: Ofen/Wacht — Erzähler bei Jorins Strang (Spitzen,
        // Knechte). Laufen über OnKill, gedrosselt wie Bossrufe.
        new StoryBark("lancer.fall", "n"),
        new StoryBark("thrall.fall", "n"),
    };

    static readonly Queue<StoryBark> pending = new Queue<StoryBark>();
    static readonly Dictionary<string, float> lastSaid = new Dictionary<string, float>();

    /// <summary>Test counter: barks accepted by <see cref="Say"/>.</summary>
    public static int said;

    public static StoryBark Get(string id)
    {
        foreach (var b in All) if (b.id == id) return b;
        return null;
    }

    /// <summary>
    /// Co-op seam (cinderreach-group-quest-map): set by the network layer, called
    /// for every bark this process accepts; the host forwards it to all clients,
    /// so everyone hears the boss, not only who received the phase event.
    /// </summary>
    public static System.Action<string> Relay;

    /// <summary>Queues bark <paramref name="id"/> unless it was said within <see cref="RepeatSeconds"/>.</summary>
    public static bool Say(string id)
    {
        if (!Accept(id)) return false;
        Relay?.Invoke(id);
        return true;
    }

    /// <summary>A bark the host relayed: queued like <see cref="Say"/> but never sent on.</summary>
    public static bool SayRemote(string id)
    {
        relayed++;
        Debug.Log($"[Story] bark {id} relayed by the host.");
        return Accept(id);
    }

    /// <summary>Test counter: barks that arrived from the host.</summary>
    public static int relayed;

    static bool Accept(string id)
    {
        var bark = Get(id);
        if (bark == null) return false;
        float now = Time.realtimeSinceStartup;
        if (lastSaid.TryGetValue(id, out var t) && now - t < RepeatSeconds) return false;
        lastSaid[id] = now;
        said++;
        if (pending.Count < 4) pending.Enqueue(bark);
        return true;
    }

    /// <summary>Death bark for a boss kill target (<see cref="StoryIds.KillTarget"/> form).</summary>
    public static void OnKill(string killTarget)
    {
        if (killTarget == StoryIds.Abbot) Say("abbot.death");
        else if (killTarget == StoryIds.Kilnmother) Say("kilnmother.death");
        else if (killTarget == StoryIds.Glasswright) Say("glasswright.death");
        else if (killTarget == StoryIds.AshLancer) Say("lancer.fall");
        else if (killTarget == StoryIds.CinderThrall) Say("thrall.fall");
    }

    public static bool TryTake(out StoryBark bark)
    {
        if (pending.Count > 0) { bark = pending.Dequeue(); return true; }
        bark = null;
        return false;
    }

    public static void ResetForTests()
    {
        pending.Clear();
        lastSaid.Clear();
        said = 0;
        relayed = 0;
    }
}
