using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Bone Warden ranged caster (S5 Sunken Ossuary). Holds 6–8 m distance from
/// the hero (backs off when crowded, approaches when far), then plays a
/// 0.6 s telegraph and fires a hostile <see cref="DiabloProjectile"/> fireball
/// that damages the hero. Acts only while the hero is within
/// <see cref="AggroRange"/> and never strays more than <see cref="LeashRadius"/>
/// from its spawn point; when the hero leaves aggro range or a knockback throws
/// it past the leash, the warden walks home and ignores the hero until it is
/// back (no march from the Ossuary into the Fen). Before warning and shooting
/// it checks fixed cover (encounter-roles S2): with the sight axis interrupted
/// it sidesteps for a clear angle instead of wasting the shot.
/// S2 co-op: projectiles spawn host-authoritatively (solo/host only, like loot);
/// the telegraph fans out per ClientRpc so clients see the windup, targeting
/// picks the nearest living hero.
/// </summary>
public sealed class RangedCaster : NetworkBehaviour
{
    public const float HoldMin = 6f;
    public const float HoldMax = 8f;
    public const float Telegraph = 0.6f;
    public const float Cooldown = 2.5f;
    public const int ShotDamage = 2;
    public const float AggroRange = 14f;
    public const float LeashRadius = 10f;
    /// <summary>Distance past the leash (knockback) that forces a walk home.</summary>
    public const float LeashSlack = 2f;
    public const float MoveSpeed = 2f;
    public const float ReturnSpeed = 3f;

    /// <summary>Telegraphed shots fired this run (test seam).</summary>
    public static int ShotsFired;

    /// <summary>Shots skipped for interrupted sight axes this run (test seam).</summary>
    public static int CoverHolds;

    /// <summary>True while the windup runs (test seam, drives the orb).</summary>
    public bool Telegraphing => telegraphStart >= 0f;

    float telegraphStart = -1f;
    float lastShot = float.NegativeInfinity;
    Vector3 home;
    bool homeSet;
    Transform orb;
    Light glow;

    /// <summary>Walking back to the spawn point, hero ignored.</summary>
    public bool Returning { get; private set; }
    public Vector3 Home => home;

    void Start() => SetHome(transform.position);

    public void SetHome(Vector3 at)
    {
        home = at;
        homeSet = true;
    }

    void Update() => Tick(Time.time, Time.deltaTime, AcquireHero());

    /// <summary>One step of the caster brain (headless-callable with explicit time).</summary>
    public void Tick(float now, float dt, Transform hero)
    {
        if (!homeSet) SetHome(transform.position);
        float fromHome = Flat(transform.position - home).magnitude;

        if (Returning)
        {
            if (WalkHome(dt)) Returning = false;
            return;
        }

        var flat = hero != null ? Flat(hero.position - transform.position) : Vector3.zero;
        float dist = flat.magnitude;
        // act-two-opening S2: the Barrens arrival plaza holds casters too (the Kiln keeps its old behaviour).
        if (hero != null && BarrensLayout.PlazaSafe(hero.position))
        {
            CancelTelegraph();
            return;
        }
        if (hero == null || dist > AggroRange || fromHome > LeashRadius + LeashSlack)
        {
            CancelTelegraph();
            if (fromHome > 0.3f) Returning = true;
            return;
        }

        if (flat.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(flat), Mathf.Clamp01(8f * dt));

        if (telegraphStart >= 0f)
        {
            GrowTelegraph(now);
            if (now - telegraphStart >= Telegraph)
            {
                telegraphStart = -1f;
                lastShot = now;
                HideTelegraph();
                Fire(hero);
            }
            return;
        }

        // Spacing first: only start a shot from inside the hold band, or from the
        // leash edge when the band lies beyond it.
        bool inBand = dist >= HoldMin && dist <= HoldMax;
        bool pinned = false;
        if (dist < HoldMin) pinned = !Step(-flat.normalized, HoldMin - dist, dt);
        else if (dist > HoldMax) pinned = !Step(flat.normalized, dist - HoldMax, dt);
        if ((inBand || pinned) && now - lastShot >= Cooldown)
        {
            // Fixed cover before warning + shot: sidestep for a clear angle instead.
            if (!HasClearShot(transform.position, hero.position))
            {
                CoverHolds++;
                Strafe(dt);
                return;
            }
            telegraphStart = now;
            ShowTelegraph();
            if (IsSpawned && IsServer) TelegraphClientRpc();
        }
    }

    /// <summary>Sight axis free of dressing (walls, pillars, sarcophagi)? Heroes and husks never block.</summary>
    public static bool HasClearShot(Vector3 from, Vector3 to)
    {
        var origin = from + Vector3.up * 1.2f;
        var dir = to - origin;
        dir.y = 0f;
        float dist = dir.magnitude;
        if (dist < 0.001f) return true;
        dir /= dist;
        int mask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        var hits = Physics.SphereCastAll(origin, 0.3f, dir, dist, mask, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            var t = h.collider.transform;
            Transform p = t;
            bool friendly = false;
            while (p != null)
            {
                if (p.GetComponent<EnemyHealth>() != null || p.GetComponent<PlayerHealth>() != null) { friendly = true; break; }
                p = p.parent;
            }
            if (friendly) continue;
            if (h.distance <= 0f) continue; // started inside (ground seam): not ahead
            return false;
        }
        return true;
    }

    void Strafe(float dt)
    {
        var hero = AcquireHero();
        if (hero == null) return;
        var flat = new Vector3(hero.position.x - transform.position.x, 0f, hero.position.z - transform.position.z);
        if (flat.sqrMagnitude < 0.0001f) return;
        var side = Vector3.Cross(Vector3.up, flat.normalized);
        if (!Step(side, MoveSpeed * dt, dt)) Step(-side, MoveSpeed * dt, dt);
    }

    void CancelTelegraph()
    {
        telegraphStart = -1f;
        HideTelegraph();
    }

    /// <summary>Glowing charge orb above the warden: the readable windup (like the lancer line).</summary>
    void ShowTelegraph()
    {
        if (!Application.isPlaying) return;
        if (orb == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "WardenTelegraph";
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 2f, 0f);
            var r = go.GetComponent<Renderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (r != null && shader != null)
            {
                var m = new Material(shader) { color = new Color(1f, 0.45f, 0.1f) };
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(1f, 0.4f, 0.08f) * 2f);
                r.sharedMaterial = m;
            }
            glow = go.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.5f, 0.15f);
            glow.range = 6f;
            glow.intensity = 0f;
            orb = go.transform;
        }
        orb.gameObject.SetActive(true);
    }

    void GrowTelegraph(float now)
    {
        if (orb == null || !orb.gameObject.activeSelf) return;
        float t = Mathf.Clamp01((now - telegraphStart) / Telegraph);
        orb.localScale = Vector3.one * Mathf.Lerp(0.25f, 1.1f, t);
        if (glow != null) glow.intensity = Mathf.Lerp(0f, 3f, t);
    }

    void HideTelegraph()
    {
        if (orb != null) orb.gameObject.SetActive(false);
        if (glow != null) glow.intensity = 0f;
    }

    void OnDestroy()
    {
        if (orb != null) Destroy(orb.gameObject);
    }

    /// <summary>Move toward <paramref name="dir"/>; false when the leash blocks the step.</summary>
    bool Step(Vector3 dir, float amount, float dt)
    {
        if (amount <= 0f || dir.sqrMagnitude < 0.0001f) return true;
        dir = Flat(dir).normalized;
        var next = transform.position + dir * Mathf.Min(amount, MoveSpeed * dt);
        if (Flat(next - home).magnitude > LeashRadius) return false;
        transform.position = next;
        telegraphStart = -1f; // moving cancels a stale windup
        return true;
    }

    /// <summary>Walk toward home; true once there.</summary>
    bool WalkHome(float dt)
    {
        var back = Flat(home - transform.position);
        float d = back.magnitude;
        if (d <= 0.3f) return true;
        transform.rotation = Quaternion.LookRotation(back);
        transform.position += back / d * Mathf.Min(d, ReturnSpeed * dt);
        return Flat(home - transform.position).magnitude <= 0.3f;
    }

    void Fire(Transform hero)
    {
        if (!Diablo.IsLocalAuthority() && !Diablo.IsSolo()) return;
        // act-two-opening S2: Dune Augurs cast a ground payload instead of the fireball.
        var payload = GetComponent<ICasterPayload>();
        if (payload != null)
        {
            payload.Cast(hero.position);
            if (IsSpawned && IsServer) PayloadClientRpc(hero.position);
            ShotsFired++;
            Debug.Log($"[Caster] {gameObject.name} cast its payload (shots={ShotsFired}).");
            return;
        }
        var from = transform.position + Vector3.up * 1.2f;
        var dir = hero.position - from;
        DiabloProjectile.Spawn(DiabloSkills.Skill.Fireball, from, dir, transform,
            ShotDamage, 12f, 20f, 0f, hostile: true);
        ShotsFired++;
        Debug.Log($"[Caster] {gameObject.name} fired (shots={ShotsFired}).");
    }

    /// <summary>S2 telegraph fan-out (server tells clients to show the orb).</summary>
    [ClientRpc]
    public void TelegraphClientRpc()
    {
        if (IsServer) return;
        ShowTelegraph();
    }

    /// <summary>act-two-opening S2: clients show the same payload (damage stays with the authority).</summary>
    [ClientRpc]
    public void PayloadClientRpc(Vector3 at)
    {
        if (IsServer) return;
        var payload = GetComponent<ICasterPayload>();
        if (payload != null) payload.Cast(at);
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    /// <summary>S2 co-op targeting: nearest living hero.</summary>
    public Transform AcquireHero() =>
        HitValidator.NearestLivingPlayer(transform.position) ?? HeroLocator.Hero();

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        ShotsFired = 0;
        CoverHolds = 0;
    }
}
