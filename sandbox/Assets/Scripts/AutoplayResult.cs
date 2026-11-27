using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Result contract between <see cref="SessionAutoplay"/> and the dotfiles
/// health probe G-DIABLO06 (cinderreach-loop-guard, schema 1):
/// <c>{"schema":1,"commit","started","duration_s","phases":[...],"exceptions","fps_windows":[...],"exit"}</c>.
/// Pure data + helpers so EditMode tests can pin the format.
/// </summary>
[System.Serializable]
public sealed class AutoplayResult
{
    public const int Schema = 1;
    public const string FileName = "autoplay-result.json";

    public const string PhaseTown = "town";
    public const string PhaseZone1 = "zone1";
    public const string PhaseGate = "gate";
    public const string PhaseZone2 = "zone2";
    public const string PhaseBoss = "boss";
    // act-one S5: the run continues through the Cinder Kiln to the Kilnmother.
    public const string PhaseZone3 = "zone3";
    public const string PhaseActBoss = "actboss";
    // act-two-opening S5: on through the Glass Barrens to the Glasswright.
    public const string PhaseZone4 = "zone4";
    public const string PhaseBoss3 = "boss3";
    public const string PhaseComplete = "complete";
    // cinderreach-act-boss-gating: Thewes falls last, after every other Act-III quest.
    public const string PhaseFinalBoss = "boss4";
    /// <summary>autoplay-quests: every story quest played to Done (no catch-up). Only required in quest mode.</summary>
    public const string PhaseQuests = "quests";

    /// <summary>Canonical order; a full session reaches all of them.</summary>
    public static readonly string[] RequiredPhases =
        { PhaseTown, PhaseZone1, PhaseGate, PhaseZone2, PhaseBoss, PhaseZone3, PhaseActBoss, PhaseZone4, PhaseBoss3, PhaseComplete, PhaseFinalBoss };

    public int schema = Schema;
    public string commit = "unknown";
    public string started = "";
    public float duration_s;
    public List<string> phases = new List<string>();
    public int exceptions;
    public List<float> fps_windows = new List<float>();
    public int exit = 1;
    /// <summary>Layout seed of the run (replayability S3), -1 when no zone layout ran.</summary>
    public int seed = -1;
    /// <summary>Difficulty of the run (replayability S5; G-DIABLO06 judges each one).</summary>
    public string difficulty = "Normal";
    /// <summary>Hero class of the run (second-class S5; G-DIABLO06 judges each class).</summary>
    public string classId = "fenwalker";
    /// <summary>Act I finished in this run (act-one S5).</summary>
    public bool actOneComplete;
    /// <summary>"solo", or "host"/"client" in a co-op pair run (coop-session S1).</summary>
    public string role = RoleSolo;
    /// <summary>Party on the run (party-roles S6): the hero's class plus AI companion classes.</summary>
    public List<string> partyRoles = new List<string>();
    /// <summary>Husks killed on the route (party-roles S6).</summary>
    public int huskKills;
    /// <summary>Heals cast on the route (party-roles S6).</summary>
    public int healsCast;
    /// <summary>HP restored by heals on the route (party-roles S6).</summary>
    public float hpRestored;
    /// <summary>Combo payoffs on the route, "Name=count" (class-kits-parity).</summary>
    public List<string> combos = new List<string>();
    /// <summary>autoplay-quests: the run played the story quests (solo and co-op host; act bosses need them).</summary>
    public bool questMode;
    /// <summary>Story quests in the catalog / Done at the end of the run.</summary>
    public int questsTotal;
    public int questsDone;
    /// <summary>Quest ids not Done at the end.</summary>
    public List<string> questsOpen = new List<string>();
    /// <summary>Quest ids the act state closed by catch-up instead of being played.</summary>
    public List<string> questsCaughtUp = new List<string>();
    /// <summary>Terrain navigation: path plans and last-resort warps onto validated cells.</summary>
    public int navPlans;
    /// <summary>cinderreach-act-boss-gating: act-boss kills in order, "act:boss:open" ("-" = all other act quests were Done).</summary>
    public List<string> actBossKills = new List<string>();
    public int navWarps;
    /// <summary>cinderreach-team-autoplay: Kindled unlocked in this process at the end (Thewes kill).</summary>
    public bool kindledUnlocked;
    /// <summary>cinderreach-team-autoplay: the save file on disk carries the Kindled unlock.</summary>
    public bool kindledSaved;
    /// <summary>cinderreach-team-autoplay: Act III final boss (Thewes) down in this process.</summary>
    public bool finalBossDefeated;
    /// <summary>cinderreach-team-autoplay K3: the save now starts the next run on Kindled.</summary>
    public bool nextRunKindled;

    public const string RoleSolo = "solo";
    public const string RoleHost = "host";
    public const string RoleClient = "client";
    /// <summary>Dedicated-server run: the client that walks the route (S1).</summary>
    public const string RoleLead = "lead";
    /// <summary>Dedicated-server run: a client following the lead (S1).</summary>
    public const string RoleFollow = "follow";

    /// <summary>
    /// Phases a follower must reach on a dedicated server (cinderreach-team-autoplay K2):
    /// it follows the lead through the whole campaign, fights in every zone and books
    /// every act-boss kill the server broadcasts, Thewes included.
    /// </summary>
    public static readonly string[] FollowPhases =
        { PhaseTown, PhaseZone1, PhaseZone2, PhaseBoss, PhaseZone3, PhaseActBoss, PhaseZone4, PhaseBoss3, PhaseFinalBoss };

    /// <summary>
    /// Phases a co-op client must reach (coop-trio): it follows the host from
    /// Lantern Hold through all four zones, fights in each, and books every boss
    /// kill the host broadcasts (Abbot, Kilnmother, Glasswright). Only the
    /// host-side <c>gate</c> step and the waypoint-home <c>complete</c> stay host-only.
    /// </summary>
    public static readonly string[] ClientPhases =
        { PhaseTown, PhaseZone1, PhaseZone2, PhaseBoss, PhaseZone3, PhaseActBoss, PhaseZone4, PhaseBoss3 };

    /// <summary>Required phases for a role (solo and host play the full route).</summary>
    public static string[] RequiredFor(string role) =>
        role == RoleClient ? ClientPhases
        : role == RoleLead ? RequiredPhases
        : role == RoleFollow ? FollowPhases
        : RequiredPhases;

    /// <summary>Record a reached phase once, keeping first-reach order.</summary>
    public void Reach(string phase)
    {
        if (!string.IsNullOrEmpty(phase) && !phases.Contains(phase)) phases.Add(phase);
    }

    /// <summary>True when every canonical phase was reached.</summary>
    public bool AllPhasesReached()
    {
        foreach (var p in RequiredFor(role))
            if (!phases.Contains(p)) return false;
        if (role == RoleSolo || role == RoleHost || role == RoleLead)
            if (!ActBossesFellLast()) return false;
        return !questMode || phases.Contains(PhaseQuests);
    }

    /// <summary>cinderreach-act-boss-gating: all three act bosses fell, each after every other quest of its act.</summary>
    public bool ActBossesFellLast()
    {
        var acts = new HashSet<string>();
        foreach (var k in actBossKills)
        {
            if (string.IsNullOrEmpty(k) || !k.EndsWith(":-")) return false;
            acts.Add(k.Split(':')[0]);
        }
        return acts.Contains("1") && acts.Contains("2") && acts.Contains("3");
    }

    /// <summary>
    /// cinderreach-team-autoplay K3: after a Normal campaign that killed Thewes, the team
    /// starts its next run on Kindled like a player would (Settings → new run): campaign
    /// restarts, hero kept, saved. True when it switched.
    /// </summary>
    public static bool PrepareKindledCampaign()
    {
        if (!RunProfile.DifficultyUnlocked || !RunProfile.Finale.FinalBossDefeated
            || DifficultyProfile.Current == DifficultyProfile.Kindled) return false;
        RunProfile.StartCampaign(RunProfile.Kindled);
        DiabloNetworkPlayer.SaveLocalHero();
        return RunProfile.Difficulty == RunProfile.Kindled;
    }

    /// <summary>True when the hero's save on disk carries the Kindled unlock.</summary>
    public static bool SaveCarriesKindled()
    {
        try
        {
            return SaveService.HasSave()
                && System.IO.File.ReadAllText(SaveService.DefaultPath).Replace(" ", "").Contains("\"difficultyUnlocked\":true");
        }
        catch (System.Exception) { return false; }
    }

    /// <summary>Exit code: 0 only with all phases and zero exceptions.</summary>
    public int ComputeExit() => exceptions == 0 && AllPhasesReached() ? 0 : 1;

    public string ToJson() => JsonUtility.ToJson(this, true);

    public static AutoplayResult FromJson(string json) => JsonUtility.FromJson<AutoplayResult>(json);

    /// <summary>Only real exceptions count (errors/asserts are logged, not counted).</summary>
    public static bool CountsAsException(LogType type) => type == LogType.Exception;

    /// <summary>Write <see cref="FileName"/> into <paramref name="dir"/> (created if missing).</summary>
    public string WriteTo(string dir)
    {
        System.IO.Directory.CreateDirectory(dir);
        var path = System.IO.Path.Combine(dir, FileName);
        var tmp = path + ".tmp";
        System.IO.File.WriteAllText(tmp, ToJson());
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        System.IO.File.Move(tmp, path);
        return path;
    }
}
