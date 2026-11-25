using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// A joining client loads the local save on spawn. A hero created this session
/// (never saved) must become that save first, or the join shows the previous or
/// default character instead of the one the player just made.
/// </summary>
public sealed class JoinCarriesCreatedHeroTests
{
    string path;
    GameObject hero;
    GameObject joined;
    bool prevInWorld;

    [SetUp]
    public void SetUp()
    {
        RunProfile.Reset();
        prevInWorld = ClassSelectUI.HeroInWorld;
        path = Path.Combine(Application.temporaryCachePath, "join-carry-test.json");
        if (File.Exists(path)) File.Delete(path);
    }

    [TearDown]
    public void TearDown()
    {
        ClassSelectUI.HeroInWorld = prevInWorld;
        if (hero != null) Object.Destroy(hero);
        if (joined != null) Object.Destroy(joined);
        if (File.Exists(path)) File.Delete(path);
        RunProfile.Reset();
    }

    static GameObject MakeHero(string name)
    {
        var go = new GameObject(name);
        go.AddComponent<Equipment>();
        go.AddComponent<Inventory>();
        go.AddComponent<Wallet>();
        go.AddComponent<DiabloProgression>();
        return go;
    }

    [Test]
    public void CarryRule_OnlyWithAHeroInWorldAndNotFromAClientSession()
    {
        Assert.IsTrue(MultiplayerAutoStarter.ShouldCarryHeroIntoJoin(true, false));
        Assert.IsFalse(MultiplayerAutoStarter.ShouldCarryHeroIntoJoin(false, false), "Start screen: the save is the chosen hero.");
        Assert.IsFalse(MultiplayerAutoStarter.ShouldCarryHeroIntoJoin(true, true), "A client never writes server state over its save.");
    }

    [UnityTest]
    public IEnumerator Join_BringsTheCreatedHeroInsteadOfTheOldSave()
    {
        // An older hero sits in the save.
        hero = MakeHero(DiabloNetworkPlayer.HeroName);
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        RunProfile.Hero = HeroProfile.Sanitized(new HeroProfile { heroName = "Wanderer", gender = 0, hairStyle = 0, hairColor = 0, build = 0 });
        SaveService.SaveNow(hero, -1, path);

        // This session the player creates a new character and plays it.
        RunProfile.ClassId = ClassCatalog.RogueId;
        var created = HeroProfile.Sanitized(new HeroProfile { heroName = "Sanna", gender = 1, hairStyle = 2, hairColor = 3, build = 1 });
        RunProfile.Hero = created;
        ClassSelectUI.HeroInWorld = true;
        yield return null;

        Assert.IsTrue(MultiplayerAutoStarter.CarryHeroIntoJoin(false, path));

        // The client hero spawns and loads the save.
        RunProfile.Reset();
        joined = MakeHero("JoinedHero");
        Assert.IsTrue(SaveService.TryLoad(joined, path));
        Assert.AreEqual(ClassCatalog.RogueId, RunProfile.ClassId);
        Assert.AreEqual("Sanna", RunProfile.Hero.heroName);
        Assert.AreEqual(created.gender, RunProfile.Hero.gender);
        Assert.AreEqual(created.hairStyle, RunProfile.Hero.hairStyle);
        Assert.AreEqual(created.hairColor, RunProfile.Hero.hairColor);
        Assert.AreEqual(created.build, RunProfile.Hero.build);
    }

    [UnityTest]
    public IEnumerator Join_FromTheStartScreenKeepsTheSave()
    {
        hero = MakeHero(DiabloNetworkPlayer.HeroName);
        RunProfile.Hero = HeroProfile.Sanitized(new HeroProfile { heroName = "Sanna", gender = 1 });
        SaveService.SaveNow(hero, -1, path);
        RunProfile.Hero = HeroProfile.Sanitized(new HeroProfile { heroName = "Wanderer" });
        ClassSelectUI.HeroInWorld = false;
        yield return null;

        Assert.IsFalse(MultiplayerAutoStarter.CarryHeroIntoJoin(false, path));
        joined = MakeHero("JoinedHero");
        Assert.IsTrue(SaveService.TryLoad(joined, path));
        Assert.AreEqual("Sanna", RunProfile.Hero.heroName, "Continue semantics: the saved hero joins.");
    }
}
