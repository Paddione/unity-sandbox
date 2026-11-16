using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>D2 combat feel end-to-end (headless): knockback, retaliation,
/// death-respawn, target bar, step-in, sounded swings.</summary>
public class DiabloD2FeelLoopTests
{
    GameObject player;
    DiabloClickController controller;
    DiabloMelee melee;
    Equipment equipment;
    PlayerHealth health;

    [SetUp]
    public void Setup()
    {
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        DiabloAudio.ResetRun();

        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.AddComponent<CharacterController>();
        controller = player.AddComponent<DiabloClickController>();
        melee = player.AddComponent<DiabloMelee>();
        melee.cooldown = 0f;
        equipment = player.AddComponent<Equipment>();
        equipment.Equip(ItemDatabase.CreateStarterSword());
        health = player.AddComponent<PlayerHealth>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var loot in GameObject.FindObjectsOfType<LootDrop>())
            if (loot != null) Object.DestroyImmediate(loot.gameObject);
        foreach (var orb in GameObject.FindObjectsOfType<ToyTrigger>())
            if (orb.gameObject.name.StartsWith("DiabloOrb_"))
                Object.DestroyImmediate(orb.gameObject);
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
            Object.DestroyImmediate(husk);
        var bar = GameObject.Find("DTargetBar");
        if (bar != null) Object.DestroyImmediate(bar);
        if (player != null) Object.DestroyImmediate(player);
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        DiabloAudio.ResetRun();
    }

    static GameObject SpawnHusk(Vector3 pos, int hp = 3, LootTier tier = LootTier.Normal)
    {
        var husk = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        husk.name = "TestHusk";
        husk.tag = "Husk";
        husk.transform.position = pos;
        husk.AddComponent<EnemyHealth>().SetTier(hp, tier);
        return husk;
    }

    [UnityTest]
    public IEnumerator Strike_KnocksBackEliteAndSounds()
    {
        var husk = SpawnHusk(new Vector3(0f, 1f, 1.5f), 15, LootTier.Elite);
        Vector3 before = husk.transform.position;
        controller.SetAimPoint(new Vector3(0f, 0f, 10f));
        Assert.IsTrue(controller.Strike(), "Strike into the arc must hit.");
        yield return null;
        Assert.IsTrue(husk != null, "Elite (15 HP) must survive one starter strike.");
        Assert.AreEqual(DiabloMelee.KnockbackElite,
            Vector3.Distance(before, husk.transform.position), 0.05f,
            "Hit must shove the elite 0.2 m.");
        Assert.Greater(DiabloAudio.soundedSwings, 0, "Swing must whoosh.");
        Assert.Greater(DiabloAudio.soundedHits, 0, "Hit must thump.");
        Assert.AreSame(husk, DiabloMelee.LastStruck, "Last struck must be the hit husk.");
    }

    [UnityTest]
    public IEnumerator Kill_SoundsDeath()
    {
        var husk = SpawnHusk(new Vector3(0f, 1f, 1.5f));
        controller.SetAimPoint(new Vector3(0f, 0f, 10f));
        Assert.IsTrue(controller.Strike(), "Strike into the arc must hit.");
        yield return null;
        Assert.IsTrue(husk == null, "Starter strike (3 dmg) must kill the 3 HP husk.");
        Assert.Greater(DiabloAudio.soundedDeaths, 0, "Kill must rattle.");
    }

    [UnityTest]
    public IEnumerator Retaliation_HurtsHero()
    {
        var husk = SpawnHusk(new Vector3(0f, 1f, 1.2f));
        husk.AddComponent<EnemyAttack>(); // default damage 1
        float maxHP = health.MaxHP;
        Assert.AreEqual(maxHP, health.CurrentHP);
        float waited = 0f;
        while (health.CurrentHP >= maxHP && waited < 4f)
        {
            yield return null;
            waited += Time.deltaTime;
        }
        Assert.Less(health.CurrentHP, maxHP, "Husk must strike back inside its range.");
    }

    [UnityTest]
    public IEnumerator Death_RespawnsAtCamp()
    {
        var husk = SpawnHusk(new Vector3(0f, 1f, 1.2f), 15, LootTier.Elite);
        var atk = husk.AddComponent<EnemyAttack>();
        atk.damage = 2;
        float waited = 0f;
        bool respawned = false;
        while (waited < 12f && !respawned)
        {
            yield return null;
            waited += Time.deltaTime;
            respawned = health.CurrentHP >= health.MaxHP
                && Vector3.Distance(player.transform.position, new Vector3(0f, 0.1f, 11f)) < 0.6f;
        }
        Assert.IsTrue(respawned, "Death must respawn the hero at the camp with full HP.");
    }

    [UnityTest]
    public IEnumerator TargetBar_ShowsStruckHusk()
    {
        var barGo = new GameObject("DTargetBar");
        var bar = barGo.AddComponent<DiabloTargetBar>();
        var husk = SpawnHusk(new Vector3(0f, 1f, 1.5f), 6, LootTier.Normal);
        controller.SetAimPoint(new Vector3(0f, 0f, 10f));
        Assert.IsTrue(controller.Strike(), "Strike must hit (3 of 6 HP).");
        yield return null;
        yield return null;
        Assert.AreEqual(0.5f, husk.GetComponent<EnemyHealth>().HPFraction(), 0.01f);
        Assert.AreEqual(0.5f, bar.TestFillAmount, 0.01f, "Bar must mirror husk HP.");
        Object.DestroyImmediate(husk);
    }

    [UnityTest]
    public IEnumerator Strike_StepsIntoAim()
    {
        controller.SetAimPoint(new Vector3(0f, 0f, 10f));
        Vector3 before = player.transform.position;
        Assert.IsFalse(controller.Strike(), "Empty arc must whiff.");
        Assert.AreEqual(DiabloClickController.StrikeStep,
            Vector3.Distance(before, player.transform.position), 0.05f,
            "Strike must step into the aim, even on whiffs.");
        yield return null;
    }
}
