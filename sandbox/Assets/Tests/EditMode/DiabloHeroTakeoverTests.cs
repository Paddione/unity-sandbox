using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Host/client zones: the owned network hero replaces the scene's solo hero (no double).</summary>
public class DiabloHeroTakeoverTests
{
    GameObject solo;
    GameObject owned;

    [SetUp]
    public void SetUp()
    {
        // Earlier suites leave a zone scene open (with its own "Player").
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [TearDown]
    public void TearDown()
    {
        if (solo != null) Object.DestroyImmediate(solo);
        if (owned != null) Object.DestroyImmediate(owned);
    }

    [Test]
    public void OwnedHero_HidesSoloHeroAndTakesItsName()
    {
        solo = new GameObject(DiabloNetworkPlayer.HeroName);
        owned = new GameObject("NetworkPlayerDiablo(Clone)");
        var stoodDown = DiabloNetworkPlayer.TakeOverSoloHero(owned);
        Assert.AreSame(solo, stoodDown);
        Assert.IsFalse(solo.activeSelf, "Solo hero must vanish, not just idle (the double).");
        Assert.AreEqual(DiabloNetworkPlayer.HeroName, owned.name);
        Assert.AreSame(owned, GameObject.Find(DiabloNetworkPlayer.HeroName),
            "Name lookups (husks, target bar, pickups) must resolve to the owned hero.");
    }

    [Test]
    public void NoSoloHero_StillTakesName()
    {
        owned = new GameObject("NetworkPlayerDiablo(Clone)");
        Assert.IsNull(DiabloNetworkPlayer.TakeOverSoloHero(owned));
        Assert.AreEqual(DiabloNetworkPlayer.HeroName, owned.name);
        Assert.IsTrue(owned.activeSelf);
    }
}
