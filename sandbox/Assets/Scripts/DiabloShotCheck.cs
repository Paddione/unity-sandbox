using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Visual self-check for the Windows player: launched with <c>-shotcheck</c>,
/// it takes over the local hero, plays the skill poses (bow draw/release, aim
/// hold, fireball flight, blast) from fixed camera angles, saves screenshots
/// and quits. Output: <c>-shotdir &lt;path&gt;</c>, default
/// <c>&lt;game folder&gt;/shotcheck</c>. Inert without the flag.
/// </summary>
public sealed class DiabloShotCheck : MonoBehaviour
{
    public const string Flag = "-shotcheck";

    string dir;
    int index;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!HasArg(Flag)) return;
        var go = new GameObject("DiabloShotCheck");
        DontDestroyOnLoad(go);
        go.AddComponent<DiabloShotCheck>();
    }

    IEnumerator Start()
    {
        dir = ArgValue("-shotdir") ?? Path.Combine(Path.GetDirectoryName(Application.dataPath), "shotcheck");
        Directory.CreateDirectory(dir);
        foreach (var old in Directory.GetFiles(dir, "*.png")) File.Delete(old);
        Log($"start, output {dir}");

        // Wait for the owned hero (network host spawn) with its skills.
        DiabloClickController hero = null;
        float deadline = Time.realtimeSinceStartup + 30f;
        while (hero == null && Time.realtimeSinceStartup < deadline)
        {
            var go = GameObject.Find(DiabloNetworkPlayer.HeroName);
            if (go != null)
            {
                var c = go.GetComponent<DiabloClickController>();
                if (c != null && c.isActiveAndEnabled && go.GetComponent<DiabloSkills>() != null) hero = c;
            }
            yield return null;
        }
        if (hero == null)
        {
            Log("FAIL no controllable hero within 30 s");
            Quit(2);
            yield break;
        }
        DiabloClickController.ExternalDrive = true;
        HitStop.Enabled = false;
        var skills = hero.GetComponent<DiabloSkills>();
        yield return Wait(1.5f); // let the scene settle (fade-ins, first frames)

        float baseDist = hero.distance, basePitch = hero.pitch, baseYaw = hero.yaw;
        var camFwd = Quaternion.Euler(0f, baseYaw, 0f) * Vector3.forward;
        var camRight = Quaternion.Euler(0f, baseYaw, 0f) * Vector3.right;

        // 1) Gameplay framing, idle with sword.
        yield return Shot("gameplay_idle");

        // 1b) Wide look over the moor: props, fires, fog.
        hero.distance = 24f;
        yield return Wait(0.5f);
        yield return Shot("world_overview");
        hero.distance = baseDist;
        yield return Wait(0.3f);

        // 2) Close side view (left side, bow hand, toward the camera).
        hero.distance = 5.5f;
        hero.pitch = 30f;
        Face(hero, -camRight);
        yield return Wait(0.6f);
        yield return Shot("close_idle_sword");

        skills.CastToward(DiabloSkills.Skill.Arrow, -camRight);
        yield return Wait(0.08f);
        yield return Shot("bow_release_early");
        yield return Wait(0.15f);
        yield return Shot("bow_release_mid");
        yield return Wait(0.8f);
        yield return Shot("bow_aim_hold");

        // 3) Three-quarter front view of the aim.
        Face(hero, (-camFwd - camRight).normalized);
        skills.CastToward(DiabloSkills.Skill.Arrow, (-camFwd - camRight).normalized);
        yield return Wait(0.2f);
        yield return Shot("bow_release_front");
        yield return Wait(0.9f);
        yield return Shot("bow_aim_front");
        yield return Wait(DiabloSkills.BowStanceSeconds + 0.3f);
        yield return Shot("bow_put_away");

        // 4) Fireball in flight, then a blast in frame.
        hero.distance = 8f;
        hero.pitch = 45f;
        Face(hero, -camRight);
        yield return Wait(0.4f);
        skills.CastToward(DiabloSkills.Skill.Fireball, -camRight);
        yield return Wait(0.18f);
        yield return Shot("fireball_flight");
        var blastAt = hero.transform.position - camRight * 3f + Vector3.up * 0.5f;
        DiabloProjectile.SpawnBlast(blastAt, skills.fireballBlastRadius);
        yield return Wait(0.06f);
        yield return Shot("fireball_blast");

        // 5) Combat juice at gameplay framing: numbers, crit, blood, embers.
        hero.distance = baseDist;
        hero.pitch = basePitch;
        yield return Wait(1.2f);
        var hitAt = hero.transform.position - camRight * 2.5f;
        DiabloJuice.OnEnemyHit(hitAt, 2, false);
        yield return Wait(0.12f);
        DiabloJuice.OnEnemyHit(hitAt + camFwd * 0.6f, 6, true);
        yield return Wait(0.1f);
        yield return Shot("damage_numbers_crit");
        DiabloJuice.OnEnemyDeath(hitAt);
        DiabloProgression.GrantLocal(DiabloProgression.XpNormal, hitAt);
        yield return Wait(0.15f);
        yield return Shot("death_burst_xp");

        // 6) Level-up moment (banner at its overshoot, pillar rising).
        var prog = hero.GetComponent<DiabloProgression>();
        if (prog != null)
        {
            prog.AddXp(DiabloProgression.XpToNext(prog.Level) - prog.Xp);
            yield return Wait(0.3f);
            yield return Shot("level_up");
            yield return Wait(1.0f);
            yield return Shot("level_up_settled");
        }

        hero.distance = baseDist;
        hero.pitch = basePitch;
        DiabloClickController.ExternalDrive = false;
        Log($"DONE {index} shots");
        Quit(0);
    }

    static void Face(DiabloClickController hero, Vector3 dir)
    {
        hero.SetAimPoint(hero.transform.position + dir * 4f);
    }

    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        index++;
        string path = Path.Combine(dir, $"{index:00}_{name}.png");
        var tex = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Destroy(tex);
        Log($"shot {path}");
    }

    static IEnumerator Wait(float seconds)
    {
        float end = Time.time + seconds;
        while (Time.time < end) yield return null;
    }

    static void Log(string msg) => Debug.Log($"[ShotCheck] {msg}");

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

    static string ArgValue(string flag)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == flag) return args[i + 1];
        return null;
    }
}
