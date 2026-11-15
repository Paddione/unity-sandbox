using UnityEngine;

/// <summary>
/// Melee swing for the Diablo hero (Q5). Range arc + cooldown; damage goes
/// through <see cref="EnemyHealth"/>. Driven by
/// <see cref="DiabloClickController"/> attack-move (or tests directly).
/// </summary>
public sealed class DiabloMelee : MonoBehaviour
{
    public float attackRange = 2.2f;
    public int damage = 1;
    public float cooldown = 0.5f;

    public static int Swings;

    float lastSwing = float.NegativeInfinity;

    /// <summary>Attempt one swing at target. Returns true on a landed hit.</summary>
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
        health.TakeDamage(damage);
        Debug.Log($"[Diablo] Melee swing #{Swings} hit {target.name}.");
        return true;
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        Swings = 0;
    }
}
