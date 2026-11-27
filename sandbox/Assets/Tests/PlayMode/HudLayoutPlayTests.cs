using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// cinderreach-hud-redesign in play: every piece of the canvas "HUD im Spiel"
/// exists, sits in its screen region and keeps its minimum size — globe and
/// flask/gold column at the bottom, skill plate with names and XP bar in the
/// middle, target bar with tier plaque on top, round minimap and the log panel
/// top right, party frame top left.
/// </summary>
public sealed class HudLayoutPlayTests
{
    readonly List<GameObject> spawned = new List<GameObject>();

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        MapScene.ResetCache();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        DiabloMelee.LastStruck = null;
        foreach (var go in spawned) if (go != null) Object.Destroy(go);
        spawned.Clear();
        if (DiabloLifeGlobe.Instance != null) Object.Destroy(DiabloLifeGlobe.Instance.gameObject);
        RunProfile.Reset();
        yield return null;
    }

    GameObject MakeHero(Vector3 at)
    {
        var hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = DiabloNetworkPlayer.HeroName;
        hero.transform.position = at;
        hero.AddComponent<DiabloProgression>();
        hero.AddComponent<Equipment>();
        hero.AddComponent<Inventory>();
        hero.AddComponent<Wallet>();
        hero.AddComponent<PlayerHealth>();
        hero.AddComponent<DiabloSkills>();
        hero.AddComponent<StoryDirector>();
        spawned.Add(hero);
        return hero;
    }

    static RectTransform Find(GameObject root, string name) =>
        root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == name);

    /// <summary>Screen-space centre of a rect (overlay canvases: world corners are pixels).</summary>
    static Vector2 Center01(RectTransform r)
    {
        var c = new Vector3[4];
        r.GetWorldCorners(c);
        var mid = (c[0] + c[2]) * 0.5f;
        return new Vector2(mid.x / Screen.width, mid.y / Screen.height);
    }

    [UnityTest]
    public IEnumerator Hud_PiecesExistInTheirRegionsWithMinimumSizes()
    {
        var hero = MakeHero(new Vector3(0f, 1f, 8f));
        hero.GetComponent<PlayerHealth>().GrantShield(15f, 4f);
        DiabloLifeGlobe.Ensure();
        var globeGo = DiabloLifeGlobe.Instance.gameObject;

        var enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemy.name = "Ash Elite_0";
        enemy.transform.position = hero.transform.position + new Vector3(3f, 0f, 0f);
        enemy.AddComponent<EnemyHealth>().tier = LootTier.Elite;
        spawned.Add(enemy);
        var barGo = new GameObject("DTargetBar");
        var bar = barGo.AddComponent<DiabloTargetBar>();
        spawned.Add(barGo);
        DiabloMelee.LastStruck = enemy;
        DiabloMelee.LastStruckTime = Time.time;

        yield return DiabloTestFixture.WaitSeconds(0.4f);
        // The skill bar builds from DiabloSkills.Update, which needs a click controller; drive it directly.
        typeof(DiabloSkills).GetMethod("UpdateHud", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(hero.GetComponent<DiabloSkills>(), null);
        DiabloMelee.LastStruckTime = Time.time;
        yield return null;
        Canvas.ForceUpdateCanvases();

        // Bottom block.
        var globe = Find(globeGo, "GlobeGoldRim");
        Assert.IsNotNull(globe, "life globe");
        Assert.GreaterOrEqual(globe.rect.width, HudLayout.GlobeSize - 0.5f);
        Assert.Less(Center01(globe).y, 0.3f, "globe sits at the bottom");
        Assert.Less(Center01(globe).x, 0.5f, "globe left of centre");
        Assert.IsTrue(DiabloLifeGlobe.Instance.TestShieldChipShown, "shield chip while a shield is live");

        var flask = Find(globeGo, "FlaskFrame");
        Assert.IsNotNull(flask);
        Assert.GreaterOrEqual(flask.rect.width, HudLayout.SlotSize - 0.5f);
        Assert.Greater(Center01(flask).x, 0.5f, "flask is the last cell of the row");

        var keys = globeGo.GetComponentsInChildren<Button>(true).Where(b => b.name.StartsWith("MenuKey_")).ToList();
        Assert.AreEqual(4, keys.Count, "I, C, M, Esc");
        foreach (var k in keys)
        {
            var r = (RectTransform)k.transform;
            Assert.GreaterOrEqual(r.rect.height, 44f, k.name + " is touch-size");
            Assert.IsTrue(k.targetGraphic.raycastTarget, k.name + " is clickable");
        }
        Assert.IsNotNull(Find(globeGo, "HudGoldCard"));
        Assert.AreEqual("0", DiabloLifeGlobe.Instance.TestGoldText);

        var plate = Find(hero, "HudPlate");
        Assert.IsNotNull(plate, "skill plate");
        Assert.IsNotNull(plate.Find("GoldTop"), "ember-gold top line");
        var slots = hero.GetComponentsInChildren<RectTransform>(true).Where(r => r.name.StartsWith("SkillSlot_")).ToList();
        Assert.AreEqual(HudLayout.SkillSlots, slots.Count);
        foreach (var s in slots)
        {
            Assert.GreaterOrEqual(s.rect.width, HudLayout.SlotSize - 0.5f);
            Assert.IsNotNull(s.Find("SkillName"), s.name + " names its skill");
            Assert.AreEqual(Image.Type.Filled, s.Find("CooldownVeil").GetComponent<Image>().type, "radial sweep");
        }
        Assert.IsNotNull(Find(hero, "XpBack"), "XP bar");
        Assert.IsNotNull(Find(hero, "XpAmountLabel"));

        // Top block.
        Assert.IsTrue(bar.TestTierBadgeShown, "elite plaque next to the name");
        Assert.Greater(Center01(Find(barGo, "Panel")).y, 0.8f, "target bar on top");
        var mini = Find(hero, "MinimapViewport");
        Assert.IsNotNull(mini.GetComponent<Mask>(), "round minimap");
        Assert.Greater(Center01(mini).x, 0.75f);
        Assert.Greater(Center01(mini).y, 0.7f);
        Assert.IsNotNull(Find(hero, "StoryTrackerPanel"), "log panel under the minimap");

        var own = Find(globeGo, "PartyFrameOwn");
        Assert.IsNotNull(own, "own party frame");
        Assert.IsTrue(own.gameObject.activeSelf);
        Assert.Less(Center01(own).x, 0.25f);
        Assert.Greater(Center01(own).y, 0.8f);
    }
}
