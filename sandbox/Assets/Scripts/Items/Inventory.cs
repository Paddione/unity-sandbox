using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bag inventory for the Diablo hero (Ashen Fen). Pickups land here via
/// <see cref="LootDrop"/>; <see cref="DiabloInventoryUI"/> equips out of the
/// bag with a swap-back. Local-only per client (P2 pattern); the server only
/// relays drops, never inventories.
/// </summary>
public sealed class Inventory : MonoBehaviour
{
    public const int Capacity = 20;

    readonly List<ItemInstance> bag = new List<ItemInstance>();

    public int Count => bag.Count;

    internal List<ItemInstance> Contents() => new(bag);

    internal void SetContents(List<ItemInstance> items)
    {
        bag.Clear();
        if (items != null) bag.AddRange(items);
    }

    internal int IndexOf(string instanceId) =>
        string.IsNullOrEmpty(instanceId) ? -1 : bag.FindIndex(item => item.instanceId == instanceId);
    public int FreeSlots => Capacity - bag.Count;

    public ItemInstance Get(int index)
    {
        if (index < 0 || index >= bag.Count) return null;
        return bag[index];
    }

    /// <summary>Remove bag[index] (vendor resale, potion drinking). False when empty.</summary>
    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= bag.Count) return false;
        bag.RemoveAt(index);
        return true;
    }

    /// <summary>Add loot to the bag. False when full (caller keeps the loot).</summary>
    public bool Add(ItemInstance item)
    {
        if (item == null || bag.Count >= Capacity) return false;
        item.EnsureIdentity();
        bag.Add(item);
        Debug.Log($"[Loot] Bagged {item.rarity} {item.slot} ({LootDrop.ItemLabel(item)}). " +
                  $"({bag.Count}/{Capacity}).");
        return true;
    }

    /// <summary>
    /// Equip bag[index], swapping the previously equipped item back into the
    /// bag. Space is guaranteed: the swap slot frees exactly one slot.
    /// </summary>
    public bool EquipFromBag(int index, Equipment eq) => EquipFromBag(index, eq, RunProfile.ClassId);

    internal bool EquipFromBag(int index, Equipment eq, string classId)
    {
        if (eq == null || index < 0 || index >= bag.Count) return false;
        var item = bag[index];
        if (!Equipment.CanEquip(item, classId))
        {
            Debug.Log($"[Loot] {item.baseType} is for {ClassCatalog.Get(ItemDatabase.ClassRestriction(item.baseType)).displayName} only.");
            return false;
        }
        var current = eq.GetEquipped(item.slot);
        bag.RemoveAt(index);
        eq.Equip(item);
        if (current != null) bag.Add(current);
        Debug.Log($"[Loot] Equipped {item.rarity} {item.slot} from bag " +
                  $"({(current != null ? "swapped" : "slot was empty")}).");
        return true;
    }
}
