using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// S3 crit + area + host-authoritative hits: fixed RNG thresholds (ForceCrit
/// seam), one hit per valid target, inside/outside at the scaled edge, scaled
/// heal/control areas, unchanged single-target/ticks, rejected
/// cooldown/radius abuse, replicated crit flag.
/// </summary>
public class ItemCombatStatsTests
{
    GameObject floor;
    GameObject player;
    DiabloSkills skills;
    DiabloMelee melee;
    Equipment eq;
    string savedClass;

    [SetUp]
    public void Setup()
    {
        HitStop.Enabled = false;
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        DiabloProjectile.ResetRun();
        DiabloJuice.ResetRun();
        savedClass = RunProfile.ClassId;
        floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "TestFloor";
        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.AddComponent<CharacterController>();
        player.AddComponent<DiabloClickController>(); // attaches DiabloSkills
        melee = player.AddComponent<DiabloMelee>();
        eq = player.AddComponent<Equipment>();
        player.AddComponent<PlayerHealth>();
        skills = player.GetComponent<DiabloSkills>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var p in GameObject.FindObjectsOfType<DiabloProjectile>())
            Object.DestroyImmediate(p.gameObject);
        foreach (var ally in GameObject.FindObjectsOfType<PlayerHealth>())
            if (ally.gameObject.name == "TestAlly")
                Object.DestroyImmediate(ally.gameObject);
        DiabloTestFixture.CleanupTestScene(player);
        if (floor != null) Object.DestroyImmediate(floor);
        RunProfile.ClassId = savedClass;
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        DiabloProjectile.ResetRun();
        DiabloJuice.ResetRun();
        HitStop.Enabled = true;
    }

    static ItemInstance Item(ItemSlot slot, params (string key, float value)[] affixes)
    {
        var item = new ItemInstance { slot = slot, rarity = ItemRarity.Rare, baseType = "Test" };
        foreach (var (k, v) in affixes) item.affixes.Add(new Affix(k, v));
        return item;
    }

    static GameObject SpawnHusk(Vector3 pos, int hp)
    {
        var husk = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        husk.name = "TestHusk";
        husk.tag = "Husk";
        husk.transform.position = pos;
        husk.AddComponent<EnemyHealth>().SetTier(hp, LootTier.Normal);
        return husk;
    }

    static GameObject SpawnAlly(Vector3 pos)
    {
        var ally = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        ally.name = "TestAlly";
        ally.transform.position = pos;
        ally.AddComponent<PlayerHealth>();
        return ally;
    }

    // ---------- crit ----------

    [Test]
    public void Crit_BasePlusLegacyPlusRating_CappedAt75()
    {
        Assert.AreEqual(5f, eq.GetCrit(), 0.001f, "naked base");
        eq.Equip(Item(ItemSlot.Weapon, ("Crit", 10)));
        eq.Equip(Item(ItemSlot.Helmet, ("Crit", 5), ("CritRating", 100)));
        Assert.AreEqual(30f, eq.GetCrit(), 0.001f, "5 base + 15 legacy + 100 rating / 10");
        eq.Equip(Item(ItemSlot.Helmet, ("Crit", 10), ("CritRating", 2000)));
        Assert.AreEqual(Equipment.MaxCritChance, eq.GetCrit(), 0.001f, "cap holds at 75");
    }

    [Test]
    public void Crit_ForceSeamDoublesExactlyTwoX()
    {
        DiabloJuice.ForceCrit = true;
        int doubled = DiabloJuice.RollCrit(player.transform, 10, out bool crit);
        Assert.IsTrue(crit);
        Assert.AreEqual(Mathf.RoundToInt(10 * DiabloJuice.CritMultiplier), doubled);
        Assert.AreEqual(2f, DiabloJuice.CritMultiplier, 0.001f, "S3 keeps the 2x crit");
        DiabloJuice.ForceCrit = false;
        Assert.AreEqual(10, DiabloJuice.RollCrit(player.transform, 10, out crit));
        Assert.IsFalse(crit);
    }

    [Test]
    public void Crit_ServerRollMayExceedInputCap_WhileInputStaysCapped()
    {
        DiabloJuice.ForceCrit = true;
        int serverBlow = DiabloJuice.RollCrit(player.transform, 40, out _);
        Assert.Greater(serverBlow, HitValidator.MaxDamage, "legit 2x server crit may exceed MaxDamage");
        Assert.IsFalse(HitValidator.DamagePlausible(serverBlow), "client-sent numbers stay capped");
        Assert.IsTrue(HitValidator.DamagePlausible(40));
    }

    // ---------- area ----------

    [Test]
    public void Area_SqrtScalingAndHundredCap()
    {
        Assert.AreEqual(2.5f, eq.ScaleArea(2.5f), 0.0001f, "no gear = identity");
        eq.Equip(Item(ItemSlot.Armor, ("IncreasedArea", 44)));
        Assert.AreEqual(2.5f * 1.2f, eq.ScaleArea(2.5f), 0.0001f, "sqrt(1.44) = 1.2");
        eq.Equip(Item(ItemSlot.Armor, ("IncreasedArea", 500)));
        Assert.AreEqual(100f, eq.AreaBonusPct(), 0.001f, "cap at 100%");
        Assert.AreEqual(2.5f * Mathf.Sqrt(2f), eq.ScaleArea(2.5f), 0.0001f);
    }

    [Test]
    public void Area_NewAffixesRollInTheirSlots()
    {
        Assert.IsNotNull(AffixLibrary.Get(AffixLibrary.CritRating));
        Assert.IsNotNull(AffixLibrary.Get(AffixLibrary.IncreasedArea));
        CollectionAssert.Contains(AffixLibrary.PoolFor(ItemSlot.Weapon), AffixLibrary.CritRating);
        CollectionAssert.Contains(AffixLibrary.PoolFor(ItemSlot.Helmet), AffixLibrary.CritRating);
        CollectionAssert.Contains(AffixLibrary.PoolFor(ItemSlot.Armor), AffixLibrary.IncreasedArea);
        CollectionAssert.Contains(AffixLibrary.PoolFor(ItemSlot.Helmet), AffixLibrary.IncreasedArea);
        CollectionAssert.DoesNotContain(AffixLibrary.PoolFor(ItemSlot.Weapon), AffixLibrary.IncreasedArea);
        var rng = new System.Random(5);
        for (int i = 0; i < 100; i++)
        {
            float v = AffixLibrary.RollValue(AffixLibrary.CritRating, 1, rng);
            Assert.That(v, Is.InRange(5f, 20f));
            float a = AffixLibrary.RollValue(AffixLibrary.IncreasedArea, 2, rng);
            Assert.That(a, Is.InRange(15f, 40f));
        }
    }

    [Test]
    public void Area_TooltipNamesPercent()
    {
        eq.Equip(Item(ItemSlot.Armor, ("IncreasedArea", 44)));
        string line = DiabloInventoryUI.AreaLine(eq);
        StringAssert.Contains("+44%", line);
        StringAssert.Contains("x1.2", line);
    }

    // ---------- scaled hit space ----------

    [UnityTest]
    public IEnumerator Cleave_HitsInsideScaledEdge_Once_SkipsOutside()
    {
        yield return null;
        DiabloJuice.ForceCrit = false; // deterministic 4-blow
        eq.Equip(Item(ItemSlot.Armor, ("IncreasedArea", 44))); // 2.6 * 1.2 = 3.12
        var inside = SpawnHusk(new Vector3(0f, 1.05f, 3.0f), 100);
        var outside = SpawnHusk(new Vector3(0f, 1.05f, 3.5f), 100);
        skills.DoCleave(Vector3.forward, cosmetic: false);
        yield return null;
        Assert.AreEqual(100 - skills.EffectiveDamage(DiabloSkills.Skill.Cleave),
            inside.GetComponent<EnemyHealth>().CurrentHP, "exactly one direct blow, no crit");
        Assert.AreEqual(100, outside.GetComponent<EnemyHealth>().CurrentHP, "outside the scaled edge survives");
    }

    [UnityTest]
    public IEnumerator Halo_HitSpaceMatchesScaledVfxRadius()
    {
        yield return null;
        DiabloJuice.ForceCrit = false;
        eq.Equip(Item(ItemSlot.Armor, ("IncreasedArea", 44))); // 3.5 * 1.2 = 4.2
        var inside = SpawnHusk(new Vector3(0f, 1.05f, 4.0f), 100);
        var outside = SpawnHusk(new Vector3(0f, 1.05f, 4.5f), 100);
        int before = PresentationVfx.CleaveArcs;
        int hits = skills.DoHalo(cosmetic: false);
        yield return null;
        Assert.AreEqual(1, hits, "scaled halo clips exactly one husk");
        Assert.AreEqual(100 - skills.EffectiveDamage(DiabloSkills.Skill.SearingHalo),
            inside.GetComponent<EnemyHealth>().CurrentHP);
        Assert.AreEqual(100, outside.GetComponent<EnemyHealth>().CurrentHP);
        Assert.Greater(PresentationVfx.CleaveArcs, before, "same call emits the identical scaled ring");
    }

    [Test]
    public void Fireball_BlastRadiusIsScaled_ArrowStaysSingleTarget()
    {
        eq.Equip(Item(ItemSlot.Armor, ("IncreasedArea", 44)));
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.Fireball, Vector3.forward));
        var shots = GameObject.FindObjectsOfType<DiabloProjectile>();
        Assert.AreEqual(1, shots.Length);
        Assert.AreEqual(skills.ScaledArea(skills.fireballBlastRadius), shots[0].blastRadius, 0.0001f);
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.Arrow, Vector3.forward));
        foreach (var p in GameObject.FindObjectsOfType<DiabloProjectile>())
            if (p.kind == DiabloSkills.Skill.Arrow)
                Assert.AreEqual(0f, p.blastRadius, "flight/single-target ranges never scale");
    }

    [Test]
    public void GroupHeal_ScaledRadiusMendsExtendedRing()
    {
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        eq.Equip(Item(ItemSlot.Armor, ("IncreasedArea", 44))); // 6 * 1.2 = 7.2
        var near = SpawnAlly(new Vector3(0f, 1.05f, 5.0f));
        var extended = SpawnAlly(new Vector3(0f, 1.05f, 6.5f));
        var far = SpawnAlly(new Vector3(0f, 1.05f, 8.0f));
        foreach (var a in new[] { near, extended, far })
            a.GetComponent<PlayerHealth>().TakeDamage(10f);
        float restored = skills.DoGroupHeal(cosmetic: false);
        Assert.Greater(restored, 0f);
        Assert.Greater(near.GetComponent<PlayerHealth>().CurrentHP, 10f);
        Assert.Greater(extended.GetComponent<PlayerHealth>().CurrentHP, 10f,
            "scaled ring reaches past the base 6 m");
        Assert.AreEqual(10f, far.GetComponent<PlayerHealth>().CurrentHP, 0.001f);
    }

    // ---------- unchanged paths ----------

    [UnityTest]
    public IEnumerator TicksThornsAndHeals_NeverCrit()
    {
        yield return null;
        DiabloJuice.ForceCrit = true; // would double everything crit-capable
        int rollsBefore = DiabloJuice.CritsRolled;
        var husk = SpawnHusk(new Vector3(0f, 1.05f, 2f), 100);
        DiabloSkills.ApplyHit(husk.gameObject, 5, player.transform.position, 0f,
            player.transform, canCrit: false);
        Assert.AreEqual(95, husk.GetComponent<EnemyHealth>().CurrentHP, "tick keeps its normal result");
        husk.GetComponent<EnemyHealth>().TakeDamage(3, crit: false); // thorns path: flat TakeDamage
        Assert.AreEqual(92, husk.GetComponent<EnemyHealth>().CurrentHP);
        Assert.AreEqual(rollsBefore, DiabloJuice.CritsRolled, "no crit rolled for ticks/thorns");
        var ally = SpawnAlly(new Vector3(0f, 1.05f, 1f));
        ally.GetComponent<PlayerHealth>().TakeDamage(5f);
        ally.GetComponent<PlayerHealth>().Heal(5f);
        Assert.AreEqual(rollsBefore, DiabloJuice.CritsRolled, "heals never roll");
    }

    [UnityTest]
    public IEnumerator Melee_ThreatBooksPostCritBlow()
    {
        yield return null;
        DiabloJuice.ForceCrit = true;
        var husk = SpawnHusk(new Vector3(0f, 1.05f, 2f), 100);
        Assert.IsTrue(melee.TryAttack(husk, player.transform));
        yield return null;
        var table = husk.GetComponent<ThreatTable>();
        Assert.IsNotNull(table);
        Assert.AreEqual(2, table.ThreatOf(player.transform), 0.001f, "1 base damage x2 crit booked");
        StringAssert.EndsWith("!", DiabloJuice.LastNumberText, "crit flag rides the number");
    }

    // ---------- host intent gate ----------

    [Test]
    public void IntentGate_RejectsAbuse_WithoutMutation()
    {
        var pos = new Vector3(0f, 0f, 0f);
        var dir = Vector3.forward;
        string ok = DiabloNetworkPlayer.ValidateSkillIntent(
            (int)DiabloSkills.Skill.Cleave, ClassCatalog.Fenwalker, pos, pos, dir, 0f);
        Assert.IsNull(ok, "clean intent passes");
        StringAssert.StartsWith("unknown skill", DiabloNetworkPlayer.ValidateSkillIntent(
            999, ClassCatalog.Fenwalker, pos, pos, dir, 0f));
        StringAssert.StartsWith("cooldown", DiabloNetworkPlayer.ValidateSkillIntent(
            (int)DiabloSkills.Skill.Cleave, ClassCatalog.Fenwalker, pos, pos, dir, 1.5f));
        StringAssert.StartsWith("anchor", DiabloNetworkPlayer.ValidateSkillIntent(
            (int)DiabloSkills.Skill.Cleave, ClassCatalog.Fenwalker, pos, new Vector3(10f, 0f, 0f), dir, 0f));
        Assert.AreEqual("facing", DiabloNetworkPlayer.ValidateSkillIntent(
            (int)DiabloSkills.Skill.Cleave, ClassCatalog.Fenwalker, pos, pos, Vector3.zero, 0f));
        StringAssert.StartsWith("class", DiabloNetworkPlayer.ValidateSkillIntent(
            (int)DiabloSkills.Skill.SearingHalo, ClassCatalog.Rogue, pos, pos, dir, 0f));
        Assert.AreEqual(0, EnemyHealth.HusksKilled, "gate rejects without any mutation");
    }

    [UnityTest]
    public IEnumerator HostExecute_RunsFromGear_StampsCooldown()
    {
        yield return null;
        DiabloJuice.ForceCrit = false;
        var husk = SpawnHusk(new Vector3(0f, 1.05f, 2f), 100);
        Assert.IsTrue(skills.ServerExecuteSkill(
            DiabloSkills.Skill.Cleave, Vector3.forward, ClassCatalog.Fenwalker));
        yield return null;
        Assert.AreEqual(100 - skills.EffectiveDamage(DiabloSkills.Skill.Cleave),
            husk.GetComponent<EnemyHealth>().CurrentHP, "host rolls from S2 gear");
        Assert.IsFalse(skills.ServerExecuteSkill(
            DiabloSkills.Skill.Cleave, Vector3.forward, ClassCatalog.Fenwalker),
            "authoritative cooldown gates the replay");
        Assert.IsFalse(skills.ServerExecuteSkill(
            (DiabloSkills.Skill)999, Vector3.forward, ClassCatalog.Fenwalker),
            "unknown skill id executes nothing");
    }
}
