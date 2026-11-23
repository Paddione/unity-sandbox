using UnityEngine;
using Unity.Netcode;

/// <summary>
/// The Hollow Abbot (S5 boss, ~150 HP for a level 5–8 hero with S4 points).
/// Phase 1: melee combo (reuse <see cref="EnemyAttack"/>) + ground-circle
/// telegraph slam. Phase 2 (at/below 50 % HP): summons 3 husks once and
/// adds a projectile fan to the slam. Death is owned by
/// <see cref="EnemyHealth"/> (Boss tier: guaranteed Rare+, 150–250 gold);
/// this controller polls the fraction each frame and — on the same frame,
/// before the deferred destroy — records the kill and opens
/// <see cref="SessionComplete"/>. S2 co-op: phase replicates via
/// <see cref="Phase"/> (1/2, server writes), summons spawn via
/// NetworkObject.Spawn on the server, slam/phase telegraphs fan out per
/// ClientRpc so clients see the windup.
/// </summary>
public sealed class BossController : NetworkBehaviour
{
    public const string BossName = "Hollow Abbot";
    public const int BossMaxHP = 150;
    public const float PhaseTwoFraction = 0.5f;
    public const float SlamRadius = 3f;
    public const float SlamWindup = 0.8f;
    public const int SlamDamage = 3;

    /// <summary>Replicated phase (1 or 2, server writes).</summary>
    public readonly NetworkVariable<int> Phase = new NetworkVariable<int>(1,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Boss kills this run (test seam).</summary>
    public static int BossesKilled;
    /// <summary>Phase-2 entries this run (test seam).</summary>
    public static int PhaseTwoEntries;
    /// <summary>Rarity line shown on the session screen (test seam).</summary>
    public static string LastBossDrop = "";

    bool phaseTwo;
    bool dead;
    float slamStart = -1f;

    public override void OnNetworkSpawn()
    {
        if (IsServer) Phase.Value = phaseTwo ? 2 : 1;
        else if (Phase.Value == 2 && !phaseTwo) { phaseTwo = true; }
        Phase.OnValueChanged += OnPhaseChanged;
    }

    public override void OnNetworkDespawn()
    {
        Phase.OnValueChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(int oldV, int newV)
    {
        if (IsServer) return;
        if (newV == 2 && !phaseTwo)
        {
            phaseTwo = true;
            PhaseTwoEntries++;
            Debug.Log($"[Boss] {BossName} mirrors phase 2 (adds={PhaseTwoEntries}).");
        }
    }

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
            {
                slamStart = Time.time; // ground-circle telegraph starts
                PresentationVfx.BossTelegraph(transform.position, SlamRadius);
                if (IsSpawned && IsServer) SlamTelegraphClientRpc();
            }
        }
    }

    /// <summary>Phase-2 trigger (headless-callable): summons 3 husks once.</summary>
    public void EnterPhaseTwo()
    {
        if (phaseTwo) return;
        phaseTwo = true;
        PhaseTwoEntries++;
        Debug.Log($"[Boss] {BossName} enters phase 2 (adds={PhaseTwoEntries}).");
        try { DiabloAudio.PlayBossRoar(); } catch { }
        try { MusicDirector.Ensure().PlayBoss(); } catch { }
        PresentationVfx.PhaseTwoBurst(transform.position);
        if (IsSpawned)
        {
            if (!IsServer) return;
            Phase.Value = 2;
            PhaseTwoClientRpc();
        }
        else if (!Diablo.IsLocalAuthority() && !Diablo.IsSolo()) return;
        // Spawn on the server (replicates to clients when spawned); solo spawns locally.
        var nm = NetworkManager.Singleton;
        bool netServer = nm != null && nm.IsListening && nm.IsServer;
        for (int i = 0; i < 3; i++)
        {
            float a = i * Mathf.PI * 2f / 3f;
            var pos = transform.position + new Vector3(Mathf.Cos(a) * 2.5f, 0.05f, Mathf.Sin(a) * 2.5f);
            SpawnHusk($"Husk_Summon_{i}_{PhaseTwoEntries}", pos, netServer);
        }
    }

    [ClientRpc]
    void PhaseTwoClientRpc()
    {
        if (IsServer) return;
        DiabloJuice.OnBlast(transform.position);
        PresentationVfx.PhaseTwoBurst(transform.position);
    }

    [ClientRpc]
    void SlamTelegraphClientRpc()
    {
        if (IsServer) return;
        DiabloJuice.OnBlast(transform.position);
        PresentationVfx.BossTelegraph(transform.position, SlamRadius);
    }

    void ResolveSlam()
    {
        var hero = AcquireHero();
        if (hero == null) return;
        if (Planar(hero) <= SlamRadius)
        {
            var hp = hero.GetComponent<PlayerHealth>();
            int slam = DifficultyProfile.Current.ScaleDamage(SlamDamage);
            if (hp != null) hp.TakeDamage(slam, transform.position);
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
                RangedCaster.ShotDamage, 11f, 18f, 0f, hostile: true);
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
        if (string.IsNullOrEmpty(LastBossDrop)) LastBossDrop = Loc.T("loot.cache.boss");
        Debug.Log($"[Boss] {BossName} slain (kills={BossesKilled}, drop={LastBossDrop}).");
        // replayability S4: the first kill unlocks Kindled and persists it.
        bool unlocked = DifficultyProfile.UnlockKindled();
        SessionComplete.KindledUnlockedThisRun |= unlocked;
        // act-one S1: the Abbot opens the Cinder Kiln.
        bool kilnOpened = RunProfile.Act.Unlock(ActProgress.Kiln);
        if (kilnOpened) UnityEngine.Debug.Log("[Act] Cinder Kiln unlocked.");
        if ((unlocked || kilnOpened) && hero != null && Diablo.IsLocalAuthority()) SaveService.SaveNow(hero);
        // S5: clients autosave their own hero on the shared boss kill, like solo.
        var relay = DiabloNetworkPlayer.HostRelay();
        if (relay != null) relay.BroadcastBossKill();
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

    /// <summary>S2 co-op targeting: nearest living hero.</summary>
    public Transform AcquireHero() =>
        HitValidator.NearestLivingPlayer(transform.position) ?? HeroLocator.Hero();

    /// <summary>Ash-grey summon colour (URP Lit).</summary>
    public static readonly Color SummonTint = new Color(0.32f, 0.29f, 0.27f);

    static void SpawnHusk(string name, Vector3 pos, bool netServer)
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
        // Server spawn replicates to clients; solo stays local.
        if (netServer)
        {
            husk.AddComponent<NetworkObject>().Spawn();
        }
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        BossesKilled = 0;
        PhaseTwoEntries = 0;
        LastBossDrop = "";
    }
}
