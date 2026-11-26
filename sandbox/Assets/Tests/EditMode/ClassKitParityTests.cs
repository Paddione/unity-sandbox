using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// cinderreach-class-kits-parity: Fenwalker and Rogue carry six own skills,
/// every combo pays off by the formulas in <see cref="ClassCombos"/>, the
/// reworked uniques feed the combos, and old saves get their ranks back.
/// </summary>
public class ClassKitParityTests
{
    GameObject hero;
    DiabloSkills skills;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        EnemyHealth.ResetRun();
        DiabloProjectile.ResetRun();
        DiabloSkills.ResetRun();
        DiabloJuice.ForceCrit = false;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        hero = new GameObject("KitHero");
        hero.transform.position = Vector3.zero;
        hero.AddComponent<DiabloProgression>();
        hero.AddComponent<PlayerHealth>();
        skills = hero.AddComponent<DiabloSkills>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in GameObject.FindGameObjectsWithTag("Husk")) Object.DestroyImmediate(go);
        foreach (var k in PowderKeg.Active.ToArray()) if (k != null) Object.DestroyImmediate(k.gameObject);
        foreach (var c in SmokeCloud.Active.ToArray()) if (c != null) Object.DestroyImmediate(c.gameObject);
        foreach (var z in MireZone.Active.ToArray()) if (z != null) Object.DestroyImmediate(z.gameObject);
        Object.DestroyImmediate(hero);
        DiabloJuice.ForceCrit = null;
        DiabloSkills.ResetRun();
        EnemyHealth.ResetRun();
        DiabloProjectile.ResetRun();
        RunProfile.Reset();
    }

    static EnemyHealth Husk(string name, Vector3 pos, int hp = 60)
    {
        var go = new GameObject(name);
        go.tag = "Husk";
        go.transform.position = pos;
        var h = go.AddComponent<EnemyHealth>();
        h.SetTier(hp, LootTier.Normal);
        go.AddComponent<EnemyAttack>();
        return h;
    }

    Equipment Equip(string uniqueId)
    {
        var eq = hero.GetComponent<Equipment>() ?? hero.AddComponent<Equipment>();
        var item = ItemDatabase.CreateUnique(uniqueId);
        if (item.slot == ItemSlot.Weapon) eq.weapon = item;
        else if (item.slot == ItemSlot.Armor) eq.armor = item;
        else if (item.slot == ItemSlot.Helmet) eq.helmet = item;
        return eq;
    }

    static int Lost(EnemyHealth h) => h.maxHP - h.CurrentHP;

    // ------------------------------------------------------------ kits

    [Test]
    public void SkillEnum_AppendsKitSkillsWithoutMovingOldValues()
    {
        Assert.AreEqual(11, (int)DiabloSkills.Skill.RenewingCircle);
        Assert.AreEqual(12, (int)DiabloSkills.Skill.MireHook);
        Assert.AreEqual(13, (int)DiabloSkills.Skill.BogCharge);
        Assert.AreEqual(14, (int)DiabloSkills.Skill.MireGround);
        Assert.AreEqual(15, (int)DiabloSkills.Skill.IronVow);
        Assert.AreEqual(16, (int)DiabloSkills.Skill.SmokePot);
        Assert.AreEqual(17, (int)DiabloSkills.Skill.ShadowRoll);
        Assert.AreEqual(18, (int)DiabloSkills.Skill.VenomVial);
        Assert.AreEqual(19, (int)DiabloSkills.Skill.PowderKeg);
    }

    [Test]
    public void EveryClass_CarriesSixOwnSkills_OnTheSameKeys()
    {
        foreach (var cls in ClassCatalog.All)
        {
            Assert.AreEqual(6, cls.skills.Length, cls.id);
            CollectionAssert.AreEqual(new[] { "Q", "E", "R", "Space", "F", "G" }, cls.keys, cls.id);
            CollectionAssert.DoesNotContain(cls.skills, DiabloSkills.Skill.Fireball, cls.id);
            CollectionAssert.DoesNotContain(cls.skills, DiabloSkills.Skill.Arrow, cls.id);
            CollectionAssert.AllItemsAreUnique(cls.skills, cls.id);
        }
        CollectionAssert.AreEqual(new[]
        {
            DiabloSkills.Skill.Cleave, DiabloSkills.Skill.MireHook, DiabloSkills.Skill.SeismicSlam,
            DiabloSkills.Skill.BogCharge, DiabloSkills.Skill.MireGround, DiabloSkills.Skill.IronVow,
        }, ClassCatalog.Fenwalker.skills);
        CollectionAssert.AreEqual(new[]
        {
            DiabloSkills.Skill.QuickShot, DiabloSkills.Skill.BladeFlurry, DiabloSkills.Skill.SmokePot,
            DiabloSkills.Skill.ShadowRoll, DiabloSkills.Skill.VenomVial, DiabloSkills.Skill.PowderKeg,
        }, ClassCatalog.Rogue.skills);
    }

    [Test]
    public void KitSkills_HaveNameDescriptionIconAndCooldown()
    {
        foreach (var cls in ClassCatalog.All)
            foreach (var s in cls.skills)
            {
                Assert.IsFalse(string.IsNullOrEmpty(DiabloSkills.SkillName(s)), s.ToString());
                string desc = DiabloSkills.SkillDescription(s);
                Assert.IsFalse(desc.StartsWith("skill.desc."), $"{s} lacks a description");
                Assert.IsNotNull(DiabloMenuArt.SkillIcon(s), $"{s} lacks an icon");
                Assert.Greater(skills.BaseCooldown(s), 0f, s.ToString());
            }
    }

    [Test]
    public void KitSkills_ScaleWithTheClassSpecialty()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        Assert.AreEqual(9, skills.EffectiveDamage(DiabloSkills.Skill.PowderKeg), "6 x 1.5");
        Assert.AreEqual(2, skills.EffectiveDamage(DiabloSkills.Skill.VenomVial), "1 x 1.5 -> 2");
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        Assert.AreEqual(3, skills.EffectiveDamage(DiabloSkills.Skill.MireHook), "tank factor is control, not damage");
        Assert.AreEqual(0, skills.EffectiveDamage(DiabloSkills.Skill.IronVow));
    }

    // ------------------------------------------------------------ formulas

    [Test]
    public void Formulas_MatchTheTooltips()
    {
        Assert.AreEqual(4, ClassCombos.CleaveDamageOn(4, stunned: false, lancerWard: false, retribution: 0));
        Assert.AreEqual(6, ClassCombos.CleaveDamageOn(4, stunned: true, lancerWard: false, retribution: 0), "Shatter x1.5");
        Assert.AreEqual(8, ClassCombos.CleaveDamageOn(4, stunned: true, lancerWard: true, retribution: 0), "lancer-ward x2");
        Assert.AreEqual(4 + ClassCombos.RetributionCap, ClassCombos.CleaveDamageOn(4, false, false, 999), "Retribution capped");
        Assert.AreEqual(4f, ClassCombos.SlamStunOn(2f, mired: true), 0.0001f, "Fen Shackle x2");
        Assert.AreEqual(2f, ClassCombos.SlamStunOn(2f, mired: false), 0.0001f);
        Assert.AreEqual(6, ClassCombos.FlurryDamageOn(4, blinded: true), "Throat Cut x1.5");
        Assert.AreEqual(9 + 3 * ClassCombos.VenomBurnPerStack, ClassCombos.KegDamageOn(9, 3, ignited: false), "Venom Burn");
        Assert.AreEqual(Mathf.RoundToInt(9 * ClassCombos.IgniteFactor), ClassCombos.KegDamageOn(9, 0, ignited: true), "Spark Shot");
        Assert.AreEqual(ClassCombos.VenomMaxStacks, ClassCombos.AddStacks(4, 3), "venom caps");
    }

    // ------------------------------------------------------------ Fenwalker combos

    [Test]
    public void Shatter_CleaveHitsStunnedHuskHarder()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        var stunned = Husk("Stunned", new Vector3(1.5f, 0f, 0f));
        var free = Husk("Free", new Vector3(-1.5f, 0f, 0f));
        stunned.GetComponent<EnemyAttack>().Stun(5f);
        skills.DoCleave(Vector3.forward, cosmetic: false);
        int blow = skills.EffectiveDamage(DiabloSkills.Skill.Cleave);
        Assert.AreEqual(blow, Lost(free));
        Assert.AreEqual(ClassCombos.CleaveDamageOn(blow, true, false, 0), Lost(stunned));
        Assert.AreEqual(1, ClassCombos.Triggered(ClassCombos.Shatter));
    }

    [Test]
    public void LancerWard_DoublesShatter()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        Equip(ItemDatabase.LancerWardId);
        var stunned = Husk("Stunned", new Vector3(1.5f, 0f, 0f));
        stunned.GetComponent<EnemyAttack>().Stun(5f);
        skills.DoCleave(Vector3.forward, cosmetic: false);
        int blow = skills.EffectiveDamage(DiabloSkills.Skill.Cleave);
        Assert.AreEqual(ClassCombos.CleaveDamageOn(blow, true, true, 0), Lost(stunned));
    }

    [Test]
    public void FenShackle_SlamHoldsMiredHusksTwiceAsLong()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        var mired = Husk("Mired", new Vector3(1.5f, 0f, 0f));
        var dry = Husk("Dry", new Vector3(-1.5f, 0f, 0f));
        EnemyStatus.Mire(mired.gameObject, 5f);
        skills.DoSlam(cosmetic: false);
        Assert.AreEqual(skills.slamStunSeconds * ClassCombos.MiredStunFactor,
            mired.GetComponent<EnemyAttack>().StunLeft, 0.01f);
        Assert.AreEqual(skills.slamStunSeconds, dry.GetComponent<EnemyAttack>().StunLeft, 0.01f);
    }

    [Test]
    public void SeismicCrown_MiresEverythingStruck()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        Equip(ItemDatabase.SeismicCrownId);
        var a = Husk("A", new Vector3(1.5f, 0f, 0f));
        skills.DoSlam(cosmetic: false);
        Assert.IsTrue(EnemyStatus.IsMired(a.gameObject));
    }

    [Test]
    public void MireHook_PullsMiresAndCallsOut()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        var far = Husk("Far", new Vector3(0f, 0f, 7f));
        var hooked = ClassKitSkills.MireHook(skills, Vector3.forward, cosmetic: false);
        Assert.AreEqual(far.gameObject, hooked);
        Assert.AreEqual(ClassCombos.MireHookPullTo, far.transform.position.z, 0.01f, "pulled in front of the tank");
        Assert.IsTrue(EnemyStatus.IsMired(far.gameObject));
        Assert.AreEqual(skills.EffectiveDamage(DiabloSkills.Skill.MireHook), Lost(far));
        Assert.GreaterOrEqual(far.GetComponent<ThreatTable>().ThreatOf(hero.transform), ThreatTable.SlamThreatBonus, "hook calls the husk out");
    }

    [Test]
    public void MoorvogtAnchor_DragsMiredNeighbours()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        Equip(ItemDatabase.MoorvogtAnchorId);
        Husk("Target", new Vector3(0f, 0f, 8f));
        var neighbour = Husk("Neighbour", new Vector3(2f, 0f, 8f));
        var dryNeighbour = Husk("Dry", new Vector3(-2f, 0f, 8f));
        EnemyStatus.Mire(neighbour.gameObject, 5f);
        ClassKitSkills.MireHook(skills, Vector3.forward, cosmetic: false);
        Assert.Less(neighbour.transform.position.magnitude, 3f, "mired neighbour dragged in");
        Assert.AreEqual(8f, dryNeighbour.transform.position.z, 0.01f, "dry neighbour stays");
    }

    [Test]
    public void BogCharge_MovesTheTankAndMiresTheLane()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        var lane = Husk("Lane", new Vector3(0.5f, 0f, 3f));
        var aside = Husk("Aside", new Vector3(4f, 0f, 3f));
        int hits = ClassKitSkills.BogCharge(skills, Vector3.forward, cosmetic: false);
        Assert.AreEqual(1, hits);
        Assert.AreEqual(ClassCombos.BogChargeDistance, hero.transform.position.z, 0.01f);
        Assert.IsTrue(EnemyStatus.IsMired(lane.gameObject));
        Assert.IsFalse(EnemyStatus.IsMired(aside.gameObject));
    }

    [Test]
    public void MireGround_TickMiresAndHurts()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        var inPool = Husk("In", new Vector3(2f, 0f, 0f));
        var outPool = Husk("Out", new Vector3(9f, 0f, 0f));
        var zone = ClassKitSkills.MireGround(skills, cosmetic: false);
        Assert.AreEqual(1, zone.TickNow());
        Assert.IsTrue(EnemyStatus.IsMired(inPool.gameObject));
        Assert.Greater(Lost(inPool), 0);
        Assert.IsFalse(EnemyStatus.IsMired(outPool.gameObject));
        Assert.AreEqual(ClassCombos.MiredMoveFactor, EnemyStatus.MoveFactor(inPool.gameObject), 0.0001f);
    }

    [Test]
    public void Retribution_IronVowStoresDamage_NextCleaveReleasesItOnce()
    {
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        var husk = Husk("Target", new Vector3(1.5f, 0f, 0f));
        skills.cleaveKnockback = 0f; // keep the target inside the second swing
        var hp = hero.GetComponent<PlayerHealth>();
        ClassKitSkills.IronVow(skills, cosmetic: false);
        Assert.AreEqual(ClassCombos.IronVowShield, hp.ShieldAmount, 0.0001f);
        hp.TakeDamage(8f);
        var state = HeroComboState.For(hero);
        Assert.AreEqual(8, state.Retribution, "pre-shield damage counts");
        int blow = skills.EffectiveDamage(DiabloSkills.Skill.Cleave);
        skills.DoCleave(Vector3.forward, cosmetic: false);
        Assert.AreEqual(blow + 8, Lost(husk));
        Assert.AreEqual(0, state.Retribution, "one Cleave spends the store");
        skills.DoCleave(Vector3.forward, cosmetic: false);
        Assert.AreEqual(2 * blow + 8, Lost(husk));
    }

    [Test]
    public void Bulwark_BlockedHitChargesRetribution()
    {
        BuildUniqueEffects.ChargeBulwarkRetribution(hero, 5f);
        Assert.AreEqual(5, HeroComboState.For(hero).Retribution);
    }

    // ------------------------------------------------------------ Rogue combos

    [Test]
    public void VenomBurn_KegConsumesVenomForBonusDamage()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        var husk = Husk("Target", new Vector3(0f, 0f, 4f));
        int hits = ClassKitSkills.VenomVial(skills, Vector3.forward, cosmetic: false);
        Assert.AreEqual(1, hits);
        Assert.AreEqual(ClassCombos.VenomStacksPerVial, EnemyStatus.Venom(husk.gameObject));
        int afterVial = Lost(husk);
        var keg = ClassKitSkills.PowderKegCast(skills, Vector3.forward, cosmetic: false);
        int dealt = keg.Detonate(false);
        int blast = skills.EffectiveDamage(DiabloSkills.Skill.PowderKeg);
        Assert.AreEqual(ClassCombos.KegDamageOn(blast, ClassCombos.VenomStacksPerVial, false), dealt);
        Assert.AreEqual(afterVial + dealt, Lost(husk));
        Assert.AreEqual(0, EnemyStatus.Venom(husk.gameObject), "venom burnt off");
        Assert.AreEqual(1, ClassCombos.Triggered(ClassCombos.VenomBurn));
    }

    [Test]
    public void Venom_TicksPerStack()
    {
        var husk = Husk("Target", new Vector3(0f, 0f, 4f));
        EnemyStatus.AddVenom(husk.gameObject, hero.transform, 3);
        int dmg = husk.GetComponent<EnemyStatus>().TickVenomNow();
        Assert.AreEqual(3 * ClassCombos.VenomDamagePerStack, dmg);
        Assert.AreEqual(dmg, Lost(husk));
    }

    [Test]
    public void Ambush_ShadowRollMakesTheNextHitCrit_Once()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        DiabloJuice.ForceCrit = null;
        var husk = Husk("Target", new Vector3(0f, 0f, 9f));
        ClassKitSkills.ShadowRoll(skills, Vector3.back, cosmetic: false);
        Assert.AreEqual(-ClassCombos.ShadowRollDistance, hero.transform.position.z, 0.01f);
        Assert.IsTrue(DiabloSkills.ApplyHit(husk.gameObject, 2, hero.transform.position, 0f, hero.transform));
        Assert.IsFalse(DiabloSkills.ApplyHit(husk.gameObject, 2, hero.transform.position, 0f, hero.transform),
            "no gear crit, ambush spent");
        Assert.AreEqual(1, ClassCombos.Triggered(ClassCombos.Ambush));
    }

    [Test]
    public void KeenCadence_LengthensAmbush()
    {
        var eq = Equip(ItemDatabase.KeenCadenceId);
        Assert.AreEqual(ClassCombos.KeenAmbushSeconds, ClassCombos.AmbushWindow(eq), 0.0001f);
        Assert.AreEqual(ClassCombos.AmbushSeconds, ClassCombos.AmbushWindow(null), 0.0001f);
    }

    [Test]
    public void ThroatCut_FlurryCutsBlindedHusksHarder()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        var blind = Husk("Blind", new Vector3(0.6f, 0f, 1.5f));
        var sighted = Husk("Sighted", new Vector3(-0.6f, 0f, 1.5f));
        EnemyStatus.Blind(blind.gameObject, 5f);
        Assert.AreEqual(2, skills.DoFlurry(Vector3.forward, cosmetic: false));
        int blow = skills.EffectiveDamage(DiabloSkills.Skill.BladeFlurry);
        Assert.AreEqual(blow, Lost(sighted));
        Assert.AreEqual(ClassCombos.FlurryDamageOn(blow, true), Lost(blind));
    }

    [Test]
    public void SmokePot_BlindsHusksInTheCloud()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        var inside = Husk("Inside", new Vector3(0f, 0f, 5f));
        var outside = Husk("Outside", new Vector3(8f, 0f, -6f));
        ClassKitSkills.SmokePot(skills, Vector3.forward, cosmetic: false);
        Assert.IsTrue(EnemyStatus.IsBlinded(inside.gameObject));
        Assert.IsFalse(EnemyStatus.IsBlinded(outside.gameObject));
        Assert.IsTrue(SmokeCloud.Covers(inside.transform.position));
    }

    [Test]
    public void SmugglersFuse_KegInSmokeBlowsAtOnceAndPoisons()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        Equip(ItemDatabase.SmugglersFuseId);
        var husk = Husk("Target", new Vector3(0f, 0f, 4f));
        ClassKitSkills.SmokePot(skills, Vector3.forward, cosmetic: false);
        ClassKitSkills.PowderKegCast(skills, Vector3.forward, cosmetic: false);
        Assert.AreEqual(0, PowderKeg.Active.Count, "keg went off at once");
        Assert.Greater(Lost(husk), 0);
        Assert.AreEqual(BuildUniqueEffects.FuseVenomStacks, EnemyStatus.Venom(husk.gameObject));
    }

    [Test]
    public void Shadowstride_FlurryHitsShortenTheRoll()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        Equip(ItemDatabase.ShadowstrideVestId);
        Husk("A", new Vector3(0.5f, 0f, 1.5f));
        Husk("B", new Vector3(-0.5f, 0f, 1.5f));
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.ShadowRoll, Vector3.back));
        float before = skills.CooldownLeft(DiabloSkills.Skill.ShadowRoll);
        hero.transform.position = Vector3.zero;
        skills.DoFlurry(Vector3.forward, cosmetic: false);
        Assert.AreEqual(before - 2 * BuildUniqueEffects.ShadowstrideRollCut,
            skills.CooldownLeft(DiabloSkills.Skill.ShadowRoll), 0.01f);
    }

    [Test]
    public void QuickShotVariants_FanTradesDamageForBolts()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        CollectionAssert.AreEqual(new[] { SkillVariantCatalog.QuickSingleId, SkillVariantCatalog.QuickFanId },
            SkillVariantCatalog.VariantIds(ClassCatalog.RogueId, DiabloSkills.Skill.QuickShot));
        Assert.AreEqual(SkillVariantCatalog.QuickSingleId,
            SkillVariantCatalog.DefaultVariantId(ClassCatalog.RogueId, DiabloSkills.Skill.QuickShot), "legacy default");
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.QuickShot, Vector3.forward));
        Assert.AreEqual(1, DiabloProjectile.Launched);
        Assert.IsTrue(RunProfile.TrySetSkillVariant(ClassCatalog.RogueId, DiabloSkills.Skill.QuickShot,
            SkillVariantCatalog.QuickFanId, SkillVariantCatalog.TownCenter, solo: true));
        skills.ReduceCooldown(DiabloSkills.Skill.QuickShot, 10f);
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.QuickShot, Vector3.forward));
        Assert.AreEqual(1 + SkillVariantCatalog.FanBolts, DiabloProjectile.Launched);
        int single = skills.EffectiveDamage(DiabloSkills.Skill.QuickShot);
        Assert.Less(SkillVariantCatalog.FanBoltDamage(single), single);
        StringAssert.Contains(SkillVariantCatalog.FanBolts.ToString(),
            SkillVariantCatalog.Describe(ClassCatalog.RogueId, DiabloSkills.Skill.QuickShot, SkillVariantCatalog.QuickFanId, skills));
    }

    // ------------------------------------------------------------ uniques and saves

    [Test]
    public void ComboUniques_AreKnownClassBoundAndDescribed()
    {
        Assert.IsTrue(ItemDatabase.IsKnownUniqueId(ItemDatabase.MoorvogtAnchorId));
        Assert.IsTrue(ItemDatabase.IsKnownUniqueId(ItemDatabase.SmugglersFuseId));
        Assert.AreEqual(ClassCatalog.FenwalkerId,
            ItemDatabase.ClassRestriction(ItemDatabase.CreateUnique(ItemDatabase.MoorvogtAnchorId).baseType));
        Assert.AreEqual(ClassCatalog.RogueId,
            ItemDatabase.ClassRestriction(ItemDatabase.CreateUnique(ItemDatabase.SmugglersFuseId).baseType));
        foreach (var id in new[]
        {
            ItemDatabase.LancerWardId, ItemDatabase.BulwarkCarrierId, ItemDatabase.SeismicCrownId,
            ItemDatabase.NightfangId, ItemDatabase.ShadowstrideVestId, ItemDatabase.KeenCadenceId,
            ItemDatabase.MoorvogtAnchorId, ItemDatabase.SmugglersFuseId,
        })
            Assert.IsFalse(BuildUniqueEffects.EffectText(id).StartsWith("unique.effect"), id);
    }

    [Test]
    public void BuildWeaponPool_DropsBothBuildWeaponsPerClass()
    {
        var fen = new System.Collections.Generic.HashSet<string>();
        var rogue = new System.Collections.Generic.HashSet<string>();
        var rng = new System.Random(7);
        for (int i = 0; i < 400; i++)
        {
            fen.Add(ItemDatabase.UniqueWeaponForClass(ClassCatalog.FenwalkerId, rng).uniqueId);
            rogue.Add(ItemDatabase.UniqueWeaponForClass(ClassCatalog.RogueId, rng).uniqueId);
        }
        CollectionAssert.IsSupersetOf(fen, new[] { ItemDatabase.LancerWardId, ItemDatabase.MoorvogtAnchorId });
        CollectionAssert.IsSupersetOf(rogue, new[] { ItemDatabase.NightfangId, ItemDatabase.SmugglersFuseId });
    }

    [Test]
    public void OldSave_RefundsRanksOutsideTheKit()
    {
        var sheet = new CharacterSheet { fireballRank = 2, arrowRank = 1, slamRank = 3, cleaveRank = 1, freeSkillPoints = 0 };
        Assert.AreEqual(3, sheet.RefundForeignRanks(ClassCatalog.Fenwalker));
        Assert.AreEqual(3, sheet.freeSkillPoints);
        Assert.AreEqual(0, sheet.fireballRank);
        Assert.AreEqual(3, sheet.slamRank, "kit ranks stay");
        Assert.AreEqual(1, sheet.cleaveRank);
        Assert.AreEqual(0, sheet.RefundForeignRanks(ClassCatalog.Fenwalker), "idempotent");
    }

    [Test]
    public void KitRanks_RoundTripThroughTheSave()
    {
        var sheet = new CharacterSheet { freeSkillPoints = 2 };
        Assert.IsTrue(sheet.SpendSkill(DiabloSkills.Skill.PowderKeg));
        Assert.IsTrue(sheet.SpendSkill(DiabloSkills.Skill.PowderKeg));
        var copy = new CharacterSheet();
        copy.FromEntries(sheet.ToEntries());
        Assert.AreEqual(2, copy.GetRank(DiabloSkills.Skill.PowderKeg));
        Assert.AreEqual(0, copy.GetRank(DiabloSkills.Skill.MireHook));
    }

    [Test]
    public void CoopIntent_AcceptsKitSkillsForTheirClassOnly()
    {
        Assert.IsNull(DiabloNetworkPlayer.ValidateSkillIntent((int)DiabloSkills.Skill.PowderKeg,
            ClassCatalog.Rogue, Vector3.zero, Vector3.zero, Vector3.forward, 0f));
        Assert.IsNull(DiabloNetworkPlayer.ValidateSkillIntent((int)DiabloSkills.Skill.MireHook,
            ClassCatalog.Fenwalker, Vector3.zero, Vector3.zero, Vector3.forward, 0f));
        Assert.IsNotNull(DiabloNetworkPlayer.ValidateSkillIntent((int)DiabloSkills.Skill.PowderKeg,
            ClassCatalog.Fenwalker, Vector3.zero, Vector3.zero, Vector3.forward, 0f));
        Assert.IsNotNull(DiabloNetworkPlayer.ValidateSkillIntent(20,
            ClassCatalog.Rogue, Vector3.zero, Vector3.zero, Vector3.forward, 0f));
    }
}
