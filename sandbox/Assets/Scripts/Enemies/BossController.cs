using UnityEngine;

/// <summary>
/// The Hollow Abbot (S5 boss, ~150 HP for a level 5–8 hero with S4 points).
/// Phase 1: melee combo (reuse <see cref="EnemyAttack"/>) + ground-circle
/// telegraph slam. Phase 2 (at/below 50 % HP): summons 3 husks once and
/// adds a projectile fan to the slam. Death is owned by
/// <see cref="EnemyHealth"/> (Boss tier: guaranteed Rare+, 150–250 gold);
/// this controller polls the fraction each frame and — on the same frame,
/// before the deferred destroy — records the kill and opens
/// <see cref="SessionComplete"/>. Summons spawn host-authoritatively
/// (solo/host only, like loot).
/// </summary>
public sealed class BossController : MonoBehaviour
{
    public const string BossName = "Hollow Abbot";
    public const int BossMaxHP = 150;
    public const float PhaseTwoFraction = 0.5f;
    public const float SlamRadius = 3f;
    public const float SlamWindup = 0.8f;
    public const int SlamDamage = 3;

    /// <summary>Boss kills this run (test seam).</summary>
    public static int BossesKilled;
    /// <summary>Phase-2 entries this run (test seam).</summary>
    public static int PhaseTwoEntries;
    /// <summary>Rarity line shown on the session screen (test seam).</summary>
    public static string LastBossDrop = "";

    bool phaseTwo;
    bool dead;
    float slamStart = -1f;

    void Update()
    {
        if (dead) return;
        var hp = GetComponent<EnemyHealth>();
        if (hp == null) return;
        if (!phaseTwo && hp.HPFraction() <= PhaseTwoFraction)
            EnterPhaseTwo();
        if (hp.HPFraction() <= 0f)
        {
            var hero = AcquireHero();
            OnBossDeath(hero != null ? hero.gameObject : null);
            return;
        }
        if (slamStart >= 0f && Time.time - slamStart >= SlamWindup)
        {
            slamStart = -1f;
            ResolveSlam();
        }
        else if (slamStart < 0f)
        {
            var hero = AcquireHero();
            if (hero != null && Planar(hero) <= SlamRadius + 0.5f)
                slamStart = Time.time; // ground-circle telegraph starts
        }
    }

    /// <summary>Phase-2 trigger (headless-callable): summons 3 husks once.</summary>
    public void EnterPhaseTwo()
    {
        if (phaseTwo) return;
        phaseTwo = true;
        PhaseTwoEntries++;
        Debug.Log($"[Boss] {BossName} enters phase 2 (adds={PhaseTwoEntries}).");
        if (!Diablo.IsLocalAuthority() && !Diablo.IsSolo()) return;
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.PI * 2f / 3f;
            var pos = transform.position + new Vector3(Mathf.Cos(a) * 2.5f, 0.05f, Mathf.Sin(a) * 2.5f);
            SpawnHusk($"Husk_Summon_{i}_{PhaseTwoEntries}", pos);
        }
    }

    void ResolveSlam()
    {
        var hero = AcquireHero();
        if (hero == null) return;
        if (Planar(hero) <= SlamRadius)
        {
            var hp = hero.GetComponent<PlayerHealth>();
            int slam = DifficultyProfile.Current.ScaleDamage(SlamDamage);
            if (hp != null) hp.TakeDamage(slam);
            DiabloAudio.PlayHurt();
            DiabloJuice.OnPlayerHurt(hero.position, slam);
        }
        DiabloJuice.OnBlast(transform.position);
        if (phaseTwo) FireFan(hero);
    }

    void FireFan(Transform hero)
    {
        if (!Diablo.IsLocalAuthority() && !Diablo.IsSolo()) return;
        var from = transform.position + Vector3.up * 1.4f;
        var baseDir = hero.position - from;
        baseDir.y = 0f;
        if (baseDir.sqrMagnitude < 0.0001f) baseDir = Vector3.forward;
        baseDir.Normalize();
        for (int i = -1; i <= 1; i++)
        {
            var dir = Quaternion.Euler(0f, i * 20f, 0f) * baseDir;
            DiabloProjectile.Spawn(DiabloSkills.Skill.Fireball, from, dir, transform,
                RangedCaster.ShotDamage, 11f, 18f, 0f);
        }
    }

    /// <summary>
    /// Boss death bookkeeping: kill counter, drop line, session screen.
    /// Loot/XP itself already ran through <see cref="EnemyHealth"/> with
    /// <see cref="LootTier.Boss"/> (guaranteed Rare+, 150–250 gold).
    /// </summary>
    public void OnBossDeath(GameObject hero)
    {
        if (dead) return;
        dead = true;
        BossesKilled++;
        if (string.IsNullOrEmpty(LastBossDrop)) LastBossDrop = "Rare+ boss cache";
        Debug.Log($"[Boss] {BossName} slain (kills={BossesKilled}, drop={LastBossDrop}).");
        // replayability S4: the first kill unlocks Kindled and persists it.
        bool unlocked = DifficultyProfile.UnlockKindled();
        SessionComplete.KindledUnlockedThisRun |= unlocked;
        // act-one S1: the Abbot opens the Cinder Kiln.
        bool kilnOpened = RunProfile.Act.Unlock(ActProgress.Kiln);
        if (kilnOpened) UnityEngine.Debug.Log("[Act] Cinder Kiln unlocked.");
        if ((unlocked || kilnOpened) && hero != null && Diablo.IsLocalAuthority()) SaveService.SaveNow(hero);
        SessionComplete.Show(hero, LastBossDrop);
    }

    float Planar(Transform hero)
    {
        var d = hero.position - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    /// <summary>Hero object for the session screen (death-dispatch path).</summary>
    public GameObject HeroObject()
    {
        var h = AcquireHero();
        return h != null ? h.gameObject : null;
    }

    Transform AcquireHero() => HeroLocator.Hero(); // cached per frame (act-one perf fix)

    /// <summary>Ash-grey summon colour (URP Lit).</summary>
    public static readonly Color SummonTint = new Color(0.32f, 0.29f, 0.27f);

    static void SpawnHusk(string name, Vector3 pos)
    {
        var husk = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        husk.name = name;
        // The primitive's default material has no URP shader and renders magenta in
        // the player (seen in shotcheck 21_abbot_phase2): give it a URP Lit ash tint.
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var rend = husk.GetComponent<Renderer>();
        if (shader != null && rend != null)
            rend.sharedMaterial = new Material(shader) { color = SummonTint };
        husk.tag = "Husk";
        husk.transform.position = pos;
        husk.AddComponent<EnemyHealth>().SetTier(5, LootTier.Normal);
        husk.AddComponent<EnemyAttack>().damage = 1;
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        BossesKilled = 0;
        PhaseTwoEntries = 0;
        LastBossDrop = "";
    }
}
