using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>D2 combat-feel data contracts: audio clips, tuning constants,
/// knockback math, HP fractions, zone wiring.</summary>
public class DiabloD2FeelTests
{
    const string ZonePath = "Assets/Scenes/DiabloZone.unity";

    [TearDown]
    public void TearDown()
    {
        // Reset only: the synth GO persists for the session, destroying it
        // would orphan the static AudioSource.
        DiabloAudio.ResetRun();
        DiabloMelee.ResetRun();
        EnemyHealth.ResetRun();
    }

    [Test]
    public void AudioClips_AreValidMono22050()
    {
        var clips = new[]
        {
            DiabloAudio.SwingClip, DiabloAudio.HitClip, DiabloAudio.DeathClip,
            DiabloAudio.LootClip, DiabloAudio.UIClickClip, DiabloAudio.HurtClip,
            DiabloAudio.PlayerDeathClip,
        };
        foreach (var clip in clips)
        {
            Assert.IsNotNull(clip, "Every combat clip must be synthesized.");
            Assert.Greater(clip.samples, 0, $"Clip {clip.name} must hold samples.");
            Assert.AreEqual(22050, clip.frequency, $"Clip {clip.name} must be 22050 Hz.");
            Assert.AreEqual(1, clip.channels, $"Clip {clip.name} must be mono.");
        }
        DiabloAudio.ResetRun();
        Assert.AreEqual(0, DiabloAudio.soundedSwings);
        Assert.AreEqual(0, DiabloAudio.soundedHits);
        Assert.AreEqual(0, DiabloAudio.soundedDeaths);
    }

    [Test]
    public void TuningConstants_MatchPlan()
    {
        Assert.AreEqual(0.6f, DiabloMelee.KnockbackNormal);
        Assert.AreEqual(0.2f, DiabloMelee.KnockbackElite);
        Assert.AreEqual(0.5f, DiabloClickController.StrikeStep, "Legacy step constant stays; strikes no longer move.");
        Assert.AreEqual(60f, DiabloMelee.ArcHalfAngle, "LMB cone is 120°.");
        Assert.AreEqual(3.2f, DiabloMelee.OutreachRange, "RMB outranges husk melee (2.0 m).");
        Assert.AreEqual(25f, DiabloMelee.OutreachHalfAngle, "Thrust cone is narrow.");
        Assert.AreEqual(1.2f, DiabloMelee.OutreachCooldown, "Thrust is slower than the swing.");
        Assert.AreEqual(60f, DiabloBlock.BlockHalfAngle, "Block covers the frontal arc.");
        Assert.AreEqual(0.5f, DiabloBlock.BlockMultiplier, "Block halves frontal damage.");
        Assert.AreEqual(2.0f, EnemyAttack.Range);
        Assert.AreEqual(0.4f, EnemyAttack.Windup);
        Assert.AreEqual(1.5f, EnemyAttack.Cooldown);
        Assert.AreEqual(8f, EnemyAttack.AggroRange);
        Assert.AreEqual(1.5f, EnemyAttack.ApproachSpeed);
        Assert.AreEqual(1.8f, EnemyAttack.StopDistance);
        Assert.AreEqual(12f, DiabloTargetBar.MaxRange);
        Assert.AreEqual(3f, DiabloTargetBar.FadeSeconds);
    }

    [Test]
    public void Knockback_DisplacesByTier()
    {
        var go = new GameObject("TestHusk");
        go.transform.position = new Vector3(0f, 0f, 2f);
        var hp = go.AddComponent<EnemyHealth>();
        Vector3 before = go.transform.position;
        hp.ApplyKnockback(new Vector3(0f, 0f, 0f), DiabloMelee.KnockbackNormal);
        Assert.AreEqual(0.6f, Vector3.Distance(before, go.transform.position), 0.01f,
            "Normal husk must pop 0.6 m.");
        before = go.transform.position;
        hp.SetTier(12, LootTier.Elite);
        hp.ApplyKnockback(new Vector3(0f, 0f, 0f), DiabloMelee.KnockbackElite);
        Assert.AreEqual(0.2f, Vector3.Distance(before, go.transform.position), 0.01f,
            "Elite must resist (0.2 m).");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void HPFraction_Clamps()
    {
        var go = new GameObject("TestHusk");
        var hp = go.AddComponent<EnemyHealth>();
        hp.SetTier(3, LootTier.Normal); // explicit init (Awake is play-mode only)
        Assert.AreEqual(1f, hp.HPFraction(), 0.001f);
        hp.TakeDamage(1);
        Assert.AreEqual(2f / 3f, hp.HPFraction(), 0.001f);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void AshenFenScene_ArmsHusksAndBar()
    {
        var scene = EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        Assert.IsTrue(scene.IsValid(), "DiabloZone.unity must open cleanly.");

        var husks = GameObject.FindGameObjectsWithTag("Husk");
        Assert.GreaterOrEqual(husks.Length, 10, "Ashen Fen must spread 10+ husks.");
        foreach (var husk in husks)
        {
            // act-one S3: Ash Lancers are armed with their own charge instead of melee.
            if (husk.GetComponent<AshLancer>() != null) continue;
            var atk = husk.GetComponent<EnemyAttack>();
            Assert.IsNotNull(atk, $"Husk '{husk.name}' needs EnemyAttack.");
            var tier = husk.GetComponent<EnemyHealth>().tier;
            bool boss = tier == LootTier.Boss || tier == LootTier.ActBoss;
            int expectedDamage = tier == LootTier.Normal ? 1 : (boss ? 3 : 2);
            Assert.AreEqual(expectedDamage, atk.damage,
                $"Husk '{husk.name}' damage must follow its tier.");
        }

        var bar = GameObject.Find("DTargetBar");
        Assert.IsNotNull(bar, "Zone needs the D2 target bar.");
        Assert.IsNotNull(bar.GetComponent<DiabloTargetBar>());

        var sword = GameObject.Find("StarterSword");
        Assert.IsNotNull(sword, "Starter sword prop must survive the rebuild.");
    }
}
