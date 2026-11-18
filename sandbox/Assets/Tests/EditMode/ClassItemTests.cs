using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>second-class S4: class-bound items, weighted weapon drops, class look on the real rig.</summary>
public class ClassItemTests
{
    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }

    [TearDown]
    public void Teardown() => RunProfile.Reset();

    [Test]
    public void Restriction_WeaponsAreClassBound_ArmorIsShared()
    {
        Assert.AreEqual(ClassCatalog.FenwalkerId, ItemDatabase.ClassRestriction("ShortSword"));
        Assert.AreEqual(ClassCatalog.FenwalkerId, ItemDatabase.ClassRestriction(ItemDatabase.StarterSwordBase));
        Assert.AreEqual(ClassCatalog.LanternbearerId, ItemDatabase.ClassRestriction("Lantern"));
        Assert.AreEqual("", ItemDatabase.ClassRestriction("Breastplate"));
        var lantern = ItemDatabase.CreateUnique("Lantern of the Hold");
        Assert.IsFalse(Equipment.CanEquip(lantern, ClassCatalog.FenwalkerId));
        Assert.IsTrue(Equipment.CanEquip(lantern, ClassCatalog.LanternbearerId));
        var plate = ItemDatabase.CreateUnique("Cryptwarden Plate");
        Assert.IsTrue(Equipment.CanEquip(plate, ClassCatalog.FenwalkerId));
        Assert.IsTrue(Equipment.CanEquip(plate, ClassCatalog.LanternbearerId));
        Assert.AreEqual(3, ItemDatabase.GetUniques().Count, "P2 three-unique contract unchanged");
    }

    [Test]
    public void EquipFromBag_RefusesTheOtherClassWeapon()
    {
        var hero = new GameObject("BagHero");
        var eq = hero.AddComponent<Equipment>();
        var bag = hero.AddComponent<Inventory>();
        bag.Add(ItemDatabase.CreateUnique("Lantern of the Hold"));
        Assert.IsFalse(bag.EquipFromBag(0, eq), "Fenwalker cannot wield a lantern");
        Assert.AreEqual(1, bag.Count);
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        Assert.IsTrue(bag.EquipFromBag(0, eq));
        Assert.AreEqual("Lantern of the Hold", eq.weapon.uniqueId);
        Object.DestroyImmediate(hero);
    }

    [Test]
    public void WeaponDrops_FitTheFinderSeventyPercent()
    {
        foreach (var cls in ClassCatalog.All)
        {
            RunProfile.ClassId = cls.id;
            var rng = new System.Random(21);
            int own = 0;
            const int n = 1000;
            for (int i = 0; i < n; i++)
            {
                var item = LootDrop.BuildItem(new LootRoll { drops = true, rarity = ItemRarity.Magic, slot = ItemSlot.Weapon }, rng);
                if (Equipment.CanEquip(item, cls.id)) own++;
            }
            Assert.That(own / (float)n, Is.InRange(0.65f, 0.75f), cls.id);
        }
    }

    [Test]
    public void ShouldShow_SkipsAutomationAndExplicitClass()
    {
        Assert.IsTrue(ClassSelectUI.ShouldShow(new[] { "cinderreach" }));
        Assert.IsFalse(ClassSelectUI.ShouldShow(new[] { "cinderreach", "-autoplay" }));
        Assert.IsFalse(ClassSelectUI.ShouldShow(new[] { "cinderreach", "-shotcheck" }));
        Assert.IsFalse(ClassSelectUI.ShouldShow(new[] { "cinderreach", "-class", "lanternbearer" }));
        Assert.IsFalse(ClassSelectUI.ShouldShow(new[] { "cinderreach", "--net-client" }));
    }

    [Test]
    public void Appearance_ReskinsTheMageOntoTheKnightRig()
    {
        var knight = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/KayKit/" + ClassCatalog.Fenwalker.modelPath);
        var mage = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/KayKit/" + ClassCatalog.Lanternbearer.modelPath);
        Assert.IsNotNull(knight);
        Assert.IsNotNull(mage);
        var hero = (GameObject)Object.Instantiate(knight);
        var eq = hero.AddComponent<Equipment>();
        eq.Equip(ItemDatabase.CreateStarterSword());
        var look = hero.AddComponent<ClassAppearance>();
        look.lanternbearerModel = mage;
        var knightBody = hero.GetComponentsInChildren<SkinnedMeshRenderer>();
        Assert.IsNotEmpty(knightBody);

        look.Apply(ClassCatalog.Lanternbearer);
        Assert.AreEqual(ClassCatalog.LanternbearerId, look.AppliedClassId);
        foreach (var r in knightBody) Assert.IsFalse(r.enabled, $"knight mesh {r.name} hidden");
        var skin = hero.transform.Find(ClassAppearance.SkinRootName);
        Assert.IsNotNull(skin);
        var mageMeshes = skin.GetComponentsInChildren<SkinnedMeshRenderer>();
        Assert.IsNotEmpty(mageMeshes);
        foreach (var smr in mageMeshes)
            foreach (var b in smr.bones)
                Assert.IsTrue(b == null || b.IsChildOf(hero.transform), $"{smr.name} bone {b?.name} bound to the hero rig");
        Assert.IsNotNull(FindDeep(hero.transform, ClassAppearance.LanternName));
        Assert.AreEqual(ItemDatabase.StarterLanternBase, eq.weapon.baseType, "starter weapon follows the class");

        look.Apply(ClassCatalog.Fenwalker);
        foreach (var r in knightBody) Assert.IsTrue(r.enabled);
        Assert.IsFalse(skin.gameObject.activeSelf);
        Assert.AreEqual(ItemDatabase.StarterSwordBase, eq.weapon.baseType);
        Object.DestroyImmediate(hero);
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var hit = FindDeep(t.GetChild(i), name);
            if (hit != null) return hit;
        }
        return null;
    }
}
