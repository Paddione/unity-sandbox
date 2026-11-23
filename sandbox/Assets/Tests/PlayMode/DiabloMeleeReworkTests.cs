using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>Cinderreach melee rework (stationary LMB cone, RMB outreach
/// thrust, Shift block + reflect, visible target bar).</summary>
public class DiabloMeleeReworkTests
{
    GameObject player;
    DiabloClickController controller;
    DiabloMelee melee;
    DiabloBlock block;
    Equipment equipment;
    PlayerHealth health;

    [SetUp]
    public void Setup()
    {
        DiabloJuice.ForceCrit = false; // exact-damage asserts: no random crits
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        DiabloAudio.ResetRun();
        DiabloProjectile.ResetRun();

        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.AddComponent<CharacterController>();
        controller = player.AddComponent<DiabloClickController>();
        melee = player.AddComponent<DiabloMelee>();
        melee.cooldown = 0f;
        block = player.GetComponent<DiabloBlock>();
        equipment = player.AddComponent<Equipment>();
        equipment.Equip(ItemDatabase.CreateStarterSword());
        health = player.AddComponent<PlayerHealth>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var loot in GameObject.FindObjectsByType<LootDrop>(FindObjectsSortMode.None))
            if (loot != null) Object.DestroyImmediate(loot.gameObject);
        foreach (var orb in GameObject.FindObjectsByType<ToyTrigger>(FindObjectsSortMode.None))
            if (orb.gameObject.name.StartsWith("DiabloOrb_"))
                Object.DestroyImmediate(orb.gameObject);
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
            Object.DestroyImmediate(husk);
        foreach (var proj in GameObject.FindObjectsByType<DiabloProjectile>(FindObjectsSortMode.None))
            if (proj != null) Object.DestroyImmediate(proj.gameObject);
        var bar = GameObject.Find("DTargetBar");
        if (bar != null) Object.DestroyImmediate(bar);
        if (player != null) Object.DestroyImmediate(player);
        var globeGo = GameObject.Find("DLifeGlobe");
        if (globeGo != null) Object.DestroyImmediate(globeGo);
        PlayerHealth.LastDeathTime = float.NegativeInfinity;
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        DiabloAudio.ResetRun();
        DiabloProjectile.ResetRun();
    }

    static GameObject SpawnHusk(Vector3 pos, int hp = 6, LootTier tier = LootTier.Normal)
    {
        var husk = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        husk.name = "TestHusk";
        husk.tag = "Husk";
        husk.transform.position = pos;
        husk.AddComponent<EnemyHealth>().SetTier(hp, tier);
        return husk;
    }

    [UnityTest]
    public IEnumerator StationaryCone_HitsTwoHusksWithoutMoving()
    {
        var left = SpawnHusk(new Vector3(0.5f, 1f, 1.5f));
        var right = SpawnHusk(new Vector3(-0.5f, 1f, 1.5f));
        var behind = SpawnHusk(new Vector3(0f, 1f, -1.5f));
        controller.SetAimPoint(new Vector3(0f, 0f, 10f));
        Vector3 before = player.transform.position;
        Assert.IsTrue(controller.Strike(), "Cone into the arc must hit.");
        yield return null;
        yield return null;
        Assert.AreEqual(0f, Vector3.Distance(before, player.transform.position), 0.001f,
            "LMB must not move the hero.");
        Assert.AreEqual(0.5f, left.GetComponent<EnemyHealth>().HPFraction(), 0.01f,
            "Left husk in the cone takes damage.");
        Assert.AreEqual(0.5f, right.GetComponent<EnemyHealth>().HPFraction(), 0.01f,
            "Right husk in the cone takes damage.");
        Assert.AreEqual(1f, behind.GetComponent<EnemyHealth>().HPFraction(), 0.001f,
            "Husk behind the hero is spared.");
        Assert.IsTrue(DiabloMelee.LastStruck == left || DiabloMelee.LastStruck == right,
            "Target bar follows a struck husk.");
    }

    [UnityTest]
    public IEnumerator StationaryCone_WhiffDoesNotMove()
    {
        controller.SetAimPoint(new Vector3(0f, 0f, 10f));
        Vector3 before = player.transform.position;
        Assert.IsFalse(controller.Strike(), "Empty arc must whiff.");
        Assert.AreEqual(0f, Vector3.Distance(before, player.transform.position), 0.001f,
            "Whiffs must not step the hero forward.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Thrust_OutreachesHuskMelee()
    {
        var husk = SpawnHusk(new Vector3(0f, 1f, 3f));
        controller.SetAimPoint(new Vector3(0f, 0f, 10f));
        Vector3 before = player.transform.position;
        Assert.IsFalse(controller.Strike(), "3 m is past the 2.2 m swing.");
        Assert.AreEqual(1f, husk.GetComponent<EnemyHealth>().HPFraction(), 0.001f,
            "Swing must not reach 3 m.");
        Assert.IsTrue(controller.Thrust(), "Thrust must reach 3 m.");
        yield return null;
        Assert.Less(husk.GetComponent<EnemyHealth>().HPFraction(), 1f,
            "Thrust damages past husk melee range (2.0 m).");
        // The fixture has no floor, so the controller can fall during the yielded frame.
        var displacement = Vector3.ProjectOnPlane(player.transform.position - before, Vector3.up);
        Assert.AreEqual(0f, displacement.magnitude, 0.001f, "Thrust must not advance the hero.");
        Object.DestroyImmediate(husk);
    }

    [Test]
    public void Block_HalvesFrontalDamage_FullFromBehind()
    {
        controller.SetAimPoint(new Vector3(0f, 0f, 10f));
        block.Held = true;
        float max = health.MaxHP;
        Assert.GreaterOrEqual(max, 8f, "Precondition: hero must take two 4-dmg hits.");
        health.TakeDamage(4f, player.transform.position + new Vector3(0f, 0f, 2f));
        Assert.AreEqual(max - 2f, health.CurrentHP, 0.001f, "Frontal hit is halved.");
        health.TakeDamage(4f, player.transform.position + new Vector3(0f, 0f, -2f));
        Assert.AreEqual(max - 6f, health.CurrentHP, 0.001f, "Hit from behind lands full.");
    }

    [Test]
    public void Block_ReleasedTakesFullDamage()
    {
        controller.SetAimPoint(new Vector3(0f, 0f, 10f));
        block.Held = false;
        float max = health.MaxHP;
        health.TakeDamage(4f, player.transform.position + new Vector3(0f, 0f, 2f));
        Assert.AreEqual(max - 4f, health.CurrentHP, 0.001f, "Released block takes full damage.");
    }

    [UnityTest]
    public IEnumerator Block_ReflectsHostileProjectile()
    {
        controller.SetAimPoint(new Vector3(0f, 0f, -10f));
        block.Held = true;
        var husk = SpawnHusk(new Vector3(0f, 1f, -8f));
        float hpBefore = health.CurrentHP;
        var p = DiabloProjectile.Spawn(DiabloSkills.Skill.Fireball, new Vector3(0f, 1.2f, -4f),
            Vector3.forward, null, RangedCaster.ShotDamage, 12f, 20f, 0f, hostile: true);
        int guard = 0;
        while (p != null && p.hostile && guard++ < 200) p.Step(0.02f);
        Assert.IsNotNull(p, "Projectile survives the block (reflected, not impacted).");
        Assert.IsFalse(p.hostile, "Blocked projectile turns friendly.");
        Assert.AreEqual(0, DiabloProjectile.HeroHits, "Blocked projectile never hits the hero.");
        Assert.AreEqual(hpBefore, health.CurrentHP, 0.001f, "Hero takes no damage on reflect.");
        for (int i = 0; i < 400 && p != null; i++) p.Step(0.02f);
        Assert.Less(husk.GetComponent<EnemyHealth>().HPFraction(), 1f,
            "Reflected projectile damages the husk it flies back into.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator TargetBar_HasSpriteAndMirrorsDamage()
    {
        var barGo = new GameObject("DTargetBar");
        var bar = barGo.AddComponent<DiabloTargetBar>();
        var fillImg = GameObject.Find("BarFill").GetComponent<Image>();
        Assert.IsNotNull(fillImg.sprite, "Bar fill needs a sprite or fillAmount never renders.");
        var husk = SpawnHusk(new Vector3(0f, 1f, 1.5f));
        controller.SetAimPoint(new Vector3(0f, 0f, 10f));
        Assert.IsTrue(controller.Strike(), "Strike must hit (3 of 6 HP).");
        yield return null;
        yield return null;
        Assert.AreEqual(0.5f, husk.GetComponent<EnemyHealth>().HPFraction(), 0.01f);
        Assert.AreEqual(0.5f, bar.TestFillAmount, 0.01f, "Bar mirrors husk HP.");
        Assert.AreEqual(bar.TestFillAmount, fillImg.fillAmount, 0.001f);
        Object.DestroyImmediate(husk);
    }
}
