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
            nm.Shutdown();
            Application.Quit(0);
        }
    }
}
