using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// In-engine cutscenes (diablo-cutscene-pipeline): boss introductions and
/// main-quest dialogue highlights. No video, no Timeline: a static,
/// headless-callable state machine in the style of <see cref="ZoneGate"/>
/// (explicit time, <see cref="ResetRun"/> seam) plus a thin per-frame
/// <see cref="Tick"/> hook from <see cref="StoryDirector"/>.
///
/// Boss intro: hero enters the arena trigger once per run — letterbox bars +
/// boss name banner, camera dolly to a side framing, boss roar; movement and
/// hero attacks freeze, the boss holds (see <see cref="HoldBosses"/>), skip
/// (space/esc/click) or timeout releases everything and the fight starts.
/// Dialogue highlight: main-quest talks (see <see cref="StoryDirector.TryOpenTalk"/>)
/// play inside the same letterbox and auto-advance on the voice length; the
/// existing close path (and its booking) is untouched.
/// </summary>
public static class CutsceneDirector
{
    /// <summary>Boss intro hookup: anchor is the boss GameObject name in the scene.</summary>
    public struct BossIntro
    {
        public string id;
        public string anchorName;
        public float triggerRange;
        public string bannerKey;
        public float duration;
    }

    public static readonly BossIntro[] BossIntros =
    {
        new BossIntro { id = "intro.abbot", anchorName = BossController.BossName, triggerRange = 12f, bannerKey = "name.enemy.abbot", duration = 4.5f },
        new BossIntro { id = "intro.kilnmother", anchorName = KilnLayout.KilnmotherName, triggerRange = 24f, bannerKey = "name.enemy.kilnmother", duration = 4.5f },
        new BossIntro { id = "intro.glasswright", anchorName = GlasswrightController.BossName, triggerRange = 16f, bannerKey = "name.enemy.glasswright", duration = 4.5f },
    };

    /// <summary>True while a boss intro runs: the three boss brains hold still (no aggro, no attacks).</summary>
    public static bool HoldBosses { get; private set; }
    /// <summary>True while any sequence (boss intro or highlighted dialogue) runs.</summary>
    public static bool Active { get; private set; }
    /// <summary>Sequence id, or "" when idle.</summary>
    public static string ActiveId { get; private set; } = "";
    /// <summary>Starts, skips and natural completions this run (test seams).</summary>
    public static int Starts, Skips, Completes;
    static readonly HashSet<string> played = new HashSet<string>();

    static float t0;
    static float lineStart;
    static float lineWindow = 2f;
    static bool roared;
    static Vector3 camPos;
    static Quaternion camRot;
    static Vector3 dollyFrom;
    static Quaternion dollyFromRot;
    static Vector3 dollyTo;
    static Quaternion dollyToRot;
    static ThirdPersonController frozenTpc;
    static DiabloMelee frozenMelee;
    static StoryDialogUI dialogUi;
    static GameObject letterbox;
    static Text bannerText;

    /// <summary>Ids already played this run.</summary>
    public static bool HasPlayed(string id) => !string.IsNullOrEmpty(id) && played.Contains(id);

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        RestoreControl();
        if (letterbox != null) Object.DestroyImmediate(letterbox);
        letterbox = null;
        bannerText = null;
        Active = false;
        ActiveId = "";
        HoldBosses = false;
        dialogUi = null;
        Starts = 0;
        Skips = 0;
        Completes = 0;
        played.Clear();
    }

    /// <summary>Per-frame hook (called from StoryDirector). Never throws into the caller.</summary>
    public static void Tick(float now)
    {
        try { TickInner(now); }
        catch (System.Exception e) { Debug.LogWarning($"[Cutscene] tick failed: {e.Message}"); }
    }

    static void TickInner(float now)
    {
        if (Active)
        {
            if (dialogUi != null) TickDialog(now);
            else TickBossIntro(now);
            return;
        }
        if (!Diablo.IsLocalAuthority()) return;
        var hero = HeroLocator.Solo();
        if (hero == null) return;
        foreach (var intro in BossIntros)
        {
            if (HasPlayed(intro.id)) continue;
            var anchor = GameObject.Find(intro.anchorName);
            if (anchor == null || !BossAlive(anchor)) continue;
            var d = anchor.transform.position - hero.position;
            d.y = 0f;
            if (d.magnitude <= intro.triggerRange)
                TryStartBossIntro(intro.id, now);
        }
    }

    static bool BossAlive(GameObject anchor)
    {
        var hp = anchor.GetComponent<EnemyHealth>();
        return hp == null || hp.HPFraction() > 0f;
    }

    /// <summary>Headless-callable boss intro start. False when busy, unknown id or already played.</summary>
    public static bool TryStartBossIntro(string id, float now)
    {
        if (Active) return false;
        int idx = IndexOf(id);
        if (idx < 0 || HasPlayed(id)) return false;
        var intro = BossIntros[idx];
        var anchor = GameObject.Find(intro.anchorName);
        if (anchor == null || !BossAlive(anchor)) return false;
        var hero = HeroLocator.Solo();
        if (hero == null) return false;
        Active = true;
        ActiveId = id;
        HoldBosses = true;
        Starts++;
        played.Add(id);
        t0 = now;
        roared = false;
        frozenTpc = hero.GetComponent<ThirdPersonController>();
        if (frozenTpc != null) frozenTpc.enabled = false;
        frozenMelee = hero.GetComponent<DiabloMelee>();
        if (frozenMelee != null) frozenMelee.enabled = false;
        var cam = Camera.main;
        if (cam != null)
        {
            camPos = cam.transform.position;
            camRot = cam.transform.rotation;
            FrameBoss(cam, anchor.transform.position, hero.position);
            dollyFrom = cam.transform.position;
            dollyFromRot = cam.transform.rotation;
        }
        ShowLetterbox(Loc.T(intro.bannerKey));
        return true;
    }

    static void TickBossIntro(float now)
    {
        int idx = IndexOf(ActiveId);
        if (idx < 0) { End(false); return; }
        var intro = BossIntros[idx];
        var anchor = GameObject.Find(intro.anchorName);
        var hero = HeroLocator.Solo();
        if (anchor == null || hero == null) { End(false); return; }
        if (!roared)
        {
            roared = true;
            try { DiabloAudio.PlayBossRoar(); } catch { }
        }
        var cam = Camera.main;
        if (cam != null)
        {
            float t = Mathf.Clamp01((now - t0) / 1.2f);
            float e = t * t * (3f - 2f * t);
            cam.transform.position = Vector3.Lerp(dollyFrom, dollyTo, e);
            cam.transform.rotation = Quaternion.Slerp(dollyFromRot, dollyToRot, e);
        }
        if (SkipPressed() || now - t0 >= intro.duration) End(SkipPressed());
    }

    static void FrameBoss(Camera cam, Vector3 boss, Vector3 heroPos)
    {
        var axis = boss - heroPos;
        axis.y = 0f;
        if (axis.sqrMagnitude < 0.01f) axis = Vector3.forward;
        axis.Normalize();
        var side = Vector3.Cross(Vector3.up, axis).normalized;
        dollyTo = boss - axis * 6.5f + side * 3.5f + Vector3.up * 3f;
        var mid = (boss + heroPos) * 0.5f + Vector3.up * 1.2f;
        dollyToRot = Quaternion.LookRotation(mid - dollyTo);
    }

    /// <summary>Headless-callable dialogue highlight: letterbox + auto-advance on voice length.</summary>
    public static bool PlayDialog(StoryDialogUI ui, string npc)
    {
        if (Active || ui == null || !ui.IsOpen) return false;
        Active = true;
        ActiveId = "dialog." + (npc ?? "?");
        dialogUi = ui;
        Starts++;
        lineStart = Time.unscaledTime;
        lineWindow = Mathf.Max(1.2f, ui.LastLineDuration + 0.6f);
        ShowLetterbox("");
        return true;
    }

    static void TickDialog(float now)
    {
        var ui = dialogUi;
        if (ui == null || !ui.IsOpen) { End(false); return; }
        if (SkipPressed())
        {
            ui.SetOpen(false);
            End(true);
            return;
        }
        if (now - lineStart >= lineWindow)
        {
            int before = ui.LineIndex;
            ui.Advance();
            if (!ui.IsOpen) { End(false); return; }
            if (ui.LineIndex != before)
            {
                lineStart = now;
                lineWindow = Mathf.Max(1.2f, ui.LastLineDuration + 0.6f);
            }
            else { lineStart = now; }
        }
    }

    static bool SkipPressed()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
            return true;
        var mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
    }

    /// <summary>Skip the running sequence (headless-callable). False when idle.</summary>
    public static bool Skip()
    {
        if (!Active) return false;
        if (dialogUi != null) dialogUi.SetOpen(false);
        End(true);
        return true;
    }

    static void End(bool skipped)
    {
        if (skipped) Skips++;
        else Completes++;
        RestoreControl();
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = camPos;
            cam.transform.rotation = camRot;
        }
        HideLetterbox();
        Active = false;
        ActiveId = "";
        HoldBosses = false;
        dialogUi = null;
    }

    static void RestoreControl()
    {
        if (frozenTpc != null)
        {
            try { frozenTpc.enabled = true; } catch { }
            frozenTpc = null;
        }
        if (frozenMelee != null)
        {
            try { frozenMelee.enabled = true; } catch { }
            frozenMelee = null;
        }
    }

    static int IndexOf(string id)
    {
        for (int i = 0; i < BossIntros.Length; i++)
            if (BossIntros[i].id == id) return i;
        return -1;
    }

    static void ShowLetterbox(string banner)
    {
        EnsureLetterbox();
        if (bannerText != null)
        {
            bannerText.text = banner ?? "";
            bannerText.enabled = !string.IsNullOrEmpty(banner);
        }
        if (letterbox != null) letterbox.SetActive(true);
    }

    static void HideLetterbox()
    {
        if (letterbox != null) letterbox.SetActive(false);
    }

    static void EnsureLetterbox()
    {
        if (letterbox != null) return;
        letterbox = new GameObject("CutsceneLetterbox");
        var canvas = letterbox.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        letterbox.SetActive(false);
        AddBar("TopBar", 1f, 1f);
        AddBar("BottomBar", 0f, 0f);
        bannerText = DiabloUIFactory.MakeText(letterbox.transform, "CutsceneBanner", "", 34,
            DiabloPalette.GoldBright, TextAnchor.MiddleCenter);
        var rt = bannerText.rectTransform;
        rt.anchorMin = new Vector2(0f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
        rt.offsetMin = new Vector2(0f, 40f); rt.offsetMax = new Vector2(0f, 110f);
        bannerText.enabled = false;
    }

    static void AddBar(string name, float anchorY, float pivotY)
    {
        var go = new GameObject(name);
        go.transform.SetParent(letterbox.transform, false);
        var img = go.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, anchorY); rt.anchorMax = new Vector2(1f, anchorY);
        rt.pivot = new Vector2(0.5f, pivotY);
        rt.sizeDelta = new Vector2(0f, 140f);
    }
}
