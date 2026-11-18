using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>act-one S2: Cinder Kiln scene inventory and the locked Kiln gate.</summary>
public class KilnConfigTests
{
    const string ZonePath = "Assets/Scenes/DiabloZone.unity";

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        ZoneGate.ResetRun();
    }

    [TearDown]
    public void Teardown()
    {
        if (SaveService.HasSave()) SaveService.DeleteSave();
        ZoneGate.ResetRun();
        RunProfile.Reset();
    }

    [Test]
    public void Scene_HasKilnRootDomesEmbersAndGatePair()
    {
        EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        var root = GameObject.Find(KilnLayout.RootName);
        Assert.IsNotNull(root);
        Assert.AreEqual(KilnLayout.KilnOffset, root.transform.position);
        int domes = 0, embers = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.StartsWith("KilnDome_")) domes++;
            if (t.name.StartsWith("KilnEmber_") && t.GetComponent<Light>() != null) embers++;
        }
        Assert.AreEqual(KilnLayout.KilnDomes, domes);
        Assert.AreEqual(KilnLayout.EmberLights, embers);
        Assert.IsNotNull(GameObject.Find("KilnFloor").GetComponent<Collider>());

        var gate = GameObject.Find(ZoneGate.KilnGateName).GetComponent<ZoneGate>();
        Assert.AreEqual(ActProgress.Kiln, gate.targetZone);
        Assert.Less(Vector3.Distance(gate.destination, KilnLayout.KilnArrival), 0.01f);
        Assert.IsNotNull(gate.transform.Find(ZoneGate.LockedHintName));
        var ret = GameObject.Find(ZoneGate.KilnReturnGateName).GetComponent<ZoneGate>();
        Assert.AreEqual(ActProgress.Ossuary, ret.targetZone);
        Assert.GreaterOrEqual(Vector3.Distance(ret.destination, gate.transform.position), 7.9f, "arrival clear of the Kiln gate");
    }

    [Test]
    public void LockedGate_RefusesAndShowsHint_UnlockedGateTravels()
    {
        var hero = new GameObject("Player");
        hero.AddComponent<PlayerHealth>();
        var gateGo = new GameObject(ZoneGate.KilnGateName);
        var hint = new GameObject(ZoneGate.LockedHintName);
        hint.transform.SetParent(gateGo.transform, false);
        var gate = gateGo.AddComponent<ZoneGate>();
        gate.targetZone = ActProgress.Kiln;
        gate.destination = KilnLayout.KilnArrival;

        Assert.IsTrue(gate.Locked);
        gate.Teleport(hero);
        Assert.AreEqual(0, ZoneGate.Teleports);
        Assert.AreEqual(1, ZoneGate.Refusals);
        Assert.AreNotEqual(KilnLayout.KilnArrival, hero.transform.position);
        gate.RefreshHint();
        Assert.IsTrue(hint.activeSelf);

        RunProfile.Act.Unlock(ActProgress.Kiln);
        Assert.IsFalse(gate.Locked);
        gate.RefreshHint();
        Assert.IsFalse(hint.activeSelf);
        gate.Teleport(hero);
        Assert.AreEqual(1, ZoneGate.Teleports);
        Assert.AreEqual(KilnLayout.KilnArrival, hero.transform.position);

        Object.DestroyImmediate(gateGo);
        Object.DestroyImmediate(hero);
    }

    [Test]
    public void ReturnAndOssuaryGates_AreNeverLocked()
    {
        var go = new GameObject("G");
        var g = go.AddComponent<ZoneGate>();
        g.targetZone = ActProgress.Ossuary;
        Assert.IsFalse(g.Locked);
        g.targetZone = ActProgress.Town;
        Assert.IsFalse(g.Locked);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void KilnArrival_IsClearOfEveryThrallAndElitePack()
    {
        float clear = EnemyAttack.AggroRange + 1f;
        for (int v = 0; v < KilnLayout.Variants; v++)
        {
            for (int i = 0; i < KilnLayout.ThrallCount; i++)
                Assert.GreaterOrEqual(Flat(KilnLayout.KilnArrival - KilnLayout.ThrallAnchor(v, i)), clear, $"thrall {i} variant {v}");
            for (int i = 0; i < KilnLayout.EliteCount; i++)
                Assert.GreaterOrEqual(Flat(KilnLayout.KilnArrival - KilnLayout.EliteAnchor(v, i)), clear, $"elite {i} variant {v}");
        }
    }

    static float Flat(Vector3 d) => new Vector2(d.x, d.z).magnitude;
}
