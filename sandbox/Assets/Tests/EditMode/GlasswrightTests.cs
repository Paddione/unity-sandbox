using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>act-two-opening S3: the Glasswright's lances, pillars and ward, Kindled nova, death bookkeeping and scene placement.</summary>
public class GlasswrightTests
{
    GameObject hero;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        GlasswrightController.ResetRun();
        GlassWard.ResetRun();
        ShardLance.ResetRun();
        ShatterNova.ResetRun();
        ShardField.ResetRun();
        SessionComplete.ResetRun();
        hero = new GameObject(DiabloNetworkPlayer.HeroName);
        hero.AddComponent<PlayerHealth>();
    }

    [TearDown]
    public void Teardown()
    {
        foreach (var t in new System.Type[] { typeof(ShardLance), typeof(ShatterNova), typeof(ShardField), typeof(GlasswrightController), typeof(SessionComplete) })
            foreach (var o in Object.FindObjectsByType(t, FindObjectsSortMode.None))
                if (o is Component c && c != null) Object.DestroyImmediate(c.gameObject);
        foreach (var h in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            if (h.name.StartsWith(GlasswrightController.PillarPrefix)) Object.DestroyImmediate(h.gameObject);
        if (hero != null) Object.DestroyImmediate(hero);
        SessionComplete.ResetRun();
        if (SaveService.HasSave()) SaveService.DeleteSave();
        RunProfile.Reset();
    }

    static GlasswrightController Boss()
    {
        var go = new GameObject(GlasswrightController.BossName);
        go.AddComponent<EnemyHealth>().SetTier(GlasswrightController.MaxHP, LootTier.Boss);
        var b = go.AddComponent<GlasswrightController>();
        go.AddComponent<GlassWard>();
        return b;
    }

    [Test]
    public void LanceRule_IsAStripAlongTheAim()
    {
        var o = Vector3.zero;
        Assert.IsTrue(ShardLance.Hits(o, Vector3.forward, new Vector3(0.5f, 0f, 6f)));
        Assert.IsFalse(ShardLance.Hits(o, Vector3.forward, new Vector3(0.7f, 0f, 6f)), "sidestep clears the 1.2 m strip");
        Assert.IsFalse(ShardLance.Hits(o, Vector3.forward, new Vector3(0f, 0f, -1f)), "never behind the boss");
        Assert.IsFalse(ShardLance.Hits(o, Vector3.forward, new Vector3(0f, 0f, ShardLance.Length + 0.5f)));
    }

    [Test]
    public void Lance_WarnsThenCutsOnce()
    {
        hero.transform.position = new Vector3(0f, 0f, 5f);
        var lance = ShardLance.Spawn(Vector3.zero, Vector3.forward);
        var hp = hero.GetComponent<PlayerHealth>();
        float full = hp.CurrentHP;
        float t0 = Time.time;
        Assert.IsFalse(lance.Tick(t0 + 0.5f, hero.transform));
        Assert.AreEqual(full, hp.CurrentHP, "telegraph never hurts");
        Assert.IsTrue(lance.Tick(t0 + ShardLance.Telegraph + 0.01f, hero.transform));
        Assert.AreEqual(full - ShardLance.Damage, hp.CurrentHP, 0.001f);
        Assert.IsFalse(lance.Tick(t0 + 5f, hero.transform), "once");
        Assert.AreEqual(1, ShardLance.HeroHits);
    }

    [Test]
    public void Volley_IsAFanOfThree()
    {
        var boss = Boss();
        boss.FireLancesNow(Time.time, boss.transform.position + new Vector3(0f, 0f, 8f));
        Assert.AreEqual(3, Object.FindObjectsByType<ShardLance>(FindObjectsSortMode.None).Length);
        Assert.AreEqual(1, GlasswrightController.LanceVolleys);
    }

    [Test]
    public void PhaseTwo_RaisesPillarsThatWardTheBoss()
    {
        var boss = Boss();
        var hp = boss.GetComponent<EnemyHealth>();
        hp.TakeDamage(10, false, boss.transform.position + Vector3.forward);
        Assert.AreEqual(GlasswrightController.MaxHP - 10, hp.CurrentHP, "no ward in phase 1");
        boss.EnterPhaseTwo();
        boss.EnterPhaseTwo();
        Assert.AreEqual(1, GlasswrightController.PhaseTwoEntries, "once");
        Assert.AreEqual(GlasswrightController.PillarCount, boss.LivePillars);
        int before = hp.CurrentHP;
        hp.TakeDamage(10, false, boss.transform.position + Vector3.forward);
        Assert.AreEqual(before - 5, hp.CurrentHP, "pillars halve directed hits");
        hp.TakeDamage(1, false, boss.transform.position + Vector3.forward);
        Assert.AreEqual(before - 6, hp.CurrentHP, "never below 1");
        Assert.AreEqual(2, GlassWard.Warded);
        hp.TakeDamage(10);
        Assert.AreEqual(before - 16, hp.CurrentHP, "undirected ticks bypass the ward");
    }

    [Test]
    public void PhaseTwo_Normal_CallsFields_Kindled_FiresNova()
    {
        var boss = Boss();
        boss.EnterPhaseTwo();
        var target = boss.transform.position + new Vector3(0f, 0f, 7f);
        boss.Think(100f, target);
        Assert.AreEqual(1, GlasswrightController.FieldsCalled);
        Assert.AreEqual(0, GlasswrightController.NovasFired);

        RunProfile.DifficultyUnlocked = true;
        RunProfile.Difficulty = RunProfile.Kindled;
        boss.Think(200f, target);
        Assert.AreEqual(1, GlasswrightController.FieldsCalled, "Kindled replaces the fields");
        Assert.AreEqual(1, GlasswrightController.NovasFired);
    }

    [Test]
    public void Nova_HitsOnlyOutsideTheSafeCircle()
    {
        Assert.IsFalse(ShatterNova.Hits(Vector3.zero, new Vector3(3f, 0f, 0f)));
        Assert.IsTrue(ShatterNova.Hits(Vector3.zero, new Vector3(5f, 0f, 0f)));
        hero.transform.position = new Vector3(6f, 0f, 0f);
        var nova = ShatterNova.Spawn(Vector3.zero);
        float t0 = Time.time;
        Assert.IsFalse(nova.Tick(t0 + 0.5f, hero.transform));
        Assert.IsTrue(nova.Tick(t0 + GlasswrightController.NovaWindup + 0.01f, hero.transform));
        Assert.AreEqual(1, ShatterNova.HeroHits);
    }

    [Test]
    public void Death_DefeatsZoneBoss_SavesAndShowsClearedScreen()
    {
        RunProfile.Act.Unlock(ActProgress.Kiln);
        RunProfile.Act.CompleteAct();
        var boss = Boss();
        boss.OnEnemyDeath();
        boss.OnEnemyDeath();
        Assert.AreEqual(1, GlasswrightController.Kills);
        Assert.IsTrue(RunProfile.Act.ZoneBossDefeated);
        Assert.IsTrue(SaveService.HasSave(), "first kill autosaves");
        Assert.IsTrue(SessionComplete.ZoneBossMode);
        Assert.IsFalse(SessionComplete.ActOneMode);
        Assert.AreEqual(1, SessionComplete.Shown);
        Assert.AreEqual("PHASE I", DiabloTargetBar.PhaseLabel(boss.GetComponent<EnemyHealth>()));
    }

    [Test]
    public void Scene_PlacesTheGlasswrightBehindItsElites()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/DiabloZone.unity", OpenSceneMode.Single);
        var go = GameObject.Find(GlasswrightController.BossName);
        Assert.IsNotNull(go);
        Assert.IsNotNull(go.GetComponent<GlassWard>());
        Assert.IsNotNull(go.GetComponent<EnemyAttack>());
        Assert.AreEqual(LootTier.Boss, go.GetComponent<EnemyHealth>().tier);
        Assert.AreEqual(GlasswrightController.MaxHP, go.GetComponent<EnemyHealth>().maxHP);
        Assert.Less(Vector3.Distance(go.transform.position, BarrensLayout.GlasswrightPos), 0.01f);
        var arrival = BarrensLayout.BarrensArrival - go.transform.position;
        arrival.y = 0f;
        Assert.Greater(arrival.magnitude, GlasswrightController.AggroRange + 10f, "no boss pull from the arrival");
        Assert.AreEqual(GlasswrightController.BossName, DiabloTargetBar.DisplayName(go.name));
        Assert.AreEqual("Glass Pillar", DiabloTargetBar.DisplayName("Glass Pillar_2_1"));
    }
}
