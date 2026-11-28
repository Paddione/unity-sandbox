using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>cinderreach-map-overlay: each legend category hides exactly its own markers.</summary>
public class WorldMapFilterTests
{
    static MapIcon Icon(MapCategory cat, float x = 0f)
        => new MapIcon { world = new Vector3(x, 0f, 0f), shape = MapIconShape.Dot, color = Color.white, size = 10f, category = cat };

    static List<MapIcon> Sample() => new List<MapIcon>
    {
        Icon(MapCategory.Quests, 1f),
        Icon(MapCategory.Party, 2f),
        Icon(MapCategory.Waypoints, 3f),
        Icon(MapCategory.Persons, 4f),
        Icon(MapCategory.Elites, 5f),
        Icon(MapCategory.None, 6f),
    };

    [Test]
    public void AllEnabled_KeepsEverything()
    {
        var into = new List<MapIcon>();
        WorldMapFilter.Apply(Sample(), MapCategory.All, into);
        Assert.AreEqual(6, into.Count);
    }

    [Test]
    public void EachCategory_HidesExactlyItsMarkers()
    {
        foreach (var cat in new[] { MapCategory.Quests, MapCategory.Party, MapCategory.Waypoints, MapCategory.Persons, MapCategory.Elites })
        {
            var into = new List<MapIcon>();
            WorldMapFilter.Apply(Sample(), MapCategory.All & ~cat, into);
            Assert.AreEqual(5, into.Count, cat.ToString());
            Assert.IsFalse(into.Exists(i => i.category == cat), cat + " must be hidden");
            Assert.IsTrue(into.Exists(i => i.category == MapCategory.None), "hero marker always stays");
            Assert.IsTrue(WorldMapFilter.IsVisible(Icon(MapCategory.None), MapCategory.None));
            Assert.IsFalse(WorldMapFilter.IsVisible(Icon(cat), MapCategory.All & ~cat));
        }
    }

    [Test]
    public void NoneEnabled_KeepsOnlyHero()
    {
        var into = new List<MapIcon>();
        WorldMapFilter.Apply(Sample(), MapCategory.None, into);
        Assert.AreEqual(1, into.Count);
        Assert.AreEqual(MapCategory.None, into[0].category);
    }
}
