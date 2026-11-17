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

    Transform victim;
    float windupStart = -1f;
    float lastStrike = float.NegativeInfinity;
    Vector3 baseScale;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        var hero = AcquireHero();
        if (hero == null)
        {
            CancelWindup();
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
            if (move > 0f) transform.position += step * move;
            return;
        }
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
