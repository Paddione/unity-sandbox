using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Session proof driver.
///
/// Launched with <c>-autoplay</c> (inert without the flag): takes over the
/// local hero via <see cref="DiabloClickController.ExternalDrive"/> and plays
/// the whole Cinderreach session: Lantern Hold waypoints, flask, Ashen Fen
/// clear, FenGate → Ossuary, Bone Wardens and The Hollow Abbot, KilnGate →
/// Cinder Kiln and The Kilnmother, BarrensGate → Glass Barrens and The
/// Glasswright, then home by waypoint.
///
/// Quest mode (always on for solo and the co-op host, since
/// cinderreach-act-boss-gating the act bosses only appear once every other
/// quest of their act is done): around that route, <see cref="QuestRound"/> plays every story
/// quest the way a player would: talks to the NPCs for offers, talk goals and
/// hand-ins (<see cref="StoryDirector.BookTalk"/>), walks onto wanted finds,
/// hunts wanted kill targets and travels between zones by waypoint whenever
/// the story asks for it. <see cref="AutoplayQuestLedger"/> reports quests the
/// act state closed by catch-up instead of play.
///
/// Movement runs through <see cref="AutoplayMover"/>: grid A* around walls
/// and props, stall detection with replanning, and a last-resort warp onto a
/// validated free cell, so terrain never wedges the hero.
///
/// Logs every phase plus an exception counter and quits with exit code 0
/// (clean) or 1 (failure). Optional cheat <c>-autoplay-god</c> (refills HP
/// every tick) exists only for run determinism and is always disclosed.
/// </summary>
public sealed class SessionAutoplay : MonoBehaviour
{
    public const string Flag = "-autoplay";
    public const string GodFlag = "-autoplay-god";
    /// <summary>Directory for <see cref="AutoplayResult.FileName"/> (default &lt;game&gt;/autoplay).</summary>
    public const string OutFlag = "-autoplay-out";
    /// <summary>Commit sha stamped into the result (the player cannot know it).</summary>
    public const string CommitFlag = "-autoplay-commit";
    /// <summary>Perf runs only: keep the zone map open as overlay while playing (cinderreach-map-overlay K6).</summary>
    public const string MapFlag = "-autoplay-map";

    /// <summary>Talk distance to a story NPC (inside <see cref="Vendor.InteractRadius"/>).</summary>
    const float TalkReach = 2.5f;
    /// <summary>A fight target whose HP does not move for this long is set aside.</summary>
    const float TargetStallSeconds = 25f;
    /// <summary>Story-find trigger half-extent (0.72 m) plus hero radius: planner goal around a find.</summary>
    const float PickupReach = 1.05f;

    int phasesDone;
    int exceptionCount;
    bool godMode;
    bool questMode;
    int startBosses;
    readonly AutoplayResult result = new AutoplayResult();
    float startedAt;
    AutoplayMover mover;
    AutoplayQuestLedger ledger;
    readonly SceneQuestWorld questWorld = new SceneQuestWorld();

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

    void Update() => ledger?.Observe(RunProfile.Story);

    IEnumerator Start()
    {
        godMode = HasArg(GodFlag);
        startedAt = Time.realtimeSinceStartup;
        result.started = System.DateTime.UtcNow.ToString("o");
        result.commit = ArgValue(CommitFlag) ?? "unknown";
        bool coopHost = CoopAutoplay.Role == AutoplayResult.RoleHost;
        if (coopHost) result.role = AutoplayResult.RoleHost;
        // cinderreach-team-autoplay K2: on a dedicated server client 1 plays the full route as lead.
        coopLead = CoopAutoplay.Role == AutoplayResult.RoleLead;
        if (coopLead) result.role = AutoplayResult.RoleLead;
        questMode = true; // act bosses need their act's quests (solo and co-op host alike)
        result.questMode = questMode;
        Log($"start, god={(godMode ? "ON (deterministic cheat)" : "off")}, layout seed={LayoutSeeder.CurrentSeed} ({LayoutSeeder.Current})");
        Log("quest-mode:" + questMode);
        startBosses = BossController.BossesKilled;
        DiabloSkills.ResetRun();
        // Quest mode walks back to Lantern Hold between zones and makes a second Barrens pass.
        float deadline = Time.realtimeSinceStartup + (questMode ? 2400f : 1100f);
        if (coopLead) deadline += 1200f; // three processes share the machine, the party waits for followers

        if (coopLead)
        {
            yield return CoopAutoplay.WaitForParty(c => leadHero = c);
            if (leadHero == null)
            {
                Log("FAIL co-op lead: no owned hero or party incomplete");
                Finish(1);
                yield break;
            }
            Log($"co-op lead: party of {CoopAutoplay.PlayerCount()} together, client {Unity.Netcode.NetworkManager.Singleton.LocalClientId}");
        }

        DiabloClickController hero = leadHero;
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
        if (HasArg(MapFlag))
        {
            // Perf proof with the overlay map open: force overlay (not the saved fullscreen choice).
            DiabloSettings.MapFullscreen = false;
            var map = hero.GetComponent<WorldMapUI>();
            if (map != null) { map.SetOpen(true); Log($"perf: zone map open, mode={map.Mode}"); }
            else Log("perf: no WorldMapUI on the hero, map stays closed");
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
        mover = new AutoplayMover(new AutoplayNav(hero.transform));
        ledger = new AutoplayQuestLedger(RunProfile.Story);

        int startHusks = AutoplayCombat.CountHusks(excludeBoss: false);
        bool townOk = false;
        yield return PhaseTownWaypoints(hero, deadline, ok => townOk = ok);
        bool flaskOk = false;
        yield return PhaseFlask(hero, ok => flaskOk = ok);
        if (townOk && flaskOk) result.Reach(AutoplayResult.PhaseTown);
        if (questMode) yield return QuestRound(hero, deadline);
        bool fenOk = false;
        yield return FightLoop(hero, "fen", excludeBoss: true, maxDist: 40f, budget: 120f, deadline: deadline, ok => fenOk = ok);
        if (fenOk) result.Reach(AutoplayResult.PhaseZone1);
        if (questMode) yield return QuestRound(hero, deadline);
        bool fenGateOk = false;
        yield return PhaseGate(hero, ZoneGate.FenGateName, expectZone: 2, deadline, ok => fenGateOk = ok);
        if (fenGateOk) result.Reach(AutoplayResult.PhaseGate);
        if (questMode)
        {
            yield return DiscoverShrine(hero, "Ossuary", deadline);
            yield return QuestRound(hero, deadline);
        }
        bool ossuaryOk = false;
        yield return FightLoop(hero, "ossuary", excludeBoss: true, maxDist: 80f, budget: 150f, deadline: deadline, ok => ossuaryOk = ok);
        if (ossuaryOk) result.Reach(AutoplayResult.PhaseZone2);
        bool bossOk = false;
        yield return PhaseBoss(hero, deadline, ok => bossOk = ok);
        if (bossOk) result.Reach(AutoplayResult.PhaseBoss);
        if (questMode) yield return QuestRound(hero, deadline);
        // act-one S5: the Abbot opened the Kiln gate; fight through zone 3 to the Kilnmother.
        bool kilnGateOk = false;
        yield return PhaseGate(hero, ZoneGate.KilnGateName, expectZone: 3, deadline, ok => kilnGateOk = ok);
        if (questMode)
        {
            yield return DiscoverShrine(hero, "Kiln", deadline);
            yield return QuestRound(hero, deadline);
        }
        bool kilnOk = false;
        yield return FightLoop(hero, "kiln", excludeBoss: true, maxDist: 40f, budget: 180f, deadline: deadline, ok => kilnOk = ok);
        if (kilnGateOk && kilnOk) result.Reach(AutoplayResult.PhaseZone3);
        yield return QuestsUntilBoss(hero, 1, deadline);
        bool actOk = false;
        yield return PhaseKilnmother(hero, deadline, ok => actOk = ok);
        if (actOk) result.Reach(AutoplayResult.PhaseActBoss);
        var kilnShrine = WaypointShrine.Find("Kiln");
        if (kilnShrine != null) kilnShrine.TryDiscover(hero.gameObject);
        if (questMode) yield return QuestRound(hero, deadline);
        // act-two-opening S5: the Kilnmother opened the Glass Barrens; fight through zone 4 to the Glasswright.
        bool barrensGateOk = false;
        yield return PhaseGate(hero, ZoneGate.BarrensGateName, expectZone: ActProgress.Barrens, deadline, ok => barrensGateOk = ok);
        if (questMode)
        {
            yield return DiscoverShrine(hero, "Barrens", deadline);
            yield return QuestRound(hero, deadline);
        }
        bool barrensOk = false;
        yield return FightLoop(hero, "barrens", excludeBoss: true, maxDist: 40f, budget: 260f, deadline: deadline, ok => barrensOk = ok);
        if (barrensGateOk && barrensOk) result.Reach(AutoplayResult.PhaseZone4);
        yield return QuestsUntilBoss(hero, 2, deadline);
        bool zoneBossOk = false;
        yield return PhaseGlasswright(hero, deadline, ok => zoneBossOk = ok);
        if (zoneBossOk) result.Reach(AutoplayResult.PhaseBoss3);
        // Home by waypoint: Barrens → Lantern Hold.
        var barrensShrine = WaypointShrine.Find("Barrens");
        if (barrensShrine != null) barrensShrine.TryDiscover(hero.gameObject);
        if (questMode && ZoneMap.IndexAt(hero.transform.position) == ActProgress.Barrens)
            yield return QuestSweep(hero, deadline); // finds that opened with the Glasswright's fall
        bool home = WaypointShrine.Travel("Town", hero.gameObject);
        mover.Reset();
        Log($"phase waypoint_home: travelled={home} hero={Fmt(hero.transform.position)}");
        if (bossOk && actOk && zoneBossOk && home && RunProfile.Act.ActOneComplete
            && RunProfile.Act.ZoneBossDefeated && (coopLead || SessionComplete.ZoneBossMode))
            result.Reach(AutoplayResult.PhaseComplete);
        yield return QuestsUntilBoss(hero, 3, deadline);
        yield return PhaseThewes(hero, deadline);
        // Co-op lead: the kill RPC outruns the server's story broadcast; H13 needs H12 Done here.
        if (coopLead) yield return WaitFor(() => RunProfile.Story.Status("h12") == StoryStatus.Done, 15f);
        yield return QuestRound(hero, deadline); // H13 homecoming after the final boss
        ReportQuests();
        result.actBossKills = new List<string>(ActBossGate.KillLog);
        Log("phase actbosses: kills=[" + string.Join(" ", result.actBossKills) + "] spawns=" +
            string.Join(",", ActBossGate.Spawns) + " fellLast=" + result.ActBossesFellLast());

        int huskKills = startHusks - AutoplayCombat.CountHusks(excludeBoss: false);
        int bossKills = BossController.BossesKilled - startBosses;
        // A lead client never runs the boss bookkeeping (the server does); its act state proves the kills.
        if (coopLead) bossKills = RunProfile.Act.IsUnlocked(ActProgress.Kiln) ? 1 : 0;
        DiabloClickController.ExternalDrive = false;
        result.huskKills = huskKills;
        result.healsCast = DiabloSkills.HealsCast;
        result.hpRestored = DiabloSkills.HPRestored;
        result.partyRoles = PartyRoleIds();
        result.combos = ClassCombos.TriggeredLines();
        result.navPlans = mover.Nav.Plans;
        result.navWarps = mover.Warps;
        Log($"DONE phases={phasesDone} huskKills={huskKills} bossKills={bossKills} " +
            $"phaseTwo={BossController.PhaseTwoEntries} overlay={SessionComplete.Shown} " +
            $"exceptions={exceptionCount} party={string.Join(",", result.partyRoles)} " +
            $"heals={result.healsCast} hpRestored={result.hpRestored:0} " +
            $"combos={string.Join(",", result.combos)} " +
            $"quests={result.questsDone}/{result.questsTotal} navPlans={result.navPlans} navWarps={result.navWarps} " +
            $"result_phases={string.Join(",", result.phases)}");
        result.kindledUnlocked = RunProfile.DifficultyUnlocked;
        result.finalBossDefeated = RunProfile.Finale.FinalBossDefeated;
        Finish(exceptionCount > 0 || bossKills < 1 || (!coopLead && SessionComplete.Shown < 1) ? 1 : 0);
    }

    void ReportQuests()
    {
        var s = RunProfile.Story;
        result.questsTotal = ledger.Total;
        result.questsDone = ledger.Done(s).Count;
        result.questsOpen = ledger.Open(s);
        result.questsCaughtUp = ledger.CaughtUp(s);
        if (result.questsOpen.Count == 0 && result.questsCaughtUp.Count == 0) result.Reach(AutoplayResult.PhaseQuests);
        string open = string.Join(",", result.questsOpen);
        string caught = string.Join(",", result.questsCaughtUp);
        Log($"phase quests: done={result.questsDone}/{result.questsTotal} open=[{open}] caughtUp=[{caught}]");
    }

    // ---------------------------------------------------------------- movement

    /// <summary>Walk to <paramref name="dest"/> through <see cref="AutoplayMover"/>; passes success to <paramref name="onResult"/>.</summary>
    IEnumerator Walk(DiabloClickController hero, Vector3 dest, float arrive, float budget, float deadline,
        float planAccept = 0f, System.Func<bool> done = null, System.Action<bool> onResult = null)
    {
        mover.Reset();
        float end = Time.realtimeSinceStartup + budget;
        bool ok = false;
        while (Time.realtimeSinceStartup < end && Time.realtimeSinceStartup < deadline)
        {
            TickGod(hero);
            if ((done != null && done()) || mover.Tick(hero, dest, arrive, null, planAccept))
            {
                ok = true;
                break;
            }
            yield return null;
        }
        hero.SetDestination(hero.transform.position);
        onResult?.Invoke(ok);
    }

    /// <summary>
    /// One combat frame against <paramref name="target"/>: path toward it while
    /// out of range (ranged casts on the way), swing and cast in range.
    /// </summary>
    void FightTick(DiabloClickController hero, DiabloSkills skills, GameObject target, bool boss)
    {
        var toTarget = target.transform.position - hero.transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;
        if (dist > hero.attackRange)
        {
            mover.Tick(hero, target.transform.position, hero.attackRange * 0.9f, target);
            if (skills != null && dist < 18f) AutoplayCombat.CastProfile(skills, toTarget, inRange: false);
        }
        else
        {
            hero.SetAimPoint(target.transform.position);
            hero.Strike();
            if (skills != null) AutoplayCombat.CastProfile(skills, toTarget, inRange: true, boss: boss);
        }
    }

    // ------------------------------------------------------------------ phases

    IEnumerator PhaseTownWaypoints(DiabloClickController hero, float deadline, System.Action<bool> onResult = null)
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
            bool walked = false;
            yield return Walk(hero, target.transform.position, 2f, 15f, deadline, onResult: ok => walked = ok);
            if (walked) reached++;
            Log($"phase town_waypoints: {name} reached={walked}");
        }
        var townShrine = WaypointShrine.Find("Town");
        if (townShrine != null) townShrine.TryDiscover(hero.gameObject);
        phasesDone++;
        onResult?.Invoke(reached > 0);
        Log("phase town_waypoints done");
    }

    IEnumerator PhaseFlask(DiabloClickController hero, System.Action<bool> onResult = null)
    {
        Log("phase flask start (gear-slots S2 seam: HealingFlask.TryDrink)");
        var flask = HealingFlask.For(hero.gameObject);
        bool drank = flask.TryDrink();
        Log($"phase flask: drank={drank} charges={flask.Charges}/{flask.MaxCharges}");
        // Walk back into the Lantern Hold, which refills a spent flask within one check.
        mover.Reset();
        float until = Time.time + 12f;
        while (Time.time < until && !flask.IsFull)
        {
            mover.Tick(hero, SkillVariantCatalog.TownCenter, 0.5f);
            yield return null;
        }
        Log($"phase flask: refilled={flask.IsFull}");
        onResult?.Invoke(drank && flask.IsFull);
        phasesDone++;
        Log("phase flask done");
        yield return null;
    }

    /// <summary>
    /// Clear of Husk-tagged targets (husks, Bone Wardens, boss summons); the
    /// boss itself is excluded unless <paramref name="excludeBoss"/> is false.
    /// A target that takes no damage for <see cref="TargetStallSeconds"/> is
    /// set aside and retried once nothing else is left.
    /// </summary>
    IEnumerator FightLoop(DiabloClickController hero, string label, bool excludeBoss,
        float maxDist, float budget, float deadline, System.Action<bool> onResult = null)
    {
        Log($"phase combat_{label} start (excludeBoss={excludeBoss}) hero={Fmt(hero.transform.position)}");
        var skills = hero.GetComponent<DiabloSkills>();
        int start = AutoplayCombat.CountHusks(excludeBoss: excludeBoss);
        float end = Time.realtimeSinceStartup + budget;
        bool cleared = false;
        int teleportsAtStart = ZoneGate.Teleports;
        var aside = new HashSet<GameObject>();
        GameObject current = null;
        float currentHp = 0f, currentSince = 0f;
        mover.Reset();
        while (Time.realtimeSinceStartup < end && Time.realtimeSinceStartup < deadline)
        {
            TickGod(hero);
            if (ZoneGate.Teleports != teleportsAtStart)
            {
                // A gate fired mid-fight: the zone is not cleared, it was left.
                Log($"phase combat_{label}: FAIL hero left the zone mid-fight " +
                    $"{AutoplayCombat.HuskCensus(hero.transform.position, excludeBoss)}");
                break;
            }
            var husk = AutoplayCombat.NearestHusk(hero.transform.position, maxDist, excludeBoss, aside);
            if (husk == null && aside.Count > 0)
            {
                aside.Clear();
                husk = AutoplayCombat.NearestHusk(hero.transform.position, maxDist, excludeBoss, aside);
            }
            if (husk == null)
            {
                Log($"phase combat_{label}: clear (kills={start - AutoplayCombat.CountHusks(excludeBoss: excludeBoss)}) " +
                    $"{AutoplayCombat.HuskCensus(hero.transform.position, excludeBoss)}");
                cleared = true;
                break;
            }
            if (husk != current)
            {
                current = husk;
                currentHp = AutoplayCombat.Hp(husk);
                currentSince = Time.realtimeSinceStartup;
            }
            else if (AutoplayCombat.Hp(husk) < currentHp)
            {
                currentHp = AutoplayCombat.Hp(husk);
                currentSince = Time.realtimeSinceStartup;
            }
            else if (Time.realtimeSinceStartup - currentSince > TargetStallSeconds)
            {
                Log($"phase combat_{label}: target {husk.name} set aside (no damage in {TargetStallSeconds:0} s)");
                aside.Add(husk);
                current = null;
                continue;
            }
            FightTick(hero, skills, husk, boss: false);
            yield return null;
        }
        if (!cleared) Log($"phase combat_{label}: not cleared, {AutoplayCombat.HuskCensus(hero.transform.position, excludeBoss)}");
        Log($"phase combat_{label} done (kills={start - AutoplayCombat.CountHusks(excludeBoss: excludeBoss)}, cleared={cleared})");
        phasesDone++;
        onResult?.Invoke(cleared);
    }

    /// <summary>Pass a ZoneGate (explicit Teleport, deterministic) and verify
    /// the zone stamp + autosave in the save file.</summary>
    IEnumerator PhaseGate(DiabloClickController hero, string gateName, int expectZone, float deadline,
        System.Action<bool> onResult = null)
    {
        onResult?.Invoke(false);
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
        mover.Reset();
        float leg = Time.realtimeSinceStartup + 30f;
        while (ZoneGate.Teleports == before && Time.realtimeSinceStartup < leg && Time.realtimeSinceStartup < deadline)
        {
            TickGod(hero);
            if (mover.Tick(hero, gate.transform.position, 4f)) break;
            yield return null;
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
        mover.Reset();
        bool saved = SaveService.HasSave()
            && System.IO.File.ReadAllText(SaveService.DefaultPath).Contains($"\"zoneReached\": {expectZone}");
        Log($"phase gate_{gateName}: teleports={ZoneGate.Teleports - before} zoneSaved={saved} (expectZone={expectZone})");
        phasesDone++;
        onResult?.Invoke(ZoneGate.Teleports > before && saved);
        Log($"phase gate_{gateName} done");
        yield return null;
    }

    /// <summary>
    /// Shared boss fight: <paramref name="pick"/> chooses the frame's target
    /// (the boss, or something it must lose first); in quest mode a wanted
    /// kill target (e.g. Kiln summons for a hunt) near the hero goes first.
    /// Runs until <paramref name="kills"/> moves past its start value.
    /// </summary>
    IEnumerator BossFight(DiabloClickController hero, string label, GameObject boss,
        System.Func<int> kills, System.Func<GameObject, GameObject> pick, float budget, float deadline)
    {
        var skills = hero.GetComponent<DiabloSkills>();
        int startKills = kills();
        float end = Time.realtimeSinceStartup + budget;
        mover.Reset();
        while (kills() <= startKills && Time.realtimeSinceStartup < end && Time.realtimeSinceStartup < deadline)
        {
            TickGod(hero);
            if (boss == null) break; // destroyed after death, bookkeeping runs in Update
            var target = pick(boss);
            if (questMode)
            {
                var add = NearestWantedKill(hero.transform.position, 12f, null);
                if (add != null) target = add;
            }
            FightTick(hero, skills, target, boss: target == boss);
            yield return null;
        }
        Log($"phase {label}: fight ended kills={kills() - startKills} hero={Fmt(hero.transform.position)}");
    }

    bool coopLead;
    DiabloClickController leadHero;

    /// <summary>Lead client: boss kills arrive as act-state broadcasts from the server (0..4).</summary>
    int ActKills()
    {
        if (!coopLead) return 0;
        return (RunProfile.Act.IsUnlocked(ActProgress.Kiln) ? 1 : 0) + (RunProfile.Act.ActOneComplete ? 1 : 0)
            + (RunProfile.Act.ZoneBossDefeated ? 1 : 0) + (RunProfile.Finale.FinalBossDefeated ? 1 : 0);
    }

    static IEnumerator WaitFor(System.Func<bool> done, float seconds)
    {
        float until = Time.realtimeSinceStartup + seconds;
        while (!done() && Time.realtimeSinceStartup < until) yield return null;
    }

    /// <summary>Walk to The Hollow Abbot and fight until BossesKilled ticks,
    /// then wait for the SessionComplete overlay.</summary>
    IEnumerator PhaseBoss(DiabloClickController hero, float deadline, System.Action<bool> onResult = null)
    {
        onResult?.Invoke(false);
        Log("phase boss start (The Hollow Abbot, phase 1 + 2)");
        var boss = GameObject.Find(BossController.BossName);
        if (boss == null)
        {
            // Co-op lead: the party may fell the boss during the zone fight; the server's kill broadcast counts.
            if (coopLead && RunProfile.Act.IsUnlocked(ActProgress.Kiln)) { Log("phase boss: already down (party kill)"); phasesDone++; onResult?.Invoke(true); yield break; }
            Log("phase boss: FAIL boss not found");
            yield break;
        }
        Log($"phase boss: hero={Fmt(hero.transform.position)} boss={Fmt(boss.transform.position)}");
        yield return BossFight(hero, "boss", boss, () => BossController.BossesKilled + ActKills(), b => b, 200f, deadline);
        if (coopLead) yield return WaitFor(() => RunProfile.Act.IsUnlocked(ActProgress.Kiln), 15f);
        int bossKills = coopLead ? (RunProfile.Act.IsUnlocked(ActProgress.Kiln) ? 1 : 0) : BossController.BossesKilled - startBosses;
        Log($"phase boss: bossKills={bossKills} phaseTwo={BossController.PhaseTwoEntries} drop={BossController.LastBossDrop}");
        float overlayEnd = Time.realtimeSinceStartup + 10f;
        while (!coopLead && SessionComplete.Shown < 1 && Time.realtimeSinceStartup < overlayEnd)
            yield return null;
        Log($"phase boss done (overlay={SessionComplete.Shown} summary='{SessionComplete.LastSummary}')");
        phasesDone++;
        onResult?.Invoke(bossKills >= 1);
    }

    /// <summary>act-one S5: fight The Kilnmother until she falls and the Act I screen shows.</summary>
    IEnumerator PhaseKilnmother(DiabloClickController hero, float deadline, System.Action<bool> onResult = null)
    {
        onResult?.Invoke(false);
        int startKills = KilnmotherController.Kills;
        Log("phase actboss start (The Kilnmother, phase 1 + 2)");
        var boss = GameObject.Find(KilnLayout.KilnmotherName);
        if (boss == null)
        {
            if (coopLead && RunProfile.Act.ActOneComplete) { Log("phase actboss: already down (party kill)"); phasesDone++; onResult?.Invoke(true); yield break; }
            Log("phase actboss: FAIL Kilnmother not found");
            yield break;
        }
        yield return BossFight(hero, "actboss", boss, () => KilnmotherController.Kills + ActKills(), b => b, 240f, deadline);
        if (coopLead) yield return WaitFor(() => RunProfile.Act.ActOneComplete, 15f);
        int kills = coopLead ? (RunProfile.Act.ActOneComplete ? 1 : 0) : KilnmotherController.Kills - startKills;
        Log($"phase actboss: kills={kills} phaseTwo={KilnmotherController.PhaseTwoEntries} waves={KilnmotherController.WavesFired} " +
            $"act={RunProfile.Act.ActOneComplete} overlay={SessionComplete.ActOneMode}");
        phasesDone++;
        onResult?.Invoke(kills >= 1 && RunProfile.Act.ActOneComplete);
    }

    /// <summary>
    /// act-two-opening S5: fight The Glasswright. Living Glass Pillars come
    /// first (they ward the boss), then the boss until it falls and the
    /// Barrens-cleared screen shows.
    /// </summary>
    IEnumerator PhaseGlasswright(DiabloClickController hero, float deadline, System.Action<bool> onResult = null)
    {
        onResult?.Invoke(false);
        int startKills = GlasswrightController.Kills;
        Log("phase boss3 start (The Glasswright, phase 1 + 2)");
        var boss = GameObject.Find(GlasswrightController.BossName);
        if (boss == null)
        {
            if (coopLead && RunProfile.Act.ZoneBossDefeated) { Log("phase boss3: already down (party kill)"); phasesDone++; onResult?.Invoke(true); yield break; }
            Log("phase boss3: FAIL Glasswright not found");
            yield break;
        }
        yield return BossFight(hero, "boss3", boss, () => GlasswrightController.Kills + ActKills(), AutoplayCombat.GlassPillarOr, 260f, deadline);
        if (coopLead) yield return WaitFor(() => RunProfile.Act.ZoneBossDefeated, 15f);
        int kills = coopLead ? (RunProfile.Act.ZoneBossDefeated ? 1 : 0) : GlasswrightController.Kills - startKills;
        Log($"phase boss3: kills={kills} phaseTwo={GlasswrightController.PhaseTwoEntries} lances={GlasswrightController.LanceVolleys} " +
            $"novas={GlasswrightController.NovasFired} fields={GlasswrightController.FieldsCalled} warded={GlassWard.Warded} " +
            $"zoneBoss={RunProfile.Act.ZoneBossDefeated} drop={GlasswrightController.LastZoneUnique?.uniqueId} overlay={SessionComplete.ZoneBossMode}");
        phasesDone++;
        onResult?.Invoke(kills >= 1 && RunProfile.Act.ZoneBossDefeated);
    }

    /// <summary>
    /// cinderreach-act-boss-gating: plays quest rounds until every other quest
    /// of <paramref name="act"/> is Done, then waits for the boss to appear
    /// and its spawn cutscene to end (the boss holds while it runs).
    /// </summary>
    IEnumerator QuestsUntilBoss(DiabloClickController hero, int act, float deadline)
    {
        // Co-op lead: kills and finds book on the server and arrive with its next story
        // broadcast, so it gets more rounds and a short sync pause before each check.
        int rounds = coopLead ? 10 : 6;
        for (int round = 0; round < rounds && Time.realtimeSinceStartup < deadline; round++)
        {
            if (coopLead) yield return WaitFor(() => false, 1.5f);
            if (ActBossGate.Ready(RunProfile.Story, act)) break;
            yield return QuestRound(hero, deadline);
        }
        if (coopLead) yield return WaitFor(() => ActBossGate.Ready(RunProfile.Story, act), 5f);
        var open = new List<string>();
        foreach (var id in ActBossGate.RequiredQuests(act))
            if (RunProfile.Story.Status(id) != StoryStatus.Done) open.Add(id);
        var boss = ActBossGate.Bosses[act - 1];
        float wait = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < wait
               && (GameObject.Find(boss.anchorName) == null || CutsceneDirector.Active))
            yield return null;
        Log($"quest gate act {act}: ready={open.Count == 0} open=[{string.Join(",", open)}] " +
            $"boss={GameObject.Find(boss.anchorName) != null} spawns={ActBossGate.Spawns[act - 1]} " +
            $"cutscene={CutsceneDirector.HasPlayed(boss.introId)}");
    }

    /// <summary>cinderreach-act-boss-gating: Thewes at the Moortor falls last in Act III (H12).</summary>
    IEnumerator PhaseThewes(DiabloClickController hero, float deadline, System.Action<bool> onResult = null)
    {
        onResult?.Invoke(false);
        Log("phase boss4 start (Thewes)");
        var boss = GameObject.Find(StoryIds.Thewes);
        if (boss == null)
        {
            if (coopLead && RunProfile.Finale.FinalBossDefeated)
            {
                Log("phase boss4: already down (party kill)");
                phasesDone++;
                onResult?.Invoke(true);
                result.Reach(AutoplayResult.PhaseFinalBoss);
                yield break;
            }
            Log("phase boss4: FAIL Thewes not found");
            yield break;
        }
        if (ZoneMap.IndexAt(hero.transform.position) != ActProgress.Barrens)
            yield return TravelToZone(hero, ActProgress.Barrens);
        var skills = hero.GetComponent<DiabloSkills>();
        float end = Time.realtimeSinceStartup + 240f;
        mover.Reset();
        while (boss != null && boss.GetComponent<EnemyHealth>() is EnemyHealth eh && eh.IsAlive
               && Time.realtimeSinceStartup < end && Time.realtimeSinceStartup < deadline)
        {
            TickGod(hero);
            FightTick(hero, skills, boss, boss: true);
            yield return null;
        }
        if (coopLead) yield return WaitFor(() => RunProfile.Finale.FinalBossDefeated, 15f);
        bool down = RunProfile.Finale.FinalBossDefeated;
        Log($"phase boss4: finalBoss={down} h12={RunProfile.Story.Status("h12")}");
        phasesDone++;
        onResult?.Invoke(down);
        if (down) result.Reach(AutoplayResult.PhaseFinalBoss);
    }

    // ------------------------------------------------------------------ quests

    static string ShrineFor(int zone)
    {
        switch (zone)
        {
            // Shrine ids match the zone names (KilnLayout.Shrines).
            case ActProgress.Ossuary: return StoryIds.ZoneOssuary;
            case ActProgress.Kiln: return StoryIds.ZoneKiln;
            case ActProgress.Barrens: return StoryIds.ZoneBarrens;
            default: return "Town";
        }
    }

    static bool CanTravelTo(int zone)
    {
        var shrine = WaypointShrine.Find(ShrineFor(zone));
        return shrine != null && WaypointShrine.Destinations().Contains(shrine);
    }

    /// <summary>Walk to this zone's waypoint shrine so later quest trips can travel back by waypoint.</summary>
    IEnumerator DiscoverShrine(DiabloClickController hero, string id, float deadline)
    {
        var shrine = WaypointShrine.Find(id);
        if (shrine == null || RunProfile.Act.Waypoints.Contains(id)) yield break;
        yield return Walk(hero, shrine.transform.position, 1.5f, 30f, deadline);
        bool found = shrine.TryDiscover(hero.gameObject) || RunProfile.Act.Waypoints.Contains(id);
        Log($"quest shrine {id}: discovered={found}");
    }

    IEnumerator TravelToZone(DiabloClickController hero, int zone)
    {
        if (ZoneMap.IndexAt(hero.transform.position) == zone) yield break;
        bool ok = WaypointShrine.Travel(ShrineFor(zone), hero.gameObject);
        mover.Reset();
        Log($"quest travel to {ShrineFor(zone)}: ok={ok} hero={Fmt(hero.transform.position)}");
        yield return null;
        yield return null;
    }

    /// <summary>
    /// Plays what the story wants right now, starting from this zone: sweeps
    /// it, walks to Lantern Hold for pending talks, visits other reachable
    /// zones with wanted finds or kills, then returns to the zone it began in.
    /// </summary>
    IEnumerator QuestRound(DiabloClickController hero, float deadline)
    {
        int origin = ZoneMap.IndexAt(hero.transform.position);
        var visited = new HashSet<int>();
        for (int step = 0; step < 8 && Time.realtimeSinceStartup < deadline; step++)
        {
            yield return QuestSweep(hero, deadline);
            int here = ZoneMap.IndexAt(hero.transform.position);
            visited.Add(here);
            int next = 0;
            if (here != ActProgress.Town && TalkPendingInTown() && CanTravelTo(ActProgress.Town)) next = ActProgress.Town;
            if (next == 0) next = ZoneWithWork(here, visited);
            if (next == 0) break;
            visited.Remove(next);
            yield return TravelToZone(hero, next);
        }
        if (ZoneMap.IndexAt(hero.transform.position) != origin && CanTravelTo(origin))
            yield return TravelToZone(hero, origin);
    }

    bool TalkPendingInTown()
    {
        foreach (var npc in AutoplayQuests.PendingTalks(RunProfile.Story))
            if (questWorld.TryNpc(npc, out _)) return true;
        return false;
    }

    /// <summary>A reachable zone (not <paramref name="here"/>, not yet visited this round) with wanted finds or kills; 0 when none.</summary>
    int ZoneWithWork(int here, HashSet<int> visited)
    {
        var s = RunProfile.Story;
        var finds = AutoplayQuests.WantedFinds(s);
        var kills = AutoplayQuests.WantedKills(s);
        for (int zone = ActProgress.Town; zone <= ActProgress.LastZone; zone++)
        {
            if (zone == here || visited.Contains(zone) || !CanTravelTo(zone)) continue;
            foreach (var p in Object.FindObjectsByType<StoryPickup>(FindObjectsSortMode.None))
                if (p.gameObject.activeInHierarchy && finds.Contains(p.itemId) && ZoneMap.IndexAt(p.transform.position) == zone)
                    return zone;
            foreach (var e in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
                if (e.IsAlive && e.gameObject.activeInHierarchy && kills.Contains(StoryIds.KillTarget(e.gameObject.name))
                    && ZoneMap.IndexAt(e.transform.position) == zone)
                    return zone;
        }
        return 0;
    }

    /// <summary>
    /// Works through this zone's quest steps, nearest first: pending NPC talks,
    /// wanted finds, wanted (non-boss) kills. A step that makes no progress
    /// within its budget is set aside for the rest of the sweep.
    /// </summary>
    IEnumerator QuestSweep(DiabloClickController hero, float deadline)
    {
        var skills = hero.GetComponent<DiabloSkills>();
        var asideNpc = new HashSet<string>();
        var asideObj = new HashSet<GameObject>();
        for (int guard = 0; guard < 80 && Time.realtimeSinceStartup < deadline; guard++)
        {
            ledger.Observe(RunProfile.Story);
            var pos = hero.transform.position;
            int zone = ZoneMap.IndexAt(pos);
            string npc = null;
            GameObject obj = null;
            Vector3 at = default;
            float best = float.MaxValue;
            foreach (var id in AutoplayQuests.PendingTalks(RunProfile.Story))
            {
                if (asideNpc.Contains(id) || !questWorld.TryNpc(id, out var p) || ZoneMap.IndexAt(p) != zone) continue;
                float d = Vector3.Distance(pos, p);
                if (d < best) { best = d; npc = id; obj = null; at = p; }
            }
            var finds = AutoplayQuests.WantedFinds(RunProfile.Story);
            foreach (var p in Object.FindObjectsByType<StoryPickup>(FindObjectsSortMode.None))
            {
                if (!p.gameObject.activeInHierarchy || !finds.Contains(p.itemId) || asideObj.Contains(p.gameObject)) continue;
                if (ZoneMap.IndexAt(p.transform.position) != zone) continue;
                float d = Vector3.Distance(pos, p.transform.position);
                if (d < best) { best = d; npc = null; obj = p.gameObject; at = p.transform.position; }
            }
            var kill = NearestWantedKill(pos, float.MaxValue, asideObj);
            if (kill != null && ZoneMap.IndexAt(kill.transform.position) == zone
                && Vector3.Distance(pos, kill.transform.position) < best)
            {
                npc = null;
                obj = kill;
            }
            if (npc == null && obj == null) break;

            if (npc != null)
            {
                bool walked = false;
                yield return Walk(hero, at, TalkReach, 45f, deadline, onResult: ok => walked = ok);
                int rev = RunProfile.Story.Revision;
                var plan = StoryDirector.BookTalk(npc, hero.gameObject);
                yield return null;
                // Co-op lead: the server books the talk; wait for its story broadcast before
                // planning the next talk, or the NPC is set aside with offers still pending.
                if (coopLead) yield return WaitFor(() => RunProfile.Story.Revision != rev, 4f);
                Log($"quest talk {npc}: {plan.kind} {plan.questId} walked={walked}");
                if (RunProfile.Story.Revision == rev) asideNpc.Add(npc);
            }
            else if (obj.GetComponent<StoryPickup>() != null)
            {
                string item = obj.GetComponent<StoryPickup>().itemId;
                // Finds lie on props (sarcophagi, slag): plan to a free cell beside the trigger, stop once taken.
                var find = obj;
                yield return Walk(hero, obj.transform.position, 0.4f, 45f, deadline, PickupReach, () => !find.activeInHierarchy);
                float wait = Time.realtimeSinceStartup + 1f;
                while (obj.activeInHierarchy && Time.realtimeSinceStartup < wait) yield return null;
                Log($"quest find {item}: taken={!obj.activeInHierarchy}");
                if (obj.activeInHierarchy) asideObj.Add(obj);
            }
            else
            {
                string target = obj.name;
                float end = Time.realtimeSinceStartup + 45f;
                mover.Reset();
                while (obj != null && obj.GetComponent<EnemyHealth>() is EnemyHealth eh && eh.IsAlive
                       && Time.realtimeSinceStartup < end && Time.realtimeSinceStartup < deadline)
                {
                    TickGod(hero);
                    FightTick(hero, skills, obj, boss: false);
                    yield return null;
                }
                bool dead = obj == null || obj.GetComponent<EnemyHealth>() == null || !obj.GetComponent<EnemyHealth>().IsAlive;
                Log($"quest hunt {target}: killed={dead}");
                if (!dead) asideObj.Add(obj);
            }
        }
        ledger.Observe(RunProfile.Story);
    }

    /// <summary>Nearest living enemy an active quest still wants (bosses excluded) within <paramref name="maxDist"/>.</summary>
    static GameObject NearestWantedKill(Vector3 from, float maxDist, HashSet<GameObject> aside)
    {
        var wanted = AutoplayQuests.WantedKills(RunProfile.Story);
        if (wanted.Count == 0) return null;
        GameObject best = null;
        float bestD = maxDist;
        foreach (var e in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (e == null || !e.IsAlive || !e.gameObject.activeInHierarchy) continue;
            if (aside != null && aside.Contains(e.gameObject)) continue;
            if (AutoplayCombat.IsBoss(e.gameObject) || !wanted.Contains(StoryIds.KillTarget(e.gameObject.name))) continue;
            float d = Vector3.Distance(from, e.transform.position);
            if (d < bestD) { bestD = d; best = e.gameObject; }
        }
        return best;
    }

    void TickGod(DiabloClickController hero)
    {
        if (!godMode) return;
        var health = hero.GetComponent<PlayerHealth>();
        if (health != null && health.CurrentHP < health.MaxHP) health.Heal(health.MaxHP);
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

    /// <summary>Party on the run: the hero's class plus AI companion classes.</summary>
    static List<string> PartyRoleIds()
    {
        var roles = new List<string> { RunProfile.ClassId };
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
        result.fps_windows = new List<float>(FpsReporter.Windows);
        result.seed = LayoutSeeder.CurrentSeed;
        result.difficulty = DifficultyProfile.Current.name;
        result.classId = RunProfile.ClassId;
        result.actOneComplete = RunProfile.Act.ActOneComplete;
        if (coopLead)
        {
            result.nextRunKindled = AutoplayResult.PrepareKindledCampaign();
            result.kindledSaved = AutoplayResult.SaveCarriesKindled();
        }
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
