using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Local-art pilot self-check: launched with <c>-pilotshots</c>, it frames the
/// pilot assets (chest, both banners, icon, hound idle/run/attack + a genuine
/// aggro take) from staged game cameras, saves PNGs and quits. Output:
/// <c>-shotdir &lt;path&gt;</c>. Inert without the flag.
/// </summary>
public sealed class PilotShotRig : MonoBehaviour
{
    public const string Flag = "-pilotshots";

    string dir;
    int index;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!HasArg(Flag)) return;
        var go = new GameObject("PilotShotRig");
        DontDestroyOnLoad(go);
        go.AddComponent<PilotShotRig>();
    }

    static bool HasArg(string name)
    {
        foreach (var a in System.Environment.GetCommandLineArgs())
            if (a == name) return true;
        return false;
    }

    static string ArgValue(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == name) return args[i + 1];
        return null;
    }

    IEnumerator Start()
    {
        dir = ArgValue("-shotdir") ?? Path.Combine(
            Path.GetDirectoryName(Application.dataPath), "pilotshots");
        Directory.CreateDirectory(dir);
        foreach (var old in Directory.GetFiles(dir, "*.png")) File.Delete(old);
        yield return new WaitForSecondsRealtime(2.0f); // scene settle

        // Dismiss the title menu (it auto-skips only for known automation
        // flags; the pilot flag is new). Public API, no menu code touched.
        float menuDeadline = Time.realtimeSinceStartup + 10f;
        while (Time.realtimeSinceStartup < menuDeadline)
        {
            var menu = Object.FindFirstObjectByType<MainMenu>();
            if (menu != null && MainMenu.CurrentState != MainMenu.State.Playing)
                menu.CloseToGame();
            var heroProbe = GameObject.Find("Player");
            var clickProbe = heroProbe != null
                ? heroProbe.GetComponent<DiabloClickController>() : null;
            if (MainMenu.CurrentState == MainMenu.State.Playing
                && heroProbe != null
                && (clickProbe == null || !DiabloClickController.ExternalDrive))
                break;
            yield return null;
        }

        var cam = Camera.main;
        if (cam == null) { Quit(2); yield break; }
        var hero = GameObject.Find("Player");
        try { SaveService.DeleteSave(); } catch { } // deterministic Lv-1 staged shots
        var heroHome = hero != null ? hero.transform.position : Vector3.zero;
        if (hero != null) hero.transform.position = new Vector3(0f, -60f, 11f); // holding cell: no aggro photobombs
        var clicker = hero != null ? hero.GetComponent<DiabloClickController>() : null;
        if (clicker != null) clicker.enabled = false; // free camera for staged shots

        // 1. chest + icon from the game camera.
        var chest = GameObject.Find("DChestZone");
        if (chest != null)
        {
            Frame(cam, chest.transform.position + new Vector3(0f, 1.0f, 0f),
                chest.transform.position + new Vector3(-2.4f, 1.7f, 2.4f));
            yield return Shot("pilot_chest");
            Frame(cam, chest.transform.position + new Vector3(0f, 1.55f, 0f),
                chest.transform.position + new Vector3(-1.1f, 1.7f, 1.1f));
            yield return Shot("pilot_icon");
        }

        // 2. alcove banner (frayed alpha edge) + ossuary banner.
        var alcove = GameObject.Find("PilotBanner_Alcove");
        var bannerLight = new GameObject("PilotBannerLight");
        var bl = bannerLight.AddComponent<Light>();
        bl.type = LightType.Point;
        bl.color = new Color(1f, 0.7f, 0.45f);
        bl.intensity = 6f;
        bl.range = 6f;
        bl.shadows = LightShadows.None;
        if (alcove != null)
        {
            bannerLight.transform.position = alcove.transform.position + new Vector3(0.3f, 1.2f, 1.2f);
            Frame(cam, alcove.transform.position + new Vector3(0f, 1.0f, 0f),
                alcove.transform.position + new Vector3(0.4f, 1.2f, 2.6f));
            yield return Shot("pilot_banner_alcove");
        }
        var ossBanner = GameObject.Find("PilotBanner_Ossuary");
        if (ossBanner != null)
        {
            bannerLight.transform.position = ossBanner.transform.position + new Vector3(0.3f, 1.2f, -1.2f);
            Frame(cam, ossBanner.transform.position + new Vector3(0f, 1.0f, 0f),
                ossBanner.transform.position + new Vector3(0.4f, 1.2f, -2.6f));
            yield return Shot("pilot_banner_ossuary");
        }
        Object.Destroy(bannerLight);

        // 3. hound: deterministic clip frames (mimic off so it cannot fight
        // the rig; re-enabled for the genuine aggro take below).
        var brute = GameObject.Find("Fen Brute");
        Animator hound = null;
        QuadrupedMimic mimic = null;
        if (brute != null)
        {
            // The host husk carries its own (hidden) skeleton Animator on the
            // root; the hound's Animator is the one with the mimic attached.
            hound = brute.GetComponentInChildren<Animator>();
            mimic = brute.GetComponentInChildren<QuadrupedMimic>();
            if (mimic != null)
            {
                hound = mimic.GetComponent<Animator>();
                mimic.enabled = false;
            }
            Frame(cam, brute.transform.position + new Vector3(0f, 0.5f, 0f),
                brute.transform.position + new Vector3(-2.6f, 1.2f, 2.6f));
            yield return Shot("pilot_hound_idle");
            if (hound != null)
            {
                hound.speed = 1f;
                var clips0 = hound.GetCurrentAnimatorClipInfo(0);
                string clipInfo = clips0.Length > 0
                    ? $"{clips0[0].clip.name} len={clips0[0].clip.length}"
                    : "none";
                Debug.Log($"[PilotShotRig] hound={hound.gameObject.name} " +
                    $"ctrl={hound.runtimeAnimatorController != null} clip={clipInfo}");
                for (int i = 0; i < 4; i++)
                {
                    hound.Play("Run", 0, i / 4f);
                    hound.Update(0f);
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    yield return Shot($"pilot_hound_run_{i}");
                }
                for (int i = 0; i < 4; i++)
                {
                    hound.Play("Attack", 0, i / 3f);
                    hound.Update(0f);
                    yield return new WaitForEndOfFrame();
                    yield return new WaitForEndOfFrame();
                    yield return Shot($"pilot_hound_attack_{i}");
                }
            }
        }

        // 4. genuine aggro take: hero next to the brute, camera watches.
        // Short (3 takes): the hero can die in the pack, which would kill the
        // coroutine via a stale reference — guard everything, always quit.
        try
        {
            if (brute != null)
            {
                if (mimic != null) mimic.enabled = true;
                for (int i = 0; i < 3; i++)
                {
                    hero = GameObject.Find("Player");
                    if (hero == null || brute == null) break;
                    hero.transform.position = brute.transform.position + new Vector3(1.5f, 0f, 1.5f);
                    if (clicker != null) clicker.enabled = true;
                    var b = brute.transform.position;
                    Frame(cam, b + new Vector3(0f, 0.6f, 0f), b + new Vector3(-2.4f, 1.5f, 2.4f));
                    yield return new WaitForSecondsRealtime(1.0f);
                    yield return Shot($"pilot_hound_combat_{i}");
                }
            }
        }
        finally
        {
            Quit(0);
        }
        yield break;
    }

    void Frame(Camera cam, Vector3 lookAt, Vector3 camPos)
    {
        cam.transform.position = camPos;
        cam.transform.LookAt(lookAt);
    }

    IEnumerator Shot(string name)
    {
        // The title menu can (re)open after our initial dismissal; close it
        // before every shot so staged framing stays unobstructed.
        var menu = Object.FindFirstObjectByType<MainMenu>();
        if (menu != null && MainMenu.CurrentState != MainMenu.State.Playing)
            menu.CloseToGame();
        yield return new WaitForEndOfFrame();
        string path = Path.Combine(dir, $"{index:00}_{name}.png");
        index++;
        ScreenCapture.CaptureScreenshot(path);
        yield return new WaitForSecondsRealtime(0.5f);
    }

    void Quit(int code)
    {
        Debug.Log($"[PilotShotRig] done, shots in {dir}, exit {code}");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(code);
#endif
    }
}
