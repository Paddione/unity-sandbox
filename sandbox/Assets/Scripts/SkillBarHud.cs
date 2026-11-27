using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UGUI skill bar (SkillBarCanvas: plate, slot row, cooldown veils, text line),
/// extracted from <see cref="DiabloSkills"/> (cinderreach-skills-modularization S3).
/// Plain class owned by DiabloSkills; reads combat state only through narrow
/// delegates (cooldown left, active class, last used skill). The canvas name
/// and child hierarchy are unchanged from the monolithic version.
/// </summary>
public sealed class SkillBarHud
{
    readonly Transform owner;
    readonly Func<DiabloSkills.Skill, float> cooldownLeft;
    readonly Func<ClassDefinition> activeClass;
    readonly Func<DiabloSkills.Skill> lastUsed;

    Text hud;
    Transform slotsContainer;
    string hudBuiltFor = "";
    readonly List<(DiabloSkills.Skill skill, Image frame, Image veil, Text cdText)> hudSlots =
        new List<(DiabloSkills.Skill, Image, Image, Text)>();
    // Longest cooldown seen since the slot went on cooldown: the radial sweep's 100 %.
    readonly Dictionary<DiabloSkills.Skill, float> cdSpan = new Dictionary<DiabloSkills.Skill, float>();

    /// <summary>Sweep fill (0..1) for a cooldown: left over the span it started from.</summary>
    public static float SweepFill(float left, float span) => left <= 0f || span <= 0f ? 0f : Mathf.Clamp01(left / span);

    public SkillBarHud(Transform owner, Func<DiabloSkills.Skill, float> cooldownLeft,
        Func<ClassDefinition> activeClass, Func<DiabloSkills.Skill> lastUsed)
    {
        this.owner = owner;
        this.cooldownLeft = cooldownLeft;
        this.activeClass = activeClass;
        this.lastUsed = lastUsed;
    }

    public void Update()
    {
        if (hud == null) Build();
        var cls = activeClass();
        if (hudBuiltFor != cls.id) RebuildSlots();
        hud.text = Line();
        var last = lastUsed();
        foreach (var (skill, frame, veil, cdText) in hudSlots)
        {
            float left = cooldownLeft(skill);
            bool onCd = left > 0f;
            cdSpan.TryGetValue(skill, out float span);
            if (!onCd) span = 0f;
            else if (left > span) span = left;
            cdSpan[skill] = span;
            if (veil != null)
            {
                veil.enabled = onCd;
                veil.fillAmount = SweepFill(left, span);
            }
            if (cdText != null)
            {
                cdText.enabled = onCd;
                if (onCd) cdText.text = left >= 10f ? $"{left:0}" : $"{left:0.0}";
            }
            if (frame != null)
                frame.color = skill == last ? DiabloPalette.GoldBright : Color.white;
        }
    }

    /// <summary>Skill bar text from the class (Fenwalker keeps "Fireball / Bow / Cleave").</summary>
    public string Line()
    {
        var cls = activeClass();
        var parts = new List<string>();
        for (int i = 0; i < cls.skills.Length; i++)
            parts.Add(Slot(cls.KeyLabel(i), DiabloSkills.SkillName(cls.skills[i]), cls.skills[i]));
        return string.Join("     ", parts.ToArray()) + Loc.T("hud.skillbar.thrust");
    }

    string Slot(string key, string name, DiabloSkills.Skill s)
    {
        float left = cooldownLeft(s);
        return left > 0f ? $"[{key}] {name} {left:0.0}s" : $"[{key}] {name}";
    }

    void Build()
    {
        var canvasGo = new GameObject("SkillBarCanvas");
        canvasGo.transform.SetParent(owner, false);
        DiabloUIFactory.ConfigureOverlayCanvas(canvasGo, sortingOrder: 5, raycaster: true);

        // HUD plate under skills, names and the XP bar (XpCanvas sorts above at 6), canvas
        // "HUD im Spiel": crypt panel, bronze edge, ember-gold top line.
        var prt = HudChrome.Panel(canvasGo.transform, "HudPlate", new Vector2(HudLayout.PanelWidth, HudLayout.PanelHeight), goldTop: true,
            fill: new Color(0.078f, 0.063f, 0.102f, 0.94f));
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0f, HudLayout.PanelBottom);

        var rowGo = new GameObject("SkillSlotsRow", typeof(RectTransform));
        rowGo.transform.SetParent(canvasGo.transform, false);
        var rrt = rowGo.GetComponent<RectTransform>();
        rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(0.5f, 0f);
        rrt.sizeDelta = new Vector2(HudLayout.SkillSlots * HudLayout.SlotSize + (HudLayout.SkillSlots - 1) * HudLayout.SlotGap, HudLayout.SlotSize);
        rrt.anchoredPosition = new Vector2(HudLayout.SkillRowCenterX, HudLayout.SlotY);
        var hz = rowGo.AddComponent<HorizontalLayoutGroup>();
        hz.spacing = HudLayout.SlotGap;
        hz.childAlignment = TextAnchor.MiddleLeft;
        hz.childControlWidth = false;
        hz.childControlHeight = false;
        slotsContainer = rowGo.transform;

        var go = new GameObject("SkillBar");
        go.transform.SetParent(canvasGo.transform, false);
        hud = go.AddComponent<Text>();
        hud.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hud.fontSize = 14;
        hud.alignment = TextAnchor.MiddleCenter;
        hud.color = DiabloPalette.Parchment;
        hud.horizontalOverflow = HorizontalWrapMode.Overflow;
        hud.raycastTarget = false;
        go.AddComponent<Shadow>();
        var r = hud.rectTransform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0f);
        r.sizeDelta = new Vector2(920f, 20f);
        r.anchoredPosition = new Vector2(0f, 2f);
        hud.enabled = false; // hud-redesign: names sit under the slots; Line() stays the text seam

        RebuildSlots();
    }

    void RebuildSlots()
    {
        if (slotsContainer == null) return;
        foreach (Transform c in slotsContainer)
            if (Application.isPlaying) UnityEngine.Object.Destroy(c.gameObject);
            else UnityEngine.Object.DestroyImmediate(c.gameObject);
        hudSlots.Clear();

        var cls = activeClass();
        var last = lastUsed();
        for (int i = 0; i < cls.skills.Length; i++)
        {
            var sk = cls.skills[i];
            string key = cls.keys[i] == "Space" ? Loc.T("key.space.short") : cls.keys[i];

            var slot = new GameObject($"SkillSlot_{sk}", typeof(RectTransform));
            slot.transform.SetParent(slotsContainer, false);
            slot.GetComponent<RectTransform>().sizeDelta = new Vector2(HudLayout.SlotSize, HudLayout.SlotSize);
            var frame = slot.AddComponent<Image>();
            frame.sprite = DiabloMenuArt.SlotBackground(sk == last ? ItemRarity.Rare : (ItemRarity?)null, 96);
            frame.type = Image.Type.Sliced;
            frame.raycastTarget = true;

            var capturedSkill = sk;
            var hover = slot.AddComponent<HoverInfoSource>();
            hover.Bind(owner != null ? owner.gameObject : null, () =>
            {
                var data = SkillBreakdown.BuildFor(owner != null ? owner.gameObject : null, capturedSkill);
                return (data.title, data.meta, data.body, "");
            });

            var spr = DiabloMenuArt.SkillIcon(sk);
            if (spr != null)
            {
                var iImg = HudChrome.Img(slot.transform, "Icon", spr, Color.white);
                iImg.preserveAspect = true;
                HudChrome.Stretch(iImg.rectTransform, 6f);
            }

            // Radial cooldown sweep (canvas: dark wedge shrinking clockwise) instead of a flat veil.
            var veil = HudChrome.Img(slot.transform, "CooldownVeil", HudChrome.WhiteSprite(), DiabloPalette.CooldownVeil);
            veil.type = Image.Type.Filled;
            veil.fillMethod = Image.FillMethod.Radial360;
            veil.fillOrigin = (int)Image.Origin360.Top;
            veil.fillClockwise = false;
            HudChrome.Stretch(veil.rectTransform, 3f);
            veil.enabled = false;

            var cd = DiabloUIFactory.MakeText(slot.transform, "CdText", "", 24, Color.white, TextAnchor.MiddleCenter, font: DiabloUIFactory.DisplayFont);
            cd.fontStyle = FontStyle.Bold;
            HudChrome.Stretch(cd.rectTransform, 0f);
            cd.enabled = false;

            var keyLbl = DiabloUIFactory.MakeText(slot.transform, "KeyBadge", key, 13, DiabloPalette.TrimGold, TextAnchor.UpperLeft, font: DiabloUIFactory.BodyFont);
            keyLbl.fontStyle = FontStyle.Bold;
            HudChrome.Stretch(keyLbl.rectTransform, 5f);

            var nameLbl = DiabloUIFactory.MakeText(slot.transform, "SkillName", DiabloSkills.SkillName(sk), 13, DiabloPalette.ParchmentDim,
                TextAnchor.MiddleCenter, shadow: false, font: DiabloUIFactory.BodyFont);
            nameLbl.horizontalOverflow = HorizontalWrapMode.Overflow;
            var nr = nameLbl.rectTransform;
            nr.anchorMin = nr.anchorMax = nr.pivot = new Vector2(0.5f, 0f);
            nr.sizeDelta = new Vector2(HudLayout.SlotSize + HudLayout.SlotGap, 16f);
            nr.anchoredPosition = new Vector2(0f, HudLayout.SlotNameY - HudLayout.SlotY);

            hudSlots.Add((sk, frame, veil, cd));
        }
        hudBuiltFor = cls.id;
    }
}
