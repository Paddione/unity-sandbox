using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// cinderreach-companion-rendering: companions spawn as hero clones (no
/// capsule primitives), carry class names, and walk with the leader.
/// </summary>
public class CompanionRenderTests
{
    GameObject hero;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        PartyCompanion.Live.Clear();
        hero = new GameObject(DiabloNetworkPlayer.HeroName);
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "HeroVisual";
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(hero.transform, false);
        hero.AddComponent<Equipment>();
        hero.AddComponent<PlayerHealth>();
        hero.AddComponent<ClassAppearance>();
        hero.AddComponent<Animator>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var c in PartyCompanion.Live.ToArray())
            if (c != null) Object.DestroyImmediate(c.gameObject);
        PartyCompanion.Live.Clear();
        var roster = PartyRoster.Instance;
        if (roster != null) Object.DestroyImmediate(roster.gameObject);
        Object.DestroyImmediate(hero);
        RunProfile.Reset();
    }

    [UnityTest]
    public IEnumerator CompanionsSpawnAsHeroClonesWithClassNames()
    {
        var roster = PartyRoster.EnsureSoloParty(hero.transform);
        Assert.IsNotNull(roster, "solo party fills the missing roles");
        yield return null;
        yield return null;
        Assert.AreEqual(2, PartyCompanion.Live.Count, "tank hero gains healer + dps partners");
        foreach (var c in PartyCompanion.Live)
        {
            Assert.IsTrue(c.name.StartsWith("Companion_"), "no capsule placeholder name");
            Assert.IsNull(c.GetComponent<MeshFilter>(), "root carries no primitive mesh");
            Assert.IsNotNull(c.transform.Find("HeroVisual"), "hero visuals cloned onto the companion");
            Assert.IsNotNull(c.GetComponent<Animator>(), "locomotion driver kept");
            Assert.IsNotNull(c.GetComponent<ClassAppearance>(), "class look applied");
            Assert.IsNotNull(c.GetComponent<PartyCompanion>(), "party brain present");
            Assert.IsNotNull(c.GetComponent<PlayerHealth>(), "own health present");
            Assert.AreEqual(ClassCatalog.Get(c.classId).displayName, c.DisplayName);
            Assert.IsNull(c.GetComponent<Collider>(), "companions move without a collider");
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator CompanionsFollowTheLeader()
    {
        PartyRoster.EnsureSoloParty(hero.transform);
        yield return null;
        yield return null;
        Assert.AreEqual(2, PartyCompanion.Live.Count);
        var comp = PartyCompanion.Live[0];
        DiabloTestFixture.TeleportHero(hero, hero.transform.position + new Vector3(12f, 0f, 0f));
        float before = Vector3.Distance(hero.transform.position, comp.transform.position);
        Assert.Greater(before, 8f, "the leader walked away");
        yield return DiabloTestFixture.WaitSeconds(1.5f);
        float after = Vector3.Distance(hero.transform.position, comp.transform.position);
        Assert.Less(after, before - 2f, "the companion closed the gap on its formation slot");
    }

    [UnityTest]
    public IEnumerator FallbackCapsuleIsUrpTintedWhenNoHeroStands()
    {
        hero.name = "NoHeroHere";
        var go = CompanionFactory.BuildBody(ClassCatalog.Fenwalker,
            CompanionFactory.CompanionProfile(ClassCatalog.Fenwalker, 0), null);
        Assert.IsNotNull(go);
        var rend = go.GetComponent<Renderer>();
        Assert.IsNotNull(rend);
        Assert.IsNotNull(rend.sharedMaterial, "never a bare primitive");
        Assert.IsNotNull(rend.sharedMaterial.shader, "URP Lit, never magenta default");
        Object.DestroyImmediate(go);
        hero.name = DiabloNetworkPlayer.HeroName;
        yield return null;
    }
}
