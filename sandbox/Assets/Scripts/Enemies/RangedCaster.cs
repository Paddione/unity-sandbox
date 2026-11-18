using UnityEngine;

/// <summary>
/// Bone Warden ranged caster (S5 Sunken Ossuary). Holds 6–8 m distance from
/// the hero (backs off when crowded, approaches when far), then plays a
/// 0.6 s telegraph and fires a hostile <see cref="DiabloProjectile"/> fireball
/// that damages the hero. Acts only while the hero is within
/// <see cref="AggroRange"/> and never strays more than <see cref="LeashRadius"/>
/// from its spawn point; when the hero leaves aggro range or a knockback throws
/// it past the leash, the warden walks home and ignores the hero until it is
/// back (no march from the Ossuary into the Fen).
/// Projectiles spawn host-authoritatively (solo/host only, like loot);
/// clients see the telegraph but never spawn.
/// </summary>
public sealed class RangedCaster : MonoBehaviour
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

    float telegraphStart = -1f;
    float lastShot = float.NegativeInfinity;
    Vector3 home;
    bool homeSet;

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
        if (hero == null || dist > AggroRange || fromHome > LeashRadius + LeashSlack)
        {
            telegraphStart = -1f;
            if (fromHome > 0.3f) Returning = true;
            return;
        }

        if (flat.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(flat), Mathf.Clamp01(8f * dt));

        if (telegraphStart >= 0f)
        {
            if (now - telegraphStart >= Telegraph)
            {
                telegraphStart = -1f;
                lastShot = now;
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
            telegraphStart = now;
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
        var from = transform.position + Vector3.up * 1.2f;
        var dir = hero.position - from;
        DiabloProjectile.Spawn(DiabloSkills.Skill.Fireball, from, dir, transform,
            ShotDamage, 12f, 20f, 0f, hostile: true);
        ShotsFired++;
        Debug.Log($"[Caster] {gameObject.name} fired (shots={ShotsFired}).");
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    Transform AcquireHero() => HeroLocator.Hero(); // cached per frame (act-one perf fix)

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun() => ShotsFired = 0;
}
