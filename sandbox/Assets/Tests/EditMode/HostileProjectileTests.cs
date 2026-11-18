using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Enemy fireballs (Bone Warden, Hollow Abbot) hit the hero and pass through
/// husks; hero projectiles keep ignoring the hero. Before the fix every enemy
/// fireball searched only for husks and never hurt the hero.
/// </summary>
public class HostileProjectileTests
{
    const float Dt = 0.02f;
    GameObject hero;
    PlayerHealth health;

    [SetUp]
    public void Setup()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RunProfile.Reset();
        DiabloProjectile.ResetRun();
        hero = new GameObject(DiabloNetworkPlayer.HeroName);
        hero.transform.position = new Vector3(0f, 0f, 8f);
        health = hero.AddComponent<PlayerHealth>();
    }

    [TearDown]
    public void Teardown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        DiabloProjectile.ResetRun();
        RunProfile.Reset();
    }

    static void Fly(DiabloProjectile p)
    {
        for (int i = 0; i < 200 && p != null; i++) p.Step(Dt);
    }

    static GameObject Husk(Vector3 at)
    {
        var go = new GameObject("Husk");
        go.tag = "Husk";
        go.transform.position = at;
        go.AddComponent<EnemyHealth>();
        return go;
    }

    [Test]
    public void HostileFireball_DamagesTheHeroInItsPath()
    {
        float before = health.CurrentHP;
        var p = DiabloProjectile.Spawn(DiabloSkills.Skill.Fireball, new Vector3(0f, 1.2f, 0f), Vector3.forward,
            null, RangedCaster.ShotDamage, 12f, 20f, 0f, hostile: true);
        Fly(p);
        Assert.AreEqual(1, DiabloProjectile.HeroHits);
        Assert.AreEqual(before - DifficultyProfile.Current.ScaleDamage(RangedCaster.ShotDamage), health.CurrentHP, 0.001f);
    }

    [Test]
    public void HostileFireball_PassesThroughHusks()
    {
        Husk(new Vector3(0f, 0f, 4f));
        var p = DiabloProjectile.Spawn(DiabloSkills.Skill.Fireball, new Vector3(0f, 1.2f, 0f), Vector3.forward,
            null, RangedCaster.ShotDamage, 12f, 20f, 0f, hostile: true);
        Fly(p);
        Assert.AreEqual(1, DiabloProjectile.Impacts, "one impact: the hero, not the husk in front");
        Assert.AreEqual(1, DiabloProjectile.HeroHits, "hero behind the husk is hit");
    }

    [Test]
    public void HostileFireball_MissesAHeroOffTheLine()
    {
        hero.transform.position = new Vector3(3f, 0f, 8f);
        float before = health.CurrentHP;
        var p = DiabloProjectile.Spawn(DiabloSkills.Skill.Fireball, new Vector3(0f, 1.2f, 0f), Vector3.forward,
            null, RangedCaster.ShotDamage, 12f, 20f, 0f, hostile: true);
        Fly(p);
        Assert.AreEqual(0, DiabloProjectile.HeroHits);
        Assert.AreEqual(before, health.CurrentHP, 0.001f);
    }

    [Test]
    public void HeroFireball_NeverHurtsTheHero()
    {
        float before = health.CurrentHP;
        var p = DiabloProjectile.Spawn(DiabloSkills.Skill.Fireball, new Vector3(0f, 1.2f, 0f), Vector3.forward,
            null, 5, 12f, 20f, 0f);
        Fly(p);
        Assert.AreEqual(0, DiabloProjectile.HeroHits);
        Assert.AreEqual(before, health.CurrentHP, 0.001f);
    }
}
