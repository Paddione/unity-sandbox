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
    // act-one S2: Ossuary → Cinder Kiln (locked until the Abbot falls) and back.
    public const string KilnGateName = "KilnGate";
    public const string KilnReturnGateName = "KilnReturnGate";
    public const string LockedHintName = "GateHint";

    /// <summary>Gate leads into a zone the act has not opened yet.</summary>
    public bool Locked => !RunProfile.Act.IsUnlocked(targetZone);

    /// <summary>Gates refused because the target zone was locked (test seam).</summary>
    public static int Refusals;

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

    void Update()
    {
        RefreshHint();
        TickDwell(Time.time);
    }

    /// <summary>Locked-gate hint follows the act state (headless-callable).</summary>
    public void RefreshHint()
    {
        var hint = transform.Find(LockedHintName);
        if (hint == null) return;
        if (Application.isPlaying && hint.GetComponent<TextMesh>() == null)
        {
            var tm = hint.gameObject.AddComponent<TextMesh>();
            tm.text = KilnLayout.LockedHint;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = 0.5f;
            tm.fontSize = 48;
            tm.color = new Color(1f, 0.7f, 0.35f);
        }
        if (hint.gameObject.activeSelf != Locked) hint.gameObject.SetActive(Locked);
    }

    /// <summary>Move the hero, stamp the zone and autosave (headless-callable).</summary>
    public void Teleport(GameObject hero)
    {
        if (hero == null) return;
        if (Locked)
        {
            Refusals++;
            Debug.Log($"[Zone] Gate {gameObject.name} is locked (zone {targetZone}).");
            return;
        }
        ZoneTravel.TeleportHero(hero, destination);
        if (targetZone == ActProgress.Kiln) KilnLayout.ResetPlaza(); // fair-fights: re-arm arrival safety
        Teleports++;
        if (Diablo.IsLocalAuthority()) SaveService.SaveNow(hero, zoneReached: targetZone);
        Debug.Log($"[Zone] Gate {gameObject.name} → zone {targetZone} (teleports={Teleports}).");

    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        Teleports = 0;
        Refusals = 0;
    }
}
