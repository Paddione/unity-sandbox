using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Full Cinderreach Startup / Expedition Select screen.
/// Shows atmospheric 3D-rendered sanctuary key art, heraldic crest, two detailed class
/// cards (portrait + role lore + skill icon badges), Continue saved expedition info card,
/// Difficulty switcher, embedded Settings modal, and Quit.
/// Preserves existing contracts: ClassFlag, IsOpen, ClassFromCommandLine, ShouldShow,
/// SetOpen, ChooseNew, Continue, and object names ClassSelectCanvas / Card_fenwalker / Card_lanternbearer.
/// </summary>
public sealed class ClassSelectUI : MonoBehaviour
{
    public const string ClassFlag = "-class";

    public bool IsOpen { get; private set; }

    static readonly List<ClassSelectUI> live = new List<ClassSelectUI>();

    /// <summary>Any start/class screen open (no scene scan).</summary>
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

    void OnDestroy()
    {
        live.Remove(this);
        PlayerAccount.NameChanged -= RefreshAccountLabel;
        if (Account != null) PlayerAccount.AccountChanged -= Account.Refresh;
        CloudHeroSync.Synced -= OnCloudSynced;
        CloudHeroSync.Conflict -= OnCloudConflict;
    }
    public static bool ClassFromCommandLine { get; private set; }

    Canvas canvas;
    GameObject mainContent;
    GameObject settingsModal;
    /// <summary>accounts S1: player name window, opened from the title row.</summary>
    public AccountPanel Account { get; private set; }
    Text accountLabel;
    Button continueBtn;
    Text continueLabel;
    Text diffBadgeText;
    Button diffToggleBtn;
    readonly List<UnityEngine.Events.UnityAction> settingsRefreshes = new List<UnityEngine.Events.UnityAction>();

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

    // ---- continue after an in-game reload ("New run") ----
    bool awaitingReloadContinue;
    int reloadStartFrame;
    float reloadDeadline;
    public const float ReloadContinueTimeout = 10f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void HookSceneReload()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (SaveService.ContinueAfterReload || SaveService.NewHeroAfterReload != null) BeginReloadContinue();
    }

    bool StartFreshHero(string classId)
    {
        RunProfile.ClassId = ClassCatalog.Get(classId).id;
        RunProfile.Hero = HeroProfile.Sanitized(SaveService.NewHeroProfile);
        ApplyToHero();
        HeroInWorld = true;
        Debug.Log($"[Class] Fresh {RunProfile.Class.displayName} after the reload.");
        return true;
    }

    /// <summary>
    /// Resume the saved hero once the reloaded scene's hero stands (network
    /// takeover included). Runtime boots do not re-run after LoadScene, so
    /// without this a "New run" would leave a fresh level-1 hero that the next
    /// autosave writes over the real save.
    /// </summary>
    public static ClassSelectUI BeginReloadContinue()
    {
        var ui = new GameObject("ReloadContinue").AddComponent<ClassSelectUI>();
        ui.awaitingReloadContinue = true;
        ui.reloadStartFrame = Time.frameCount;
        ui.reloadDeadline = Time.unscaledTime + ReloadContinueTimeout;
        return ui;
    }

    void TickReloadContinue()
    {
        var target = Hero();
        bool settled = Time.frameCount >= reloadStartFrame + 3; // let the owned hero take over 'Player'
        var net = target != null ? target.GetComponent<DiabloNetworkPlayer>() : null;
        var nm = Unity.Netcode.NetworkManager.Singleton;
        bool netReady = net == null || nm == null || !nm.IsListening || net.IsSpawned;
        if (target != null && settled && netReady)
        {
            awaitingReloadContinue = false;
            string p = SaveService.ContinueAfterReloadPath;
            SaveService.ContinueAfterReload = false;
            SaveService.ContinueAfterReloadPath = null;
            string newClass = SaveService.NewHeroAfterReload;
        SaveService.NewHeroAfterReload = null;
        bool ok = newClass != null ? StartFreshHero(newClass) : Continue(p);
            Debug.Log(!ok ? "[Class] Reload continue found no readable save."
                : newClass != null ? "[Class] Fresh hero stands after the reload."
                : "[Class] Resumed the saved hero after the reload.");
            Destroy(gameObject);
        }
        else if (Time.unscaledTime > reloadDeadline)
        {
            awaitingReloadContinue = false;
            SaveService.ContinueAfterReload = false;
            SaveService.NewHeroAfterReload = null;
            Debug.LogWarning("[Class] Reload continue timed out: no hero.");
            Destroy(gameObject);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!ShouldShow(System.Environment.GetCommandLineArgs())) return;
        if (Object.FindFirstObjectByType<ClassAppearance>(FindObjectsInactive.Include) == null) return;
        var ui = new GameObject("ClassSelect").AddComponent<ClassSelectUI>();
        ui.SetOpen(true);
    }

    /// <summary>Pure rule: no select screen for automation, co-op peers or an explicit class.</summary>
    public static bool ShouldShow(string[] args)
    {
        foreach (var a in args)
            if (a == SessionAutoplay.Flag || a == "-shotcheck" || a == ClassFlag
                || a == "--net-client" || a == "--net-server" || a == DedicatedServer.Flag || a == "-batchmode"
                || a == CoopAutoplay.Flag)
                return false;
        return !MultiplayerAutoStarter.IsClone();
    }

    void Update()
    {
        if (awaitingReloadContinue) { TickReloadContinue(); return; }
        if (!IsOpen) return;
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame && CustomizeVisible)
        {
            CancelCustomize();
            DiabloUIFactory.MarkEscConsumed();
            return;
        }
        if (kb != null && kb.escapeKey.wasPressedThisFrame && ConfirmVisible)
        {
            CancelNew();
            DiabloUIFactory.MarkEscConsumed();
            return;
        }
        if (kb != null && kb.escapeKey.wasPressedThisFrame && Account != null && Account.IsOpen)
        {
            Account.SetOpen(false);
            DiabloUIFactory.MarkEscConsumed();
            return;
        }
        if (kb != null && kb.escapeKey.wasPressedThisFrame && settingsModal != null && settingsModal.activeSelf)
        {
            ShowSettings(false);
            DiabloUIFactory.MarkEscConsumed();
        }
    }

    public const float FadeOutSeconds = 0.45f;
    CanvasGroup fadeGroup;
    Coroutine fadeRoutine;

    public void SetOpen(bool open)
    {
        if (!live.Contains(this)) live.Add(this); // EditMode AddComponent skips OnEnable
        if (open && canvas == null) Build();
        bool wasOpen = IsOpen;
        IsOpen = open;
        if (fadeRoutine != null) { StopCoroutine(fadeRoutine); fadeRoutine = null; }
        if (fadeGroup != null) { fadeGroup.alpha = 1f; fadeGroup.blocksRaycasts = open; }
        if (!open && wasOpen) { try { MusicDirector.Ensure().PlayZone(ActProgress.Town); } catch { } }
        else if (open && !wasOpen && Application.isPlaying) { try { MusicDirector.Ensure().PlayMenu(); } catch { } }
        // Leaving the start screen for the world: a short fade instead of a hard cut
        // (input is released at once; only the picture lingers).
        if (!open && wasOpen && canvas != null && fadeGroup != null && Application.isPlaying && isActiveAndEnabled)
            fadeRoutine = StartCoroutine(FadeOut());
        else if (canvas != null) canvas.enabled = open;
        DiabloClickController.ExternalDrive = open;
        if (!open && previewView != null) previewView.Hide();
        ShowCardFigures(open);
        if (open)
        {
            PendingCustomizeClass = null;
            if (customizeModal != null) customizeModal.SetActive(false);
            CancelNew();
            if (mainContent != null) mainContent.SetActive(true);
            ShowSettings(false);
            RefreshContinueAndDiff();
        }
    }

    System.Collections.IEnumerator FadeOut()
    {
        float t0 = Time.unscaledTime;
        while (Time.unscaledTime - t0 < FadeOutSeconds)
        {
            fadeGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - t0) / FadeOutSeconds);
            yield return null;
        }
        fadeGroup.alpha = 1f;
        canvas.enabled = false;
        fadeRoutine = null;
    }

    /// <summary>New character of a class (direct seam). The first autosave replaces an old save.</summary>
    public void ChooseNew(string classId)
    {
        DiabloAudio.PlayUIClick();
        RunProfile.ClassId = ClassCatalog.Get(classId).id;
        ApplyToHero();
        Debug.Log($"[Class] New {RunProfile.Class.displayName}.");
        HeroInWorld = true;
        SetOpen(false);
    }

    // ---- "Begin as <class>" with a hero at stake ----

    /// <summary>True once a hero entered the world this session (new or continued).</summary>
    public static bool HeroInWorld;
    /// <summary>Class awaiting confirmation (null when no confirm is open). Test seam.</summary>
    public string PendingNewClass { get; private set; }
    public bool ConfirmVisible => confirmModal != null && confirmModal.activeSelf;
    GameObject confirmModal;
    Text confirmBody;
    Text confirmGoLabel;

    // ---- character-creation step (after the class pick) ----
    GameObject customizeModal;
    Text customizeClassLabel;
    HeroPreviewView previewView;
    /// <summary>True while the embedded 3D draft preview stands (test seam).</summary>
    public bool PreviewVisible => previewView != null && previewView.IsLive;
    Text genderValue, hairValue, hairColorValue, buildValue, customizeHint;
    Button customizeStartBtn;
    Text customizeStartLabel;
    InputField heroNameField;
    /// <summary>Class being customized (null when the step is closed). Test seam.</summary>
    public string PendingCustomizeClass { get; private set; }
    /// <summary>Draft hero look while customizing. Test seam.</summary>
    public HeroProfile DraftProfile { get; private set; }
    public bool CustomizeVisible => customizeModal != null && customizeModal.activeSelf;
    readonly System.Random draftRng = new System.Random();

    /// <summary>What a new hero would replace, or null when nothing is at stake.</summary>
    /// <summary>Save path the confirm text peeks (null = default). Test seam.</summary>
    public static string PeekPathOverride;

    public static string ReplacedHeroLine(string path = null)
    {
        path = path ?? PeekPathOverride;
        if (HeroInWorld)
        {
            var hero = Hero();
            var prog = hero != null ? hero.GetComponent<DiabloProgression>() : null;
            return Loc.F("menu.newhero.replaces_current", RunProfile.Class.displayName, (prog != null ? prog.Level : 1));
        }
        if (SaveService.TryPeek(out SaveGame save, path))
            return Loc.F("menu.newhero.replaces_saved", ClassCatalog.Get(save.classId).displayName, save.level, save.gold);
        return null;
    }

    /// <summary>Card button: confirm first when a hero would be replaced, then customize.</summary>
    public void RequestNew(string classId)
    {
        string replaced = ReplacedHeroLine();
        if (replaced == null || confirmModal == null) { OpenCustomize(classId); return; }
        DiabloAudio.PlayUIClick();
        PendingNewClass = ClassCatalog.Get(classId).id;
        var cls = ClassCatalog.Get(PendingNewClass);
        confirmBody.text = Loc.F("menu.newhero.body", cls.displayName, replaced);
        confirmGoLabel.text = Loc.F("menu.newhero.go", cls.displayName);
        if (mainContent != null) mainContent.SetActive(false);
        confirmModal.SetActive(true);
    }

    public void ConfirmNew()
    {
        string id = PendingNewClass;
        CancelNew();
        if (id != null) OpenCustomize(id);
    }

    public void CancelNew()
    {
        PendingNewClass = null;
        if (confirmModal != null) confirmModal.SetActive(false);
        if (mainContent != null && !CustomizeVisible) mainContent.SetActive(true);
    }

    // ---- character-creation step ----

    bool draftNameEdited;

    /// <summary>Open the creation step for a class with a fresh random draft (name included).</summary>
    public void OpenCustomize(string classId)
    {
        if (customizeModal == null) { BeginNew(classId); return; }
        DiabloAudio.PlayUIClick();
        PendingCustomizeClass = ClassCatalog.Get(classId).id;
        DraftProfile = HeroProfile.CreateDefault(draftRng);
        draftNameEdited = false;
        if (mainContent != null) mainContent.SetActive(false);
        if (confirmModal != null) confirmModal.SetActive(false);
        customizeModal.SetActive(true);
        RefreshCustomize();
        PreviewDraft();
    }

    public void CancelCustomize()
    {
        PendingCustomizeClass = null;
        if (previewView != null) previewView.Hide();
        if (customizeModal != null) customizeModal.SetActive(false);
        if (mainContent != null) mainContent.SetActive(true);
        ApplyToHero();
    }

    void PreviewDraft()
    {
        var target = Hero();
        if (target == null || PendingCustomizeClass == null) return;
        var cls = ClassCatalog.Get(PendingCustomizeClass);
        var look = target.GetComponent<ClassAppearance>();
        if (look != null) look.Apply(cls, DraftProfile);
        // The embedded 3D preview mirrors the same draft on its own rig.
        if (previewView != null) previewView.Show(cls, DraftProfile);
    }

    void RefreshCustomize()
    {
        var p = DraftProfile;
        if (customizeClassLabel != null)
            customizeClassLabel.text = Loc.F("menu.create.class", ClassCatalog.Get(PendingCustomizeClass ?? ClassCatalog.FenwalkerId).displayName);
        if (genderValue != null)
            genderValue.text = Loc.T(p.gender == (int)HeroGender.Female ? "menu.create.gender.female"
                : p.gender == (int)HeroGender.Male ? "menu.create.gender.male" : "menu.create.gender.nonbinary");
        if (hairValue != null) hairValue.text = Loc.T($"menu.create.hair.{Mathf.Clamp(p.hairStyle, 0, 5)}");
        if (hairColorValue != null) hairColorValue.text = Loc.T($"menu.create.haircolor.{Mathf.Clamp(p.hairColor, 0, 9)}");
        if (buildValue != null) buildValue.text = Loc.T($"menu.create.build.{Mathf.Clamp(p.build, 0, 2)}");
        if (heroNameField != null && heroNameField.text != p.heroName) heroNameField.SetTextWithoutNotify(p.heroName);
        if (customizeStartLabel != null)
            customizeStartLabel.text = Loc.F("menu.create.start", ClassCatalog.Get(PendingCustomizeClass ?? ClassCatalog.FenwalkerId).displayName);
    }

    public void CycleGender()
    {
        var p = DraftProfile;
        p.gender = (p.gender + 1) % 3;
        if (!draftNameEdited) p.heroName = HeroNames.RandomName((HeroGender)p.gender, draftRng);
        DraftProfile = p; RefreshCustomize(); PreviewDraft();
    }

    public void CycleHair()
    {
        var p = DraftProfile; p.hairStyle = (p.hairStyle + 1) % HeroAppearance.HairStyleCount;
        DraftProfile = p; RefreshCustomize(); PreviewDraft();
    }

    public void CycleHairColor()
    {
        var p = DraftProfile; p.hairColor = (p.hairColor + 1) % HeroAppearance.HairColorCount;
        DraftProfile = p; RefreshCustomize(); PreviewDraft();
    }

    public void CycleBuild()
    {
        var p = DraftProfile; p.build = (p.build + 1) % HeroAppearance.BuildCount;
        DraftProfile = p; RefreshCustomize(); PreviewDraft();
    }

    /// <summary>Dice button: a fresh random name for the draft gender.</summary>
    public void RerollDraftName()
    {
        var p = DraftProfile;
        p.heroName = HeroNames.RandomName((HeroGender)Mathf.Clamp(p.gender, 0, 2), draftRng);
        DraftProfile = p; draftNameEdited = false; RefreshCustomize();
    }

    /// <summary>Name field edit (test seam mirrors the InputField).</summary>
    public void SetDraftName(string raw)
    {
        var p = DraftProfile; p.heroName = raw ?? ""; DraftProfile = p; draftNameEdited = true;
        if (heroNameField != null && heroNameField.text != p.heroName) heroNameField.SetTextWithoutNotify(p.heroName);
    }

    /// <summary>Start button: a clean name is required; the draft becomes the hero and the run begins.</summary>
    public void StartCustomizedHero()
    {
        if (PendingCustomizeClass == null) return;
        string raw = heroNameField != null ? heroNameField.text : DraftProfile.heroName;
        string clean = HeroNames.Clean(raw);
        if (clean == null)
        {
            if (customizeHint != null) customizeHint.text = Loc.T("menu.create.name.rule");
            return;
        }
        var p = DraftProfile; p.heroName = clean; DraftProfile = p;
        RunProfile.Hero = HeroProfile.Sanitized(p);
        string classId = PendingCustomizeClass;
        PendingCustomizeClass = null;
        if (previewView != null) previewView.Hide();
        if (customizeModal != null) customizeModal.SetActive(false);
        BeginNew(classId);
    }

    /// <summary>
    /// Fresh hero. Mid-session (solo) the world reloads with a reset run so
    /// level, gear, act progress and dead enemies do not carry over; co-op
    /// keeps the old in-place class switch (a reload would drop the peers).
    /// </summary>
    void BeginNew(string classId)
    {
        if (HeroInWorld && Application.isPlaying && Diablo.IsSolo())
        {
            DiabloAudio.PlayUIClick();
            SaveService.NewHeroAfterReload = ClassCatalog.Get(classId).id;
            SaveService.NewHeroProfile = HeroProfile.Sanitized(RunProfile.Hero);
            SaveService.ContinueAfterReload = false;
            RunProfile.Reset();
            SessionComplete.ResetRun();
            Debug.Log($"[Class] New {ClassCatalog.Get(classId).displayName}: reloading a fresh world.");
            SceneReload.ReloadActiveScene();
            return;
        }
        ChooseNew(classId);
    }

    /// <summary>Continue the saved character (class from the save, no choice).</summary>
    public bool Continue(string path = null)
    {
        var target = Hero();
        if (target == null || !SaveService.TryLoad(target, path)) return false;
        DiabloAudio.PlayUIClick();
        ApplyToHero();
        Debug.Log($"[Class] Continue as {RunProfile.Class.displayName}.");
        HeroInWorld = true;
        SetOpen(false);
        return true;
    }

    public void ShowSettings(bool visible)
    {
        if (canvas == null) Build();
        if (mainContent != null) mainContent.SetActive(!visible);
        if (settingsModal != null) settingsModal.SetActive(visible);
        if (visible) foreach (var r in settingsRefreshes) r?.Invoke();
    }

    public void QuitGame()
    {
        Debug.Log("[Class] Quit requested.");
#if UNITY_EDITOR
        if (Application.isPlaying && !Application.isBatchMode)
            UnityEditor.EditorApplication.isPlaying = false;
#else
        if (!Application.isBatchMode) Application.Quit();
#endif
    }

    static GameObject Hero() => GameObject.Find(DiabloNetworkPlayer.HeroName);

    void ApplyToHero()
    {
        var target = Hero();
        if (target == null) return;
        RunProfile.Hero = HeroProfile.Sanitized(RunProfile.Hero);
        var look = target.GetComponent<ClassAppearance>();
        if (look != null) look.Apply(RunProfile.Class, RunProfile.Hero);
        var net = target.GetComponent<DiabloNetworkPlayer>();
        if (net != null) net.PublishHeroName();
    }

    /// <summary>Continue button text with the saved hero when the save is readable.</summary>
    public static string ContinueLabel(string path = null)
    {
        if (!SaveService.TryPeek(out SaveGame save, path))
            return Loc.T("menu.continue.plain");
        var cls = ClassCatalog.Get(save.classId);
        return Loc.F("menu.continue.hero", cls.displayName.ToUpperInvariant(), save.level, save.gold, Loc.Display(save.difficulty ?? RunProfile.Normal).ToUpperInvariant());
    }

    void RefreshContinueAndDiff()
    {
        bool hasSave = SaveService.HasSave();
        if (continueBtn != null)
        {
            continueBtn.gameObject.SetActive(hasSave);
            if (hasSave && continueLabel != null)
                continueLabel.text = ContinueLabel();
        }
        if (diffBadgeText != null) diffBadgeText.text = DiabloInventoryUI.DifficultyText();
        if (diffToggleBtn != null)
        {
            // Locked: the badge already explains how to unlock Kindled, a dead button only repeats it.
            diffToggleBtn.gameObject.SetActive(RunProfile.DifficultyUnlocked);
            diffToggleBtn.interactable = RunProfile.DifficultyUnlocked;
            DiabloUIFactory.StyleButtonActive(diffToggleBtn, RunProfile.Difficulty == RunProfile.Kindled,
                RunProfile.DifficultyUnlocked ? Loc.F("menu.mode.current", Loc.Display(RunProfile.Difficulty)) : Loc.T("menu.mode.locked"));
        }
    }

    void Build()
    {
        var canvasGo = new GameObject("ClassSelectCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 25);
        fadeGroup = canvasGo.AddComponent<CanvasGroup>();

        // Full-viewport obsidian backdrop covering gameplay HUD behind start screen cleanly
        var backdropBase = new GameObject("BackdropFill", typeof(RectTransform));
        backdropBase.transform.SetParent(canvasGo.transform, false);
        var baseImg = backdropBase.AddComponent<Image>();
        baseImg.color = new Color(0.03f, 0.03f, 0.05f, 1f);
        Stretch(backdropBase.GetComponent<RectTransform>());

        var keyArt = DiabloMenuArt.MenuArtSprite("StartupBackdrop");
        if (keyArt != null)
        {
            var artGo = new GameObject("StartupKeyArt", typeof(RectTransform));
            artGo.transform.SetParent(canvasGo.transform, false);
            var artImg = artGo.AddComponent<Image>();
            artImg.sprite = keyArt;
            artImg.preserveAspect = true;
            artImg.color = new Color(0.88f, 0.84f, 0.82f, 0.92f);
            artImg.raycastTarget = false;
            Stretch(artGo.GetComponent<RectTransform>());
        }

        // Subtle atmospheric left-to-right veil so hero cards on left stay crisp while hearth glows right
        var veilGo = new GameObject("BackdropVignette", typeof(RectTransform));
        veilGo.transform.SetParent(canvasGo.transform, false);
        var vImg = veilGo.AddComponent<Image>();
        vImg.color = new Color(0.035f, 0.03f, 0.055f, 0.40f);
        vImg.raycastTarget = false;
        Stretch(veilGo.GetComponent<RectTransform>());

        // Main left-anchored expedition container (leaves right hearth heroes framed cleanly!)
        mainContent = new GameObject("MainContainer", typeof(RectTransform));
        mainContent.transform.SetParent(canvasGo.transform, false);
        var mrt = mainContent.GetComponent<RectTransform>();
        mrt.anchorMin = new Vector2(0.03f, 0.05f);
        mrt.anchorMax = new Vector2(0.64f, 0.95f);
        mrt.offsetMin = mrt.offsetMax = Vector2.zero;

        var mv = mainContent.AddComponent<VerticalLayoutGroup>();
        mv.spacing = 12f;
        mv.childControlWidth = true;
        mv.childControlHeight = false;

        // Title Crest Row
        var brandRow = DiabloUIFactory.MakeHorizontalRow(mainContent.transform, "BrandRow", 16f, 84f);
        brandRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
        var crestSprite = DiabloMenuArt.MenuArtSprite("CrestCinderreach");
        if (crestSprite != null)
        {
            var crestGo = new GameObject("Crest", typeof(RectTransform));
            crestGo.transform.SetParent(brandRow.transform, false);
            DiabloUIFactory.SetLayout(crestGo, prefH: 80f, prefW: 80f);
            var cImg = crestGo.AddComponent<Image>();
            cImg.sprite = crestSprite;
            cImg.preserveAspect = true;
            cImg.raycastTarget = false;
        }
        var titleCol = new GameObject("TitleCol", typeof(RectTransform));
        titleCol.transform.SetParent(brandRow.transform, false);
        DiabloUIFactory.SetLayout(titleCol, prefH: 82f, flexW: 1f);
        var tv = titleCol.AddComponent<VerticalLayoutGroup>();
        tv.spacing = 2f;
        tv.childControlWidth = true;
        tv.childControlHeight = false;
        var mainTitle = DiabloUIFactory.MakeText(titleCol.transform, "GameTitle", "CINDERREACH", 40, DiabloPalette.GoldBright, TextAnchor.MiddleLeft);
        mainTitle.fontStyle = FontStyle.Bold;
        DiabloUIFactory.SetLayout(mainTitle.gameObject, prefH: 46f, flexW: 1f);
        var subTitle = DiabloUIFactory.MakeText(titleCol.transform, "Subtitle", Loc.T("menu.start.subtitle"), 16, DiabloPalette.ParchmentDim, TextAnchor.MiddleLeft);
        DiabloUIFactory.SetLayout(subTitle.gameObject, prefH: 24f, flexW: 1f);

        // accounts S1: who other players will see; click to rename.
        var accountBtn = DiabloUIFactory.MakeButton(brandRow.transform, "AccountButton", "", 15, false, null, () =>
        {
            DiabloAudio.PlayUIClick();
            Account.SetOpen(true);
        });
        DiabloUIFactory.SetLayout(accountBtn.gameObject, prefH: 44f, prefW: 280f);
        accountLabel = accountBtn.GetComponentInChildren<Text>();
        PlayerAccount.NameChanged += RefreshAccountLabel;
        RefreshAccountLabel();
        CloudHeroSync.Synced += OnCloudSynced;
        CloudHeroSync.Conflict += OnCloudConflict;
        if (!Application.isBatchMode)
            _ = CloudHeroSync.SignInAndSyncAsync(); // online name + cloud hero; offline keeps the local ones

        DiabloUIFactory.MakeDivider(mainContent.transform, 6f);

        // Class Selection Card Deck
        var cardsRow = new GameObject("CardsRow", typeof(RectTransform));
        cardsRow.transform.SetParent(mainContent.transform, false);
        DiabloUIFactory.SetLayout(cardsRow, prefH: CardsRowHeight, flexW: 1f);
        var ch = cardsRow.AddComponent<HorizontalLayoutGroup>();
        ch.spacing = 18f;
        ch.childControlWidth = true;
        ch.childControlHeight = true;
        ch.childForceExpandWidth = true;
        ch.childForceExpandHeight = true;

        foreach (var cls in ClassCatalog.All)
            BuildClassCard(cardsRow.transform, cls);

        // Continue Expedition Button (shown when save file exists)
        continueBtn = DiabloUIFactory.MakeButton(mainContent.transform, "ContinueButton", Loc.T("menu.continue.button"), 18, true, DiabloPalette.GoldBright, () => Continue());
        DiabloUIFactory.SetLayout(continueBtn.gameObject, prefH: 48f, flexW: 1f);
        continueLabel = continueBtn.GetComponentInChildren<Text>();

        // Footer Controls: Difficulty + Settings + Quit
        var footerRow = DiabloUIFactory.MakeHorizontalRow(mainContent.transform, "FooterBar", 10f, 42f);
        diffBadgeText = DiabloUIFactory.MakeText(footerRow.transform, "DiffLabel", DiabloInventoryUI.DifficultyText(), 14, DiabloPalette.ParchmentDim);
        DiabloUIFactory.SetLayout(diffBadgeText.gameObject, prefH: 38f, prefW: 260f, flexW: 1f);

        diffToggleBtn = DiabloUIFactory.MakeSmallButton(footerRow.transform, Loc.T("menu.mode.normal"), () =>
        {
            if (!RunProfile.DifficultyUnlocked) return;
            DiabloAudio.PlayUIClick();
            RunProfile.Difficulty = RunProfile.Difficulty == RunProfile.Kindled ? RunProfile.Normal : RunProfile.Kindled;
            RefreshContinueAndDiff();
        }, 38f, 220f);

        // coop-session S1: host or join before choosing (the panel keeps this overlay open).
        var coopBtn = DiabloUIFactory.MakeButton(footerRow.transform, "CoopButton", Loc.T("menu.coop"), 15, false, null, () =>
        {
            DiabloAudio.PlayUIClick();
            JoinPanel.Open();
        });
        DiabloUIFactory.SetLayout(coopBtn.gameObject, prefH: 38f, prefW: 130f);

        // cinderreach-dedicated-server S2: one click into the always-on server (no code, no address).
        var serverBtn = DiabloUIFactory.MakeButton(footerRow.transform, "JoinServerButton", Loc.T("menu.joinserver"), 15, false, null, () =>
        {
            DiabloAudio.PlayUIClick();
            JoinPanel.JoinServer();
            JoinPanel.Open();
        });
        DiabloUIFactory.SetLayout(serverBtn.gameObject, prefH: 38f, prefW: 150f);

        var settingsBtn = DiabloUIFactory.MakeButton(footerRow.transform, "SettingsButton", Loc.T("menu.settings"), 15, false, null, () =>
        {
            DiabloAudio.PlayUIClick();
            ShowSettings(true);
        });
        DiabloUIFactory.SetLayout(settingsBtn.gameObject, prefH: 38f, prefW: 150f);

        var quitBtn = DiabloUIFactory.MakeButton(footerRow.transform, "QuitButton", Loc.T("menu.quit"), 15, false, DiabloPalette.ParchmentDim, QuitGame);
        DiabloUIFactory.SetLayout(quitBtn.gameObject, prefH: 38f, prefW: 120f);

        // Startup Settings Overlay Modal
        settingsModal = DiabloUIFactory.MakePanel(canvasGo.transform, "StartupSettingsPanel", new Vector2(560f, 590f), Vector2.zero);
        var sv = settingsModal.AddComponent<VerticalLayoutGroup>();
        sv.padding = new RectOffset(22, 22, 18, 20);
        sv.spacing = 8f;
        sv.childControlWidth = true;
        sv.childControlHeight = false;
        DiabloUIFactory.MakeHeader(settingsModal.transform, Loc.T("menu.settings.title"), DiabloMenuArt.MenuArtSprite("SettingsCog"), () => ShowSettings(false));
        DiabloUIFactory.BuildSettingsWidgets(settingsModal.transform, settingsRefreshes, onBack: () => ShowSettings(false));
        settingsModal.SetActive(false);

        // Confirm before a new hero replaces a saved or current one.
        confirmModal = DiabloUIFactory.MakePanel(canvasGo.transform, "NewHeroConfirmPanel", new Vector2(600f, 290f), Vector2.zero);
        var cv = confirmModal.AddComponent<VerticalLayoutGroup>();
        cv.padding = new RectOffset(24, 24, 18, 20);
        cv.spacing = 12f;
        cv.childControlWidth = true;
        cv.childControlHeight = false;
        DiabloUIFactory.MakeHeader(confirmModal.transform, Loc.T("menu.newhero.title"), DiabloMenuArt.MenuArtSprite("CrestCinderreach"), CancelNew);
        confirmBody = DiabloUIFactory.MakeText(confirmModal.transform, "ConfirmBody", "", 17, DiabloPalette.Parchment, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(confirmBody.gameObject, prefH: 112f, flexW: 1f);
        var crow = DiabloUIFactory.MakeHorizontalRow(confirmModal.transform, "ConfirmButtons", 14f, 46f);
        var goBtn = DiabloUIFactory.MakeButton(crow.transform, "ConfirmNewButton", Loc.T("menu.newhero.begin"), 17, true, DiabloPalette.GoldBright, ConfirmNew);
        DiabloUIFactory.SetLayout(goBtn.gameObject, prefH: 44f, flexW: 1f);
        confirmGoLabel = goBtn.GetComponentInChildren<Text>();
        var backBtn = DiabloUIFactory.MakeButton(crow.transform, "CancelNewButton", Loc.T("menu.back"), 17, false, null, CancelNew);
        DiabloUIFactory.SetLayout(backBtn.gameObject, prefH: 44f, flexW: 1f);
        confirmModal.SetActive(false);
        Account = AccountPanel.Build(canvasGo.transform);
        BuildHeroChoice(canvasGo.transform);
        BuildCustomize(canvasGo.transform);
        RefreshContinueAndDiff();
        canvas.enabled = false;
    }

    // ------------------------------------------------ accounts S3: cloud hero

    GameObject heroChoiceModal;
    Text heroChoiceBody;

    void OnCloudSynced()
    {
        if (this == null) return;
        RefreshContinueAndDiff();
        if (Account != null && Account.IsOpen && !string.IsNullOrEmpty(CloudHeroSync.Status))
            Account.Status.text = CloudHeroSync.Status;
    }

    void OnCloudConflict(SaveGame cloud, SaveGame local)
    {
        if (this == null || heroChoiceModal == null) return;
        heroChoiceBody.text = HeroChoiceText(cloud, local);
        heroChoiceModal.SetActive(true);
    }

    /// <summary>Conflict prompt body (pure): both heroes with class and level.</summary>
    public static string HeroChoiceText(SaveGame cloud, SaveGame local)
    {
        string Describe(SaveGame s) => s == null ? Loc.T("menu.herochoice.unreadable")
            : Loc.F("menu.herochoice.describe", ClassCatalog.Get(s.classId).displayName, s.level, s.gold);
        return Loc.F("menu.herochoice.body", Describe(cloud), Describe(local)) +
               Loc.T("menu.herochoice.ask");
    }

    /// <summary>Creation step size: the full-body preview needs the height.</summary>
    public static readonly Vector2 CustomizePanelSize = new Vector2(1100f, 820f);
    public const float CustomizePreviewHeight = 600f;
    public const float CustomizePreviewWidth = 440f;

    void BuildCustomize(Transform canvas)
    {
        customizeModal = DiabloUIFactory.MakePanel(canvas, "HeroCustomizePanel", CustomizePanelSize, Vector2.zero);
        var v = customizeModal.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(24, 24, 18, 20);
        v.spacing = 10f;
        v.childControlWidth = true;
        v.childControlHeight = false;
        DiabloUIFactory.MakeHeader(customizeModal.transform, Loc.T("menu.create.title"), DiabloMenuArt.MenuArtSprite("CrestCinderreach"), CancelCustomize);
        customizeClassLabel = DiabloUIFactory.MakeText(customizeModal.transform, "CustomizeClass", "", 16, DiabloPalette.ParchmentDim, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(customizeClassLabel.gameObject, prefH: 24f, flexW: 1f);

        // Body: full-body preview on the left, the choices on the right.
        var body = new GameObject("CustomizeBody", typeof(RectTransform));
        body.transform.SetParent(customizeModal.transform, false);
        DiabloUIFactory.SetLayout(body, prefH: CustomizePreviewHeight, flexW: 1f);
        var bh = body.AddComponent<HorizontalLayoutGroup>();
        bh.spacing = 24f;
        bh.childControlWidth = true;
        bh.childControlHeight = true;
        bh.childForceExpandWidth = false;
        bh.childForceExpandHeight = true;
        previewView = HeroPreviewView.BuildInto(body.transform, CustomizePreviewHeight, CustomizePreviewWidth);

        var col = new GameObject("CustomizeChoices", typeof(RectTransform));
        col.transform.SetParent(body.transform, false);
        DiabloUIFactory.SetLayout(col, flexW: 1f);
        var cv = col.AddComponent<VerticalLayoutGroup>();
        cv.spacing = 14f;
        cv.padding = new RectOffset(0, 0, 60, 0);
        cv.childControlWidth = true;
        cv.childControlHeight = false;

        genderValue = CycleRow(col.transform, "GenderRow", "menu.create.gender", CycleGender);
        hairValue = CycleRow(col.transform, "HairRow", "menu.create.hair", CycleHair);
        hairColorValue = CycleRow(col.transform, "HairColorRow", "menu.create.haircolor", CycleHairColor);
        buildValue = CycleRow(col.transform, "BuildRow", "menu.create.build", CycleBuild);

        var nameLabel = DiabloUIFactory.MakeText(col.transform, "HeroNameLabel", Loc.T("menu.create.name"), 15, DiabloPalette.ParchmentDim);
        DiabloUIFactory.SetLayout(nameLabel.gameObject, prefH: 22f, flexW: 1f);
        var nameRow = DiabloUIFactory.MakeHorizontalRow(col.transform, "HeroNameRow", 10f, 46f);
        heroNameField = AccountPanel.Field(nameRow.transform, "HeroNameField", "", 16);
        heroNameField.onValueChanged.AddListener(raw => SetDraftName(raw));
        DiabloUIFactory.SetLayout(heroNameField.gameObject, prefH: 44f, flexW: 1f);
        var diceBtn = DiabloUIFactory.MakeButton(nameRow.transform, "RerollNameButton", Loc.T("menu.create.random"), 15, false, null, RerollDraftName);
        DiabloUIFactory.SetLayout(diceBtn.gameObject, prefH: 44f, prefW: 120f);

        customizeHint = DiabloUIFactory.MakeText(col.transform, "CustomizeHint", "", 14, DiabloPalette.ParchmentDim, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(customizeHint.gameObject, prefH: 22f, flexW: 1f);

        var row = DiabloUIFactory.MakeHorizontalRow(customizeModal.transform, "CustomizeButtons", 14f, 46f);
        customizeStartBtn = DiabloUIFactory.MakeButton(row.transform, "StartCustomizedButton", "", 17, true, DiabloPalette.GoldBright, StartCustomizedHero);
        DiabloUIFactory.SetLayout(customizeStartBtn.gameObject, prefH: 44f, flexW: 1f);
        customizeStartLabel = customizeStartBtn.GetComponentInChildren<Text>();
        var backBtn = DiabloUIFactory.MakeButton(row.transform, "CancelCustomizeButton", Loc.T("menu.back"), 17, false, null, CancelCustomize);
        DiabloUIFactory.SetLayout(backBtn.gameObject, prefH: 44f, flexW: 1f);
        customizeModal.SetActive(false);
    }

    Text CycleRow(Transform parent, string rowName, string labelKey, UnityEngine.Events.UnityAction onCycle)
    {
        var row = DiabloUIFactory.MakeHorizontalRow(parent, rowName, 10f, 42f);
        var label = DiabloUIFactory.MakeText(row.transform, "Label", Loc.T(labelKey), 15, DiabloPalette.ParchmentDim);
        DiabloUIFactory.SetLayout(label.gameObject, prefH: 38f, prefW: 150f);
        var value = DiabloUIFactory.MakeText(row.transform, "Value", "", 16, DiabloPalette.Parchment, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(value.gameObject, prefH: 38f, flexW: 1f);
        var btn = DiabloUIFactory.MakeSmallButton(row.transform, "›", onCycle, 38f, 52f);
        DiabloUIFactory.SetLayout(btn.gameObject, prefH: 38f, prefW: 52f);
        return value;
    }

    void BuildHeroChoice(Transform canvas)
    {
        heroChoiceModal = DiabloUIFactory.MakePanel(canvas, "HeroChoicePanel", new Vector2(640f, 320f), Vector2.zero);
        var v = heroChoiceModal.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(24, 24, 18, 20);
        v.spacing = 12f;
        v.childControlWidth = true;
        v.childControlHeight = false;
        DiabloUIFactory.MakeHeader(heroChoiceModal.transform, Loc.T("menu.herochoice.title"), DiabloMenuArt.MenuArtSprite("CrestCinderreach"));
        heroChoiceBody = DiabloUIFactory.MakeText(heroChoiceModal.transform, "HeroChoiceBody", "", 17, DiabloPalette.Parchment, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(heroChoiceBody.gameObject, prefH: 120f, flexW: 1f);
        var row = DiabloUIFactory.MakeHorizontalRow(heroChoiceModal.transform, "HeroChoiceButtons", 14f, 46f);
        var cloudBtn = DiabloUIFactory.MakeButton(row.transform, "KeepCloudHeroButton", Loc.T("menu.herochoice.cloud"), 17, true, DiabloPalette.GoldBright, () => ChooseHero(true));
        DiabloUIFactory.SetLayout(cloudBtn.gameObject, prefH: 44f, flexW: 1f);
        var localBtn = DiabloUIFactory.MakeButton(row.transform, "KeepLocalHeroButton", Loc.T("menu.herochoice.local"), 17, false, null, () => ChooseHero(false));
        DiabloUIFactory.SetLayout(localBtn.gameObject, prefH: 44f, flexW: 1f);
        heroChoiceModal.SetActive(false);
    }

    async void ChooseHero(bool keepCloud)
    {
        DiabloAudio.PlayUIClick();
        if (heroChoiceModal != null) heroChoiceModal.SetActive(false);
        await CloudHeroSync.ResolveConflictAsync(keepCloud);
    }

    /// <summary>Title-row label: the name this player shows online.</summary>
    public static string AccountLabel(string name) => Loc.F("menu.account.playingas", name);

    void RefreshAccountLabel()
    {
        if (accountLabel != null) accountLabel.text = AccountLabel(PlayerAccount.DisplayName);
    }

    void BuildClassCard(Transform parent, ClassDefinition cls)
    {
        var card = new GameObject($"Card_{cls.id}", typeof(RectTransform));
        card.transform.SetParent(parent, false);
        var img = card.AddComponent<Image>();
        img.sprite = DiabloMenuArt.PanelBackground();
        img.type = Image.Type.Sliced;

        var btn = card.AddComponent<Button>();
        var cols = btn.colors;
        cols.normalColor = Color.white;
        cols.highlightedColor = new Color(1.16f, 1.10f, 1.04f, 1f);
        cols.pressedColor = new Color(0.86f, 0.82f, 0.78f, 1f);
        btn.colors = cols;
        string id = cls.id;
        btn.onClick.AddListener(() => RequestNew(id));

        var v = card.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(18, 18, 14, 14);
        v.spacing = 6f;
        v.childControlWidth = true;
        v.childControlHeight = false;

        // Top section: Portrait box + Class Title & Identity
        var topRow = DiabloUIFactory.MakeHorizontalRow(card.transform, "ClassTopRow", 14f, CardFigureHeight + 4f);
        topRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

        var frame = new GameObject("PortraitFrame", typeof(RectTransform));
        frame.transform.SetParent(topRow.transform, false);
        DiabloUIFactory.SetLayout(frame, prefH: CardFigureHeight, prefW: CardFigureWidth);
        var fImg = frame.AddComponent<Image>();
        fImg.sprite = DiabloMenuArt.SlotBackground(cls.role == ClassDefinition.PartyRole.Healer ? ItemRarity.Unique
            : cls.role == ClassDefinition.PartyRole.Dps ? ItemRarity.Magic : ItemRarity.Rare);
        fImg.type = Image.Type.Sliced;
        fImg.raycastTarget = false;

        var portSprite = DiabloMenuArt.ClassPortrait(cls.id);
        Image portraitImg = null;
        if (portSprite != null)
        {
            var pGo = new GameObject("Portrait", typeof(RectTransform));
            pGo.transform.SetParent(frame.transform, false);
            var pImg = pGo.AddComponent<Image>();
            pImg.sprite = portSprite;
            pImg.preserveAspect = true;
            pImg.raycastTarget = false;
            portraitImg = pImg;
            var pr = pGo.GetComponent<RectTransform>();
            pr.anchorMin = Vector2.zero;
            pr.anchorMax = Vector2.one;
            pr.offsetMin = new Vector2(8f, 8f);
            pr.offsetMax = new Vector2(-8f, -8f);
        }
        // Full-body 3D figure over the bust art (the art stays as fallback without a hero rig).
        var figureGo = new GameObject("Figure", typeof(RectTransform));
        figureGo.transform.SetParent(frame.transform, false);
        var fr = figureGo.GetComponent<RectTransform>();
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = new Vector2(6f, 6f);
        fr.offsetMax = new Vector2(-6f, -6f);
        cardFigures.Add(new KeyValuePair<string, HeroPreviewView>(cls.id, HeroPreviewView.AttachTo(fr, portraitImg, turntable: false)));

        var headCol = new GameObject("ClassTitleCol", typeof(RectTransform));
        headCol.transform.SetParent(topRow.transform, false);
        DiabloUIFactory.SetLayout(headCol, prefH: CardFigureHeight, flexW: 1f);
        var hv = headCol.AddComponent<VerticalLayoutGroup>();
        hv.spacing = 4f;
        hv.childAlignment = TextAnchor.MiddleLeft;
        hv.childControlWidth = true;
        hv.childControlHeight = false;

        var nameLbl = DiabloUIFactory.MakeText(headCol.transform, "ClassName", cls.displayName, 26, DiabloPalette.ClassAccent(cls.id));
        nameLbl.fontStyle = FontStyle.Bold;
        DiabloUIFactory.SetLayout(nameLbl.gameObject, prefH: 32f, flexW: 1f);

        string subtitle = CardSubtitle(cls);
        var subLbl = DiabloUIFactory.MakeText(headCol.transform, "ClassRole", subtitle, 14, DiabloPalette.TrimGold);
        DiabloUIFactory.SetLayout(subLbl.gameObject, prefH: 20f, flexW: 1f);

        string statTag = CardStatTag(cls);
        var statLbl = DiabloUIFactory.MakeText(headCol.transform, "ClassTag", statTag, 13, DiabloPalette.ParchmentDim);
        DiabloUIFactory.SetLayout(statLbl.gameObject, prefH: 20f, flexW: 1f);

        // Lore blurb
        var blurbLbl = DiabloUIFactory.MakeText(card.transform, "ClassBlurb", cls.blurb, 15, DiabloPalette.Parchment);
        DiabloUIFactory.SetLayout(blurbLbl.gameObject, prefH: 52f, flexW: 1f);

        DiabloUIFactory.MakeDivider(card.transform, 5f);

        // Skill Icon Rows
        var skillsHdr = DiabloUIFactory.MakeText(card.transform, "SkillsHeader", Loc.T("menu.class.skills"), 13, DiabloPalette.ParchmentDim);
        DiabloUIFactory.SetLayout(skillsLblHeight(skillsHdr.gameObject), prefH: 18f, flexW: 1f);

        for (int i = 0; i < cls.skills.Length; i++)
        {
            var sk = cls.skills[i];
            var key = cls.KeyLabel(i);
            var row = DiabloUIFactory.MakeHorizontalRow(card.transform, $"SkillRow_{sk}", 10f, 30f);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

            var slot = new GameObject("IconSlot", typeof(RectTransform));
            slot.transform.SetParent(row.transform, false);
            DiabloUIFactory.SetLayout(slot, prefH: 30f, prefW: 30f);
            var sImg = slot.AddComponent<Image>();
            sImg.sprite = DiabloMenuArt.SlotBackground(null, 64);
            sImg.type = Image.Type.Sliced;
            sImg.raycastTarget = false;

            var skSprite = DiabloMenuArt.SkillIcon(sk);
            if (skSprite != null)
            {
                var iGo = new GameObject("Icon", typeof(RectTransform));
                iGo.transform.SetParent(slot.transform, false);
                var iImg = iGo.AddComponent<Image>();
                iImg.sprite = skSprite;
                iImg.preserveAspect = true;
                iImg.raycastTarget = false;
                var ir = iGo.GetComponent<RectTransform>();
                ir.anchorMin = Vector2.zero;
                ir.anchorMax = Vector2.one;
                ir.offsetMin = new Vector2(4f, 4f);
                ir.offsetMax = new Vector2(-4f, -4f);
            }

            var sText = DiabloUIFactory.MakeText(row.transform, "SkillDesc",
                $"<color={DiabloPalette.ToHex(DiabloPalette.TrimGold)}>[{key}]</color> {DiabloSkills.SkillName(sk)}", 15, DiabloPalette.Parchment);
            DiabloUIFactory.SetLayout(sText.gameObject, prefH: 28f, flexW: 1f);
        }

        // Flexible spacer before CTA banner
        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(card.transform, false);
        DiabloUIFactory.SetLayout(spacer, prefH: 4f, flexH: 1f);

        var ctaGo = new GameObject("PlayCallout", typeof(RectTransform));
        ctaGo.transform.SetParent(card.transform, false);
        DiabloUIFactory.SetLayout(ctaGo, prefH: 36f, flexW: 1f);
        var ctaImg = ctaGo.AddComponent<Image>();
        ctaImg.sprite = DiabloMenuArt.ButtonBackground(true);
        ctaImg.type = Image.Type.Sliced;
        ctaImg.raycastTarget = false;
        var ctaText = DiabloUIFactory.MakeText(ctaGo.transform, "CalloutText", Loc.F("menu.class.cta", cls.displayName.ToUpperInvariant()), 15, DiabloPalette.GoldBright, TextAnchor.MiddleCenter);
        ctaText.fontStyle = FontStyle.Bold;
        Stretch(ctaText.rectTransform);
    }

    static GameObject skillsLblHeight(GameObject go) => go;

    /// <summary>Class card figure frame: tall enough for the whole standing hero.</summary>
    public const float CardFigureWidth = 150f;
    public const float CardFigureHeight = 220f;
    public const float CardsRowHeight = 700f;

    readonly List<KeyValuePair<string, HeroPreviewView>> cardFigures = new List<KeyValuePair<string, HeroPreviewView>>();

    /// <summary>Live card figures by class (test seam).</summary>
    public HeroPreviewView CardFigure(string classId)
    {
        foreach (var kv in cardFigures) if (kv.Key == classId) return kv.Value;
        return null;
    }

    /// <summary>Showcase look per class card (the creation step picks the real one).</summary>
    public static HeroProfile CardProfile(ClassDefinition cls)
    {
        var p = new HeroProfile { heroName = cls.displayName, hairStyle = 0, build = (int)HeroBuild.Athletic };
        if (cls.id == ClassCatalog.LanternbearerId) { p.gender = (int)HeroGender.Female; p.hairStyle = 1; p.hairColor = 5; }
        else if (cls.id == ClassCatalog.RogueId) { p.gender = (int)HeroGender.Male; p.hairStyle = 2; p.hairColor = 7; }
        else { p.gender = (int)HeroGender.Male; p.hairStyle = 0; p.hairColor = 1; }
        return HeroProfile.Sanitized(p);
    }

    void ShowCardFigures(bool show)
    {
        foreach (var kv in cardFigures)
        {
            if (kv.Value == null) continue;
            if (show && Application.isPlaying) kv.Value.Show(ClassCatalog.Get(kv.Key), CardProfile(ClassCatalog.Get(kv.Key)));
            else kv.Value.Hide();
        }
    }

    /// <summary>Card role line, driven by the party role so new classes never inherit another's title.</summary>
    public static string CardSubtitle(ClassDefinition cls)
    {
        switch (cls.role)
        {
            case ClassDefinition.PartyRole.Healer: return Loc.T("menu.class.role.healer");
            case ClassDefinition.PartyRole.Dps: return Loc.T("menu.class.role.dps");
            default: return Loc.T("menu.class.role.tank");
        }
    }

    /// <summary>Starter weapon and real skill count (matches ClassAppearance.SwapStarterWeapon).</summary>
    public static string CardStatTag(ClassDefinition cls)
    {
        string starter = cls.id == ClassCatalog.LanternbearerId ? ItemDatabase.StarterLanternBase
            : cls.id == ClassCatalog.RogueId ? ItemDatabase.StarterBowBase
            : ItemDatabase.StarterSwordBase;
        return Loc.F("menu.class.stattag", LootDrop.PrettyName(starter), cls.skills.Length);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
