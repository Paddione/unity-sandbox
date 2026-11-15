using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// Q4 co-op entry point: player-hosted session without builds.
/// Main editor hosts; ParrelSync clones (dataPath contains "_clone")
/// or `--net-client` auto-connect. Default 127.0.0.1:7777; override with
/// `--net-address <host> [--net-port <port>]` for cross-machine play
/// (e.g. gpu-metal over tailnet).
/// No hard ParrelSync dependency — clone detection is path-based.
/// </summary>
public sealed class MultiplayerAutoStarter : MonoBehaviour
{
    public string address = "127.0.0.1";
    public ushort port = 7777;

    void Start()
    {
        var manager = NetworkManager.Singleton;
        if (manager == null)
        {
            Debug.LogWarning("[Net] No NetworkManager in scene; solo greybox only.");
            return;
        }

        string targetAddress = ArgValue("--net-address") ?? address;
        ushort targetPort = port;
        string portArg = ArgValue("--net-port");
        if (portArg != null && ushort.TryParse(portArg, out ushort parsedPort))
            targetPort = parsedPort;

        bool isClient = IsClone() || HasArg("--net-client");

        var transport = manager.GetComponent<UnityTransport>();
        if (transport != null)
        {
            // Host/server must listen on all interfaces (0.0.0.0), otherwise only
            // loopback clients can connect. Clients ignore the listen address.
            if (isClient)
                transport.SetConnectionData(targetAddress, targetPort);
            else
                transport.SetConnectionData(targetAddress, targetPort, "0.0.0.0");
            if (manager.NetworkConfig.NetworkTransport == null)
                manager.NetworkConfig.NetworkTransport = transport;
        }

        Debug.Log($"[Net] Scene: {SceneManager.GetActiveScene().name} (P2: Greybox + DiabloZone share this starter).");
        if (isClient)
        {
            Debug.Log($"[Net] Starting client -> {targetAddress}:{targetPort} (clone={IsClone()}).");
            manager.StartClient();
        }
        else if (HasArg("--net-server"))
        {
            Debug.Log("[Net] Starting server.");
            manager.StartServer();
        }
        else
        {
            Debug.Log($"[Net] Starting host on {targetAddress}:{targetPort}.");
            manager.StartHost();
        }
    }

    /// <summary>ParrelSync clones live next to the project as _clone_N.</summary>
    public static bool IsClone() =>
        Application.dataPath != null && Application.dataPath.Contains("_clone");

    public static bool IsClonePath(string dataPath) =>
        !string.IsNullOrEmpty(dataPath) && dataPath.Contains("_clone");

    static bool HasArg(string flag)
    {
        foreach (var a in System.Environment.GetCommandLineArgs())
            if (a == flag) return true;
        return false;
    }

    static string ArgValue(string flag)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == flag) return args[i + 1];
        return null;
    }
}
