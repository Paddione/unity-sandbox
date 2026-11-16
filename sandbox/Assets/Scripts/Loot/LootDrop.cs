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
        var nm = NetworkManager.Singleton;
        // Only the server spawns networked loot; clients receive the replica.
        // (Spawn() on a client only logs an error and spawns nothing.)
        if (networked && nm != null && nm.IsServer)
        {
            loot.AddComponent<NetworkObject>();
            loot.AddComponent<LootNetRelay>();
            loot.GetComponent<NetworkObject>().Spawn();
        }
        return loot;
    }

    /// <summary>
    /// Pickup routing (Blood Moor): into the hero's bag when present, else
    /// straight onto the body (legacy solo behavior without an Inventory).
    /// A full bag overflows onto the body so no loot is ever lost or stuck.
    /// </summary>
    public static void CollectInto(GameObject hero, ItemInstance item)
    {
        if (hero == null || item == null) return;
        var inv = hero.GetComponent<Inventory>();
        if (inv != null && inv.Add(item))
        {
            DiabloAudio.PlayLoot();
            return;
        }
        var eq = hero.GetComponent<Equipment>();
        if (eq != null) eq.Equip(item);
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
            var nmChest = NetworkManager.Singleton;
            bool netChest = nmChest != null && nmChest.IsListening;
            if (netChest && !nmChest.IsServer)
            {
                // Dedicated server owns the drop: forward, server replicates it.
                collected = true;
                var chestRelay = GetComponent<LootNetRelay>();
                if (chestRelay != null) chestRelay.RequestChestServerRpc();
                return;
            }
            collected = true;
            var rng = new System.Random();
            SpawnLoot(transform.position + Vector3.up * 1f, LootTier.Chest, rng, netChest);
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
            CollectInto(go, item); // client prediction
            if (relay != null) relay.RequestPickupServerRpc();
            return;
        }
        CollectInto(go, item);
    }
}
