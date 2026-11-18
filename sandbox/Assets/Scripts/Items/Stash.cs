using System.Collections.Generic;

/// <summary>
/// Lantern Hold chest storage (replayability S1 data, S5 adds the chest and UI).
/// Pure C#, 40 slots, API mirrors <see cref="Inventory"/>. Persisted through
/// <see cref="SaveGame.stash"/> via <see cref="RunProfile"/>.
/// </summary>
public sealed class Stash
{
    public const int Capacity = 40;

    readonly List<ItemInstance> items = new List<ItemInstance>();

    public int Count => items.Count;
    public int FreeSlots => Capacity - items.Count;

    public ItemInstance Get(int index) =>
        index < 0 || index >= items.Count ? null : items[index];

    /// <summary>Store an item. False when full or null (caller keeps it).</summary>
    public bool Add(ItemInstance item)
    {
        if (item == null || items.Count >= Capacity) return false;
        items.Add(item);
        return true;
    }

    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= items.Count) return false;
        items.RemoveAt(index);
        return true;
    }

    public List<ItemInstance> ToList() => new List<ItemInstance>(items);

    /// <summary>Rebuild from a saved list; entries beyond <see cref="Capacity"/> or null are dropped.</summary>
    public static Stash FromList(List<ItemInstance> saved)
    {
        var s = new Stash();
        if (saved != null)
            foreach (var item in saved)
                s.Add(item);
        return s;
    }
}
