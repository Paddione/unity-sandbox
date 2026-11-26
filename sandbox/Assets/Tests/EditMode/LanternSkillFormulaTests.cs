using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>second-class S2: Lanternbearer formulas; Fenwalker formulas unchanged.</summary>
public class LanternSkillFormulaTests
{
    GameObject hero;
    DiabloSkills skills;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        // Own empty scene: zone husks left open by other suites must not take halo hits.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        hero = new GameObject("FormulaHero");
        hero.AddComponent<DiabloProgression>();
        skills = hero.AddComponent<DiabloSkills>();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(hero);
        Wisp.ResetRun();
        RunProfile.Reset();
    }

    [Test]
    public void Fenwalker_DamageAndCooldownsUnchanged()
    {
        Assert.AreEqual(3, skills.EffectiveDamage(DiabloSkills.Skill.Fireball));
        Assert.AreEqual(2, skills.EffectiveDamage(DiabloSkills.Skill.Arrow));
        Assert.AreEqual(4, skills.EffectiveDamage(DiabloSkills.Skill.Cleave));
        Assert.AreEqual(1.0f, skills.EffectiveCooldown(DiabloSkills.Skill.Fireball), 0.0001f);
        Assert.AreEqual(6f, skills.EffectiveCooldown(DiabloSkills.Skill.Cleave), 0.0001f);
        var sheet = hero.GetComponent<DiabloProgression>().Sheet;
        sheet.focus = 5;
        sheet.fireballRank = 2;
        // 3 × (1 + 0.2·2) × (1 + 0.08·5) = 5.88 → 6, focus factor 1.
        Assert.AreEqual(6, skills.EffectiveDamage(DiabloSkills.Skill.Fireball));
    }

    [Test]
    public void Lanternbearer_BaseValues()
    {
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        Assert.AreEqual(4, skills.EffectiveDamage(DiabloSkills.Skill.LanternBolt));
        Assert.AreEqual(1, skills.EffectiveDamage(DiabloSkills.Skill.Wisps));
        Assert.AreEqual(3, skills.EffectiveDamage(DiabloSkills.Skill.SearingHalo));
        Assert.AreEqual(0, skills.EffectiveDamage(DiabloSkills.Skill.VeilStep));
        Assert.AreEqual(1.2f, skills.EffectiveCooldown(DiabloSkills.Skill.LanternBolt), 0.0001f);
        Assert.AreEqual(14f, skills.EffectiveCooldown(DiabloSkills.Skill.Wisps), 0.0001f);
        Assert.AreEqual(7f, skills.EffectiveCooldown(DiabloSkills.Skill.SearingHalo), 0.0001f);
        Assert.AreEqual(5f, skills.EffectiveCooldown(DiabloSkills.Skill.VeilStep), 0.0001f);
        Assert.AreEqual(3.5f, skills.haloRadius, 0.0001f);
        Assert.AreEqual(6f, skills.veilDistance, 0.0001f);
    }

    [Test]
    public void Lanternbearer_FocusScalesStrongerThanFenwalker()
    {
        var sheet = hero.GetComponent<DiabloProgression>().Sheet;
        sheet.focus = 5;
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        // 4 × (1 + 0.08·5·1.5) = 6.4 → 6.
        Assert.AreEqual(6, skills.EffectiveDamage(DiabloSkills.Skill.LanternBolt));
        // Cooldown focus reduction is class independent: 1.2 × (1 − 0.03·5).
        Assert.AreEqual(1.2f * 0.85f, skills.EffectiveCooldown(DiabloSkills.Skill.LanternBolt), 0.0001f);
    }

    [Test]
    public void WispCount_GrowsAtRankThree()
    {
        Assert.AreEqual(2, skills.WispCountFor(0));
        Assert.AreEqual(2, skills.WispCountFor(2));
        Assert.AreEqual(3, skills.WispCountFor(3));
        Assert.AreEqual(3, skills.WispCountFor(5));
    }

    [Test]
    public void ClassGate_CastsOnlyOwnSkills()
    {
        Assert.IsFalse(skills.CastToward(DiabloSkills.Skill.SearingHalo, Vector3.forward), "Fenwalker has no halo");
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        Assert.IsFalse(skills.CastToward(DiabloSkills.Skill.Cleave, Vector3.forward), "Lanternbearer has no cleave");
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.SearingHalo, Vector3.forward));
        Assert.Greater(skills.CooldownLeft(DiabloSkills.Skill.SearingHalo), 0f);
        Assert.AreEqual(0f, skills.CooldownLeft(DiabloSkills.Skill.Cleave), "separate cooldown tracks");
    }

    [Test]
    public void HudLine_FollowsTheClass()
    {
        StringAssert.Contains("[Q] Spalten", skills.HudLine());
        StringAssert.Contains("[E] Moorhaken", skills.HudLine());
        StringAssert.Contains("[G] Eisenschwur", skills.HudLine());
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        var line = skills.HudLine();
        StringAssert.Contains("[Q] Laternenblitz", line);
        StringAssert.Contains("[E] Irrlichter", line);
        StringAssert.Contains("[R] Sengender Kranz", line);
        StringAssert.Contains("[Leertaste] Schleierschritt", line);
    }

    [Test]
    public void WispsRank_ThreeAddsAThirdWisp()
    {
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        var sheet = hero.GetComponent<DiabloProgression>().Sheet;
        sheet.freeSkillPoints = 3;
        for (int i = 0; i < 3; i++) Assert.IsTrue(sheet.SpendSkill(DiabloSkills.Skill.Wisps));
        Assert.AreEqual(3, sheet.GetRank(DiabloSkills.Skill.Wisps));
        Assert.AreEqual(0, sheet.cleaveRank, "never leaks into Cleave");
        Assert.AreEqual(3, skills.SummonWisps(cosmetic: true).Count);
    }

    [Test]
    public void LanternSounds_AreSynthesized()
    {
        Assert.IsNotNull(DiabloAudio.LanternClip);
        Assert.IsNotNull(DiabloAudio.HaloClip);
        Assert.IsNotNull(DiabloAudio.VeilClip);
    }
}
