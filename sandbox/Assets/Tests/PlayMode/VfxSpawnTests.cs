using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// S3 VFX: every skill fires a pooled effect, the boss telegraph and the
/// phase-two burst fire, Rare+ drops earn a beam (Magic does not), and the
/// live particle budget stays under <see cref="PresentationVfx.MaxBudget"/>.
/// Headless-safe: counters increment even without a GPU backend.
/// </summary>
public class VfxSpawnTests
{
    GameObject hero;
    DiabloSkills skills;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        EnemyHealth.ResetRun();
        DiabloJuice.ResetRun();
        PresentationVfx.ResetRun();
        BossController.ResetRun();
        hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = "Player";
        hero.transform.position = new Vector3(0f, 1.05f, 0f);
        hero.AddComponent<CharacterController>();
        hero.AddComponent<DiabloClickController>();
        hero.AddComponent<DiabloMelee>();
        hero.AddComponent<PlayerHealth>();
        skills = hero.GetComponent<DiabloSkills>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var h in GameObject.FindGameObjectsWithTag("Husk"))
            Object.DestroyImmediate(h);
        foreach (var p in Object.FindObjectsOfType<DiabloProjectile>())
            Object.DestroyImmediate(p.gameObject);
        DiabloTestFixture.CleanupTestScene(hero);
        EnemyHealth.ResetRun();
        PresentationVfx.ResetRun();
        BossController.ResetRun();
        RunProfile.Reset();
    }

    [UnityTest]
    public IEnumerator Fireball_CastEmitsTrail()
    {
        yield return null;
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.Fireball, Vector3.forward));
        Assert.Greater(PresentationVfx.FireballTrails, 0, "Fireball must emit a trail.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Arrow_CastEmitsTrail()
    {
        yield return null;
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.Arrow, Vector3.forward));
        Assert.Greater(PresentationVfx.ArrowTrails, 0, "Arrow must emit a trail.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Cleave_CastEmitsArc()
    {
        yield return null;
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.Cleave, Vector3.forward));
        Assert.Greater(PresentationVfx.CleaveArcs, 0, "Cleave must emit an arc.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator LanternSkills_EmitVfx()
    {
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        yield return null;
        skills.DoHalo(cosmetic: true);
        Assert.Greater(PresentationVfx.CleaveArcs, 0, "Halo must emit a ring.");
        skills.DoVeilStep(Vector3.forward);
        Assert.Greater(PresentationVfx.VeilPuffs, 0, "Veil Step must emit puffs.");
        RunProfile.Reset();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Boss_TelegraphAndPhaseTwoFire()
    {
        yield return null;
        var boss = new GameObject(BossController.BossName);
        boss.tag = "Husk";
        boss.transform.position = new Vector3(0f, 0.05f, 2f); // inside slam range
        boss.AddComponent<EnemyHealth>().SetTier(BossController.BossMaxHP, LootTier.Boss);
        var ctrl = boss.AddComponent<BossController>();
        yield return null; // Update arms the telegraph when the hero is close
        Assert.Greater(PresentationVfx.BossTelegraphs, 0, "Slam windup must show a ground ring.");
        ctrl.EnterPhaseTwo();
        Assert.Greater(PresentationVfx.PhaseTwoBursts, 0, "Phase 2 must erupt.");
        Assert.AreEqual(1, BossController.PhaseTwoEntries);
        yield return null;
    }

    [Test]
    public void RareBeam_OnlyRarePlus()
    {
        Assert.IsFalse(PresentationVfx.ShouldBeamFor(ItemRarity.Magic), "Magic must stay quiet.");
        Assert.IsTrue(PresentationVfx.ShouldBeamFor(ItemRarity.Rare), "Rare must beam.");
        Assert.IsTrue(PresentationVfx.ShouldBeamFor(ItemRarity.Unique), "Unique must beam.");
    }

    [UnityTest]
    public IEnumerator LootDrop_RarePlusSpawnsBeam()
    {
        yield return null;
        int beams = PresentationVfx.RareBeams;
        var rng = new System.Random(1234);
        bool sawRare = false;
        for (int i = 0; i < 40; i++)
        {
            var orb = LootDrop.SpawnLoot(Vector3.zero, LootTier.Boss, rng, networked: false);
            if (orb == null) continue;
            var drop = orb.GetComponent<LootDrop>();
            if (drop != null && drop.item != null && PresentationVfx.ShouldBeamFor(drop.item.rarity))
                sawRare = true;
            Object.DestroyImmediate(orb);
        }
        Assert.IsTrue(sawRare, "Seeded Boss rolls must include a Rare+ drop.");
        Assert.Greater(PresentationVfx.RareBeams, beams, "Rare+ drops must spawn a beam.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Budget_StaysUnderMax()
    {
        yield return null;
        var at = new Vector3(0f, 1f, 0f);
        for (int i = 0; i < 20; i++)
        {
            PresentationVfx.FireballTrail(at);
            PresentationVfx.ArrowTrail(at);
            PresentationVfx.CleaveArc(at, 2.6f);
            PresentationVfx.BossTelegraph(at, 3f);
            PresentationVfx.PhaseTwoBurst(at);
            PresentationVfx.LevelUpPillar(at);
            PresentationVfx.RareBeam(at, ItemRarity.Rare);
            PresentationVfx.VeilPuff(at);
        }
        yield return null;
        Assert.LessOrEqual(PresentationVfx.ActiveParticles(), PresentationVfx.MaxBudget,
            $"Live particles must stay under budget ({PresentationVfx.MaxBudget}).");
    }
}
