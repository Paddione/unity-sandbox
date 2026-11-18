using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Co-op window (coop-session S1): host on a port or join "ip[:port]" without
/// restarting the game. Opened from the class select ("Co-op: host / join") or
/// with F7. Uses <see cref="MultiplayerAutoStarter.Restart"/>, so the same
/// rules as <c>-host</c>/<c>-join</c> apply (<see cref="JoinArgs"/>). HD
/// scaler convention: 1920×1080 reference, match 0.5.
/// </summary>
public sealed class JoinPanel : MonoBehaviour
{
    public const string ObjectName = "JoinPanel";

    public InputField AddressField { get; private set; }
    public Text Status { get; private set; }
    public bool IsOpen => canvas != null && canvas.enabled;

    Canvas canvas;
    float nextStatus;

    /// <summary>Open (and build on first use) the co-op window.</summary>
    public static JoinPanel Open()
    {
        var panel = Find();
        if (panel == null) panel = new GameObject(ObjectName).AddComponent<JoinPanel>();
        panel.SetOpen(true);
        return panel;
    }

    public static JoinPanel Find()
    {
        var go = GameObject.Find(ObjectName);
        return go != null ? go.GetComponent<JoinPanel>() : null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        // Hotkey host for F7; inert for automation runs.
        foreach (var a in System.Environment.GetCommandLineArgs())
            if (a == SessionAutoplay.Flag || a == CoopAutoplay.Flag || a == "-shotcheck" || a == "-batchmode") return;
        if (Object.FindFirstObjectByType<MultiplayerAutoStarter>() == null) return;
        var panel = new GameObject(ObjectName).AddComponent<JoinPanel>();
        panel.SetOpen(false);
    }

    public void SetOpen(bool open)
    {
        if (open && canvas == null) Build();
        if (canvas != null) canvas.enabled = open;
        if (open) RefreshStatus();
    }

    /// <summary>Host on the port typed in the field (address part ignored).</summary>
    public bool Host()
    {
        JoinArgs.TrySplit(AddressField != null ? AddressField.text : null, out _, out var port, out _);
        return MultiplayerAutoStarter.Restart(new JoinArgs(JoinArgs.NetMode.Host, JoinArgs.DefaultAddress, port));
    }

    /// <summary>Join the "ip[:port]" typed in the field.</summary>
    public bool Join()
    {
        JoinArgs.TrySplit(AddressField != null ? AddressField.text : null, out var host, out var port, out var warning);
        if (warning != null) Debug.LogWarning($"[Net] {warning}");
        return MultiplayerAutoStarter.Restart(new JoinArgs(JoinArgs.NetMode.Client, host, port));
    }

    void Update()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.f7Key.wasPressedThisFrame) SetOpen(!IsOpen);
        if (IsOpen && Time.unscaledTime >= nextStatus) RefreshStatus();
    }

    void RefreshStatus()
    {
        nextStatus = Time.unscaledTime + 0.5f;
        if (Status != null) Status.text = StatusText(NetworkManager.Singleton, MultiplayerAutoStarter.Current);
    }

    /// <summary>One-line session state for the window.</summary>
    public static string StatusText(NetworkManager nm, JoinArgs current)
    {
        if (nm == null) return "Offline (no NetworkManager)";
        if (nm.ShutdownInProgress) return "Leaving session…";
        if (!nm.IsListening) return "Not connected";
        if (nm.IsHost)
        {
            int others = Mathf.Max(0, nm.ConnectedClientsIds.Count - 1);
            return others == 0
                ? $"Hosting on port {current.Port}, waiting for players"
                : $"Hosting on port {current.Port}, {others} joined";
        }
        if (nm.IsConnectedClient) return $"Joined {current.Address}:{current.Port}";
        return $"Connecting to {current.Address}:{current.Port}…";
    }

    void Build()
    {
        var canvasGo = new GameObject("JoinCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var panel = Panel(canvasGo.transform, "Panel", new Vector2(620f, 360f), Vector2.zero);
        Label(panel.transform, "Co-op", 28, new Vector2(0f, 140f), new Vector2(560f, 44f)).color = DiabloPalette.TrimGold;
        Label(panel.transform, "Host on a port, or join ip:port", 16, new Vector2(0f, 95f), new Vector2(560f, 30f));
        AddressField = Field(panel.transform, $"{JoinArgs.DefaultAddress}:{JoinArgs.DefaultPort}", new Vector2(0f, 45f));
        Button(panel.transform, "Host", new Vector2(-190f, -30f), () => Host());
        Button(panel.transform, "Join", new Vector2(0f, -30f), () => Join());
        Button(panel.transform, "Close", new Vector2(190f, -30f), () => SetOpen(false));
        Status = Label(panel.transform, "", 16, new Vector2(0f, -110f), new Vector2(560f, 40f));
    }

    static GameObject Panel(Transform parent, string name, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = DiabloMenuArt.PanelBackground();
        img.type = Image.Type.Sliced;
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        return go;
    }

    static Text Label(Transform parent, string text, int size, Vector2 pos, Vector2 box)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = DiabloPalette.Parchment;
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta = box;
        r.anchoredPosition = pos;
        return label;
    }

    static InputField Field(Transform parent, string value, Vector2 pos)
    {
        var go = new GameObject("AddressField", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = DiabloMenuArt.TabBackground(false);
        img.type = Image.Type.Sliced;
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta = new Vector2(420f, 46f);
        r.anchoredPosition = pos;
        var text = Label(go.transform, "", 18, Vector2.zero, new Vector2(400f, 40f));
        text.supportRichText = false;
        var field = go.AddComponent<InputField>();
        field.textComponent = text;
        field.characterLimit = 64;
        field.text = value;
        return field;
    }

    static void Button(Transform parent, string text, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button_" + text, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = DiabloMenuArt.TabBackground(false);
        img.type = Image.Type.Sliced;
        var b = go.AddComponent<Button>();
        b.onClick.AddListener(onClick);
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta = new Vector2(170f, 48f);
        r.anchoredPosition = pos;
        Label(go.transform, text, 18, Vector2.zero, new Vector2(170f, 48f));
    }
}
