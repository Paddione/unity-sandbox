using UnityEngine;

/// <summary>
/// 3-slot equipment (Armor/Weapon/Helmet). Equip replaces the slot item and
/// recalculates derived stats. Local-only in P2; the server applies it on pickup.
/// </summary>
public sealed class Equipment : MonoBehaviour
{
    public ItemInstance armor;
    public ItemInstance weapon;
    public ItemInstance helmet;

    public const float BaseDamage = 1f;
    /// <summary>Survives ~10 normal husk strikes (1 dmg / 1.5 s each).</summary>
    public const float BaseMaxHP = 20f;
    public const float BaseMoveSpeed = 5f;

    public void Equip(ItemInstance item)
    {
        if (item == null) return;
        SetSlot(item.slot, item);
        string label = item.rarity == ItemRarity.Unique ? item.uniqueId : item.baseType;
        Debug.Log($"[Loot] Equipped {item.rarity} {item.slot}: {label} " +
                  $"(dmg={GetDamage()}, hp={GetMaxHP()}).");
    }

    /// <summary>Class binding (second-class S4): restricted bases only for their class.</summary>
    public static bool CanEquip(ItemInstance item, string classId)
    {
        if (item == null) return false;
        string only = ItemDatabase.ClassRestriction(item.baseType);
        return only == "" || only == ClassCatalog.Get(classId).id;
    }

    /// <summary>Currently equipped item in a slot (null when empty).</summary>
    public ItemInstance GetEquipped(ItemSlot slot)
    {
        switch (slot)
        {
            case ItemSlot.Armor: return armor;
            case ItemSlot.Weapon: return weapon;
            default: return helmet;
        }
    }

    /// <summary>Move the equipped item back into a bag. False when empty/full.</summary>
    public bool UnequipTo(ItemSlot slot, Inventory inv)
    {
        var current = GetEquipped(slot);
        if (current == null || inv == null) return false;
        if (!inv.Add(current)) return false;
        SetSlot(slot, null);
        return true;
    }

    void SetSlot(ItemSlot slot, ItemInstance item)
    {
        switch (slot)
        {
            case ItemSlot.Armor: armor = item; break;
            case ItemSlot.Weapon: weapon = item; break;
            default: helmet = item; break;
        }
    }

    public float GetDamage() => BaseDamage + (weapon != null
        ? weapon.GetAffix(AffixLibrary.Damage) + weapon.GetAffix(AffixLibrary.FireDamage)
        : 0f);
    public float GetMaxHP() => BaseMaxHP + LevelLife() + (armor != null ? armor.GetAffix("MaxHP") : 0f) + (helmet != null ? helmet.GetAffix("MaxHP") : 0f);
    float LevelLife()
    {
        var prog = GetComponent<DiabloProgression>();
        return prog != null ? prog.BonusLife : 0f;
    }

    public float GetMoveSpeed() => BaseMoveSpeed + (armor != null ? armor.GetAffix("MoveSpeed") : 0f);
    public float GetCrit() => 5f + (weapon != null ? weapon.GetAffix("Crit") : 0f) + (helmet != null ? helmet.GetAffix("Crit") : 0f);
    public float GetXPMul() => 100f + (helmet != null ? helmet.GetAffix("XPMul") : 0f);

    // --- replayability S2: aggregated affix effects --------------------------

    /// <summary>Hard caps keep stacked rolls sane.</summary>
    public const float MaxLifeStealPct = 25f;
    public const float MaxCooldownReductionPct = 40f;

    /// <summary>Sum of one affix over all three equipped slots.</summary>
    public float Total(string key) =>
        (armor != null ? armor.GetAffix(key) : 0f)
        + (weapon != null ? weapon.GetAffix(key) : 0f)
        + (helmet != null ? helmet.GetAffix(key) : 0f);

    public float LifeStealPct() => UnityEngine.Mathf.Min(MaxLifeStealPct, Total(AffixLibrary.LifeSteal));
    public float GoldFindPct() => Total(AffixLibrary.GoldFind);
    public float MagicFindPct() => Total(AffixLibrary.MagicFind);
    public int SkillRankBonus() => UnityEngine.Mathf.RoundToInt(Total(AffixLibrary.SkillRank));
    public int ThornsDamage() => UnityEngine.Mathf.RoundToInt(Total(AffixLibrary.Thorns));

    /// <summary>Cooldown factor from CooldownReduction (1 = none, floored by the cap).</summary>
    public float CooldownFactor() =>
        1f - UnityEngine.Mathf.Min(MaxCooldownReductionPct, Total(AffixLibrary.CooldownReduction)) / 100f;

    /// <summary>Gold after Gold Find (rounded, never below the base amount).</summary>
    public static int ApplyGoldFind(int amount, float goldFindPct) =>
        UnityEngine.Mathf.Max(amount, UnityEngine.Mathf.RoundToInt(amount * (1f + goldFindPct / 100f)));

    /// <summary>
    /// Heal the attacker by LifeSteal % of the damage dealt. Returns the healed
    /// amount (0 without gear, health or steal). Fractions accumulate per hero.
    /// </summary>
    public static float ApplyLifeSteal(Transform attacker, int damageDealt)
    {
        if (attacker == null || damageDealt <= 0) return 0f;
        var eq = attacker.GetComponent<Equipment>();
        var hp = attacker.GetComponent<PlayerHealth>();
        if (eq == null || hp == null) return 0f;
        float pct = eq.LifeStealPct();
        if (pct <= 0f) return 0f;
        float heal = damageDealt * pct / 100f;
        hp.Heal(heal);
        return heal;
    }

    /// <summary>Magic Find of the local hero (loot rolls happen at enemy death).</summary>
    public static float LocalMagicFind()
    {
        var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        var eq = hero != null ? hero.GetComponent<Equipment>() : null;
        return eq != null ? eq.MagicFindPct() : 0f;
    }
}
