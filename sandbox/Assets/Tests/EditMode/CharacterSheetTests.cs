using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>S4 progression + cinderreach-levelup-empower: class-template
/// attribute awards over level jumps, attribute/rank effects, empowered
/// rank-5 multipliers and point survival through the S2 save roundtrip.</summary>
public class CharacterSheetTests
{
    string path;
    GameObject hero;
    DiabloProgression prog;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
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
        RunProfile.Reset();
    }

    [Test]
    public void LevelJump_AwardsClassTemplateAttributesAndOneSkillPointPerLevel()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        int xp = DiabloProgression.XpToNext(1) + DiabloProgression.XpToNext(2) + DiabloProgression.XpToNext(3);
        prog.AddXp(xp);
        Assert.AreEqual(4, prog.Level);
        Assert.AreEqual(0, prog.Sheet.freeAttributePoints, "attributes are auto-assigned");
        Assert.AreEqual(3, prog.Sheet.freeSkillPoints);
        // Fenwalker template 2/2/1 over three levels.
        Assert.AreEqual(6, prog.Sheet.strength);
        Assert.AreEqual(6, prog.Sheet.vitality);
        Assert.AreEqual(3, prog.Sheet.focus);
    }

    [Test]
    public void AwardLevel_AppliesEachClassTemplate()
    {
        var expected = new[]
        {
            (ClassCatalog.Fenwalker, 2, 2, 1),
            (ClassCatalog.Rogue, 2, 1, 2),
            (ClassCatalog.Lanternbearer, 1, 1, 3),
        };
        foreach (var (cls, str, vit, foc) in expected)
        {
            var sheet = new CharacterSheet();
            sheet.AwardLevel(cls);
            Assert.AreEqual(str, sheet.strength, $"{cls.id} strength");
            Assert.AreEqual(vit, sheet.vitality, $"{cls.id} vitality");
            Assert.AreEqual(foc, sheet.focus, $"{cls.id} focus");
            Assert.AreEqual(str + vit + foc, CharacterSheet.AttributesPerLevel, $"{cls.id} sums to 5");
            Assert.AreEqual(0, sheet.freeAttributePoints, $"{cls.id} no free points");
            Assert.AreEqual(1, sheet.freeSkillPoints, $"{cls.id} one skill point");
        }
    }

    [Test]
    public void AwardLevel_VitalityTemplateAddsLife()
    {
        var sheet = prog.Sheet;
        float baseLife = prog.BonusLife;
        sheet.AwardLevel(ClassCatalog.Fenwalker);
        Assert.AreEqual(2, sheet.vitality);
        Assert.AreEqual(baseLife + 2f * CharacterSheet.VitalityLifePerPoint, prog.BonusLife, 0.001f);
    }

    [Test]
    public void SetState_DistributesLegacyFreeAttributePoints()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        prog.SetState(3, 10, freeAttr: 4, freeSkill: 0,
            strength: 2, vitality: 1, focus: 0, ranks: null);
        // Fenwalker template order S,S,V,V,F distributes 4 leftover points as S,S,V,V.
        Assert.AreEqual(0, prog.Sheet.freeAttributePoints);
        Assert.AreEqual(4, prog.Sheet.strength);
        Assert.AreEqual(3, prog.Sheet.vitality);
        Assert.AreEqual(0, prog.Sheet.focus);
    }

    [Test]
    public void SpendSkill_CapsAtFive_AndRankFiveEmpowers()
    {
        for (int i = 0; i < 6; i++) prog.Sheet.AwardLevel(ClassCatalog.Fenwalker);
        for (int i = 0; i < 5; i++)
            Assert.IsTrue(prog.SpendSkillPoint(DiabloSkills.Skill.Fireball), $"rank {i + 1} must succeed");
        Assert.AreEqual(5, prog.Sheet.GetRank(DiabloSkills.Skill.Fireball));
        Assert.IsFalse(prog.SpendSkillPoint(DiabloSkills.Skill.Fireball), "rank 6 must fail");
        Assert.IsTrue(prog.Sheet.IsEmpowered(DiabloSkills.Skill.Fireball));
        Assert.AreEqual(1f + 5f * CharacterSheet.RankDamageBonus + CharacterSheet.EmpowerDamageBonus,
            prog.Sheet.RankMultiplier(DiabloSkills.Skill.Fireball), 0.001f);
        Assert.AreEqual(1f, prog.Sheet.RankMultiplier(DiabloSkills.Skill.Arrow),
            "unranked skills stay at ×1");
    }

    [Test]
    public void RankFour_StaysAtPlainMultiplier()
    {
        for (int i = 0; i < 4; i++) prog.Sheet.AwardLevel(ClassCatalog.Fenwalker);
        for (int i = 0; i < 4; i++) prog.Sheet.SpendSkill(DiabloSkills.Skill.Cleave);
        Assert.IsFalse(prog.Sheet.IsEmpowered(DiabloSkills.Skill.Cleave));
        Assert.AreEqual(1f + 4f * CharacterSheet.RankDamageBonus,
            prog.Sheet.RankMultiplier(DiabloSkills.Skill.Cleave), 0.001f);
    }

    [Test]
    public void GearRankBonus_DoesNotEmpower()
    {
        var sheet = new CharacterSheet { cleaveRank = 4 };
        Assert.AreEqual(1f + 5f * CharacterSheet.RankDamageBonus,
            sheet.RankMultiplier(DiabloSkills.Skill.Cleave, gearRankBonus: 1), 0.001f,
            "rank 4 + gear 1 scales like effective rank 5 but without the empower bonus");
        sheet.cleaveRank = 5;
        Assert.AreEqual(1f + 6f * CharacterSheet.RankDamageBonus + CharacterSheet.EmpowerDamageBonus,
            sheet.RankMultiplier(DiabloSkills.Skill.Cleave, gearRankBonus: 1), 0.001f,
            "rank 5 + gear 1 keeps the empower bonus on top of the gear rank");
    }

    [Test]
    public void IsEmpowered_FlagsOnlySpentRanks()
    {
        var sheet = new CharacterSheet { cleaveRank = 4 };
        Assert.IsFalse(sheet.IsEmpowered(DiabloSkills.Skill.Cleave));
        sheet.cleaveRank = 5;
        Assert.IsTrue(sheet.IsEmpowered(DiabloSkills.Skill.Cleave));
        Assert.IsFalse(sheet.IsEmpowered(DiabloSkills.Skill.Fireball), "unranked skill stays plain");
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
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
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
            // cinderreach-levelup-empower: the legacy free attribute point is
            // auto-assigned on load (Fenwalker order S,S,V,V,F → +1 Strength).
            Assert.AreEqual(0, sheet.freeAttributePoints);
            Assert.AreEqual(3, sheet.strength);
            Assert.AreEqual(1, sheet.vitality);
            Assert.AreEqual(3, sheet.focus);
            // class-kits-parity: Fireball/Arrow left the Fenwalker kit, their 4 ranks come back as points.
            Assert.AreEqual(2 + 4, sheet.freeSkillPoints);
            Assert.AreEqual(0, sheet.GetRank(DiabloSkills.Skill.Fireball));
            Assert.AreEqual(0, sheet.GetRank(DiabloSkills.Skill.Arrow));
            Assert.AreEqual(2, sheet.GetRank(DiabloSkills.Skill.Cleave), "kit ranks survive");
        }
        finally
        {
            Object.DestroyImmediate(fresh);
        }
    }
}
