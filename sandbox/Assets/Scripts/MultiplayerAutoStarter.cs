using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// Q4 co-op entry point: player-hosted session without builds.
/// Start parameters come from <see cref="JoinArgs"/>: <c>-host [-port N]</c>,
/// <c>-join ip[:port]</c>, the P2 flags (<c>--net-client</c>,
/// <c>--net-address</c>, <c>--net-port</c>, <c>--net-server</c>) and ParrelSync
/// clones (dataPath contains "_clone"). No flag hosts on 127.0.0.1:7777
/// (solo = host nobody joined). S6 adds <c>-host-relay</c> and
/// <c>-join-code CODE</c>: the Sessions SDK then owns transport setup and the
/// NGO start, with a loud fallback to direct UTP when UGS is unreachable.
/// <see cref="Restart"/> switches mode at runtime (<see cref="JoinPanel"/>).
/// </summary>
public sealed class MultiplayerAutoStarter : MonoBehaviour
{
    public string address = JoinArgs.DefaultAddress;
    public ushort port = JoinArgs.DefaultPort;

    /// <summary>Mode this process started or last restarted with.</summary>
    public static JoinArgs Current { get; private set; } =
        new JoinArgs(JoinArgs.NetMode.Host, JoinArgs.DefaultAddress, JoinArgs.DefaultPort);

    static MultiplayerAutoStarter instance;

    void Awake() => instance = this;

    void Start()
    {
        var args = JoinArgs.Parse(System.Environment.GetCommandLineArgs(), IsClone());
        if (args.Warning != null) Debug.LogWarning($"[Net] {args.Warning}");
        Debug.Log($"[Net] Scene: {SceneManager.GetActiveScene().name} (P2: Greybox + DiabloZone share this starter).");
        Begin(args);
    }

    /// <summary>Relay boot budget (UGS + allocation before direct fallback).</summary>
    public const float RelayBootSeconds = 90f;

    /// <summary>Apply transport data and start host, client or server.</summary>
    static bool Begin(JoinArgs args)
    {
        if (args.RelayHost || args.JoinCode != null)
        {
            var manager0 = NetworkManager.Singleton;
            if (manager0 == null)
            {
                Debug.LogWarning("[Net] No NetworkManager in scene; solo greybox only.");
                return false;
            }
            Current = args;
            Debug.Log(args.RelayHost
                ? "[Net] Starting host via relay (code follows)."
                : $"[Net] Joining via relay code {args.JoinCode}.");
            instance.StartCoroutine(instance.RelayBoot(args));
            return true;
        }
        return DirectBegin(args);
    }

    /// <summary>Direct UTP start (S1 path, also the relay fallback).</summary>
    static bool DirectBegin(JoinArgs args)
    {
        var manager = NetworkManager.Singleton;
        if (manager == null)
        {
            Debug.LogWarning("[Net] No NetworkManager in scene; solo greybox only.");
            return false;
        }
        var transport = manager.GetComponent<UnityTransport>();
        if (transport != null)
        {
            // Host/server must listen on all interfaces (0.0.0.0), otherwise only
            // loopback clients can connect. Clients ignore the listen address.
            if (args.IsClient)
                transport.SetConnectionData(args.Address, args.Port);
            else
                transport.SetConnectionData(args.Address, args.Port, "0.0.0.0");
            if (manager.NetworkConfig.NetworkTransport == null)
                manager.NetworkConfig.NetworkTransport = transport;
        }
        Current = args;
        switch (args.Mode)
        {
            case JoinArgs.NetMode.Client:
                Debug.Log($"[Net] Starting client -> {args.Address}:{args.Port} (clone={IsClone()}).");
                return manager.StartClient();
            case JoinArgs.NetMode.Server:
                Debug.Log("[Net] Starting server.");
                return manager.StartServer();
            default:
                Debug.Log($"[Net] Starting host on {args.Address}:{args.Port}.");
                return manager.StartHost();
        }
    }

    /// <summary>
    /// Leave the current session and start again as host or client (JoinPanel).
    /// Returns false when there is no starter in the scene.
    /// </summary>
    public static bool Restart(JoinArgs args)
    {
        if (instance == null) return false;
        instance.StartCoroutine(instance.RestartRoutine(args));
        return true;
    }

    IEnumerator RestartRoutine(JoinArgs args)
    {
        var manager = NetworkManager.Singleton;
        if (manager == null) yield break;
        if (manager.IsListening || manager.ShutdownInProgress)
        {
            Debug.Log($"[Net] Leaving {Current} for {args}.");
            manager.Shutdown();
            while (manager.ShutdownInProgress) yield return null;
        }
        yield return null;
        Begin(args);
    }

    /// <summary>
    /// Relay boot (S6): UGS session first; the SDK configures the transport
    /// and starts NGO itself. On failure or timeout the direct UTP path runs
    /// instead — loudly, never silently.
    /// </summary>
    IEnumerator RelayBoot(JoinArgs args)
    {
        var manager = NetworkManager.Singleton;
        if (manager == null) yield break;
        float deadline = Time.realtimeSinceStartup + RelayBootSeconds;
        if (args.RelayHost)
        {
            var task = RelayJoin.HostWithRelayAsync();
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            string code = task.IsCompletedSuccessfully ? task.Result : null;
            if (!string.IsNullOrEmpty(code))
            {
                Debug.Log($"[Net] {RelayJoin.JoinCodeLogPrefix}{code}");
                yield break; // session owns NGO from here
            }
            Debug.LogWarning("[Net] Relay host failed, falling back to direct UTP.");
            DirectBegin(new JoinArgs(JoinArgs.NetMode.Host, args.Address, args.Port));
            yield break;
        }
        var join = RelayJoin.JoinWithCodeAsync(args.JoinCode ?? "");
        while (!join.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
        bool ok = join.IsCompletedSuccessfully && join.Result;
        if (ok) yield break; // session owns NGO from here
        Debug.LogWarning("[Net] Relay join failed, falling back to direct UTP.");
        DirectBegin(new JoinArgs(JoinArgs.NetMode.Client, args.Address, args.Port));
    }

    /// <summary>ParrelSync clones live next to the project as _clone_N.</summary>
    public static bool IsClone() =>
        Application.dataPath != null && Application.dataPath.Contains("_clone");

    public static bool IsClonePath(string dataPath) =>
        !string.IsNullOrEmpty(dataPath) && dataPath.Contains("_clone");
}
