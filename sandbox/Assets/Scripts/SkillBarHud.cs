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
            if (veil != null) veil.enabled = onCd;
            if (cdText != null)
            {
                cdText.enabled = onCd;
                if (onCd) cdText.text = $"{left:0.0}";
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

        // Bronze HUD plate under skills, key labels and the XP bar (XpCanvas sorts above at 6),
        // so the bar reads against bright town paving and dark fen alike.
        var plateGo = new GameObject("HudPlate", typeof(RectTransform));
        plateGo.transform.SetParent(canvasGo.transform, false);
        var plate = plateGo.AddComponent<Image>();
        plate.sprite = DiabloMenuArt.PanelBackground();
        plate.type = Image.Type.Sliced;
        plate.color = new Color(1f, 1f, 1f, 0.92f);
        plate.raycastTarget = false;
        var prt = plate.rectTransform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0f);
        prt.sizeDelta = new Vector2(880f, 100f);
        prt.anchoredPosition = new Vector2(0f, -6f);

        var rowGo = new GameObject("SkillSlotsRow", typeof(RectTransform));
        rowGo.transform.SetParent(canvasGo.transform, false);
        var rrt = rowGo.GetComponent<RectTransform>();
        rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(0.5f, 0f);
        rrt.sizeDelta = new Vector2(340f, 52f);
        rrt.anchoredPosition = new Vector2(0f, 20f);
        var hz = rowGo.AddComponent<HorizontalLayoutGroup>();
        hz.spacing = 10f;
        hz.childAlignment = TextAnchor.MiddleCenter;
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
            slot.GetComponent<RectTransform>().sizeDelta = new Vector2(50f, 50f);
            var frame = slot.AddComponent<Image>();
            frame.sprite = DiabloMenuArt.SlotBackground(sk == last ? ItemRarity.Rare : (ItemRarity?)null, 64);
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
                var iGo = new GameObject("Icon", typeof(RectTransform));
                iGo.transform.SetParent(slot.transform, false);
                var iImg = iGo.AddComponent<Image>();
                iImg.sprite = spr;
                iImg.preserveAspect = true;
                iImg.raycastTarget = false;
                var ir = iGo.GetComponent<RectTransform>();
                ir.anchorMin = Vector2.zero;
                ir.anchorMax = Vector2.one;
                ir.offsetMin = new Vector2(4f, 4f);
                ir.offsetMax = new Vector2(-4f, -4f);
            }

            var veilGo = new GameObject("CooldownVeil", typeof(RectTransform));
            veilGo.transform.SetParent(slot.transform, false);
            var veil = veilGo.AddComponent<Image>();
            veil.color = new Color(0.02f, 0.02f, 0.04f, 0.72f);
            veil.raycastTarget = false;
            var vr = veilGo.GetComponent<RectTransform>();
            vr.anchorMin = Vector2.zero;
            vr.anchorMax = Vector2.one;
            vr.offsetMin = vr.offsetMax = Vector2.zero;
            veil.enabled = false;

            var cd = DiabloUIFactory.MakeText(slot.transform, "CdText", "", 15, DiabloPalette.GoldBright, TextAnchor.MiddleCenter);
            cd.fontStyle = FontStyle.Bold;
            var cdr = cd.rectTransform;
            cdr.anchorMin = Vector2.zero;
            cdr.anchorMax = Vector2.one;
            cdr.offsetMin = cdr.offsetMax = Vector2.zero;
            cd.enabled = false;

            var keyLbl = DiabloUIFactory.MakeText(slot.transform, "KeyBadge", key, 11, DiabloPalette.TrimGold, TextAnchor.LowerRight);
            keyLbl.fontStyle = FontStyle.Bold;
            var kr = keyLbl.rectTransform;
            kr.anchorMin = Vector2.zero;
            kr.anchorMax = Vector2.one;
            kr.offsetMin = new Vector2(2f, 2f);
            kr.offsetMax = new Vector2(-4f, -2f);

            hudSlots.Add((sk, frame, veil, cd));
        }
        hudBuiltFor = cls.id;
    }
}
