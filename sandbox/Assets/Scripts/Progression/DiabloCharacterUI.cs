using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// S4 character sheet panel (UGUI, built at runtime like
/// <see cref="DiabloInventoryUI"/>): attributes (Strength/Vitality/Focus, one
/// point each) and skill ranks (Fireball/Arrow/Cleave 1–5, +20 % damage per
/// rank). <c>C</c> toggles, <c>Esc</c> closes. Unspent points pulse so a fresh
/// level-up is impossible to miss. Public contract for tests: IsOpen, SetOpen.
/// Local-only like the sheet itself; mirrors never open it.
/// </summary>
public sealed class DiabloCharacterUI : MonoBehaviour
{
    public bool IsOpen { get; private set; }

    Canvas canvas;
    Text attrPointsText;
    Text skillPointsText;
    Text strengthText;
    Text vitalityText;
    Text focusText;
    Text fireballText;
    Text arrowText;
    Text cleaveText;
    bool building;

    DiabloProgression prog;
    DiabloSkills skills;

    void Awake()
    {
        EnsureBuilt();
        SetOpen(false);
    }

    /// <summary>
    /// Headless EditMode `AddComponent` can skip `Awake`-time wiring, so every
    /// entry point heals itself (same pattern as DiabloInventoryUI).
    /// </summary>
    public void EnsureBuilt()
    {
        if (canvas != null) return;
        prog = GetComponent<DiabloProgression>();
        skills = GetComponent<DiabloSkills>();
        BuildUI();
    }

    bool IsRemoteMirror()
    {
        var net = GetComponent<DiabloNetworkPlayer>();
        return net != null && net.IsSpawned && !net.IsOwner;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && !IsRemoteMirror())
        {
            if (kb.cKey.wasPressedThisFrame) SetOpen(!IsOpen);
            else if (kb.escapeKey.wasPressedThisFrame && IsOpen) SetOpen(false);
        }
        if (!IsOpen || building) return;
        // Unspent points pulse: scale the point labels on a sine.
        var sheet = prog != null ? prog.Sheet : null;
        if (sheet != null && sheet.HasUnspent)
        {
            float s = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 5f);
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
        if (canvas != null) canvas.enabled = open;
        if (open) Refresh();
    }

    /// <summary>Rebuild all labels from the sheet (also the HD-test seam).</summary>
    public void Refresh()
    {
        EnsureBuilt();
        var sheet = prog != null ? prog.Sheet : CharacterSheet.For(gameObject);
        if (sheet == null) return;
        attrPointsText.text = $"Attribute points: {sheet.freeAttributePoints}";
        skillPointsText.text = $"Skill points: {sheet.freeSkillPoints}";
        strengthText.text = $"Strength {sheet.strength}  (+{sheet.strength * CharacterSheet.StrengthMeleePerPoint} melee)";
        vitalityText.text = $"Vitality {sheet.vitality}  (+{sheet.VitalityLife():0} life)";
        focusText.text = $"Focus {sheet.focus}  (+{(sheet.FocusSkillMultiplier() - 1f) * 100f:0}% skill, -{(1f - sheet.CooldownMultiplier()) * 100f:0}% cd)";
        fireballText.text = $"Fireball {RankPips(sheet.fireballRank)}";
        arrowText.text = $"Bow {RankPips(sheet.arrowRank)}";
        float radius = sheet.CleaveRadius(
            skills != null ? skills.cleaveBaseRadius : 2.6f,
            skills != null ? skills.cleaveRadiusPerRank : 0.4f);
        cleaveText.text = $"Cleave {RankPips(sheet.cleaveRank)}  ({radius:0.0} m)";
        if (!sheet.HasUnspent)
        {
            attrPointsText.rectTransform.localScale = Vector3.one;
            skillPointsText.rectTransform.localScale = Vector3.one;
        }
    }

    static string RankPips(int rank)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < CharacterSheet.MaxSkillRank; i++)
            sb.Append(i < rank ? '●' : '○');
        return sb.ToString();
    }

    // ---------- spend hooks (button targets) ----------

    public void SpendStrength() => Spend(CharacterSheet.Attribute.Strength);
    public void SpendVitality() => Spend(CharacterSheet.Attribute.Vitality);
    public void SpendFocus() => Spend(CharacterSheet.Attribute.Focus);
    public void SpendFireball() => SpendRank(DiabloSkills.Skill.Fireball);
    public void SpendArrow() => SpendRank(DiabloSkills.Skill.Arrow);
    public void SpendCleave() => SpendRank(DiabloSkills.Skill.Cleave);

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

    // ---------- build ----------

    void BuildUI()
    {
        building = true;
        var canvasGo = new GameObject("CharacterCanvas");
        canvasGo.transform.SetParent(transform, false);
        canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var panelGo = new GameObject("Panel");
        panelGo.transform.SetParent(canvasGo.transform, false);
        var panel = panelGo.AddComponent<Image>();
        panel.sprite = DiabloMenuArt.PanelBackground();
        panel.type = Image.Type.Sliced;
        panel.color = Color.white;
        var rect = panelGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(480f, 560f);
        rect.anchoredPosition = new Vector2(-16f, 0f);

        var layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;

        MakeLabel(panelGo.transform, "Character (C to close)", 20);
        attrPointsText = MakeLabel(panelGo.transform, "", 16);
        AddRow(panelGo.transform, "STR", out strengthText, SpendStrength);
        AddRow(panelGo.transform, "VIT", out vitalityText, SpendVitality);
        AddRow(panelGo.transform, "FOC", out focusText, SpendFocus);
        skillPointsText = MakeLabel(panelGo.transform, "", 16);
        AddRow(panelGo.transform, "Q", out fireballText, SpendFireball);
        AddRow(panelGo.transform, "E", out arrowText, SpendArrow);
        AddRow(panelGo.transform, "R", out cleaveText, SpendCleave);
        building = false;
    }

    void AddRow(Transform parent, string key, out Text value, UnityEngine.Events.UnityAction onPlus)
    {
        var row = new GameObject("Row" + key);
        row.transform.SetParent(parent, false);
        var hz = row.AddComponent<HorizontalLayoutGroup>();
        hz.spacing = 8f;
        hz.childControlWidth = true;
        value = MakeLabel(row.transform, "", 16);
        var btnGo = new GameObject("Plus");
        btnGo.transform.SetParent(row.transform, false);
        var image = btnGo.AddComponent<Image>();
        image.sprite = DiabloMenuArt.TabBackground(false);
        image.type = Image.Type.Sliced;
        var button = btnGo.AddComponent<Button>();
        button.onClick.AddListener(onPlus);
        var label = MakeLabel(btnGo.transform, "+", 18);
        label.alignment = TextAnchor.MiddleCenter;
        var lr = label.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero;
        lr.offsetMax = Vector2.zero;
        var br = btnGo.GetComponent<RectTransform>();
        br.sizeDelta = new Vector2(44f, 30f);
    }

    static Text MakeLabel(Transform parent, string text, int size)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.color = DiabloPalette.Parchment;
        return label;
    }
}
