using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// act-two-opening S3 (PlayMode): the Glasswright engages inside its aggro
/// range, its lances cut a hero who stands in the line, broken pillars drop
/// the ward, and the kill books the zone boss.
/// </summary>
public class GlasswrightFightTests
{
    GameObject hero;
    GameObject boss;

    [SetUp]
    public void Setup()
    {
        DiabloJuice.ForceCrit = false;
        RunProfile.Reset();
        EnemyHealth.ResetRun();
        GlasswrightController.ResetRun();
        GlassWard.ResetRun();
        ShardLance.ResetRun();
        SessionComplete.ResetRun();
        RunProfile.Act.Unlock(ActProgress.Kiln);
        RunProfile.Act.CompleteAct();

        hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = "Player";
        hero.AddComponent<CharacterController>();
        hero.AddComponent<PlayerHealth>();
        hero.transform.position = BarrensLayout.GlasswrightPos + new Vector3(0f, 1f, 30f); // out of aggro
        BarrensLayout.PlazaSafe(hero.transform.position); // not on the plaza: breach latch

        boss = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        boss.name = GlasswrightController.BossName;
        boss.tag = "Husk";
        boss.transform.position = BarrensLayout.GlasswrightPos;
        boss.AddComponent<EnemyHealth>().SetTier(GlasswrightController.MaxHP, LootTier.Boss);
        boss.AddComponent<GlasswrightController>();
        boss.AddComponent<GlassWard>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var l in Object.FindObjectsOfType<ShardLance>()) Object.DestroyImmediate(l.gameObject);
        foreach (var f in Object.FindObjectsOfType<ShardField>()) Object.DestroyImmediate(f.gameObject);
        foreach (var h in Object.FindObjectsOfType<EnemyHealth>()) Object.DestroyImmediate(h.gameObject);
        foreach (var s in Object.FindObjectsOfType<SessionComplete>()) Object.DestroyImmediate(s);
        DiabloTestFixture.CleanupTestScene(hero);
        GlasswrightController.ResetRun();
        SessionComplete.ResetRun();
        RunProfile.Reset();
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    [UnityTest]
    public IEnumerator Lances_StayQuietOutOfRange_ThenCutAHeroInTheLine()
    {
        yield return new WaitForSeconds(1f);
        Assert.AreEqual(0, GlasswrightController.LanceVolleys, "out of aggro range: quiet");
        hero.transform.position = BarrensLayout.GlasswrightPos + new Vector3(0f, 1f, 7f);
        var hp = hero.GetComponent<PlayerHealth>();
        float full = hp.CurrentHP;
        yield return new WaitForSeconds(ShardLance.Telegraph + 0.4f);
        Assert.GreaterOrEqual(GlasswrightController.LanceVolleys, 1);
        Assert.IsTrue(boss.GetComponent<GlasswrightController>().Engaged);
        Assert.GreaterOrEqual(ShardLance.HeroHits, 1, "standing in the middle lance cuts");
        Assert.Less(hp.CurrentHP, full);
    }

    [UnityTest]
    public IEnumerator BrokenPillars_DropTheWard_AndTheKillBooksTheZoneBoss()
    {
        var glass = boss.GetComponent<GlasswrightController>();
        var health = boss.GetComponent<EnemyHealth>();
        health.TakeDamage(GlasswrightController.MaxHP / 2);
        yield return null; // Update sees ≤ 50 %
        Assert.IsTrue(glass.PhaseTwo);
        Assert.AreEqual(GlasswrightController.PillarCount, glass.LivePillars);
        foreach (var p in Object.FindObjectsOfType<EnemyHealth>())
            if (p.name.StartsWith(GlasswrightController.PillarPrefix)) p.TakeDamage(GlasswrightController.PillarHP);
        yield return null;
        Assert.AreEqual(0, glass.LivePillars);
        int before = health.CurrentHP;
        health.TakeDamage(10, false, boss.transform.position + Vector3.forward * 2f);
        Assert.AreEqual(before - 10, health.CurrentHP, "no ward without pillars");
        health.TakeDamage(health.CurrentHP, false, boss.transform.position + Vector3.forward * 2f);
        yield return null;
        Assert.AreEqual(1, GlasswrightController.Kills);
        Assert.IsTrue(RunProfile.Act.ZoneBossDefeated);
        Assert.IsTrue(SessionComplete.ZoneBossMode);
    }
}
