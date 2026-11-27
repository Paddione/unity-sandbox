using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Ashen Fen data contracts: starter sword, bag, swap, pickup routing.</summary>
public class DiabloAshenFenTests
{
    const string ZonePath = "Assets/Scenes/DiabloZone.unity";

    [Test]
    public void StarterSword_HasFixedStats()
    {
        var sword = ItemDatabase.CreateStarterSword();
        Assert.AreEqual(ItemSlot.Weapon, sword.slot);
        Assert.AreEqual(ItemRarity.Rare, sword.rarity, "No blue items (playtest 2026-11-23).");
        Assert.AreEqual("Worn Short Sword", sword.baseType);
        Assert.AreEqual(2f, sword.GetAffix("Damage"));
    }

    [Test]
    public void StarterSword_RaisesHeroDamage()
    {
        var go = new GameObject("TestGear");
        var eq = go.AddComponent<Equipment>();
        Assert.AreEqual(1f, eq.GetDamage());
        eq.Equip(ItemDatabase.CreateStarterSword());
        Assert.AreEqual(3f, eq.GetDamage(), "Starter sword (+2 Damage) must raise damage to 3.");
        Object.DestroyImmediate(go);
    }

    static GameObject HeroWithBag()
    {
        var go = new GameObject("Player");
        go.AddComponent<Equipment>();
        go.AddComponent<Inventory>();
        return go;
    }

    [Test]
    public void Inventory_CapsAtTwenty()
    {
        var go = HeroWithBag();
        var inv = go.GetComponent<Inventory>();
        for (int i = 0; i < Inventory.Capacity; i++)
            Assert.IsTrue(inv.Add(ItemDatabase.CreateStarterSword()), $"Slot {i} must accept loot.");
        Assert.AreEqual(0, inv.FreeSlots);
        Assert.IsFalse(inv.Add(ItemDatabase.CreateStarterSword()), "Full bag must refuse loot.");
        Assert.IsNull(inv.Get(-1));
        Assert.IsNull(inv.Get(Inventory.Capacity));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void EquipFromBag_SwapsBackToBag()
    {
        var go = HeroWithBag();
        var inv = go.GetComponent<Inventory>();
        var eq = go.GetComponent<Equipment>();
        eq.Equip(ItemDatabase.CreateStarterSword());
        var loot = new ItemInstance
        {
            slot = ItemSlot.Weapon, rarity = ItemRarity.Rare, baseType = "HeavyAxe",
            affixes = new System.Collections.Generic.List<Affix> { new Affix("Damage", 5f) },
        };
        Assert.IsTrue(inv.Add(loot));
        Assert.IsTrue(inv.EquipFromBag(0, eq), "Equip from bag must succeed.");
        Assert.AreEqual("HeavyAxe", eq.weapon.baseType, "Axe must be equipped.");
        Assert.AreEqual(1, inv.Count, "Starter sword must swap back into the bag.");
        Assert.AreEqual("Worn Short Sword", inv.Get(0).baseType);
        Assert.IsFalse(inv.EquipFromBag(7, eq), "Out-of-range index must fail.");
        Assert.IsFalse(inv.EquipFromBag(0, null), "Null equipment must fail.");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void UnequipTo_MovesSlotIntoBag()
    {
        var go = HeroWithBag();
        var inv = go.GetComponent<Inventory>();
        var eq = go.GetComponent<Equipment>();
        Assert.IsFalse(eq.UnequipTo(ItemSlot.Weapon, inv), "Empty slot must fail.");
        eq.Equip(ItemDatabase.CreateStarterSword());
        Assert.IsTrue(eq.UnequipTo(ItemSlot.Weapon, inv), "Equipped sword must move to the bag.");
        Assert.IsNull(eq.weapon, "Slot must be empty after unequip.");
        Assert.AreEqual(1, inv.Count);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void MeleeDamage_UsesEquippedWeapon()
    {
        var go = HeroWithBag();
        var melee = go.AddComponent<DiabloMelee>();
        Assert.AreEqual(1, melee.CurrentDamage(go.transform), "Bare hero swings flat damage.");
        go.GetComponent<Equipment>().Equip(ItemDatabase.CreateStarterSword());
        Assert.AreEqual(3, melee.CurrentDamage(go.transform),
            "Starter sword (+2) must raise swing damage to 3.");
        Assert.AreEqual(1, melee.CurrentDamage(null), "Null attacker falls back to flat damage.");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void CollectInto_UsesBagWithEquipOverflow()
    {
        var go = HeroWithBag();
        var inv = go.GetComponent<Inventory>();
        var eq = go.GetComponent<Equipment>();
        var loot = ItemDatabase.CreateStarterSword();
        LootDrop.CollectInto(go, loot);
        Assert.AreEqual(1, inv.Count, "Pickup must land in the bag, not on the body.");
        Assert.IsNull(eq.weapon, "Body must stay untouched while the bag has room.");
        for (int i = inv.Count; i < Inventory.Capacity; i++) inv.Add(ItemDatabase.CreateStarterSword());
        var overflow = new ItemInstance
        {
            slot = ItemSlot.Armor, rarity = ItemRarity.Magic, baseType = "ClothTunic",
            affixes = new System.Collections.Generic.List<Affix> { new Affix("MaxHP", 5f) },
        };
        LootDrop.CollectInto(go, overflow);
        Assert.IsNotNull(eq.armor, "Full bag must overflow onto the body (no loot lost).");
        LootDrop.CollectInto(null, loot); // must not throw
        Object.DestroyImmediate(go);
    }

    [Test]
    public void CollectInto_LegacyHeroEquipsDirectly()
    {
        var go = new GameObject("Player");
        var eq = go.AddComponent<Equipment>(); // no Inventory: Q5 behavior
        LootDrop.CollectInto(go, ItemDatabase.CreateStarterSword());
        Assert.IsNotNull(eq.weapon, "Hero without a bag keeps auto-equip.");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void ClicksBlocked_OnlyWithOpenPanel()
    {
        Assert.IsFalse(DiabloClickController.ShouldBlockClicks(null), "No panel must not block.");
        var go = HeroWithBag();
        var ui = go.AddComponent<DiabloInventoryUI>();
        Assert.IsFalse(DiabloClickController.ShouldBlockClicks(ui), "Closed panel must not block.");
        ui.SetOpen(true);
        Assert.IsTrue(DiabloClickController.ShouldBlockClicks(ui), "Open panel must eat clicks.");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void PanelRefresh_BuildsBagButtons()
    {
        var go = HeroWithBag();
        go.GetComponent<Inventory>().Add(ItemDatabase.CreateStarterSword());
        var ui = go.AddComponent<DiabloInventoryUI>();
        ui.SetOpen(true); // exercises MakeButton headless (PlayMode NRE repro)
        Assert.IsTrue(ui.IsOpen);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void LootWeights_NormalTierIsRarePlus()
    {
        LootTable.WeightsFor(LootTier.Normal, out int magic, out int rare, out int unique);
        Assert.AreEqual(0, magic, "No blue drops (playtest 2026-11-23).");
        Assert.AreEqual(98, rare);
        Assert.AreEqual(2, unique);
        Assert.AreEqual(70, LootTable.DropChanceFor(LootTier.Normal));
    }

    [Test]
    public void Campfire_HealsOnePerSecond()
    {
        Assert.AreEqual(4f, CampfireHeal.Radius);
        Assert.AreEqual(5f, CampfireHeal.HealPerSecond);
        var go = new GameObject("Player");
        go.AddComponent<Equipment>();
        var hp = go.AddComponent<PlayerHealth>();
        hp.TakeDamage(10f);
        Assert.AreEqual(hp.MaxHP - 10f, hp.CurrentHP);
        hp.Heal(CampfireHeal.HealPerSecond * 2f);
        Assert.AreEqual(hp.MaxHP, hp.CurrentHP, "Two campfire-seconds must heal 10 HP.");
        hp.Heal(99f);
        Assert.AreEqual(hp.MaxHP, hp.CurrentHP, "Heal must clamp at max.");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void AshenFenScene_HasCampHusksBruteAndStarter()
    {
        var scene = EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        Assert.IsTrue(scene.IsValid(), "DiabloZone.unity must open cleanly.");

        var husks = GameObject.FindGameObjectsWithTag("Husk");
        Assert.GreaterOrEqual(husks.Length, 10, "Ashen Fen must spread 10+ husks.");
        var brute = GameObject.Find("Fen Brute");
        Assert.IsNotNull(brute, "Ashen Fen needs the Fen Brute elite.");
        Assert.AreEqual(12, brute.GetComponent<EnemyHealth>().maxHP);

        int logs = 0;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (!go.name.StartsWith("Palisade_")) continue;
            logs++;
            // Position tolerance mirrors the builder (ring center 0,11 radius 6,
            // north gate): blind placement drifts show up here, not just in counts.
            var p = go.transform.position;
            float ringDist = Mathf.Sqrt(p.x * p.x + (p.z - 11f) * (p.z - 11f));
            Assert.GreaterOrEqual(ringDist, 5.5f, $"{go.name} must sit on the ring (r>=5.5).");
            Assert.LessOrEqual(ringDist, 6.5f, $"{go.name} must sit on the ring (r<=6.5).");
            Assert.GreaterOrEqual(p.y, 0.75f, $"{go.name} must stand upright (y>=0.75).");
            Assert.LessOrEqual(p.y, 1.75f, $"{go.name} must stand upright (y<=1.75).");
        }
        Assert.GreaterOrEqual(logs, 12, "Encampment needs a 12+ log palisade ring.");

        var heal = GameObject.Find("CampfireHeal");
        Assert.IsNotNull(heal, "Encampment needs the campfire heal trigger.");
        Assert.IsNotNull(heal.GetComponent<CampfireHeal>());

        var eventSys = GameObject.Find("EventSystem");
        Assert.IsNotNull(eventSys, "Zone needs an EventSystem for the inventory panel.");
        Assert.IsNotNull(eventSys.GetComponent<UnityEngine.EventSystems.EventSystem>());
        Assert.IsNotNull(eventSys.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(),
            "EventSystem must use the Input System module (Standalone reads the legacy API and throws).");
        Assert.IsNull(eventSys.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>(),
            "StandaloneInputModule must be gone (legacy-Input exceptions + server log spam).");

        var player = GameObject.Find("Player");
        Assert.IsNotNull(player.GetComponent<Inventory>(), "Zone hero needs a bag.");
        Assert.IsNotNull(player.GetComponent<PlayerHealth>(), "Zone hero needs HP for the campfire.");
        Assert.IsNotNull(player.GetComponent<DiabloInventoryUI>(), "Zone hero needs the inventory UI.");
        var eq = player.GetComponent<Equipment>();
        Assert.IsNotNull(eq.weapon, "Zone hero must spawn with the starter sword.");
        Assert.AreEqual(3f, eq.GetDamage(), "Starter sword (+2) must raise spawn damage to 3.");

        Transform sword = player.transform.Find("StarterSword");
        if (sword == null)
            foreach (var tr in player.GetComponentsInChildren<Transform>(true))
                if (tr.gameObject.name == "StarterSword") sword = tr;
        Assert.IsNotNull(sword, "Starter sword must be visible for the hero.");
        Assert.That(sword.parent.name.ToLowerInvariant(), Does.Contain("hand"),
            $"Sword must sit in the hand, not at '{sword.parent.name}' (hip fallback is silent and wrong).");

        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/AddressableAssets/NetworkPlayerDiablo.prefab");
        Assert.IsNotNull(prefab.GetComponent<Inventory>(), "Net prefab needs a bag.");
        Assert.IsNotNull(prefab.GetComponent<PlayerHealth>(), "Net prefab needs HP.");
        Assert.IsNotNull(prefab.GetComponent<DiabloInventoryUI>(), "Net prefab needs the UI.");
        Assert.AreEqual(3f, prefab.GetComponent<Equipment>().GetDamage(),
            "Net prefab must carry the starter sword damage.");
        Transform prefabSword = null;
        foreach (var tr in prefab.GetComponentsInChildren<Transform>(true))
            if (tr.gameObject.name == "StarterSword") prefabSword = tr;
        Assert.IsNotNull(prefabSword, "Net prefab needs the visible sword prop.");
        Assert.That(prefabSword.parent.name.ToLowerInvariant(), Does.Contain("hand"),
            "Prefab sword must sit in the hand, not at the hip.");
    }
}
