using NUnit.Framework;
using UnityEngine;

/// <summary>S1 EditMode: sector geometry, warn lead, single hit, seeded order, safe route.</summary>
public class KilnSectorPatternTests
{
    [Test]
    public void WarnLead_IsOnePointFiveSeconds()
    {
        var p = new KilnSectorPattern(Vector3.zero, 10f, new System.Collections.Generic.List<int> { 0 });
        Assert.IsTrue(p.IsWarning(10.5f));
        Assert.IsFalse(p.IsBurning(11.4f), "still warning just before 1.5s");
        Assert.IsTrue(p.IsBurning(11.6f));
        Assert.IsTrue(p.IsExpired(13.5f), "1.5s warn + 2s burn");
        Assert.AreEqual(1.5f, KilnSectorPattern.WarnSeconds, 0.001f);
        Assert.AreEqual(2f, KilnSectorPattern.BurnSeconds, 0.001f);
    }

    [Test]
    public void SameGeometry_DrivesMarkAndHit()
    {
        var p = new KilnSectorPattern(Vector3.zero, 0f, new System.Collections.Generic.List<int> { 0 });
        Vector3 inside = new Vector3(5f, 0f, 0f); // sector 0, radius 5
        Vector3 outside = new Vector3(-5f, 0f, 0f); // sector 2
        Assert.IsTrue(p.Contains(inside), "mark geometry hits sector 0");
        Assert.IsFalse(p.Contains(outside));
        int hits = 0;
        p.Tick(0.5f, inside, () => hits++); // warning: no hit
        Assert.AreEqual(0, hits);
        p.Tick(2f, outside, () => hits++); // burning but outside: no hit
        Assert.AreEqual(0, hits);
        p.Tick(2f, inside, () => hits++);
        Assert.AreEqual(1, hits, "same geometry hits during burn");
    }

    [Test]
    public void MaxOneHit_PerPattern()
    {
        var p = new KilnSectorPattern(Vector3.zero, 0f, new System.Collections.Generic.List<int> { 0, 1 });
        Vector3 inside = new Vector3(5f, 0f, 0f);
        int hits = 0;
        p.Tick(2f, inside, () => hits++);
        p.Tick(2.5f, inside, () => hits++);
        p.Tick(3f, inside, () => hits++);
        Assert.AreEqual(1, hits);
        Assert.IsTrue(p.HitConsumed);
    }

    [Test]
    public void SafeRoute_AtMostTwoAdjacentActive()
    {
        foreach (int seed in new[] { 0, 1, 42, 12345 })
            for (int w = 0; w < 8; w++)
            {
                var sectors = KilnSectorPattern.ActiveSectors(seed, w);
                Assert.GreaterOrEqual(sectors.Count, 1);
                Assert.LessOrEqual(sectors.Count, 2, "max 2 active keeps a safe route open");
                if (sectors.Count == 2)
                    Assert.AreEqual((sectors[0] + 1) % 4, sectors[1], "active pair is adjacent");
                // Safe sector exists and its midpoint is outside the pattern.
                int safe = -1;
                for (int s = 0; s < 4; s++) if (!sectors.Contains(s)) { safe = s; break; }
                Assert.GreaterOrEqual(safe, 0);
                var p = new KilnSectorPattern(Vector3.zero, 0f, sectors);
                float ang = safe * 90f * Mathf.Deg2Rad;
                Assert.IsFalse(p.Contains(new Vector3(Mathf.Cos(ang) * 5f, 0f, Mathf.Sin(ang) * 5f)));
            }
    }

    [Test]
    public void Order_FollowsRunSeed_Deterministically()
    {
        var a = KilnSectorPattern.ActiveSectors(7, 3);
        var b = KilnSectorPattern.ActiveSectors(7, 3);
        CollectionAssert.AreEqual(a, b, "same seed+wave is deterministic");
    }
}
