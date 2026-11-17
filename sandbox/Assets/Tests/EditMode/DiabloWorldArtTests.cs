using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Blood Moor dressing: Blender props replace the greybox look.</summary>
public class DiabloWorldArtTests
{
    const string ZonePath = "Assets/Scenes/DiabloZone.unity";

    [Test]
    public void BloodMoor_IsDressedWithBlenderProps()
    {
        EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        var root = GameObject.Find("WorldDressing");
        Assert.IsNotNull(root, "Zone needs the WorldDressing root.");
        Assert.GreaterOrEqual(root.transform.childCount, 150, "Moor needs its props and plant scatter.");
        Assert.IsNotNull(GameObject.Find("CampfireModel"), "Campfire must use the Blender model.");
        Assert.IsNotNull(GameObject.Find("GateBrazier_0"));
        Assert.IsNotNull(GameObject.Find("DeadTree_0"));
        Assert.IsNotNull(GameObject.Find("Grave_0"));

        // Greybox holders keep names/colliders but hand their look over.
        var log = GameObject.Find("Palisade_0");
        Assert.IsNotNull(log);
        Assert.IsFalse(log.GetComponent<Renderer>().enabled, "Palisade holder must hide its cylinder.");
        Assert.IsNotNull(GameObject.Find("Art_Palisade_0"), "Palisade log needs its stake model.");
    }

    [Test]
    public void BloodMoor_FiresAreLit()
    {
        EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        var fires = Object.FindObjectsByType<DiabloFire>(FindObjectsSortMode.None);
        Assert.GreaterOrEqual(fires.Length, 9, "Campfire, 4 torches and 4 braziers must burn.");
        foreach (var f in fires)
        {
            var l = f.GetComponent<Light>();
            Assert.AreEqual(LightType.Point, l.type);
            Assert.AreEqual(LightShadows.None, l.shadows, "Fire lights stay shadowless (cost).");
        }
        Assert.IsTrue(RenderSettings.fog, "Moor haze must be on.");
    }
}
