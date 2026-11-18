using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Fair-fights arrival plaza (K3 rules without a scene): the gate and the
/// waypoint arrivals share one latched safe strip. Leaving it breaches the
/// latch for the entry; heroes outside the Kiln never trip it.
/// </summary>
public class KilnPlazaTests
{
    [SetUp]
    public void Setup() => RunProfile.Reset();

    [TearDown]
    public void Teardown() => RunProfile.Reset();

    [Test]
    public void BothArrivals_AreInsideThePlaza()
    {
        KilnLayout.ResetPlaza();
        Assert.IsTrue(KilnLayout.PlazaSafe(KilnLayout.KilnArrival), "gate arrival");
        KilnLayout.ResetPlaza();
        Assert.IsTrue(KilnLayout.PlazaSafe(KilnLayout.WaypointArrival), "waypoint arrival");
        Assert.IsFalse(KilnLayout.PlazaBreached);
    }

    [Test]
    public void Leaving_BreachesTheLatchForThisEntry()
    {
        KilnLayout.ResetPlaza();
        Assert.IsTrue(KilnLayout.PlazaSafe(KilnLayout.KilnArrival));
        var outside = KilnLayout.KilnArrival + new Vector3(0f, 0f, -6.5f);
        Assert.IsFalse(KilnLayout.PlazaSafe(outside), "advance leaves the plaza");
        Assert.IsTrue(KilnLayout.PlazaBreached);
        Assert.IsFalse(KilnLayout.PlazaSafe(KilnLayout.KilnArrival), "no re-arm by retreating");
    }

    [Test]
    public void GateToShrine_StaysInsideTheUnion()
    {
        KilnLayout.ResetPlaza();
        var shrine = KilnLayout.Shrines[3].pos;
        for (int i = 0; i <= 10; i++)
        {
            var p = Vector3.Lerp(KilnLayout.KilnArrival, shrine, i / 10f);
            Assert.IsTrue(KilnLayout.PlazaSafe(p), $"path step {i}");
        }
        Assert.IsFalse(KilnLayout.PlazaBreached, "walking gate to shrine never breaches");
    }

    [Test]
    public void Plaza_IgnoresHeroesOutsideTheKiln()
    {
        KilnLayout.ResetPlaza();
        Assert.IsFalse(KilnLayout.PlazaSafe(Vector3.zero)); // Ashen Fen
        Assert.IsFalse(KilnLayout.PlazaSafe(KilnLayout.OssuaryOffset)); // Ossuary
        Assert.IsFalse(KilnLayout.PlazaBreached, "far heroes never trip the latch");
    }

    [Test]
    public void WaypointArrival_MatchesTheShrineRule()
    {
        var (id, _, zone, pos) = KilnLayout.Shrines[3];
        Assert.AreEqual("Kiln", id);
        Assert.AreEqual(ActProgress.Kiln, zone);
        var expected = pos + Vector3.up * 0.5f + Vector3.forward * WaypointShrine.ArrivalOffset
            + Vector3.up * 0.05f;
        Assert.Less(Vector3.Distance(KilnLayout.WaypointArrival, expected), 0.01f);
    }
}
