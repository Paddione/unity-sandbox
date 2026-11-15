using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Item loot pickup (P2). Carries a rolled <see cref="ItemInstance"/> and one
/// <see cref="ToyTrigger"/> in Pickup mode (Q5 counting compat: Pickups++ arms
/// the goal pad). Solo/host applies the equip directly; a client forwards it
/// to the server via <see cref="LootNetRelay"/> (host is source of truth).
/// </summary>
public sealed class LootDrop : MonoBehaviour
{
    public ItemInstance item;
    public bool isChest;
    public bool collected;

    /// <summary>Roll the tier table and spawn loot; null when nothing drops.</summary>
    public static GameObject SpawnLoot(Vector3 pos, LootTier tier, System.Random rng, bool networked)
    {
        var roll = LootTable.Roll(tier, rng);
        if (!roll.drops) return null;
        return SpawnItem(pos, BuildItem(roll, rng), networked);
    }

    public static ItemInstance BuildItem(LootRoll roll, System.Random rng)
    {
        if (roll.rarity == ItemRarity.Unique)
        {
            string id = roll.slot == ItemSlot.Weapon ? "Emberbrand Blade"
                : roll.slot == ItemSlot.Armor ? "Cryptwarden Plate" : "Seer's Circlet";
            return ItemDatabase.CreateUnique(id);
        }
        var bases = ItemDatabase.GetBaseTypes(roll.slot);
        return new ItemInstance
        {
            slot = roll.slot,
            rarity = roll.rarity,
            baseType = bases[rng.Next(bases.Count)],
            affixes = AffixLibrary.RollAffixes(roll.slot, roll.rarity, rng),
        };
    }

    static GameObject SpawnItem(Vector3 pos, ItemInstance item, bool networked)
    {
        var loot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        EnemyHealth.OrbsDropped++;
        EnemyHealth.ItemsDropped++;
        loot.name = $"DiabloOrb_{EnemyHealth.OrbsDropped}";
        loot.transform.position = pos;
        loot.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            color = ColorFor(item.rarity)
        };
        loot.GetComponent<Renderer>().sharedMaterial = mat;
        loot.GetComponent<BoxCollider>().isTrigger = true;
        loot.AddComponent<ToyTrigger>().mode = ToyTrigger.Mode.Pickup;
        var drop = loot.AddComponent<LootDrop>();
        drop.item = item;
        Debug.Log($"[Loot] Dropped {item.rarity} {item.slot} ({ItemLabel(item)}).");
        if (networked)
        {
            loot.AddComponent<NetworkObject>();
            loot.AddComponent<LootNetRelay>();
            loot.GetComponent<NetworkObject>().Spawn();
        }
        return loot;
    }

    public static Color ColorFor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Unique: return new Color(1f, 0.5f, 0.1f);
            case ItemRarity.Rare: return new Color(1f, 0.85f, 0.2f);
            default: return new Color(0.35f, 0.6f, 1f);
        }
    }

    public static string ItemLabel(ItemInstance item)
    {
        return item.rarity == ItemRarity.Unique ? item.uniqueId : item.baseType;
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected || other == null || other.gameObject == null) return;
        var go = other.gameObject;
        if (go.name != "Player" && !go.CompareTag("Player")) return;
        if (isChest)
        {
            collected = true;
            var rng = new System.Random();
            bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            SpawnLoot(transform.position + Vector3.up * 1f, LootTier.Chest, rng, networked);
            Debug.Log("[Loot] Chest opened (guaranteed Rare+).");
            return;
        }
        if (item == null) return;
        collected = true;
        var nm = NetworkManager.Singleton;
        bool net = nm != null && nm.IsListening;
        if (net && !nm.IsServer)
        {
            var relay = GetComponent<LootNetRelay>();
            var equip = go.GetComponent<Equipment>();
            if (equip != null) equip.Equip(item); // client prediction
            if (relay != null) relay.RequestPickupServerRpc();
            return;
        }
        var eq = go.GetComponent<Equipment>();
        if (eq != null) eq.Equip(item);
    }
}
