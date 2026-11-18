using UnityEngine;

/// <summary>
/// Ashen-fen art bible, single source for every menu color.
/// Style: gothic ashen-fen dark fantasy — charred stone panels, ember-gold
/// trim, parchment text, blood-red life. Mood: torchlight ember key with a
/// cool fen-night shadow bounce.
/// Every menu script reads these; no literal <c>Color.*</c> outside this file
/// and <see cref="LootDrop"/> rarity colors (gameplay, kept in sync).
/// </summary>
public static class DiabloPalette
{
    public static readonly Color Panel = new Color(0.078f, 0.063f, 0.102f, 0.96f); // #14101A crypt
    public static readonly Color PanelEdge = new Color(0.23f, 0.18f, 0.13f, 1f);   // #3A2E22 bronze
    public static readonly Color TrimGold = new Color(0.95f, 0.69f, 0.20f, 1f);    // #F2B134 ember
    public static readonly Color Parchment = new Color(0.91f, 0.85f, 0.75f, 1f);   // #E8D9BE
    public static readonly Color ParchmentDim = new Color(0.65f, 0.60f, 0.52f, 1f);
    public static readonly Color Abyss = new Color(0.043f, 0.035f, 0.063f, 1f);    // #0B0910
    public static readonly Color Blood = new Color(0.70f, 0.07f, 0.09f, 1f);       // #B31217
    public static readonly Color SlotEmpty = new Color(0.10f, 0.09f, 0.12f, 1f);
    public static readonly Color TabActive = new Color(0.28f, 0.21f, 0.13f, 1f);
    public static readonly Color TabInactive = new Color(0.13f, 0.11f, 0.15f, 1f);
    public static readonly Color ButtonPrimary = new Color(0.46f, 0.10f, 0.10f, 1f);
    public static readonly Color ButtonSecondary = new Color(0.17f, 0.14f, 0.19f, 1f);
    public static readonly Color TooltipBg = new Color(0.055f, 0.045f, 0.075f, 0.96f);
    public static readonly Color GoldBright = new Color(0.98f, 0.80f, 0.28f, 1f);
    public static readonly Color LanternEmber = new Color(0.96f, 0.56f, 0.18f, 1f);
    public static readonly Color FenSteel = new Color(0.55f, 0.72f, 0.88f, 1f);
    public static readonly Color DisabledText = new Color(0.45f, 0.42f, 0.38f, 1f);

    public static Color ClassAccent(string classId)
    {
        return classId == ClassCatalog.LanternbearerId ? LanternEmber : FenSteel;
    }

    public static Color Rarity(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Unique: return new Color(1f, 0.48f, 0.10f, 1f); // ember
            case ItemRarity.Rare: return new Color(0.95f, 0.76f, 0.19f, 1f); // gold
            default: return new Color(0.35f, 0.55f, 0.94f, 1f); // magic blue
        }
    }

    public static string ToHex(Color c)
    {
        return $"#{(int)(c.r * 255):X2}{(int)(c.g * 255):X2}{(int)(c.b * 255):X2}";
    }
}
