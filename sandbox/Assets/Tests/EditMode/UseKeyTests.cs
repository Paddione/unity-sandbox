using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Use key (T): picks the nearest usable in reach that has a label, skips
/// item orbs and far objects, and every prompt text exists in the catalog.
/// EditMode AddComponent does not run OnEnable, so the tests register by hand.
/// </summary>
public class UseKeyTests
{
    GameObject root;

    [SetUp]
    public void SetUp() => root = new GameObject("UseKeyTestRoot");

    [TearDown]
    public void TearDown()
    {
        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true)) Usables.Unregister(mb);
        Object.DestroyImmediate(root);
    }

    T Place<T>(string name, Vector3 pos) where T : MonoBehaviour
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        go.transform.position = pos;
        var c = go.AddComponent<T>();
        Usables.Register(c);
        return c;
    }

    [Test]
    public void Nearest_PicksClosestInReach()
    {
        var stash = Place<StashChest>("Stash", new Vector3(2f, 0f, 0f));
        var board = Place<ContractBoard>("Board", new Vector3(0f, 0f, 1f));
        Assert.AreSame(board, Usables.Nearest(Vector3.zero, out var owner));
        Assert.AreSame(board, owner);
        Assert.AreSame(stash, Usables.Nearest(new Vector3(3f, 0f, -1f), out _));
        Assert.IsNull(Usables.Nearest(new Vector3(20f, 0f, 20f), out _));
    }

    [Test]
    public void Nearest_SkipsItemOrbsButOffersChests()
    {
        var orb = Place<LootDrop>("Orb", new Vector3(0.5f, 0f, 0f));
        Assert.IsNull(Usables.Nearest(Vector3.zero, out _), "item orbs are walk-over, no use prompt");
        orb.isChest = true;
        Assert.AreSame(orb, Usables.Nearest(Vector3.zero, out _));
        orb.collected = true;
        Assert.IsNull(Usables.Nearest(Vector3.zero, out _));
    }

    [Test]
    public void PromptTexts_ExistInCatalog()
    {
        foreach (var key in new[] { "use.prompt", "use.trade", "use.smith", "use.stash", "use.waypoint",
                     "use.risk", "use.contract", "use.chest", "story.talk" })
            Assert.IsTrue(Loc.Has(key), key);
        Assert.AreEqual("[T] Kiste öffnen", Loc.F("use.prompt", UseKey.Binding.ToString(), Loc.T("use.chest")));
    }
}
