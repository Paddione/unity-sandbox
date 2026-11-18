using NUnit.Framework;
using UnityEngine;

/// <summary>S1 light: zone grades differ, cross-fade lasts 1.5 s, ambience API intact.</summary>
public class ZoneAtmosphereTests
{
    [Test]
    public void BlendDuration_IsOnePointFiveSeconds()
    {
        Assert.AreEqual(1.5f, ZoneAtmosphere.BlendSeconds);
    }

    [Test]
    public void Grades_DifferPerZone()
    {
        var town = ZoneAtmosphere.ForZone(ActProgress.Town);
        var ossuary = ZoneAtmosphere.ForZone(ActProgress.Ossuary);
        var kiln = ZoneAtmosphere.ForZone(ActProgress.Kiln);
        Assert.AreNotEqual(town.exposure, ossuary.exposure, "Ossuary is near-dark, town is lit.");
        Assert.AreNotEqual(town.vignette, ossuary.vignette, "Ossuary carries a stronger vignette.");
        Assert.AreNotEqual(town.saturation, ossuary.saturation, "Town is warmer/saturated.");
        Assert.AreNotEqual(town.temperature, ossuary.temperature, "Town warm 2700K vs ossuary cold dark.");
        Assert.AreNotEqual(ossuary.temperature, kiln.temperature, "Kiln runs hotter than the ossuary.");
    }

    [Test]
    public void ShowZone_BlendsOverBlendSeconds()
    {
        var go = new GameObject("Atmo");
        var atmo = go.AddComponent<ZoneAtmosphere>();
        atmo.ShowZone(ActProgress.Ossuary);
        Assert.IsTrue(atmo.Blending, "fade starts on ShowZone");
        // Half the blend time must not finish the fade.
        Assert.IsTrue(atmo.Tick(ZoneAtmosphere.BlendSeconds * 0.5f));
        Assert.IsTrue(atmo.Blending);
        // Mid-blend grade sits between town and ossuary exposure.
        float mid = atmo.Current.exposure;
        float townExp = ZoneAtmosphere.ForZone(ActProgress.Town).exposure;
        float ossExp = ZoneAtmosphere.ForZone(ActProgress.Ossuary).exposure;
        Assert.IsTrue((mid - townExp) * (mid - ossExp) <= 0f, $"mid-blend exposure {mid} between {townExp} and {ossExp}");
        // Completing the full duration lands exactly on the target grade.
        Assert.IsFalse(atmo.Tick(ZoneAtmosphere.BlendSeconds));
        Assert.IsFalse(atmo.Blending);
        Assert.AreEqual(ossExp, atmo.Current.exposure, 1e-4f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void DiabloAmbience_ApiIntact_AndBedVolumePerZone()
    {
        var mi = typeof(DiabloAmbience).GetMethod("Ensure", new[] { typeof(Transform) });
        Assert.IsNotNull(mi, "Ensure(Transform) signature must stay.");
        Assert.AreEqual(DiabloAmbience.BedVolume, DiabloAmbience.BedVolumeForZone(ActProgress.Town), 1e-6f);
        Assert.AreNotEqual(
            DiabloAmbience.BedVolumeForZone(ActProgress.Town),
            DiabloAmbience.BedVolumeForZone(ActProgress.Ossuary),
            "bed volume differs per zone");
    }
}
