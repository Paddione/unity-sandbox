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
        // S2 economy: independent gold roll beside the item orb (local-only
        // pickup, own counters — item-orb counts stay exact for the Q5 loop).
        GoldDrop.SpawnGold(pos, tier, rng);
        var profile = DifficultyProfile.Current;
        var roll = LootTable.Roll(tier, rng, Equipment.LocalMagicFind(), profile.uniqueMul);
        if (!roll.drops) return null;
        return SpawnItem(pos, BuildItem(roll, rng), networked);
    }

    public static ItemInstance BuildItem(LootRoll roll, System.Random rng)
    {
        string weaponClass = roll.slot == ItemSlot.Weapon ? WeaponClassFor(RunProfile.ClassId, rng) : "";
        if (roll.rarity == ItemRarity.Unique)
        {
            if (roll.slot == ItemSlot.Weapon)
                return ItemDatabase.UniqueWeaponForClass(weaponClass, rng);
            string id = roll.slot == ItemSlot.Armor ? "Cryptwarden Plate" : "Seer's Circlet";
            return ItemDatabase.CreateUnique(id);
        }
        var bases = roll.slot == ItemSlot.Weapon
            ? ItemDatabase.GetBaseTypes(roll.slot, weaponClass)
            : ItemDatabase.GetBaseTypes(roll.slot);
        return new ItemInstance
        {
            slot = roll.slot,
            rarity = roll.rarity,
            baseType = bases[rng.Next(bases.Count)],
            affixes = AffixLibrary.RollAffixes(roll.slot, roll.rarity, rng, DifficultyProfile.Current.affixTier),
            affixTier = System.Math.Max(1, roll.affixTier), // smithing S1: Stufe am Drop erfassen
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
        // S3 VFX hook only: Rare+ drops earn a light pillar, Magic stays quiet.
        if (PresentationVfx.ShouldBeamFor(item.rarity)) PresentationVfx.RareBeam(pos, item.rarity);
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
    /// Pickup routing (Ashen Fen): into the hero's bag when present, else
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
        if (eq == null) return;
        if (!Equipment.CanEquip(item, RunProfile.ClassId))
        {
            Debug.Log($"[Loot] Bag full, {item.baseType} does not fit this class: left behind.");
            return;
        }
        eq.Equip(item);
    }

    /// <summary>Share of weapon drops that fit the finder's class (second-class S4).</summary>
    public const float OwnClassWeaponShare = 0.7f;

    /// <summary>Class whose weapons a drop rolls: 70 % the hero's class, else the other one.</summary>
    public static string WeaponClassFor(string heroClassId, System.Random rng)
    {
        string own = ClassCatalog.Get(heroClassId).id;
        if (rng.NextDouble() < OwnClassWeaponShare) return own;
        return own == ClassCatalog.FenwalkerId ? ClassCatalog.LanternbearerId : ClassCatalog.FenwalkerId;
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
