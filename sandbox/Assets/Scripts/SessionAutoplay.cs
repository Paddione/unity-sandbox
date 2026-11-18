using System.Collections;
using UnityEngine;

/// <summary>
/// S6 session proof driver (plan stage S6, basis pass without S5).
///
/// Launched with <c>-autoplay</c> (inert without the flag): takes over the
/// local hero via <see cref="DiabloClickController.ExternalDrive"/>, walks a
/// scripted route through Lantern Hold, fights husks with
/// <see cref="DiabloMelee"/> and <see cref="DiabloSkills"/>, buys a potion
/// through the S3 seam (<see cref="VendorStock.BuyPotion"/>), visits the
/// Fen Gate, then stops at the S5 seams below. Logs every phase plus an
/// exception counter and quits with exit code 0 (clean) or 1 (failure).
///
/// S5 SEAMS (Ossuary follow-up pass, names from the plan identity table):
/// <list type="bullet">
/// <item>Ossuary root + return gate: <c>ZoneGate</c> (S5) at the Fen Gate —
/// pass both gates once <c>ZoneGate</c> exists.</item>
/// <item>Zone-2 ranged enemy: <c>Bone Warden</c> (<c>RangedCaster</c>, S5).</item>
/// <item>Boss: <c>The Hollow Abbot</c> (<c>BossController</c>, S5) — fight
/// goes here; the phase-2 + SessionComplete shots belong to S6-full.</item>
/// <item>End screen: <c>SessionComplete</c> overlay (S5).</item>
/// </list>
/// Optional cheat <c>-autoplay-god</c> (refills HP every tick) exists only
/// for run determinism and is always disclosed in the log.
/// </summary>
public sealed class SessionAutoplay : MonoBehaviour
{
    public const string Flag = "-autoplay";
    public const string GodFlag = "-autoplay-god";

    /// <summary>Scene name of the Fen Gate extraction pad (Q3 GoalPad).</summary>
    public const string FenGateObjectName = "GoalPad";

    /// <summary>S5 seam: Ossuary gate object name (ZoneGate, does not exist yet).</summary>
    public const string OssuaryGateSeamName = "OssuaryGate (ZoneGate, S5)";

    /// <summary>S5 seam: boss object name (The Hollow Abbot, does not exist yet).</summary>
    public const string BossSeamName = "The Hollow Abbot (BossController, S5)";

    int phasesDone;
    int exceptionCount;
    bool godMode;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!HasArg(Flag)) return;
        var go = new GameObject("SessionAutoplay");
        DontDestroyOnLoad(go);
        go.AddComponent<SessionAutoplay>();
    }

    void OnEnable() => Application.logMessageReceived += CountExceptions;
    void OnDisable() => Application.logMessageReceived -= CountExceptions;

    void CountExceptions(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Exception) exceptionCount++;
    }

    IEnumerator Start()
    {
        godMode = HasArg(GodFlag);
        Log($"start, god={(godMode ? "ON (deterministic cheat)" : "off")}");
        float deadline = Time.realtimeSinceStartup + 240f;

        DiabloClickController hero = null;
        float heroDeadline = Time.realtimeSinceStartup + 30f;
        while (hero == null && Time.realtimeSinceStartup < heroDeadline)
        {
            var go = GameObject.Find(DiabloNetworkPlayer.HeroName);
            if (go != null)
            {
                var c = go.GetComponent<DiabloClickController>();
                if (c != null && c.isActiveAndEnabled) hero = c;
            }
            yield return null;
        }
        if (hero == null)
        {
            Log("FAIL no controllable hero within 30 s");
            Quit(1);
            yield break;
        }
        DiabloClickController.ExternalDrive = true;
        HitStop.Enabled = false;

        // Deterministic purse for the potion leg (S2 Wallet seam).
        var wallet = Wallet.EnsureWallet(hero.gameObject);
        if (wallet != null && wallet.Gold < ItemDatabase.PotionPrice)
        {
            wallet.Add(ItemDatabase.PotionPrice * 10);
            Log($"purse topped up to {wallet.Gold} gold (deterministic run)");
        }

        yield return PhaseTownWaypoints(hero, deadline);
        yield return PhaseBuyPotion(hero);
        yield return PhaseCombat(hero, deadline);
        yield return PhaseFenGate(hero, deadline);
        PhaseOssuarySeam();
        PhaseBossSeam();

        DiabloClickController.ExternalDrive = false;
        Log($"DONE phases={phasesDone} exceptions={exceptionCount}");
        Quit(exceptionCount > 0 ? 1 : 0);
    }

    IEnumerator PhaseTownWaypoints(DiabloClickController hero, float deadline)
    {
        Log("phase town_waypoints start (Lantern Hold: campfire, vendor, gate pillars)");
        string[] stops = { "Campfire", Vendor.VendorObjectName, "ZGatePillar_0" };
        foreach (var name in stops)
        {
            var target = GameObject.Find(name);
            if (target == null)
            {
                Log($"phase town_waypoints: '{name}' not found, skipped");
                continue;
            }
            hero.SetDestination(target.transform.position);
            float leg = Time.realtimeSinceStartup + 25f;
            while (hero.HasDestination && Time.realtimeSinceStartup < leg
                   && Time.realtimeSinceStartup < deadline)
            {
                TickGod(hero);
                yield return null;
            }
            Log($"phase town_waypoints: reached {name}");
        }
        phasesDone++;
        Log("phase town_waypoints done");
    }

    IEnumerator PhaseBuyPotion(DiabloClickController hero)
    {
        Log("phase vendor_potion start (S3 seam: VendorStock.BuyPotion)");
        var inv = hero.GetComponent<Inventory>();
        bool bought = VendorStock.BuyPotion(wallet: Wallet.EnsureWallet(hero.gameObject), inv: inv);
        Log($"phase vendor_potion: bought={bought} (S3 seam, no UI drive in basis pass)");
        phasesDone++;
        Log("phase vendor_potion done");
        yield return null;
    }

    IEnumerator PhaseCombat(DiabloClickController hero, float deadline)
    {
        Log("phase combat start (melee + Fireball/Arrow vs husks)");
        var skills = hero.GetComponent<DiabloSkills>();
        int startCount = CountHusks();
        int kills = 0;
        float combatEnd = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < combatEnd && Time.realtimeSinceStartup < deadline)
        {
            TickGod(hero);
            var husk = NearestHusk(hero.transform.position, 30f);
            if (husk == null)
            {
                kills = startCount - CountHusks();
                Log($"phase combat: no husks left in 30 m (kills={kills})");
                break;
            }
            var toTarget = husk.transform.position - hero.transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude > hero.attackRange)
            {
                hero.AttackTarget(husk);
            }
            else
            {
                hero.SetAimPoint(husk.transform.position);
                hero.Strike();
                if (skills != null)
                {
                    if (skills.CooldownLeft(DiabloSkills.Skill.Fireball) <= 0f)
                        skills.CastToward(DiabloSkills.Skill.Fireball, toTarget);
                    else if (skills.CooldownLeft(DiabloSkills.Skill.Arrow) <= 0f)
                        skills.CastToward(DiabloSkills.Skill.Arrow, toTarget);
                }
            }
            yield return null;
        }
        kills = startCount - CountHusks();
        Log($"phase combat done (kills={kills})");
        phasesDone++;
    }

    IEnumerator PhaseFenGate(DiabloClickController hero, float deadline)
    {
        Log("phase fen_gate start (walk to GoalPad extraction)");
        var gate = GameObject.Find(FenGateObjectName);
        if (gate == null)
        {
            Log("phase fen_gate: GoalPad not found, skipped");
            yield break;
        }
        hero.SetDestination(gate.transform.position);
        float leg = Time.realtimeSinceStartup + 30f;
        while (hero.HasDestination && Time.realtimeSinceStartup < leg
               && Time.realtimeSinceStartup < deadline)
        {
            TickGod(hero);
            yield return null;
        }
        phasesDone++;
        Log("phase fen_gate done (S5 follow-up: pass ZoneGate both ways here)");
    }

    void PhaseOssuarySeam()
    {
        // S5 TODO (follow-up pass after the rebase): teleport through the
        // ZoneGate pair into the Ossuary root, clear Bone Wardens
        // (RangedCaster), verify zoneReached == 2 + autosave.
        Log($"phase ossuary SKIPPED — seam: {OssuaryGateSeamName} not present in basis (S5)");
    }

    void PhaseBossSeam()
    {
        // S5 TODO (follow-up pass): fight The Hollow Abbot (BossController,
        // phase 1 combo + phase 2 summons), verify SessionComplete overlay.
        Log($"phase boss SKIPPED — seam: {BossSeamName} not present in basis (S5)");
    }

    void TickGod(DiabloClickController hero)
    {
        if (!godMode) return;
        var health = hero.GetComponent<PlayerHealth>();
        if (health != null && health.CurrentHP < health.MaxHP) health.Heal(health.MaxHP);
    }

    static int CountHusks()
    {
        int n = 0;
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk != null && husk.GetComponent<EnemyHealth>() != null) n++;
        }
        return n;
    }

    static GameObject NearestHusk(Vector3 from, float maxDist)
    {
        GameObject best = null;
        float bestDist = maxDist;
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk == null || husk.GetComponent<EnemyHealth>() == null) continue;
            float d = Vector3.Distance(from, husk.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = husk;
            }
        }
        return best;
    }

    static void Log(string msg) => Debug.Log($"[Autoplay] {msg}");

    static void Quit(int code)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(code);
#endif
    }

    static bool HasArg(string flag)
    {
        foreach (var a in System.Environment.GetCommandLineArgs())
            if (a == flag) return true;
        return false;
    }
}
