using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// cinderreach-group-quest-map in play: a client's dialog reaches the other
/// players through the host, host barks reach clients, and the local hero gets
/// a live minimap and a zone map (M) with quest places.
/// </summary>
public sealed class GroupQuestMapPlayTests
{
    readonly List<GameObject> spawned = new List<GameObject>();

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        StoryDialogShare.ResetForTests();
        StoryBarks.ResetForTests();
        MapScene.ResetCache();
    }

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
        RunProfile.Reset();
        StoryBarks.ResetForTests();
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
            PlayerPrefab = Resources.Load<GameObject>("NetworkPlayerDiablo"),
        };
        DiabloNetworkBootstrap.RegisterLootPrefab(manager);
        return manager;
    }

    [UnityTest]
    public IEnumerator Coop_ClientDialogReachesGroup_HostBarkReachesClient()
    {
        ushort port;
        using (var socket = new System.Net.Sockets.UdpClient(0))
            port = (ushort)((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
        var host = NewManager("share host", port);
        Assert.That(host.StartHost(), Is.True);
        yield return null;
        var client = NewManager("share client", port);
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
        yield return null;

        // The client talks: its owned hero sends, the host relays to everyone but the speaker.
        var speaker = owned.GetComponent<DiabloNetworkPlayer>();
        speaker.ShareDialog(SharedDialogOp.Open, "h1.offer", StoryIds.Ottilie, "0,1,2,3", 0);
        deadline = Time.realtimeSinceStartup + 5f;
        while (StoryDialogShare.Received == 0 && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(StoryDialogShare.Received, Is.GreaterThanOrEqualTo(1), "the host's copy of the speaker got the dialog");
        Assert.That(StoryDialogShare.LastOp, Is.EqualTo(SharedDialogOp.Open));
        Assert.That(StoryDialogShare.LastDialog, Is.EqualTo("h1.offer"));
        int afterOpen = StoryDialogShare.Received;

        speaker.ShareDialog(SharedDialogOp.Line, "h1.offer", StoryIds.Ottilie, "", 2);
        deadline = Time.realtimeSinceStartup + 5f;
        while (StoryDialogShare.Received == afterOpen && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(StoryDialogShare.LastOp, Is.EqualTo(SharedDialogOp.Line));
        Assert.That(StoryDialogShare.LastIndex, Is.EqualTo(2));

        // A forged open (lines past the script) dies at the host.
        int beforeForged = StoryDialogShare.Received;
        speaker.ShareDialog(SharedDialogOp.Open, "h1.offer", StoryIds.Ottilie, "0,9", 0);
        for (int i = 0; i < 20; i++) yield return null;
        Assert.That(StoryDialogShare.Received, Is.EqualTo(beforeForged), "the host drops invalid dialogs");

        // The host's bark goes out to the client.
        var hostRelay = host.SpawnManager.GetLocalPlayerObject().GetComponent<DiabloNetworkPlayer>();
        hostRelay.BroadcastBark("abbot.phase2");
        deadline = Time.realtimeSinceStartup + 5f;
        while (StoryBarks.relayed == 0 && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(StoryBarks.relayed, Is.GreaterThanOrEqualTo(1), "the client queued the host's bark");
    }

    GameObject MakeHero(Vector3 at)
    {
        var hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = DiabloNetworkPlayer.HeroName;
        hero.transform.position = at;
        hero.AddComponent<DiabloProgression>();
        hero.AddComponent<Equipment>();
        hero.AddComponent<Inventory>();
        hero.AddComponent<Wallet>();
        hero.AddComponent<PlayerHealth>();
        hero.AddComponent<StoryDirector>();
        spawned.Add(hero);
        return hero;
    }

    GameObject MakeNpc(string id, Vector3 at)
    {
        var go = new GameObject("StoryNpc_" + id);
        go.transform.position = at;
        go.AddComponent<CapsuleCollider>();
        go.AddComponent<StoryNpc>().npcId = id;
        spawned.Add(go);
        return go;
    }

    [UnityTest]
    public IEnumerator Minimap_AndZoneMap_ShowHeroAndQuestPlaces()
    {
        MakeNpc(StoryIds.Ottilie, new Vector3(1.5f, 0f, 14.5f));
        var hero = MakeHero(new Vector3(0f, 1f, 8f));
        RunProfile.Story.Refresh(RunProfile.Act); // h1 offered by Ottilie
        yield return DiabloTestFixture.WaitSeconds(0.6f);

        var director = hero.GetComponent<StoryDirector>();
        Assert.IsTrue(director.Marks.Exists(m => m.questId == "h1" && m.kind == QuestMarkKind.Offer), "Ottilie's offer is a map place");
        var mini = hero.GetComponent<MinimapHud>();
        Assert.IsTrue(mini.Built && mini.Visible);
        Assert.That(mini.IconsShown, Is.GreaterThanOrEqualTo(3), "hero, Ottilie and her offer");

        var map = hero.GetComponent<WorldMapUI>();
        map.SetOpen(true);
        yield return null;
        Assert.IsTrue(map.IsOpen);
        Assert.AreEqual(ActProgress.Town, map.ViewZone);
        Assert.IsTrue(DiabloUIFactory.IsAnyModalOpen(hero), "the zone map is a modal panel");
        yield return null;
        Assert.IsFalse(mini.Visible, "the minimap hides under the zone map");
        Assert.That(map.IconsShown, Is.GreaterThanOrEqualTo(3));
        map.Cycle(1);
        Assert.AreEqual(ActProgress.Ossuary, map.ViewZone, "the Ossuary is open from the start");
        map.Cycle(1);
        Assert.AreEqual(ActProgress.Town, map.ViewZone, "locked Kiln and Barrens are skipped");
        map.SetOpen(false);
        yield return null;
        Assert.IsTrue(mini.Visible);

        // Taking h1 tracks it: the tracker names the task.
        Assert.IsTrue(RunProfile.Story.Accept("h1"));
        yield return DiabloTestFixture.WaitSeconds(0.4f);
        var tracker = hero.transform.Find("StoryCanvas/StoryTracker").GetComponent<UnityEngine.UI.Text>();
        StringAssert.Contains(Loc.T("quest.h1.title"), tracker.text);
        var log = hero.GetComponent<StoryQuestUI>();
        log.SetOpen(true);
        log.ToggleTrack("h1");
        yield return DiabloTestFixture.WaitSeconds(0.4f);
        Assert.IsFalse(tracker.text.Contains(Loc.T("quest.h1.title")), "hidden from the tracker");
        log.SetOpen(false);
    }

    [UnityTest]
    public IEnumerator HeroClone_CarriesNoStoryHudOrMaps()
    {
        var hero = MakeHero(new Vector3(0f, 1f, 8f));
        yield return null;
        var clone = CompanionFactory.CloneHeroVisual(hero);
        spawned.Add(clone);
        Assert.IsNotNull(clone);
        Assert.IsNull(clone.GetComponent<StoryDirector>(), "a companion or paperdoll clone runs no story");
        Assert.IsNull(clone.GetComponent<MinimapHud>());
        Assert.IsNull(clone.GetComponent<WorldMapUI>());
        Assert.IsNull(clone.GetComponent<StoryQuestUI>());
        yield return null;
        Assert.AreEqual(1, Object.FindObjectsByType<StoryDirector>(FindObjectsSortMode.None).Length);
    }
}
