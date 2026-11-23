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
    /// <summary>Relay join code field (S6).</summary>
    public InputField CodeField { get; private set; }
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

    /// <summary>Host a relay session (join code appears in the status line).</summary>
    public bool HostRelay()
    {
        return MultiplayerAutoStarter.Restart(new JoinArgs(JoinArgs.NetMode.Host,
            JoinArgs.DefaultAddress, JoinArgs.DefaultPort, relayHost: true));
    }

    /// <summary>Find and join the published Cinderreach server (no code, no address).</summary>
    public static bool JoinServer() =>
        MultiplayerAutoStarter.Restart(new JoinArgs(JoinArgs.NetMode.Client,
            JoinArgs.DefaultAddress, JoinArgs.DefaultPort, joinServer: true));

    /// <summary>Join the relay code typed in the code field.</summary>
    public bool JoinByCode()
    {
        string code = CodeField != null ? CodeField.text : null;
        return MultiplayerAutoStarter.Restart(new JoinArgs(JoinArgs.NetMode.Client,
            JoinArgs.DefaultAddress, JoinArgs.DefaultPort, joinCode: code));
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
        if (nm == null) return Loc.T("join.status.offline");
        if (nm.ShutdownInProgress) return Loc.T("join.status.leaving");
        if (current.JoinServer && !nm.IsListening)
            return MultiplayerAutoStarter.ServerJoinError != null
                ? Loc.F("join.status.servererror", MultiplayerAutoStarter.ServerJoinError) : Loc.T("join.status.searching");
        if (current.JoinServer && nm.IsConnectedClient) return Loc.T("join.status.serverconnected");
        if (MultiplayerAutoStarter.ServerJoinError != null && nm.IsHost && nm.ConnectedClientsIds.Count <= 1)
            return Loc.F("join.status.serverdown", MultiplayerAutoStarter.ServerJoinError);
        if (!nm.IsListening)
            return current.JoinCode != null ? Loc.F("join.status.relayconnecting", current.JoinCode)
                : current.RelayHost ? Loc.T("join.status.relaycreating")
                : Loc.T("join.status.notconnected");
        if (nm.IsHost)
        {
            int others = Mathf.Max(0, nm.ConnectedClientsIds.Count - 1);
            if (current.RelayHost && RelayJoin.LastJoinCode != null)
                return others == 0
                    ? Loc.F("join.status.relaywaiting", RelayJoin.LastJoinCode)
                    : Loc.F("join.status.relayjoined", RelayJoin.LastJoinCode, others);
            return others == 0
                ? Loc.F("join.status.hostwaiting", current.Port)
                : Loc.F("join.status.hostjoined", current.Port, others);
        }
        if (nm.IsConnectedClient)
            return current.JoinCode != null ? Loc.F("join.status.joinedrelay", current.JoinCode)
                : Loc.F("join.status.joined", current.Address, current.Port);
        return Loc.F("join.status.connecting", current.Address, current.Port);
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

        var panel = Panel(canvasGo.transform, "Panel", new Vector2(620f, 560f), Vector2.zero);
        Label(panel.transform, Loc.T("menu.coop"), 28, new Vector2(0f, 235f), new Vector2(560f, 44f)).color = DiabloPalette.TrimGold;
        Button(panel.transform, "Join server", Loc.T("join.btn.server"), new Vector2(0f, 185f), () => JoinServer());
        Label(panel.transform, Loc.T("join.hint"), 16, new Vector2(0f, 150f), new Vector2(560f, 30f));
        AddressField = Field(panel.transform, "AddressField", $"{JoinArgs.DefaultAddress}:{JoinArgs.DefaultPort}", new Vector2(0f, 100f));
        Button(panel.transform, "Host", Loc.T("join.btn.host"), new Vector2(-190f, 35f), () => Host());
        Button(panel.transform, "Join", Loc.T("join.btn.join"), new Vector2(0f, 35f), () => Join());
        Button(panel.transform, "Close", Loc.T("join.btn.close"), new Vector2(190f, 35f), () => SetOpen(false));
        Label(panel.transform, Loc.T("join.codelabel"), 16, new Vector2(0f, -25f), new Vector2(560f, 30f));
        CodeField = Field(panel.transform, "CodeField", "", new Vector2(0f, -70f));
        Button(panel.transform, "HostRelay", Loc.T("join.btn.hostrelay"), new Vector2(-95f, -135f), () => HostRelay());
        Button(panel.transform, "JoinByCode", Loc.T("join.btn.joincode"), new Vector2(95f, -135f), () => JoinByCode());
        Status = Label(panel.transform, "", 16, new Vector2(0f, -195f), new Vector2(560f, 40f));
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

    static InputField Field(Transform parent, string name, string value, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
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

    static void Button(Transform parent, string id, string text, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button_" + id, typeof(RectTransform));
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
