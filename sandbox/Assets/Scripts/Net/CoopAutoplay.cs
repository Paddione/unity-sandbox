using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Co-op pair driver (coop-session S1), launched with
/// <c>-coop-autoplay host|client</c>. The host role only marks
/// <see cref="SessionAutoplay"/> (which also needs <c>-autoplay</c>): it waits
/// for the client and stamps <c>role: host</c>. The client role drives the
/// joined hero itself: it follows the host's mirror at about 3 m and attacks
/// husks near it (hits go through the validated ServerRpc), then writes its own
/// <see cref="AutoplayResult"/> with <c>role: client</c> and the
/// <see cref="AutoplayResult.ClientPhases"/>: <c>town</c> once it stood next to
/// the host in Lantern Hold, then (coop-trio) one phase per zone it fought in
/// and per boss kill the host broadcast, through Act I and the Glass Barrens.
/// Several clients may run at once (<c>-coop-peers</c> on the host).
/// </summary>
public sealed class CoopAutoplay : MonoBehaviour
{
    public const string Flag = "-coop-autoplay";
    public const float FollowDistance = 3f;
    public const float EngageRange = 9f;
    /// <summary>Strikes or attack casts the client must make in a zone to book it.</summary>
    public const int MinZoneHits = 3;
    /// <summary>Players the host waits for before the route starts (<c>-coop-peers N</c>, default 2).</summary>
    public const string PeersFlag = "-coop-peers";
    public const float Budget = 1000f;

    /// <summary>"host", "client" or null (no pair run).</summary>
    public static string Role => RoleFor(System.Environment.GetCommandLineArgs());

    public static string RoleFor(string[] args)
    {
        if (args == null) return null;
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == Flag)
            {
                var v = args[i + 1].ToLowerInvariant();
                if (v == AutoplayResult.RoleHost || v == AutoplayResult.RoleClient
                    || v == AutoplayResult.RoleLead || v == AutoplayResult.RoleFollow) return v;
            }
        return null;
    }

    /// <summary>Total players (host included) a pair/trio run waits for; 2..4.</summary>
    public static int PeersFor(string[] args)
    {
        if (args != null)
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == PeersFlag && int.TryParse(args[i + 1], out int n))
                    return Mathf.Clamp(n, 2, 4);
        return 2;
    }

    int exceptions;
    int hits;
    int roleCasts;
    int deaths;
    readonly AutoplayResult result = new AutoplayResult { role = Role ?? AutoplayResult.RoleClient };
    float startedAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var role = Role;
        if (role != AutoplayResult.RoleClient && role != AutoplayResult.RoleLead && role != AutoplayResult.RoleFollow) return;
        var go = new GameObject("CoopAutoplay");
        DontDestroyOnLoad(go);
        go.AddComponent<CoopAutoplay>();
    }

    void OnEnable() => Application.logMessageReceived += Count;
    void OnDisable() => Application.logMessageReceived -= Count;

    void Count(string condition, string stackTrace, LogType type)
    {
        if (AutoplayResult.CountsAsException(type)) exceptions++;
    }

    IEnumerator Start()
    {
        startedAt = Time.realtimeSinceStartup;
        result.started = System.DateTime.UtcNow.ToString("o");
        result.commit = Arg(SessionAutoplay.CommitFlag) ?? "unknown";
        bool god = Has(SessionAutoplay.GodFlag);
        float deadline = Time.realtimeSinceStartup + Budget;
        Log($"client start, target {MultiplayerAutoStarter.Current}, god={(god ? "ON" : "off")}");

        // 1. Connected with an owned hero.
        DiabloClickController hero = null;
        float connectBy = Time.realtimeSinceStartup + 60f;
        while (Time.realtimeSinceStartup < connectBy)
        {
            var nm = NetworkManager.Singleton;
            var go = GameObject.Find(DiabloNetworkPlayer.HeroName);
            var net = go != null ? go.GetComponent<DiabloNetworkPlayer>() : null;
            if (nm != null && nm.IsConnectedClient && net != null && net.IsOwner)
            {
                hero = go.GetComponent<DiabloClickController>();
                if (hero != null && hero.isActiveAndEnabled) break;
                hero = null;
            }
            yield return null;
        }
        if (hero == null)
        {
            Log("FAIL not connected with an owned hero within 60 s");
            Finish();
            yield break;
        }
        Log($"connected as client {NetworkManager.Singleton.LocalClientId}, hero at {Fmt(hero.transform.position)}");
        DiabloClickController.ExternalDrive = true;
        HitStop.Enabled = false;

        if (result.role == AutoplayResult.RoleLead)
        {
            yield return LeadRoute(hero, god, deadline);
            DiabloClickController.ExternalDrive = false;
            Finish();
            yield break;
        }
        bool serverRun = result.role == AutoplayResult.RoleFollow;
        bool leaderSeen = false;

        // 2. Town: reach the host (dedicated server: the lead) in Lantern Hold.
        Transform host = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (!Connected()) break;
            host = HostMirror();
            if (host != null && Flat(host.position - hero.transform.position).magnitude <= FollowDistance + 1.5f)
                break;
            if (host != null) hero.SetDestination(host.position);
            Refill(hero, god);
            yield return null;
        }
        if (host == null || !Connected())
        {
            Log("FAIL host hero not reached in Lantern Hold");
            Finish();
            yield break;
        }
        result.Reach(AutoplayResult.PhaseTown);
        Log("phase town: next to the host");

        // 3. coop-trio: follow the host through every zone and fight next to it.
        // The host's gate broadcasts pull this hero from zone to zone; per zone it
        // must land MinZoneHits validated hits, and each boss kill the host
        // broadcasts must be booked here (Abbot → Kiln open, Kilnmother → Act I,
        // Glasswright → zone boss). Ends with the Glasswright kill.
        var skills = hero.GetComponent<DiabloSkills>();
        var zoneHits = new int[ActProgress.LastZone + 1];
        int lastZone = ActProgress.Town;
        bool wasDown = false;
        float lastLog = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup < deadline && Connected() && !RunProfile.Act.ZoneBossDefeated)
        {
            if (serverRun)
            {
                // Dedicated server: the lead quits after its route; that ends this follower's run too.
                if (HostMirror() != null) leaderSeen = true;
                else if (leaderSeen) { Log("lead left, follower run over"); break; }
            }
            Refill(hero, god);
            var hp = hero.GetComponent<PlayerHealth>();
            bool down = hp != null && hp.CurrentHP <= 0f;
            if (down && !wasDown) { deaths++; Log($"hero down (deaths={deaths}) at {Fmt(hero.transform.position)}"); }
            wasDown = down;
            int zone = EncounterLayout.CurrentZone(hero.transform.position);
            if (zone != lastZone)
            {
                Log($"zone {lastZone} → {zone}: hits there {zoneHits[lastZone]}, hero at {Fmt(hero.transform.position)}");
                lastZone = zone;
            }
            BookPhases(zoneHits, zone);
            host = HostMirror();
            if (host == null) { yield return null; continue; }
            if (Time.realtimeSinceStartup - lastLog > 30f)
            {
                lastLog = Time.realtimeSinceStartup;
                Log($"following the host: zone {zone} hits {zoneHits[zone]} hero {Fmt(hero.transform.position)} host {Fmt(host.position)}");
            }
            var husk = NearestHusk(hero.transform.position, host.position);
            if (husk != null)
            {
                var to = Flat(husk.transform.position - hero.transform.position);
                if (to.magnitude > hero.attackRange) hero.AttackTarget(husk);
                else
                {
                    hero.SetAimPoint(husk.transform.position);
                    if (hero.Strike()) { hits++; zoneHits[zone]++; }
                }
                // Ranged classes (healer bolts, rogue shots) fight mostly by cast:
                // a cast at a husk counts as taking part in the zone like a strike.
                if (RoleCast(skills, to)) { roleCasts++; zoneHits[zone]++; }
            }
            else
            {
                if (skills != null && TryHeal(skills)) roleCasts++;
                var away = Flat(hero.transform.position - host.position);
                if (away.magnitude > FollowDistance)
                    hero.SetDestination(host.position + away.normalized * FollowDistance);
            }
            yield return null;
        }
        BookPhases(zoneHits, EncounterLayout.CurrentZone(hero.transform.position));
        Log($"route end: hits={hits} roleCasts={roleCasts} deaths={deaths} perZone=[{zoneHits[1]},{zoneHits[2]},{zoneHits[3]},{zoneHits[4]}] " +
            $"kiln={RunProfile.Act.IsUnlocked(ActProgress.Kiln)} actOne={RunProfile.Act.ActOneComplete} " +
            $"zoneBoss={RunProfile.Act.ZoneBossDefeated} connected={Connected()}");
        DiabloClickController.ExternalDrive = false;
        Finish();
    }

    /// <summary>
    /// Client phase booking (coop-trio): zoneN once the hero landed
    /// <see cref="MinZoneHits"/> strikes or attack casts there and moved on (zone 4 counts in place),
    /// bosses once the host's kill broadcast changed this process's act state.
    /// </summary>
    void BookPhases(int[] zoneHits, int zone)
    {
        if (zoneHits[ActProgress.Town] >= MinZoneHits && zone > ActProgress.Town) Reach(AutoplayResult.PhaseZone1);
        if (zoneHits[ActProgress.Ossuary] >= MinZoneHits) Reach(AutoplayResult.PhaseZone2);
        if (RunProfile.Act.IsUnlocked(ActProgress.Kiln)) Reach(AutoplayResult.PhaseBoss);
        if (zoneHits[ActProgress.Kiln] >= MinZoneHits) Reach(AutoplayResult.PhaseZone3);
        if (RunProfile.Act.ActOneComplete) Reach(AutoplayResult.PhaseActBoss);
        if (zoneHits[ActProgress.Barrens] >= MinZoneHits) Reach(AutoplayResult.PhaseZone4);
        if (RunProfile.Act.ZoneBossDefeated) Reach(AutoplayResult.PhaseBoss3);
    }

    void Reach(string phase)
    {
        if (result.phases.Contains(phase)) return;
        result.Reach(phase);
        Log($"phase {phase}");
    }

    /// <summary>
    /// coop-trio: the client plays its class like a player would — specialty
    /// skills by role (casts go out as host-validated intents), base Fireball
    /// otherwise. True when a cast went out.
    /// </summary>
    static bool RoleCast(DiabloSkills skills, Vector3 to)
    {
        if (skills == null) return false;
        float dist = to.magnitude;
        switch (skills.ActiveClass.role)
        {
            case ClassDefinition.PartyRole.Tank:
                if (dist <= skills.slamRadius && skills.CastToward(DiabloSkills.Skill.SeismicSlam, to)) return true;
                break;
            case ClassDefinition.PartyRole.Dps:
                if (dist <= skills.flurryRadius && skills.CastToward(DiabloSkills.Skill.BladeFlurry, to)) return true;
                if (dist <= skills.quickShotRange && skills.CastToward(DiabloSkills.Skill.QuickShot, to)) return true;
                break;
            case ClassDefinition.PartyRole.Healer:
                if (TryHeal(skills)) return true;
                if (dist <= skills.lanternBoltRange && skills.CastToward(DiabloSkills.Skill.LanternBolt, to)) return true;
                break;
        }
        return skills.CastToward(DiabloSkills.Skill.Fireball, to);
    }

    /// <summary>Healer only: group heal when two allies are hurt nearby, else mend the nearest hurt ally.</summary>
    static bool TryHeal(DiabloSkills skills)
    {
        if (skills.ActiveClass.role != ClassDefinition.PartyRole.Healer) return false;
        int hurt = 0;
        foreach (var hp in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            if (hp == null || hp.CurrentHP <= 0f || hp.CurrentHP >= hp.MaxHP) continue;
            if (Flat(hp.transform.position - skills.transform.position).magnitude <= skills.groupHealRadius) hurt++;
        }
        if (hurt >= 2 && skills.CastToward(DiabloSkills.Skill.RenewingCircle, skills.transform.forward)) return true;
        return skills.CastToward(DiabloSkills.Skill.MendingLight, skills.transform.forward);
    }

    void Finish()
    {
        result.exceptions = exceptions;
        result.duration_s = Time.realtimeSinceStartup - startedAt;
        result.fps_windows = new System.Collections.Generic.List<float>(FpsReporter.Windows);
        result.classId = RunProfile.ClassId;
        result.difficulty = DifficultyProfile.Current.name;
        result.seed = LayoutSeeder.CurrentSeed;
        result.exit = result.ComputeExit();
        string dir = Arg(SessionAutoplay.OutFlag)
            ?? System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath) ?? ".", "autoplay");
        try
        {
            Log($"result written: {result.WriteTo(dir)} exit={result.exit} phases={string.Join(",", result.phases)} exceptions={exceptions}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CoopAutoplay] result write failed ({dir}): {e.Message}");
            result.exit = 1;
        }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(result.exit);
#endif
    }

    static bool Connected()
    {
        var nm = NetworkManager.Singleton;
        return nm != null && nm.IsConnectedClient;
    }

    /// <summary>
    /// The hero this client follows: the host's mirror (owned by the server), or on a
    /// dedicated server the other player with the lowest client id (the lead).
    /// </summary>
    static Transform HostMirror()
    {
        DiabloNetworkPlayer lead = null;
        foreach (var p in Object.FindObjectsByType<DiabloNetworkPlayer>(FindObjectsSortMode.None))
        {
            if (!p.IsSpawned || p.IsOwner) continue;
            if (p.OwnerClientId == NetworkManager.ServerClientId) return p.transform;
            if (lead == null || p.OwnerClientId < lead.OwnerClientId) lead = p;
        }
        return lead != null ? lead.transform : null;
    }

    /// <summary>Spawned heroes in the session (own included).</summary>
    static int PlayerCount()
    {
        int n = 0;
        foreach (var p in Object.FindObjectsByType<DiabloNetworkPlayer>(FindObjectsSortMode.None))
            if (p.IsSpawned) n++;
        return n;
    }

    /// <summary>Seconds the lead fights per zone before it moves on (followers need their hits too).</summary>
    public const float LeadZoneSeconds = 45f;
    /// <summary>Strikes or casts the lead lands per zone.</summary>
    public const int LeadZoneHits = 8;

    /// <summary>
    /// Dedicated-server lead (S1): waits for the party, fights in the Fen, walks the party
    /// through the Fen Gate (the server's leader rule moves everyone) and fights in the Ossuary.
    /// </summary>
    IEnumerator LeadRoute(DiabloClickController hero, bool god, float deadline)
    {
        int peers = PeersFor(System.Environment.GetCommandLineArgs());
        float joinBy = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < joinBy && Connected() && PlayerCount() < peers)
            yield return null;
        if (PlayerCount() < peers)
        {
            Log($"FAIL lead: {PlayerCount()}/{peers} players within 120 s");
            yield break;
        }
        Log($"lead: party of {PlayerCount()} together");
        result.Reach(AutoplayResult.PhaseTown);

        yield return LeadFight(hero, god, ActProgress.Town, deadline);
        if (lastLeadHits >= LeadZoneHits) Reach(AutoplayResult.PhaseZone1);

        var gate = GameObject.Find(ZoneGate.FenGateName);
        if (gate == null) { Log("FAIL lead: Fen Gate missing"); yield break; }
        float gateBy = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < gateBy && Connected()
               && EncounterLayout.CurrentZone(hero.transform.position) != ActProgress.Ossuary)
        {
            Refill(hero, god);
            hero.SetDestination(gate.transform.position);
            yield return null;
        }
        if (EncounterLayout.CurrentZone(hero.transform.position) != ActProgress.Ossuary)
        {
            Log($"FAIL lead: no group travel into the Ossuary, hero at {Fmt(hero.transform.position)}");
            yield break;
        }
        Reach(AutoplayResult.PhaseGate);
        yield return LeadFight(hero, god, ActProgress.Ossuary, deadline);
        if (lastLeadHits >= LeadZoneHits) Reach(AutoplayResult.PhaseZone2);
        // Give followers time to land their own hits before the lead leaves.
        float linger = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < linger && Connected()) { Refill(hero, god); yield return null; }
    }

    int lastLeadHits;

    IEnumerator LeadFight(DiabloClickController hero, bool god, int zone, float deadline)
    {
        lastLeadHits = 0;
        var skills = hero.GetComponent<DiabloSkills>();
        float until = Time.realtimeSinceStartup + LeadZoneSeconds;
        float hardStop = Time.realtimeSinceStartup + 180f;
        while (Connected() && Time.realtimeSinceStartup < deadline && Time.realtimeSinceStartup < hardStop
               && (lastLeadHits < LeadZoneHits || Time.realtimeSinceStartup < until))
        {
            Refill(hero, god);
            var husk = NearestHuskWithin(hero.transform.position, 30f, zone);
            if (husk == null) { yield return null; continue; }
            var to = Flat(husk.transform.position - hero.transform.position);
            if (to.magnitude > hero.attackRange) hero.AttackTarget(husk);
            else
            {
                hero.SetAimPoint(husk.transform.position);
                if (hero.Strike()) { hits++; lastLeadHits++; }
            }
            if (RoleCast(skills, to)) { roleCasts++; lastLeadHits++; }
            yield return null;
        }
        Log($"lead fight zone {zone}: {lastLeadHits} hits");
    }

    static GameObject NearestHuskWithin(Vector3 from, float range, int zone)
    {
        GameObject best = null;
        float bestD = range;
        foreach (var h in GameObject.FindGameObjectsWithTag("Husk"))
        {
            var eh = h.GetComponent<EnemyHealth>();
            if (eh == null || eh.tier == LootTier.Boss) continue;
            if (EncounterLayout.CurrentZone(h.transform.position) != zone) continue;
            float d = Flat(h.transform.position - from).magnitude;
            if (d < bestD) { bestD = d; best = h; }
        }
        return best;
    }

    static GameObject NearestHusk(Vector3 from, Vector3 host)
    {
        GameObject best = null;
        float bestD = float.MaxValue;
        foreach (var h in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (h.GetComponent<EnemyHealth>() == null) continue;
            if (Flat(h.transform.position - host).magnitude > EngageRange) continue;
            float d = Flat(h.transform.position - from).magnitude;
            if (d < bestD) { bestD = d; best = h; }
        }
        return best;
    }

    static bool NoHuskWithin(Vector3 at, float radius)
    {
        foreach (var h in GameObject.FindGameObjectsWithTag("Husk"))
            if (h.GetComponent<EnemyHealth>() != null && Flat(h.transform.position - at).magnitude <= radius)
                return false;
        return true;
    }

    static void Refill(DiabloClickController hero, bool god)
    {
        if (!god) return;
        var hp = hero.GetComponent<PlayerHealth>();
        if (hp != null) hp.Heal(hp.MaxHP);
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    static string Fmt(Vector3 v) => $"({v.x:0},{v.y:0},{v.z:0})";
    static void Log(string msg) => Debug.Log($"[CoopAutoplay] {msg}");

    static string Arg(string flag)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == flag) return args[i + 1];
        return null;
    }

    static bool Has(string flag)
    {
        foreach (var a in System.Environment.GetCommandLineArgs())
            if (a == flag) return true;
        return false;
    }
}
