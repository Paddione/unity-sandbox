using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>cinderreach-party-roles S3: rogue shots/flurry hit, heals mend allies and book threat.</summary>
public class PartySkillTests
{
    GameObject hero;
    DiabloSkills skills;

    [SetUp]
    public void Setup()
    {
        RunProfile.Reset();
        EnemyHealth.ResetRun();
        DiabloProjectile.ResetRun();
        hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        hero.name = "Player";
        Object.DestroyImmediate(hero.GetComponent<Collider>());
        hero.transform.position = new Vector3(0f, 0.05f, 0f);
        hero.AddComponent<DiabloProgression>();
        hero.AddComponent<PlayerHealth>();
        skills = hero.AddComponent<DiabloSkills>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var go in GameObject.FindGameObjectsWithTag("Husk")) Object.DestroyImmediate(go);
        GameObject fx;
        while ((fx = GameObject.Find("MendingLight")) != null) Object.DestroyImmediate(fx);
        Object.DestroyImmediate(hero);
        EnemyHealth.ResetRun();
        DiabloProjectile.ResetRun();
        RunProfile.Reset();
    }

    static GameObject Ally(string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.AddComponent<PlayerHealth>();
        return go;
    }

    static EnemyHealth Husk(string name, Vector3 pos, int hp = 30)
    {
        var go = new GameObject(name);
        go.tag = "Husk";
        go.transform.position = pos;
        var h = go.AddComponent<EnemyHealth>();
        h.SetTier(hp, LootTier.Normal);
        return h;
    }

    [UnityTest]
    public IEnumerator QuickShot_FliesAndHitsFirstHuskOnly()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        yield return null;
        var near = Husk("Near", new Vector3(0f, 1.05f, 6f), 10);
        var behind = Husk("Behind", new Vector3(0f, 1.05f, 9f), 10);
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.QuickShot, Vector3.forward));
        Assert.AreEqual(1, DiabloProjectile.Launched);
        Assert.IsTrue(skills.BowDrawn, "rogue draws the bow for the shot");
        yield return DiabloTestFixture.WaitSeconds(1.0f);
        Assert.AreEqual(1, DiabloProjectile.Impacts);
        Assert.Less(near.HPFraction(), 1f, "quick shot damages the first husk");
        Assert.AreEqual(1f, behind.HPFraction(), "quick shot does not pierce");
    }

    [UnityTest]
    public IEnumerator Flurry_HitsFrontalConeOnly()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        yield return null;
        var front = Husk("Front", new Vector3(0f, 0.05f, 2f), 10);
        var side = Husk("Side", new Vector3(2f, 0.05f, 0.5f), 10);
        var far = Husk("Far", new Vector3(0f, 0.05f, 5f), 10);
        int hits = skills.DoFlurry(Vector3.forward, cosmetic: false);
        Assert.AreEqual(1, hits);
        Assert.Less(front.HPFraction(), 1f, "flurry hits the frontal husk");
        Assert.AreEqual(1f, side.HPFraction(), "flurry ignores husks outside the cone");
        Assert.AreEqual(1f, far.HPFraction(), "flurry ignores husks outside the radius");
        yield return null;
    }

    [UnityTest]
    public IEnumerator TargetHeal_MendsNearestInjuredAllyOnly()
    {
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        yield return null;
        var ally = Ally("Ally", new Vector3(0f, 0.05f, 5f));
        var allyHp = ally.GetComponent<PlayerHealth>();
        var heroHp = hero.GetComponent<PlayerHealth>();
        allyHp.TakeDamage(10f);
        heroHp.TakeDamage(5f);
        float restored = skills.DoTargetHeal(cosmetic: false);
        Assert.AreEqual(10f, restored, 0.0001f, "12 heal on 10 missing HP restores 10");
        Assert.AreEqual(allyHp.MaxHP, allyHp.CurrentHP, 0.0001f, "ally is whole again");
        Assert.AreEqual(15f, heroHp.CurrentHP, 0.0001f, "target heal never mends the caster");
        Object.DestroyImmediate(ally);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TargetHeal_FindsHurtMirror_ReportsGainWithoutLocalChange()
    {
        // coop-hp-sync: another player's hero shows its owner's HP; the healer
        // sees the hurt mirror, and the predicted gain is what the relay sends.
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        yield return null;
        var ally = Ally("Mirror", new Vector3(0f, 0.05f, 5f));
        var allyHp = ally.GetComponent<PlayerHealth>();
        allyHp.SetMirrored(8f, 20f);
        allyHp.TakeDamage(5f);
        Assert.AreEqual(8f, allyHp.CurrentHP, 0.0001f, "local hits never touch a mirror");
        Assert.AreSame(allyHp, skills.FindHealTarget(skills.targetHealRange));
        float restored = skills.DoTargetHeal(cosmetic: false);
        Assert.AreEqual(12f, restored, 0.0001f, "12 heal on 12 missing HP");
        Assert.AreEqual(8f, allyHp.CurrentHP, 0.0001f, "the owner applies it, the mirror waits for the sync");
        allyHp.SetMirrored(20f, 20f);
        Assert.IsNull(skills.FindHealTarget(skills.targetHealRange), "whole again after the owner's update");
        Object.DestroyImmediate(ally);
        yield return null;
    }

    [UnityTest]
    public IEnumerator TargetHeal_BooksThreatOnEngagedEnemies()
    {
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        yield return null;
        var husk = Husk("ThreatHusk", new Vector3(0f, 0.05f, 4f), 30);
        var ally = Ally("Ally", new Vector3(0f, 0.05f, 3f));
        var allyHp = ally.GetComponent<PlayerHealth>();
        DiabloSkills.ApplyHit(husk.gameObject, 2, hero.transform.position, 0f, hero.transform);
        float huskHp = husk.HPFraction();
        allyHp.TakeDamage(10f);
        skills.DoTargetHeal(cosmetic: false);
        var table = husk.GetComponent<ThreatTable>();
        Assert.IsNotNull(table);
        Assert.AreEqual(2f + 10f, table.ThreatOf(hero.transform), 0.0001f, "damage plus restored HP");
        Assert.AreEqual(huskHp, husk.HPFraction(), 0.0001f, "healing never touches enemies");
        Object.DestroyImmediate(ally);
        yield return null;
    }

    [UnityTest]
    public IEnumerator GroupHeal_MendsPartyInRadiusAndSkipsDead()
    {
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        yield return null;
        var heroHp = hero.GetComponent<PlayerHealth>();
        var near = Ally("Near", new Vector3(0f, 0.05f, 3f));
        var nearHp = near.GetComponent<PlayerHealth>();
        var full = Ally("Full", new Vector3(0f, 0.05f, 4f));
        var fullHp = full.GetComponent<PlayerHealth>();
        var far = Ally("Far", new Vector3(0f, 0.05f, 10f));
        var farHp = far.GetComponent<PlayerHealth>();
        var dead = Ally("Dead", new Vector3(0f, 0.05f, 2f));
        var deadHp = dead.GetComponent<PlayerHealth>();
        heroHp.TakeDamage(5f);
        nearHp.TakeDamage(10f);
        farHp.TakeDamage(6f);
        deadHp.TakeDamage(999f);
        int amount = skills.EffectiveHeal(DiabloSkills.Skill.RenewingCircle);
        float restored = skills.DoGroupHeal(cosmetic: false);
        float expected = Mathf.Min(5f, amount) + Mathf.Min(10f, amount);
        Assert.AreEqual(expected, restored, 0.0001f);
        Assert.AreEqual(heroHp.MaxHP, heroHp.CurrentHP, 0.0001f, "the circle includes the caster");
        Assert.AreEqual(10f + Mathf.Min(10f, amount), nearHp.CurrentHP, 0.0001f);
        Assert.AreEqual(fullHp.MaxHP, fullHp.CurrentHP, 0.0001f, "the whole stay whole");
        Assert.AreEqual(farHp.MaxHP - 6f, farHp.CurrentHP, 0.0001f, "out of radius, untouched");
        Assert.AreEqual(0f, deadHp.CurrentHP, "the dead are not mended");
        foreach (var a in new[] { near, full, far, dead }) Object.DestroyImmediate(a);
        yield return null;
    }
}
