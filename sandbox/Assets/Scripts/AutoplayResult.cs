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
    public const string PhaseComplete = "complete";

    /// <summary>Canonical order; a full session reaches all of them.</summary>
    public static readonly string[] RequiredPhases =
        { PhaseTown, PhaseZone1, PhaseGate, PhaseZone2, PhaseBoss, PhaseZone3, PhaseActBoss, PhaseComplete };

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

    public const string RoleSolo = "solo";
    public const string RoleHost = "host";
    public const string RoleClient = "client";

    /// <summary>
    /// Phases a co-op client must reach: it follows the host through Lantern
    /// Hold and the Ashen Fen (coop-session S1). Later stages extend the list
    /// as gates and bosses become co-op.
    /// </summary>
    public static readonly string[] ClientPhases = { PhaseTown, PhaseZone1 };

    /// <summary>Required phases for a role (solo and host play the full route).</summary>
    public static string[] RequiredFor(string role) => role == RoleClient ? ClientPhases : RequiredPhases;

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
        return true;
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
