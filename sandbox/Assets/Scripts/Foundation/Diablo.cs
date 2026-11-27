using Unity.Netcode;

/// <summary>Shared netcode-mode predicates for local-only feel effects.</summary>
public static class Diablo
{
    public static bool NetcodeListening()
    {
        var nm = NetworkManager.Singleton;
        return nm != null && nm.IsListening;
    }

    /// <summary>Offline or host: this process simulates and renders the world.</summary>
    public static bool IsLocalAuthority()
    {
        var nm = NetworkManager.Singleton;
        return nm == null || !nm.IsListening || nm.IsHost;
    }

    /// <summary>Offline or a host nobody else joined (time effects stay private).</summary>
    public static bool IsSolo()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening) return true;
        return nm.IsHost && nm.ConnectedClientsIds.Count <= 1;
    }
}
