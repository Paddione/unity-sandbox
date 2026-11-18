using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>S1 PlayMode: Kindled finale wiring on the Kilnmother.</summary>
public class KindledFinaleTests
{
    GameObject hero;
    GameObject mother;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        KilnmotherController.ResetRun();
        KilnSectorPattern.ResetRun();
        SessionComplete.ResetRun();
        BurningGround.ResetRun();
        hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = DiabloNetworkPlayer.HeroName;
        hero.transform.position = new Vector3(0f, 1f, 40f);
        hero.AddComponent<DiabloProgression>();
        hero.AddComponent<Equipment>();
        hero.AddComponent<Inventory>();
        hero.AddComponent<Wallet>();
        hero.AddComponent<PlayerHealth>();
        mother = new GameObject(KilnLayout.KilnmotherName);
        mother.tag = "Husk";
        mother.transform.position = Vector3.zero;
        mother.AddComponent<EnemyHealth>().SetTier(KilnmotherController.MaxHP, LootTier.ActBoss);
        mother.AddComponent<KilnmotherController>();
    }

    [TearDown]
    public void TearDown()
    {
        if (mother != null) Object.DestroyImmediate(mother);
        DiabloTestFixture.CleanupTestScene(hero);
        foreach (var b in Object.FindObjectsOfType<BurningGround>()) Object.DestroyImmediate(b.gameObject);
        if (SaveService.HasSave()) SaveService.DeleteSave();
        RunProfile.Reset();
        KilnmotherController.ResetRun();
        KilnSectorPattern.ResetRun();
        SessionComplete.ResetRun();
    }

    static void Kindled()
    {
        RunProfile.DifficultyUnlocked = true;
        RunProfile.Difficulty = RunProfile.Kindled;
        RunProfile.RunSeed = 11;
    }

    [UnityTest]
    public IEnumerator KindledPhaseTwo_FiresSectors_NotFields()
    {
        Kindled();
        var ctrl = mother.GetComponent<KilnmotherController>();
        Assert.IsTrue(KilnmotherController.KindledFinaleActive);
        ctrl.EnterPhaseTwo();
        var p = ctrl.FireSectorPatternNow(100f);
        Assert.IsNotNull(p);
        Assert.LessOrEqual(p.sectors.Count, 2);
        Assert.AreEqual(1, KilnmotherController.SectorPatternsFired);
        Assert.AreEqual(0, BurningGround.Ticks);
        yield return null;
        ctrl.ClearSectorPatterns();
        Assert.AreEqual(0, ctrl.ActiveSectorPatternCount);
    }

    [UnityTest]
    public IEnumerator WarnBeforeDamage_MaxOneHit()
    {
        Kindled();
        var ctrl = mother.GetComponent<KilnmotherController>();
        var p = ctrl.FireSectorPatternNow(200f);
        var hp = hero.GetComponent<PlayerHealth>();
        float full = hp.CurrentHP;
        // Hero inside an active sector: pick sector midpoint.
        float ang = p.sectors[0] * 90f * Mathf.Deg2Rad;
        DiabloTestFixture.TeleportHero(hero, new Vector3(Mathf.Cos(ang) * 5f, 1f, Mathf.Sin(ang) * 5f));
        bool expired = p.Tick(200.5f, hero.transform.position, null); // warning
        Assert.IsFalse(expired);
        Assert.AreEqual(full, hp.CurrentHP, 0.001f, "no damage during warning");
        p.Tick(202f, hero.transform.position, null); // burn
        Assert.Less(hp.CurrentHP, full, "burn hits once");
        float afterFirst = hp.CurrentHP;
        p.Tick(202.5f, hero.transform.position, null);
        p.Tick(203f, hero.transform.position, null);
        Assert.AreEqual(afterFirst, hp.CurrentHP, 0.001f, "max one hit per pattern");
        yield return null;
    }

    [UnityTest]
    public IEnumerator SafeRoute_Traversable()
    {
        Kindled();
        var ctrl = mother.GetComponent<KilnmotherController>();
        var p = ctrl.FireSectorPatternNow(300f);
        int safe = -1;
        for (int s = 0; s < 4; s++) if (!p.sectors.Contains(s)) { safe = s; break; }
        Assert.GreaterOrEqual(safe, 0);
        float ang = safe * 90f * Mathf.Deg2Rad;
        DiabloTestFixture.TeleportHero(hero, new Vector3(Mathf.Cos(ang) * 5f, 1f, Mathf.Sin(ang) * 5f));
        var hp = hero.GetComponent<PlayerHealth>();
        float full = hp.CurrentHP;
        for (float t = 301.6f; t < 303.4f; t += 0.4f) p.Tick(t, hero.transform.position, null);
        Assert.AreEqual(full, hp.CurrentHP, 0.001f, "safe sector stays traversable through the burn");
        yield return null;
    }

    [UnityTest]
    public IEnumerator NormalPhaseTwo_Unchanged()
    {
        Assert.AreEqual(RunProfile.Normal, RunProfile.Difficulty);
        Assert.IsFalse(KilnmotherController.KindledFinaleActive);
        var ctrl = mother.GetComponent<KilnmotherController>();
        ctrl.EnterPhaseTwo();
        Assert.AreEqual(0, ctrl.ActiveSectorPatternCount, "no sectors on Normal");
        Assert.AreEqual(0, KilnSectorPattern.PatternsFired);
        Assert.AreEqual(KilnmotherController.SummonCount, 4, "summons stay");
        Assert.AreEqual(2.5f, KilnmotherController.WaveIntervalPhaseTwo, 0.001f);
        Assert.AreEqual(5f, KilnmotherController.FieldInterval, 0.001f);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Death_ClearsPatterns()
    {
        Kindled();
        var ctrl = mother.GetComponent<KilnmotherController>();
        ctrl.EnterPhaseTwo();
        ctrl.FireSectorPatternNow(400f);
        Assert.AreEqual(1, ctrl.ActiveSectorPatternCount);
        mother.GetComponent<EnemyHealth>().TakeDamage(KilnmotherController.MaxHP);
        yield return null;
        Assert.AreEqual(0, ctrl.ActiveSectorPatternCount, "death clears the effect");
    }
}
