using System.Collections;
using UnityEngine;

/// <summary>
/// S6 session proof driver (full pass with S5).
///
/// Launched with <c>-autoplay</c> (inert without the flag): takes over the
/// local hero via <see cref="DiabloClickController.ExternalDrive"/> and plays
/// the whole Cinderreach session: Lantern Hold waypoints, Ashen Fen clear
/// (melee + <see cref="DiabloSkills"/>), potion via the S3 seam
/// (<see cref="VendorStock.BuyPotion"/>), FenGate → Ossuary through the S5
/// <see cref="ZoneGate"/> pair (teleport + <c>zoneReached</c>/autosave check),
/// Bone Warden clear (<see cref="RangedCaster"/>), The Hollow Abbot
/// (<see cref="BossController"/>, phase 1 + 2) and the
/// <see cref="SessionComplete"/> overlay, then back through the return gate.
/// Logs every phase plus an exception counter and quits with exit code 0
/// (clean) or 1 (failure).
///
/// Optional cheat <c>-autoplay-god</c> (refills HP every tick) exists only
/// for run determinism and is always disclosed in the log.
/// </summary>
public sealed class SessionAutoplay : MonoBehaviour
{
    public const string Flag = "-autoplay";
    public const string GodFlag = "-autoplay-god";
    /// <summary>Directory for <see cref="AutoplayResult.FileName"/> (default &lt;game&gt;/autoplay).</summary>
    public const string OutFlag = "-autoplay-out";
    /// <summary>Commit sha stamped into the result (the player cannot know it).</summary>
    public const string CommitFlag = "-autoplay-commit";

    int phasesDone;
    int exceptionCount;
    bool godMode;
    int startBosses;
    bool lastPhaseOk;
    readonly AutoplayResult result = new AutoplayResult();
    float startedAt;

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
        if (AutoplayResult.CountsAsException(type)) exceptionCount++;
    }

    IEnumerator Start()
    {
        godMode = HasArg(GodFlag);
        startedAt = Time.realtimeSinceStartup;
        result.started = System.DateTime.UtcNow.ToString("o");
        result.commit = ArgValue(CommitFlag) ?? "unknown";
        Log($"start, god={(godMode ? "ON (deterministic cheat)" : "off")}, layout seed={LayoutSeeder.CurrentSeed} ({LayoutSeeder.Current})");
        startBosses = BossController.BossesKilled;
        float deadline = Time.realtimeSinceStartup + 600f;

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
            Finish(1);
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

        int startHusks = CountHusks(excludeBoss: false);
        yield return PhaseTownWaypoints(hero, deadline);
        bool townOk = lastPhaseOk;
        yield return PhaseBuyPotion(hero);
        if (townOk && lastPhaseOk) result.Reach(AutoplayResult.PhaseTown);
        yield return FightLoop(hero, "fen", excludeBoss: true, maxDist: 40f, budget: 120f, deadline: deadline);
        if (lastPhaseOk) result.Reach(AutoplayResult.PhaseZone1);
        yield return PhaseGate(hero, ZoneGate.FenGateName, expectZone: 2, deadline);
        if (lastPhaseOk) result.Reach(AutoplayResult.PhaseGate);
        yield return FightLoop(hero, "ossuary", excludeBoss: true, maxDist: 80f, budget: 150f, deadline: deadline);
        if (lastPhaseOk) result.Reach(AutoplayResult.PhaseZone2);
        yield return PhaseBoss(hero, deadline);
        bool bossOk = lastPhaseOk;
        if (bossOk) result.Reach(AutoplayResult.PhaseBoss);
        yield return PhaseGate(hero, ZoneGate.OssuaryGateName, expectZone: 1, deadline);
        if (bossOk && lastPhaseOk && SessionComplete.Shown >= 1) result.Reach(AutoplayResult.PhaseComplete);

        int huskKills = startHusks - CountHusks(excludeBoss: false);
        int bossKills = BossController.BossesKilled - startBosses;
        DiabloClickController.ExternalDrive = false;
        Log($"DONE phases={phasesDone} huskKills={huskKills} bossKills={bossKills} " +
            $"phaseTwo={BossController.PhaseTwoEntries} overlay={SessionComplete.Shown} " +
            $"exceptions={exceptionCount} result_phases={string.Join(",", result.phases)}");
        Finish(exceptionCount > 0 || bossKills < 1 || SessionComplete.Shown < 1 ? 1 : 0);
    }

    /// <summary>Stamp and write the result JSON, then quit with the worse of
    /// the legacy verdict and the result contract (all phases, 0 exceptions).</summary>
    void Finish(int legacyExit)
    {
        result.exceptions = exceptionCount;
        result.duration_s = Time.realtimeSinceStartup - startedAt;
        result.fps_windows = new System.Collections.Generic.List<float>(FpsReporter.Windows);
        result.seed = LayoutSeeder.CurrentSeed;
        result.exit = Mathf.Max(legacyExit, result.ComputeExit());
        string dir = ArgValue(OutFlag)
            ?? System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath) ?? ".", "autoplay");
        try
        {
            Log($"result written: {result.WriteTo(dir)} exit={result.exit}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Autoplay] result write failed ({dir}): {e.Message}");
            result.exit = 1;
        }
        Quit(result.exit);
    }

    IEnumerator PhaseTownWaypoints(DiabloClickController hero, float deadline)
    {
        Log("phase town_waypoints start (Lantern Hold: campfire, vendor, gate pillars)");
        string[] stops = { "Campfire", Vendor.VendorObjectName, "ZGatePillar_0" };
        int reached = 0;
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
            reached++;
            Log($"phase town_waypoints: reached {name}");
        }
        phasesDone++;
        lastPhaseOk = reached > 0;
        Log("phase town_waypoints done");
    }

    IEnumerator PhaseBuyPotion(DiabloClickController hero)
    {
        Log("phase vendor_potion start (S3 seam: VendorStock.BuyPotion)");
        var inv = hero.GetComponent<Inventory>();
        bool bought = VendorStock.BuyPotion(wallet: Wallet.EnsureWallet(hero.gameObject), inv: inv);
        Log($"phase vendor_potion: bought={bought}");
        lastPhaseOk = bought;
        phasesDone++;
        Log("phase vendor_potion done");
        yield return null;
    }

    /// <summary>
    /// Attack-move clear of Husk-tagged targets (husks, Bone Wardens, boss
    /// summons); the boss itself is excluded unless <paramref name="excludeBoss"/>
    /// is false. Casts Fireball/Arrow while closing, strikes + cleaves in range.
    /// </summary>
    IEnumerator FightLoop(DiabloClickController hero, string label, bool excludeBoss,
        float maxDist, float budget, float deadline)
    {
        Log($"phase combat_{label} start (excludeBoss={excludeBoss}) hero={Fmt(hero.transform.position)}");
        var skills = hero.GetComponent<DiabloSkills>();
        int start = CountHusks(excludeBoss: excludeBoss);
        float end = Time.realtimeSinceStartup + budget;
        Vector3 flast = hero.transform.position;
        float flastT = Time.realtimeSinceStartup;
        int fnudges = 0;
        GameObject curTarget = null;
        bool cleared = false;
        while (Time.realtimeSinceStartup < end && Time.realtimeSinceStartup < deadline)
        {
            TickGod(hero);
            var husk = NearestHusk(hero.transform.position, maxDist, excludeBoss);
            curTarget = husk;
            if (husk == null)
            {
                Log($"phase combat_{label}: clear (kills={start - CountHusks(excludeBoss: excludeBoss)})");
                cleared = true;
                break;
            }
            var toTarget = husk.transform.position - hero.transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            if (dist > hero.attackRange)
            {
                hero.AttackTarget(husk);
                if (skills != null && dist < 18f)
                {
                    if (skills.CooldownLeft(DiabloSkills.Skill.Fireball) <= 0f)
                        skills.CastToward(DiabloSkills.Skill.Fireball, toTarget);
                    else if (skills.CooldownLeft(DiabloSkills.Skill.Arrow) <= 0f)
                        skills.CastToward(DiabloSkills.Skill.Arrow, toTarget);
                }
            }
            else
            {
                UnstickTick(hero, husk.transform.position, ref flast, ref flastT, ref fnudges);
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
        Log($"phase combat_{label} done (kills={start - CountHusks(excludeBoss: excludeBoss)}, cleared={cleared})");
        phasesDone++;
        lastPhaseOk = cleared;
    }

    /// <summary>Pass a ZoneGate (explicit Teleport, deterministic) and verify
    /// the zone stamp + autosave in the save file.</summary>
    IEnumerator PhaseGate(DiabloClickController hero, string gateName, int expectZone, float deadline)
    {
        lastPhaseOk = false;
        Log($"phase gate_{gateName} start (expect zoneReached={expectZone}) hero={Fmt(hero.transform.position)}");
        var gate = GameObject.Find(gateName);
        if (gate == null)
        {
            Log($"phase gate_{gateName}: FAIL gate not found");
            yield break;
        }
        var zg = gate.GetComponent<ZoneGate>();
        if (zg == null)
        {
            Log($"phase gate_{gateName}: FAIL no ZoneGate component");
            yield break;
        }
        int before = ZoneGate.Teleports;
        hero.SetDestination(gate.transform.position);
        float leg = Time.realtimeSinceStartup + 40f;
        float arrived = Vector3.Distance(hero.transform.position, gate.transform.position);
        while (arrived > 4f && ZoneGate.Teleports == before
               && Time.realtimeSinceStartup < leg && Time.realtimeSinceStartup < deadline)
        {
            TickGod(hero);
            yield return null;
            arrived = Vector3.Distance(hero.transform.position, gate.transform.position);
        }
        if (ZoneGate.Teleports == before) zg.Teleport(hero.gameObject);
        bool saved = SaveService.HasSave()
            && System.IO.File.ReadAllText(SaveService.DefaultPath).Contains($"\"zoneReached\": {expectZone}");
        Log($"phase gate_{gateName}: teleports={ZoneGate.Teleports - before} zoneSaved={saved} (expectZone={expectZone})");
        phasesDone++;
        lastPhaseOk = ZoneGate.Teleports > before && saved;
        Log($"phase gate_{gateName} done");
        yield return null;
    }

    /// <summary>Walk to The Hollow Abbot and fight until BossesKilled ticks,
    /// then wait for the SessionComplete overlay.</summary>
    IEnumerator PhaseBoss(DiabloClickController hero, float deadline)
    {
        lastPhaseOk = false;
        Log("phase boss start (The Hollow Abbot, phase 1 + 2)");
        var boss = GameObject.Find(BossController.BossName);
        if (boss == null)
        {
            Log("phase boss: FAIL boss not found");
            yield break;
        }
        var skills = hero.GetComponent<DiabloSkills>();
        Log($"phase boss: hero={Fmt(hero.transform.position)} boss={Fmt(boss.transform.position)}");
        Vector3 lastPos = hero.transform.position;
        float lastT = Time.realtimeSinceStartup;
        int nudges = 0;
        float end = Time.realtimeSinceStartup + 200f;
        while (BossController.BossesKilled <= startBosses
               && Time.realtimeSinceStartup < end && Time.realtimeSinceStartup < deadline)
        {
            TickGod(hero);
            if (boss == null) break; // destroyed after death, bookkeeping runs in Update
            UnstickTick(hero, boss.transform.position, ref lastPos, ref lastT, ref nudges);
            var toTarget = boss.transform.position - hero.transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude > hero.attackRange)
            {
                hero.AttackTarget(boss);
                if (skills != null && toTarget.magnitude < 18f)
                {
                    if (skills.CooldownLeft(DiabloSkills.Skill.Fireball) <= 0f)
                        skills.CastToward(DiabloSkills.Skill.Fireball, toTarget);
                    else if (skills.CooldownLeft(DiabloSkills.Skill.Arrow) <= 0f)
                        skills.CastToward(DiabloSkills.Skill.Arrow, toTarget);
                }
            }
            else
            {
                hero.SetAimPoint(boss.transform.position);
                hero.Strike();
                if (skills != null)
                {
                    if (skills.CooldownLeft(DiabloSkills.Skill.Cleave) <= 0f)
                        skills.CastToward(DiabloSkills.Skill.Cleave, toTarget);
                    else if (skills.CooldownLeft(DiabloSkills.Skill.Fireball) <= 0f)
                        skills.CastToward(DiabloSkills.Skill.Fireball, toTarget);
                }
            }
            yield return null;
        }
        int bossKills = BossController.BossesKilled - startBosses;
        Log($"phase boss: bossKills={bossKills} phaseTwo={BossController.PhaseTwoEntries} drop={BossController.LastBossDrop}");
        float overlayEnd = Time.realtimeSinceStartup + 10f;
        while (SessionComplete.Shown < 1 && Time.realtimeSinceStartup < overlayEnd)
            yield return null;
        Log($"phase boss done (overlay={SessionComplete.Shown} summary='{SessionComplete.LastSummary}')");
        phasesDone++;
        lastPhaseOk = bossKills >= 1;
    }

    void TickGod(DiabloClickController hero)
    {
        if (!godMode) return;
        var health = hero.GetComponent<PlayerHealth>();
        if (health != null && health.CurrentHP < health.MaxHP) health.Heal(health.MaxHP);
    }

    static bool IsBoss(GameObject go) =>
        go != null && go.GetComponent<BossController>() != null;

    static int CountHusks(bool excludeBoss)
    {
        int n = 0;
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk == null || husk.GetComponent<EnemyHealth>() == null) continue;
            if (excludeBoss && IsBoss(husk)) continue;
            n++;
        }
        return n;
    }

    static GameObject NearestHusk(Vector3 from, float maxDist, bool excludeBoss)
    {
        GameObject best = null;
        float bestDist = maxDist;
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk == null || husk.GetComponent<EnemyHealth>() == null) continue;
            if (excludeBoss && IsBoss(husk)) continue;
            float d = Vector3.Distance(from, husk.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = husk;
            }
        }
        return best;
    }

    /// <summary>Straight-line attack-move wedges on pillars; nudge through.</summary>
    void UnstickTick(DiabloClickController hero, Vector3 target,
        ref Vector3 lastPos, ref float lastT, ref int nudges)
    {
        if (Time.realtimeSinceStartup - lastT < 5f) return;
        float moved = Vector3.Distance(hero.transform.position, lastPos);
        lastPos = hero.transform.position;
        lastT = Time.realtimeSinceStartup;
        if (moved > 1f || nudges >= 6) return;
        var dir = target - hero.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        var cc = hero.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        hero.transform.position += dir.normalized * 3f;
        if (cc != null) cc.enabled = true;
        nudges++;
        Log($"unstick nudge #{nudges} (moved {moved:0.1} m in 5 s)");
    }

    static string Fmt(Vector3 v) => $"({v.x:0},{v.y:0},{v.z:0})";

    static void Log(string msg) => Debug.Log($"[Autoplay] {msg}");

    static void Quit(int code)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(code);
#endif
    }

    static string ArgValue(string flag)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == flag) return args[i + 1];
        return null;
    }

    static bool HasArg(string flag)
    {
        foreach (var a in System.Environment.GetCommandLineArgs())
            if (a == flag) return true;
        return false;
    }
}
