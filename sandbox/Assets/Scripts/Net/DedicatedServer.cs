using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Dedicated server mode (cinderreach-dedicated-server S1), started with <c>-server</c>
/// (alias <c>--net-server</c>). The server runs NGO without a hero of its own: the
/// living player with the lowest client id leads group travel, broadcasts ride any
/// spawned player object, nothing is saved on the server, and once a session that had
/// players runs empty the process quits so its supervisor restarts a fresh run.
/// Pure rules live here so EditMode tests pin them; <see cref="DedicatedServerHost"/>
/// applies them at runtime.
/// </summary>
public static class DedicatedServer
{
    public const string Flag = "-server";
    public const string LegacyFlag = "--net-server";
    /// <summary>Seconds an emptied server waits before quitting (a reconnect inside it keeps the run).</summary>
    public const float EmptyGraceSeconds = 20f;
    /// <summary>Frame cap: a headless server otherwise spins a core at thousands of fps.</summary>
    public const int ServerFrameRate = 60;

    /// <summary>True when the command line asks for a dedicated server.</summary>
    public static bool Requested(string[] args)
    {
        if (args == null) return false;
        foreach (var a in args)
            if (a == Flag || a == LegacyFlag) return true;
        return false;
    }

    static int requestedCache = -1;

    /// <summary>Process-level <see cref="Requested(string[])"/>, cached.</summary>
    public static bool RequestedByProcess
    {
        get
        {
            if (requestedCache < 0)
                requestedCache = Requested(System.Environment.GetCommandLineArgs()) ? 1 : 0;
            return requestedCache == 1;
        }
    }

    /// <summary>True while this process listens as a server without a local player (not a host).</summary>
    public static bool Active
    {
        get
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsListening) return nm.IsServer && !nm.IsHost;
            return RequestedByProcess;
        }
    }

    /// <summary>
    /// Leader choice: the living player with the lowest client id, else the lowest id at all
    /// (a downed party still travels with its first player). -1 when nobody is connected.
    /// </summary>
    public static int LeaderIndex(IList<ulong> clientIds, IList<bool> alive)
    {
        int best = -1, bestAny = -1;
        for (int i = 0; i < clientIds.Count; i++)
        {
            if (bestAny < 0 || clientIds[i] < clientIds[bestAny]) bestAny = i;
            bool living = alive == null || i >= alive.Count || alive[i];
            if (living && (best < 0 || clientIds[i] < clientIds[best])) best = i;
        }
        return best >= 0 ? best : bestAny;
    }

    /// <summary>
    /// Quit rule: only a server that has had players quits, and only after it stayed
    /// empty for <see cref="EmptyGraceSeconds"/>.
    /// </summary>
    public static bool ShouldQuit(bool hadPlayers, int connected, float emptySince, float now) =>
        hadPlayers && connected == 0 && emptySince >= 0f && now - emptySince >= EmptyGraceSeconds;

    /// <summary>The leading player's hero on the server, null when nobody is connected.</summary>
    public static Transform Leader()
    {
        var players = new List<DiabloNetworkPlayer>();
        foreach (var p in Object.FindObjectsByType<DiabloNetworkPlayer>(FindObjectsSortMode.None))
            if (p != null && p.IsSpawned) players.Add(p);
        if (players.Count == 0) return null;
        var ids = new List<ulong>(players.Count);
        var alive = new List<bool>(players.Count);
        foreach (var p in players)
        {
            ids.Add(p.OwnerClientId);
            var hp = p.GetComponent<PlayerHealth>();
            alive.Add(hp == null || hp.CurrentHP > 0f);
        }
        int i = LeaderIndex(ids, alive);
        return i >= 0 ? players[i].transform : null;
    }

    static bool difficultyAdopted;

    /// <summary>
    /// cinderreach-team-autoplay K3: the server has no save; the first joining hero's run
    /// difficulty decides the session (Kindled only when that hero unlocked it).
    /// </summary>
    public static string PartyDifficulty(string reported, bool unlocked) =>
        unlocked && string.Equals(reported, RunProfile.Kindled, System.StringComparison.OrdinalIgnoreCase)
            ? RunProfile.Kindled : RunProfile.Normal;

    /// <summary>Applies <see cref="PartyDifficulty"/> once per server run and rescales living enemies.</summary>
    public static void AdoptPartyDifficulty(string reported, bool unlocked, ulong sender)
    {
        if (difficultyAdopted) return;
        difficultyAdopted = true;
        string d = PartyDifficulty(reported, unlocked);
        if (d == RunProfile.Kindled) RunProfile.DifficultyUnlocked = true;
        DifficultyProfile.Select(d);
        int rescaled = EnemyHealth.RescaleAll();
        var p = DifficultyProfile.Current;
        Debug.Log($"[Difficulty] server adopts {p.name} from client {sender}: hp x{p.hpMul} damage x{p.damageMul} " +
                  $"gold x{p.goldMul} unique x{p.uniqueMul} affixTier {p.affixTier}, {rescaled} enemies rescaled.");
    }

    /// <summary>Any spawned player object (server broadcasts ride it when there is no host hero).</summary>
    public static DiabloNetworkPlayer AnyPlayer()
    {
        DiabloNetworkPlayer best = null;
        foreach (var p in Object.FindObjectsByType<DiabloNetworkPlayer>(FindObjectsSortMode.None))
            if (p != null && p.IsSpawned && (best == null || p.OwnerClientId < best.OwnerClientId)) best = p;
        return best;
    }
}
