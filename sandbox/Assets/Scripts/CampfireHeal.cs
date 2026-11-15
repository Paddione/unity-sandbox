using UnityEngine;

/// <summary>
/// Rogue Encampment campfire: standing in the trigger radius slowly heals
/// <see cref="PlayerHealth"/> (solo hero and co-op mirrors alike, locally).
/// The builder sizes the trigger from <see cref="Radius"/>.
/// </summary>
public sealed class CampfireHeal : MonoBehaviour
{
    public const float Radius = 4f;
    public const float HealPerSecond = 1f;

    void OnTriggerStay(Collider other)
    {
        if (other == null || other.gameObject == null) return;
        var health = other.gameObject.GetComponent<PlayerHealth>();
        if (health == null) return;
        health.Heal(HealPerSecond * Time.deltaTime);
    }
}
