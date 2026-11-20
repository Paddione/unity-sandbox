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
        return SpawnItem(pos, BuildItem(roll, rng, ItemLevelRules.ForDrop(pos, tier)), networked);
    }

    public static ItemInstance BuildItem(LootRoll roll, System.Random rng, int itemLevel = 1)
    {
        int affixTier = roll.affixTier > 0 ? roll.affixTier : DifficultyProfile.Current.affixTier;
        string weaponClass = roll.slot == ItemSlot.Weapon ? WeaponClassFor(RunProfile.ClassId, rng) : "";
        if (roll.rarity == ItemRarity.Unique)
        {
            // S4: weapons keep the class-bound selection, armor/helmets split
            // legacy/matrix so every matrix piece drops regularly.
            var unique = roll.slot == ItemSlot.Weapon
                ? ItemDatabase.UniqueWeaponForClass(weaponClass, rng)
                : ItemDatabase.CreateUnique(roll.slot == ItemSlot.Armor
                    ? ItemDatabase.UniqueArmor(rng) : ItemDatabase.UniqueHelmet(rng));
            return ItemDatabase.RollUnique(unique, itemLevel, affixTier);
        }
        var bases = roll.slot == ItemSlot.Weapon
            ? ItemDatabase.GetBaseTypes(roll.slot, weaponClass)
            : ItemDatabase.GetBaseTypes(roll.slot);
        return new ItemInstance
        {
            slot = roll.slot,
            rarity = roll.rarity,
            baseType = bases[rng.Next(bases.Count)],
            affixes = AffixLibrary.RollAffixes(roll.slot, roll.rarity, rng, affixTier, itemLevel),
            affixTier = affixTier,
            itemLevel = itemLevel,
            instanceId = System.Guid.NewGuid().ToString("N"),
        };
    }

    public static GameObject SpawnItem(Vector3 pos, ItemInstance item, bool networked)
    {
        if (item == null) return null;
        var nm = NetworkManager.Singleton;
        var prefab = Resources.Load<GameObject>("NetworkLootDrop");
        if (networked && (nm == null || !nm.IsServer || !nm.IsListening || prefab == null))
            return null;
        item.EnsureIdentity();
        GameObject loot = prefab != null ? Instantiate(prefab, pos, Quaternion.identity) : new GameObject();
        loot.transform.position = pos;
        var drop = loot.GetComponent<LootDrop>() ?? loot.AddComponent<LootDrop>();
        var trigger = loot.GetComponent<BoxCollider>() ?? loot.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = Vector3.one * 0.5f;
        var toy = loot.AddComponent<ToyTrigger>();
        toy.mode = ToyTrigger.Mode.Pickup;
        if (networked)
        {
            try
            {
                loot.GetComponent<LootNetRelay>().SetItem(item);
                loot.GetComponent<NetworkObject>().Spawn();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Loot] Could not spawn pickup: {e.Message}");
                Destroy(loot);
                return null;
            }
        }
        else drop.ShowItem(item);
        EnemyHealth.OrbsDropped++;
        EnemyHealth.ItemsDropped++;
        loot.name = $"DiabloOrb_{EnemyHealth.OrbsDropped}";
        Debug.Log($"[Loot] Dropped {item.rarity} {item.slot} ({ItemLabel(item)}).");
        return loot;
    }

    internal void ShowItem(ItemInstance rolled)
    {
        item = rolled;
        if (item == null) return;
        var visual = transform.Find("LootVisual");
        if (visual != null) Destroy(visual.gameObject);
        // S5: ground sprite of the concrete item + rarity glow behind it.
        LootDropView.ShowOn(gameObject, item);
        if (PresentationVfx.ShouldBeamFor(item.rarity))
            PresentationVfx.RareBeam(transform.position, item.rarity);
    }

    internal static void RecordPickup()
    {
        ToyTrigger.Pickups++;
        ToyTriggerShared.RefreshGoal();
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

    /// <summary>Class whose weapons a drop rolls: 70 % the hero's class, else one of the other two.</summary>
    public static string WeaponClassFor(string heroClassId, System.Random rng)
    {
        string own = ClassCatalog.Get(heroClassId).id;
        if (rng.NextDouble() < OwnClassWeaponShare) return own;
        var others = new System.Collections.Generic.List<string>();
        foreach (var c in ClassCatalog.All)
            if (c.id != own) others.Add(c.id);
        if (others.Count == 0) return own;
        return others[rng.Next(others.Count)];
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
        return PrettyName(item.rarity == ItemRarity.Unique ? item.uniqueId : item.baseType);
    }

    /// <summary>
    /// Player-facing name for stable ids: kebab unique ids ("bulwark-carrier",
    /// "wick-of-the-halo") become title case, CamelCase base types ("ClothTunic")
    /// get spaces. Ids themselves stay untouched (save, network, art keys).
    /// </summary>
    public static string PrettyName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw ?? "";
        var sb = new System.Text.StringBuilder(raw.Length + 8);
        if (raw.IndexOf('-') >= 0 && raw.IndexOf(' ') < 0)
        {
            var words = raw.Split('-');
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i];
                if (w.Length == 0) continue;
                if (sb.Length > 0) sb.Append(' ');
                bool small = i > 0 && (w == "of" || w == "the" || w == "and");
                sb.Append(small ? w : char.ToUpperInvariant(w[0]) + w.Substring(1));
            }
            return sb.ToString();
        }
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (i > 0 && char.IsUpper(c) && char.IsLower(raw[i - 1])) sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
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
        var relay = GetComponent<LootNetRelay>();
        if (relay != null && relay.IsSpawned)
        {
            var player = go.GetComponent<DiabloNetworkPlayer>();
            if (player != null && player.IsOwner) relay.RequestPickupServerRpc();
            return;
        }
        var inv = go.GetComponent<Inventory>();
        var equipment = go.GetComponent<Equipment>();
        if (inv != null)
        {
            if (!inv.Add(item)) return;
        }
        else
        {
            if (equipment == null || !Equipment.CanEquip(item, RunProfile.ClassId)) return;
            equipment.Equip(item);
        }
        collected = true;
        RecordPickup();
        DiabloAudio.PlayLoot();
        GetComponent<Collider>().enabled = false;
        foreach (var n in new[] { "LootVisual", "LootBall", "ItemIcon", "RarityGlow", "GroundGlow", "BallMesh" })
        {
            var visual = transform.Find(n);
            if (visual != null) visual.gameObject.SetActive(false);
            var ball = transform.Find("LootBall");
            if (ball != null)
            {
                var nested = ball.Find(n);
                if (nested != null) nested.gameObject.SetActive(false);
            }
        }
    }
}
