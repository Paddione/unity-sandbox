using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Sanctuary Pause / System Menu opened on Escape when no other modal dialog owns the screen.
/// Provides Resume, Inventory, Character Sheet, full embedded Settings, Return to Start Screen,
/// and Quit without losing save state.
/// </summary>
public sealed class PauseMenuUI : MonoBehaviour
{
    public bool IsOpen { get; private set; }
    public bool SettingsOpen { get; private set; }
    static readonly List<PauseMenuUI> live = new List<PauseMenuUI>();

    /// <summary>Any pause menu open (no scene scan; the modal checks run on click frames).</summary>
    public static bool AnyOpen
    {
        get
        {
            for (int i = 0; i < live.Count; i++)
                if (live[i] != null && live[i].IsOpen) return true;
            return false;
        }
    }

    void OnEnable() { if (!live.Contains(this)) live.Add(this); }

    void OnDestroy() => live.Remove(this);

    /// <summary>True while this menu holds solo time at 0 (co-op never pauses the shared world).</summary>
    public bool HoldsTime { get; private set; }

    Canvas canvas;
    GameObject mainCard;
    GameObject settingsCard;
    Text statusLabel;
    Image portraitImage;
    readonly List<UnityEngine.Events.UnityAction> settingsRefreshes = new List<UnityEngine.Events.UnityAction>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    internal static void Boot()
    {
        if (Object.FindFirstObjectByType<ClassAppearance>(FindObjectsInactive.Include) == null) return;
        if (Object.FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include) != null) return;
        new GameObject("PauseMenu").AddComponent<PauseMenuUI>();
    }

    void Update()
    {
        // A hit-stop that started just before opening restores its saved scale
        // after us; re-assert the freeze while the menu is up.
        if (HoldsTime && Time.timeScale != 0f) Time.timeScale = 0f;
        var kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
        if (DiabloUIFactory.WasEscConsumedThisFrame) return;

        var cls = Object.FindFirstObjectByType<ClassSelectUI>(FindObjectsInactive.Include);
        if (cls != null && cls.IsOpen) return;
        if (SessionComplete.Shown > 0 && IsSessionCanvasVisible()) return;

        if (IsOpen)
        {
            if (SettingsOpen) ShowMain();
            else SetOpen(false);
            DiabloUIFactory.MarkEscConsumed();
            return;
        }

        var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        if (DiabloUIFactory.IsAnyModalOpen(hero)) return;

        SetOpen(true);
        DiabloUIFactory.MarkEscConsumed();
    }

    static bool IsSessionCanvasVisible()
    {
        var sc = Object.FindFirstObjectByType<SessionComplete>(FindObjectsInactive.Include);
        if (sc == null) return false;
        var c = sc.GetComponentInChildren<Canvas>(true);
        return c != null && c.enabled;
    }

    public void SetOpen(bool open)
    {
        if (!live.Contains(this)) live.Add(this); // EditMode AddComponent skips OnEnable
        if (open && canvas == null) Build();
        if (IsOpen != open) DiabloAudio.PlayUIClick();
        IsOpen = open;
        if (canvas != null) canvas.enabled = open;
        DiabloClickController.ExternalDrive = open;
        SetTimeHeld(open && Application.isPlaying && Diablo.IsSolo());
        if (open)
        {
            SettingsOpen = false;
            ShowMain();
            RefreshStatus();
        }
    }

    void SetTimeHeld(bool hold)
    {
        if (hold == HoldsTime) return;
        HoldsTime = hold;
        Time.timeScale = hold ? 0f : 1f;
    }

    void OnDisable() => SetTimeHeld(false);

    public void ShowMain()
    {
        SettingsOpen = false;
        if (mainCard != null) mainCard.SetActive(true);
        if (settingsCard != null) settingsCard.SetActive(false);
        RefreshStatus();
    }

    public void ShowSettings()
    {
        if (canvas == null) Build();
        SettingsOpen = true;
        if (mainCard != null) mainCard.SetActive(false);
        if (settingsCard != null) settingsCard.SetActive(true);
        foreach (var r in settingsRefreshes) r?.Invoke();
    }

    public void OpenInventory()
    {
        SetOpen(false);
        var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        hero?.GetComponent<DiabloInventoryUI>()?.SetOpen(true);
    }

    public void OpenCharacter()
    {
        SetOpen(false);
        var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        hero?.GetComponent<DiabloCharacterUI>()?.SetOpen(true);
    }

    public void ReturnToMainMenu()
    {
        var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        if (hero != null) SaveService.SaveNow(hero);
        SetOpen(false);
        var cls = Object.FindFirstObjectByType<ClassSelectUI>(FindObjectsInactive.Include);
        if (cls == null) cls = new GameObject("ClassSelect").AddComponent<ClassSelectUI>();
        cls.SetOpen(true);
    }

    public void QuitGame()
    {
        var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        if (hero != null) SaveService.SaveNow(hero);
        Debug.Log("[Menu] Quit requested.");
#if UNITY_EDITOR
        if (Application.isPlaying && !Application.isBatchMode)
            UnityEditor.EditorApplication.isPlaying = false;
#else
        if (!Application.isBatchMode) Application.Quit();
#endif
    }

    void RefreshStatus()
    {
        if (statusLabel == null) return;
        var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        var prog = hero != null ? hero.GetComponent<DiabloProgression>() : null;
        var wallet = hero != null ? hero.GetComponent<Wallet>() : null;
        int lv = prog != null ? prog.Level : 1;
        int gold = wallet != null ? wallet.Gold : 0;
        string accent = DiabloPalette.ToHex(DiabloPalette.ClassAccent(RunProfile.ClassId));
        string heroName = string.IsNullOrEmpty(RunProfile.Hero.heroName) ? RunProfile.Class.displayName : RunProfile.Hero.heroName;
        statusLabel.text = Loc.F("menu.pause.banner", accent, heroName) +
            Loc.F("menu.pause.status", RunProfile.Class.displayName, lv, gold, Loc.Display(RunProfile.Difficulty));
        if (portraitImage != null)
        {
            var portrait = DiabloMenuArt.ClassPortrait(RunProfile.ClassId);
            portraitImage.sprite = portrait;
            portraitImage.enabled = portrait != null;
        }
    }

    void Build()
    {
        var canvasGo = new GameObject("PauseCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 24);

        var scrim = new GameObject("Scrim", typeof(RectTransform));
        scrim.transform.SetParent(canvasGo.transform, false);
        var sImg = scrim.AddComponent<Image>();
        sImg.color = new Color(0.03f, 0.02f, 0.04f, 0.78f);
        var sr = scrim.GetComponent<RectTransform>();
        sr.anchorMin = Vector2.zero;
        sr.anchorMax = Vector2.one;
        sr.offsetMin = sr.offsetMax = Vector2.zero;

        // Main pause card
        mainCard = DiabloUIFactory.MakePanel(canvasGo.transform, "PausePanel", new Vector2(460f, 600f), Vector2.zero);
        var v = mainCard.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(22, 22, 18, 20);
        v.spacing = 10f;
        v.childControlWidth = true;
        v.childControlHeight = false;

        DiabloUIFactory.MakeHeader(mainCard.transform, Loc.T("menu.pause.title"), DiabloMenuArt.MenuArtSprite("CrestCinderreach"), () => SetOpen(false));
        // Hero banner: class portrait in a rare frame next to class, level, gold and difficulty.
        var banner = DiabloUIFactory.MakeHorizontalRow(mainCard.transform, "HeroBanner", 12f, 68f);
        var bannerRow = banner.GetComponent<HorizontalLayoutGroup>();
        if (bannerRow != null) bannerRow.childAlignment = TextAnchor.MiddleCenter;
        var frame = new GameObject("PortraitFrame", typeof(RectTransform));
        frame.transform.SetParent(banner.transform, false);
        DiabloUIFactory.SetLayout(frame, prefH: 64f, prefW: 64f);
        var frameImg = frame.AddComponent<Image>();
        frameImg.sprite = DiabloMenuArt.SlotBackground(ItemRarity.Rare, 64);
        frameImg.type = Image.Type.Sliced;
        var pGo = new GameObject("Portrait", typeof(RectTransform));
        pGo.transform.SetParent(frame.transform, false);
        portraitImage = pGo.AddComponent<Image>();
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;
        var pr = pGo.GetComponent<RectTransform>();
        pr.anchorMin = Vector2.zero;
        pr.anchorMax = Vector2.one;
        pr.offsetMin = new Vector2(5f, 5f);
        pr.offsetMax = new Vector2(-5f, -5f);
        statusLabel = DiabloUIFactory.MakeText(banner.transform, "HeroStatus", "", 15, DiabloPalette.ParchmentDim, TextAnchor.MiddleLeft);
        DiabloUIFactory.SetLayout(statusLabel.gameObject, prefH: 60f, flexW: 1f);
        DiabloUIFactory.MakeDivider(mainCard.transform);

        AddMenuButton(mainCard.transform, "ResumeButton", Loc.T("menu.pause.resume"), true, () => SetOpen(false));
        AddMenuButton(mainCard.transform, "InventoryButton", Loc.T("menu.pause.inventory"), false, OpenInventory);
        AddMenuButton(mainCard.transform, "CharacterButton", Loc.T("menu.pause.character"), false, OpenCharacter);
        AddMenuButton(mainCard.transform, "SettingsButton", Loc.T("menu.pause.settings"), false, ShowSettings);
        DiabloUIFactory.MakeDivider(mainCard.transform);
        AddMenuButton(mainCard.transform, "SaveMenuButton", Loc.T("menu.pause.mainmenu"), false, ReturnToMainMenu);
        AddMenuButton(mainCard.transform, "QuitButton", Loc.T("menu.pause.quit"), false, QuitGame);
        var keys = DiabloUIFactory.MakeText(mainCard.transform, "PauseHotkeys", Loc.T("menu.pause.hotkeys"), 12, DiabloPalette.ParchmentDim, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(keys.gameObject, prefH: 22f, flexW: 1f);

        // Embedded settings modal card
        settingsCard = DiabloUIFactory.MakePanel(canvasGo.transform, "PauseSettingsPanel", new Vector2(560f, 600f), Vector2.zero);
        var sv = settingsCard.AddComponent<VerticalLayoutGroup>();
        sv.padding = new RectOffset(22, 22, 18, 20);
        sv.spacing = 8f;
        sv.childControlWidth = true;
        sv.childControlHeight = false;
        DiabloUIFactory.MakeHeader(settingsCard.transform, Loc.T("menu.settings.title"), DiabloMenuArt.MenuArtSprite("SettingsCog"), ShowMain);
        DiabloUIFactory.BuildSettingsWidgets(settingsCard.transform, settingsRefreshes, onBack: ShowMain);
        settingsCard.SetActive(false);
        canvas.enabled = false;
    }

    static void AddMenuButton(Transform parent, string name, string label, bool primary, UnityEngine.Events.UnityAction onClick)
    {
        var btn = DiabloUIFactory.MakeButton(parent, name, label, 17, primary, null, onClick);
        DiabloUIFactory.SetLayout(btn.gameObject, prefH: 44f, flexW: 1f);
    }
}
