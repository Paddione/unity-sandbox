using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reload of the active zone scene ("New run", "New hero").
/// The NetworkManager lives in DontDestroyOnLoad. Left running across
/// LoadScene, the reloaded scene's solo hero yields its click controller to
/// netcode (<see cref="DiabloClickController.ShouldYieldToNetcode"/>) while
/// the old session never spawns a new player: no hero drives the camera and
/// the screen stays on the grey clear colour. The old manager therefore shuts
/// down and is destroyed before the load, so the scene's own NetworkManager
/// and <see cref="MultiplayerAutoStarter"/> boot exactly as on a fresh start.
/// <c>AfterSceneLoad</c> boots run once per process, so the scene-scoped ones
/// (pause menu, layout, party, join panel) run again after the load.
/// </summary>
public sealed class SceneReload : MonoBehaviour
{
    /// <summary>Upper bound for the netcode shutdown before the load goes ahead anyway.</summary>
    public const float ShutdownTimeoutSeconds = 5f;

    /// <summary>True from the request until the reloaded scene has booted.</summary>
    public static bool InProgress { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => InProgress = false;

    /// <summary>Reload the active scene after a full netcode teardown.</summary>
    public static void ReloadActiveScene()
    {
        if (InProgress) return;
        InProgress = true;
        var go = new GameObject("SceneReload");
        DontDestroyOnLoad(go);
        var runner = go.AddComponent<SceneReload>();
        runner.StartCoroutine(runner.Run(SceneManager.GetActiveScene().name));
    }

    IEnumerator Run(string sceneName)
    {
        yield return TearDownNetwork();
        SceneManager.sceneLoaded += OnLoaded;
        Debug.Log($"[Reload] Loading {sceneName} with a fresh netcode session.");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>Shut the persistent NetworkManager down and destroy it (test seam).</summary>
    public static IEnumerator TearDownNetwork()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) yield break;
        if (nm.IsListening && !nm.ShutdownInProgress) nm.Shutdown();
        float deadline = Time.realtimeSinceStartup + ShutdownTimeoutSeconds;
        while (nm != null && nm.ShutdownInProgress && Time.realtimeSinceStartup < deadline)
            yield return null;
        if (nm != null) Destroy(nm.gameObject);
        yield return null; // let OnDestroy clear the singleton before the next scene's manager enables
    }

    static void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnLoaded;
        InProgress = false;
        RebootSceneServices();
        var runner = FindFirstObjectByType<SceneReload>();
        if (runner != null) Destroy(runner.gameObject);
    }

    /// <summary>
    /// Re-run the scene-scoped boots that Unity only runs after the first scene
    /// load. Each boot guards itself against duplicates and wrong scenes.
    /// </summary>
    public static void RebootSceneServices()
    {
        PauseMenuUI.Boot();
        LayoutSeeder.Boot();
        PartyRoster.Boot();
        JoinPanel.Boot();
    }
}
