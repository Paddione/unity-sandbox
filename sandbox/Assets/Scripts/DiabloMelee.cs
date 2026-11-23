using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Melee swing for the Diablo hero (Q5, Cinderreach rework). Stationary
/// frontal cone + cooldown; damage goes through <see cref="EnemyHealth"/>.
/// Driven by <see cref="DiabloClickController"/> (LMB cone, RMB outreach
/// thrust) or tests directly. Single-target <see cref="TryAttack"/> stays
/// for netcode/autoplay compatibility; player swings use the cone.
/// </summary>
public sealed class DiabloMelee : MonoBehaviour
{
    public float attackRange = 2.2f;
    public int damage = 1;
    public float cooldown = 0.5f;

    /// <summary>Outreach thrust (RMB): outranges husk melee (2.0 m).</summary>
    public const float OutreachRange = 3.2f;
    /// <summary>Narrow thrust cone (half-angle).</summary>
    public const float OutreachHalfAngle = 25f;
    /// <summary>Thrust cooldown (slower than the 0.5 s swing).</summary>
    public const float OutreachCooldown = 1.2f;

    public static int Swings;

    float lastSwing = float.NegativeInfinity;
    float lastThrust = float.NegativeInfinity;

    /// <summary>
    /// Damage for one swing: equipped weapon stats when the attacker gears
    /// through <see cref="Equipment"/>, else the flat <see cref="damage"/>.
    /// </summary>
    public int CurrentDamage(Transform attacker)
    {
        var sheet = attacker != null ? CharacterSheet.For(attacker.gameObject) : null;
        int strBonus = sheet != null ? sheet.MeleeBonus() : 0;
        var eq = attacker != null ? attacker.GetComponent<Equipment>() : null;
        if (eq == null) return damage + strBonus;
        return Mathf.Max(1, Mathf.RoundToInt(eq.GetDamage()) + strBonus);
    }

    /// <summary>
    /// Knockback distance by tier (D2 pop, not punt: combos must stay in the
    /// 2.2 m arc — 1.2 m broke the arc-kill contract, see Strike_HitsHuskInFacingArc).
    /// </summary>
    public const float KnockbackNormal = 0.6f;
    public const float KnockbackElite = 0.2f;

    /// <summary>Last husk actually hit (target bar follows it; null when none).</summary>
    public static GameObject LastStruck;
    public static float LastStruckTime;

    /// <summary>
    /// Attempt one swing at target (single-target path for netcode relay and
    /// autoplay chase). Player LMB/RMB use the cone (<see cref="TryConeAttack"/>
    /// / <see cref="TryThrust"/>). Returns true on a landed hit.
    /// </summary>
    public bool TryAttack(GameObject target, Transform attacker)
    {
        if (target == null || attacker == null) return false;
        if (Time.time - lastSwing < cooldown) return false;
        var toTarget = target.transform.position - attacker.position;
        toTarget.y = 0f;
        if (toTarget.magnitude > attackRange) return false;
        var health = target.GetComponent<EnemyHealth>();
        if (health == null) return false;
        lastSwing = Time.time;
        Swings++;
        if (toTarget.sqrMagnitude > 0.0001f)
            attacker.rotation = Quaternion.LookRotation(toTarget);
        HitSingle(target, health, attacker);
        LastStruck = target;
        LastStruckTime = Time.time;
        Debug.Log($"[Diablo] Melee swing #{Swings} hit {target.name}.");
        return true;
    }

    /// <summary>Frontal cone for cursor strikes (half-angle).</summary>
    public const float ArcHalfAngle = 60f;

    /// <summary>Nearest husk inside the facing arc, or null.</summary>
    public static GameObject FindArcTarget(Transform attacker, Vector3 dir, float range)
    {
        var all = FindArcTargets(attacker, dir, range, ArcHalfAngle);
        return all.Count > 0 ? all[0] : null;
    }

    /// <summary>All husks inside the facing cone, nearest first.</summary>
    public static List<GameObject> FindArcTargets(Transform attacker, Vector3 dir, float range, float halfAngle)
    {
        var found = new List<GameObject>();
        if (attacker == null) return found;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return found;
        dir.Normalize();
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk.GetComponent<EnemyHealth>() == null) continue;
            var to = husk.transform.position - attacker.position;
            to.y = 0f;
            float dist = to.magnitude;
            if (dist > range) continue;
            if (dist > 0.001f && Vector3.Angle(dir, to / dist) > halfAngle) continue;
            found.Add(husk);
        }
        found.Sort((a, b) =>
        {
            float da = (a.transform.position - attacker.position).sqrMagnitude;
            float db = (b.transform.position - attacker.position).sqrMagnitude;
            return da.CompareTo(db);
        });
        return found;
    }

    /// <summary>
    /// Stationary LMB cone: damages every husk in the 120° arc to
    /// <see cref="attackRange"/>. Returns the hit count (0 = whiff).
    /// </summary>
    public int TryConeAttack(Transform attacker, Vector3 dir)
    {
        if (attacker == null) return 0;
        if (Time.time - lastSwing < cooldown) return 0;
        var targets = FindArcTargets(attacker, dir, attackRange, ArcHalfAngle);
        if (targets.Count == 0) return 0;
        lastSwing = Time.time;
        Swings++;
        if (dir.sqrMagnitude > 0.0001f)
            attacker.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z).normalized);
        foreach (var t in targets)
        {
            var health = t.GetComponent<EnemyHealth>();
            if (health != null) HitSingle(t, health, attacker);
        }
        LastStruck = targets[0];
        LastStruckTime = Time.time;
        DiabloAudio.PlayHit();
        MeleeConeVfx.ShowCone(attacker.position, dir, attackRange, ArcHalfAngle, true);
        Debug.Log($"[Diablo] Melee cone #{Swings} hit {targets.Count}.");
        return targets.Count;
    }

    /// <summary>
    /// RMB outreach thrust: narrow cone to <see cref="OutreachRange"/>.
    /// Returns the hit count (0 = whiff).
    /// </summary>
    public int TryThrust(Transform attacker, Vector3 dir)
    {
        if (attacker == null) return 0;
        if (Time.time - lastThrust < OutreachCooldown) return 0;
        var targets = FindArcTargets(attacker, dir, OutreachRange, OutreachHalfAngle);
        if (targets.Count == 0) return 0;
        lastThrust = Time.time;
        Swings++;
        if (dir.sqrMagnitude > 0.0001f)
            attacker.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z).normalized);
        foreach (var t in targets)
        {
            var health = t.GetComponent<EnemyHealth>();
            if (health != null) HitSingle(t, health, attacker);
        }
        LastStruck = targets[0];
        LastStruckTime = Time.time;
        DiabloAudio.PlayHit();
        MeleeConeVfx.ShowCone(attacker.position, dir, OutreachRange, OutreachHalfAngle, true);
        Debug.Log($"[Diablo] Melee thrust #{Swings} hit {targets.Count}.");
        return targets.Count;
    }

    /// <summary>True when the LMB swing is off cooldown (whiff cones pace on this).</summary>
    public bool SwingReady => Time.time - lastSwing >= cooldown;

    /// <summary>True when the RMB thrust is off cooldown.</summary>
    public bool ThrustReady => Time.time - lastThrust >= OutreachCooldown;

    /// <summary>Swing toward a direction; hits every husk in the arc.</summary>
    public bool TryAttackDirection(Transform attacker, Vector3 dir)
    {
        return TryConeAttack(attacker, dir) > 0;
    }

    /// <summary>
    /// S3: exactly one crit roll per direct hit; the post-crit blow books
    /// threat (ThreatTable.BookDamage with blow after crit).
    /// </summary>
    void HitSingle(GameObject target, EnemyHealth health, Transform attacker)
    {
        int blow = DiabloJuice.RollCrit(attacker, CurrentDamage(attacker), out bool crit);
        ThreatTable.BookDamage(target, attacker, blow);
        health.TakeDamage(blow, crit, attacker.position);
        Equipment.ApplyLifeSteal(attacker, blow);
        health.ApplyKnockback(attacker.position,
            health.tier == LootTier.Normal ? KnockbackNormal : KnockbackElite);
        DiabloAudio.PlayHit();
        HitStop.Trigger(crit ? HitStop.Duration * 1.6f : HitStop.Duration);
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        Swings = 0;
        LastStruck = null;
        LastStruckTime = 0f;
    }
}
