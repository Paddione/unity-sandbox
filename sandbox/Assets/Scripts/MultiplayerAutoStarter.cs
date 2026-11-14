using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// Q4 co-op entry point: player-hosted session without builds.
/// Main editor hosts; ParrelSync clones (dataPath contains "_clone")
/// or `--net-client` auto-connect to 127.0.0.1:7777.
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

        var transport = manager.GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.SetConnectionData(address, port);
            if (manager.NetworkConfig.NetworkTransport == null)
                manager.NetworkConfig.NetworkTransport = transport;
        }

        if (IsClone() || HasArg("--net-client"))
        {
            Debug.Log($"[Net] Starting client -> {address}:{port} (clone={IsClone()}).");
            manager.StartClient();
        }
        else if (HasArg("--net-server"))
        {
            Debug.Log("[Net] Starting server.");
            manager.StartServer();
        }
        else
        {
            Debug.Log($"[Net] Starting host on {address}:{port}.");
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
}
