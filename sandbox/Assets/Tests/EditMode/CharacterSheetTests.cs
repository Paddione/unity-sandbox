using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>S4 progression: point awards over level jumps, attribute/rank
/// effects, and point survival through the S2 save roundtrip.</summary>
public class CharacterSheetTests
{
    string path;
    GameObject hero;
    DiabloProgression prog;

    [SetUp]
    public void Setup()
    {
        path = Path.Combine(Path.GetTempPath(), $"cinderreach-s4-{System.Guid.NewGuid():N}.json");
        hero = new GameObject("Player");
        hero.AddComponent<Equipment>();
        prog = hero.AddComponent<DiabloProgression>();
    }

    [TearDown]
    public void Teardown()
    {
        if (hero != null) Object.DestroyImmediate(hero);
        SaveService.DeleteSave(path);
        SaveService.DeleteSave(); // AddXp autosaves to the default path; keep it clean.
        DiabloProgression.ResetRun();
    }

    [Test]
    public void LevelJump_AwardsOneAttributeAndOneSkillPointPerLevel()
    {
        int xp = DiabloProgression.XpToNext(1) + DiabloProgression.XpToNext(2) + DiabloProgression.XpToNext(3);
        prog.AddXp(xp);
        Assert.AreEqual(4, prog.Level);
        Assert.AreEqual(3, prog.Sheet.freeAttributePoints);
        Assert.AreEqual(3, prog.Sheet.freeSkillPoints);
    }

    [Test]
    public void SpendAttribute_NeedsFreePoint_AndVitalityAddsLife()
    {
        Assert.IsFalse(prog.SpendAttributePoint(CharacterSheet.Attribute.Strength));
        prog.Sheet.AwardLevel();
        float baseLife = prog.BonusLife;
        Assert.IsTrue(prog.SpendAttributePoint(CharacterSheet.Attribute.Vitality));
        Assert.AreEqual(1, prog.Sheet.vitality);
        Assert.AreEqual(0, prog.Sheet.freeAttributePoints);
        Assert.AreEqual(baseLife + CharacterSheet.VitalityLifePerPoint, prog.BonusLife, 0.001f);
    }

    [Test]
    public void SpendSkill_CapsAtFive_AndRankScalesDamage()
    {
        for (int i = 0; i < 6; i++) prog.Sheet.AwardLevel();
        for (int i = 0; i < 5; i++)
            Assert.IsTrue(prog.SpendSkillPoint(DiabloSkills.Skill.Fireball), $"rank {i + 1} must succeed");
        Assert.AreEqual(5, prog.Sheet.GetRank(DiabloSkills.Skill.Fireball));
        Assert.IsFalse(prog.SpendSkillPoint(DiabloSkills.Skill.Fireball), "rank 6 must fail");
        Assert.AreEqual(1f + 5f * CharacterSheet.RankDamageBonus,
            prog.Sheet.RankMultiplier(DiabloSkills.Skill.Fireball), 0.001f);
        Assert.AreEqual(1f, prog.Sheet.RankMultiplier(DiabloSkills.Skill.Arrow),
            "unranked skills stay at ×1");
    }

    [Test]
    public void Focus_ScalesSkillDamage_AndCooldowns()
    {
        var sheet = new CharacterSheet { focus = 4 };
        Assert.AreEqual(1f + 4f * CharacterSheet.FocusSkillDamagePerPoint,
            sheet.FocusSkillMultiplier(), 0.001f);
        Assert.AreEqual(1f - 4f * CharacterSheet.FocusCooldownPerPoint,
            sheet.CooldownMultiplier(), 0.001f);
        var capped = new CharacterSheet { focus = 100 };
        Assert.AreEqual(CharacterSheet.MinCooldownFactor, capped.CooldownMultiplier(), 0.001f);
    }

    [Test]
    public void CleaveRadius_GrowsWithRank()
    {
        var sheet = new CharacterSheet();
        const float @base = 2.6f, perRank = 0.4f;
        Assert.AreEqual(@base, sheet.CleaveRadius(@base, perRank), 0.001f);
        sheet.cleaveRank = 3;
        Assert.AreEqual(@base + 3f * perRank, sheet.CleaveRadius(@base, perRank), 0.001f);
    }

    [Test]
    public void Points_SurviveSaveRoundtrip()
    {
        hero.AddComponent<Inventory>();
        hero.AddComponent<Wallet>();
        prog.SetState(5, 77);
        prog.Sheet.freeAttributePoints = 1;
        prog.Sheet.freeSkillPoints = 2;
        prog.Sheet.strength = 2;
        prog.Sheet.vitality = 1;
        prog.Sheet.focus = 3;
        prog.Sheet.fireballRank = 3;
        prog.Sheet.arrowRank = 1;
        prog.Sheet.cleaveRank = 2;

        var save = SaveService.CaptureState(hero, zoneReached: 1);
        SaveService.WriteSave(save, path);

        var fresh = new GameObject("Player");
        fresh.AddComponent<Equipment>();
        fresh.AddComponent<DiabloProgression>();
        try
        {
            Assert.IsTrue(SaveService.TryLoad(fresh, path));
            var sheet = fresh.GetComponent<DiabloProgression>().Sheet;
            Assert.AreEqual(1, sheet.freeAttributePoints);
            Assert.AreEqual(2, sheet.freeSkillPoints);
            Assert.AreEqual(2, sheet.strength);
            Assert.AreEqual(1, sheet.vitality);
            Assert.AreEqual(3, sheet.focus);
            Assert.AreEqual(3, sheet.GetRank(DiabloSkills.Skill.Fireball));
            Assert.AreEqual(1, sheet.GetRank(DiabloSkills.Skill.Arrow));
            Assert.AreEqual(2, sheet.GetRank(DiabloSkills.Skill.Cleave));
        }
        finally
        {
            Object.DestroyImmediate(fresh);
        }
    }
}
