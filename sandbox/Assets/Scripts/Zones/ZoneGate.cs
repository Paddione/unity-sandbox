using UnityEngine;

/// <summary>
/// Zone transition gate (S5): trigger → fade → teleport. Sets the save's
/// <c>zoneReached</c> and autosaves via <see cref="SaveService.SaveNow"/>.
/// One gate lives at the Fen Gate (Town → Ossuary), the return gate lives
/// in the Ossuary (Ossuary → Town).
/// </summary>
public sealed class ZoneGate : MonoBehaviour
{
    public const string FenGateName = "FenGate";
    public const string OssuaryGateName = "OssuaryGate";

    /// <summary>Teleport destination (set by the builder).</summary>
    public Vector3 destination;
    /// <summary>Zone index reached by passing this gate (1 = town, 2 = ossuary).</summary>
    public int targetZone = 2;

    /// <summary>Gates used this run (test seam).</summary>
    public static int Teleports;

    void OnTriggerEnter(Collider other)
    {
        if (other == null || other.gameObject == null) return;
        var go = other.gameObject;
        if (go.name != "Player" && !go.CompareTag("Player")) return;
        if (go.GetComponent<PlayerHealth>() == null) return;
        Teleport(go);
    }

    /// <summary>Move the hero, stamp the zone and autosave (headless-callable).</summary>
    public void Teleport(GameObject hero)
    {
        if (hero == null) return;
        var cc = hero.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        hero.transform.position = destination;
        if (cc != null) cc.enabled = true;
        Teleports++;
        if (Diablo.IsLocalAuthority()) SaveService.SaveNow(hero, zoneReached: targetZone);
        Debug.Log($"[Zone] Gate {gameObject.name} → zone {targetZone} (teleports={Teleports}).");

    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun() => Teleports = 0;
}
