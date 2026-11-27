using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// cinderreach-hud-redesign in co-op: a joined hero shows up in the party
/// frames with name, class and level, and its life bar follows the replicated
/// HP after it takes damage.
/// </summary>
public sealed class PartyFramesPlayTests
{
    readonly List<GameObject> spawned = new List<GameObject>();

    [SetUp]
    public void Setup() => RunProfile.Reset();

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (var go in spawned)
        {
            if (go == null) continue;
            var nm = go.GetComponent<NetworkManager>();
            if (nm != null) nm.Shutdown();
        }
        yield return null;
        foreach (var go in spawned) if (go != null) Object.Destroy(go);
        spawned.Clear();
        if (DiabloLifeGlobe.Instance != null) Object.Destroy(DiabloLifeGlobe.Instance.gameObject);
        RunProfile.Reset();
        yield return null;
    }

    NetworkManager NewManager(string label, ushort port)
    {
        var go = new GameObject(label);
        spawned.Add(go);
        var manager = go.AddComponent<NetworkManager>();
        var transport = go.AddComponent<UnityTransport>();
        transport.SetConnectionData("127.0.0.1", port);
        manager.NetworkConfig = new NetworkConfig
        {
            NetworkTransport = transport,
            EnableSceneManagement = false,
            PlayerPrefab = Res.Get<GameObject>("NetworkPlayerDiablo"),
        };
        DiabloNetworkBootstrap.RegisterLootPrefab(manager);
        return manager;
    }

    [UnityTest]
    public IEnumerator Coop_JoinedHeroGetsAFrameThatFollowsItsHp()
    {
        ushort port;
        using (var socket = new System.Net.Sockets.UdpClient(0))
            port = (ushort)((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
        var host = NewManager("party host", port);
        Assert.That(host.StartHost(), Is.True);
        yield return null;
        var client = NewManager("party client", port);
        Assert.That(client.StartClient(), Is.True);
        float deadline = Time.realtimeSinceStartup + 10f;
        NetworkObject owned = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            owned = client.SpawnManager?.GetLocalPlayerObject();
            if (owned != null && client.IsConnectedClient) break;
            yield return null;
        }
        Assert.That(owned, Is.Not.Null, "Client player spawned.");

        DiabloLifeGlobe.Ensure();
        var frames = DiabloLifeGlobe.Instance.GetComponent<PartyFramesHud>();
        Assert.IsNotNull(frames, "party frames spawn with the globe");

        // The client levels up: its owner writes heroLevel, the other copies read it.
        var prog = owned.GetComponent<DiabloProgression>();
        prog.SetState(7, 0);
        deadline = Time.realtimeSinceStartup + 5f;
        while (Time.realtimeSinceStartup < deadline && !frames.Shown.Any(r => !r.own && r.level == 7)) yield return null;
        var row = frames.Shown.FirstOrDefault(r => !r.own && r.level == 7);
        Assert.AreEqual(7, row.level, "a remote copy of the joined hero shows level 7");
        Assert.IsNotEmpty(row.name);
        Assert.IsNotEmpty(row.classId);
        Assert.Greater(row.hp01, 0f, "alive on join");
        Assert.LessOrEqual(frames.Shown.Count, PartyFramesHud.MaxFrames);

        // Damage on the owner replicates through hpState into the frame.
        for (int i = 0; i < 10; i++) yield return null; // let the level-up life gain settle
        float before = frames.Shown.First(r => !r.own && r.level == 7).hp01;
        owned.GetComponent<PlayerHealth>().TakeDamage(10f);
        deadline = Time.realtimeSinceStartup + 5f;
        while (Time.realtimeSinceStartup < deadline && !frames.Shown.Any(r => !r.own && r.level == 7 && r.hp01 < before - 0.01f)) yield return null;
        Assert.IsTrue(frames.Shown.Any(r => !r.own && r.level == 7 && r.hp01 < before - 0.01f), "the life bar follows the replicated HP");
    }
}
