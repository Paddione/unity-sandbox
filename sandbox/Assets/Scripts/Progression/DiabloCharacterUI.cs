using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Runtime character & skill sheet (C toggles, Esc closes).
/// Displays hero portrait, attributes (STR/VIT/FOC), and class skill ranks with
/// Blender-rendered skill icons, crisp non-overlapping rows, disabled states at 0 points
/// or max rank, and a detailed hover breakdown.
/// </summary>
public sealed class DiabloCharacterUI : MonoBehaviour
{
    public bool IsOpen { get; private set; }

    Canvas canvas;
    Transform panelRoot;
    Transform skillSection;
    Text headerSubText;
    Text attrPointsText;
    Text skillPointsText;
    Text strengthText;
    Text vitalityText;
    Text focusText;
    Text detailText;
    Image portraitImg;
    Button strPlusBtn;
    Button vitPlusBtn;
    Button focPlusBtn;
    readonly List<(DiabloSkills.Skill skill, Text label, Button plus)> skillRows = new List<(DiabloSkills.Skill, Text, Button)>();
    string builtFor = "";
    Text variantText;
    readonly List<(DiabloSkills.Skill skill, Button button, Text label)> variantButtons =
        new List<(DiabloSkills.Skill, Button, Text)>();
    public static string VariantLockedHint => Loc.T("char.variant.locked");
    bool building;

    /// <summary>Skills currently displayed on the sheet (class-reactive; test seam).</summary>
    public DiabloSkills.Skill[] RowSkills
    {
        get
        {
            var a = new DiabloSkills.Skill[skillRows.Count];
            for (int i = 0; i < a.Length; i++) a[i] = skillRows[i].skill;
            return a;
        }
    }

    DiabloProgression prog;
    DiabloSkills skills;

    void Awake()
    {
        EnsureBuilt();
        SetOpen(false);
    }

    public void EnsureBuilt()
    {
        if (canvas != null) return;
        prog = GetComponent<DiabloProgression>();
        skills = GetComponent<DiabloSkills>();
        BuildUI();
    }

    bool IsRemoteMirror() => DiabloUIFactory.IsRemoteMirror(this);

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && !IsRemoteMirror())
        {
            if (kb.cKey.wasPressedThisFrame && (!DiabloUIFactory.IsAnyModalOpen(gameObject) || IsOpen))
                SetOpen(!IsOpen);
            else if (kb.escapeKey.wasPressedThisFrame && IsOpen)
            {
                SetOpen(false);
                DiabloUIFactory.MarkEscConsumed();
            }
        }
        if (!IsOpen || building) return;
        var sheet = prog != null ? prog.Sheet : null;
        if (sheet != null && sheet.HasUnspent)
        {
            float s = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 5f);
            if (attrPointsText != null && sheet.freeAttributePoints > 0)
                attrPointsText.rectTransform.localScale = Vector3.one * s;
            if (skillPointsText != null && sheet.freeSkillPoints > 0)
                skillPointsText.rectTransform.localScale = Vector3.one * s;
        }
    }

    public void SetOpen(bool open)
    {
        EnsureBuilt();
        if (IsRemoteMirror()) open = false;
        if (IsOpen != open) DiabloAudio.PlayUIClick();
        IsOpen = open;
        if (open) DiabloUIFactory.CloseSecondaryPanels(gameObject, this);
        else InfoTooltip.HideFor(gameObject);
        if (canvas != null) canvas.enabled = open;
        if (open) Refresh();
    }

    /// <summary>Rebuild all labels and button states from CharacterSheet (test seam).</summary>
    public void Refresh()
    {
        EnsureBuilt();
        var sheet = prog != null ? prog.Sheet : CharacterSheet.For(gameObject);
        if (sheet == null) return;
        if (builtFor != RunProfile.Class.id) BuildSkillRows();

        if (portraitImg != null) portraitImg.sprite = DiabloMenuArt.ClassPortrait(RunProfile.ClassId);
        int lv = prog != null ? prog.Level : 1;
        if (headerSubText != null)
            headerSubText.text = Loc.F("char.header", RunProfile.Class.displayName, lv);

        attrPointsText.text = Loc.F("char.attrpoints", sheet.freeAttributePoints);
        attrPointsText.color = sheet.freeAttributePoints > 0 ? DiabloPalette.GoldBright : DiabloPalette.ParchmentDim;
        skillPointsText.text = Loc.F("char.skillpoints", sheet.freeSkillPoints);
        skillPointsText.color = sheet.freeSkillPoints > 0 ? DiabloPalette.GoldBright : DiabloPalette.ParchmentDim;

        bool canAttr = sheet.freeAttributePoints > 0;
        if (strPlusBtn != null) strPlusBtn.interactable = canAttr;
        if (vitPlusBtn != null) vitPlusBtn.interactable = canAttr;
        if (focPlusBtn != null) focPlusBtn.interactable = canAttr;

        strengthText.text = Loc.F("char.strength.line", sheet.strength, sheet.strength * CharacterSheet.StrengthMeleePerPoint);
        vitalityText.text = Loc.F("char.vitality.line", sheet.vitality, sheet.VitalityLife());
        float factor = RunProfile.Class.focusSkillFactor;
        focusText.text = Loc.F("char.focus.line", sheet.focus, (sheet.FocusSkillMultiplier(factor) - 1f) * 100f, (1f - sheet.CooldownMultiplier()) * 100f);

        foreach (var (skill, text, plus) in skillRows)
        {
            text.text = RowText(sheet, skill);
            if (plus != null) plus.interactable = sheet.freeSkillPoints > 0 && sheet.GetRank(skill) < CharacterSheet.MaxSkillRank;
        }
        if (variantText != null) variantText.text = VariantLines();
        RefreshVariantButtons();
        if (!sheet.HasUnspent)
        {
            attrPointsText.rectTransform.localScale = Vector3.one;
            skillPointsText.rectTransform.localScale = Vector3.one;
        }
    }

    string RowText(CharacterSheet sheet, DiabloSkills.Skill skill)
    {
        int rank = sheet.GetRank(skill);
        string line = $"{DiabloSkills.SkillName(skill)}  {RankPips(rank)}";
        if (skill == DiabloSkills.Skill.Cleave)
        {
            string variant = RunProfile.GetSkillVariant(RunProfile.Class.id, skill);
            float radius = SkillVariantCatalog.CleaveRadiusFor(skills, variant);
            float arc = SkillVariantCatalog.CleaveArcFor(variant);
            string shape = arc >= 360f ? "360°" : Loc.F("char.variant.cone", arc);
            line += $"  ({SkillVariantCatalog.DisplayName(RunProfile.Class.id, skill, variant)}: {shape}, {radius:0.0} m)";
        }
        else if (skill == DiabloSkills.Skill.SearingHalo)
        {
            string variant = RunProfile.GetSkillVariant(RunProfile.Class.id, skill);
            float dur = SkillVariantCatalog.HaloDurationFor(variant);
            string mode = dur > 0f ? Loc.F("char.variant.stays", dur) : Loc.T("char.variant.moves");
            line += $"  ({SkillVariantCatalog.DisplayName(RunProfile.Class.id, skill, variant)}: {mode})";
        }
        else if (skill == DiabloSkills.Skill.QuickShot && RunProfile.Class.id == ClassCatalog.RogueId)
        {
            string variant = RunProfile.GetSkillVariant(RunProfile.Class.id, skill);
            string mode = variant == SkillVariantCatalog.QuickFanId
                ? Loc.F("char.variant.bolts", SkillVariantCatalog.FanBolts) : Loc.F("char.variant.bolts", 1);
            line += $"  ({SkillVariantCatalog.DisplayName(RunProfile.Class.id, skill, variant)}: {mode})";
        }
        else if (skill == DiabloSkills.Skill.Wisps && skills != null)
            line += Loc.F("char.skill.wisps", skills.WispCountFor(sheet.wispsRank));
        return line;
    }

    static string RankPips(int rank)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < CharacterSheet.MaxSkillRank; i++)
            sb.Append(i < rank ? '●' : '○');
        return sb.ToString();
    }

    public void SpendStrength() => Spend(CharacterSheet.Attribute.Strength);
    public void SpendVitality() => Spend(CharacterSheet.Attribute.Vitality);
    public void SpendFocus() => Spend(CharacterSheet.Attribute.Focus);
    public void SpendFireball() => SpendRank(DiabloSkills.Skill.Fireball);
    public void SpendArrow() => SpendRank(DiabloSkills.Skill.Arrow);
    public void SpendCleave() => SpendRank(DiabloSkills.Skill.Cleave);
    public void SpendSkill(DiabloSkills.Skill skill) => SpendRank(skill);

    void Spend(CharacterSheet.Attribute attribute)
    {
        EnsureBuilt();
        if (prog != null && prog.SpendAttributePoint(attribute)) { DiabloAudio.PlayUIClick(); Refresh(); }
    }

    void SpendRank(DiabloSkills.Skill skill)
    {
        EnsureBuilt();
        if (prog != null && prog.SpendSkillPoint(skill)) { DiabloAudio.PlayUIClick(); Refresh(); }
    }

    // ---------- variant selection (skill-variants S1) ----------

    /// <summary>Solo session (mirrors never pick variants).</summary>
    public bool IsSoloSession()
    {
        var net = GetComponent<DiabloNetworkPlayer>();
        return net == null || !net.IsSpawned;
    }

    /// <summary>Variant switch allowed only solo inside the Lantern Hold camp.</summary>
    public bool CanChangeVariant() =>
        IsSoloSession() && SkillVariantCatalog.IsInLanternHold(transform.position);

    /// <summary>
    /// Pick a skill variant. True on success; false outside the Hold, in
    /// co-op, or for unknown ids. Points and ranks stay untouched.
    /// </summary>
    public bool TrySelectVariant(DiabloSkills.Skill skill, string variantId)
    {
        EnsureBuilt();
        if (!CanChangeVariant()) return false;
        bool ok = RunProfile.TrySetSkillVariant(RunProfile.Class.id, skill, variantId,
            transform.position, solo: true);
        if (ok) Refresh();
        return ok;
    }

    /// <summary>
    /// Swap a variant skill to its other variant (button hook). Outside the
    /// Hold or in co-op the detail line explains why nothing changed.
    /// </summary>
    public bool CycleVariant(DiabloSkills.Skill skill)
    {
        EnsureBuilt();
        var ids = SkillVariantCatalog.VariantIds(RunProfile.Class.id, skill);
        if (ids.Length < 2) return false;
        string current = RunProfile.GetSkillVariant(RunProfile.Class.id, skill);
        string next = current == ids[0] ? ids[1] : ids[0];
        bool ok = TrySelectVariant(skill, next);
        if (detailText != null)
            detailText.text = ok ? SkillVariantCatalog.Describe(RunProfile.Class.id, skill, next, skills) : VariantLockedHint;
        if (ok) DiabloAudio.PlayUIClick();
        return ok;
    }

    void RefreshVariantButtons()
    {
        bool can = CanChangeVariant();
        foreach (var (skill, button, label) in variantButtons)
        {
            if (button == null) continue;
            button.interactable = can;
            if (label != null)
                label.text = SkillVariantCatalog.DisplayName(RunProfile.Class.id, skill,
                    RunProfile.GetSkillVariant(RunProfile.Class.id, skill));
        }
    }

    /// <summary>Sheet lines for the variant skills (preview == runtime values).</summary>
    public string VariantLines()
    {
        EnsureBuilt();
        var cls = RunProfile.Class;
        var sb = new System.Text.StringBuilder();
        foreach (var skill in cls.skills)
        {
            string current = RunProfile.GetSkillVariant(cls.id, skill);
            if (current == null) continue;
            if (sb.Length > 0) sb.AppendLine();
            sb.Append($"{DiabloSkills.SkillName(skill)}: ");
            sb.Append(SkillVariantCatalog.Describe(cls.id, skill, current, skills));
        }
        if (sb.Length == 0) sb.Append(Loc.T("char.variant.none"));
        else if (!CanChangeVariant())
        {
            sb.AppendLine();
            sb.Append(VariantLockedHint);
        }
        return sb.ToString();
    }

    // ---------- build ----------

    void BuildUI()
    {
        building = true;
        var canvasGo = new GameObject("CharacterCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 10);

        var panelGo = DiabloUIFactory.MakePanel(canvasGo.transform, "Panel", new Vector2(530f, 650f), new Vector2(-18f, 0f));
        // Six-skill classes (Lanternbearer) plus variant lines outgrow a fixed height: fit to content.
        var fit = panelGo.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var pr = panelGo.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(1f, 0.5f);
        pr.anchorMax = new Vector2(1f, 0.5f);
        pr.pivot = new Vector2(1f, 0.5f);

        var layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 7f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        DiabloUIFactory.MakeHeader(panelGo.transform, Loc.T("char.title"), DiabloMenuArt.MenuArtSprite("CrestCinderreach"), () => SetOpen(false));

        // Portrait + Hero Class card
        var topRow = DiabloUIFactory.MakeHorizontalRow(panelGo.transform, "HeroIdentityRow", 12f, 68f);
        topRow.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
        var pFrame = new GameObject("PortraitSlot", typeof(RectTransform));
        pFrame.transform.SetParent(topRow.transform, false);
        DiabloUIFactory.SetLayout(pFrame, prefH: 64f, prefW: 64f);
        var pfImg = pFrame.AddComponent<Image>();
        pfImg.sprite = DiabloMenuArt.SlotBackground(ItemRarity.Unique, 64);
        pfImg.type = Image.Type.Sliced;

        var pGo = new GameObject("Portrait", typeof(RectTransform));
        pGo.transform.SetParent(pFrame.transform, false);
        portraitImg = pGo.AddComponent<Image>();
        portraitImg.sprite = DiabloMenuArt.ClassPortrait(RunProfile.ClassId);
        portraitImg.preserveAspect = true;
        var pir = pGo.GetComponent<RectTransform>();
        pir.anchorMin = Vector2.zero;
        pir.anchorMax = Vector2.one;
        pir.offsetMin = new Vector2(4f, 4f);
        pir.offsetMax = new Vector2(-4f, -4f);

        headerSubText = DiabloUIFactory.MakeText(topRow.transform, "HeroClassSub", "", 16, DiabloPalette.Parchment);
        DiabloUIFactory.SetLayout(headerSubText.gameObject, prefH: 60f, flexW: 1f);

        DiabloUIFactory.MakeDivider(panelGo.transform);

        attrPointsText = DiabloUIFactory.MakeText(panelGo.transform, "AttrPoints", Loc.T("char.attrpoints.zero"), 16, DiabloPalette.GoldBright);
        attrPointsText.fontStyle = FontStyle.Bold;
        DiabloUIFactory.SetLayout(attrPointsText.gameObject, prefH: 24f, flexW: 1f);

        AddRow(panelGo.transform, "STR", null, out strengthText, out strPlusBtn, SpendStrength,
            Loc.T("char.strength.tip"), infoProvider: () => StatHoverData("strength"));
        AddRow(panelGo.transform, "VIT", null, out vitalityText, out vitPlusBtn, SpendVitality,
            Loc.T("char.vitality.tip"), infoProvider: () => StatHoverData("vitality"));
        AddRow(panelGo.transform, "FOC", null, out focusText, out focPlusBtn, SpendFocus,
            Loc.T("char.focus.tip"), infoProvider: () => StatHoverData("focus"));

        DiabloUIFactory.MakeDivider(panelGo.transform);

        skillPointsText = DiabloUIFactory.MakeText(panelGo.transform, "SkillPoints", Loc.T("char.skillpoints.zero"), 16, DiabloPalette.GoldBright);
        skillPointsText.fontStyle = FontStyle.Bold;
        DiabloUIFactory.SetLayout(skillPointsText.gameObject, prefH: 24f, flexW: 1f);

        var skillsGo = new GameObject("SkillRowsContainer", typeof(RectTransform));
        skillsGo.transform.SetParent(panelGo.transform, false);
        DiabloUIFactory.SetLayout(skillsGo, prefH: 214f, flexW: 1f);
        var sv = skillsGo.AddComponent<VerticalLayoutGroup>();
        sv.spacing = 6f;
        sv.childControlWidth = true;
        sv.childControlHeight = false;
        skillSection = skillsGo.transform;

        panelRoot = panelGo.transform;
        BuildSkillRows();
        variantText = DiabloUIFactory.MakeText(panelGo.transform, "VariantLines", "", 13, DiabloPalette.ParchmentDim);
        DiabloUIFactory.SetLayout(variantText.gameObject, prefH: 54f, flexW: 1f);

        var tipCard = new GameObject("DetailCard", typeof(RectTransform));
        tipCard.transform.SetParent(panelGo.transform, false);
        DiabloUIFactory.SetLayout(tipCard, prefH: 52f, flexW: 1f);
        var tcBg = tipCard.AddComponent<Image>();
        tcBg.sprite = DiabloMenuArt.TooltipBackground();
        tcBg.type = Image.Type.Sliced;
        var tv = tipCard.AddComponent<VerticalLayoutGroup>();
        tv.padding = new RectOffset(10, 10, 6, 6);
        detailText = DiabloUIFactory.MakeText(tipCard.transform, "Detail", Loc.T("char.hint"), 13, DiabloPalette.ParchmentDim);
        DiabloUIFactory.SetLayout(detailText.gameObject, prefH: 38f, flexW: 1f);

        building = false;
    }

    /// <summary>One rank row per class skill (rebuilt cleanly inside dedicated container above DetailCard).</summary>
    void BuildSkillRows()
    {
        foreach (var (_, text, _) in skillRows)
            if (text != null)
            {
                var row = text.transform.parent.gameObject;
                if (Application.isPlaying) Destroy(row); else DestroyImmediate(row);
            }
        skillRows.Clear();
        variantButtons.Clear();
        var parent = skillSection != null ? skillSection : panelRoot;
        var cls = RunProfile.Class;
        for (int i = 0; i < cls.skills.Length; i++)
        {
            var skill = cls.skills[i];
            var spr = DiabloMenuArt.SkillIcon(skill);
            var sk = skill;
            AddRow(parent, cls.keys[i], spr, out Text text, out Button plus, () => SpendRank(sk),
                Loc.F("char.skill.tip", cls.KeyLabel(i), DiabloSkills.SkillName(sk), CharacterSheet.MaxSkillRank)
                    + "\n" + DiabloSkills.SkillDescription(sk), 44f, () => SkillHoverData(sk));
            skillRows.Add((sk, text, plus));
            if (SkillVariantCatalog.VariantIds(cls.id, sk).Length >= 2)
            {
                var vBtn = DiabloUIFactory.MakeSmallButton(text.transform.parent, Loc.T("char.variant.btn"), () => CycleVariant(sk), 30f, 118f);
                vBtn.gameObject.name = "VariantButton";
                vBtn.transform.SetSiblingIndex(plus.transform.GetSiblingIndex());
                variantButtons.Add((sk, vBtn, vBtn.GetComponentInChildren<Text>()));
            }
        }
        builtFor = cls.id;
    }

    (string title, string meta, string body, string hint) StatHoverData(string id)
    {
        var entry = CharacterStatBreakdown.BuildFor(gameObject).Find(id);
        return entry != null
            ? (entry.name, entry.formula, entry.RenderTooltipBody(), entry.capNote)
            : ("", "", "", "");
    }

    (string title, string meta, string body, string hint) SkillHoverData(DiabloSkills.Skill skill)
    {
        var data = SkillBreakdown.BuildFor(gameObject, skill);
        return (data.title, data.meta, data.body, "");
    }

    void AddRow(Transform parent, string key, Sprite iconSprite, out Text value, out Button plusBtn,
        UnityEngine.Events.UnityAction onPlus, string tooltip, float height = 38f,
        System.Func<(string title, string meta, string body, string hint)> infoProvider = null)
    {
        var row = new GameObject("Row" + key, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        DiabloUIFactory.SetLayout(row, prefH: height, flexW: 1f);
        var bg = row.AddComponent<Image>();
        bg.sprite = DiabloMenuArt.TooltipBackground();
        bg.type = Image.Type.Sliced;

        var hz = row.AddComponent<HorizontalLayoutGroup>();
        hz.padding = new RectOffset(8, 8, 4, 4);
        hz.spacing = 8f;
        hz.childAlignment = TextAnchor.MiddleLeft;
        hz.childControlWidth = true;
        hz.childControlHeight = false;
        hz.childForceExpandWidth = false;

        if (iconSprite != null)
        {
            var slotGo = new GameObject("SkillSlot", typeof(RectTransform));
            slotGo.transform.SetParent(row.transform, false);
            DiabloUIFactory.SetLayout(slotGo, prefH: 34f, prefW: 34f);
            var sImg = slotGo.AddComponent<Image>();
            sImg.sprite = DiabloMenuArt.SlotBackground(null, 64);
            sImg.type = Image.Type.Sliced;
            var iGo = new GameObject("Icon", typeof(RectTransform));
            iGo.transform.SetParent(slotGo.transform, false);
            var iImg = iGo.AddComponent<Image>();
            iImg.sprite = iconSprite;
            iImg.preserveAspect = true;
            var ir = iGo.GetComponent<RectTransform>();
            ir.anchorMin = Vector2.zero;
            ir.anchorMax = Vector2.one;
            ir.offsetMin = new Vector2(3f, 3f);
            ir.offsetMax = new Vector2(-3f, -3f);
        }

        value = DiabloUIFactory.MakeText(row.transform, "ValueLabel", "", 15, DiabloPalette.Parchment);
        DiabloUIFactory.SetLayout(value.gameObject, prefH: 30f, flexW: 1f);

        plusBtn = DiabloUIFactory.MakeButton(row.transform, "Plus", "+", 18, true, DiabloPalette.GoldBright, onPlus);
        DiabloUIFactory.SetLayout(plusBtn.gameObject, prefH: 30f, prefW: 38f);

        if (!string.IsNullOrEmpty(tooltip) || infoProvider != null)
        {
            var hover = row.AddComponent<HoverInfoSource>();
            hover.Bind(gameObject, infoProvider, onEnter: () =>
            {
                if (detailText != null && !string.IsNullOrEmpty(tooltip)) detailText.text = tooltip;
            });
        }
    }
}
