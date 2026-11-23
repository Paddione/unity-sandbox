using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// The Glasswright (act-two-opening S3), zone boss of the Glass Barrens.
/// Phase 1: shard lances, a fan of three telegraphed lines toward the target
/// (step sideways out of the line). Phase 2 below 50 % HP: three Glass
/// Pillars rise once and ward the boss (directed hits halved while any
/// pillar stands), lances come faster, and shard fields land under the
/// target. Kindled phase 2 replaces the fields with the Shatter Nova: a ring
/// warning, then shards cut everyone farther than <see cref="NovaSafeRadius"/>
/// from the boss (close in to survive). Death sets
/// <see cref="ActProgress.ZoneBossDefeated"/>, saves and opens the cleared screen.
/// Co-op: the brain runs on the authority (server/solo); lances, novas and
/// fields fan out per ClientRpc and every peer resolves them against its own
/// hero (per-client hero HP, like the rest of enemy-to-hero damage).
/// </summary>
public sealed class GlasswrightController : NetworkBehaviour, IEnemyDeathHook
{
    public const string BossName = "The Glasswright";
    public const int MaxHP = 420;
    public const float PhaseTwoFraction = 0.5f;
    public const float AggroRange = 11f;
    public const float LanceInterval = 3.5f;
    public const float LanceIntervalPhaseTwo = 2.6f;
    public const float LanceFanDegrees = 25f;
    public const float FieldInterval = 6f;
    public const float NovaInterval = 6.5f;
    public const float NovaSafeRadius = 4f;
    public const float NovaWindup = 1.2f;
    public const int NovaDamage = 4;
    public const int PillarCount = 3;
    public const int PillarHP = 12;
    public const float PillarRadius = 5f;
    public const string PillarPrefix = "Glass Pillar_";

    /// <summary>Replicated phase (1 or 2, server writes) for the boss bar on clients.</summary>
    public readonly NetworkVariable<int> Phase = new NetworkVariable<int>(1,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public static int Kills;
    public static int PhaseTwoEntries;
    public static int LanceVolleys;
    public static int NovasFired;
    public static int FieldsCalled;

    public bool PhaseTwo { get; private set; }
    public bool Engaged { get; private set; }
    bool dead;
    float lastLance = float.NegativeInfinity;
    float lastField = float.NegativeInfinity;
    float lastNova = float.NegativeInfinity;
    readonly List<EnemyHealth> pillars = new List<EnemyHealth>();

    /// <summary>Kindled swaps the phase-2 fields for the Shatter Nova.</summary>
    public static bool NovaActive => DifficultyProfile.Current == DifficultyProfile.Kindled;

    /// <summary>Living pillars (ward check, test seam).</summary>
    public int LivePillars
    {
        get
        {
            int n = 0;
            foreach (var p in pillars) if (p != null && p.IsAlive) n++;
            return n;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) Phase.Value = PhaseTwo ? 2 : 1;
        else if (Phase.Value == 2) PhaseTwo = true;
        Phase.OnValueChanged += OnPhaseChanged;
    }

    public override void OnNetworkDespawn() => Phase.OnValueChanged -= OnPhaseChanged;

    void OnPhaseChanged(int oldV, int newV)
    {
        if (!IsServer && newV == 2) PhaseTwo = true;
    }

    bool Authority => !IsSpawned || IsServer;

    void Update()
    {
        if (dead || !Authority) return;
        var hp = GetComponent<EnemyHealth>();
        if (hp == null) return;
        if (!PhaseTwo && hp.HPFraction() <= PhaseTwoFraction) EnterPhaseTwo();
        var target = AcquireHero();
        if (target == null) return;
        if (BarrensLayout.PlazaSafe(target.position)) return;
        if (Planar(target.position) > AggroRange) return;
        if (!Engaged) Engage();
        Think(Time.time, target.position);
    }

    /// <summary>One step of the boss brain at a known time and target (headless-callable).</summary>
    public void Think(float now, Vector3 target)
    {
        if (now - lastLance >= (PhaseTwo ? LanceIntervalPhaseTwo : LanceInterval)) FireLancesNow(now, target);
        if (!PhaseTwo) return;
        if (NovaActive)
        {
            if (now - lastNova >= NovaInterval) FireNovaNow(now);
        }
        else if (now - lastField >= FieldInterval)
        {
            lastField = now;
            FieldsCalled++;
            ShardField.Spawn(target);
            if (IsSpawned && IsServer) FieldClientRpc(target);
        }
    }

    void Engage()
    {
        Engaged = true;
        try { DiabloAudio.PlayBossRoar(); } catch { }
        try { MusicDirector.Ensure().PlayTrack(MusicDirector.GlassBossTrack); } catch { }
    }

    /// <summary>Fan of three lances from the boss toward <paramref name="target"/>.</summary>
    public void FireLancesNow(float now, Vector3 target)
    {
        lastLance = now;
        LanceVolleys++;
        SpawnLances(transform.position, target);
        if (IsSpawned && IsServer) LanceClientRpc(transform.position, target);
    }

    static void SpawnLances(Vector3 origin, Vector3 target)
    {
        var dir = target - origin;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        dir.Normalize();
        for (int i = -1; i <= 1; i++)
            ShardLance.Spawn(origin, Quaternion.Euler(0f, i * LanceFanDegrees, 0f) * dir);
    }

    public ShatterNova FireNovaNow(float now)
    {
        lastNova = now;
        NovasFired++;
        if (IsSpawned && IsServer) NovaClientRpc(transform.position);
        return ShatterNova.Spawn(transform.position);
    }

    /// <summary>Phase 2 (headless-callable): raise the pillars once.</summary>
    public void EnterPhaseTwo()
    {
        if (PhaseTwo) return;
        PhaseTwo = true;
        PhaseTwoEntries++;
        DiabloJuice.OnBlast(transform.position);
        try { PresentationVfx.PhaseTwoBurst(transform.position); } catch { }
        if (IsSpawned && IsServer) Phase.Value = 2;
        var nm = NetworkManager.Singleton;
        bool netServer = nm != null && nm.IsListening && nm.IsServer;
        for (int i = 0; i < PillarCount; i++)
        {
            float a = (90f + i * 120f) * Mathf.Deg2Rad;
            var pos = transform.position + new Vector3(Mathf.Cos(a) * PillarRadius, 0f, Mathf.Sin(a) * PillarRadius);
            pillars.Add(SpawnPillar($"{PillarPrefix}{i}_{PhaseTwoEntries}", pos, netServer));
        }
        Debug.Log($"[Glasswright] Phase 2: {PillarCount} glass pillars ward the boss.");
    }

    static EnemyHealth SpawnPillar(string name, Vector3 pos, bool netServer)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.tag = "Husk";
        pos.y = 1.2f;
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.9f, 2.4f, 0.9f);
        go.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        var r = go.GetComponent<Renderer>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (r != null && shader != null)
        {
            var m = new Material(shader) { color = new Color(0.4f, 0.9f, 0.85f) };
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(0.3f, 0.9f, 0.85f) * 1.5f);
            r.sharedMaterial = m;
        }
        var hp = go.AddComponent<EnemyHealth>();
        hp.SetTier(PillarHP, LootTier.Normal);
        if (netServer) go.AddComponent<NetworkObject>().Spawn();
        return hp;
    }

    public void OnEnemyDeath()
    {
        if (dead) return;
        dead = true;
        Kills++;
        bool first = RunProfile.Act.DefeatZoneBoss();
        Debug.Log($"[Glasswright] slain (kills={Kills}, zone boss defeated={RunProfile.Act.ZoneBossDefeated}).");
        var hero = AcquireHero();
        var heroGo = hero != null ? hero.gameObject : GameObject.Find(DiabloNetworkPlayer.HeroName);
        if (first) DropZoneUnique();
        if (first && heroGo != null && Diablo.IsLocalAuthority()) SaveService.SaveNow(heroGo);
        var relay = DiabloNetworkPlayer.HostRelay();
        if (relay != null) relay.BroadcastZoneBossKill();
        if (Application.isPlaying)
            try { MusicDirector.Ensure().PlayZone(ActProgress.Barrens); } catch { }
        SessionComplete.ShowZoneBossCleared(heroGo);
    }

    /// <summary>Last guaranteed Barrens unique (test seam).</summary>
    public static ItemInstance LastZoneUnique;

    /// <summary>
    /// act-two-opening S4: the first kill drops one Barrens unique at boss
    /// level on top of the regular Boss-tier loot (authority only, networked
    /// like any loot drop).
    /// </summary>
    void DropZoneUnique()
    {
        if (!Diablo.IsLocalAuthority() && !Diablo.IsSolo()) return;
        var rng = new System.Random();
        string id = ItemDatabase.ZoneUniqueIds[rng.Next(ItemDatabase.ZoneUniqueIds.Length)];
        int level = ItemLevelRules.ForDrop(transform.position, LootTier.Boss);
        var item = ItemDatabase.RollUnique(ItemDatabase.CreateUnique(id), level, DifficultyProfile.Current.affixTier);
        LastZoneUnique = item;
        var nm = NetworkManager.Singleton;
        bool networked = nm != null && nm.IsListening;
        if (Application.isPlaying)
            LootDrop.SpawnItem(transform.position + new Vector3(1.2f, 0.8f, 0f), item, networked);
        SessionComplete.BestDrop = LootDrop.PrettyName(id);
        Debug.Log($"[Glasswright] dropped {id} (ilvl {level}).");
    }

    [ClientRpc]
    void LanceClientRpc(Vector3 origin, Vector3 target)
    {
        if (IsServer) return;
        SpawnLances(origin, target);
    }

    [ClientRpc]
    void NovaClientRpc(Vector3 center)
    {
        if (IsServer) return;
        ShatterNova.Spawn(center);
    }

    [ClientRpc]
    void FieldClientRpc(Vector3 at)
    {
        if (IsServer) return;
        ShardField.Spawn(at);
    }

    float Planar(Vector3 p)
    {
        var d = p - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    /// <summary>Co-op targeting: nearest living hero, solo hero otherwise.</summary>
    public Transform AcquireHero() =>
        HitValidator.NearestLivingPlayer(transform.position) ?? HeroLocator.Hero();

    public static void ResetRun()
    {
        Kills = 0;
        PhaseTwoEntries = 0;
        LanceVolleys = 0;
        NovasFired = 0;
        FieldsCalled = 0;
        LastZoneUnique = null;
    }
}

/// <summary>
/// One shard lance: a line telegraph for <see cref="Telegraph"/> s, then the
/// line cuts once for <see cref="Damage"/>. Resolved on every peer against the
/// local hero (<see cref="HeroLocator.Solo"/>).
/// </summary>
public sealed class ShardLance : MonoBehaviour
{
    public const float Telegraph = 0.9f;
    public const float Length = 14f;
    public const float Width = 1.2f;
    public const int Damage = 3;

    public static int HeroHits;

    Vector3 origin;
    Vector3 dir;
    float bornAt;
    bool fired;
    Transform line;

    public static ShardLance Spawn(Vector3 origin, Vector3 dir)
    {
        origin.y = 0.04f;
        dir.y = 0f;
        dir.Normalize();
        var go = new GameObject("ShardLance");
        go.transform.position = origin;
        var l = go.AddComponent<ShardLance>();
        l.origin = origin;
        l.dir = dir;
        l.bornAt = Time.time;
        var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = "LanceTelegraph";
        var col = strip.GetComponent<Collider>();
        if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
        strip.transform.SetParent(go.transform, false);
        strip.transform.position = origin + dir * (Length * 0.5f);
        strip.transform.rotation = Quaternion.LookRotation(dir);
        strip.transform.localScale = new Vector3(Width * 0.3f, 0.02f, Length);
        var r = strip.GetComponent<Renderer>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (r != null && shader != null)
        {
            var m = new Material(shader) { color = new Color(0.5f, 1f, 0.95f) };
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(0.4f, 1f, 0.9f));
            r.sharedMaterial = m;
        }
        l.line = strip.transform;
        return l;
    }

    /// <summary>Pure rule: is <paramref name="p"/> inside the lance strip?</summary>
    public static bool Hits(Vector3 origin, Vector3 dir, Vector3 p)
    {
        var d = p - origin;
        d.y = 0f;
        dir.y = 0f;
        dir.Normalize();
        float along = Vector3.Dot(d, dir);
        if (along < 0f || along > Length) return false;
        var side = d - dir * along;
        return side.magnitude <= Width * 0.5f;
    }

    void Update() => Tick(Time.time, HeroLocator.Solo());

    /// <summary>Widen the warning, then cut once (headless-callable). True when it fired.</summary>
    public bool Tick(float now, Transform hero)
    {
        if (fired) return false;
        float t = Mathf.Clamp01((now - bornAt) / Telegraph);
        if (line != null) line.localScale = new Vector3(Width * Mathf.Lerp(0.3f, 1f, t), 0.02f, Length);
        if (now - bornAt < Telegraph) return false;
        fired = true;
        if (hero != null && Hits(origin, dir, hero.position))
        {
            var hp = hero.GetComponent<PlayerHealth>();
            if (hp != null && hp.CurrentHP > 0f)
            {
                int dmg = DifficultyProfile.Current.ScaleDamage(Damage);
                hp.TakeDamage(dmg, origin); // directional: a frontal block catches it
                HeroHits++;
                DiabloJuice.OnPlayerHurt(hero.position, dmg);
            }
        }
        DiabloJuice.OnBlast(origin + dir * (Length * 0.5f));
        if (Application.isPlaying) Destroy(gameObject, 0.15f); else DestroyImmediate(gameObject);
        return true;
    }

    public static void ResetRun() => HeroHits = 0;
}

/// <summary>
/// Kindled Shatter Nova: a ring marks the safe circle for
/// <see cref="GlasswrightController.NovaWindup"/> s, then everyone outside it
/// takes <see cref="GlasswrightController.NovaDamage"/> (unblockable).
/// </summary>
public sealed class ShatterNova : MonoBehaviour
{
    public static int HeroHits;

    float bornAt;
    bool fired;

    public static ShatterNova Spawn(Vector3 center)
    {
        center.y = 0.05f;
        var go = new GameObject("ShatterNova");
        go.transform.position = center;
        var n = go.AddComponent<ShatterNova>();
        n.bornAt = Time.time;
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "NovaSafeRing";
        var col = ring.GetComponent<Collider>();
        if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
        ring.transform.SetParent(go.transform, false);
        float s = GlasswrightController.NovaSafeRadius * 2f;
        ring.transform.localScale = new Vector3(s, 0.01f, s);
        var r = ring.GetComponent<Renderer>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (r != null && shader != null)
        {
            var m = new Material(shader) { color = new Color(0.85f, 1f, 1f) };
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(0.6f, 1f, 1f) * 1.2f);
            r.sharedMaterial = m;
        }
        return n;
    }

    /// <summary>Pure rule: outside the safe circle means hit.</summary>
    public static bool Hits(Vector3 center, Vector3 hero)
    {
        var d = hero - center;
        d.y = 0f;
        return d.magnitude > GlasswrightController.NovaSafeRadius;
    }

    void Update() => Tick(Time.time, HeroLocator.Solo());

    public bool Tick(float now, Transform hero)
    {
        if (fired) return false;
        if (now - bornAt < GlasswrightController.NovaWindup) return false;
        fired = true;
        if (hero != null && Hits(transform.position, hero.position))
        {
            var hp = hero.GetComponent<PlayerHealth>();
            if (hp != null && hp.CurrentHP > 0f)
            {
                int dmg = DifficultyProfile.Current.ScaleDamage(GlasswrightController.NovaDamage);
                hp.TakeDamage(dmg);
                HeroHits++;
                DiabloJuice.OnPlayerHurt(hero.position, dmg);
            }
        }
        DiabloJuice.OnBlast(transform.position);
        if (Application.isPlaying) Destroy(gameObject, 0.15f); else DestroyImmediate(gameObject);
        return true;
    }

    public static void ResetRun() => HeroHits = 0;
}
