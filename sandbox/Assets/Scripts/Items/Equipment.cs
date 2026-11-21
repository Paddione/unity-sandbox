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

    /// <summary>
    /// Unity (scene serialization) and JsonUtility (saves) never keep a null
    /// [Serializable] field: an empty slot comes back as a blank ItemInstance.
    /// Blank means no base type and no unique id.
    /// </summary>
    public static bool IsBlank(ItemInstance item) =>
        item == null || (string.IsNullOrEmpty(item.baseType) && string.IsNullOrEmpty(item.uniqueId));

    /// <summary>Turn blank slot placeholders back into real empty slots.</summary>
    public void NormalizeEmptySlots()
    {
        if (IsBlank(armor)) armor = null;
        if (IsBlank(weapon)) weapon = null;
        if (IsBlank(helmet)) helmet = null;
    }

    void Awake() => NormalizeEmptySlots();

    public void Equip(ItemInstance item)
    {
        if (item == null) return;
        SetSlot(item.slot, item);
        string label = item.rarity == ItemRarity.Unique ? item.uniqueId : item.baseType;
        Debug.Log($"[Loot] Equipped {item.rarity} {item.slot}: {label} " +
                  $"(dmg={GetDamage()}, hp={GetMaxHP()}).");
    }

    /// <summary>Class binding (second-class S4): restricted bases only for their class; consumables cannot equip.</summary>
    public static bool CanEquip(ItemInstance item, string classId)
    {
        if (item == null || item.slot == ItemSlot.Consumable) return false;
        string only = ItemDatabase.ClassRestriction(item.baseType);
        return only == "" || only == ClassCatalog.Get(classId).id;
    }

    /// <summary>Currently equipped item in a slot (null when empty).</summary>
    public ItemInstance GetEquipped(ItemSlot slot)
    {
        NormalizeEmptySlots();
        switch (slot)
        {
            case ItemSlot.Armor: return armor;
            case ItemSlot.Weapon: return weapon;
            case ItemSlot.Helmet: return helmet;
            default: return null;
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
            case ItemSlot.Helmet: helmet = item; break;
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

    /// <summary>S3 crit: 5 % base + legacy Crit points + 1 point per 10 CritRating, capped.</summary>
    public const float BaseCritChance = 5f;
    public const float MaxCritChance = 75f;
    public float GetCrit() => UnityEngine.Mathf.Min(MaxCritChance,
        BaseCritChance + Total(AffixLibrary.Crit) + Total(AffixLibrary.CritRating) / 10f);

    /// <summary>S3 area: IncreasedArea summed over all slots, capped at 100 % (= 2x area).</summary>
    public const float MaxIncreasedAreaPct = 100f;
    public float AreaBonusPct() =>
        UnityEngine.Mathf.Min(MaxIncreasedAreaPct, Total(AffixLibrary.IncreasedArea));

    /// <summary>
    /// Scale an AoE radius after rank/variant scaling. Hit volume and VFX share
    /// the result, so visuals never lie about the lethal area. Single-target,
    /// flight and move ranges bypass this on purpose.
    /// </summary>
    public float ScaleArea(float baseRadius) =>
        baseRadius * UnityEngine.Mathf.Sqrt(1f + AreaBonusPct() / 100f);
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
