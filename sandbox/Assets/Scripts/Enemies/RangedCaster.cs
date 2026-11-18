using UnityEngine;

/// <summary>
/// Bone Warden ranged caster (S5 Sunken Ossuary). Holds 6–8 m distance from
/// the hero (backs off when crowded, approaches when far), then plays a
/// 0.6 s telegraph and fires a <see cref="DiabloProjectile"/> fireball.
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

    /// <summary>Telegraphed shots fired this run (test seam).</summary>
    public static int ShotsFired;

    float telegraphStart = -1f;
    float lastShot = float.NegativeInfinity;

    void Update()
    {
        var hero = AcquireHero();
        if (hero == null) { telegraphStart = -1f; return; }
        var flat = hero.position - transform.position;
        flat.y = 0f;
        float dist = flat.magnitude;
        if (flat.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(flat), 8f * Time.deltaTime);

        if (telegraphStart >= 0f)
        {
            if (Time.time - telegraphStart >= Telegraph)
            {
                telegraphStart = -1f;
                lastShot = Time.time;
                Fire(hero);
            }
            return;
        }
        // Spacing first: only start a shot from inside the hold band.
        if (dist < HoldMin) Step(-flat.normalized, HoldMin - dist);
        else if (dist > HoldMax) Step(flat.normalized, Mathf.Min(dist - HoldMax, 2f * Time.deltaTime));
        else if (Time.time - lastShot >= Cooldown)
            telegraphStart = Time.time;
    }

    void Step(Vector3 dir, float amount)
    {
        if (amount <= 0f || dir.sqrMagnitude < 0.0001f) return;
        dir.y = 0f;
        dir.Normalize();
        transform.position += dir * Mathf.Min(amount, 2f * Time.deltaTime + amount * 0.02f);
        var p = transform.position;
        p.y = transform.position.y;
        transform.position = p;
        telegraphStart = -1f; // moving cancels a stale windup
    }

    void Fire(Transform hero)
    {
        if (!Diablo.IsLocalAuthority() && !Diablo.IsSolo()) return;
        var from = transform.position + Vector3.up * 1.2f;
        var dir = hero.position - from;
        DiabloProjectile.Spawn(DiabloSkills.Skill.Fireball, from, dir, transform,
            ShotDamage, 12f, 20f, 0f);
        ShotsFired++;
        Debug.Log($"[Caster] {gameObject.name} fired (shots={ShotsFired}).");
    }

    Transform AcquireHero()
    {
        var solo = GameObject.Find("Player");
        if (solo != null && solo.GetComponent<PlayerHealth>() != null) return solo.transform;
        foreach (var h in GameObject.FindObjectsOfType<PlayerHealth>())
            return h.transform;
        return null;
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun() => ShotsFired = 0;
}
