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
        if (item == null) return "Empty slot — no item here.";
        var sb = new System.Text.StringBuilder();
        string name = item.rarity == ItemRarity.Unique && !string.IsNullOrEmpty(item.uniqueId)
            ? item.uniqueId : item.baseType;
        sb.AppendLine($"{name} [{item.rarity}] ({item.slot})");
        sb.AppendLine(item.itemLevel <= 0
            ? "Item level: legacy stock (Altbestand, pre-level scaling)"
            : $"Item level: {item.itemLevel}");
        if (item.slot == ItemSlot.Consumable)
        {
            sb.AppendLine("Use: drink to restore +10 HP (key 1).");
            return sb.ToString().TrimEnd();
        }
        if (!Equipment.CanEquip(item, RunProfile.ClassId))
        {
            string who = ClassCatalog.Get(ItemDatabase.ClassRestriction(item.baseType)).displayName;
            sb.AppendLine($"Cannot equip: {who} only. You may still sell or discard it.");
        }
        if (item.affixes == null || item.affixes.Count == 0)
            sb.AppendLine("Rolls: none");
        else
        {
            sb.AppendLine("Rolls:");
            foreach (var a in item.affixes)
                sb.AppendLine($"  {AffixLibrary.Describe(a)}");
        }
        sb.AppendLine("vs. equipped:");
        sb.AppendLine(DeltaLines(item, equipped));
        string effect = BuildUniqueEffects.EffectTextFor(item);
        if (!string.IsNullOrEmpty(effect)) sb.AppendLine($"Unique: {effect}");
        return sb.ToString().TrimEnd();
    }

    /// <summary>Per-affix delta lines with explicit +/- prefixes.</summary>
    public static string DeltaLines(ItemInstance item, ItemInstance equipped)
    {
        if (item == null) return "no item";
        var keys = new System.Collections.Generic.List<string>();
        void Collect(ItemInstance it)
        {
            if (it?.affixes == null) return;
            foreach (var a in it.affixes)
                if (!keys.Contains(a.key)) keys.Add(a.key);
        }
        Collect(item);
        Collect(equipped);
        if (keys.Count == 0) return "  no bonuses";
        var lines = new System.Collections.Generic.List<string>();
        foreach (var key in keys)
        {
            float mine = item.GetAffix(key);
            float theirs = equipped != null ? equipped.GetAffix(key) : 0f;
            float delta = mine - theirs;
            string text = AffixLibrary.Describe(new Affix(key, mine));
            if (Mathf.Abs(delta) < 0.0001f) lines.Add($"  = {text}");
            else lines.Add($"  {(delta > 0f ? "+" : "")}{delta:0.##} {key} ({text})");
        }
        return string.Join("\n", lines.ToArray());
    }

    /// <summary>Icon for the card header (art coverage via S5 map).</summary>
    public static Sprite IconFor(ItemInstance item, int size = 64) => DiabloMenuArt.ItemIcon(item, size);
}
