using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// gear-slots goal in play: worn limb art per class, the six-slot paperdoll,
/// the flask HUD, and the timed and kill-bound unique effects.
/// </summary>
public sealed class GearSlotsPlayTests
{
    readonly List<GameObject> spawned = new List<GameObject>();

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        DiabloJuice.ForceCrit = null;
        foreach (var go in spawned) if (go != null) Object.Destroy(go);
        spawned.Clear();
        yield return null;
    }

    GameObject Track(GameObject go)
    {
        spawned.Add(go);
        return go;
    }

    GameObject PlainHero(string name, Vector3 at)
    {
        var go = Track(new GameObject(name));
        go.transform.position = at;
        go.AddComponent<Equipment>();
        go.AddComponent<Inventory>();
        go.AddComponent<Wallet>();
        go.AddComponent<PlayerHealth>().Refresh();
        return go;
    }

    static bool Active(GameObject root, string objectName) =>
        root.GetComponentsInChildren<Transform>().Any(child => child.name == objectName);

    static int Count(GameObject root, string objectName) =>
        root.GetComponentsInChildren<Transform>().Count(child => child.name == objectName);

    [UnityTest]
    public IEnumerator LimbGear_RendersOnEveryClassAndClearsOnUnequip()
    {
        var source = Resources.Load<HeroArtSource>("CinderreachHeroArt");
        Assert.IsNotNull(source);
        var hero = Track(Object.Instantiate(source.LightClothing));
        var equipment = hero.AddComponent<Equipment>();
        var inventory = hero.AddComponent<Inventory>();
        var appearance = hero.AddComponent<ClassAppearance>();
        appearance.lanternbearerModel = source.LightClothing;
        var alt = Track(Object.Instantiate(source.LightClothing));
        alt.SetActive(false);
        appearance.rogueHoodedModel = alt;
        appearance.rogueModel = alt;
        yield return null;

        foreach (var cls in ClassCatalog.All)
        {
            appearance.Apply(cls);
            yield return null;
            foreach (var slot in new[] { ItemSlot.Gloves, ItemSlot.Boots, ItemSlot.Belt })
            {
                var items = ItemDatabase.GetBaseTypes(slot)
                    .Select(b => new ItemInstance { slot = slot, rarity = ItemRarity.Rare, baseType = b, affixes = new List<Affix>() })
                    .Concat(ItemDatabase.RoleUniquesFor(slot).Select(ItemDatabase.CreateUnique));
                string previous = null;
                foreach (var item in items)
                {
                    equipment.Equip(item);
                    yield return null;
                    string key = HeroGearModels.VisualKey(item);
                    Assert.IsNotNull(key, $"{cls.id} {item.baseType}");
                    int expected = slot == ItemSlot.Belt ? 1 : 2;
                    Assert.AreEqual(expected, Count(hero, key), $"{cls.id}: {key} on {(expected == 2 ? "both sides" : "the hips")}.");
                    if (previous != null && previous != key) Assert.IsFalse(Active(hero, previous), previous);
                    previous = key;
                }
                Assert.IsTrue(equipment.UnequipTo(slot, inventory));
                yield return null;
                yield return null;
                Assert.IsFalse(Active(hero, previous), $"{cls.id}: {previous} leaves with the item.");
                while (inventory.Count > 0) inventory.RemoveAt(0);
            }
        }
        equipment.Equip(ItemDatabase.CreateUnique(ItemDatabase.PilgrimStepsId));
        equipment.Equip(ItemDatabase.CreateUnique(ItemDatabase.CutpurseWrapsId));
        equipment.Equip(ItemDatabase.CreateUnique(ItemDatabase.QuickdrawBeltId));
        yield return null;
        var art = hero.GetComponent<HeroEquipmentAppearance>();
        Assert.IsNotNull(art, "ClassAppearance wires the worn-gear art.");
        Assert.AreEqual(ItemDatabase.PilgrimStepsId, art.BootsKey);
        Assert.AreEqual(ItemDatabase.CutpurseWrapsId, art.GlovesKey);
        Assert.AreEqual(ItemDatabase.QuickdrawBeltId, art.BeltKey);
    }

    [UnityTest]
    public IEnumerator Paperdoll_ShowsSixSlotsAndDropsOnTheNewOnes()
    {
        var hero = PlainHero(DiabloNetworkPlayer.HeroName, new Vector3(200f, 0f, 0f));
        var inventory = hero.GetComponent<Inventory>();
        var equipment = hero.GetComponent<Equipment>();
        var gloves = new ItemInstance { slot = ItemSlot.Gloves, rarity = ItemRarity.Rare, baseType = "LeatherGloves",
            affixes = new List<Affix> { new Affix(AffixLibrary.Crit, 4f) }, instanceId = System.Guid.NewGuid().ToString("N") };
        var belt = ItemDatabase.RollUnique(ItemDatabase.CreateUnique(ItemDatabase.VialSashId), 5, 1);
        inventory.Add(gloves);
        inventory.Add(belt);
        var ui = hero.AddComponent<DiabloInventoryUI>();
        ui.SetOpen(true);
        yield return null;
        foreach (var slot in Equipment.GearSlots)
        {
            var frame = hero.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "EquipFrame" + (int)slot);
            Assert.IsNotNull(frame, slot + " has a paperdoll frame.");
            var label = hero.GetComponentsInChildren<UnityEngine.UI.Text>(true).First(t => t.name == "SlotLabel" + (int)slot);
            StringAssert.Contains(ItemText.Slot(slot), label.text);
        }
        Assert.IsFalse(ui.DropBagOnSlot(0, ItemSlot.Boots), "Gloves never go on the feet.");
        Assert.AreEqual(ItemSlot.Boots, ui.LastRejectedSlot);
        Assert.IsTrue(ui.DropBagOnSlot(0, ItemSlot.Gloves));
        Assert.AreSame(gloves, equipment.gloves);
        ui.RightClickBag(0);
        Assert.AreSame(belt, equipment.belt);
        Assert.AreEqual(2, HealingFlask.For(hero).MaxCharges, "The vial sash adds a flask charge at once.");
        ui.RightClickSlot(ItemSlot.Gloves);
        Assert.IsNull(equipment.gloves);
        Assert.AreEqual(1, inventory.Count);
        ui.SetOpen(false);
    }

    [UnityTest]
    public IEnumerator FlaskHud_ShowsCooldownSweepAfterADrink()
    {
        var hero = PlainHero(DiabloNetworkPlayer.HeroName, new Vector3(200f, 0f, 0f));
        DiabloLifeGlobe.Ensure();
        yield return null;
        var globe = DiabloLifeGlobe.Instance;
        Assert.IsNotNull(globe);
        Assert.AreEqual(0f, globe.TestFlaskSweep, 0.001f, "A full flask shows no sweep.");
        var hp = hero.GetComponent<PlayerHealth>();
        hp.TakeDamage(12f);
        float hurt = hp.CurrentHP;
        Assert.IsTrue(HealingFlask.For(hero).TryDrink());
        yield return new WaitForSeconds(0.7f);
        Assert.Greater(globe.TestFlaskSweep, 0.95f, "Right after the drink the sweep covers the icon.");
        StringAssert.EndsWith("s", globe.TestFlaskText);
        Assert.Greater(hp.CurrentHP, hurt, "The first tick landed.");
    }

    [UnityTest]
    public IEnumerator TimedEffects_BastionFenWrapAndGrips()
    {
        var hero = PlainHero("TimedHero", new Vector3(200f, 0f, 0f));
        var eq = hero.GetComponent<Equipment>();
        var hp = hero.GetComponent<PlayerHealth>();
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.BastionTreadsId));
        var state = GearEffectState.For(hero);
        hero.transform.position += Vector3.right;
        yield return null;
        Assert.AreEqual(10f, GearUniqueEffects.ShapeIncoming(hp, 10f, null), 0.001f, "Moving: no reduction.");
        yield return new WaitForSeconds(GearUniqueEffects.BastionStillSeconds + 0.1f);
        Assert.AreEqual(10f * GearUniqueEffects.BastionDamageMul, GearUniqueEffects.ShapeIncoming(hp, 10f, null), 0.001f);

        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.FenWrapId));
        hp.TakeDamage(10f);
        float hurt = hp.CurrentHP;
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(hurt, hp.CurrentHP, 0.001f, "No regeneration right after a hit.");
        state.LastDamageTime = Time.time - GearUniqueEffects.FenWrapQuietSeconds - 0.1f;
        yield return new WaitForSeconds(0.5f);
        Assert.Greater(hp.CurrentHP, hurt, "Fen wrap regenerates once the hero is left alone.");
    }

    [UnityTest]
    public IEnumerator KillAndCritEffects_ResolveOnTheStriker()
    {
        var hero = PlainHero("StrikerHero", new Vector3(200f, 0f, 0f));
        var eq = hero.GetComponent<Equipment>();
        var hp = hero.GetComponent<PlayerHealth>();
        hp.TakeDamage(15f);

        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.CutpurseWrapsId));
        DiabloJuice.ForceCrit = true;
        float before = hp.CurrentHP;
        DiabloJuice.RollCrit(hero.transform, 4, out bool crit);
        Assert.IsTrue(crit);
        Assert.AreEqual(before + GearUniqueEffects.CutpurseCritHeal, hp.CurrentHP, 0.001f);
        DiabloJuice.ForceCrit = false;

        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.OssuaryThuribleId));
        eq.Equip(ItemDatabase.CreateUnique(ItemDatabase.GhostwalkBootsId));
        var husk = Track(new GameObject("ThuribleTarget"));
        husk.tag = "Husk";
        husk.transform.position = hero.transform.position + Vector3.forward;
        var enemy = husk.AddComponent<EnemyHealth>();
        enemy.SetTier(1, LootTier.Normal);
        var bystander = Track(new GameObject("CinderBystander"));
        bystander.tag = "Husk";
        bystander.transform.position = husk.transform.position + Vector3.right;
        var bystanderHp = bystander.AddComponent<EnemyHealth>();
        bystanderHp.SetTier(50, LootTier.Normal);
        yield return null;

        before = hp.CurrentHP;
        int dmg = DiabloJuice.RollCrit(hero.transform, 5, out _);
        enemy.TakeDamage(dmg, false);
        Assert.IsTrue(enemy.IsDead);
        Assert.AreEqual(before + GearUniqueEffects.ThuribleKillHeal, hp.CurrentHP, 0.001f, "The kill heals the striker.");
        Assert.AreEqual(1.0, bystanderHp.HPFraction(), 0.001, "No cleaver, no burst.");

        Assert.AreEqual(1, GearUniqueEffects.CinderBurst(husk.transform.position));
        Assert.Less(bystanderHp.HPFraction(), 1f, "The cinderfang burst hits enemies close by.");
    }
}
