using UnityEngine;

/// <summary>
/// Husk counterattack (Blood Moor retaliation, D2 Fallen Ones). Distance-based
/// (never triggers — autosync-safe): in melee range the husk crouches
/// (telegraph), then strikes after the windup. Dodging out of range whiffs.
/// Damage goes to local <see cref="PlayerHealth"/> (solo-first, like HP).
/// </summary>
public sealed class EnemyAttack : MonoBehaviour
{
    public const float Range = 2.0f;
    public const float Windup = 0.4f;
    public const float Cooldown = 1.5f;
    public const float DodgeLeeway = 0.5f;

    /// <summary>Shuffle toward the hero when near but out of reach (D2 roam).</summary>
    public const float AggroRange = 8f;
    public const float ApproachSpeed = 1.5f;
    public const float StopDistance = 1.8f;

    /// <summary>Set by the builder per tier (1 normal, 2 elite/brute).</summary>
    public int damage = 1;

    /// <summary>Husks closer than this push apart while approaching (no stacking).</summary>
    public const float SeparationRadius = 1.1f;
    /// <summary>Probe radius/height for the dressing check (capsule waist).</summary>
    public const float ProbeRadius = 0.35f;
    public const float ProbeHeight = 0.6f;
    static readonly float[] DetourAngles = { 0f, 35f, -35f, 70f, -70f, 105f, -105f };

    static readonly System.Collections.Generic.List<EnemyAttack> live =
        new System.Collections.Generic.List<EnemyAttack>();

    /// <summary>Times the approach had to turn off the straight line (test seam).</summary>
    public int detours;

    Transform victim;
    HuskAnimator anim;
    float windupStart = -1f;
    float lastStrike = float.NegativeInfinity;
    Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
        anim = GetComponent<HuskAnimator>();
        if (anim == null) anim = gameObject.AddComponent<HuskAnimator>();
    }

    void OnEnable() => live.Add(this);
    void OnDisable() => live.Remove(this);

    void Update()
    {
        var hero = AcquireHero();
        if (hero == null)
        {
            CancelWindup();
            anim.SetMoving(false);
            return;
        }
        float dist = PlanarDistance(hero);
        FaceHero(hero);
        if (dist > Range && dist <= AggroRange && windupStart < 0f
            && Time.time - lastStrike >= Cooldown)
        {
            var step = hero.position - transform.position;
            step.y = 0f;
            step.Normalize();
            float move = Mathf.Min(ApproachSpeed * Time.deltaTime, dist - StopDistance);
            bool walked = false;
            if (move > 0f)
            {
                var dir = Steer(step, move);
                if (dir.sqrMagnitude > 0f)
                {
                    transform.position += dir * move;
                    walked = true;
                }
            }
            anim.SetMoving(walked);
            return;
        }
        anim.SetMoving(false);
        if (windupStart >= 0f)
        {
            if (dist > Range + DodgeLeeway + 1.5f)
            {
                // Ran far away: cancel, short cooldown before re-engaging.
                CancelWindup();
                lastStrike = Time.time;
            }
            else if (Time.time - windupStart >= Windup)
            {
                ResolveWindup(hero, dist);
            }
            return;
        }
        if (dist <= Range && Time.time - lastStrike >= Cooldown)
        {
            victim = hero;
            windupStart = Time.time;
            anim.OnAttack();
            transform.localScale = new Vector3(
                baseScale.x * 1.1f, baseScale.y * 0.85f, baseScale.z * 1.1f);
        }
    }

    void ResolveWindup(Transform hero, float dist)
    {
        transform.localScale = baseScale;
        windupStart = -1f;
        lastStrike = Time.time;
        if (hero == null || dist > Range + DodgeLeeway) return; // dodged
        var hp = hero.GetComponent<PlayerHealth>();
        if (hp == null) return;
        hp.TakeDamage(damage);
        DiabloAudio.PlayHurt();
        if (hp.CurrentHP <= 0f)
        {
            hp.DieAndRespawn();
            DiabloAudio.PlayPlayerDeath();
        }
    }

    /// <summary>
    /// Walk direction around static dressing plus husk separation. Probes the
    /// straight line first, then fans out; heroes (Ignore Raycast), triggers
    /// and other husks never block (husks separate by distance math, since
    /// their transform-moved colliders are not synced). Zero = boxed in.
    /// </summary>
    Vector3 Steer(Vector3 toHero, float move)
    {
        var desired = toHero + Separation();
        desired.y = 0f;
        if (desired.sqrMagnitude < 0.0001f) desired = toHero;
        desired.Normalize();
        var origin = transform.position + Vector3.up * ProbeHeight;
        float probe = move + ProbeRadius + 0.25f;
        int mask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        for (int i = 0; i < DetourAngles.Length; i++)
        {
            var dir = Quaternion.Euler(0f, DetourAngles[i], 0f) * desired;
            if (!Blocked(origin, dir, probe, mask))
            {
                if (i > 0) detours++;
                return dir;
            }
        }
        return Vector3.zero;
    }

    bool Blocked(Vector3 origin, Vector3 dir, float probe, int mask)
    {
        var hits = Physics.SphereCastAll(origin, ProbeRadius, dir, probe, mask,
            QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (h.collider.GetComponentInParent<EnemyHealth>() != null) continue;
            if (h.collider.GetComponentInParent<PlayerHealth>() != null) continue;
            if (h.distance <= 0f) continue; // started inside (ground seam): not ahead
            return true;
        }
        return false;
    }

    Vector3 Separation()
    {
        var push = Vector3.zero;
        foreach (var other in live)
        {
            if (other == this) continue;
            var d = transform.position - other.transform.position;
            d.y = 0f;
            float m = d.magnitude;
            if (m >= SeparationRadius || m < 0.0001f) continue;
            push += d / m * (1f - m / SeparationRadius);
        }
        return push;
    }

    void CancelWindup()
    {
        if (windupStart < 0f) return;
        windupStart = -1f;
        victim = null;
        transform.localScale = baseScale;
    }

    float PlanarDistance(Transform hero)
    {
        var d = hero.position - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    void FaceHero(Transform hero)
    {
        var d = hero.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(d), 10f * Time.deltaTime);
    }

    Transform AcquireHero()
    {
        if (victim != null)
        {
            if (victim.GetComponent<PlayerHealth>() != null) return victim;
            victim = null;
        }
        var solo = GameObject.Find("Player");
        if (solo != null && solo.GetComponent<PlayerHealth>() != null) return solo.transform;
        float best = float.MaxValue;
        Transform bestT = null;
        foreach (var h in GameObject.FindObjectsOfType<PlayerHealth>())
        {
            var d = h.transform.position - transform.position;
            d.y = 0f;
            if (d.magnitude < best)
            {
                best = d.magnitude;
                bestT = h.transform;
            }
        }
        return bestT;
    }
}
