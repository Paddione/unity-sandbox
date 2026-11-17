using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>S4 ranks + Cleave: rank scales ApplyHit damage, Cleave (R) hits
/// every husk in radius, and the Cleave arc art ships in Resources.</summary>
public class SkillRankTests
{
    GameObject floor;
    GameObject player;
    DiabloSkills skills;
    DiabloProgression prog;

    [SetUp]
    public void Setup()
    {
        HitStop.Enabled = false;
        DiabloJuice.ForceCrit = false;
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        DiabloProgression.ResetRun();
        DiabloProjectile.ResetRun();
        floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "TestFloor";
        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.AddComponent<CharacterController>();
        player.AddComponent<DiabloMelee>();
        player.AddComponent<DiabloClickController>();
        skills = player.GetComponent<DiabloSkills>();
        prog = player.GetComponent<DiabloProgression>();
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
        DiabloProgression.ResetRun();
        DiabloProjectile.ResetRun();
        DiabloJuice.ForceCrit = null;
        HitStop.Enabled = true;
        SaveService.DeleteSave(); // rank spends autosave to the default path
        DiabloProgression.ResetRun();
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

    [UnityTest]
    public IEnumerator Cleave_HitsAllHusksInRadius()
    {
        yield return null;
        var a = SpawnHusk(new Vector3(1f, 1.05f, 0.5f), 3);
        var b = SpawnHusk(new Vector3(-1.5f, 1.05f, -1f), 3);
        var c = SpawnHusk(new Vector3(0f, 1.05f, -2.4f), 3);
        var far = SpawnHusk(new Vector3(0f, 1.05f, 8f), 3);
        _ = (a, b, c);

        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.Cleave, Vector3.forward));
        yield return null;

        Assert.IsTrue(a == null && b == null && c == null,
            "Every husk inside the cleave radius must die.");
        Assert.IsTrue(far != null, "Husks outside the cleave radius must survive.");
        Assert.AreEqual(3, EnemyHealth.HusksKilled);
    }

    [UnityTest]
    public IEnumerator Rank_IncreasesApplyHitDamage()
    {
        yield return null;
        var plain = SpawnHusk(new Vector3(0f, 1.05f, 6f), 100);
        var ranked = SpawnHusk(new Vector3(2f, 1.05f, 6f), 100);

        int baseDmg = skills.EffectiveDamage(DiabloSkills.Skill.Fireball);
        DiabloSkills.ApplyHit(plain, baseDmg, player.transform.position, 0f, player.transform);

        prog.Sheet.AwardLevel();
        prog.Sheet.AwardLevel();
        Assert.IsTrue(prog.SpendSkillPoint(DiabloSkills.Skill.Fireball));
        Assert.IsTrue(prog.SpendSkillPoint(DiabloSkills.Skill.Fireball));
        int rankedDmg = skills.EffectiveDamage(DiabloSkills.Skill.Fireball);
        Assert.Greater(rankedDmg, baseDmg, "Two fireball ranks must raise the cast damage.");
        DiabloSkills.ApplyHit(ranked, rankedDmg, player.transform.position, 0f, player.transform);

        Assert.AreEqual(1f - baseDmg / 100f, plain.GetComponent<EnemyHealth>().HPFraction(), 0.001f);
        Assert.AreEqual(1f - rankedDmg / 100f, ranked.GetComponent<EnemyHealth>().HPFraction(), 0.001f);
    }

    [UnityTest]
    public IEnumerator Cleave_Cooldown_BlocksImmediateRecast()
    {
        yield return null;
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.Cleave, Vector3.forward));
        Assert.IsFalse(skills.CastToward(DiabloSkills.Skill.Cleave, Vector3.forward),
            "6 s cleave cooldown must gate the second cast.");
        Assert.Greater(skills.CooldownLeft(DiabloSkills.Skill.Cleave), 5f);
    }

    [Test]
    public void SkillArt_CleaveArcPrefabShips()
    {
        var arc = Resources.Load<GameObject>("SkillArt/CleaveArc");
        Assert.IsNotNull(arc, "Run SkillArtBuilder.Build.");
        Assert.Greater(arc.GetComponentsInChildren<ParticleSystem>().Length, 0,
            "Cleave needs its sweep ring.");
    }
}
