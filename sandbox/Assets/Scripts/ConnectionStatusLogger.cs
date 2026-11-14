using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Server-operator observability: who connected, who left, when serving starts.
/// This is what makes a remote join verifiable in a headless server log.
/// </summary>
public sealed class ConnectionStatusLogger : MonoBehaviour
{
    void Start()
    {
        var manager = NetworkManager.Singleton;
        if (manager == null)
        {
            Debug.LogWarning("[Net] No NetworkManager for connection logging.");
            return;
        }
        manager.OnServerStarted += () => Debug.Log("[Net] Server started, listening for clients.");
        manager.OnClientConnectedCallback += id =>
            Debug.Log($"[Net] Client connected: {id} (total={manager.ConnectedClients.Count}).");
        manager.OnClientDisconnectCallback += id =>
            Debug.Log($"[Net] Client disconnected: {id} (total={manager.ConnectedClients.Count}).");
    }
}
