using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Blood Moor end-to-end (headless): kill, loot to bag, equip, heal.</summary>
public class DiabloBloodMoorLoopTests
{
    GameObject player;
    Equipment equipment;
    Inventory inventory;
    PlayerHealth health;
    DiabloInventoryUI ui;
    DiabloClickController controller;
    DiabloMelee melee;

    [SetUp]
    public void Setup()
    {
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();

        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.AddComponent<CharacterController>();
        controller = player.AddComponent<DiabloClickController>();
        melee = player.AddComponent<DiabloMelee>();
        melee.cooldown = 0f;
        equipment = player.AddComponent<Equipment>();
        inventory = player.AddComponent<Inventory>();
        health = player.AddComponent<PlayerHealth>();
        ui = player.AddComponent<DiabloInventoryUI>();
        equipment.Equip(ItemDatabase.CreateStarterSword());
    }

    [TearDown]
    public void TearDown()
    {
        // Every dropped orb, with or without a LootDrop (no-drop rolls leave
        // legacy ToyTrigger orbs that would leak into other suites).
        foreach (var orb in GameObject.FindObjectsOfType<ToyTrigger>())
            if (orb.gameObject.name.StartsWith("DiabloOrb_"))
                Object.DestroyImmediate(orb.gameObject);
        foreach (var loot in GameObject.FindObjectsOfType<LootDrop>())
            if (loot != null) Object.DestroyImmediate(loot.gameObject);
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
            Object.DestroyImmediate(husk);
        foreach (var left in GameObject.FindObjectsOfType<CampfireHeal>())
            Object.DestroyImmediate(left.gameObject);
        var eventSys = GameObject.Find("EventSystem");
        if (eventSys != null) Object.DestroyImmediate(eventSys);
        if (player != null) Object.DestroyImmediate(player);
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
    }

    static GameObject SpawnHusk(Vector3 pos)
    {
        var husk = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        husk.name = "TestHusk";
        husk.tag = "Husk";
        husk.transform.position = pos;
        // Elite tier: 100 % drop (Normal rolls 70 % and would flake the test).
        husk.AddComponent<EnemyHealth>().SetTier(3, LootTier.Elite);
        return husk;
    }

    /// <summary>
    /// Teleport the hero so physics (and triggers) actually sees it: an
    /// enabled CharacterController does not rebase its internal position on
    /// a raw transform set (plus this project disables autoSyncTransforms).
    /// </summary>
    static void TeleportHero(GameObject hero, Vector3 pos)
    {
        var cc = hero.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        hero.transform.position = pos;
        if (cc != null) cc.enabled = true;
        Physics.SyncTransforms();
    }

    [UnityTest]
    public IEnumerator Kill_LootLandsInBag_EquipFromPanel()
    {
        Assert.AreEqual(3f, equipment.GetDamage(), "Hero must start with the starter sword (+2).");

        var husk = SpawnHusk(new Vector3(0f, 1f, 1.5f));
        controller.SetAimPoint(new Vector3(0f, 0f, 10f));
        Assert.IsTrue(controller.Strike(), "Strike into the arc must hit.");
        yield return null;
        Assert.IsTrue(husk == null, "Husk must die from one starter strike (3 dmg vs 3 HP).");

        // Walk the hero onto the dropped loot until the trigger fires.
        var drop = GameObject.FindFirstObjectByType<LootDrop>();
        Assert.IsNotNull(drop, "Kill must drop loot.");
        var looted = drop.item;
        TeleportHero(player, drop.transform.position);
        int bagBefore = inventory.Count;
        float pickupWait = 0f;
        while (inventory.Count == bagBefore && pickupWait < 2f)
        {
            yield return null;
            pickupWait += Time.deltaTime;
        }
        Assert.Greater(inventory.Count, bagBefore, "Pickup must land in the bag, not on the body.");
        Assert.IsTrue(equipment.weapon.baseType == "Worn Short Sword",
            "Body must keep the starter while loot goes to the bag.");

        ui.EquipIndex(inventory.Count - 1);
        Assert.AreSame(looted, equipment.GetEquipped(looted.slot),
            "Panel equip must wield the looted item in its own slot.");
        if (looted.slot == ItemSlot.Weapon)
            Assert.AreEqual("Worn Short Sword", inventory.Get(inventory.Count - 1).baseType,
                "Starter sword must swap back into the bag.");
    }

    [UnityTest]
    public IEnumerator PanelButton_SubmitsThroughEventSystem()
    {
        var eventSysGo = new GameObject("EventSystem");
        eventSysGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSysGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        var axe = new ItemInstance
        {
            slot = ItemSlot.Weapon, rarity = ItemRarity.Rare, baseType = "HeavyAxe",
            affixes = new System.Collections.Generic.List<Affix> { new Affix("Damage", 5f) },
        };
        LogAssert.Expect(LogType.Log, "[Loot] Bagged Rare Weapon (HeavyAxe). (1/20).");
        inventory.Add(axe);
        ui.SetOpen(true);
        var button = GameObject.FindFirstObjectByType<UnityEngine.UI.Button>();
        Assert.IsNotNull(button, "Open panel must build a bag button.");
        LogAssert.Expect(LogType.Log, "[Loot] Equipped Rare Weapon: HeavyAxe (dmg=6, hp=3).");
        LogAssert.Expect(LogType.Log, "[Loot] Equipped Rare Weapon from bag (swapped).");
        UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,
            new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current),
            UnityEngine.EventSystems.ExecuteEvents.submitHandler);
        Assert.AreSame(axe, equipment.weapon, "Submit must wield the bag weapon.");
        LogAssert.NoUnexpectedReceived();
        yield return null;
    }

    [UnityTest]
    public IEnumerator Campfire_HealsHeroStandingInRadius()
    {
        var fire = new GameObject("CampfireHeal");
        fire.transform.position = new Vector3(10f, 1f, 10f);
        var col = fire.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(CampfireHeal.Radius * 2f, 3f, CampfireHeal.Radius * 2f);
        fire.AddComponent<CampfireHeal>();

        health.TakeDamage(2f);
        Assert.AreEqual(1f, health.CurrentHP);
        TeleportHero(player, new Vector3(10f, 1.05f, 10f));
        float waited = 0f;
        while (waited < 1.5f)
        {
            yield return null;
            waited += Time.deltaTime;
        }
        Assert.Greater(health.CurrentHP, 1f, "Standing in the campfire must heal over time.");
    }
}
