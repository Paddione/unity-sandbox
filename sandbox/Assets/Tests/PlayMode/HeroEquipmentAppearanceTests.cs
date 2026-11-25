using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class HeroEquipmentAppearanceTests
{
    GameObject hero;
    GameObject altModel;

    [UnityTest]
    public IEnumerator CatalogModelsReplaceEachOtherAndRestoreStarterArmor()
    {
        var source = Resources.Load<HeroArtSource>("CinderreachHeroArt");
        Assert.IsNotNull(source);
        hero = Object.Instantiate(source.LightClothing);
        var equipment = hero.AddComponent<Equipment>();
        var inventory = hero.AddComponent<Inventory>();
        var appearance = hero.AddComponent<ClassAppearance>();
        appearance.lanternbearerModel = source.LightClothing; // any rig-compatible model stands in for the Mage
        // A second, distinct asset for the (female) Rogue: Mage -> Rogue is a direct skin-to-skin switch.
        altModel = Object.Instantiate(source.LightClothing);
        altModel.SetActive(false);
        appearance.rogueHoodedModel = altModel;
        appearance.rogueModel = altModel; // the default profile is male: his Rogue body path needs the stand-in too
        yield return null;

        foreach (var cls in ClassCatalog.All)
        {
            appearance.Apply(cls);
            yield return null;
            Assert.AreEqual(cls.id, appearance.AppliedClassId);
            // Only the tank wears the brass starter guard; no shared outfit hides the class body.
            Assert.AreEqual(cls.id == ClassCatalog.FenwalkerId, Active("Yellow starter armor"), cls.id);
            Assert.IsFalse(Active("Light clothing"), cls.id);
            // Gear never hides the class body: the Lanternbearer's mapped skin stands with gear active.
            bool skinned = cls.id != ClassCatalog.FenwalkerId;
            Assert.AreEqual(skinned, Active(ClassAppearance.SkinRootName), cls.id);
            if (skinned)
                Assert.Greater(hero.transform.Find(ClassAppearance.SkinRootName).childCount, 0,
                    $"{cls.id}: the new skin maps in the same Apply although the old one is still being destroyed");
        }
        appearance.Apply(ClassCatalog.Fenwalker);
        yield return null;

        foreach (var slot in new[] { ItemSlot.Armor, ItemSlot.Helmet, ItemSlot.Weapon })
        {
            string previous = null;
            var candidates = ItemDatabase.UniqueIds.Select(ItemDatabase.CreateUnique).Where(item => item.slot == slot);
            foreach (var item in candidates)
            {
                equipment.Equip(item);
                yield return null;
                Assert.IsTrue(Active(item.uniqueId), item.uniqueId);
                if (previous != null) Assert.IsFalse(Active(previous), previous);
                if (slot == ItemSlot.Armor) Assert.IsFalse(Active("Yellow starter armor"));
                previous = item.uniqueId;
            }
            Assert.IsTrue(equipment.UnequipTo(slot, inventory));
            yield return null;
            Assert.IsFalse(Active(previous));
            Assert.IsTrue(Active("Yellow starter armor"));
        }
    }

    bool Active(string objectName) => hero.GetComponentsInChildren<Transform>()
        .Any(child => child.name == objectName);

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (hero != null) Object.Destroy(hero);
        if (altModel != null) Object.Destroy(altModel);
        yield return null;
    }
}
