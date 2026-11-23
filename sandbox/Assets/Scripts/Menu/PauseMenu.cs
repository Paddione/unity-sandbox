using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pause menu (presentation S4): Esc toggles Time.timeScale=0 + dimmed
/// overlay; Resume / Settings / Menu. Settings edits GameSettings.Current.
/// Menu returns to MainMenu title state. Unscaled UI so it works paused.
/// </summary>
public sealed class PauseMenu : MonoBehaviour
{
    public bool IsOpen { get; private set; }
    public static int Opens;

    Canvas canvas;
    GameObject settingsRow;

    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            if (MainMenu.CurrentState != MainMenu.State.Playing && !IsOpen) return;
            SetOpen(!IsOpen);
        }
    }

    public void SetOpen(bool open)
    {
        if (open && canvas == null) Build();
        IsOpen = open;
        if (canvas != null) canvas.enabled = open;
        Time.timeScale = open ? 0f : 1f;
        DiabloClickController.ExternalDrive = open || MainMenu.CurrentState != MainMenu.State.Playing;
        if (open) Opens++;
    }

    public void Resume() => SetOpen(false);

    public void BackToMenu()
    {
        SetOpen(false);
        Time.timeScale = 1f;
        var menu = Object.FindFirstObjectByType<MainMenu>();
        if (menu == null) menu = new GameObject("MainMenu").AddComponent<MainMenu>();
        menu.Open();
    }

    void Build()
    {
        var root = new GameObject("PauseCanvas");
        root.transform.SetParent(transform, false);
        canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        var dim = new GameObject("Dim", typeof(RectTransform));
        dim.transform.SetParent(root.transform, false);
        var dimg = dim.AddComponent<Image>();
        dimg.color = new Color(0.02f, 0.01f, 0.02f, 0.6f);
        var dr = dim.GetComponent<RectTransform>();
        dr.anchorMin = Vector2.zero; dr.anchorMax = Vector2.one;
        dr.offsetMin = Vector2.zero; dr.offsetMax = Vector2.zero;

        var panel = new GameObject("PausePanel", typeof(RectTransform));
        panel.transform.SetParent(root.transform, false);
        var pimg = panel.AddComponent<Image>();
        pimg.sprite = DiabloMenuArt.PanelBackground();
        pimg.type = Image.Type.Sliced;
        var pr = panel.GetComponent<RectTransform>();
        pr.sizeDelta = new Vector2(440f, 420f);
        pr.anchoredPosition = Vector2.zero;

        Label(panel.transform, Loc.T("menu.pause.title"), 32, new Vector2(0f, 150f), new Vector2(400f, 50f));
        Btn(panel.transform, Loc.T("menu.pause.resume.short"), new Vector2(0f, 70f), () => Resume());
        Btn(panel.transform, Loc.T("menu.pause.shake"), new Vector2(0f, 0f), () =>
        {
            GameSettings.Current.shake = !GameSettings.Current.shake;
            GameSettings.Current.Apply();
            GameSettings.Save();
        });
        Btn(panel.transform, Loc.T("menu.pause.menu"), new Vector2(0f, -70f), () => BackToMenu());
        canvas.enabled = false;
    }

    static Text Label(Transform parent, string text, int size, Vector2 pos, Vector2 box)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = DiabloPalette.TrimGold;
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta = box;
        r.anchoredPosition = pos;
        return t;
    }

    static void Btn(Transform parent, string text, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = DiabloMenuArt.TabBackground(false);
        img.type = Image.Type.Sliced;
        var b = go.AddComponent<Button>();
        b.onClick.AddListener(onClick);
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta = new Vector2(320f, 52f);
        r.anchoredPosition = pos;
        Label(go.transform, text, 18, Vector2.zero, new Vector2(320f, 52f));
    }
}
