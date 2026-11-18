using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// S5 save-on-join (cinderreach-coop-session): a client keeps its own
/// character across sessions (roundtrip through a join report), the host
/// clamps reported stats (level 1..30, non-negative points, catalog gear),
/// and gate travel stamps the save. The live ServerRpc/client broadcast legs
/// are proven by the 2-process pair run (both sides save, see coop-run-log).
/// </summary>
public class CoopSaveJoinTests
{
    GameObject hero;

    [SetUp]
    public void Setup()
    {
        ZoneGate.ResetRun();
        Wallet.ResetRun();
        hero = new GameObject("Player");
        hero.AddComponent<CharacterController>();
        hero.AddComponent<Equipment>();
        hero.AddComponent<Inventory>();
        hero.AddComponent<Wallet>();
        hero.AddComponent<PlayerHealth>();
        hero.AddComponent<DiabloProgression>();
    }

    [TearDown]
    public void TearDown()
    {
        if (hero != null) Object.DestroyImmediate(hero);
        foreach (var g in GameObject.FindObjectsOfType<ZoneGate>())
            if (g != null) Object.DestroyImmediate(g.gameObject);
        foreach (var p in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            if (p != null && p.gameObject != hero) Object.DestroyImmediate(p.gameObject);
        ZoneGate.ResetRun();
        Wallet.ResetRun();
        if (RunProfile.ClassId != ClassCatalog.FenwalkerId) RunProfile.ClassId = ClassCatalog.FenwalkerId;
        if (SaveService.HasSave()) SaveService.DeleteSave();
    }

    static ItemInstance KnownWeapon()
    {
        var bases = ItemDatabase.GetBaseTypes(ItemSlot.Weapon);
        Assert.IsNotEmpty(bases);
        return new ItemInstance { slot = ItemSlot.Weapon, rarity = ItemRarity.Magic, baseType = bases[0] };
    }

    static void GearUp(GameObject hero)
    {
        var prog = hero.GetComponent<DiabloProgression>();
        prog.SetState(5, 10, 1, 1, 2, 1, 0, new System.Collections.Generic.List<SkillRankEntry>());
        var eq = hero.GetComponent<Equipment>();
        eq.weapon = KnownWeapon();
        hero.GetComponent<Wallet>().Add(40);
    }

    [UnityTest]
    public IEnumerator ClientSave_RoundtripsLevelAndGear()
    {
        GearUp(hero);
        var gate = new GameObject(ZoneGate.FenGateName);
        var zg = gate.AddComponent<ZoneGate>();
        zg.destination = new Vector3(200f, 1f, 10f);
        zg.targetZone = 2;
        zg.ApplyTravel(hero, zg.destination); // stamps zoneReached + saves, like a gate pass
        Assert.IsTrue(ZoneGate.Teleports == 1);

        var fresh = new GameObject("FreshJoin");
        fresh.AddComponent<Equipment>();
        fresh.AddComponent<Inventory>();
        fresh.AddComponent<Wallet>();
        fresh.AddComponent<PlayerHealth>();
        Assert.IsTrue(SaveService.TryLoad(fresh), "join with a save takes over level and gear");
        Assert.AreEqual(2, SaveService.LoadedZoneReached);
        var prog = fresh.GetComponent<DiabloProgression>();
        Assert.AreEqual(5, prog.Level);
        Assert.AreEqual(40, fresh.GetComponent<Wallet>().Gold);
        Assert.AreEqual(hero.GetComponent<Equipment>().weapon.baseType,
            fresh.GetComponent<Equipment>().weapon.baseType);
        Object.DestroyImmediate(fresh);
        yield return null;
    }

    [UnityTest]
    public IEnumerator JoinReport_SurvivesJson()
    {
        GearUp(hero);
        var report = DiabloNetworkPlayer.CaptureJoinReport(hero);
        string json = JsonUtility.ToJson(report);
        var back = JsonUtility.FromJson<DiabloNetworkPlayer.JoinReport>(json);
        Assert.AreEqual(report.level, back.level);
        Assert.AreEqual(report.strength, back.strength);
        Assert.AreEqual(report.weapon.baseType, back.weapon.baseType);
        yield return null;
    }

    [UnityTest]
    public IEnumerator JoinSanitize_ClampsFakes()
    {
        var raw = new DiabloNetworkPlayer.JoinReport
        {
            level = 99,
            xp = -5,
            freeAttr = -2,
            freeSkill = -1,
            strength = -4,
            vitality = 3,
            focus = 1,
            ranks = new System.Collections.Generic.List<SkillRankEntry>(),
            armor = new ItemInstance { slot = ItemSlot.Armor, rarity = (ItemRarity)99, baseType = "Nope" },
            weapon = new ItemInstance { slot = ItemSlot.Consumable, rarity = ItemRarity.Magic, baseType = "Potion" },
            helmet = null,
        };
        var clean = DiabloNetworkPlayer.SanitizeJoinReport(raw);
        Assert.AreEqual(DiabloProgression.MaxLevel, clean.level, "level 99 clamps to 30");
        Assert.AreEqual(0, clean.xp);
        Assert.AreEqual(0, clean.freeAttr);
        Assert.AreEqual(3, clean.vitality, "honest values survive");
        Assert.IsNull(clean.armor, "unknown base dropped");
        Assert.IsNull(clean.weapon, "consumable is no weapon");
        Assert.IsNull(clean.helmet);
        yield return null;
    }

    [UnityTest]
    public IEnumerator JoinApply_SetsMirrorPower()
    {
        var bases = ItemDatabase.GetBaseTypes(ItemSlot.Weapon);
        var report = new DiabloNetworkPlayer.JoinReport
        {
            level = 12,
            xp = 0,
            freeAttr = 0,
            freeSkill = 0,
            strength = 3,
            vitality = 2,
            focus = 0,
            ranks = new System.Collections.Generic.List<SkillRankEntry>(),
            weapon = new ItemInstance { slot = ItemSlot.Weapon, rarity = ItemRarity.Magic, baseType = bases[0] },
        };
        var clean = DiabloNetworkPlayer.SanitizeJoinReport(report);
        var mirror = new GameObject("Mirror");
        mirror.AddComponent<Equipment>();
        mirror.AddComponent<PlayerHealth>();
        DiabloNetworkPlayer.ApplyJoinReport(mirror, clean);
        Assert.AreEqual(12, mirror.GetComponent<DiabloProgression>().Level);
        Assert.AreEqual(bases[0], mirror.GetComponent<Equipment>().weapon.baseType);
        Assert.Greater(mirror.GetComponent<PlayerHealth>().MaxHP, Equipment.BaseMaxHP - 1f);
        Object.DestroyImmediate(mirror);
        yield return null;
    }
}
