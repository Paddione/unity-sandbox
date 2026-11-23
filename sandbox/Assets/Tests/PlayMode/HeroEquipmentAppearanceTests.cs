using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class HeroEquipmentAppearanceTests
{
    GameObject hero;

    [UnityTest]
    public IEnumerator CatalogModelsReplaceEachOtherAndRestoreStarterArmor()
    {
        var source = Resources.Load<HeroArtSource>("CinderreachHeroArt");
        Assert.IsNotNull(source);
        hero = Object.Instantiate(source.LightClothing);
        var equipment = hero.AddComponent<Equipment>();
        var inventory = hero.AddComponent<Inventory>();
        var appearance = hero.AddComponent<ClassAppearance>();
        yield return null;

        foreach (var cls in ClassCatalog.All)
        {
            appearance.Apply(cls);
            yield return null;
            Assert.AreEqual(cls.id, appearance.AppliedClassId);
            Assert.IsTrue(Active("Yellow starter armor"));
            Assert.IsTrue(Active("Light clothing"));
        }

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
        yield return null;
    }
}
