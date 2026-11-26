using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// "New hero" / "New run" reload: the persistent NetworkManager must be gone
/// before the scene loads (else the reloaded solo hero yields its controller to
/// netcode that never spawns a player: frozen camera, grey screen), and the
/// scene-scoped boots that Unity runs once per process come back.
/// </summary>
public class SceneReloadTests
{
    GameObject netGo;
    GameObject heroGo;

    [TearDown]
    public void TearDown()
    {
        if (netGo != null) Object.Destroy(netGo);
        if (heroGo != null) Object.Destroy(heroGo);
        foreach (var p in Object.FindObjectsByType<PauseMenuUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.Destroy(p.gameObject);
    }

    [UnityTest]
    public IEnumerator TearDownNetwork_StopsAndRemovesTheRunningManager()
    {
        netGo = new GameObject("ReloadTestNet");
        var manager = netGo.AddComponent<NetworkManager>();
        var transport = netGo.AddComponent<UnityTransport>();
        transport.SetConnectionData("127.0.0.1", 7791);
        manager.NetworkConfig = new NetworkConfig { NetworkTransport = transport, EnableSceneManagement = false };
        Assert.IsTrue(manager.StartHost(), "host starts");
        yield return null;
        Assert.AreSame(manager, NetworkManager.Singleton);
        Assert.IsTrue(manager.IsListening);

        yield return SceneReload.TearDownNetwork();

        Assert.IsTrue(manager == null, "the old manager is destroyed, so the reloaded scene boots its own");
        Assert.IsNull(NetworkManager.Singleton, "no stale singleton makes the new solo hero yield its controller");
        netGo = null;
    }

    [UnityTest]
    public IEnumerator TearDownNetwork_WithoutNetcodeIsANoOp()
    {
        Assert.IsNull(NetworkManager.Singleton);
        yield return SceneReload.TearDownNetwork();
        Assert.IsNull(NetworkManager.Singleton);
    }

    [UnityTest]
    public IEnumerator RebootSceneServices_BringsThePauseMenuBack()
    {
        heroGo = new GameObject(DiabloNetworkPlayer.HeroName);
        heroGo.AddComponent<ClassAppearance>();
        foreach (var p in Object.FindObjectsByType<PauseMenuUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(p.gameObject);
        Assert.IsNull(Object.FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include), "the reload destroyed it");

        SceneReload.RebootSceneServices();
        yield return null;
        Assert.IsNotNull(Object.FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include), "Esc works again after a new hero");

        SceneReload.RebootSceneServices();
        yield return null;
        Assert.AreEqual(1, Object.FindObjectsByType<PauseMenuUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
            "boots stay idempotent");
    }
}
