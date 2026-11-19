using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// party-team: computer teammates are itemizable and stay on the
/// protagonist's level. Gear on a companion scales its damage, heal and
/// survivability; class-bound weapons only fit their own class; the roster
/// mirrors the hero's level, attributes and ranks without saves or HUDs.
/// </summary>
public class PartyGearLevelTests
{
    GameObject leader;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        EnemyHealth.ResetRun();
        DiabloProjectile.ResetRun();
        leader = new GameObject(DiabloNetworkPlayer.HeroName);
        leader.transform.position = new Vector3(0f, 0.05f, 0f);
        leader.AddComponent<Equipment>();
        leader.AddComponent<PlayerHealth>();
        leader.AddComponent<DiabloProgression>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var c in Object.FindObjectsByType<PartyCompanion>(FindObjectsSortMode.None)) Object.DestroyImmediate(c.gameObject);
        var roster = GameObject.Find("PartyRoster");
        if (roster != null) Object.DestroyImmediate(roster);
        foreach (var go in GameObject.FindGameObjectsWithTag("Husk")) Object.DestroyImmediate(go);
        Object.DestroyImmediate(leader);
        RunProfile.Reset();
        EnemyHealth.ResetRun();
        DiabloProjectile.ResetRun();
    }

    static PartyCompanion ByRole(PartyRoster roster, ClassDefinition.PartyRole role)
    {
        foreach (var c in roster.Companions)
            if (c != null && c.Role.role == role) return c;
        return null;
    }

    [UnityTest]
    public IEnumerator Companions_SpawnWithGearAwareHealth()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        var roster = PartyRoster.EnsureSoloParty(leader.transform);
        yield return null;
        var tank = ByRole(roster, ClassDefinition.PartyRole.Tank);
        Assert.IsNotNull(tank);
        Assert.IsNotNull(tank.Gear, "companions spawn with their own gear slots");
        var php = tank.GetComponent<PlayerHealth>();
        Assert.AreEqual(Equipment.BaseMaxHP, php.MaxHP, 0.0001f,
            "fresh companion: base hp, gear counted from the start");

        php.TakeDamage(10f);
        var armor = new ItemInstance
        {
            slot = ItemSlot.Armor,
            rarity = ItemRarity.Magic,
            baseType = "ClothTunic",
            affixes = new List<Affix> { new Affix(AffixLibrary.MaxHP, 6f) },
        };
        Assert.IsTrue(tank.Equip(armor), "unrestricted armor fits every companion");
        Assert.AreEqual(Equipment.BaseMaxHP + 6f, php.MaxHP, 0.0001f,
            "companion armor raises companion max hp");
        Assert.AreEqual(Equipment.BaseMaxHP - 10f, php.CurrentHP, 0.0001f,
            "equipping never heals, it only raises the ceiling");
    }

    [UnityTest]
    public IEnumerator Equip_RespectsCompanionClass()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId; // healer + dps companions
        var roster = PartyRoster.EnsureSoloParty(leader.transform);
        yield return null;
        var dps = ByRole(roster, ClassDefinition.PartyRole.Dps);
        var healer = ByRole(roster, ClassDefinition.PartyRole.Healer);
        Assert.IsNotNull(dps);
        Assert.IsNotNull(healer);

        Assert.IsTrue(dps.Equip(ItemDatabase.CreateStarterBow()), "rogue bow fits the rogue");
        Assert.IsFalse(dps.Equip(ItemDatabase.CreateStarterSword()), "fenwalker blade does not");
        Assert.IsTrue(healer.Equip(ItemDatabase.CreateStarterLantern()), "lantern fits the healer");
        Assert.IsFalse(healer.Equip(ItemDatabase.CreateStarterBow()), "rogue bow does not");
        Assert.IsFalse(dps.Equip(null), "nothing equips nothing");
        Assert.AreSame(ClassCatalog.Rogue, dps.Role, "failed equips change neither role nor gear");
    }

    [UnityTest]
    public IEnumerator Gear_BoostsCompanionDamageAndHeal()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        var roster = PartyRoster.EnsureSoloParty(leader.transform);
        yield return null; // classOverride wires in Start
        var dps = ByRole(roster, ClassDefinition.PartyRole.Dps);
        var healer = ByRole(roster, ClassDefinition.PartyRole.Healer);
        var dpsSkills = dps.GetComponent<DiabloSkills>();
        var healerSkills = healer.GetComponent<DiabloSkills>();

        int plainShot = dpsSkills.EffectiveDamage(DiabloSkills.Skill.Arrow);
        var bow = new ItemInstance
        {
            slot = ItemSlot.Weapon,
            rarity = ItemRarity.Rare,
            baseType = "ShortBow",
            affixes = new List<Affix> { new Affix(AffixLibrary.Damage, 3f) },
        };
        Assert.IsTrue(dps.Equip(bow));
        Assert.Greater(dpsSkills.EffectiveDamage(DiabloSkills.Skill.Arrow), plainShot,
            "weapon damage scales the companion's arrows");

        int plainMend = healerSkills.EffectiveHeal(DiabloSkills.Skill.MendingLight);
        var circlet = new ItemInstance
        {
            slot = ItemSlot.Helmet,
            rarity = ItemRarity.Magic,
            baseType = "Circlet",
            affixes = new List<Affix> { new Affix(AffixLibrary.SkillRank, 2f) },
        };
        Assert.IsTrue(healer.Equip(circlet));
        Assert.Greater(healerSkills.EffectiveHeal(DiabloSkills.Skill.MendingLight), plainMend,
            "skill ranks scale the companion's heal");

        var leaderHp = leader.GetComponent<PlayerHealth>();
        leaderHp.TakeDamage(15f);
        float hurt = leaderHp.CurrentHP;
        float restored = healerSkills.DoTargetHeal(cosmetic: false);
        Assert.Greater(restored, 0f, "the geared heal lands on a real target");
        Assert.Greater(leaderHp.CurrentHP, hurt, "the leader is mended by geared companion heal");
    }

    [UnityTest]
    public IEnumerator Companions_MirrorHeroLevelAndAttributes()
    {
        RunProfile.ClassId = ClassCatalog.LanternbearerId; // tank + dps companions
        var heroProg = leader.GetComponent<DiabloProgression>();
        heroProg.SetState(5, 7);
        heroProg.Sheet.strength = 2;
        heroProg.Sheet.vitality = 1;
        heroProg.Sheet.focus = 3;

        var roster = PartyRoster.EnsureSoloParty(leader.transform);
        var tank = ByRole(roster, ClassDefinition.PartyRole.Tank);
        Assert.IsNotNull(tank);
        var prog = tank.GetComponent<DiabloProgression>();
        Assert.IsTrue(prog.IsCompanion);
        Assert.AreEqual(5, prog.Level, "spawn mirrors the hero level at once");
        Assert.AreEqual(7, prog.Xp);
        Assert.AreEqual(2, prog.Sheet.strength, "spawn mirrors the hero build");
        Assert.AreEqual(3, prog.Sheet.focus);
        Assert.AreEqual(
            Equipment.BaseMaxHP + 4f * DiabloProgression.LifePerLevel + 1f * 5f,
            tank.GetComponent<PlayerHealth>().MaxHP, 0.0001f,
            "mirrored level and vitality raise companion max hp");

        heroProg.SetState(6, 0);
        yield return null; // roster sync propagates the new level
        Assert.AreEqual(6, prog.Level, "roster sync keeps companions on the hero's level");
        Assert.IsNull(tank.transform.Find("XpCanvas"), "companions run no own xp bar");
        Assert.IsNotNull(leader.transform.Find("XpCanvas"), "the hero keeps its xp bar");
    }
}
