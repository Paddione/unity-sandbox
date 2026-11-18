using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>act-one S5: waypoint shrines, discovery, destinations, travel and the window.</summary>
public class WaypointTests
{
    GameObject hero;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        ZoneTravel.ResetRun();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        hero = new GameObject(DiabloNetworkPlayer.HeroName);
        foreach (var (id, name, zone, pos) in KilnLayout.Shrines)
        {
            var go = new GameObject("WaypointShrine_" + id);
            go.transform.position = pos;
            var s = go.AddComponent<WaypointShrine>();
            s.shrineId = id;
            s.displayName = name;
            s.zone = zone;
        }
    }

    [TearDown]
    public void Teardown()
    {
        foreach (var s in Object.FindObjectsByType<WaypointShrine>(FindObjectsSortMode.None)) Object.DestroyImmediate(s.gameObject);
        Object.DestroyImmediate(hero);
        DiabloClickController.ExternalDrive = false;
        if (SaveService.HasSave()) SaveService.DeleteSave();
        RunProfile.Reset();
    }

    [Test]
    public void Discovery_NeedsProximityAndHappensOnce()
    {
        var town = WaypointShrine.Find("Town");
        hero.transform.position = town.transform.position + new Vector3(5f, 0f, 0f);
        Assert.IsFalse(town.TryDiscover(hero));
        hero.transform.position = town.transform.position + new Vector3(1f, 0f, 0f);
        Assert.IsTrue(town.TryDiscover(hero));
        Assert.IsFalse(town.TryDiscover(hero), "only once");
        CollectionAssert.Contains(RunProfile.Act.Waypoints, "Town");
    }

    [Test]
    public void Destinations_AreDiscoveredAndUnlockedOnly()
    {
        RunProfile.Act.Discover("Town");
        RunProfile.Act.Discover("Kiln");
        var d = WaypointShrine.Destinations();
        Assert.AreEqual(1, d.Count, "Kiln is discovered but locked");
        Assert.AreEqual("Town", d[0].shrineId);
        RunProfile.Act.Unlock(ActProgress.Kiln);
        Assert.AreEqual(2, WaypointShrine.Destinations().Count);
        Assert.IsFalse(WaypointShrine.Travel("Ossuary", hero), "not discovered");
    }

    [Test]
    public void Travel_TeleportsThroughZoneTravelAndSaves()
    {
        RunProfile.Act.Discover("Town");
        hero.transform.position = new Vector3(400f, 0f, 0f);
        Assert.IsTrue(WaypointShrine.Travel("Town", hero));
        Assert.AreEqual(1, ZoneTravel.Travels);
        Assert.Less(Vector3.Distance(hero.transform.position, WaypointShrine.Find("Town").Arrival), 0.01f);
        Assert.IsTrue(SaveService.HasSave());
    }

    [Test]
    public void Window_ListsDestinationsWithHdScaler()
    {
        RunProfile.Act.Discover("Town");
        RunProfile.Act.Discover("Ossuary");
        var ui = hero.AddComponent<DiabloWaypointUI>();
        ui.SetOpen(true);
        CollectionAssert.AreEqual(new[] { "Lantern Hold", "Sunken Ossuary" }, ui.Labels());
        Assert.IsTrue(DiabloClickController.ExternalDrive);
        var scaler = hero.GetComponentInChildren<CanvasScaler>();
        Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);
        ui.SetOpen(false);
        Assert.IsFalse(DiabloClickController.ExternalDrive);
    }

    [Test]
    public void Scene_HasOneShrinePerZone()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/DiabloZone.unity", OpenSceneMode.Single);
        foreach (var (id, _, zone, pos) in KilnLayout.Shrines)
        {
            var go = GameObject.Find("WaypointShrine_" + id);
            Assert.IsNotNull(go, id);
            var s = go.GetComponent<WaypointShrine>();
            Assert.AreEqual(zone, s.zone, id);
            Assert.Less(Vector3.Distance(go.transform.position, pos + Vector3.up * 0.5f), 0.01f, id);
        }
    }
}
