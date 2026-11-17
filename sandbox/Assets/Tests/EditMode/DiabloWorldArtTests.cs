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
    public void BloodMoor_NoGreyboxLeftVisible()
    {
        EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        foreach (var prefix in new[] { "MoorEdge_", "ZStep_", "BloodPatch_" })
        {
            int holders = 0;
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (!go.name.StartsWith(prefix)) continue;
                holders++;
                Assert.IsFalse(go.GetComponent<Renderer>().enabled, $"{go.name} must hide its greybox cube.");
                Assert.IsNotNull(go.GetComponent<Collider>(), $"{go.name} keeps its collider.");
            }
            Assert.Greater(holders, 0, $"Zone must still contain {prefix} holders.");
        }
        Assert.IsNotNull(GameObject.Find("MoorWall_N_0"), "Moor edge needs drystone wall segments.");
        Assert.IsNotNull(GameObject.Find("Art_ZStep_0"), "Entry steps need the stone step model.");
        Assert.IsNotNull(GameObject.Find("Art_BloodPatch_0"), "Blood patches need the splat model.");

        var goal = GameObject.Find("GoalPad");
        Assert.IsFalse(goal.GetComponent<Renderer>().enabled, "GoalPad hands its look to the crypt seal.");
        Assert.IsTrue(goal.GetComponent<Collider>().isTrigger, "GoalPad stays the extraction trigger.");
        Assert.IsNotNull(GameObject.Find("CryptSeal"), "Crypt gate needs the rune seal.");
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
        var cam = GameObject.Find("Main Camera").GetComponent<Camera>();
        Assert.AreEqual(CameraClearFlags.SolidColor, cam.clearFlags, "Sky beyond the wall must be night haze.");
        Assert.AreEqual(RenderSettings.fogColor, cam.backgroundColor);
    }
}
