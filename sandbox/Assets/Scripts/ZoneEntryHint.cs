using UnityEngine;

/// <summary>
/// One-shot onboarding hint at the Rogue Encampment gate (Blood Moor).
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
        Debug.Log("[Diablo] Entered the Rogue Encampment — WASD/arrows to move, aim with the mouse, " +
                  "click to strike (click a husk to chase it). " +
                  "Clear the Blood Moor, loot the west elite, open the chest, " +
                  "extract at the north Moor Gate. I opens your inventory.");
    }
}
