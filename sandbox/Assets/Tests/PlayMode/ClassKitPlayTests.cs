using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// cinderreach-class-kits-parity, real frames: Quick Shot ignites a powder
/// keg in flight (Spark Shot), nightfang leaves venom, venom ticks over time,
/// a keg fuse runs out, mired husks walk slower and blinded husks whiff.
/// </summary>
public class ClassKitPlayTests
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
        foreach (var k in PowderKeg.Active.ToArray()) if (k != null) Object.DestroyImmediate(k.gameObject);
        foreach (var c in SmokeCloud.Active.ToArray()) if (c != null) Object.DestroyImmediate(c.gameObject);
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
        return h;
    }

    [UnityTest]
    public IEnumerator SparkShot_QuickShotIgnitesTheKegInFlight()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        yield return null;
        var husk = Husk("Target", new Vector3(0f, 1.05f, 9f));
        var keg = PowderKeg.Place(hero.transform, new Vector3(0f, 0.05f, 5f), 9, 3.5f, 30f);
        var nearKeg = Husk("NearKeg", new Vector3(1.5f, 0.05f, 5f));
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.QuickShot, Vector3.forward));
        yield return DiabloTestFixture.WaitSeconds(0.6f);
        Assert.IsTrue(keg == null || keg.exploded, "the shot set the keg off");
        Assert.AreEqual(1, ClassCombos.Triggered(ClassCombos.SparkShot));
        Assert.AreEqual(ClassCombos.KegDamageOn(9, 0, ignited: true), nearKeg.maxHP - nearKeg.CurrentHP);
        Assert.Less(husk.HPFraction(), 1f, "the bolt flies on and still hits");
    }

    [UnityTest]
    public IEnumerator Nightfang_QuickShotLeavesVenomThatTicks()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        var eq = hero.AddComponent<Equipment>();
        eq.weapon = ItemDatabase.CreateUnique(ItemDatabase.NightfangId);
        yield return null;
        var husk = Husk("Target", new Vector3(0f, 1.05f, 5f));
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.QuickShot, Vector3.forward));
        yield return DiabloTestFixture.WaitSeconds(0.5f);
        Assert.AreEqual(BuildUniqueEffects.NightfangVenomStacks, EnemyStatus.Venom(husk.gameObject));
        int afterShot = husk.CurrentHP;
        yield return DiabloTestFixture.WaitSeconds(ClassCombos.VenomTick + 0.2f);
        Assert.Less(husk.CurrentHP, afterShot, "venom ticks");
    }

    [UnityTest]
    public IEnumerator PowderKeg_FuseRunsOut()
    {
        RunProfile.ClassId = ClassCatalog.RogueId;
        yield return null;
        var husk = Husk("Target", new Vector3(0f, 0.05f, 4f));
        Assert.IsTrue(skills.CastToward(DiabloSkills.Skill.PowderKeg, Vector3.forward));
        Assert.AreEqual(1, PowderKeg.Active.Count);
        Assert.AreEqual(1f, husk.HPFraction(), "nothing before the fuse");
        yield return DiabloTestFixture.WaitSeconds(ClassCombos.PowderKegFuse + 0.3f);
        Assert.AreEqual(0, PowderKeg.Active.Count);
        Assert.AreEqual(skills.EffectiveDamage(DiabloSkills.Skill.PowderKeg), husk.maxHP - husk.CurrentHP);
    }

    [UnityTest]
    public IEnumerator BlindedHusk_WhiffsItsSwing()
    {
        yield return null;
        var husk = Husk("Striker", new Vector3(0f, 0.05f, 1.5f));
        husk.gameObject.AddComponent<EnemyAttack>();
        var hp = hero.GetComponent<PlayerHealth>();
        float full = hp.CurrentHP;
        var cloud = SmokeCloud.Spawn(hero.transform, husk.transform.position, 3f, 5f);
        yield return DiabloTestFixture.WaitSeconds(EnemyAttack.Windup + EnemyAttack.Cooldown + 0.5f);
        Assert.IsTrue(EnemyStatus.IsBlinded(husk.gameObject));
        Assert.AreEqual(full, hp.CurrentHP, 0.0001f, "blinded swings miss");
        cloud.Kill();
        yield return DiabloTestFixture.WaitSeconds(EnemyAttack.Windup + EnemyAttack.Cooldown + 1.0f);
        Assert.Less(hp.CurrentHP, full, "out of the smoke it hits again");
    }

    [UnityTest]
    public IEnumerator MiredHusk_WalksAtHalfSpeed()
    {
        yield return null;
        var dry = Husk("Dry", new Vector3(-3f, 0.05f, 6f));
        var mired = Husk("Mired", new Vector3(3f, 0.05f, 6f));
        dry.gameObject.AddComponent<EnemyAttack>();
        mired.gameObject.AddComponent<EnemyAttack>();
        var dryStart = dry.transform.position;
        var miredStart = mired.transform.position;
        float until = Time.time + 1.0f;
        while (Time.time < until)
        {
            EnemyStatus.Mire(mired.gameObject, 0.5f);
            yield return null;
        }
        float dryMoved = (dry.transform.position - dryStart).magnitude;
        float miredMoved = (mired.transform.position - miredStart).magnitude;
        Assert.Greater(dryMoved, 0.5f, "dry husk approaches");
        Assert.AreEqual(dryMoved * ClassCombos.MiredMoveFactor, miredMoved, dryMoved * 0.2f);
    }
}
