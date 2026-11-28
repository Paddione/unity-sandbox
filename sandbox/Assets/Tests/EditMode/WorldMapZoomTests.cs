using NUnit.Framework;
using UnityEngine;

/// <summary>cinderreach-map-overlay: zoom clamps to 75–150 % and centres on the hero.</summary>
public class WorldMapZoomTests
{
    const float Px = WorldMapUI.MapPx;

    [Test]
    public void Clamp_BoundsAt75And150()
    {
        Assert.AreEqual(0.75f, WorldMapZoom.ClampZoom(0.1f), 1e-4f);
        Assert.AreEqual(0.75f, WorldMapZoom.ClampZoom(0.75f), 1e-4f);
        Assert.AreEqual(1f, WorldMapZoom.ClampZoom(1f), 1e-4f);
        Assert.AreEqual(1.5f, WorldMapZoom.ClampZoom(1.5f), 1e-4f);
        Assert.AreEqual(1.5f, WorldMapZoom.ClampZoom(4f), 1e-4f);
    }

    [Test]
    public void Hero_SitsInTheMiddleWhenZoomed()
    {
        // At 100 % the picture exactly fills the frame, so centring is exact only
        // away from 100 % (or for a hero at the zone centre, covered below).
        var hero = new Vector2(0.6f, 0.55f);
        foreach (var zoom in new[] { 0.75f, 1.25f, 1.5f })
        {
            var local = WorldMapZoom.ToLocal(hero, hero, zoom, Px);
            Assert.AreEqual(0f, local.x, 1e-3f, "zoom " + zoom);
            Assert.AreEqual(0f, local.y, 1e-3f, "zoom " + zoom);
        }
        var centre = new Vector2(0.5f, 0.5f);
        Assert.AreEqual(Vector2.zero, WorldMapZoom.ToLocal(centre, centre, 1f, Px));
    }

    [Test]
    public void ZoomedIn_PictureStillCoversFrame()
    {
        // Hero at a corner: offset clamps, so the 150 % picture (960 px) still covers the 640 px frame.
        var corner = new Vector2(0f, 0f);
        var offset = WorldMapZoom.Offset(corner, 1.5f, Px);
        Assert.LessOrEqual(Mathf.Abs(offset.x), (Px * 1.5f - Px) * 0.5f + 1e-3f);
        Assert.LessOrEqual(Mathf.Abs(offset.y), (Px * 1.5f - Px) * 0.5f + 1e-3f);
        // Near edge of the picture must not leave a gap on the far side of the frame.
        float half = Px * 0.5f;
        Assert.LessOrEqual(offset.x - Px * 1.5f * 0.5f, -half + 1e-3f);
        Assert.GreaterOrEqual(offset.x + Px * 1.5f * 0.5f, half - 1e-3f);
    }

    [Test]
    public void ZoomedOut_PictureStaysInsideFrame()
    {
        var corner = new Vector2(1f, 1f);
        var offset = WorldMapZoom.Offset(corner, 0.75f, Px);
        float limit = (Px - Px * 0.75f) * 0.5f;
        Assert.LessOrEqual(Mathf.Abs(offset.x), limit + 1e-3f);
        Assert.LessOrEqual(Mathf.Abs(offset.y), limit + 1e-3f);
    }

    [Test]
    public void CentreZone_NoOffsetAtAnyZoom()
    {
        var centre = new Vector2(0.5f, 0.5f);
        Assert.AreEqual(Vector2.zero, WorldMapZoom.Offset(centre, 1.5f, Px));
        Assert.AreEqual(Vector2.zero, WorldMapZoom.ToLocal(centre, centre, 1.5f, Px));
    }
}
