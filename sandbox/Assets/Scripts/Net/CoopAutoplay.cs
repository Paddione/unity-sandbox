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
/// the host in Lantern Hold, <c>zone1</c> once it landed hits in the Ashen Fen
/// and stayed connected until the host left the Fen (or the Fen is clear).
/// </summary>
public sealed class CoopAutoplay : MonoBehaviour
{
    public const string Flag = "-coop-autoplay";
    public const float FollowDistance = 3f;
    public const float EngageRange = 9f;
    /// <summary>Hits the client must land in the Fen for zone1.</summary>
    public const int MinFenHits = 3;
    /// <summary>The host left the Fen once its mirror is beyond this X (Ossuary at +200 m).</summary>
    public const float FenLeftX = 150f;
    public const float Budget = 600f;

    /// <summary>"host", "client" or null (no pair run).</summary>
    public static string Role => RoleFor(System.Environment.GetCommandLineArgs());

    public static string RoleFor(string[] args)
    {
        if (args == null) return null;
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == Flag)
            {
                var v = args[i + 1].ToLowerInvariant();
                if (v == AutoplayResult.RoleHost || v == AutoplayResult.RoleClient) return v;
            }
        return null;
    }

    int exceptions;
    int hits;
    readonly AutoplayResult result = new AutoplayResult { role = AutoplayResult.RoleClient };
    float startedAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (Role != AutoplayResult.RoleClient) return;
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

        // 2. Town: reach the host in Lantern Hold.
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

        // 3. Fen: follow and fight until the host leaves the Fen or the Fen is clear.
        var skills = hero.GetComponent<DiabloSkills>();
        bool hostLeft = false, clear = false;
        float quietSince = -1f;
        while (Time.realtimeSinceStartup < deadline && Connected())
        {
            Refill(hero, god);
            host = HostMirror();
            if (host == null) { yield return null; continue; }
            if (host.position.x > FenLeftX) { hostLeft = true; break; }
            var husk = NearestHusk(hero.transform.position, host.position);
            if (husk != null)
            {
                quietSince = -1f;
                var to = Flat(husk.transform.position - hero.transform.position);
                if (to.magnitude > hero.attackRange) hero.AttackTarget(husk);
                else
                {
                    hero.SetAimPoint(husk.transform.position);
                    if (hero.Strike()) hits++;
                    if (skills != null && skills.CooldownLeft(DiabloSkills.Skill.Fireball) <= 0f) skills.CastToward(DiabloSkills.Skill.Fireball, to);
                }
            }
            else
            {
                var away = Flat(hero.transform.position - host.position);
                if (away.magnitude > FollowDistance)
                    hero.SetDestination(host.position + away.normalized * FollowDistance);
                if (hits >= MinFenHits)
                {
                    if (quietSince < 0f) quietSince = Time.realtimeSinceStartup;
                    if (Time.realtimeSinceStartup - quietSince > 20f && NoHuskWithin(host.position, 40f))
                    {
                        clear = true;
                        break;
                    }
                }
            }
            yield return null;
        }
        Log($"phase fen: hits={hits} hostLeft={hostLeft} clear={clear} connected={Connected()}");
        if (hits >= MinFenHits && (hostLeft || clear)) result.Reach(AutoplayResult.PhaseZone1);
        // S3: stay for the host's gate broadcast so both sides reach zone 2.
        // The broadcast pulls this hero to the Ossuary (own x passes FenLeftX);
        // fall back to quitting after the grace period (zone1 credit kept).
        float graceBy = Time.realtimeSinceStartup
            + (result.phases.Contains(AutoplayResult.PhaseZone1) ? 60f : 0f);
        while (Time.realtimeSinceStartup < deadline && Time.realtimeSinceStartup < graceBy && Connected())
        {
            if (hero.transform.position.x > FenLeftX)
            {
                Log("phase zone2: pulled to the Ossuary by the host broadcast");
                break;
            }
            yield return null;
        }
        DiabloClickController.ExternalDrive = false;
        Finish();
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

    /// <summary>The host's hero as seen by this client (the mirror owned by the server).</summary>
    static Transform HostMirror()
    {
        foreach (var p in Object.FindObjectsByType<DiabloNetworkPlayer>(FindObjectsSortMode.None))
            if (p.IsSpawned && !p.IsOwner && p.OwnerClientId == NetworkManager.ServerClientId)
                return p.transform;
        return null;
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
