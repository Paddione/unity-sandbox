using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Fair-fights K2 (PlayMode): the warden windup is visible, a direct hit
/// costs the intended HP, and sidestepping after the release or standing
/// behind solid cover prevents the hit — all in live flight.
/// </summary>
public class WardenShotTests
{
    GameObject hero;
    GameObject warden;
    RangedCaster caster;
    Vector3 home;

    [SetUp]
    public void Setup()
    {
        DiabloJuice.ForceCrit = false; // exact-damage asserts: no random crits
        RunProfile.Reset();
        RangedCaster.ResetRun();
        DiabloProjectile.ResetRun();
        EnemyHealth.ResetRun();

        hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = "Player";
        hero.AddComponent<CharacterController>();
        hero.AddComponent<PlayerHealth>();

        // No EnemyAttack here: isolate the ranged duel (melee covered by the retreat tests).
        home = KilnLayout.OssuaryOffset + new Vector3(7f, 0.05f, 0f);
        warden = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        warden.name = "Bone Warden";
        warden.tag = "Husk";
        warden.transform.position = home;
        warden.AddComponent<EnemyHealth>().SetTier(12, LootTier.Normal);
        caster = warden.AddComponent<RangedCaster>();

        DiabloTestFixture.TeleportHero(hero, home + new Vector3(7f, 0f, 0f));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var p in Object.FindObjectsOfType<DiabloProjectile>())
            Object.DestroyImmediate(p.gameObject);
        var wall = GameObject.Find("WardenCoverWall");
        if (wall != null) Object.DestroyImmediate(wall);
        DiabloTestFixture.CleanupTestScene(hero);
        if (warden != null) Object.DestroyImmediate(warden);
        RangedCaster.ResetRun();
        DiabloProjectile.ResetRun();
        EnemyHealth.ResetRun();
        RunProfile.Reset();
    }

    IEnumerator WaitForShot(float timeout = 5f)
    {
        float t0 = Time.time;
        while (RangedCaster.ShotsFired < 1 && Time.time - t0 < timeout) yield return null;
        Assert.AreEqual(1, RangedCaster.ShotsFired, "warden fired exactly once");
    }

    [UnityTest]
    public IEnumerator Windup_ShowsBeforeTheShot()
    {
        float t0 = Time.time;
        while (!caster.Telegraphing && Time.time - t0 < 3f) yield return null;
        Assert.IsTrue(caster.Telegraphing, "windup runs before the shot");
        var orb = warden.transform.Find("WardenTelegraph");
        Assert.IsNotNull(orb, "windup orb exists");
        Assert.IsTrue(orb.gameObject.activeSelf, "windup orb visible");
        float small = orb.localScale.x;
        yield return null;
        yield return null;
        Assert.Greater(orb.localScale.x, small, "orb grows during the windup");

        yield return WaitForShot();
        var shot = Object.FindObjectOfType<DiabloProjectile>();
        Assert.IsNotNull(shot);
        Assert.IsTrue(shot.hostile, "warden shots hunt the hero");
        Assert.AreEqual(1, DiabloProjectile.Launched);
    }

    [UnityTest]
    public IEnumerator DirectHit_CostsIntendedHP()
    {
        var hp = hero.GetComponent<PlayerHealth>();
        Assert.AreEqual(Equipment.BaseMaxHP, hp.CurrentHP, 0.001f);

        yield return WaitForShot();
        float t0 = Time.time;
        while (DiabloProjectile.HeroHits < 1 && Time.time - t0 < 4f) yield return null;
        Assert.AreEqual(1, DiabloProjectile.HeroHits, "hero struck");
        int expected = DifficultyProfile.Current.ScaleDamage(RangedCaster.ShotDamage);
        Assert.AreEqual(Equipment.BaseMaxHP - expected, hp.CurrentHP, 0.001f);
        foreach (var p in Object.FindObjectsOfType<DiabloProjectile>())
            Object.DestroyImmediate(p.gameObject);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Sidestep_AfterTheShot_Dodges()
    {
        yield return WaitForShot();
        Assert.AreEqual(0, DiabloProjectile.HeroHits);
        // Dodge after the release: 2.5 m sideways, the bolt flies straight on.
        var h = hero.transform.position;
        DiabloTestFixture.TeleportHero(hero, new Vector3(h.x, h.y, h.z + 2.5f));
        yield return DiabloTestFixture.WaitSeconds(2.5f); // full 20 m range fizzles
        Assert.AreEqual(0, DiabloProjectile.HeroHits, "sidestepped");
        Assert.AreEqual(Equipment.BaseMaxHP, hero.GetComponent<PlayerHealth>().CurrentHP, 0.001f);
    }

    [UnityTest]
    public IEnumerator SolidCover_BlocksTheShot()
    {
        var mid = (warden.transform.position + hero.transform.position) * 0.5f;
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "WardenCoverWall";
        wall.transform.position = new Vector3(mid.x, 1.5f, mid.z);
        wall.transform.localScale = new Vector3(0.5f, 3f, 3f);
        Physics.SyncTransforms();

        yield return WaitForShot();
        yield return DiabloTestFixture.WaitSeconds(2.5f);
        Assert.AreEqual(0, DiabloProjectile.HeroHits, "cover stopped the bolt");
        Assert.AreEqual(Equipment.BaseMaxHP, hero.GetComponent<PlayerHealth>().CurrentHP, 0.001f);
        Assert.Greater(DiabloProjectile.Impacts, 0, "bolt died on the wall");
    }
}
