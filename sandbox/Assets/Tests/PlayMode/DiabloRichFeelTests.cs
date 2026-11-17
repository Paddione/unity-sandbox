using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Feel pass: crits, damage numbers, impact bursts, shake, XP/levels, ambience.</summary>
public class DiabloRichFeelTests
{
    GameObject floor;
    GameObject player;
    GameObject cam;

    [SetUp]
    public void Setup()
    {
        HitStop.Enabled = false;
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        DiabloAudio.ResetRun();
        DiabloJuice.ResetRun();
        DiabloProgression.ResetRun();
        floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "TestFloor";
        cam = new GameObject("TestCam", typeof(Camera));
        cam.tag = "MainCamera";
        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.AddComponent<Equipment>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<CharacterController>();
        player.AddComponent<DiabloClickController>();
        player.AddComponent<DiabloMelee>();
    }

    [TearDown]
    public void TearDown()
    {
        DiabloTestFixture.CleanupTestScene(player);
        if (floor != null) Object.DestroyImmediate(floor);
        if (cam != null) Object.DestroyImmediate(cam);
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        DiabloJuice.ResetRun();
        DiabloProgression.ResetRun();
        HitStop.Enabled = true;
    }

    static GameObject SpawnHusk(Vector3 pos, int hp, LootTier tier = LootTier.Normal)
    {
        var husk = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        husk.name = "TestHusk";
        husk.tag = "Husk";
        husk.transform.position = pos;
        husk.AddComponent<EnemyHealth>().SetTier(hp, tier);
        return husk;
    }

    [Test]
    public void ClickController_AddsProgression()
    {
        Assert.IsNotNull(player.GetComponent<DiabloProgression>());
        Assert.IsNotNull(DiabloAmbience.Instance, "Local hero must bring up the ambience.");
    }

    [Test]
    public void Crit_DoublesDamage()
    {
        DiabloJuice.ForceCrit = true;
        Assert.AreEqual(6, DiabloJuice.RollCrit(player.transform, 3, out bool crit));
        Assert.IsTrue(crit);
        Assert.AreEqual(2, DiabloJuice.RollCrit(player.transform, 1, out _), "A 1-dmg crit must still add damage.");
        DiabloJuice.ForceCrit = false;
        Assert.AreEqual(3, DiabloJuice.RollCrit(player.transform, 3, out crit));
        Assert.IsFalse(crit);
    }

    [UnityTest]
    public IEnumerator CritStrike_ShowsGoldNumberAndPings()
    {
        DiabloJuice.ForceCrit = true;
        var husk = SpawnHusk(new Vector3(0f, 1.05f, 1.5f), 50);
        var melee = player.GetComponent<DiabloMelee>();
        int dmg = melee.CurrentDamage(player.transform);
        Assert.IsTrue(melee.TryAttack(husk, player.transform));
        Assert.AreEqual($"{dmg * 2}!", DiabloJuice.LastNumberText);
        Assert.AreEqual(1, DiabloAudio.soundedCrits);
        Assert.AreEqual(1, DiabloJuice.ActiveNumbers);
        yield return DiabloTestFixture.WaitSeconds(DiabloJuice.NumberLifetime + 0.2f);
        Assert.AreEqual(0, DiabloJuice.ActiveNumbers, "Numbers must expire.");
        Object.DestroyImmediate(husk);
    }

    [UnityTest]
    public IEnumerator Hits_ReuseOneParticleSystem()
    {
        yield return null;
        var husk = SpawnHusk(new Vector3(0f, 1.05f, 3f), 1000);
        var health = husk.GetComponent<EnemyHealth>();
        health.TakeDamage(1);
        int systems = DiabloJuice.ParticleSystemsCreated;
        int before = Object.FindObjectsOfType<ParticleSystem>().Length;
        for (int i = 0; i < 10; i++) health.TakeDamage(1);
        yield return null;
        Assert.AreEqual(systems, DiabloJuice.ParticleSystemsCreated, "Bursts must be pooled.");
        Assert.AreEqual(before, Object.FindObjectsOfType<ParticleSystem>().Length);
        Assert.Greater(DiabloJuice.BurstsEmitted, 10);
        Object.DestroyImmediate(husk);
    }

    [Test]
    public void Shake_DecaysAndRespectsSetting()
    {
        DiabloJuice.AddTrauma(player.transform.position, 0.5f);
        Assert.Greater(DiabloJuice.Trauma, 0.3f);
        bool was = DiabloSettings.ScreenShake;
        DiabloSettings.ScreenShake = false;
        Assert.AreEqual(Vector3.zero, DiabloJuice.ShakeOffset, "Shake off must mean no offset.");
        DiabloSettings.ScreenShake = was;
        DiabloJuice.AddTrauma(player.transform.position + Vector3.right * 100f, 1f);
        Assert.Less(DiabloJuice.Trauma, 0.6f, "Far events must not shake.");
    }

    [UnityTest]
    public IEnumerator Shake_FadesToZero()
    {
        DiabloJuice.AddTrauma(player.transform.position, 0.6f);
        yield return DiabloTestFixture.WaitSeconds(0.7f);
        Assert.AreEqual(0f, DiabloJuice.Trauma, 0.001f);
    }

    [UnityTest]
    public IEnumerator Kill_GrantsXp_AndLevelUpRaisesLife()
    {
        yield return null;
        var prog = player.GetComponent<DiabloProgression>();
        var hp = player.GetComponent<PlayerHealth>();
        float baseMax = hp.MaxHP;
        var husk = SpawnHusk(new Vector3(0f, 1.05f, 3f), 1);
        husk.GetComponent<EnemyHealth>().TakeDamage(1);
        yield return null;
        Assert.AreEqual(DiabloProgression.XpNormal, prog.Xp);

        // Fill sampled twice mid-tween must differ (animates, never snaps).
        yield return null;
        float a = prog.ShownFill;
        yield return new WaitForSecondsRealtime(0.08f);
        float b = prog.ShownFill;
        Assert.AreNotEqual(a, b, "XP bar must tween.");

        hp.TakeDamage(5f);
        prog.AddXp(DiabloProgression.XpToNext(1));
        Assert.AreEqual(2, prog.Level);
        Assert.AreEqual(1, DiabloProgression.LevelUps);
        Assert.AreEqual(1, DiabloAudio.soundedLevelUps);
        Assert.AreEqual(baseMax + DiabloProgression.LifePerLevel, hp.MaxHP, 0.001f);
        Assert.AreEqual(hp.MaxHP, hp.CurrentHP, 0.001f, "Level-up must refill life.");
    }

    [Test]
    public void XpCurve_Grows()
    {
        Assert.AreEqual(40, DiabloProgression.XpToNext(1));
        Assert.Greater(DiabloProgression.XpToNext(5), DiabloProgression.XpToNext(4));
        Assert.Greater(DiabloProgression.XpFor(LootTier.Elite), DiabloProgression.XpFor(LootTier.Normal));
    }

    [Test]
    public void SkillCasts_HaveTheirOwnSounds()
    {
        var skills = player.GetComponent<DiabloSkills>();
        skills.CastToward(DiabloSkills.Skill.Arrow, Vector3.forward);
        skills.CastToward(DiabloSkills.Skill.Fireball, Vector3.forward);
        Assert.AreEqual(2, DiabloAudio.soundedCasts);
        Assert.Greater(DiabloAudio.BlastClip.length, 0.3f);
        foreach (var p in Object.FindObjectsOfType<DiabloProjectile>())
            Object.DestroyImmediate(p.gameObject);
    }

    [Test]
    public void Ambience_LoopsBedAndEmbers()
    {
        var amb = DiabloAmbience.Instance;
        Assert.IsNotNull(amb);
        Assert.IsTrue(amb.Bed.loop);
        Assert.IsTrue(amb.Bed.isPlaying);
        Assert.Greater(amb.Bed.clip.length, 4f);
        Assert.IsTrue(amb.Motes.isPlaying);
    }
}
