using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Ranged skills: fireball blast, arrow single target, cooldown gate.</summary>
public class DiabloSkillsTests
{
    GameObject floor;
    GameObject player;
    DiabloSkills skills;

    [SetUp]
    public void Setup()
    {
        HitStop.Enabled = false;
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        DiabloProjectile.ResetRun();
        floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "TestFloor";
        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.AddComponent<CharacterController>();
        player.AddComponent<DiabloClickController>();
        player.AddComponent<DiabloMelee>();
        skills = player.GetComponent<DiabloSkills>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var p in GameObject.FindObjectsOfType<DiabloProjectile>())
            Object.DestroyImmediate(p.gameObject);
        DiabloTestFixture.CleanupTestScene(player);
        if (floor != null) Object.DestroyImmediate(floor);
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        DiabloProjectile.ResetRun();
        HitStop.Enabled = true;
    }

    static GameObject SpawnHusk(Vector3 pos, int hp)
    {
        var husk = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        husk.name = "TestHusk";
        husk.tag = "Husk";
        husk.transform.position = pos;
        husk.AddComponent<EnemyHealth>().SetTier(hp, LootTier.Normal);
        return husk;
    }

    [Test]
    public void ClickController_AddsSkills()
    {
        Assert.IsNotNull(skills, "DiabloClickController must attach DiabloSkills.");
    }

    [UnityTest]
    public IEnumerator Fireball_BlastKillsClusteredHusks()
    {
        yield return null;
        var a = SpawnHusk(new Vector3(0f, 1.05f, 8f), 3);
        var b = SpawnHusk(new Vector3(1.2f, 1.05f, 8.5f), 3);
        var far = SpawnHusk(new Vector3(8f, 1.05f, 8f), 3);

        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.Fireball, Vector3.forward));
        Assert.AreEqual(1, DiabloProjectile.Launched);
        yield return DiabloTestFixture.WaitSeconds(1.5f);

        Assert.IsTrue(a == null, "Directly hit husk should die.");
        Assert.IsTrue(b == null, "Husk inside blast radius should die.");
        Assert.IsTrue(far != null, "Husk outside blast radius must survive.");
        Assert.AreEqual(2, EnemyHealth.HusksKilled);
    }

    [UnityTest]
    public IEnumerator Arrow_HitsFirstHuskOnly()
    {
        yield return null;
        var near = SpawnHusk(new Vector3(0f, 1.05f, 6f), 5);
        var behind = SpawnHusk(new Vector3(0f, 1.05f, 9f), 5);

        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.Arrow, Vector3.forward));
        yield return DiabloTestFixture.WaitSeconds(1.0f);

        Assert.AreEqual(1, DiabloProjectile.Impacts);
        Assert.Less(near.GetComponent<EnemyHealth>().HPFraction(), 1f, "Arrow must damage the first husk.");
        Assert.AreEqual(1f, behind.GetComponent<EnemyHealth>().HPFraction(), "Arrow must not pierce.");
    }

    [Test]
    public void SkillArt_PrefabsShipInResources()
    {
        var fire = Resources.Load<GameObject>("SkillArt/FireballProjectile");
        var arrow = Resources.Load<GameObject>("SkillArt/ArrowProjectile");
        var blast = Resources.Load<GameObject>("SkillArt/FireballBlast");
        Assert.IsNotNull(fire, "Run SkillArtBuilder.Build.");
        Assert.IsNotNull(arrow);
        Assert.IsNotNull(blast);
        Assert.GreaterOrEqual(fire.GetComponentsInChildren<ParticleSystem>().Length, 3, "Fireball needs particle layers.");
        Assert.IsNotNull(arrow.GetComponentInChildren<MeshRenderer>(), "Arrow needs the KayKit mesh.");
        foreach (var r in fire.GetComponentsInChildren<ParticleSystemRenderer>())
            Assert.IsNotNull(r.sharedMaterial, $"{r.name} lost its particle material.");
    }

    [UnityTest]
    public IEnumerator Fireball_UsesParticleArt()
    {
        yield return null;
        var p = DiabloProjectile.Spawn(DiabloSkills.Skill.Fireball, new Vector3(0f, 1f, 2f), Vector3.forward,
            player.transform, 3, 11f, 16f, 2.5f);
        Assert.Greater(p.GetComponentsInChildren<ParticleSystem>().Length, 0);
    }

    [UnityTest]
    public IEnumerator RemoteCast_IsVisualOnly()
    {
        yield return null;
        var husk = SpawnHusk(new Vector3(0f, 1.05f, 6f), 3);
        var p = skills.ShowRemoteCast(DiabloSkills.Skill.Fireball, new Vector3(0f, 1f, 1f), Vector3.forward);
        Assert.IsTrue(p.cosmetic);
        yield return DiabloTestFixture.WaitSeconds(1.0f);
        Assert.AreEqual(1, DiabloProjectile.Impacts, "Mirror fireball still explodes on the husk.");
        Assert.IsTrue(husk != null, "Mirror projectiles must never deal damage.");
        Assert.AreEqual(1f, husk.GetComponent<EnemyHealth>().HPFraction());
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.Fireball, Vector3.forward),
            "A remote cast must not consume the local cooldown.");
    }

    [UnityTest]
    public IEnumerator Arrow_SticksInWall()
    {
        yield return null;
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "TestWall";
        wall.transform.position = new Vector3(0f, 1f, 6f);
        wall.transform.localScale = new Vector3(4f, 3f, 0.5f);
        try
        {
            Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.Arrow, Vector3.forward));
            yield return DiabloTestFixture.WaitSeconds(0.6f);
            Assert.AreEqual(1, DiabloProjectile.Impacts);
            var arrow = GameObject.Find("Arrow");
            Assert.IsNotNull(arrow, "Arrow stays stuck in the wall after impact.");
            Assert.Less(arrow.transform.position.z, 6f, "Arrow stops at the wall face.");
        }
        finally
        {
            Object.DestroyImmediate(wall);
        }
    }

    [UnityTest]
    public IEnumerator Cooldown_BlocksImmediateRecast()
    {
        yield return null;
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.Fireball, Vector3.forward));
        Assert.IsFalse(skills.CastToward(DiabloSkills.Skill.Fireball, Vector3.forward));
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.Arrow, Vector3.forward),
            "Cooldowns are per skill.");
        yield return DiabloTestFixture.WaitSeconds(skills.fireballCooldown + 0.1f);
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.Fireball, Vector3.forward));
    }
}
