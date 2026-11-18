using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Bone Warden leash: wardens ignore a hero beyond aggro range, never chase
/// past the leash radius and walk home when the hero leaves (they used to
/// follow the hero from the Ossuary into the Fen).
/// </summary>
public class WardenLeashTests
{
    const float Dt = 0.05f;
    GameObject hero;
    RangedCaster warden;

    [SetUp]
    public void Setup()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RangedCaster.ResetRun();
        DiabloProjectile.ResetRun();
        hero = new GameObject(DiabloNetworkPlayer.HeroName);
        hero.AddComponent<PlayerHealth>();
        warden = new GameObject("Bone Warden").AddComponent<RangedCaster>();
        warden.SetHome(Vector3.zero);
    }

    [TearDown]
    public void Teardown()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RangedCaster.ResetRun();
        DiabloProjectile.ResetRun();
    }

    float FromHome() => new Vector3(warden.transform.position.x, 0f, warden.transform.position.z).magnitude;

    [Test]
    public void HeroBeyondAggro_WardenStaysHomeAndHoldsFire()
    {
        hero.transform.position = new Vector3(RangedCaster.AggroRange + 1f, 0f, 0f);
        float t = 0f;
        for (int i = 0; i < 200; i++, t += Dt) warden.Tick(t, Dt, hero.transform);
        Assert.Less(FromHome(), 0.01f);
        Assert.AreEqual(0, RangedCaster.ShotsFired);
    }

    [Test]
    public void FleeingHero_WardenStopsAtTheLeashAndStillShoots()
    {
        hero.transform.position = new Vector3(12f, 0f, 0f);
        float t = 0f, worst = 0f;
        for (int i = 0; i < 400; i++, t += Dt)
        {
            // Hero backs off to 21 m from the warden's home at 2 m/s.
            hero.transform.position = new Vector3(Mathf.Min(21f, 12f + 2f * t), 0f, 0f);
            warden.Tick(t, Dt, hero.transform);
            worst = Mathf.Max(worst, FromHome());
        }
        Assert.LessOrEqual(worst, RangedCaster.LeashRadius + 0.01f, "never past the leash");
        Assert.IsFalse(warden.Returning, "hero still inside aggro range");
        Assert.Greater(RangedCaster.ShotsFired, 0, "pinned at the leash, the warden keeps firing");
    }

    [Test]
    public void HeroLeavesAggro_WardenWalksHome()
    {
        warden.transform.position = new Vector3(9f, 0f, 0f);
        hero.transform.position = new Vector3(40f, 0f, 0f);
        warden.Tick(0f, Dt, hero.transform);
        Assert.IsTrue(warden.Returning);
        float t = Dt;
        for (int i = 0; i < 200 && warden.Returning; i++, t += Dt) warden.Tick(t, Dt, hero.transform);
        Assert.IsFalse(warden.Returning);
        Assert.LessOrEqual(FromHome(), 0.3f);
    }

    [Test]
    public void KnockedPastTheLeash_WardenIgnoresTheHeroUntilHome()
    {
        warden.transform.position = new Vector3(RangedCaster.LeashRadius + RangedCaster.LeashSlack + 1f, 0f, 0f);
        hero.transform.position = warden.transform.position + new Vector3(7f, 0f, 0f);
        float t = 0f;
        warden.Tick(t, Dt, hero.transform);
        Assert.IsTrue(warden.Returning);
        for (int i = 0; i < 40; i++, t += Dt) warden.Tick(t, Dt, hero.transform);
        Assert.AreEqual(0, RangedCaster.ShotsFired, "no shots while walking home");
    }

    [Test]
    public void WardenShot_IsHostile()
    {
        hero.transform.position = new Vector3(7f, 0f, 0f);
        float t = 0f;
        for (int i = 0; i < 40 && RangedCaster.ShotsFired == 0; i++, t += Dt) warden.Tick(t, Dt, hero.transform);
        Assert.AreEqual(1, RangedCaster.ShotsFired);
        var shot = Object.FindObjectOfType<DiabloProjectile>();
        Assert.IsNotNull(shot);
        Assert.IsTrue(shot.hostile);
    }
}
