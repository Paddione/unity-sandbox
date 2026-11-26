using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>cinderreach-party-roles S3: Rogue and Healer formulas; pre-party skills keep legacy scaling.</summary>
public class PartySkillFormulaTests
{
    GameObject hero;
    DiabloSkills skills;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        hero = new GameObject("PartyFormulaHero");
        hero.AddComponent<DiabloProgression>();
        skills = hero.AddComponent<DiabloSkills>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(hero);
        RunProfile.Reset();
    }

    [Test]
    public void SkillEnum_AppendsPartySkillsWithoutMovingOldValues()
    {
        Assert.AreEqual(2, (int)DiabloSkills.Skill.Cleave);
        Assert.AreEqual(7, (int)DiabloSkills.Skill.SeismicSlam);
        Assert.AreEqual(8, (int)DiabloSkills.Skill.QuickShot);
        Assert.AreEqual(9, (int)DiabloSkills.Skill.BladeFlurry);
        Assert.AreEqual(10, (int)DiabloSkills.Skill.MendingLight);
        Assert.AreEqual(11, (int)DiabloSkills.Skill.RenewingCircle);
    }

    [Test]
    public void Rogue_BaseValues()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        Assert.AreEqual(3, skills.EffectiveDamage(DiabloSkills.Skill.QuickShot), "2 base x 1.5 specialty");
        Assert.AreEqual(3, skills.EffectiveDamage(DiabloSkills.Skill.BladeFlurry), "2 base x 1.5 specialty");
        Assert.AreEqual(0.3f, skills.EffectiveCooldown(DiabloSkills.Skill.QuickShot), 0.0001f);
        Assert.AreEqual(4f, skills.EffectiveCooldown(DiabloSkills.Skill.BladeFlurry), 0.0001f);
        Assert.AreEqual(20f, skills.quickShotRange, 0.0001f);
        Assert.AreEqual(2.4f, skills.flurryRadius, 0.0001f);
    }

    [Test]
    public void Healer_BaseValues()
    {
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        Assert.AreEqual(12, skills.EffectiveHeal(DiabloSkills.Skill.MendingLight), "8 base x 1.5 specialty");
        Assert.AreEqual(8, skills.EffectiveHeal(DiabloSkills.Skill.RenewingCircle), "5 base x 1.5 specialty");
        Assert.AreEqual(5f, skills.EffectiveCooldown(DiabloSkills.Skill.MendingLight), 0.0001f);
        Assert.AreEqual(12f, skills.EffectiveCooldown(DiabloSkills.Skill.RenewingCircle), 0.0001f);
        Assert.AreEqual(12f, skills.targetHealRange, 0.0001f);
        Assert.AreEqual(6f, skills.groupHealRadius, 0.0001f);
    }

    [Test]
    public void PrePartySkills_KeepLegacyScaling()
    {
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        Assert.AreEqual(4, skills.EffectiveDamage(DiabloSkills.Skill.LanternBolt), "no specialty factor on old skills");
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        Assert.AreEqual(3, skills.EffectiveDamage(DiabloSkills.Skill.Fireball));
        Assert.AreEqual(4, skills.EffectiveDamage(DiabloSkills.Skill.Cleave));
        Assert.AreEqual(3, skills.EffectiveDamage(DiabloSkills.Skill.SeismicSlam), "tank factor is control, not damage");
    }

    [Test]
    public void RogueSpecialty_ScalesStrongerThanBase()
    {
        var sheet = hero.GetComponent<DiabloProgression>().Sheet;
        sheet.focus = 5;
        RunProfile.ClassId = ClassCatalog.RogueId;
        // 2 x (1 + 0.08·5·1.5) x 1.5 = 4.8 -> 5.
        Assert.AreEqual(5, skills.EffectiveDamage(DiabloSkills.Skill.QuickShot));
        // Same rank/focus on the shared base skill: 2 x (1 + 0.08·5·1.5) = 3.2 -> 3.
        float baseMul = sheet.SkillMultiplier(DiabloSkills.Skill.Arrow, 0, ClassCatalog.Rogue.focusSkillFactor);
        Assert.AreEqual(3, Mathf.RoundToInt(skills.arrowDamage * baseMul));
    }

    [Test]
    public void ClassGate_BindsNewSpecialties()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        Assert.IsFalse(skills.CastToward(DiabloSkills.Skill.QuickShot, Vector3.forward), "tank has no quick shot");
        Assert.IsFalse(skills.CastToward(DiabloSkills.Skill.MendingLight, Vector3.forward), "tank has no mend");
        RunProfile.ClassId = ClassCatalog.RogueId;
        Assert.IsFalse(skills.CastToward(DiabloSkills.Skill.Cleave, Vector3.forward), "rogue has no cleave");
        Assert.IsFalse(skills.CastToward(DiabloSkills.Skill.MendingLight, Vector3.forward), "rogue has no mend");
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        Assert.IsFalse(skills.CastToward(DiabloSkills.Skill.QuickShot, Vector3.forward), "healer has no quick shot");
        Assert.IsTrue(ClassCatalog.Rogue.CanCast(DiabloSkills.Skill.QuickShot));
        Assert.IsTrue(ClassCatalog.Lanternbearer.CanCast(DiabloSkills.Skill.MendingLight));
    }

    [Test]
    public void Mend_WithoutTargetCastsNothingAndSkipsCooldown()
    {
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        Assert.IsFalse(skills.CastToward(DiabloSkills.Skill.MendingLight, Vector3.forward), "nobody hurt");
        Assert.AreEqual(0f, skills.CooldownLeft(DiabloSkills.Skill.MendingLight), 0.0001f);
    }

    [Test]
    public void HudLine_NamesNewSkills()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        StringAssert.Contains("[Q] Schnellschuss", skills.HudLine());
        StringAssert.Contains("[E] Klingenwirbel", skills.HudLine());
        StringAssert.Contains("[Leertaste] Schattenrolle", skills.HudLine());
        StringAssert.Contains("[G] Pulverfass", skills.HudLine());
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        StringAssert.Contains("[F] Heilendes Licht", skills.HudLine());
        StringAssert.Contains("[G] Erneuernder Kreis", skills.HudLine());
    }
}
