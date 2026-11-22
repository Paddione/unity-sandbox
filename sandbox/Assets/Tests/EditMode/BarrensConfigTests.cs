using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>act-two-opening S1: Glass Barrens layout, gates, atmosphere and the built scene.</summary>
public class BarrensConfigTests
{
    [SetUp]
    public void Setup() => RunProfile.Reset();

    [TearDown]
    public void Teardown() => RunProfile.Reset();

    [Test]
    public void Layout_SitsBeyondTheKilnInsideItsFloor()
    {
        Assert.AreEqual(600f, BarrensLayout.BarrensOffset.x);
        Assert.Less(Vector3.Distance(KilnLayout.Shrines[4].pos, BarrensLayout.ShrinePos), 0.01f, "no static-init cycle");
        Assert.AreEqual(BarrensLayout.KilnReturnArrival.x - KilnLayout.KilnOffset.x, -6f, 0.01f);
        Assert.AreEqual(ActProgress.Barrens, EncounterLayout.CurrentZone(BarrensLayout.BarrensArrival));
        Assert.AreEqual(ActProgress.Barrens, EncounterLayout.CurrentZone(BarrensLayout.ShrinePos));
        Assert.AreEqual(ActProgress.Kiln, EncounterLayout.CurrentZone(BarrensLayout.BarrensGatePos));
        Assert.AreEqual(ActProgress.Kiln, EncounterLayout.CurrentZone(BarrensLayout.KilnReturnArrival));
        foreach (var p in new[] { BarrensLayout.BarrensArrival, BarrensLayout.BarrensReturnPos, BarrensLayout.ShrinePos })
        {
            var local = p - BarrensLayout.BarrensOffset;
            Assert.LessOrEqual(Mathf.Abs(local.x), BarrensLayout.Extent);
            Assert.LessOrEqual(Mathf.Abs(local.z), BarrensLayout.Extent);
        }
        // No bounce: arrivals stand clear of the opposite gate.
        Assert.Greater(Flat(BarrensLayout.BarrensArrival - BarrensLayout.BarrensReturnPos), 4f);
        Assert.Greater(Flat(BarrensLayout.KilnReturnArrival - BarrensLayout.BarrensGatePos), 4f);
    }

    [Test]
    public void Spires_LeaveTheNorthLaneOpen()
    {
        for (int i = 0; i < BarrensLayout.ShardSpires; i++)
        {
            var s = BarrensLayout.SpireAnchor(i);
            Assert.Greater(Flat(s - BarrensLayout.BarrensArrival), 3f, $"spire {i} blocks the arrival");
            Assert.Greater(Flat(s - BarrensLayout.ShrinePos), 3f, $"spire {i} blocks the shrine");
        }
    }

    [Test]
    public void GateHint_AndAtmosphere_AreZoneSpecific()
    {
        Assert.AreEqual(BarrensLayout.LockedHint, ZoneGate.LockedHintFor(ActProgress.Barrens));
        Assert.AreEqual(KilnLayout.LockedHint, ZoneGate.LockedHintFor(ActProgress.Kiln));
        var town = ZoneAtmosphere.ForZone(ActProgress.Town);
        var barrens = ZoneAtmosphere.ForZone(ActProgress.Barrens);
        Assert.AreNotEqual(town.temperature, barrens.temperature, "Barrens get their own grade");
        Assert.AreEqual(0.2f, DiabloAmbience.BedVolumeForZone(ActProgress.Barrens), 0.0001f);
    }

    [Test]
    public void Gate_IsSealedUntilTheKilnmotherFalls()
    {
        var go = new GameObject(ZoneGate.BarrensGateName);
        var gate = go.AddComponent<ZoneGate>();
        gate.targetZone = ActProgress.Barrens;
        gate.destination = BarrensLayout.BarrensArrival;
        Assert.IsTrue(gate.Locked);
        RunProfile.Act.Unlock(ActProgress.Kiln);
        Assert.IsTrue(gate.Locked, "the Abbot opens the Kiln, not the Barrens");
        RunProfile.Act.CompleteAct();
        Assert.IsFalse(gate.Locked);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Scene_HoldsBarrensRootGatesAndShrine()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/DiabloZone.unity", OpenSceneMode.Single);
        var root = GameObject.Find(BarrensLayout.RootName);
        Assert.IsNotNull(root, "BarrensRoot built by BarrensBuilder");
        Assert.Less(Vector3.Distance(root.transform.position, BarrensLayout.BarrensOffset), 0.01f);
        Assert.IsNotNull(GameObject.Find("WaypointShrine_Barrens"));
        var gate = GameObject.Find(ZoneGate.BarrensGateName).GetComponent<ZoneGate>();
        Assert.AreEqual(ActProgress.Barrens, gate.targetZone);
        Assert.Less(Vector3.Distance(gate.destination, BarrensLayout.BarrensArrival), 0.01f);
        var ret = GameObject.Find(ZoneGate.BarrensReturnGateName).GetComponent<ZoneGate>();
        Assert.AreEqual(ActProgress.Kiln, ret.targetZone);
    }

    static float Flat(Vector3 v)
    {
        v.y = 0f;
        return v.magnitude;
    }
}
