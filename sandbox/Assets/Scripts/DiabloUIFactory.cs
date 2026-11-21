using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Shared dark-fantasy UGUI chrome helpers, layout sizing, input guards and Esc/modal
/// coordination across all Cinderreach menus so widgets stay visually consistent and
/// headless EditMode/PlayMode tests never hit missing layout or missing raycaster traps.
/// </summary>
public static class DiabloUIFactory
{
    static Font cachedFont;
    public static int EscConsumedFrame = -1;

    public static Font DefaultFont
    {
        get
        {
            if (cachedFont == null) cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return cachedFont;
        }
    }

    public static Canvas ConfigureOverlayCanvas(GameObject canvasGo, int sortingOrder = 10, bool raycaster = true)
    {
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        if (raycaster) canvasGo.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static bool IsRemoteMirror(Component host)
    {
        if (host == null) return false;
        var net = host.GetComponent<DiabloNetworkPlayer>();
        return net != null && net.IsSpawned && !net.IsOwner;
    }

    /// <summary>Records that Esc closed a top modal this frame so PauseMenu ignores the same keypress.</summary>
    public static void MarkEscConsumed()
    {
        EscConsumedFrame = Time.frameCount;
    }

    public static bool WasEscConsumedThisFrame => EscConsumedFrame == Time.frameCount;

    /// <summary>True whenever any modal UI is open or pointer hovers UI.</summary>
    public static bool IsAnyModalOpen(GameObject hero = null)
    {
        if (DiabloClickController.ExternalDrive) return true;
        if (ClassSelectUI.AnyOpen) return true;
        if (PauseMenuUI.AnyOpen) return true;
        if (hero == null) hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        if (hero != null)
        {
            var inv = hero.GetComponent<DiabloInventoryUI>();
            if (inv != null && inv.IsOpen) return true;
            var ch = hero.GetComponent<DiabloCharacterUI>();
            if (ch != null && ch.IsOpen) return true;
            var ven = hero.GetComponent<DiabloVendorUI>();
            if (ven != null && ven.IsOpen) return true;
            var st = hero.GetComponent<DiabloStashUI>();
            if (st != null && st.IsOpen) return true;
            var wp = hero.GetComponent<DiabloWaypointUI>();
            if (wp != null && wp.IsOpen) return true;
        }
        return false;
    }

    /// <summary>Blocks world NPC interaction / combat clicks while any menu or UI element captures input.</summary>
    public static bool IsWorldInputBlocked(GameObject hero = null)
    {
        if (IsAnyModalOpen(hero)) return true;
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>True when Inventory/Character/Vendor/Stash/Waypoint/Pause owns number/action hotkeys.</summary>
    public static bool IsMenuCapturingKeys(GameObject hero)
    {
        if (DiabloClickController.ExternalDrive) return true;
        if (hero != null)
        {
            var inv = hero.GetComponent<DiabloInventoryUI>();
            if (inv != null && inv.IsOpen) return true;
            var ch = hero.GetComponent<DiabloCharacterUI>();
            if (ch != null && ch.IsOpen) return true;
        }
        return PauseMenuUI.AnyOpen;
    }

    public static void CloseSecondaryPanels(GameObject hero, MonoBehaviour except = null)
    {
        if (hero == null) return;
        var inv = hero.GetComponent<DiabloInventoryUI>();
        if (inv != null && inv != except && inv.IsOpen) inv.SetOpen(false);
        var ch = hero.GetComponent<DiabloCharacterUI>();
        if (ch != null && ch != except && ch.IsOpen) ch.SetOpen(false);
        var ven = hero.GetComponent<DiabloVendorUI>();
        if (ven != null && ven != except && ven.IsOpen) ven.SetOpen(false);
        var st = hero.GetComponent<DiabloStashUI>();
        if (st != null && st != except && st.IsOpen) st.SetOpen(false);
        var wp = hero.GetComponent<DiabloWaypointUI>();
        if (wp != null && wp != except && wp.IsOpen) wp.SetOpen(false);
    }

    public static LayoutElement SetLayout(GameObject go, float prefH = -1f, float prefW = -1f, float flexW = -1f, float flexH = -1f)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (prefH >= 0f) { le.preferredHeight = prefH; le.minHeight = prefH; }
        if (prefW >= 0f) { le.preferredWidth = prefW; le.minWidth = prefW; }
        if (flexW >= 0f) le.flexibleWidth = flexW;
        if (flexH >= 0f) le.flexibleHeight = flexH;
        // Most panels run their layout groups with childControlHeight = false, which
        // positions children by their RectTransform size and ignores LayoutElement.
        // Mirror the preferred size onto the rect so those groups stack correctly
        // (a fresh RectTransform is 100x100 and made every row 100 px tall).
        var rt = go.transform as RectTransform;
        if (rt != null)
        {
            var size = rt.sizeDelta;
            if (prefH >= 0f) size.y = prefH;
            if (prefW >= 0f) size.x = prefW;
            rt.sizeDelta = size;
        }
        return le;
    }

    public static GameObject MakePanel(Transform parent, string name, Vector2 size, Vector2 pos, Sprite sprite = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite ?? DiabloMenuArt.PanelBackground();
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return go;
    }

    public static Text MakeText(Transform parent, string name, string body, int size, Color color,
        TextAnchor align = TextAnchor.MiddleLeft, bool shadow = true, bool richText = true)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = DefaultFont;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.supportRichText = richText;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.text = body;
        t.raycastTarget = false;
        if (shadow)
        {
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
            sh.effectDistance = new Vector2(1.5f, -1.5f);
        }
        return t;
    }

    public static GameObject MakeHeader(Transform parent, string title, Sprite badgeSprite = null,
        UnityEngine.Events.UnityAction onClose = null, float height = 52f)
    {
        var go = new GameObject("Header", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        SetLayout(go, prefH: height, flexW: 1f);
        var bg = go.AddComponent<Image>();
        bg.sprite = DiabloMenuArt.HeaderBanner();
        bg.type = Image.Type.Sliced;

        if (badgeSprite != null)
        {
            var iconGo = new GameObject("BadgeIcon", typeof(RectTransform));
            iconGo.transform.SetParent(go.transform, false);
            var img = iconGo.AddComponent<Image>();
            img.sprite = badgeSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            var irt = iconGo.GetComponent<RectTransform>();
            irt.anchorMin = irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.sizeDelta = new Vector2(36f, 36f);
            irt.anchoredPosition = new Vector2(14f, 0f);
        }

        var titleText = MakeText(go.transform, "BannerLabel", title, 21, DiabloPalette.TrimGold, TextAnchor.MiddleCenter);
        titleText.fontStyle = FontStyle.Bold;
        var trt = titleText.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(48f, 2f);
        trt.offsetMax = new Vector2(-48f, -2f);

        if (onClose != null)
        {
            var closeBtn = MakeButton(go.transform, "CloseIconButton", "X", 16, false, DiabloPalette.Parchment, onClose);
            var crt = closeBtn.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(1f, 0.5f);
            crt.pivot = new Vector2(1f, 0.5f);
            crt.sizeDelta = new Vector2(34f, 32f);
            crt.anchoredPosition = new Vector2(-10f, 0f);
        }
        return go;
    }

    /// <summary>
    /// Multi-line item cards (ItemDetailsView) vary from 2 to 10 lines; shrink
    /// to fit the box instead of spilling past its frame.
    /// </summary>
    public static void FitToBox(Text t, int maxSize, int minSize = 9)
    {
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize = minSize;
        t.resizeTextMaxSize = maxSize;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.alignment = TextAnchor.UpperLeft;
    }

    public static void MakeDivider(Transform parent, float height = 6f)
    {
        var go = new GameObject("Divider", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        SetLayout(go, prefH: height, flexW: 1f);
        var img = go.AddComponent<Image>();
        img.sprite = DiabloMenuArt.DividerLine();
        img.preserveAspect = false;
        img.raycastTarget = false;
    }

    public static Button MakeButton(Transform parent, string name, string text, int fontSize = 16,
        bool primary = false, Color? textColor = null, UnityEngine.Events.UnityAction onClick = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = DiabloMenuArt.ButtonBackground(primary);
        img.type = Image.Type.Sliced;
        var btn = go.AddComponent<Button>();
        var cols = btn.colors;
        cols.normalColor = Color.white;
        cols.highlightedColor = new Color(1.18f, 1.12f, 1.04f, 1f);
        cols.pressedColor = new Color(0.82f, 0.78f, 0.74f, 1f);
        cols.disabledColor = new Color(0.45f, 0.43f, 0.42f, 0.75f);
        btn.colors = cols;
        if (onClick != null) btn.onClick.AddListener(onClick);

        var lbl = MakeText(go.transform, "Label", text, fontSize, textColor ?? (primary ? DiabloPalette.GoldBright : DiabloPalette.Parchment),
            TextAnchor.MiddleCenter);
        lbl.fontStyle = primary ? FontStyle.Bold : FontStyle.Normal;
        var lr = lbl.rectTransform;
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(10f, 2f);
        lr.offsetMax = new Vector2(-10f, -2f);
        return btn;
    }

    public static Slider MakeSliderRow(Transform parent, string name, float value, UnityEngine.Events.UnityAction<float> onChange, out Text pctLabel)
    {
        var row = new GameObject(name + "Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        SetLayout(row, prefH: 30f, flexW: 1f);
        var hz = row.AddComponent<HorizontalLayoutGroup>();
        hz.spacing = 10f;
        hz.childAlignment = TextAnchor.MiddleLeft;
        hz.childControlWidth = true;
        hz.childControlHeight = true;
        hz.childForceExpandWidth = false;
        hz.childForceExpandHeight = false;

        var lbl = MakeText(row.transform, "Label", name, 15, DiabloPalette.Parchment);
        SetLayout(lbl.gameObject, prefH: 28f, prefW: 110f);

        var sliderGo = new GameObject(name + "Slider", typeof(RectTransform));
        sliderGo.transform.SetParent(row.transform, false);
        SetLayout(sliderGo, prefH: 22f, prefW: 200f, flexW: 1f);
        var bg = sliderGo.AddComponent<Image>();
        bg.sprite = DiabloMenuArt.SliderTrackSprite();
        bg.type = Image.Type.Sliced;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        var far = fillArea.GetComponent<RectTransform>();
        far.anchorMin = new Vector2(0f, 0.15f);
        far.anchorMax = new Vector2(1f, 0.85f);
        far.offsetMin = new Vector2(4f, 0f);
        far.offsetMax = new Vector2(-4f, 0f);

        var fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        var fillImg = fill.AddComponent<Image>();
        fillImg.sprite = DiabloMenuArt.SliderFillSprite();
        fillImg.type = Image.Type.Sliced;
        var fr = fill.GetComponent<RectTransform>();
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = Vector2.zero;
        fr.offsetMax = Vector2.zero;

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderGo.transform, false);
        var har = handleArea.GetComponent<RectTransform>();
        har.anchorMin = Vector2.zero;
        har.anchorMax = Vector2.one;
        har.offsetMin = new Vector2(8f, 0f);
        har.offsetMax = new Vector2(-8f, 0f);

        var handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(handleArea.transform, false);
        var hImg = handle.AddComponent<Image>();
        hImg.sprite = DiabloMenuArt.SliderKnobSprite();
        var hr = handle.GetComponent<RectTransform>();
        hr.sizeDelta = new Vector2(18f, 22f);

        var slider = sliderGo.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.fillRect = fr;
        slider.handleRect = hr;
        slider.targetGraphic = hImg;
        slider.value = value;

        var pct = MakeText(row.transform, "Pct", $"{Mathf.RoundToInt(value * 100f)}%", 14, DiabloPalette.TrimGold, TextAnchor.MiddleRight);
        SetLayout(pct.gameObject, prefH: 26f, prefW: 46f);
        pctLabel = pct;

        slider.onValueChanged.AddListener(v =>
        {
            pct.text = $"{Mathf.RoundToInt(v * 100f)}%";
            onChange?.Invoke(v);
        });
        return slider;
    }

    public static Button MakeSmallButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick = null, float height = 34f, float width = -1f)
    {
        var btn = MakeButton(parent, "SmallButton", text, 14, false, DiabloPalette.Parchment, onClick);
        SetLayout(btn.gameObject, prefH: height, prefW: width, flexW: width < 0f ? 1f : 0f);
        return btn;
    }

    public static void StyleButtonActive(Button b, bool active, string label = null)
    {
        if (b == null) return;
        var img = b.GetComponent<Image>();
        if (img != null) img.sprite = DiabloMenuArt.TabBackground(active);
        var txt = b.GetComponentInChildren<Text>();
        if (txt != null)
        {
            if (label != null) txt.text = label;
            txt.color = active ? DiabloPalette.TrimGold : DiabloPalette.ParchmentDim;
        }
    }

    /// <summary>Builds a complete reusable Settings section (Volume, Display, Quality, Gameplay, Diff/Reset) cleanly.</summary>
    public static void BuildSettingsWidgets(Transform parent, List<UnityEngine.Events.UnityAction> refreshActions,
        UnityAction<string> onStartNewRun = null, UnityEngine.Events.UnityAction onBack = null)
    {
        var sMaster = MakeSliderRow(parent, "Master", DiabloSettings.MasterVol, v => { DiabloSettings.MasterVol = v; DiabloSettings.Save(); DiabloSettings.Apply(); }, out var tMaster);
        var sMusic = MakeSliderRow(parent, "Music", DiabloSettings.MusicVol, v => { DiabloSettings.MusicVol = v; DiabloSettings.Save(); DiabloSettings.Apply(); }, out var tMusic);
        var sSfx = MakeSliderRow(parent, "SFX", DiabloSettings.SFXVol, v => { DiabloSettings.SFXVol = v; DiabloSettings.Save(); DiabloSettings.Apply(); }, out var tSfx);
        var sUi = MakeSliderRow(parent, "UI", DiabloSettings.UIVol, v => { DiabloSettings.UIVol = v; DiabloSettings.Save(); DiabloSettings.Apply(); }, out var tUi);

        MakeDivider(parent);

        var qRow = MakeHorizontalRow(parent, "QualityRow", 6f, 34f);
        var qLbl = MakeText(qRow.transform, "QLbl", "Quality:", 14, DiabloPalette.ParchmentDim);
        SetLayout(qLbl.gameObject, prefH: 30f, prefW: 72f);
        string[] qNames = { "Low", "Med", "High" };
        var qBtns = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            int level = i;
            qBtns[i] = MakeSmallButton(qRow.transform, qNames[i], () =>
            {
                DiabloAudio.PlayUIClick();
                DiabloSettings.QualityLevel = level;
                DiabloSettings.Save();
                DiabloSettings.Apply();
                RefreshAll(refreshActions);
            }, 32f);
        }

        var dRow = MakeHorizontalRow(parent, "DisplayRow", 6f, 34f);
        var resBtn = MakeSmallButton(dRow.transform, DiabloSettings.ResolutionLabel, () =>
        {
            DiabloAudio.PlayUIClick();
            DiabloSettings.CycleResolution();
            DiabloSettings.Save();
            DiabloSettings.Apply();
            RefreshAll(refreshActions);
        }, 32f);
        var fullBtn = MakeSmallButton(dRow.transform, "Fullscreen", () =>
        {
            DiabloAudio.PlayUIClick();
            DiabloSettings.Fullscreen = !DiabloSettings.Fullscreen;
            DiabloSettings.Save();
            DiabloSettings.Apply();
            RefreshAll(refreshActions);
        }, 32f);
        var vsyncBtn = MakeSmallButton(dRow.transform, "VSync", () =>
        {
            DiabloAudio.PlayUIClick();
            DiabloSettings.VSync = !DiabloSettings.VSync;
            DiabloSettings.Save();
            DiabloSettings.Apply();
            RefreshAll(refreshActions);
        }, 32f);

        var tRow = MakeHorizontalRow(parent, "GameplayRow", 6f, 34f);
        var fpsBtn = MakeSmallButton(tRow.transform, "FPS", () =>
        {
            DiabloAudio.PlayUIClick();
            DiabloSettings.ShowFPS = !DiabloSettings.ShowFPS;
            DiabloSettings.Save();
            DiabloSettings.Apply();
            RefreshAll(refreshActions);
        }, 32f);
        var shakeBtn = MakeSmallButton(tRow.transform, "Shake", () =>
        {
            DiabloAudio.PlayUIClick();
            DiabloSettings.ScreenShake = !DiabloSettings.ScreenShake;
            DiabloSettings.Save();
            DiabloSettings.Apply();
            RefreshAll(refreshActions);
        }, 32f);
        var hitBtn = MakeSmallButton(tRow.transform, "HitStop", () =>
        {
            DiabloAudio.PlayUIClick();
            DiabloSettings.HitStop = !DiabloSettings.HitStop;
            DiabloSettings.Save();
            DiabloSettings.Apply();
            RefreshAll(refreshActions);
        }, 32f);
        var dmgBtn = MakeSmallButton(tRow.transform, "DmgNum", () =>
        {
            DiabloAudio.PlayUIClick();
            DiabloSettings.ShowDamageNumbers = !DiabloSettings.ShowDamageNumbers;
            DiabloSettings.Save();
            DiabloSettings.Apply();
            RefreshAll(refreshActions);
        }, 32f);

        Text diffText = null;
        Button kindledBtn = null;
        if (onStartNewRun != null)
        {
            MakeDivider(parent);
            diffText = MakeText(parent, "DiffStatus", DiabloInventoryUI.DifficultyText(), 14, DiabloPalette.TrimGold);
            SetLayout(diffText.gameObject, prefH: 22f, flexW: 1f);
            var runRow = MakeHorizontalRow(parent, "RunDiffRow", 6f, 34f);
            MakeSmallButton(runRow.transform, "New run (Normal)", () => onStartNewRun(RunProfile.Normal), 32f);
            kindledBtn = MakeSmallButton(runRow.transform, "New run (Kindled)", () => onStartNewRun(RunProfile.Kindled), 32f);
        }

        var footer = MakeHorizontalRow(parent, "SettingsFooter", 8f, 36f);
        MakeSmallButton(footer.transform, "Reset Defaults", () =>
        {
            DiabloAudio.PlayUIClick();
            DiabloSettings.ResetDefaults();
            RefreshAll(refreshActions);
        }, 34f);
        if (onBack != null)
        {
            var closeB = MakeButton(footer.transform, "CloseButton", "Done", 15, true, DiabloPalette.GoldBright, onBack);
            SetLayout(closeB.gameObject, prefH: 34f, flexW: 1f);
        }

        refreshActions.Add(() =>
        {
            if (Mathf.Abs(sMaster.value - DiabloSettings.MasterVol) > 0.001f) sMaster.value = DiabloSettings.MasterVol;
            if (Mathf.Abs(sMusic.value - DiabloSettings.MusicVol) > 0.001f) sMusic.value = DiabloSettings.MusicVol;
            if (Mathf.Abs(sSfx.value - DiabloSettings.SFXVol) > 0.001f) sSfx.value = DiabloSettings.SFXVol;
            if (Mathf.Abs(sUi.value - DiabloSettings.UIVol) > 0.001f) sUi.value = DiabloSettings.UIVol;
            tMaster.text = $"{Mathf.RoundToInt(DiabloSettings.MasterVol * 100f)}%";
            tMusic.text = $"{Mathf.RoundToInt(DiabloSettings.MusicVol * 100f)}%";
            tSfx.text = $"{Mathf.RoundToInt(DiabloSettings.SFXVol * 100f)}%";
            tUi.text = $"{Mathf.RoundToInt(DiabloSettings.UIVol * 100f)}%";

            for (int i = 0; i < 3; i++) StyleButtonActive(qBtns[i], DiabloSettings.QualityLevel == i, qNames[i]);
            StyleButtonActive(resBtn, true, DiabloSettings.ResolutionLabel);
            StyleButtonActive(fullBtn, DiabloSettings.Fullscreen, $"Screen: {(DiabloSettings.Fullscreen ? "Full" : "Win")}");
            StyleButtonActive(vsyncBtn, DiabloSettings.VSync, $"VSync: {(DiabloSettings.VSync ? "ON" : "OFF")}");
            StyleButtonActive(fpsBtn, DiabloSettings.ShowFPS, $"FPS: {(DiabloSettings.ShowFPS ? "ON" : "OFF")}");
            StyleButtonActive(shakeBtn, DiabloSettings.ScreenShake, $"Shake: {(DiabloSettings.ScreenShake ? "ON" : "OFF")}");
            StyleButtonActive(hitBtn, DiabloSettings.HitStop, $"HitStop: {(DiabloSettings.HitStop ? "ON" : "OFF")}");
            StyleButtonActive(dmgBtn, DiabloSettings.ShowDamageNumbers, $"Numbers: {(DiabloSettings.ShowDamageNumbers ? "ON" : "OFF")}");

            if (diffText != null) diffText.text = DiabloInventoryUI.DifficultyText();
            if (kindledBtn != null)
            {
                kindledBtn.interactable = RunProfile.DifficultyUnlocked;
                StyleButtonActive(kindledBtn, RunProfile.Difficulty == RunProfile.Kindled);
            }
        });
        RefreshAll(refreshActions);
    }

    public delegate void UnityAction<T>(T arg);

    public static GameObject MakeHorizontalRow(Transform parent, string name, float spacing = 8f, float height = 36f)
    {
        var row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        SetLayout(row, prefH: height, flexW: 1f);
        var hz = row.AddComponent<HorizontalLayoutGroup>();
        hz.spacing = spacing;
        hz.childControlWidth = true;
        hz.childControlHeight = true;
        hz.childForceExpandWidth = true;
        hz.childForceExpandHeight = false;
        return row;
    }

    static void RefreshAll(List<UnityEngine.Events.UnityAction> actions)
    {
        foreach (var a in actions) a?.Invoke();
    }
}
