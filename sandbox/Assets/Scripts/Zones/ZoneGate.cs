using UnityEngine;

/// <summary>
/// Zone transition gate (S5): trigger → dwell → teleport. Sets the save's
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

    /// <summary>
    /// The hero must stand still on the gate this long before it fires (moving
    /// more than <see cref="StillRadius"/> or landing a hit restarts it). Running
    /// across or along the Fen Gate mid-fight (seeded layouts put husks behind
    /// it) no longer yanks the hero into the Ossuary.
    /// </summary>
    public const float DwellSeconds = 0.6f;

    /// <summary>Moving farther than this restarts the dwell: the gate fires for a hero who stands still on it.</summary>
    public const float StillRadius = 0.3f;

    GameObject dwellHero;
    float dwellSince = -1f;
    Vector3 dwellAnchor;

    /// <summary>Hero stepped onto the gate (trigger seam, headless-callable).</summary>
    public void BeginDwell(GameObject hero, float now)
    {
        if (!IsHero(hero)) return;
        dwellHero = hero;
        dwellSince = now;
        dwellAnchor = hero.transform.position;
    }

    /// <summary>Hero left the gate before the dwell finished.</summary>
    public void EndDwell(GameObject hero)
    {
        if (hero != null && hero == dwellHero)
        {
            dwellHero = null;
            dwellSince = -1f;
        }
    }

    /// <summary>Fire once the hero has stood on the gate for <see cref="DwellSeconds"/>. True when it teleported.</summary>
    public bool TickDwell(float now) => TickDwell(now, DiabloMelee.LastStruckTime);

    /// <summary>
    /// Dwell with combat reset: every landed hit (melee or skill, see
    /// <see cref="DiabloMelee.LastStruckTime"/>) restarts the dwell, so a hero
    /// fighting on the gate stays in the zone; standing still on it travels.
    /// </summary>
    public bool TickDwell(float now, float lastHitTime)
    {
        if (dwellHero == null || dwellSince < 0f) return false;
        if (!StillOnGate(dwellHero))
        {
            // Moved off without OnTriggerExit (CC disabled for a nudge or teleport).
            EndDwell(dwellHero);
            return false;
        }
        if (lastHitTime > dwellSince && lastHitTime <= now) dwellSince = lastHitTime;
        var pos = dwellHero.transform.position;
        var moved = pos - dwellAnchor;
        moved.y = 0f;
        if (moved.sqrMagnitude > StillRadius * StillRadius)
        {
            // Walking along or through the gate is not "standing on it".
            dwellAnchor = pos;
            dwellSince = now;
            return false;
        }
        if (now - dwellSince < DwellSeconds) return false;
        var hero = dwellHero;
        Debug.Log($"[Zone] {gameObject.name} fired after standing {now - dwellSince:0.00} s.");
        dwellHero = null;
        dwellSince = -1f;
        Teleport(hero);
        return true;
    }

    /// <summary>Hero still inside the gate trigger (bounds plus hero radius). No collider = trust the trigger events.</summary>
    bool StillOnGate(GameObject hero)
    {
        var col = GetComponent<Collider>();
        if (col == null || hero == null) return hero != null;
        var b = col.bounds;
        b.Expand(new Vector3(1f, 4f, 1f));
        return b.Contains(hero.transform.position);
    }

    static bool IsHero(GameObject go) =>
        go != null && (go.name == "Player" || go.CompareTag("Player")) && go.GetComponent<PlayerHealth>() != null;

    void OnTriggerEnter(Collider other)
    {
        if (other != null) BeginDwell(other.gameObject, Time.time);
    }

    void OnTriggerExit(Collider other)
    {
        if (other != null) EndDwell(other.gameObject);
    }

    void Update() => TickDwell(Time.time);

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
