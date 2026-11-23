using NUnit.Framework;
using UnityEngine;

/// <summary>character-creation: hero profile pools, name rules, proportions, packing.</summary>
public class HeroAppearanceTests
{
    [Test]
    public void RandomNames_ComeFromTheGenderPool()
    {
        var rng = new System.Random(42);
        for (int i = 0; i < 40; i++)
        {
            Assert.Contains(HeroNames.RandomName(HeroGender.Female, rng), HeroNames.FemalePool);
            Assert.Contains(HeroNames.RandomName(HeroGender.Male, rng), HeroNames.MalePool);
            string neutral = HeroNames.RandomName(HeroGender.NonBinary, rng);
            Assert.IsTrue(System.Array.IndexOf(HeroNames.FemalePool, neutral) >= 0
                || System.Array.IndexOf(HeroNames.MalePool, neutral) >= 0
                || System.Array.IndexOf(HeroNames.NeutralPool, neutral) >= 0, neutral);
        }
    }

    [Test]
    public void CleanName_TrimsAndRejectsShortOrDirty()
    {
        Assert.AreEqual("Mira", HeroNames.Clean("  Mira  "));
        Assert.IsNull(HeroNames.Clean("A"));
        Assert.IsNull(HeroNames.Clean(""));
        Assert.IsNull(HeroNames.Clean(null));
        Assert.IsNull(HeroNames.Clean("<script>"));
        Assert.AreEqual("Jorin Hagedorn", HeroNames.Clean("Jorin   Hagedorn"));
    }

    [Test]
    public void Sanitized_FallsBackToRandomNameAndClampsIndices()
    {
        var raw = new HeroProfile { heroName = "x", gender = 99, hairStyle = -3, hairColor = 999, build = 42 };
        var clean = HeroProfile.Sanitized(raw, new System.Random(7));
        Assert.IsNotNull(HeroNames.Clean(clean.heroName), clean.heroName);
        Assert.That(clean.gender, Is.InRange(0, 2));
        Assert.That(clean.hairStyle, Is.InRange(0, HeroAppearance.HairStyleCount - 1));
        Assert.That(clean.hairColor, Is.InRange(0, HeroAppearance.HairColorCount - 1));
        Assert.That(clean.build, Is.InRange(0, 2));
    }

    [Test]
    public void Catalog_CountsMatchTheGoal()
    {
        Assert.That(HeroAppearance.HairStyleCount, Is.InRange(4, 6));
        Assert.That(HeroAppearance.HairColorCount, Is.InRange(8, 10));
        Assert.AreEqual(3, HeroAppearance.BuildCount);
    }

    [Test]
    public void Proportions_DifferByGenderAndBuild_ButStayCosmetic()
    {
        HeroAppearance.GetProportions(HeroGender.Female, HeroBuild.Slim, out var slim);
        HeroAppearance.GetProportions(HeroGender.Male, HeroBuild.Sturdy, out var sturdy);
        Assert.AreNotEqual(slim, sturdy);
        foreach (var p in new[] { slim, sturdy })
        {
            Assert.That(p.x, Is.InRange(0.85f, 1.25f), "chest scale stays near 1 (no collision change)");
            Assert.That(p.y, Is.InRange(0.85f, 1.25f));
            Assert.That(p.z, Is.InRange(0.85f, 1.25f));
        }
        HeroAppearance.GetProportions(HeroGender.Male, HeroBuild.Slim, out var maleSlim);
        HeroAppearance.GetProportions(HeroGender.Female, HeroBuild.Slim, out var femaleSlim);
        Assert.AreNotEqual(maleSlim, femaleSlim, "gender alone changes the silhouette");
    }

    [Test]
    public void AppearanceCode_RoundtripsEveryField()
    {
        var profile = new HeroProfile { heroName = "Mira", gender = (int)HeroGender.Male, hairStyle = 4, hairColor = 9, build = (int)HeroBuild.Sturdy };
        int code = HeroAppearance.Pack(profile);
        var back = HeroAppearance.Unpack(code, "Mira");
        Assert.AreEqual(profile.gender, back.gender);
        Assert.AreEqual(profile.hairStyle, back.hairStyle);
        Assert.AreEqual(profile.hairColor, back.hairColor);
        Assert.AreEqual(profile.build, back.build);
        Assert.AreEqual("Mira", back.heroName);
        var clamped = HeroAppearance.Unpack(-12345, null);
        Assert.That(clamped.hairStyle, Is.InRange(0, HeroAppearance.HairStyleCount - 1));
    }
}
