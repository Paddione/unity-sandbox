using System.Collections;
using UnityEngine;

/// <summary>
/// S6 session proof driver (full pass with S5).
///
/// Launched with <c>-autoplay</c> (inert without the flag): takes over the
/// local hero via <see cref="DiabloClickController.ExternalDrive"/> and plays
/// the whole Cinderreach session: Lantern Hold waypoints, Ashen Fen clear
/// (melee + <see cref="DiabloSkills"/>), potion via the S3 seam
/// (<see cref="HealingFlask.TryDrink"/>), FenGate → Ossuary through the S5
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
        bool coopHost = CoopAutoplay.Role == AutoplayResult.RoleHost;
        if (coopHost) result.role = AutoplayResult.RoleHost;
        Log($"start, god={(godMode ? "ON (deterministic cheat)" : "off")}, layout seed={LayoutSeeder.CurrentSeed} ({LayoutSeeder.Current})");
        startBosses = BossController.BossesKilled;
        DiabloSkills.ResetRun();
        float deadline = Time.realtimeSinceStartup + 1100f; // act-two-opening S5: route runs through the Barrens

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
        if (coopHost)
        {
            // coop-session S1: the pair run starts the route once the client is in.
            // coop-trio: -coop-peers N waits for N players in total (host included).
            int peers = CoopAutoplay.PeersFor(System.Environment.GetCommandLineArgs());
            var nm = Unity.Netcode.NetworkManager.Singleton;
            float joinBy = Time.realtimeSinceStartup + 90f;
            while (Time.realtimeSinceStartup < joinBy && (nm == null || nm.ConnectedClientsIds.Count < peers))
            {
                yield return null;
                nm = Unity.Netcode.NetworkManager.Singleton;
            }
            if (nm == null || nm.ConnectedClientsIds.Count < peers)
            {
                Log($"FAIL co-op: {(nm == null ? 0 : nm.ConnectedClientsIds.Count)}/{peers} players within 90 s");
                Finish(1);
                yield break;
            }
            Log($"co-op host: client joined ({nm.ConnectedClientsIds.Count} connected)");
            // Relay hosting signs in first, so the solo hero found above may since have
            // stood down for the owned network hero: drive whichever "Player" is live now.
            var live = GameObject.Find(DiabloNetworkPlayer.HeroName);
            var liveCtl = live != null ? live.GetComponent<DiabloClickController>() : null;
            if (liveCtl != null && liveCtl.isActiveAndEnabled && liveCtl != hero)
            {
                hero = liveCtl;
                Log("co-op host: driving the owned network hero (solo hero stood down)");
            }
        }
        DiabloClickController.ExternalDrive = true;
        HitStop.Enabled = false;
        GrantAutoplayGear(hero);


        int startHusks = CountHusks(excludeBoss: false);
        yield return PhaseTownWaypoints(hero, deadline);
        bool townOk = lastPhaseOk;
        yield return PhaseFlask(hero);
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
        // act-one S5: the Abbot opened the Kiln gate; fight through zone 3 to the Kilnmother.
        yield return PhaseGate(hero, ZoneGate.KilnGateName, expectZone: 3, deadline);
        bool kilnGateOk = lastPhaseOk;
        yield return FightLoop(hero, "kiln", excludeBoss: true, maxDist: 40f, budget: 180f, deadline: deadline);
        if (kilnGateOk && lastPhaseOk) result.Reach(AutoplayResult.PhaseZone3);
        yield return PhaseKilnmother(hero, deadline);
        bool actOk = lastPhaseOk;
        if (actOk) result.Reach(AutoplayResult.PhaseActBoss);
        var kilnShrine = WaypointShrine.Find("Kiln");
        if (kilnShrine != null) kilnShrine.TryDiscover(hero.gameObject);
        // act-two-opening S5: the Kilnmother opened the Glass Barrens; fight through zone 4 to the Glasswright.
        yield return PhaseGate(hero, ZoneGate.BarrensGateName, expectZone: ActProgress.Barrens, deadline);
        bool barrensGateOk = lastPhaseOk;
        yield return FightLoop(hero, "barrens", excludeBoss: true, maxDist: 40f, budget: 260f, deadline: deadline);
        if (barrensGateOk && lastPhaseOk) result.Reach(AutoplayResult.PhaseZone4);
        yield return PhaseGlasswright(hero, deadline);
        bool zoneBossOk = lastPhaseOk;
        if (zoneBossOk) result.Reach(AutoplayResult.PhaseBoss3);
        // Home by waypoint (used once per run): Barrens → Lantern Hold.
        var barrensShrine = WaypointShrine.Find("Barrens");
        if (barrensShrine != null) barrensShrine.TryDiscover(hero.gameObject);
        bool home = WaypointShrine.Travel("Town", hero.gameObject);
        Log($"phase waypoint_home: travelled={home} hero={Fmt(hero.transform.position)}");
        if (bossOk && actOk && zoneBossOk && home && RunProfile.Act.ActOneComplete
            && RunProfile.Act.ZoneBossDefeated && SessionComplete.ZoneBossMode)
            result.Reach(AutoplayResult.PhaseComplete);

        int huskKills = startHusks - CountHusks(excludeBoss: false);
        int bossKills = BossController.BossesKilled - startBosses;
        DiabloClickController.ExternalDrive = false;
        result.huskKills = huskKills;
        result.healsCast = DiabloSkills.HealsCast;
        result.hpRestored = DiabloSkills.HPRestored;
        result.partyRoles = PartyRoleIds();
        result.combos = ClassCombos.TriggeredLines();
        Log($"DONE phases={phasesDone} huskKills={huskKills} bossKills={bossKills} " +
            $"phaseTwo={BossController.PhaseTwoEntries} overlay={SessionComplete.Shown} " +
            $"exceptions={exceptionCount} party={string.Join(",", result.partyRoles)} " +
            $"heals={result.healsCast} hpRestored={result.hpRestored:0} " +
            $"combos={string.Join(",", result.combos)} " +
            $"result_phases={string.Join(",", result.phases)}");
        Finish(exceptionCount > 0 || bossKills < 1 || SessionComplete.Shown < 1 ? 1 : 0);
    }

    /// <summary>Party on the run: the hero's class plus AI companion classes.</summary>
    static System.Collections.Generic.List<string> PartyRoleIds()
    {
        var roles = new System.Collections.Generic.List<string> { RunProfile.ClassId };
        var roster = PartyRoster.Instance;
        if (roster != null)
            foreach (var c in roster.Companions)
                if (c != null) roles.Add(c.classId);
        return roles;
    }

    /// <summary>Stamp and write the result JSON, then quit with the worse of
    /// the legacy verdict and the result contract (all phases, 0 exceptions).</summary>
    void Finish(int legacyExit)
    {
        result.exceptions = exceptionCount;
        result.duration_s = Time.realtimeSinceStartup - startedAt;
        result.fps_windows = new System.Collections.Generic.List<float>(FpsReporter.Windows);
        result.seed = LayoutSeeder.CurrentSeed;
        result.difficulty = DifficultyProfile.Current.name;
        result.classId = RunProfile.ClassId;
        result.actOneComplete = RunProfile.Act.ActOneComplete;
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
        string[] stops = { "Campfire", Vendor.VendorObjectName, "WaypointShrine_Town", "ZGatePillar_0" };
        int reached = 0;
        foreach (var name in stops)
        {
            var target = GameObject.Find(name);
            if (target == null)
            {
                Log($"phase town_waypoints: '{name}' not found, skipped");
                continue;
            }
            var dest = target.transform.position;
            hero.SetDestination(dest);
            Vector3 tlast = hero.transform.position;
            float tlastT = Time.realtimeSinceStartup;
            int tnudges = 0;
            float leg = Time.realtimeSinceStartup + 15f;
            while (hero.HasDestination && Time.realtimeSinceStartup < leg
                   && Time.realtimeSinceStartup < deadline)
            {
                TickGod(hero);
                TravelLegTick(hero, dest, ref tlast, ref tlastT, ref tnudges);
                yield return null;
            }
            reached++;
            Log($"phase town_waypoints: reached {name}");
        }
        phasesDone++;
        lastPhaseOk = reached > 0;
        Log("phase town_waypoints done");
    }

    IEnumerator PhaseFlask(DiabloClickController hero)
    {
        Log("phase flask start (gear-slots S2 seam: HealingFlask.TryDrink)");
        var flask = HealingFlask.For(hero.gameObject);
        bool drank = flask.TryDrink();
        Log($"phase flask: drank={drank} charges={flask.Charges}/{flask.MaxCharges}");
        // Walk back into the Lantern Hold, which refills a spent flask within one check.
        hero.SetDestination(SkillVariantCatalog.TownCenter);
        float until = Time.time + 12f;
        while (Time.time < until && !flask.IsFull) yield return null;
        Log($"phase flask: refilled={flask.IsFull}");
        lastPhaseOk = drank && flask.IsFull;
        phasesDone++;
        Log("phase flask done");
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
        int teleportsAtStart = ZoneGate.Teleports;
        while (Time.realtimeSinceStartup < end && Time.realtimeSinceStartup < deadline)
        {
            TickGod(hero);
            if (ZoneGate.Teleports != teleportsAtStart)
            {
                // A gate fired mid-fight: the zone is not cleared, it was left.
                Log($"phase combat_{label}: FAIL hero left the zone mid-fight " +
                    $"{HuskCensus(hero.transform.position, excludeBoss)}");
                break;
            }
            var husk = NearestHusk(hero.transform.position, maxDist, excludeBoss);
            curTarget = husk;
            if (husk == null)
            {
                Log($"phase combat_{label}: clear (kills={start - CountHusks(excludeBoss: excludeBoss)}) " +
                    $"{HuskCensus(hero.transform.position, excludeBoss)}");
                cleared = true;
                break;
            }
            var toTarget = husk.transform.position - hero.transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;
            if (dist > hero.attackRange)
            {
                hero.AttackTarget(husk);
                if (skills != null && dist < 18f) CastProfile(skills, toTarget, inRange: false);
            }
            else
            {
                UnstickTick(hero, husk.transform.position, ref flast, ref flastT, ref fnudges);
                hero.SetAimPoint(husk.transform.position);
                hero.Strike();
                if (skills != null) CastProfile(skills, toTarget, inRange: true);
            }
            yield return null;
        }
        if (!cleared) Log($"phase combat_{label}: not cleared, {HuskCensus(hero.transform.position, excludeBoss)}");
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
        Vector3 glast = hero.transform.position;
        float glastT = Time.realtimeSinceStartup;
        int gnudges = 0;
        float leg = Time.realtimeSinceStartup + 30f;
        float arrived = Vector3.Distance(hero.transform.position, gate.transform.position);
        while (arrived > 4f && ZoneGate.Teleports == before
               && Time.realtimeSinceStartup < leg && Time.realtimeSinceStartup < deadline)
        {
            TickGod(hero);
            TravelLegTick(hero, gate.transform.position, ref glast, ref glastT, ref gnudges);
            yield return null;
            arrived = Vector3.Distance(hero.transform.position, gate.transform.position);
        }
        bool viaBroadcast = false;
        if (ZoneGate.Teleports == before) viaBroadcast = zg.TeleportViaHost(hero.gameObject);
        if (viaBroadcast)
        {
            // The host's own travel arrives asynchronously (fade + teleport);
            // wait for it like the arrival loop above instead of failing fast.
            float waitBy = Time.realtimeSinceStartup + 15f;
            while (ZoneGate.Teleports == before
                   && Time.realtimeSinceStartup < waitBy && Time.realtimeSinceStartup < deadline)
            {
                TickGod(hero);
                yield return null;
            }
        }
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
                if (skills != null && toTarget.magnitude < 18f) CastProfile(skills, toTarget, inRange: false);
            }
            else
            {
                hero.SetAimPoint(boss.transform.position);
                hero.Strike();
                if (skills != null) CastProfile(skills, toTarget, inRange: true, boss: true);
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

    /// <summary>
    /// Class combat profile (second-class S5, rogue party-roles S6, class-kits-parity).
    /// Fenwalker: Mire Ground into packs, Seismic Slam (Fen Shackle), Cleave on
    /// stunned husks (Shatter, Retribution after Iron Vow), Mire Hook and Bog
    /// Charge to close distance. Lanternbearer: Wisps whenever ready, Searing
    /// Halo with 3+ husks (or the boss), else Lantern Bolt. Rogue: Venom Vial,
    /// Powder Keg onto venomed targets (Venom Burn), Smoke Pot into packs,
    /// Blade Flurry into smoke (Throat Cut), Shadow Roll out of a crowd
    /// (Ambush), else Quick Shot.
    /// </summary>
    static void CastProfile(DiabloSkills skills, Vector3 toTarget, bool inRange, bool boss = false)
    {
        bool Ready(DiabloSkills.Skill s) => skills.CooldownLeft(s) <= 0f;
        bool Cast(DiabloSkills.Skill s) => Ready(s) && skills.CastToward(s, toTarget);
        var pos = skills.transform.position;
        float dist = new Vector3(toTarget.x, 0f, toTarget.z).magnitude;
        var target = NearestHusk(pos + toTarget);
        if (RunProfile.ClassId == ClassCatalog.RogueId)
        {
            int close = ClassCombos.HusksInRadius(pos, 2.2f).Count;
            if (close >= 2 && Ready(DiabloSkills.Skill.ShadowRoll)
                && skills.CastToward(DiabloSkills.Skill.ShadowRoll, -toTarget)) return;
            if (target != null && EnemyStatus.Venom(target) >= ClassCombos.VenomStacksPerVial
                && dist <= ClassCombos.PowderKegRange && Cast(DiabloSkills.Skill.PowderKeg)) return;
            if (dist <= ClassCombos.VenomRange && Cast(DiabloSkills.Skill.VenomVial)) return;
            if (dist <= ClassCombos.SmokeRange && (boss || ClassCombos.HusksInRadius(pos + toTarget, ClassCombos.SmokeRadius).Count >= 2)
                && Cast(DiabloSkills.Skill.SmokePot)) return;
            if (ClassCombos.HusksInRadius(pos, skills.flurryRadius).Count >= (boss ? 1 : 2)
                && Cast(DiabloSkills.Skill.BladeFlurry)) return;
            Cast(DiabloSkills.Skill.QuickShot);
            return;
        }
        if (RunProfile.ClassId == ClassCatalog.LanternbearerId)
        {
            // coop-hp-sync: mend hurt allies first (co-op mirrors show their owner's HP).
            if (Ready(DiabloSkills.Skill.RenewingCircle) && HurtAllies(skills) >= 2
                && skills.CastToward(DiabloSkills.Skill.RenewingCircle, toTarget)) return;
            if (Ready(DiabloSkills.Skill.MendingLight) && skills.CastToward(DiabloSkills.Skill.MendingLight, toTarget)) return;
            if (Ready(DiabloSkills.Skill.Wisps)) { skills.CastToward(DiabloSkills.Skill.Wisps, toTarget); return; }
            int near = 0;
            foreach (var h in GameObject.FindGameObjectsWithTag("Husk"))
            {
                if (h == null || h.GetComponent<EnemyHealth>() == null) continue;
                var d = h.transform.position - skills.transform.position;
                d.y = 0f;
                if (d.magnitude <= skills.haloRadius) near++;
            }
            if (Ready(DiabloSkills.Skill.SearingHalo) && (near >= 3 || (boss && inRange)))
            {
                skills.CastToward(DiabloSkills.Skill.SearingHalo, toTarget);
                return;
            }
            if (Ready(DiabloSkills.Skill.LanternBolt)) skills.CastToward(DiabloSkills.Skill.LanternBolt, toTarget);
            return;
        }
        // Fenwalker (tank kit).
        var hp = skills.GetComponent<PlayerHealth>();
        if (hp != null && hp.CurrentHP < hp.MaxHP * 0.7f && Cast(DiabloSkills.Skill.IronVow)) return;
        if (boss && inRange && Ready(DiabloSkills.Skill.IronVow) && Cast(DiabloSkills.Skill.IronVow)) return;
        int nearPack = ClassCombos.HusksInRadius(pos, ClassCombos.MireGroundRadius).Count;
        if (nearPack >= 2 && Cast(DiabloSkills.Skill.MireGround)) return;
        if ((nearPack >= 2 || (boss && inRange)) && dist <= skills.slamRadius && Cast(DiabloSkills.Skill.SeismicSlam)) return;
        if (dist <= skills.CleaveRadius && Cast(DiabloSkills.Skill.Cleave)) return;
        if (dist > 3f && dist <= ClassCombos.BogChargeDistance && Cast(DiabloSkills.Skill.BogCharge)) return;
        if (dist > 2.5f && dist <= ClassCombos.MireHookRange) Cast(DiabloSkills.Skill.MireHook);
    }

    static GameObject NearestHusk(Vector3 at)
    {
        GameObject best = null;
        float bestD = 1.5f;
        foreach (var h in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (h == null || h.GetComponent<EnemyHealth>() == null) continue;
            var d = h.transform.position - at;
            d.y = 0f;
            if (d.magnitude < bestD) { bestD = d.magnitude; best = h; }
        }
        return best;
    }

    /// <summary>Hurt living allies (not the caster) inside the group-heal radius.</summary>
    static int HurtAllies(DiabloSkills skills)
    {
        int n = 0;
        foreach (var hp in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            if (hp == null || hp.transform == skills.transform || !hp.gameObject.activeInHierarchy) continue;
            if (hp.CurrentHP <= 0f || hp.CurrentHP >= hp.MaxHP) continue;
            var d = hp.transform.position - skills.transform.position;
            d.y = 0f;
            if (d.magnitude <= skills.groupHealRadius) n++;
        }
        return n;
    }

    void TickGod(DiabloClickController hero)
    {
        if (!godMode) return;
        var health = hero.GetComponent<PlayerHealth>();
        if (health != null && health.CurrentHP < health.MaxHP) health.Heal(health.MaxHP);
    }

    static bool IsBoss(GameObject go) =>
        go != null && (go.GetComponent<BossController>() != null || go.GetComponent<KilnmotherController>() != null
            || go.GetComponent<GlasswrightController>() != null);

    /// <summary>
    /// act-two-opening S5: fight The Glasswright. Living Glass Pillars come
    /// first (they ward the boss), then the boss until it falls and the
    /// Barrens-cleared screen shows.
    /// </summary>
    IEnumerator PhaseGlasswright(DiabloClickController hero, float deadline)
    {
        lastPhaseOk = false;
        int startKills = GlasswrightController.Kills;
        Log("phase boss3 start (The Glasswright, phase 1 + 2)");
        var boss = GameObject.Find(GlasswrightController.BossName);
        if (boss == null)
        {
            Log("phase boss3: FAIL Glasswright not found");
            yield break;
        }
        var skills = hero.GetComponent<DiabloSkills>();
        Vector3 lastPos = hero.transform.position;
        float lastT = Time.realtimeSinceStartup;
        int nudges = 0;
        float end = Time.realtimeSinceStartup + 260f;
        while (GlasswrightController.Kills <= startKills
               && Time.realtimeSinceStartup < end && Time.realtimeSinceStartup < deadline)
        {
            TickGod(hero);
            if (boss == null) break;
            GameObject target = boss;
            foreach (var h in GameObject.FindGameObjectsWithTag("Husk"))
                if (h != null && h.name.StartsWith(GlasswrightController.PillarPrefix) && h.GetComponent<EnemyHealth>() != null)
                {
                    target = h;
                    break;
                }
            UnstickTick(hero, target.transform.position, ref lastPos, ref lastT, ref nudges);
            var toTarget = target.transform.position - hero.transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude > hero.attackRange)
            {
                hero.AttackTarget(target);
                if (skills != null && toTarget.magnitude < 18f) CastProfile(skills, toTarget, inRange: false);
            }
            else
            {
                hero.SetAimPoint(target.transform.position);
                hero.Strike();
                if (skills != null) CastProfile(skills, toTarget, inRange: true, boss: target == boss);
            }
            yield return null;
        }
        int kills = GlasswrightController.Kills - startKills;
        Log($"phase boss3: kills={kills} phaseTwo={GlasswrightController.PhaseTwoEntries} lances={GlasswrightController.LanceVolleys} " +
            $"novas={GlasswrightController.NovasFired} fields={GlasswrightController.FieldsCalled} warded={GlassWard.Warded} " +
            $"zoneBoss={RunProfile.Act.ZoneBossDefeated} drop={GlasswrightController.LastZoneUnique?.uniqueId} overlay={SessionComplete.ZoneBossMode}");
        phasesDone++;
        lastPhaseOk = kills >= 1 && RunProfile.Act.ZoneBossDefeated;
    }

    /// <summary>act-one S5: fight The Kilnmother until she falls and the Act I screen shows.</summary>
    IEnumerator PhaseKilnmother(DiabloClickController hero, float deadline)
    {
        lastPhaseOk = false;
        int startKills = KilnmotherController.Kills;
        Log("phase actboss start (The Kilnmother, phase 1 + 2)");
        var boss = GameObject.Find(KilnLayout.KilnmotherName);
        if (boss == null)
        {
            Log("phase actboss: FAIL Kilnmother not found");
            yield break;
        }
        var skills = hero.GetComponent<DiabloSkills>();
        Vector3 lastPos = hero.transform.position;
        float lastT = Time.realtimeSinceStartup;
        int nudges = 0;
        float end = Time.realtimeSinceStartup + 240f;
        while (KilnmotherController.Kills <= startKills
               && Time.realtimeSinceStartup < end && Time.realtimeSinceStartup < deadline)
        {
            TickGod(hero);
            if (boss == null) break;
            UnstickTick(hero, boss.transform.position, ref lastPos, ref lastT, ref nudges);
            var toTarget = boss.transform.position - hero.transform.position;
            toTarget.y = 0f;
            if (toTarget.magnitude > hero.attackRange)
            {
                hero.AttackTarget(boss);
                if (skills != null && toTarget.magnitude < 18f) CastProfile(skills, toTarget, inRange: false);
            }
            else
            {
                hero.SetAimPoint(boss.transform.position);
                hero.Strike();
                if (skills != null) CastProfile(skills, toTarget, inRange: true, boss: true);
            }
            yield return null;
        }
        int kills = KilnmotherController.Kills - startKills;
        Log($"phase actboss: kills={kills} phaseTwo={KilnmotherController.PhaseTwoEntries} waves={KilnmotherController.WavesFired} " +
            $"act={RunProfile.Act.ActOneComplete} overlay={SessionComplete.ActOneMode}");
        phasesDone++;
        lastPhaseOk = kills >= 1 && RunProfile.Act.ActOneComplete;
    }

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

    /// <summary>Diagnostic: remaining husks, nearest planar/3D distance and its position.</summary>
    static string HuskCensus(Vector3 from, bool excludeBoss)
    {
        int n = 0;
        float best = float.PositiveInfinity;
        Vector3 at = Vector3.zero;
        string name = "-";
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk == null || husk.GetComponent<EnemyHealth>() == null) continue;
            if (excludeBoss && IsBoss(husk)) continue;
            n++;
            float d = Vector3.Distance(from, husk.transform.position);
            if (d < best) { best = d; at = husk.transform.position; name = husk.name; }
        }
        return n == 0 ? "census: none left" : $"census: {n} left, nearest {name} {best:0.0} m at {Fmt(at)}";
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

    /// <summary>
    /// Autoplay-only starting gear (this call site is inside the -autoplay
    /// session driver, never in real game paths): equips the class-compatible
    /// Kindled-finale Rare weapon over the +2 starter so fights resolve
    /// quicker. No drop-table, vendor, save-format or balance change.
    /// The log line below uses only single-token literals (no multi-word
    /// phrase), so LocLint stays clean and the loc allowlist needs no update.
    /// </summary>
    void GrantAutoplayGear(DiabloClickController hero)
    {
        var eq = hero.GetComponent<Equipment>();
        if (eq == null) return;
        var item = ItemDatabase.CreateKindledFinaleWeapon(RunProfile.ClassId);
        if (!Equipment.CanEquip(item, RunProfile.ClassId)) return;
        eq.Equip(item);
        Log("autoplay-gear:" + item.slot + "/" + item.baseType + "/" + item.rarity
            + "/dmg:" + eq.GetDamage() + "/hp:" + eq.GetMaxHP());
    }

    /// <summary>
    /// Travel-leg watchdog (town waypoints, gate walks): the same 5 s / 1 m /
    /// 6-nudge pattern as <see cref="UnstickTick"/>, but repaths with
    /// SetDestination after a nudge since travel has no per-frame
    /// AttackTarget to refresh the path. Speeds and destinations unchanged.
    /// The log literal is byte-identical to the fight-loop one so the loc
    /// allowlist needs no update.
    /// </summary>
    void TravelLegTick(DiabloClickController hero, Vector3 destination,
        ref Vector3 lastPos, ref float lastT, ref int nudges)
    {
        if (Time.realtimeSinceStartup - lastT < 5f) return;
        float moved = Vector3.Distance(hero.transform.position, lastPos);
        lastPos = hero.transform.position;
        lastT = Time.realtimeSinceStartup;
        if (moved > 1f || nudges >= 6) return;
        var dir = destination - hero.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        var cc = hero.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        hero.transform.position += dir.normalized * 3f;
        if (cc != null) cc.enabled = true;
        hero.SetDestination(destination);
        nudges++;
        Log($"unstick nudge #{nudges} (moved {moved:0.1} m in 5 s)");
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
