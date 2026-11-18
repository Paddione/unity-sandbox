using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>cinderreach-party-roles S1: roles, Rogue catalog, base set for all, x1.5 specialty contract.</summary>
public class PartyRoleDataTests
{
    [Test]
    public void Catalog_HasThreeRolesWithStableIds()
    {
        Assert.AreEqual(3, ClassCatalog.All.Length);
        Assert.AreEqual(ClassDefinition.PartyRole.Tank, ClassCatalog.Fenwalker.role);
        Assert.AreEqual(ClassDefinition.PartyRole.Healer, ClassCatalog.Lanternbearer.role);
        Assert.AreEqual(ClassDefinition.PartyRole.Dps, ClassCatalog.Rogue.role);
        Assert.AreSame(ClassCatalog.Rogue, ClassCatalog.Get(ClassCatalog.RogueId));
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Unknown class id 'paladin'"));
        Assert.AreSame(ClassCatalog.Fenwalker, ClassCatalog.Get("paladin"));
    }

    [Test]
    public void Rogue_FactorsModelAndFallback()
    {
        Assert.AreEqual(1.5f, ClassCatalog.Rogue.focusSkillFactor);
        Assert.AreEqual(1.5f, ClassCatalog.Rogue.strengthMeleeFactor);
        Assert.AreEqual(1.5f, ClassCatalog.Rogue.specialtyFactor);
        Assert.AreEqual("Adventurers/Characters/Rogue.fbx", ClassCatalog.Rogue.modelPath);
        Assert.IsTrue(System.IO.File.Exists("Assets/ThirdParty/KayKit/" + ClassCatalog.Rogue.modelPath));
        // Fenwalker/Lanternbearer factors stay untouched (second-class balance).
        Assert.AreEqual(1f, ClassCatalog.Fenwalker.focusSkillFactor);
        Assert.AreEqual(1f, ClassCatalog.Fenwalker.strengthMeleeFactor);
        Assert.AreEqual(1f, ClassCatalog.Fenwalker.specialtyFactor);
        Assert.AreEqual(1.5f, ClassCatalog.Lanternbearer.focusSkillFactor);
        Assert.AreEqual(0.5f, ClassCatalog.Lanternbearer.strengthMeleeFactor);
        Assert.AreEqual(1.5f, ClassCatalog.Lanternbearer.specialtyFactor);
    }

    [Test]
    public void BaseSkills_CastableByEveryClass()
    {
        foreach (var cls in ClassCatalog.All)
        {
            Assert.IsTrue(cls.CanCast(DiabloSkills.Skill.Fireball), cls.id);
            Assert.IsTrue(cls.CanCast(DiabloSkills.Skill.Arrow), cls.id);
        }
        // Specialty stays class-bound in S1: Cleave is Fenwalker-only, Lantern skills Healer-only.
        Assert.IsTrue(ClassCatalog.Fenwalker.CanCast(DiabloSkills.Skill.Cleave));
        Assert.IsFalse(ClassCatalog.Lanternbearer.CanCast(DiabloSkills.Skill.Cleave));
        Assert.IsFalse(ClassCatalog.Rogue.CanCast(DiabloSkills.Skill.Cleave));
        Assert.IsFalse(ClassCatalog.Fenwalker.CanCast(DiabloSkills.Skill.LanternBolt));
        Assert.IsTrue(ClassCatalog.Lanternbearer.CanCast(DiabloSkills.Skill.LanternBolt));
    }

    [Test]
    public void Specialty_FactorContract()
    {
        var sheet = new CharacterSheet { focus = 10 };
        // LanternBolt is Lanternbearer specialty: 1.5x over the same rank/focus without the factor.
        float specialty = sheet.SpecialtySkillMultiplier(DiabloSkills.Skill.LanternBolt, ClassCatalog.Lanternbearer);
        float plain = sheet.SkillMultiplier(DiabloSkills.Skill.LanternBolt, 0, ClassCatalog.Lanternbearer.focusSkillFactor);
        Assert.AreEqual(plain * 1.5f, specialty, 0.0001f);
        // Base skill of the same class has no specialty factor.
        float baseMul = sheet.SpecialtySkillMultiplier(DiabloSkills.Skill.Fireball, ClassCatalog.Lanternbearer);
        float basePlain = sheet.SkillMultiplier(DiabloSkills.Skill.Fireball, 0, ClassCatalog.Lanternbearer.focusSkillFactor);
        Assert.AreEqual(basePlain, baseMul, 0.0001f);
        // Tank specialty factor is control, not damage: Cleave multiplier stays 1x.
        float cleave = sheet.SpecialtySkillMultiplier(DiabloSkills.Skill.Cleave, ClassCatalog.Fenwalker);
        Assert.AreEqual(sheet.SkillMultiplier(DiabloSkills.Skill.Cleave, 0, 1f), cleave, 0.0001f);
    }

    [Test]
    public void SkillEnum_KeepsNetworkValuesWithRogueCatalogOnly()
    {
        Assert.AreEqual(0, (int)DiabloSkills.Skill.Fireball);
        Assert.AreEqual(1, (int)DiabloSkills.Skill.Arrow);
        Assert.AreEqual(2, (int)DiabloSkills.Skill.Cleave);
        Assert.AreEqual(3, (int)DiabloSkills.Skill.LanternBolt);
        Assert.AreEqual(6, (int)DiabloSkills.Skill.VeilStep);
    }

    [Test]
    public void Rogue_UniquePoolIsClassCorrect()
    {
        var rng = new System.Random(7);
        bool sawBuild = false, sawLegacy = false;
        for (int i = 0; i < 200; i++)
        {
            var item = ItemDatabase.UniqueWeaponForClass(ClassCatalog.RogueId, rng);
            Assert.IsTrue(Equipment.CanEquip(item, ClassCatalog.RogueId), item.uniqueId);
            Assert.IsFalse(Equipment.CanEquip(item, ClassCatalog.FenwalkerId), item.uniqueId);
            if (item.uniqueId == ItemDatabase.NightfangId) sawBuild = true;
            else if (item.uniqueId == ItemDatabase.RogueKeepsakeId) sawLegacy = true;
        }
        Assert.IsTrue(sawBuild && sawLegacy);
    }
}
