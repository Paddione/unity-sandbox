using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// cinderreach-hud-redesign: theme building blocks (bundled OFL fonts, panel
/// chrome, shared layout) and the pure helpers behind the new HUD pieces.
/// </summary>
public class HudThemeTests
{
    [Test]
    public void Fonts_AreBundledNotBuiltin()
    {
        var builtin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Assert.IsNotNull(Resources.Load<Font>(DiabloUIFactory.DisplayFontPath), "Cinzel ships under Resources/Fonts.");
        Assert.IsNotNull(Resources.Load<Font>(DiabloUIFactory.BodyFontPath), "Alegreya Sans ships under Resources/Fonts.");
        Assert.AreNotSame(builtin, DiabloUIFactory.DisplayFont);
        Assert.AreNotSame(builtin, DiabloUIFactory.BodyFont);
        Assert.AreNotSame(DiabloUIFactory.DisplayFont, DiabloUIFactory.BodyFont);
    }

    [Test]
    public void Panel_HasBackgroundOutlineAndOptionalGoldTop()
    {
        var root = new GameObject("PanelRoot", typeof(RectTransform));
        var plain = HudChrome.Panel(root.transform, "Plain", new Vector2(200f, 80f));
        Assert.IsNotNull(plain.GetComponent<Image>());
        Assert.IsFalse(plain.GetComponent<Image>().raycastTarget, "HUD chrome never eats clicks.");
        Assert.AreEqual(DiabloPalette.PanelEdge, plain.GetComponent<Outline>().effectColor);
        Assert.IsNull(plain.Find("GoldTop"));
        var gold = HudChrome.Panel(root.transform, "Gold", new Vector2(200f, 80f), goldTop: true);
        Assert.IsNotNull(gold.Find("GoldTop"));
        Object.DestroyImmediate(root);
    }

    [Test]
    public void Bar_FillHasSpriteSoFillAmountRenders()
    {
        var root = new GameObject("BarRoot", typeof(RectTransform));
        HudChrome.Bar(root.transform, "Bar", new Vector2(100f, 8f), DiabloPalette.Blood, out var fill);
        Assert.IsNotNull(fill.sprite);
        Assert.AreEqual(Image.Type.Filled, fill.type);
        Object.DestroyImmediate(root);
    }

    [Test]
    public void Layout_ClusterIsSymmetricAndRowFitsThePanel()
    {
        Assert.AreEqual(-HudLayout.GlobeCenterX, HudLayout.SideCenterX, 0.01f, "Globe and gold column mirror around the panel.");
        Assert.LessOrEqual(HudLayout.RowWidth, HudLayout.PanelWidth - 20f, "Six skills plus the flask fit inside the plate.");
        Assert.AreEqual(HudLayout.RowWidth * 0.5f - HudLayout.SlotSize * 0.5f, HudLayout.FlaskCenterX, 0.01f);
        Assert.Less(HudLayout.SkillRowCenterX + (HudLayout.SkillSlots * HudLayout.SlotSize + (HudLayout.SkillSlots - 1) * HudLayout.SlotGap) * 0.5f,
            HudLayout.FlaskCenterX - HudLayout.SlotSize * 0.5f, "The flask cell does not overlap the last skill.");
        Assert.GreaterOrEqual(HudLayout.SlotSize, 44f, "Touch-size slots.");
    }

    [Test]
    public void PartyRows_OwnFirstAndCappedAtFour()
    {
        var own = new PartyFramesHud.Row { name = "Wanderer", own = true, level = 14, hp01 = 0.7f, alive = true };
        var others = new[]
        {
            new PartyFramesHud.Row { name = "A", level = 3 },
            new PartyFramesHud.Row { name = "Dup", own = true },
            new PartyFramesHud.Row { name = "B" },
            new PartyFramesHud.Row { name = "C" },
            new PartyFramesHud.Row { name = "D" },
        };
        var rows = PartyFramesHud.Rows(own, others);
        Assert.AreEqual(PartyFramesHud.MaxFrames, rows.Count);
        Assert.AreEqual("Wanderer", rows[0].name);
        Assert.AreEqual("A", rows[1].name);
        Assert.AreEqual("B", rows[2].name, "A second 'own' row is never drawn twice.");
        Assert.AreEqual(0, PartyFramesHud.Rows(null, null).Count);
    }

    [Test]
    public void PureHelpers_FormatLikeTheCanvas()
    {
        Assert.AreEqual(0.25f, PartyFramesHud.Hp01(new Vector2(30f, 120f)), 1e-4f);
        Assert.AreEqual(0f, PartyFramesHud.Hp01(Vector2.zero));
        Assert.AreEqual("1.284", DiabloLifeGlobe.FormatGold(1284));
        Assert.AreEqual("62 %", DiabloTargetBar.PercentText(0.615f));
        Assert.AreEqual("", DiabloTargetBar.TierLabel(LootTier.Normal));
        Assert.IsNotEmpty(DiabloTargetBar.TierLabel(LootTier.Elite));
        Assert.AreEqual(0f, SkillBarHud.SweepFill(0f, 8f));
        Assert.AreEqual(0.5f, SkillBarHud.SweepFill(4f, 8f), 1e-4f);
        StringAssert.Contains("/", DiabloProgression.XpAmountText(3, 120));
        Assert.AreEqual(Loc.T("hud.xp.max"), DiabloProgression.XpAmountText(DiabloProgression.MaxLevel, 0));
    }
}
