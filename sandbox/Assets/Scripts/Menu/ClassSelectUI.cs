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
    public static bool ClassFromCommandLine { get; private set; }

    Canvas canvas;
    GameObject mainContent;
    GameObject settingsModal;
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
                || a == "--net-client" || a == "--net-server" || a == "-batchmode"
                || a == CoopAutoplay.Flag)
                return false;
        return !MultiplayerAutoStarter.IsClone();
    }

    void Update()
    {
        if (!IsOpen) return;
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame && settingsModal != null && settingsModal.activeSelf)
        {
            ShowSettings(false);
            DiabloUIFactory.MarkEscConsumed();
        }
    }

    public void SetOpen(bool open)
    {
        if (open && canvas == null) Build();
        IsOpen = open;
        if (canvas != null) canvas.enabled = open;
        DiabloClickController.ExternalDrive = open;
        if (open)
        {
            ShowSettings(false);
            RefreshContinueAndDiff();
        }
    }

    /// <summary>New character of a class (card button). The first autosave replaces an old save.</summary>
    public void ChooseNew(string classId)
    {
        DiabloAudio.PlayUIClick();
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
        DiabloAudio.PlayUIClick();
        ApplyToHero();
        Debug.Log($"[Class] Continue as {RunProfile.Class.displayName}.");
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
        var look = target.GetComponent<ClassAppearance>();
        if (look != null) look.Apply(RunProfile.Class);
        var net = target.GetComponent<DiabloNetworkPlayer>();
        if (net != null) net.PublishClass();
    }

    /// <summary>Continue button text with the saved hero when the save is readable.</summary>
    public static string ContinueLabel(string path = null)
    {
        if (!SaveService.TryPeek(out SaveGame save, path))
            return "CONTINUE SAVED EXPEDITION";
        var cls = ClassCatalog.Get(save.classId);
        return $"CONTINUE  —  {cls.displayName.ToUpperInvariant()}  •  LEVEL {save.level}  •  {save.gold}G  •  {(save.difficulty ?? RunProfile.Normal).ToUpperInvariant()}";
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
                RunProfile.DifficultyUnlocked ? $"Mode: {RunProfile.Difficulty}" : "Mode: Normal (Kindled locked)");
        }
    }

    void Build()
    {
        var canvasGo = new GameObject("ClassSelectCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 25);

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
        var subTitle = DiabloUIFactory.MakeText(titleCol.transform, "Subtitle", "THE ASHEN FEN  •  CHOOSE YOUR HERO", 16, DiabloPalette.ParchmentDim, TextAnchor.MiddleLeft);
        DiabloUIFactory.SetLayout(subTitle.gameObject, prefH: 24f, flexW: 1f);

        DiabloUIFactory.MakeDivider(mainContent.transform, 6f);

        // Class Selection Card Deck
        var cardsRow = new GameObject("CardsRow", typeof(RectTransform));
        cardsRow.transform.SetParent(mainContent.transform, false);
        DiabloUIFactory.SetLayout(cardsRow, prefH: 590f, flexW: 1f);
        var ch = cardsRow.AddComponent<HorizontalLayoutGroup>();
        ch.spacing = 18f;
        ch.childControlWidth = true;
        ch.childControlHeight = true;
        ch.childForceExpandWidth = true;
        ch.childForceExpandHeight = true;

        foreach (var cls in ClassCatalog.All)
            BuildClassCard(cardsRow.transform, cls);

        // Continue Expedition Button (shown when save file exists)
        continueBtn = DiabloUIFactory.MakeButton(mainContent.transform, "ContinueButton", "Continue", 18, true, DiabloPalette.GoldBright, () => Continue());
        DiabloUIFactory.SetLayout(continueBtn.gameObject, prefH: 48f, flexW: 1f);
        continueLabel = continueBtn.GetComponentInChildren<Text>();

        // Footer Controls: Difficulty + Settings + Quit
        var footerRow = DiabloUIFactory.MakeHorizontalRow(mainContent.transform, "FooterBar", 10f, 42f);
        diffBadgeText = DiabloUIFactory.MakeText(footerRow.transform, "DiffLabel", DiabloInventoryUI.DifficultyText(), 14, DiabloPalette.ParchmentDim);
        DiabloUIFactory.SetLayout(diffBadgeText.gameObject, prefH: 38f, prefW: 260f, flexW: 1f);

        diffToggleBtn = DiabloUIFactory.MakeSmallButton(footerRow.transform, "Mode: Normal", () =>
        {
            if (!RunProfile.DifficultyUnlocked) return;
            DiabloAudio.PlayUIClick();
            RunProfile.Difficulty = RunProfile.Difficulty == RunProfile.Kindled ? RunProfile.Normal : RunProfile.Kindled;
            RefreshContinueAndDiff();
        }, 38f, 220f);

        // coop-session S1: host or join before choosing (the panel keeps this overlay open).
        var coopBtn = DiabloUIFactory.MakeButton(footerRow.transform, "CoopButton", "Co-op", 15, false, null, () =>
        {
            DiabloAudio.PlayUIClick();
            JoinPanel.Open();
        });
        DiabloUIFactory.SetLayout(coopBtn.gameObject, prefH: 38f, prefW: 130f);

        var settingsBtn = DiabloUIFactory.MakeButton(footerRow.transform, "SettingsButton", "Settings", 15, false, null, () =>
        {
            DiabloAudio.PlayUIClick();
            ShowSettings(true);
        });
        DiabloUIFactory.SetLayout(settingsBtn.gameObject, prefH: 38f, prefW: 150f);

        var quitBtn = DiabloUIFactory.MakeButton(footerRow.transform, "QuitButton", "Quit", 15, false, DiabloPalette.ParchmentDim, QuitGame);
        DiabloUIFactory.SetLayout(quitBtn.gameObject, prefH: 38f, prefW: 120f);

        // Startup Settings Overlay Modal
        settingsModal = DiabloUIFactory.MakePanel(canvasGo.transform, "StartupSettingsPanel", new Vector2(560f, 590f), Vector2.zero);
        var sv = settingsModal.AddComponent<VerticalLayoutGroup>();
        sv.padding = new RectOffset(22, 22, 18, 20);
        sv.spacing = 8f;
        sv.childControlWidth = true;
        sv.childControlHeight = false;
        DiabloUIFactory.MakeHeader(settingsModal.transform, "SETTINGS", DiabloMenuArt.MenuArtSprite("SettingsCog"), () => ShowSettings(false));
        DiabloUIFactory.BuildSettingsWidgets(settingsModal.transform, settingsRefreshes, onBack: () => ShowSettings(false));
        settingsModal.SetActive(false);
        RefreshContinueAndDiff();
        canvas.enabled = false;
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
        btn.onClick.AddListener(() => ChooseNew(id));

        var v = card.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(18, 18, 16, 16);
        v.spacing = 8f;
        v.childControlWidth = true;
        v.childControlHeight = false;

        // Top section: Portrait box + Class Title & Identity
        var topRow = DiabloUIFactory.MakeHorizontalRow(card.transform, "ClassTopRow", 14f, 128f);
        topRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

        var frame = new GameObject("PortraitFrame", typeof(RectTransform));
        frame.transform.SetParent(topRow.transform, false);
        DiabloUIFactory.SetLayout(frame, prefH: 124f, prefW: 124f);
        var fImg = frame.AddComponent<Image>();
        fImg.sprite = DiabloMenuArt.SlotBackground(cls.role == ClassDefinition.PartyRole.Healer ? ItemRarity.Unique
            : cls.role == ClassDefinition.PartyRole.Dps ? ItemRarity.Magic : ItemRarity.Rare);
        fImg.type = Image.Type.Sliced;
        fImg.raycastTarget = false;

        var portSprite = DiabloMenuArt.ClassPortrait(cls.id);
        if (portSprite != null)
        {
            var pGo = new GameObject("Portrait", typeof(RectTransform));
            pGo.transform.SetParent(frame.transform, false);
            var pImg = pGo.AddComponent<Image>();
            pImg.sprite = portSprite;
            pImg.preserveAspect = true;
            pImg.raycastTarget = false;
            var pr = pGo.GetComponent<RectTransform>();
            pr.anchorMin = Vector2.zero;
            pr.anchorMax = Vector2.one;
            pr.offsetMin = new Vector2(8f, 8f);
            pr.offsetMax = new Vector2(-8f, -8f);
        }

        var headCol = new GameObject("ClassTitleCol", typeof(RectTransform));
        headCol.transform.SetParent(topRow.transform, false);
        DiabloUIFactory.SetLayout(headCol, prefH: 124f, flexW: 1f);
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
        var skillsHdr = DiabloUIFactory.MakeText(card.transform, "SkillsHeader", "COMBAT SKILLS", 13, DiabloPalette.ParchmentDim);
        DiabloUIFactory.SetLayout(skillsLblHeight(skillsHdr.gameObject), prefH: 18f, flexW: 1f);

        for (int i = 0; i < cls.skills.Length; i++)
        {
            var sk = cls.skills[i];
            var key = cls.keys[i];
            var row = DiabloUIFactory.MakeHorizontalRow(card.transform, $"SkillRow_{sk}", 10f, 34f);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;

            var slot = new GameObject("IconSlot", typeof(RectTransform));
            slot.transform.SetParent(row.transform, false);
            DiabloUIFactory.SetLayout(slot, prefH: 34f, prefW: 34f);
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
            DiabloUIFactory.SetLayout(sText.gameObject, prefH: 30f, flexW: 1f);
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
        var ctaText = DiabloUIFactory.MakeText(ctaGo.transform, "CalloutText", $"BEGIN AS {cls.displayName.ToUpperInvariant()}", 15, DiabloPalette.GoldBright, TextAnchor.MiddleCenter);
        ctaText.fontStyle = FontStyle.Bold;
        Stretch(ctaText.rectTransform);
    }

    static GameObject skillsLblHeight(GameObject go) => go;

    /// <summary>Card role line, driven by the party role so new classes never inherit another's title.</summary>
    public static string CardSubtitle(ClassDefinition cls)
    {
        switch (cls.role)
        {
            case ClassDefinition.PartyRole.Healer: return "Healer  •  Keeper of the Iron Lantern";
            case ClassDefinition.PartyRole.Dps: return "Damage  •  Blade of the Ashen Paths";
            default: return "Tank  •  Oathbound Vanguard of the Fen";
        }
    }

    /// <summary>Starter weapon and real skill count (matches ClassAppearance.SwapStarterWeapon).</summary>
    public static string CardStatTag(ClassDefinition cls)
    {
        string starter = cls.id == ClassCatalog.LanternbearerId ? ItemDatabase.StarterLanternBase
            : cls.id == ClassCatalog.RogueId ? ItemDatabase.StarterBowBase
            : ItemDatabase.StarterSwordBase;
        return $"Starter: {starter}  •  {cls.skills.Length} Skills";
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
