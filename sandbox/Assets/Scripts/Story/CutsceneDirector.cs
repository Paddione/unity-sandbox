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
/// Ability showcases (diablo-ability-showcases): the intro is extended to
/// 8–12 s and the boss demonstrates each phase-1 signature ability once as a
/// harmless presentation cast (telegraph/VFX only, never damage) with a
/// localized caption; at phase 2 a ~4 s showcase repeats the pattern for the
/// new abilities while the boss holds and the hero keeps full control.
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
        /// <summary>Localized caption keys of the phase-1 abilities showcased in the intro.</summary>
        public string[] phaseOneAbilities;
        /// <summary>Localized caption keys of the phase-2 abilities showcased at the phase change.</summary>
        public string[] phaseTwoAbilities;
        /// <summary>Played-set id of the phase-2 showcase (once per run, ResetRun re-arms).</summary>
        public string phaseTwoId;
        public float phaseTwoDuration;
    }

    public static readonly BossIntro[] BossIntros =
    {
        new BossIntro { id = "intro.abbot", anchorName = BossController.BossName, triggerRange = 12f, bannerKey = "name.enemy.abbot", duration = 9f,
            phaseOneAbilities = new[] { "cutscene.ability.abbot.slam", "cutscene.ability.abbot.combo" },
            phaseTwoAbilities = new[] { "cutscene.ability.abbot.summon", "cutscene.ability.abbot.firefan" },
            phaseTwoId = "phase2.abbot", phaseTwoDuration = 4f },
        new BossIntro { id = "intro.kilnmother", anchorName = KilnLayout.KilnmotherName, triggerRange = 24f, bannerKey = "name.enemy.kilnmother", duration = 9f,
            phaseOneAbilities = new[] { "cutscene.ability.kilnmother.wave" },
            phaseTwoAbilities = new[] { "cutscene.ability.kilnmother.field", "cutscene.ability.kilnmother.summon" },
            phaseTwoId = "phase2.kilnmother", phaseTwoDuration = 4f },
        new BossIntro { id = "intro.glasswright", anchorName = GlasswrightController.BossName, triggerRange = 16f, bannerKey = "name.enemy.glasswright", duration = 9f,
            phaseOneAbilities = new[] { "cutscene.ability.glasswright.lances" },
            phaseTwoAbilities = new[] { "cutscene.ability.glasswright.pillars", "cutscene.ability.glasswright.field" },
            phaseTwoId = "phase2.glasswright", phaseTwoDuration = 4f },
    };

    /// <summary>True while a boss intro runs: the three boss brains hold still (no aggro, no attacks).</summary>
    public static bool HoldBosses { get; private set; }
    /// <summary>True while any sequence (boss intro or highlighted dialogue) runs.</summary>
    public static bool Active { get; private set; }
    /// <summary>Sequence id, or "" when idle.</summary>
    public static string ActiveId { get; private set; } = "";
    /// <summary>Starts, skips and natural completions this run (test seams).</summary>
    public static int Starts, Skips, Completes;
    /// <summary>Harmless showcase casts fired this run (test seam).</summary>
    public static int ShowcaseCasts;
    /// <summary>Caption key of the most recent showcase cast, or "" (test seam).</summary>
    public static string CurrentAbilityKey { get; private set; } = "";
    static readonly Dictionary<string, int> showcaseCounts = new Dictionary<string, int>();
    static readonly HashSet<string> played = new HashSet<string>();
    static bool phaseTwoMode;
    static int showcaseStep;

    /// <summary>Showcase casts of one ability caption key this run.</summary>
    public static int ShowcaseCastCount(string key) =>
        key != null && showcaseCounts.TryGetValue(key, out var n) ? n : 0;

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
        ShowcaseCasts = 0;
        CurrentAbilityKey = "";
        showcaseCounts.Clear();
        phaseTwoMode = false;
        showcaseStep = 0;
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
        StartSequence(intro, id, anchor, hero, now, phaseTwo: false);
        return true;
    }

    /// <summary>Headless-callable phase-2 showcase, called from each boss's EnterPhaseTwo.
    /// False when busy, unknown intro id or already played this run. The boss holds
    /// (HoldBosses) but the hero keeps full control and the camera stays put.</summary>
    public static bool TryStartPhaseTwoShowcase(string introId, float now)
    {
        if (Active) return false;
        int idx = IndexOf(introId);
        if (idx < 0) return false;
        var intro = BossIntros[idx];
        if (string.IsNullOrEmpty(intro.phaseTwoId) || HasPlayed(intro.phaseTwoId)) return false;
        var anchor = GameObject.Find(intro.anchorName);
        if (anchor == null || !BossAlive(anchor)) return false;
        var hero = HeroLocator.Solo();
        if (hero == null) return false;
        StartSequence(intro, intro.phaseTwoId, anchor, hero, now, phaseTwo: true);
        return true;
    }

    static void StartSequence(BossIntro intro, string sequenceId, GameObject anchor, Transform hero, float now, bool phaseTwo)
    {
        Active = true;
        ActiveId = sequenceId;
        HoldBosses = true;
        Starts++;
        played.Add(sequenceId);
        t0 = now;
        roared = phaseTwo; // phase-2 entry already roared in EnterPhaseTwo
        phaseTwoMode = phaseTwo;
        showcaseStep = 0;
        CurrentAbilityKey = "";
        if (!phaseTwo)
        {
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
        }
        ShowLetterbox(Loc.T(intro.bannerKey));
    }

    static void TickBossIntro(float now)
    {
        int idx = IndexOfBoss(ActiveId);
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
        if (!phaseTwoMode)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                float t = Mathf.Clamp01((now - t0) / 1.2f);
                float e = t * t * (3f - 2f * t);
                cam.transform.position = Vector3.Lerp(dollyFrom, dollyTo, e);
                cam.transform.rotation = Quaternion.Slerp(dollyFromRot, dollyToRot, e);
            }
        }
        TickShowcaseSteps(intro, anchor.transform.position, hero.position, now);
        float duration = phaseTwoMode ? intro.phaseTwoDuration : intro.duration;
        if (SkipPressed() || now - t0 >= duration) End(SkipPressed());
    }

    /// <summary>Fire each due showcase cast once: caption + harmless presentation at the boss.</summary>
    static void TickShowcaseSteps(BossIntro intro, Vector3 bossPos, Vector3 heroPos, float now)
    {
        var abilities = phaseTwoMode ? intro.phaseTwoAbilities : intro.phaseOneAbilities;
        if (abilities == null) return;
        float duration = phaseTwoMode ? intro.phaseTwoDuration : intro.duration;
        while (showcaseStep < abilities.Length)
        {
            float at = duration * (showcaseStep + 1) / (abilities.Length + 1);
            if (now - t0 < at) break;
            string key = abilities[showcaseStep++];
            CurrentAbilityKey = key;
            ShowcaseCasts++;
            showcaseCounts[key] = ShowcaseCastCount(key) + 1;
            ShowLetterbox(Loc.T(key));
            PlayShowcaseVisual(key, bossPos, heroPos);
        }
    }

    /// <summary>Harmless demo cast: the fight's own telegraph/VFX presentation, never damage,
    /// never a lasting add, field or projectile. Glasswright lances fire away from the hero.</summary>
    static void PlayShowcaseVisual(string key, Vector3 bossPos, Vector3 heroPos)
    {
        try
        {
            switch (key)
            {
                case "cutscene.ability.abbot.slam":
                    PresentationVfx.BossTelegraph(bossPos, BossController.SlamRadius);
                    break;
                case "cutscene.ability.abbot.combo":
                    PresentationVfx.CleaveArc(bossPos, 2.2f);
                    break;
                case "cutscene.ability.kilnmother.wave":
                    PresentationVfx.CleaveArc(bossPos, 4f);
                    PresentationVfx.BossTelegraph(bossPos, 3f);
                    break;
                case "cutscene.ability.kilnmother.field":
                    PresentationVfx.BossTelegraph(heroPos, 2f);
                    break;
                case "cutscene.ability.glasswright.lances":
                    {
                        var away = bossPos - heroPos;
                        away.y = 0f;
                        if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
                        away.Normalize();
                        for (int i = -1; i <= 1; i++)
                            ShardLance.Spawn(bossPos, Quaternion.Euler(0f, i * GlasswrightController.LanceFanDegrees, 0f) * away);
                        break;
                    }
                case "cutscene.ability.glasswright.field":
                    PresentationVfx.BossTelegraph(heroPos, 2f);
                    break;
                default: // summons, fire fan, pillars: eruption at the boss (real adds/pillars already stand)
                    PresentationVfx.PhaseTwoBurst(bossPos);
                    if (key == "cutscene.ability.abbot.firefan")
                        PresentationVfx.FireballTrail(bossPos + Vector3.up * 1.4f);
                    break;
            }
        }
        catch { }
    }

    static int IndexOfBoss(string sequenceId)
    {
        int idx = IndexOf(sequenceId);
        if (idx >= 0) return idx;
        for (int i = 0; i < BossIntros.Length; i++)
            if (BossIntros[i].phaseTwoId == sequenceId) return i;
        return -1;
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
        if (!phaseTwoMode) // phase-2 showcase never moved the camera
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = camPos;
                cam.transform.rotation = camRot;
            }
        }
        HideLetterbox();
        Active = false;
        ActiveId = "";
        HoldBosses = false;
        phaseTwoMode = false;
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
