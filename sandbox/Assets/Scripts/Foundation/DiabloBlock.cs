using UnityEngine;

/// <summary>
/// Held frontal block (Shift): halves frontal damage and reflects hostile
/// projectiles. Directional: the hero must face the attacker within
/// <see cref="BlockHalfAngle"/>; hits from behind land unreduced.
/// </summary>
public sealed class DiabloBlock : MonoBehaviour
{
    /// <summary>Frontal block cone (half-angle, matches the LMB arc).</summary>
    public const float BlockHalfAngle = 60f;

    /// <summary>Damage multiplier while a frontal hit is blocked (50 %).</summary>
    public const float BlockMultiplier = 0.5f;

    /// <summary>Movement multiplier while blocking (planted, 40 %).</summary>
    public const float BlockMoveMultiplier = 0.4f;

    /// <summary>Held input (Shift). Set by the controller; tests set it directly.</summary>
    public bool Held;

    /// <summary>True while the block input is held.</summary>
    public bool IsBlocking => Held;

    /// <summary>
    /// True when the block is held and the attacker is inside the frontal cone.
    /// Pure enough for EditMode tests (no input polling inside).
    /// </summary>
    public bool BlocksFrom(Vector3 fromPos)
    {
        if (!Held) return false;
        var to = fromPos - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return true;
        var fwd = transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) return true;
        return Vector3.Angle(fwd.normalized, to.normalized) <= BlockHalfAngle;
    }

    /// <summary>Apply the block multiplier to an incoming frontal hit.</summary>
    public static float Reduce(float amount) => amount * BlockMultiplier;
}
