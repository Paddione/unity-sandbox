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
    public const float BaseMaxHP = 3f;
    public const float BaseMoveSpeed = 5f;

    public void Equip(ItemInstance item)
    {
        if (item == null) return;
        SetSlot(item.slot, item);
        string label = item.rarity == ItemRarity.Unique ? item.uniqueId : item.baseType;
        Debug.Log($"[Loot] Equipped {item.rarity} {item.slot}: {label} " +
                  $"(dmg={GetDamage()}, hp={GetMaxHP()}).");
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

    public float GetDamage() => BaseDamage + (weapon != null ? weapon.GetAffix("Damage") : 0f);
    public float GetMaxHP() => BaseMaxHP + (armor != null ? armor.GetAffix("MaxHP") : 0f) + (helmet != null ? helmet.GetAffix("MaxHP") : 0f);
    public float GetMoveSpeed() => BaseMoveSpeed + (armor != null ? armor.GetAffix("MoveSpeed") : 0f);
    public float GetCrit() => 5f + (weapon != null ? weapon.GetAffix("Crit") : 0f) + (helmet != null ? helmet.GetAffix("Crit") : 0f);
    public float GetXPMul() => 100f + (helmet != null ? helmet.GetAffix("XPMul") : 0f);
}
