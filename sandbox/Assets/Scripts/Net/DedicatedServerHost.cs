using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Runtime side of <see cref="DedicatedServer"/>: caps the frame rate, stands the
/// scene's solo hero down (the server has no hero), logs joins and leaves, and quits
/// once a session that had players stays empty (exit 0, the supervisor restarts a
/// fresh run).
/// </summary>
public sealed class DedicatedServerHost : MonoBehaviour
{
    public const string ReadyLog = "[Server] ready";

    bool hadPlayers;
    float emptySince = -1f;
    bool readyLogged;
    int lastCount = -1;
    float nextHeartbeat;
    bool quitting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!Application.isPlaying || !DedicatedServer.RequestedByProcess) return;
        if (FindAnyObjectByType<DedicatedServerHost>() != null) return;
        var go = new GameObject("DedicatedServerHost");
        DontDestroyOnLoad(go);
        go.AddComponent<DedicatedServerHost>();
    }

    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = DedicatedServer.ServerFrameRate;
        StandDownSoloHero();
    }

    System.Collections.IEnumerator QuitRoutine(NetworkManager nm)
    {
        // Unlist first (best effort, max 5 s) so no client picks a server that is going away.
        var unpublish = ServerDirectory.UnpublishAsync();
        float until = Time.realtimeSinceStartup + 5f;
        while (!unpublish.IsCompleted && Time.realtimeSinceStartup < until) yield return null;
        nm.Shutdown();
        Application.Quit(0);
    }

    /// <summary>Deactivate the scene's offline hero: on a server nobody owns it.</summary>
    public static GameObject StandDownSoloHero()
    {
        var solo = GameObject.Find(DiabloNetworkPlayer.HeroName);
        if (solo == null || solo.GetComponent<DiabloNetworkPlayer>() != null) return null;
        solo.SetActive(false);
        Debug.Log("[Server] solo hero stood down (dedicated server has no hero).");
        return solo;
    }

    void Update()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening || !nm.IsServer) return;
        if (!readyLogged)
        {
            readyLogged = true;
            Debug.Log($"{ReadyLog} (frame cap {DedicatedServer.ServerFrameRate}).");
        }
        if (ServerDirectory.Published && Time.realtimeSinceStartup >= nextHeartbeat)
        {
            nextHeartbeat = Time.realtimeSinceStartup + ServerDirectory.HeartbeatSeconds;
            _ = ServerDirectory.HeartbeatAsync();
        }
        if (quitting) return;
        int count = nm.ConnectedClientsIds.Count;
        if (count != lastCount)
        {
            Debug.Log($"[Server] players connected: {count}");
            lastCount = count;
        }
        if (count > 0)
        {
            hadPlayers = true;
            emptySince = -1f;
            return;
        }
        if (hadPlayers && emptySince < 0f) emptySince = Time.realtimeSinceStartup;
        if (DedicatedServer.ShouldQuit(hadPlayers, count, emptySince, Time.realtimeSinceStartup))
        {
            Debug.Log("[Server] session over, all players left: quitting for a fresh run.");
            hadPlayers = false;
            quitting = true;
            StartCoroutine(QuitRoutine(nm));
        }
    }
}
