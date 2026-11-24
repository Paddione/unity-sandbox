using NUnit.Framework;
using UnityEngine;

/// <summary>cinderreach-companion-rendering: gender base mapping and companion look (pure + EditMode rig).</summary>
public class CompanionLookTests
{
    GameObject hero;
    GameObject fakeRanger;
    GameObject fakeRogue;

    [TearDown]
    public void TearDown()
    {
        if (hero != null) Object.DestroyImmediate(hero);
        if (fakeRanger != null) Object.DestroyImmediate(fakeRanger);
        if (fakeRogue != null) Object.DestroyImmediate(fakeRogue);
        hero = fakeRanger = fakeRogue = null;
    }

    [Test]
    public void SkinModelPath_CoversEveryClassAndGender()
    {
        var fen = ClassCatalog.Fenwalker;
        var rog = ClassCatalog.Rogue;
        var lan = ClassCatalog.Lanternbearer;
        Assert.IsNull(ClassAppearance.SkinModelPath(fen, HeroGender.Male));
        Assert.IsNull(ClassAppearance.SkinModelPath(fen, HeroGender.NonBinary));
        Assert.AreEqual(ClassAppearance.RangerModelPath, ClassAppearance.SkinModelPath(fen, HeroGender.Female));
        Assert.AreEqual(ClassCatalog.Rogue.modelPath, ClassAppearance.SkinModelPath(rog, HeroGender.Male));
        Assert.AreEqual(ClassCatalog.Rogue.modelPath, ClassAppearance.SkinModelPath(rog, HeroGender.NonBinary));
        Assert.AreEqual(ClassAppearance.RogueHoodedModelPath, ClassAppearance.SkinModelPath(rog, HeroGender.Female));
        foreach (HeroGender g in System.Enum.GetValues(typeof(HeroGender)))
            Assert.AreEqual(ClassCatalog.Lanternbearer.modelPath, ClassAppearance.SkinModelPath(lan, g));
        Assert.IsNull(ClassAppearance.SkinModelPath(null, HeroGender.Male), "unknown class falls back to the Knight base");
    }

    [Test]
    public void CompanionProfile_IsStableAndDistinctPerSlot()
    {
        var a = CompanionFactory.CompanionProfile(ClassCatalog.Rogue, 0);
        var b = CompanionFactory.CompanionProfile(ClassCatalog.Rogue, 0);
        Assert.AreEqual(a.gender, b.gender);
        Assert.AreEqual(a.hairStyle, b.hairStyle);
        Assert.AreNotEqual(
            CompanionFactory.CompanionProfile(ClassCatalog.Fenwalker, 0).gender,
            CompanionFactory.CompanionProfile(ClassCatalog.Fenwalker, 1).gender,
            "slots read as distinct characters");
        for (int slot = 0; slot < PartyRoster.PartySize; slot++)
        {
            var p = CompanionFactory.CompanionProfile(ClassCatalog.Lanternbearer, slot);
            Assert.IsTrue(p.gender >= 0 && p.gender <= 2);
            Assert.IsTrue(p.hairStyle >= 0 && p.hairStyle < HeroAppearance.HairStyleCount);
            Assert.IsTrue(p.hairColor >= 0 && p.hairColor < HeroAppearance.HairColorCount);
            Assert.IsTrue(p.build >= 0 && p.build < HeroAppearance.BuildCount);
        }
    }

    /// <summary>Fake stock model: one skinned mesh on a "chest" bone.</summary>
    static GameObject FakeModel(string boneName)
    {
        var model = new GameObject("FakeModel");
        var bone = new GameObject(boneName);
        bone.transform.SetParent(model.transform, false);
        var smr = model.AddComponent<SkinnedMeshRenderer>();
        smr.sharedMesh = new Mesh();
        smr.bones = new[] { bone.transform };
        return model;
    }

    void BuildHero()
    {
        hero = new GameObject("TestHero");
        var chest = new GameObject("chest");
        chest.transform.SetParent(hero.transform, false);
        var baseMesh = hero.AddComponent<SkinnedMeshRenderer>();
        baseMesh.sharedMesh = new Mesh();
        hero.AddComponent<ClassAppearance>();
    }

    [Test]
    public void Apply_FemaleFenwalkerSkinsRangerAndHidesBase()
    {
        BuildHero();
        fakeRanger = FakeModel("chest");
        var look = hero.GetComponent<ClassAppearance>();
        look.rangerModel = fakeRanger;
        var profile = new HeroProfile { heroName = "Mira", gender = (int)HeroGender.Female, hairStyle = 4, hairColor = 1, build = 0 };
        look.Apply(ClassCatalog.Fenwalker, HeroProfile.Sanitized(profile));
        Assert.AreEqual(ClassCatalog.FenwalkerId, look.AppliedClassId);
        var baseMesh = hero.GetComponent<SkinnedMeshRenderer>();
        Assert.IsFalse(baseMesh.enabled, "Knight base hides under the Ranger skin");
        var skin = hero.transform.Find(ClassAppearance.SkinRootName);
        Assert.IsNotNull(skin, "Ranger skin binds onto the hero rig");
        Assert.IsTrue(skin.gameObject.activeSelf);
    }

    [Test]
    public void Apply_MaleFenwalkerKeepsKnightBase()
    {
        BuildHero();
        fakeRanger = FakeModel("chest");
        var look = hero.GetComponent<ClassAppearance>();
        look.rangerModel = fakeRanger;
        var profile = new HeroProfile { heroName = "Jorin", gender = (int)HeroGender.Male, hairStyle = 4, hairColor = 1, build = 0 };
        look.Apply(ClassCatalog.Fenwalker, HeroProfile.Sanitized(profile));
        Assert.IsTrue(hero.GetComponent<SkinnedMeshRenderer>().enabled);
        var skin = hero.transform.Find(ClassAppearance.SkinRootName);
        Assert.IsTrue(skin == null || !skin.gameObject.activeSelf, "no skin stands on the Knight base");
    }

    [Test]
    public void Apply_MissingModelFallsBackToBase()
    {
        BuildHero();
        var look = hero.GetComponent<ClassAppearance>();
        Assert.IsNull(look.rogueHoodedModel, "builder refs are empty in the test scene");
        var profile = new HeroProfile { heroName = "Runa", gender = (int)HeroGender.Female, hairStyle = 4, hairColor = 1, build = 0 };
        look.Apply(ClassCatalog.Rogue, HeroProfile.Sanitized(profile));
        Assert.AreEqual(ClassCatalog.RogueId, look.AppliedClassId);
        Assert.IsTrue(hero.GetComponent<SkinnedMeshRenderer>().enabled, "no crash, no hide without a model");
    }
}
