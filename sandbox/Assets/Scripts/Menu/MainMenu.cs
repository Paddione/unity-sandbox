using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main menu (presentation S4): Title state over live scene backdrop,
/// New/Continue/Settings/Quit + Host/Join (coop overlay, hidden when
/// JoinPanel unavailable). Continue only with save. Settings edits
/// GameSettings.Current (resolution, fullscreen, volumes, shake,
/// damage numbers) and persists on close. HD scaler 1920x1080 match 0.5.
/// </summary>
public sealed class MainMenu : MonoBehaviour
{
    public enum State { Title, Settings, Playing }
    public static State CurrentState = State.Title;
    public static int Opens;
    public static string LastAction = "";

    Canvas canvas;
    GameObject settingsPanel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var args = System.Environment.GetCommandLineArgs();
        foreach (var a in args)
            if (a == SessionAutoplay.Flag || a == "-shotcheck" || a == "-batchmode"
                || a == "--net-client" || a == "--net-server" || a == DedicatedServer.Flag)
                return; // automation boots straight into gameplay
        if (Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include) != null) return;
        if (ClassSelectOwnsStart(args)) return;
        var go = new GameObject("MainMenu");
        go.AddComponent<MainMenu>().Open();
    }

    /// <summary>
    /// One start screen: in the game scene the startup/class screen
    /// (<see cref="ClassSelectUI"/>) already offers Continue, Co-op, Settings
    /// and Quit, so this legacy title would only stack a second screen above it.
    /// </summary>
    public static bool ClassSelectOwnsStart(string[] args) =>
        ClassSelectUI.ShouldShow(args)
        && Object.FindFirstObjectByType<ClassAppearance>(FindObjectsInactive.Include) != null;

    public void Open()
    {
        if (canvas == null) Build();
        CurrentState = State.Title;
        Opens++;
        canvas.enabled = true;
        DiabloClickController.ExternalDrive = true;
        LastAction = "open";
    }

    public void CloseToGame()
    {
        CurrentState = State.Playing;
        if (canvas != null) canvas.enabled = false;
        DiabloClickController.ExternalDrive = false;
        LastAction = "play";
    }

    public bool NewGame(bool confirmOverwrite)
    {
        if (SaveService.HasSave() && !confirmOverwrite) return false;
        SaveService.DeleteSave();
        LastAction = "new";
        CloseToGame();
        return true;
    }

    public bool ContinueGame()
    {
        var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        if (hero == null || !SaveService.HasSave()) return false;
        if (!SaveService.TryLoad(hero)) return false;
        LastAction = "continue";
        CloseToGame();
        return true;
    }

    public void OpenSettings()
    {
        CurrentState = State.Settings;
        if (settingsPanel != null) settingsPanel.SetActive(true);
        LastAction = "settings";
    }

    public void CloseSettings()
    {
        CurrentState = State.Title;
        if (settingsPanel != null) settingsPanel.SetActive(false);
        GameSettings.Save();
        LastAction = "settings-closed";
    }

    public void Quit()
    {
        LastAction = "quit";
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void HostOrJoin()
    {
        LastAction = "coop";
        try { JoinPanel.Open(); } catch { }
    }

    void Build()
    {
        var root = new GameObject("MainMenuCanvas");
        root.transform.SetParent(transform, false);
        canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        var panel = Panel(root.transform, "MenuPanel", new Vector2(560f, 640f), Vector2.zero);
        Label(panel.transform, "CINDERREACH", 56, new Vector2(0f, 200f), new Vector2(520f, 80f), DiabloPalette.TrimGold);
        Label(panel.transform, Loc.T("menu.main.press"), 14, new Vector2(0f, 140f), new Vector2(520f, 30f), new Color(0.85f, 0.8f, 0.7f, 0.7f));

        bool hasSave = false;
        try { hasSave = SaveService.HasSave(); } catch { }
        float y = 60f;
        if (hasSave) { Button(panel.transform, Loc.T("menu.continue.button"), new Vector2(0f, y), () => ContinueGame()); y -= 64f; }
        Button(panel.transform, hasSave ? Loc.T("menu.main.new_overwrite") : Loc.T("menu.main.new"), new Vector2(0f, y), () => NewGame(hasSave));
        y -= 64f;
        Button(panel.transform, Loc.T("menu.main.coop"), new Vector2(0f, y), () => HostOrJoin());
        y -= 64f;
        Button(panel.transform, Loc.T("menu.settings"), new Vector2(0f, y), () => OpenSettings());
        y -= 64f;
        Button(panel.transform, Loc.T("menu.quit"), new Vector2(0f, y), () => Quit());

        settingsPanel = Panel(root.transform, "SettingsPanel", new Vector2(560f, 420f), Vector2.zero);
        Label(settingsPanel.transform, Loc.T("menu.settings.title"), 28, new Vector2(0f, 160f), new Vector2(520f, 50f), DiabloPalette.TrimGold);
        var s = GameSettings.Current;
        SliderRow(settingsPanel.transform, Loc.T("settings.master"), new Vector2(0f, 90f), s.master, v => { GameSettings.Current.master = v; GameSettings.Current.Apply(); });
        SliderRow(settingsPanel.transform, Loc.T("settings.music"), new Vector2(0f, 40f), s.music, v => { GameSettings.Current.music = v; GameSettings.Current.Apply(); });
        SliderRow(settingsPanel.transform, Loc.T("settings.sfx"), new Vector2(0f, -10f), s.sfx, v => { GameSettings.Current.sfx = v; GameSettings.Current.Apply(); });
        ToggleRow(settingsPanel.transform, Loc.T("settings.shake"), new Vector2(-120f, -70f), s.shake, v => { GameSettings.Current.shake = v; GameSettings.Current.Apply(); });
        ToggleRow(settingsPanel.transform, Loc.T("settings.dmgnums"), new Vector2(120f, -70f), s.damageNumbers, v => { GameSettings.Current.damageNumbers = v; GameSettings.Current.Apply(); });
        Button(settingsPanel.transform, Loc.T("menu.back"), new Vector2(0f, -140f), () => CloseSettings());
        settingsPanel.SetActive(false);
        canvas.enabled = false;
    }

    static GameObject Panel(Transform parent, string name, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = DiabloMenuArt.PanelBackground();
        img.type = Image.Type.Sliced;
        img.color = new Color(1f, 1f, 1f, 0.96f);
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        return go;
    }

    static Text Label(Transform parent, string text, int size, Vector2 pos, Vector2 box, Color color)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta = box;
        r.anchoredPosition = pos;
        return t;
    }

    static void Button(Transform parent, string text, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = DiabloMenuArt.TabBackground(false);
        img.type = Image.Type.Sliced;
        var b = go.AddComponent<Button>();
        b.onClick.AddListener(onClick);
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta = new Vector2(340f, 52f);
        r.anchoredPosition = pos;
        Label(go.transform, text, 20, Vector2.zero, new Vector2(340f, 52f), DiabloPalette.Parchment);
    }

    static void SliderRow(Transform parent, string name, Vector2 pos, float value, System.Action<float> onChange)
    {
        var go = new GameObject("Slider_" + name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta = new Vector2(460f, 40f);
        r.anchoredPosition = pos;
        Label(go.transform, name, 16, new Vector2(-160f, 0f), new Vector2(120f, 40f), DiabloPalette.Parchment);
        var sgo = new GameObject("S", typeof(RectTransform));
        sgo.transform.SetParent(go.transform, false);
        var slider = sgo.AddComponent<Slider>();
        var sr = sgo.GetComponent<RectTransform>();
        sr.sizeDelta = new Vector2(280f, 24f);
        sr.anchoredPosition = new Vector2(60f, 0f);
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = value;
        slider.onValueChanged.AddListener(v => onChange(v));
    }

    static void ToggleRow(Transform parent, string name, Vector2 pos, bool value, System.Action<bool> onChange)
    {
        var go = new GameObject("Toggle_" + name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta = new Vector2(200f, 40f);
        r.anchoredPosition = pos;
        Label(go.transform, name, 16, new Vector2(-30f, 0f), new Vector2(120f, 40f), DiabloPalette.Parchment);
        var tgo = new GameObject("T");
        tgo.transform.SetParent(go.transform, false);
        var t = tgo.AddComponent<Toggle>();
        t.isOn = value;
        t.onValueChanged.AddListener(v => onChange(v));
        var tr = tgo.GetComponent<RectTransform>();
        tr.anchoredPosition = new Vector2(70f, 0f);
        tr.sizeDelta = new Vector2(24f, 24f);
    }

    public static void ResetForTests()
    {
        CurrentState = State.Title;
        LastAction = "";
    }
}
