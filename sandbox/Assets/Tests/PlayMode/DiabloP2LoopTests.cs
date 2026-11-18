using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>P2 loot loop logic (deterministic, no network needed).</summary>
public class DiabloP2LoopTests
{
    GameObject player;
    DiabloClickController controller;
    DiabloMelee melee;
    Equipment equipment;

    [SetUp]
    public void Setup()
    {
        DiabloJuice.ForceCrit = false; // exact-damage asserts: no random crits
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
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var loot in GameObject.FindObjectsOfType<LootDrop>())
            Object.DestroyImmediate(loot.gameObject);
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
            Object.DestroyImmediate(husk);
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
        husk.AddComponent<EnemyHealth>();
        return husk;
    }

    [UnityTest]
    public System.Collections.IEnumerator Kill_DropsExactlyOneLootOrbPerHusk()
    {
        const int kills = 5;
        for (int i = 0; i < kills; i++)
            SpawnHusk(new Vector3(i * 2f, 1f, 0f)).GetComponent<EnemyHealth>().TakeDamage(3);
        yield return null; // Destroy applies at frame end
        Assert.AreEqual(0, GameObject.FindGameObjectsWithTag("Husk").Length,
            "All 5 husks must be gone after 3 damage each.");
        Assert.AreEqual(kills, EnemyHealth.HusksKilled);
        Assert.AreEqual(kills, EnemyHealth.OrbsDropped);
        var orbs = GameObject.FindObjectsOfType<ToyTrigger>();
        int lootCount = 0;
        foreach (var o in orbs)
            if (o.gameObject.name.StartsWith("DiabloOrb_")) lootCount++;
        Assert.AreEqual(kills, lootCount, "Each kill must drop exactly one loot orb.");
    }

    [Test]
    public void SeededSpawn_BuildsItemWithMatchingSlot()
    {
        var rng = new System.Random(1234);
        GameObject spawned = null;
        for (int i = 0; i < 50 && spawned == null; i++)
            spawned = LootDrop.SpawnLoot(Vector3.zero, LootTier.Normal, rng, false);
        Assert.IsNotNull(spawned, "Seeded rolls must drop loot within 50 tries.");
        var drop = spawned.GetComponent<LootDrop>();
        Assert.IsNotNull(drop);
        Assert.IsNotNull(drop.item);
        var pool = AffixLibrary.PoolFor(drop.item.slot);
        foreach (var affix in drop.item.affixes)
            Assert.Contains(affix.key, pool, $"Affix {affix.key} must fit slot {drop.item.slot}.");
        var label = LootDrop.ItemLabel(drop.item);
        Assert.IsFalse(string.IsNullOrEmpty(label), "Loot must have a display label.");
        if (drop.item.rarity == ItemRarity.Unique)
            Assert.IsFalse(string.IsNullOrEmpty(drop.item.uniqueId), "Unique needs its fixed id.");
        else
            Assert.GreaterOrEqual(drop.item.affixes.Count, 1, "Magic/Rare needs affixes.");
    }

    [Test]
    public void LootRoll_DistributionMatchesWeights()
    {
        var rng = new System.Random(42);
        int magic = 0, rare = 0, unique = 0, drops = 0;
        const int rolls = 2000;
        for (int i = 0; i < rolls; i++)
        {
            var r = LootTable.Roll(LootTier.Normal, rng);
            if (!r.drops) continue;
            drops++;
            if (r.rarity == ItemRarity.Magic) magic++;
            else if (r.rarity == ItemRarity.Rare) rare++;
            else unique++;
        }
        Assert.Greater(drops, 1000, "Normal tier must drop often enough to measure.");
        float m = (float)magic / drops, r2 = (float)rare / drops, u = (float)unique / drops;
        Assert.That(m, Is.InRange(0.70f, 0.90f), $"Magic share {m} off 0.80.");
        Assert.That(r2, Is.InRange(0.10f, 0.26f), $"Rare share {r2} off 0.18.");
        Assert.That(u, Is.InRange(0.0f, 0.06f), $"Unique share {u} off 0.02.");
    }

    [Test]
    public void ChestRoll_AlwaysRarePlus()
    {
        var rng = new System.Random(7);
        for (int i = 0; i < 100; i++)
        {
            var r = LootTable.Roll(LootTier.Chest, rng);
            Assert.IsTrue(r.drops, "Chest must always drop.");
            Assert.AreNotEqual(ItemRarity.Magic, r.rarity, "Chest must never roll Magic.");
        }
    }

    [UnityTest]
    public System.Collections.IEnumerator Elite_SurvivesNormalBurst()
    {
        var husk = SpawnHusk(Vector3.zero);
        var health = husk.GetComponent<EnemyHealth>();
        health.SetTier(15, LootTier.Elite);
        for (int i = 0; i < 14; i++) health.TakeDamage(1);
        Assert.IsTrue(husk != null, "Elite (15 HP) must survive 14 damage.");
        health.TakeDamage(1);
        yield return null; // Destroy applies at frame end
        Assert.IsTrue(husk == null, "Elite must die at 15 damage.");
    }

    [Test]
    public void MapInput_IsCameraRelative()
    {
        // Yaw 45°: pressing "forward" moves diagonally (sin45, 0, cos45).
        Vector3 fwd = DiabloClickController.MapInput(new Vector2(0f, 1f), 45f);
        Assert.AreEqual(Mathf.Sin(45f * Mathf.Deg2Rad), fwd.x, 0.001f);
        Assert.AreEqual(Mathf.Cos(45f * Mathf.Deg2Rad), fwd.z, 0.001f);
        Assert.AreEqual(0f, fwd.y, 0.001f);
        Vector3 strafe = DiabloClickController.MapInput(new Vector2(1f, 0f), 45f);
        Assert.Greater(strafe.x, 0.5f, "Right key must strafe right.");
        Assert.Less(Mathf.Abs(strafe.z), 0.75f, "Right key must not move mostly forward.");
    }

    [Test]
    public void Aim_FacesCursorPoint()
    {
        controller.SetAimPoint(new Vector3(5f, 0f, 0f));
        Vector3 fwd = player.transform.forward;
        fwd.y = 0f;
        Assert.AreEqual(1f, fwd.normalized.x, 0.01f, "Hero must face the cursor point.");
    }

    [UnityTest]
    public System.Collections.IEnumerator Strike_HitsHuskInFacingArc()
    {
        // Stationary rework: no step-in glue, so the husk starts at 0.8 m —
        // two 0.6 m knockbacks still leave the corpse inside the 2.2 m arc
        // for the third swing (1.5 m would strand it at 2.7 m).
        var front = SpawnHusk(new Vector3(0f, 1f, 0.8f));
        var behind = SpawnHusk(new Vector3(0f, 1f, -1.5f));
        controller.SetAimPoint(new Vector3(0f, 0f, 10f));
        for (int i = 0; i < 3; i++) Assert.IsTrue(controller.Strike(), "Strike into the arc must hit.");
        yield return null; // Destroy applies at frame end
        Assert.IsTrue(front == null, "Husk in the facing arc must die from 3 strikes.");
        Assert.IsTrue(behind != null, "Husk behind the hero must survive.");
        Assert.AreEqual(1, EnemyHealth.HusksKilled);
    }

    [Test]
    public void Strike_WhiffsLoudlyWithEmptyArc()
    {
        var go = new GameObject("TestStriker");
        go.AddComponent<CharacterController>();
        var clicker = go.AddComponent<DiabloClickController>();
        go.AddComponent<DiabloMelee>();
        var driver = go.AddComponent<DiabloAnimator>(); // RequireComponent pulls in Animator
        clicker.SetAimPoint(new Vector3(0f, 0f, 10f));
        Assert.IsFalse(clicker.Strike(), "Strike into an empty arc must miss.");
        Assert.AreEqual(0, EnemyHealth.HusksKilled, "Whiff must deal no damage.");
        Assert.AreEqual(1, driver.strikeCount, "Whiff must still swing (visible feedback).");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void DiabloAnimator_MovesWhileOrdered()
    {
        var go = new GameObject("TestDriver");
        go.AddComponent<CharacterController>();
        var controller = go.AddComponent<DiabloClickController>();
        Assert.IsFalse(DiabloAnimator.IsMoving(controller), "Fresh hero is idle.");
        controller.SetDestination(new Vector3(0f, 1f, 5f));
        Assert.IsTrue(DiabloAnimator.IsMoving(controller), "Hero with a destination runs.");
        var husk = SpawnHusk(new Vector3(0f, 1f, 1f));
        controller.AttackTarget(husk);
        Assert.IsTrue(DiabloAnimator.IsMoving(controller), "Hero with an attack target runs.");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void FpsReporter_ComputesAverage()
    {
        Assert.AreEqual(60f, FpsReporter.ComputeFps(300, 5f), 0.01f);
        Assert.AreEqual(0f, FpsReporter.ComputeFps(0, 0f), 0.01f);
        Assert.AreEqual(8f, FpsReporter.ComputeFps(40, 5f), 0.01f);
    }

    [Test]
    public void Equip_RecalculatesDerivedStats()
    {
        Assert.AreEqual(1f, equipment.GetDamage());
        Assert.AreEqual(Equipment.BaseMaxHP, equipment.GetMaxHP());

        var weapon = new ItemInstance
        {
            slot = ItemSlot.Weapon, rarity = ItemRarity.Magic, baseType = "ShortSword",
        };
        weapon.affixes.Add(new Affix("Damage", 3f));
        equipment.Equip(weapon);
        Assert.AreEqual(4f, equipment.GetDamage());

        var armor = new ItemInstance
        {
            slot = ItemSlot.Armor, rarity = ItemRarity.Rare, baseType = "Breastplate",
        };
        armor.affixes.Add(new Affix("MaxHP", 12f));
        equipment.Equip(armor);
        Assert.AreEqual(Equipment.BaseMaxHP + 12f, equipment.GetMaxHP());
    }
}
