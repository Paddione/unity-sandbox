using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class select at game start (second-class S4). Two cards (name, blurb,
/// skills) start a new character; "Continue" appears when a save exists and
/// loads it, class included, without a choice. Skipped for autoplay,
/// shotcheck, co-op peers and when <c>-class &lt;id&gt;</c> is given. While
/// open, movement is locked through <see cref="DiabloClickController.ExternalDrive"/>.
/// </summary>
public sealed class ClassSelectUI : MonoBehaviour
{
    public const string ClassFlag = "-class";

    public bool IsOpen { get; private set; }
    public static bool ClassFromCommandLine { get; private set; }

    Canvas canvas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ReadCommandLine()
    {
        ClassFromCommandLine = false;
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == ClassFlag)
            {
                RunProfile.ClassId = ClassCatalog.Get(args[i + 1].ToLowerInvariant()).id;
                ClassFromCommandLine = true;
                Debug.Log($"[Class] command line: {RunProfile.Class.displayName}");
            }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!ShouldShow(System.Environment.GetCommandLineArgs())) return;
        if (Object.FindFirstObjectByType<ClassAppearance>(FindObjectsInactive.Include) == null) return; // not a zone scene
        // Own object: the solo hero is stood down when the network hero takes over,
        // so the UI must not live on it. The hero is resolved at click time.
        var ui = new GameObject("ClassSelect").AddComponent<ClassSelectUI>();
        ui.SetOpen(true);
    }

    /// <summary>Pure rule: no select screen for automation, co-op peers or an explicit class.</summary>
    public static bool ShouldShow(string[] args)
    {
        foreach (var a in args)
            if (a == SessionAutoplay.Flag || a == "-shotcheck" || a == ClassFlag
                || a == "--net-client" || a == "--net-server" || a == "-batchmode"
                || a == CoopAutoplay.Flag)
                return false;
        return !MultiplayerAutoStarter.IsClone();
    }

    public void SetOpen(bool open)
    {
        if (open && canvas == null) Build();
        IsOpen = open;
        if (canvas != null) canvas.enabled = open;
        DiabloClickController.ExternalDrive = open;
    }

    /// <summary>New character of a class (card button). The first autosave replaces an old save.</summary>
    public void ChooseNew(string classId)
    {
        RunProfile.ClassId = ClassCatalog.Get(classId).id;
        ApplyToHero();
        Debug.Log($"[Class] New {RunProfile.Class.displayName}.");
        SetOpen(false);
    }

    /// <summary>Continue the saved character (class from the save, no choice).</summary>
    public bool Continue(string path = null)
    {
        var target = Hero();
        if (target == null || !SaveService.TryLoad(target, path)) return false;
        ApplyToHero();
        Debug.Log($"[Class] Continue as {RunProfile.Class.displayName}.");
        SetOpen(false);
        return true;
    }

    /// <summary>The active local hero ("Player": the network hero once it took over).</summary>
    static GameObject Hero() => GameObject.Find(DiabloNetworkPlayer.HeroName);

    void ApplyToHero()
    {
        var target = Hero();
        if (target == null) return;
        var look = target.GetComponent<ClassAppearance>();
        if (look != null) look.Apply(RunProfile.Class);
        var net = target.GetComponent<DiabloNetworkPlayer>();
        if (net != null) net.PublishClass();
    }

    void Build()
    {
        var canvasGo = new GameObject("ClassSelectCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var panel = MakePanel(canvasGo.transform, "Panel", new Vector2(980f, 560f), Vector2.zero);
        var title = MakeLabel(panel.transform, "Choose your hero", 28, new Vector2(0f, 230f), new Vector2(900f, 50f));
        title.color = DiabloPalette.TrimGold;

        float x = -(ClassCatalog.All.Length - 1) * 470f / 2f;
        foreach (var cls in ClassCatalog.All)
        {
            var card = MakePanel(panel.transform, "Card_" + cls.id, new Vector2(440f, 360f), new Vector2(x, 0f));
            MakeLabel(card.transform, cls.displayName, 24, new Vector2(0f, 140f), new Vector2(400f, 40f)).color = DiabloPalette.TrimGold;
            MakeLabel(card.transform, cls.blurb, 15, new Vector2(0f, 70f), new Vector2(400f, 90f));
            var skills = new System.Text.StringBuilder();
            for (int i = 0; i < cls.skills.Length; i++)
                skills.Append($"[{cls.keys[i]}] {DiabloSkills.SkillName(cls.skills[i])}\n");
            MakeLabel(card.transform, skills.ToString(), 15, new Vector2(0f, -40f), new Vector2(400f, 110f));
            string id = cls.id;
            MakeButton(card.transform, $"New {cls.displayName}", new Vector2(0f, -140f), () => ChooseNew(id));
            x += 470f;
        }
        if (SaveService.HasSave())
            MakeButton(panel.transform, "Continue saved hero", new Vector2(-170f, -240f), () => Continue());
        // coop-session S1: host or join before choosing (the panel keeps this overlay open).
        MakeButton(panel.transform, "Co-op: host / join", new Vector2(SaveService.HasSave() ? 170f : 0f, -240f),
            () => JoinPanel.Open());
    }

    static GameObject MakePanel(Transform parent, string name, Vector2 size, Vector2 pos)
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

    static Text MakeLabel(Transform parent, string text, int size, Vector2 pos, Vector2 box)
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

    static void MakeButton(Transform parent, string text, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = DiabloMenuArt.TabBackground(false);
        img.type = Image.Type.Sliced;
        var b = go.AddComponent<Button>();
        b.onClick.AddListener(onClick);
        var r = go.GetComponent<RectTransform>();
        r.sizeDelta = new Vector2(300f, 48f);
        r.anchoredPosition = pos;
        MakeLabel(go.transform, text, 18, Vector2.zero, new Vector2(300f, 48f));
    }
}
