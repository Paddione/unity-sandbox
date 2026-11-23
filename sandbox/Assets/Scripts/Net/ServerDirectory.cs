using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Internet reach for the dedicated server (cinderreach-dedicated-server S2) without open
/// ports. The Sessions SDK only starts NGO as a server for sessions without a player
/// (server authentication), and an anonymous server would become a host. So the server
/// talks to Relay and Lobby directly: it reserves a Relay allocation, starts NGO as a
/// server on it and publishes the Relay join code in a public lobby named
/// <see cref="LobbyName"/> (heartbeat every <see cref="HeartbeatSeconds"/> s). A client
/// queries that lobby, joins the Relay allocation by the published code and starts as a
/// client. Players never see a code.
/// </summary>
public static class ServerDirectory
{
    /// <summary>Default lobby name clients search for.</summary>
    public const string LobbyName = "Cinderreach Server";
    /// <summary>Command-line flag: other lobby name (test runs never meet the live server).</summary>
    public const string NameFlag = "-server-name";

    /// <summary>Lobby name from <c>-server-name</c>, else <see cref="LobbyName"/>.</summary>
    public static string NameFor(string[] args)
    {
        if (args != null)
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == NameFlag && !string.IsNullOrWhiteSpace(args[i + 1])) return args[i + 1].Trim();
        return LobbyName;
    }

    static string Name => NameFor(System.Environment.GetCommandLineArgs());
    /// <summary>Lobby data key holding the Relay join code.</summary>
    public const string JoinCodeKey = "relay";
    /// <summary>Lobby data key holding the build commit (clients warn on a mismatch).</summary>
    public const string BuildKey = "build";
    /// <summary>Lobbies without a heartbeat for 30 s vanish from queries.</summary>
    public const float HeartbeatSeconds = 15f;
    /// <summary>Relay transport protocol (UDP with DTLS encryption).</summary>
    public const string Protocol = "dtls";
    /// <summary>Command-line flag: join the published server (client).</summary>
    public const string JoinServerFlag = "-join-server";
    /// <summary>Command-line flag: publish this dedicated server through Relay + Lobby.</summary>
    public const string RelayFlag = "-server-relay";
    /// <summary>Log marker the pair harness and the pod readiness probe grep for.</summary>
    public const string PublishedLog = "[Server] published";

    static string lobbyId;

    /// <summary>Realtime of the last successful lobby heartbeat (publish counts), -1 before.</summary>
    public static float LastHeartbeatAt { get; private set; } = -1f;

    /// <summary>True while this server's lobby is listed.</summary>
    public static bool Published => !string.IsNullOrEmpty(lobbyId);

    /// <summary>Server lobby options: public, unlocked, code and build visible to everyone.</summary>
    public static CreateLobbyOptions LobbyOptions(string joinCode, string build) => new CreateLobbyOptions
    {
        IsPrivate = false,
        IsLocked = false,
        Data = new Dictionary<string, DataObject>
        {
            { JoinCodeKey, new DataObject(DataObject.VisibilityOptions.Public, joinCode) },
            { BuildKey, new DataObject(DataObject.VisibilityOptions.Public, build ?? "") },
        },
    };

    /// <summary>Client lobby query: our server by name, newest first.</summary>
    public static QueryLobbiesOptions Query(string name = LobbyName) => new QueryLobbiesOptions
    {
        Count = 5,
        Filters = new List<QueryFilter>
        {
            new QueryFilter(QueryFilter.FieldOptions.Name, name, QueryFilter.OpOptions.EQ),
        },
        Order = new List<QueryOrder> { new QueryOrder(false, QueryOrder.FieldOptions.Created) },
    };

    /// <summary>Join code from a lobby's public data (null when missing or blank).</summary>
    public static string CodeOf(Lobby lobby)
    {
        if (lobby?.Data == null || !lobby.Data.TryGetValue(JoinCodeKey, out var d)) return null;
        return string.IsNullOrWhiteSpace(d.Value) ? null : d.Value.Trim();
    }

    /// <summary>
    /// Server: Relay allocation for <see cref="RelayJoin.MaxPlayers"/>, NGO server on it,
    /// public lobby with the join code. False (logged) when any step failed.
    /// </summary>
    public static async Task<bool> PublishAsync(string build)
    {
        if (!await RelayJoin.EnsureUgsAsync()) return false;
        var nm = NetworkManager.Singleton;
        var transport = nm != null ? nm.GetComponent<UnityTransport>() : null;
        if (transport == null)
        {
            Debug.LogWarning("[Server] no UnityTransport, cannot publish via relay.");
            return false;
        }
        try
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync(RelayJoin.MaxPlayers);
            string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            transport.SetRelayServerData(allocation.ToRelayServerData(Protocol));
            if (nm.NetworkConfig.NetworkTransport == null) nm.NetworkConfig.NetworkTransport = transport;
            if (!nm.StartServer())
            {
                Debug.LogWarning("[Server] NGO server did not start on the relay allocation.");
                return false;
            }
            var lobby = await LobbyService.Instance.CreateLobbyAsync(Name, RelayJoin.MaxPlayers + 1,
                LobbyOptions(code, build));
            lobbyId = lobby.Id;
            LastHeartbeatAt = Time.realtimeSinceStartup;
            Debug.Log($"{PublishedLog} '{Name}' relay region {allocation.Region}, lobby {lobby.Id}, code {code}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Server] relay publish failed ({e.Message}).");
            return false;
        }
    }

    /// <summary>Server: keep the lobby listed. False when the heartbeat failed (logged).</summary>
    public static async Task<bool> HeartbeatAsync()
    {
        if (string.IsNullOrEmpty(lobbyId)) return false;
        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            LastHeartbeatAt = Time.realtimeSinceStartup;
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Server] lobby heartbeat failed ({e.Message}).");
            return false;
        }
    }

    /// <summary>Server: remove the lobby on the way out (best effort).</summary>
    public static async Task UnpublishAsync()
    {
        if (string.IsNullOrEmpty(lobbyId)) return;
        try { await LobbyService.Instance.DeleteLobbyAsync(lobbyId); }
        catch (System.Exception e) { Debug.LogWarning($"[Server] lobby delete failed ({e.Message})."); }
        lobbyId = null;
    }

    /// <summary>
    /// Client: find the published server, join its Relay allocation and start NGO as a client.
    /// Returns null on success, else a short reason for the player.
    /// </summary>
    public static async Task<string> JoinAsync(string build)
    {
        if (!await RelayJoin.EnsureUgsAsync()) return "online services unreachable";
        var nm = NetworkManager.Singleton;
        var transport = nm != null ? nm.GetComponent<UnityTransport>() : null;
        if (transport == null) return "no network transport";
        try
        {
            var found = await LobbyService.Instance.QueryLobbiesAsync(Query(Name));
            string code = null;
            Lobby pick = null;
            foreach (var l in found.Results)
            {
                code = CodeOf(l);
                if (code != null) { pick = l; break; }
            }
            if (code == null) return "server offline";
            string serverBuild = pick.Data.TryGetValue(BuildKey, out var b) ? b.Value : "";
            if (!string.IsNullOrEmpty(build) && !string.IsNullOrEmpty(serverBuild) && serverBuild != build)
                Debug.LogWarning($"[Server] build mismatch: server {serverBuild}, client {build}.");
            var join = await RelayService.Instance.JoinAllocationAsync(code);
            transport.SetRelayServerData(join.ToRelayServerData(Protocol));
            if (nm.NetworkConfig.NetworkTransport == null) nm.NetworkConfig.NetworkTransport = transport;
            if (!nm.StartClient()) return "client did not start";
            Debug.Log($"[Net] joining Cinderreach server via relay (lobby {pick.Id}).");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Net] join server failed ({e.Message}).");
            return "join failed";
        }
    }
}
