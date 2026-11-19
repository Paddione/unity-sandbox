using System;
using System.Collections.Generic;
using UnityEngine;

public static class NetworkItemOwnership
{
    [Serializable]
    public class State
    {
        public List<ItemInstance> bag;
        public List<ItemInstance> stash;
        public ItemInstance armor;
        public ItemInstance weapon;
        public ItemInstance helmet;
        public int gold;
    }

    public static State Capture(GameObject hero, Stash stash)
    {
        var eq = hero.GetComponent<Equipment>();
        return new State
        {
            bag = hero.GetComponent<Inventory>()?.Contents(),
            stash = stash?.ToList(),
            armor = eq?.armor,
            weapon = eq?.weapon,
            helmet = eq?.helmet,
            gold = hero.GetComponent<Wallet>()?.Gold ?? 0,
        };
    }

    public static ItemInstance Validate(ItemInstance item, HashSet<string> ids)
    {
        if (item == null || !Enum.IsDefined(typeof(ItemSlot), item.slot)
            || !Enum.IsDefined(typeof(ItemRarity), item.rarity) || item.itemLevel is < 0 or > 100
            || item.affixTier is < 0 or > 2 || item.affixes == null || item.affixes.Count > 6
            || item.reforgeIndexPlusOne < 0 || item.reforgeIndexPlusOne > item.affixes.Count)
            return null;
        if (item.slot == ItemSlot.Consumable)
        {
            var potion = ItemDatabase.CreateHealingPotion();
            if (potion.baseType != item.baseType) return null;
            potion.instanceId = item.instanceId;
            item = potion;
        }
        else if (item.rarity == ItemRarity.Unique)
        {
            var unique = ItemDatabase.GetUniques().Find(x => x.uniqueId == item.uniqueId);
            if (unique == null || unique.slot != item.slot || unique.baseType != item.baseType) return null;
        }
        else if (item.slot == ItemSlot.Weapon
            ? string.IsNullOrEmpty(ItemDatabase.ClassRestriction(item.baseType))
            : !ItemDatabase.GetBaseTypes(item.slot).Contains(item.baseType)) return null;

        foreach (var affix in item.affixes)
        {
            if (affix == null || string.IsNullOrEmpty(affix.key)
                || AffixLibrary.Get(affix.key) == null || float.IsNaN(affix.value)
                || float.IsInfinity(affix.value) || affix.value < 0 || affix.value > 100)
                return null;
        }
        item.EnsureIdentity();
        return Guid.TryParseExact(item.instanceId, "N", out _) && ids.Add(item.instanceId) ? item : null;
    }
}
