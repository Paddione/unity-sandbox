using UnityEngine;

/// <summary>
/// One-shot onboarding hint at the zone entry stairs (Zone 1).
/// Fires for the first hero or network player walking in.
/// </summary>
public sealed class ZoneEntryHint : MonoBehaviour
{
    bool shown;

    void OnTriggerEnter(Collider other)
    {
        if (shown || other == null || other.gameObject == null) return;
        var go = other.gameObject;
        if (go.name != "Player" && !go.CompareTag("Player")) return;
        shown = true;
        Debug.Log("[Diablo] Entered Ember Crypt Approach — WASD/arrows to move, aim with the mouse, " +
                  "click to strike (click a husk to chase it). " +
                  "Clear the hall, loot the west elite, extract at the north crypt gate.");
    }
}
