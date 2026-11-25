using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// S5 shared item card, reused by the inventory bag detail AND the vendor
/// columns. Visual hierarchy: name/icon/rarity/slot → item level →
/// formatted rolls → per-affix comparison deltas vs. equipped → unique
/// effect. Bracket 0 (itemLevel == 0) is labeled Altbestand. Boolean unique
/// effects already read as plain text via <see cref="BuildUniqueEffects"/>.
/// Empty slots and unwearable items explain their state and the action.
/// Pure-static <see cref="Render"/> is headless-testable; the component
/// wrapper paints one UGUI text.
/// </summary>
public sealed class ItemDetailsView : MonoBehaviour
{
    Text body;

    void Awake()
    {
        body = GetComponent<Text>();
    }

    /// <summary>Paint the card into this object's text (creates one if needed).</summary>
    public void Show(ItemInstance item, ItemInstance equipped)
    {
        if (body == null) body = gameObject.GetComponent<Text>() ?? gameObject.AddComponent<Text>();
        body.supportRichText = true;
        body.text = Render(item, equipped);
    }

    /// <summary>Full card text. Prefix +/- marks gains/losses (no color needed for tests).</summary>
    public static string Render(ItemInstance item, ItemInstance equipped)
    {
        if (item == null) return Loc.T("item.card.empty");
        var sb = new System.Text.StringBuilder();
        string name = LootDrop.PrettyName(item.rarity == ItemRarity.Unique && !string.IsNullOrEmpty(item.uniqueId)
            ? item.uniqueId : item.baseType);
        sb.AppendLine(Loc.F("item.card.title", name, ItemText.Rarity(item.rarity), ItemText.Slot(item.slot)));
        sb.AppendLine(item.itemLevel <= 0
            ? Loc.T("item.card.ilvl_legacy")
            : Loc.F("item.card.ilvl", item.itemLevel));
        if (!Equipment.CanEquip(item, RunProfile.ClassId))
        {
            string who = ClassCatalog.Get(ItemDatabase.ClassRestriction(item.baseType)).displayName;
            sb.AppendLine(Loc.F("item.card.classlock", who));
        }
        if (item.affixes == null || item.affixes.Count == 0)
            sb.AppendLine(Loc.T("item.card.norolls"));
        else
        {
            sb.AppendLine(Loc.T("item.card.rolls"));
            foreach (var a in item.affixes)
                sb.AppendLine($"  {AffixLibrary.Describe(a)}");
        }
        sb.AppendLine(Loc.T("item.card.vs"));
        sb.AppendLine(DeltaLines(item, equipped));
        string effect = BuildUniqueEffects.EffectTextFor(item);
        if (!string.IsNullOrEmpty(effect)) sb.AppendLine(Loc.F("item.card.unique", effect));
        return sb.ToString().TrimEnd();
    }

    /// <summary>Per-affix delta lines with explicit +/- prefixes.</summary>
    public static string DeltaLines(ItemInstance item, ItemInstance equipped)
    {
        if (item == null) return Loc.T("item.card.noitem");
        var keys = new System.Collections.Generic.List<string>();
        void Collect(ItemInstance it)
        {
            if (it?.affixes == null) return;
            foreach (var a in it.affixes)
                if (!keys.Contains(a.key)) keys.Add(a.key);
        }
        Collect(item);
        Collect(equipped);
        if (keys.Count == 0) return Loc.T("item.card.nobonus");
        var lines = new System.Collections.Generic.List<string>();
        foreach (var key in keys)
        {
            float mine = item.GetAffix(key);
            float theirs = equipped != null ? equipped.GetAffix(key) : 0f;
            float delta = mine - theirs;
            string text = AffixLibrary.Describe(new Affix(key, mine));
            if (Mathf.Abs(delta) < 0.0001f) lines.Add($"  = {text}");
            else lines.Add($"  {(delta > 0f ? "+" : "")}{delta:0.##} {AffixLibrary.StatName(key)} ({text})");
        }
        return string.Join("\n", lines.ToArray());
    }

    /// <summary>Icon for the card header (art coverage via S5 map).</summary>
    public static Sprite IconFor(ItemInstance item, int size = 64) => DiabloMenuArt.ItemIcon(item, size);
}
