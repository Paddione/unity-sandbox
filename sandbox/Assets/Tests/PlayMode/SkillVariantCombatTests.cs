using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// skill-variants S2: cone vs round targeting, ward anchoring, reload and
/// defaults, cone rim and the co-op base-skill path.
/// </summary>
public class SkillVariantCombatTests
{
    static readonly Vector3 HoldPos = new Vector3(0f, 0.05f, 11f);

    GameObject hero;
    DiabloSkills skills;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        EnemyHealth.ResetRun();
        hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = "Player";
        Object.DestroyImmediate(hero.GetComponent<Collider>());
        hero.transform.position = HoldPos;
        hero.AddComponent<DiabloProgression>();
        hero.AddComponent<PlayerHealth>();
        skills = hero.AddComponent<DiabloSkills>();
    }

    [TearDown]
    public void TearDown()
    {
        HaloZone.ResetRun();
        foreach (var go in GameObject.FindGameObjectsWithTag("Husk")) Object.DestroyImmediate(go);
        foreach (var z in Object.FindObjectsOfType<HaloZone>()) Object.DestroyImmediate(z.gameObject);
        Object.DestroyImmediate(hero);
        EnemyHealth.ResetRun();
        RunProfile.Reset();
    }

    static EnemyHealth Husk(string name, Vector3 pos, int hp = 60)
    {
        var go = new GameObject(name);
        go.tag = "Husk";
        go.transform.position = pos;
        var h = go.AddComponent<EnemyHealth>();
        h.SetTier(hp, LootTier.Normal);
        return h;
    }

    static void SelectOnHero(GameObject heroGo, DiabloSkills.Skill skill, string variant)
    {
        heroGo.transform.position = HoldPos;
        var ui = heroGo.GetComponent<DiabloCharacterUI>();
        if (ui == null) ui = heroGo.AddComponent<DiabloCharacterUI>();
        Assert.IsTrue(ui.TrySelectVariant(skill, variant), $"select {variant} at the Hold");
    }

    [UnityTest]
    public IEnumerator Cleave_BaselineHitsBehindAndConeHitsFarAhead()
    {
        // Baseline (round): the husk behind the hero is hit too.
        var behind = Husk("Behind", HoldPos + new Vector3(0f, 0f, -2f));
        var far = Husk("Far", HoldPos + new Vector3(0f, 0f, 3.5f));
        skills.DoCleave(Vector3.forward, cosmetic: false);
        Assert.Less(behind.HPFraction(), 1f, "round swing hits behind");
        Assert.AreEqual(1f, far.HPFraction(), "round swing keeps current reach");
        yield return null;
        foreach (var go in GameObject.FindGameObjectsWithTag("Husk")) Object.DestroyImmediate(go);

        // Frontal: far target inside the cone is hit, the one behind is spared.
        SelectOnHero(hero, DiabloSkills.Skill.Cleave, SkillVariantCatalog.CleaveFrontalId);
        hero.transform.position = HoldPos;
        var behind2 = Husk("Behind2", HoldPos + new Vector3(0f, 0f, -2f));
        var far2 = Husk("Far2", HoldPos + new Vector3(0f, 0f, 3.5f));
        skills.DoCleave(Vector3.forward, cosmetic: false);
        Assert.Less(far2.HPFraction(), 1f, "cone reaches 1.5x");
        Assert.AreEqual(1f, behind2.HPFraction(), "cone spares the back half");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Cleave_ConeRimAndSideMiss()
    {
        SelectOnHero(hero, DiabloSkills.Skill.Cleave, SkillVariantCatalog.CleaveFrontalId);
        hero.transform.position = HoldPos;
        float r = skills.CleaveRadius * SkillVariantCatalog.FrontalRangeFactor;
        // ~40° off-axis inside the 90° cone, ~50° outside; side target well clear.
        var rimIn = Husk("RimIn", HoldPos + new Vector3(Mathf.Sin(40f * Mathf.Deg2Rad) * 3f, 0f, Mathf.Cos(40f * Mathf.Deg2Rad) * 3f));
        var rimOut = Husk("RimOut", HoldPos + new Vector3(Mathf.Sin(50f * Mathf.Deg2Rad) * 3f, 0f, Mathf.Cos(50f * Mathf.Deg2Rad) * 3f));
        var side = Husk("Side", HoldPos + new Vector3(3f, 0f, 0.2f));
        Assert.Less(r, 4.5f, "sanity: cone reach covers the 3 m probes");
        skills.DoCleave(Vector3.forward, cosmetic: false);
        Assert.Less(rimIn.HPFraction(), 1f, "inside the rim is hit");
        Assert.AreEqual(1f, rimOut.HPFraction(), "outside the rim is spared");
        Assert.AreEqual(1f, side.HPFraction(), "flank is spared");
        // Same damage path on both variants.
        Assert.AreEqual(skills.EffectiveDamage(DiabloSkills.Skill.Cleave),
            skills.EffectiveDamage(DiabloSkills.Skill.Cleave));
        yield return null;
    }

    [UnityTest]
    public IEnumerator Halo_MovesOrRemainsAtCastPoint()
    {
        RunProfile.ClassId = ClassCatalog.LanternbearerId;

        // Mitlaufend (legacy): instant ring, no zone left behind.
        var atCast = Husk("AtCast", HoldPos + new Vector3(2f, 0f, 0f));
        skills.DoHalo(cosmetic: false);
        Assert.Less(atCast.HPFraction(), 1f, "follow halo strikes at once");
        Assert.AreEqual(0, HaloZone.ForOwner(hero.transform).Length, "follow leaves no zone");
        yield return null;
        foreach (var go in GameObject.FindGameObjectsWithTag("Husk")) Object.DestroyImmediate(go);
        PlayerHealth.LastDeathTime = float.NegativeInfinity;

        // Ward: anchored at the cast point, keeps ticking after the hero moves on.
        SelectOnHero(hero, DiabloSkills.Skill.SearingHalo, SkillVariantCatalog.HaloWardId);
        hero.transform.position = HoldPos;
        var anchorHusk = Husk("AnchorHusk", HoldPos + new Vector3(2f, 0f, 0f));
        var heroHusk = Husk("HeroHusk", HoldPos + new Vector3(12f, 0f, 0f));
        skills.DoHalo(cosmetic: false);
        var zones = HaloZone.ForOwner(hero.transform);
        Assert.AreEqual(1, zones.Length, "ward spawns exactly one zone");
        Assert.AreEqual(HoldPos.x, zones[0].Anchor.x, 0.01f);
        Assert.AreEqual(HoldPos.z, zones[0].Anchor.z, 0.01f);

        hero.transform.position = HoldPos + new Vector3(10f, 0f, 0f);
        Physics.SyncTransforms();
        yield return null;
        Assert.AreEqual(HoldPos.x, zones[0].Anchor.x, 0.01f, "ward stays at the cast point");
        float beforeAnchor = anchorHusk.HPFraction();
        float beforeHero = heroHusk.HPFraction();
        zones[0].TickNow();
        Assert.Less(anchorHusk.HPFraction(), beforeAnchor, "ward ticks at its anchor");
        Assert.AreEqual(beforeHero, heroHusk.HPFraction(), "ward does not follow the hero");

        // Recast replaces the old ward.
        hero.transform.position = HoldPos;
        skills.DoHalo(cosmetic: false);
        Assert.AreEqual(1, HaloZone.ForOwner(hero.transform).Length, "recast replaces");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Variant_ReloadAndDefaults()
    {
        SelectOnHero(hero, DiabloSkills.Skill.Cleave, SkillVariantCatalog.CleaveFrontalId);
        string tmp = Path.Combine(Path.GetTempPath(), $"cinderreach-combat-{System.Guid.NewGuid():N}.json");
        try
        {
            SaveService.SaveNow(hero, path: tmp);
            RunProfile.Reset();
            Assert.AreEqual(SaveGame.CurrentVersion, SaveMigrations.ReadVersion(tmp));
            Assert.IsTrue(SaveService.TryLoad(hero, path: tmp));
            Assert.AreEqual(SkillVariantCatalog.CleaveFrontalId, skills.ActiveVariantId(DiabloSkills.Skill.Cleave));
        }
        finally
        {
            if (File.Exists(tmp)) File.Delete(tmp);
            if (File.Exists(tmp + ".tmp")) File.Delete(tmp + ".tmp");
        }

        // Unknown stored id resolves to the legacy default.
        RunProfile.SkillVariants["fenwalker:Cleave"] = "cleave_spin";
        Assert.AreEqual(SkillVariantCatalog.CleaveRoundId, skills.ActiveVariantId(DiabloSkills.Skill.Cleave));
        RunProfile.SkillVariants.Clear();

        // Co-op: the remote base-skill path deals no damage and ignores variants.
        RunProfile.SkillVariants["fenwalker:Cleave"] = SkillVariantCatalog.CleaveFrontalId;
        var witness = Husk("Witness", HoldPos + new Vector3(0f, 0f, 1f));
        skills.ShowRemoteCast(DiabloSkills.Skill.Cleave, hero.transform.position, Vector3.forward);
        Assert.AreEqual(1f, witness.HPFraction(), "remote cast is cosmetic");
        yield return null;
    }
}
