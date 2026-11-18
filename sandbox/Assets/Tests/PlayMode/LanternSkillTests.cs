using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>second-class S2: Lanternbearer skills in play (halo radius, veil wall stop, wisps, piercing bolt).</summary>
public class LanternSkillTests
{
    GameObject hero;
    DiabloSkills skills;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        EnemyHealth.ResetRun();
        hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = "Player";
        Object.DestroyImmediate(hero.GetComponent<Collider>());
        hero.transform.position = new Vector3(0f, 0.05f, 0f);
        hero.AddComponent<DiabloProgression>();
        hero.AddComponent<PlayerHealth>();
        skills = hero.AddComponent<DiabloSkills>();
    }

    [TearDown]
    public void TearDown()
    {
        Wisp.ResetRun();
        foreach (var go in GameObject.FindGameObjectsWithTag("Husk")) Object.DestroyImmediate(go);
        var wall = GameObject.Find("VeilWall");
        if (wall != null) Object.DestroyImmediate(wall);
        foreach (var p in Object.FindObjectsOfType<DiabloProjectile>()) Object.DestroyImmediate(p.gameObject);
        Object.DestroyImmediate(hero);
        EnemyHealth.ResetRun();
        RunProfile.Reset();
    }

    static EnemyHealth Husk(string name, Vector3 pos, int hp = 20)
    {
        var go = new GameObject(name);
        go.tag = "Husk";
        go.transform.position = pos;
        var h = go.AddComponent<EnemyHealth>();
        h.SetTier(hp, LootTier.Normal);
        return h;
    }

    [UnityTest]
    public IEnumerator Halo_HitsInsideRadiusOnly()
    {
        var inside = Husk("Inside", new Vector3(2.5f, 0.05f, 0f));
        var outside = Husk("Outside", new Vector3(5f, 0.05f, 0f));
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.SearingHalo, Vector3.forward));
        Assert.Less(inside.HPFraction(), 1f);
        Assert.AreEqual(1f, outside.HPFraction());
        yield return null;
    }

    [UnityTest]
    public IEnumerator VeilStep_StopsBeforeWall_AndBlinksFullDistanceInTheOpen()
    {
        float open = skills.DoVeilStep(Vector3.forward);
        Assert.AreEqual(skills.veilDistance, open, 0.01f);
        Assert.AreEqual(6f, hero.transform.position.z, 0.05f);

        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "VeilWall";
        wall.transform.position = new Vector3(0f, 1f, 9f);
        wall.transform.localScale = new Vector3(4f, 2f, 0.5f);
        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();
        float blocked = skills.DoVeilStep(Vector3.forward);
        Assert.Less(blocked, 3f, "wall front at z=8.75");
        Assert.Less(hero.transform.position.z + DiabloSkills.VeilHeroRadius, 8.76f, "hero stays in front of the wall");
    }

    [UnityTest]
    public IEnumerator Wisps_SpawnStrikeAndReplace()
    {
        var target = Husk("WispTarget", new Vector3(3f, 0.05f, 0f));
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.Wisps, Vector3.forward));
        Assert.AreEqual(2, Wisp.Active.Count);
        var hit = Wisp.Active[0].TryStrike(Time.time + 1f);
        Assert.AreSame(target.gameObject, hit);
        Assert.Less(target.HPFraction(), 1f);
        skills.SummonWisps(cosmetic: false);
        yield return null;
        Assert.AreEqual(2, Wisp.Active.Count, "a new cast replaces the old wisps");
    }

    [UnityTest]
    public IEnumerator LanternBolt_PiercesOneHusk()
    {
        var first = Husk("First", new Vector3(0f, 0.05f, 3f));
        var second = Husk("Second", new Vector3(0f, 0.05f, 5f));
        var third = Husk("Third", new Vector3(0f, 0.05f, 7f));
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.LanternBolt, Vector3.forward));
        var bolt = Object.FindObjectOfType<DiabloProjectile>();
        Assert.IsNotNull(bolt, "bolt spawned");
        Assert.AreEqual(DiabloSkills.Skill.LanternBolt, bolt.kind);
        Assert.AreEqual(1, bolt.pierce);
        // Wait game time, not frames: headless test frames last about 1 ms.
        float until = Time.time + 2f;
        while (bolt != null && Time.time < until) yield return null;
        Assert.Less(first.HPFraction(), 1f, "first husk hit");
        Assert.Less(second.HPFraction(), 1f, "pierced into the second");
        Assert.AreEqual(1f, third.HPFraction(), "stops at the second");
    }
}
