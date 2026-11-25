using UnityEngine;

/// <summary>
/// Six-slot equipment (Armor/Weapon/Helmet + gear-slots S1 Gloves/Boots/Belt). Equip replaces the slot item and
/// recalculates derived stats. Local-only in P2; the server applies it on pickup.
/// </summary>
public sealed class Equipment : MonoBehaviour
{
    public ItemInstance armor;
    public ItemInstance weapon;
    public ItemInstance helmet;
    public ItemInstance gloves;
    public ItemInstance boots;
    public ItemInstance belt;

    /// <summary>Every wearable slot in paperdoll order (gear-slots S1). Consumable is never worn.</summary>
    public static readonly ItemSlot[] GearSlots =
    {
        ItemSlot.Armor, ItemSlot.Weapon, ItemSlot.Helmet, ItemSlot.Gloves, ItemSlot.Boots, ItemSlot.Belt,
    };

    public static bool IsGearSlot(ItemSlot slot) => System.Array.IndexOf(GearSlots, slot) >= 0;

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
        if (IsBlank(gloves)) gloves = null;
        if (IsBlank(boots)) boots = null;
        if (IsBlank(belt)) belt = null;
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
        if (item == null || !IsGearSlot(item.slot)) return false;
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
            case ItemSlot.Gloves: return gloves;
            case ItemSlot.Boots: return boots;
            case ItemSlot.Belt: return belt;
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

    /// <summary>Set a slot directly (save/net import). Consumable is ignored.</summary>
    public void SetSlot(ItemSlot slot, ItemInstance item)
    {
        switch (slot)
        {
            case ItemSlot.Armor: armor = item; break;
            case ItemSlot.Weapon: weapon = item; break;
            case ItemSlot.Helmet: helmet = item; break;
            case ItemSlot.Gloves: gloves = item; break;
            case ItemSlot.Boots: boots = item; break;
            case ItemSlot.Belt: belt = item; break;
        }
    }

    public float GetDamage() => BaseDamage + (weapon != null
        ? weapon.GetAffix(AffixLibrary.Damage) + weapon.GetAffix(AffixLibrary.FireDamage)
        : 0f);
    /// <summary>Life from every non-weapon slot (weapon MaxHP rolls stay decorative, as before S1).</summary>
    public float GetMaxHP() => BaseMaxHP + LevelLife() + GearTotal(AffixLibrary.MaxHP);
    float LevelLife()
    {
        var prog = GetComponent<DiabloProgression>();
        return prog != null ? prog.BonusLife : 0f;
    }

    public float GetMoveSpeed() => BaseMoveSpeed + GearTotal(AffixLibrary.MoveSpeed);

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
    public float GetXPMul() => 100f + GearTotal(AffixLibrary.XPMul);

    // --- replayability S2: aggregated affix effects --------------------------

    /// <summary>Hard caps keep stacked rolls sane.</summary>
    public const float MaxLifeStealPct = 25f;
    public const float MaxCooldownReductionPct = 40f;

    /// <summary>Sum of one affix over all six equipped slots.</summary>
    public float Total(string key) => GearTotal(key) + (weapon != null ? weapon.GetAffix(key) : 0f);

    /// <summary>Sum of one affix over the five non-weapon slots.</summary>
    public float GearTotal(string key)
    {
        float total = 0f;
        foreach (var slot in GearSlots)
        {
            if (slot == ItemSlot.Weapon) continue;
            var item = GetEquippedRaw(slot);
            if (item != null) total += item.GetAffix(key);
        }
        return total;
    }

    ItemInstance GetEquippedRaw(ItemSlot slot)
    {
        switch (slot)
        {
            case ItemSlot.Armor: return armor;
            case ItemSlot.Weapon: return weapon;
            case ItemSlot.Helmet: return helmet;
            case ItemSlot.Gloves: return gloves;
            case ItemSlot.Boots: return boots;
            case ItemSlot.Belt: return belt;
            default: return null;
        }
    }

    /// <summary>True when any slot holds the unique (gear-slots S4/S5 effect checks).</summary>
    public bool Wears(string uniqueId)
    {
        if (string.IsNullOrEmpty(uniqueId)) return false;
        foreach (var slot in GearSlots)
        {
            var item = GetEquippedRaw(slot);
            if (item != null && item.rarity == ItemRarity.Unique && item.uniqueId == uniqueId) return true;
        }
        return false;
    }

    /// <summary>Every equipped item with its instance id ensured (net/session start).</summary>
    public void EnsureIdentities()
    {
        foreach (var slot in GearSlots) GetEquippedRaw(slot)?.EnsureIdentity();
    }

    /// <summary>True when an equipped item carries the instance id.</summary>
    public bool HasInstance(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return false;
        foreach (var slot in GearSlots)
            if (GetEquippedRaw(slot)?.instanceId == instanceId) return true;
        return false;
    }

    public float LifeStealPct() => UnityEngine.Mathf.Min(MaxLifeStealPct, Total(AffixLibrary.LifeSteal));
    public float GoldFindPct() => Total(AffixLibrary.GoldFind);
    public float MagicFindPct() => Total(AffixLibrary.MagicFind) + GearUniqueEffects.ExtraMagicFind(this, GoldFindPct());
    public int SkillRankBonus() => UnityEngine.Mathf.RoundToInt(Total(AffixLibrary.SkillRank));
    public int ThornsDamage() => UnityEngine.Mathf.RoundToInt(Total(AffixLibrary.Thorns) * GearUniqueEffects.ThornsMultiplier(this));

    /// <summary>Cooldown factor from CooldownReduction (1 = none, floored by the cap).</summary>
    public float CooldownFactor() =>
        1f - UnityEngine.Mathf.Min(GearUniqueEffects.CooldownCap(this, MaxCooldownReductionPct), Total(AffixLibrary.CooldownReduction)) / 100f;

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
