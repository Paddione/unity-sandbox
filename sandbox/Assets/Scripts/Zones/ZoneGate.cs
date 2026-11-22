using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Zone transition gate (S5): trigger → dwell → teleport. Sets the save's
/// <c>zoneReached</c> and autosaves via <see cref="SaveService.SaveNow"/>.
/// One gate lives at the Fen Gate (Town → Ossuary), the return gate lives
/// in the Ossuary (Ossuary → Town).
/// S3 co-op: solo/offline keeps the single-hero dwell path. With clients
/// connected, only the host decides (<see cref="CoopTick"/>): travel fires
/// when every living hero stands on the gate, or when the host held it for
/// <see cref="HostHoldSeconds"/> (pulls stragglers). Clients skip local
/// dwell and follow the <see cref="ZoneTravelRelay"/> broadcast.
/// </summary>
public sealed class ZoneGate : MonoBehaviour
{
    public const string FenGateName = "FenGate";
    public const string OssuaryGateName = "OssuaryGate";
    // act-one S2: Ossuary → Cinder Kiln (locked until the Abbot falls) and back.
    public const string KilnGateName = "KilnGate";
    public const string KilnReturnGateName = "KilnReturnGate";
    // act-two-opening S1: Kiln → Glass Barrens (locked until the Kilnmother falls) and back.
    public const string BarrensGateName = "BarrensGate";
    public const string BarrensReturnGateName = "BarrensReturnGate";
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

    /// <summary>
    /// Co-op host override (S3): the host standing on the gate this long
    /// pulls the whole party through, even if a client never reached it.
    /// </summary>
    public const float HostHoldSeconds = 3f;

    GameObject dwellHero;
    float dwellSince = -1f;
    Vector3 dwellAnchor;
    float hostHoldSince = -1f;

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
        var nmDbg = NetworkManager.Singleton;
        Debug.Log($"[Zone] {gameObject.name} fired after standing {now - dwellSince:0.00} s. " +
            $"(net={nmDbg != null && nmDbg.IsListening} server={nmDbg != null && nmDbg.IsServer} " +
            $"mirrors={HasClientMirrors()} hero={hero?.name})");
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
        var nm = NetworkManager.Singleton;
        bool net = nm != null && nm.IsListening;
        if (net && !nm.IsServer) return; // clients follow the host broadcast
        if (net && nm.IsServer && HasClientMirrors())
        {
            CoopTick(Time.time);
            return;
        }
        TickDwell(Time.time);
    }

    /// <summary>
    /// True when at least one connected client's hero mirror is spawned here.
    /// Mirror count (not <c>ConnectedClientsIds</c>, which omits the local
    /// server) decides host-vs-solo for group travel.
    /// </summary>
    public static bool HasClientMirrors()
    {
        foreach (var p in Object.FindObjectsByType<DiabloNetworkPlayer>(FindObjectsSortMode.None))
            if (p != null && p.IsSpawned && !p.IsOwner) return true;
        return false;
    }

    /// <summary>
    /// Host group-travel check (S3, headless-callable with explicit time).
    /// True when it broadcast a travel. Offline/unspawned callers get false
    /// (solo keeps the single-hero dwell path).
    /// </summary>
    public bool CoopTick(float now)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening || !nm.IsServer || !HasClientMirrors()) return false;
        if (Locked)
        {
            hostHoldSince = -1f;
            return false;
        }
        var hostHero = HostHero();
        bool hostOn = hostHero != null && StillOnGate(hostHero.gameObject);
        if (!hostOn)
        {
            if (hostHoldSince >= 0f) Debug.Log($"[ZoneDBG] hold reset {gameObject.name}.");
            hostHoldSince = -1f;
            return false;
        }
        if (hostHoldSince < 0f)
        {
            hostHoldSince = now;
            Debug.Log($"[ZoneDBG] hold start {gameObject.name}.");
        }
        bool allOn = AllLivingOnGate();
        float held = now - hostHoldSince;
        if (!ZoneTravelRelay.GroupReady(allOn, held, locked: false))
        {
            if (held >= 1f) Debug.Log($"[ZoneDBG] hold {gameObject.name} allOn={allOn} held={held:0.0}s.");
            return false;
        }
        hostHoldSince = -1f;
        var relay = DiabloNetworkPlayer.HostRelay();
        if (relay == null) return false;
        Debug.Log($"[Zone] host broadcasts {gameObject.name} → zone {targetZone} (allOn={allOn}).");
        relay.BroadcastTravel(gameObject.name, destination, targetZone);
        return true;
    }

    /// <summary>
    /// The host's own hero: the owned player object (never a name lookup —
    /// a host scene holds two objects named "Player": the stood-down solo
    /// hero and the owned network hero).
    /// </summary>
    public static Transform HostHero()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening && nm.SpawnManager != null)
        {
            var local = nm.SpawnManager.GetLocalPlayerObject();
            if (local != null) return local.transform;
        }
        return HeroLocator.Solo();
    }

    /// <summary>True when at least one living hero exists and all of them stand on this gate.</summary>
    public bool AllLivingOnGate()
    {
        bool any = false;
        foreach (var h in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            if (h == null || !h.gameObject.activeInHierarchy) continue;
            if (h.CurrentHP <= 0f) continue; // the dead don't block travel
            any = true;
            if (!StillOnGate(h.gameObject)) return false;
        }
        return any;
    }

    /// <summary>Hint text over a sealed gate, per target zone.</summary>
    public static string LockedHintFor(int zone) =>
        zone == ActProgress.Barrens ? BarrensLayout.LockedHint : KilnLayout.LockedHint;

    /// <summary>Locked-gate hint follows the act state (headless-callable).</summary>
    public void RefreshHint()
    {
        var hint = transform.Find(LockedHintName);
        if (hint == null) return;
        if (Application.isPlaying && hint.GetComponent<TextMesh>() == null)
        {
            var tm = hint.gameObject.AddComponent<TextMesh>();
            tm.text = LockedHintFor(targetZone);
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.characterSize = 0.5f;
            tm.fontSize = 48;
            tm.color = new Color(1f, 0.7f, 0.35f);
        }
        if (hint.gameObject.activeSelf != Locked) hint.gameObject.SetActive(Locked);
    }

    /// <summary>
    /// Harness travel (S3): the autoplay driver calls this when the dwell did
    /// not fire (the hero fights around the gate instead of standing still).
    /// With clients connected the host broadcasts (every side travels, like
    /// the organic <see cref="CoopTick"/> path); solo falls back to the local
    /// <see cref="Teleport"/>. Returns true when a broadcast was sent (the
    /// host's own travel then arrives asynchronously via the fade routine).
    /// </summary>
    public bool TeleportViaHost(GameObject hero)
    {
        if (hero == null) return false;
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening && nm.IsServer && HasClientMirrors() && !Locked)
        {
            var relay = DiabloNetworkPlayer.HostRelay();
            if (relay != null)
            {
                Debug.Log($"[Zone] host broadcasts {name} → zone {targetZone} (autoplay).");
                relay.BroadcastTravel(gameObject.name, destination, targetZone);
                return true;
            }
        }
        Teleport(hero);
        return false;
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
        ApplyTravel(hero, destination);
    }

    /// <summary>
    /// Shared travel body (S3): solo dwell and co-op broadcast converge here
    /// (co-op passes a per-client offset destination).
    /// </summary>
    public void ApplyTravel(GameObject hero, Vector3 dest)
    {
        if (hero == null) return;
        ZoneTravel.TeleportHero(hero, dest);
        if (targetZone == ActProgress.Kiln) KilnLayout.ResetPlaza(); // fair-fights: re-arm arrival safety
        if (targetZone == ActProgress.Barrens) BarrensLayout.ResetPlaza();
        Teleports++;
        // S5: solo/host saves like before; a client saves its own hero too.
        var net = hero.GetComponent<DiabloNetworkPlayer>();
        if (Diablo.IsLocalAuthority() || (net != null && net.IsSpawned && net.IsOwner))
            SaveService.SaveNow(hero, zoneReached: targetZone);
        // presentation S1+S2: cross-fade light grade + ambience bed + music on every gate.
        try { DiabloAmbience.ApplyZone(targetZone); } catch { }
        try { MusicDirector.Ensure().PlayZone(targetZone); } catch { }
        try { DiabloAudio.PlayGate(); } catch { }
        Debug.Log($"[Zone] Gate {gameObject.name} → zone {targetZone} (teleports={Teleports}).");
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        Teleports = 0;
        Refusals = 0;
    }
}
