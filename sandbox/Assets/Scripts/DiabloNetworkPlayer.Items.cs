using Unity.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public sealed partial class DiabloNetworkPlayer
{
    public enum ItemAction { Equip, Unequip, Store, Take, Sell, Discard, Buy, BuyPotion, DrinkPotion }

    public readonly NetworkVariable<FixedString64Bytes> visualArmor = new NetworkVariable<FixedString64Bytes>();
    public readonly NetworkVariable<FixedString64Bytes> visualWeapon = new NetworkVariable<FixedString64Bytes>();
    public readonly NetworkVariable<FixedString64Bytes> visualHelmet = new NetworkVariable<FixedString64Bytes>();

    bool itemSessionReady;
    Stash ownedStash;
    VendorStock ownedStock;
    public VendorStock CurrentNetworkStock => ownedStock;

    void InitializeItems(NetworkItemOwnership.State imported, JoinReport report, ulong sender)
    {
        var inventory = GetComponent<Inventory>() ?? gameObject.AddComponent<Inventory>();
        var eq = GetComponent<Equipment>();
        if (IsOwner) ownedStash = RunProfile.Stash;
        else
        {
            var ids = new HashSet<string>();
            eq.armor = NetworkItemOwnership.Validate(report.armor, ids);
            eq.weapon = NetworkItemOwnership.Validate(report.weapon, ids);
            eq.helmet = NetworkItemOwnership.Validate(report.helmet, ids);
            inventory.SetContents(ValidateList(imported?.bag, Inventory.Capacity, ids));
            ownedStash = Stash.FromList(ValidateList(imported?.stash, Stash.Capacity, ids));
            Wallet.EnsureWallet(gameObject).SetGold(Mathf.Clamp(imported?.gold ?? 0, 0, 1_000_000));
        }
        var stall = GameObject.Find(Vendor.VendorObjectName)?.GetComponent<Vendor>();
        ownedStock = VendorStock.Roll(Vendor.StockSeed(stall?.zoneSeed ?? 7, sender));
        itemSessionReady = true;
        SendItemState(includeStock: true);
    }

    static List<ItemInstance> ValidateList(List<ItemInstance> items, int capacity, HashSet<string> ids)
    {
        var valid = new List<ItemInstance>();
        if (items != null)
            foreach (var item in items)
            {
                if (valid.Count >= capacity) break;
                var clean = NetworkItemOwnership.Validate(item, ids);
                if (clean != null) valid.Add(clean);
            }
        return valid;
    }

    public bool RequestItemAction(ItemAction action, ItemInstance item = null)
    {
        if (!IsSpawned || !IsOwner) return false;
        string id = item?.instanceId ?? "";
        if (IsServer) return ApplyItemAction(action, id);
        SubmitItemActionServerRpc(action, id);
        return true;
    }

    [ServerRpc]
    void SubmitItemActionServerRpc(ItemAction action, string id)
    {
        ApplyItemAction(action, id);
    }

    bool ApplyItemAction(ItemAction action, string id)
    {
        if (!IsServer || !itemSessionReady || id == null || id.Length > 32) return false;
        var inventory = GetComponent<Inventory>();
        var eq = GetComponent<Equipment>();
        var wallet = Wallet.EnsureWallet(gameObject);
        int index = inventory.IndexOf(id);
        var item = inventory.Get(index);
        bool changed;
        bool vendorChanged = false;
        bool drank = false;
        switch (action)
        {
            case ItemAction.Equip:
                changed = item != null && item.slot != ItemSlot.Consumable
                    && inventory.EquipFromBag(index, eq, ClassForOwner());
                break;
            case ItemAction.Unequip:
                changed = false;
                foreach (var slot in new[] { ItemSlot.Armor, ItemSlot.Weapon, ItemSlot.Helmet })
                {
                    var current = eq.GetEquipped(slot);
                    if (current == null || current.instanceId != id) continue;
                    changed = eq.UnequipTo(slot, inventory);
                    break;
                }
                break;
            case ItemAction.Store:
                changed = InRange(StashChest.ObjectName, StashChest.InteractRadius)
                    && StashChest.Store(inventory, ownedStash, index);
                break;
            case ItemAction.Take:
                int stashIndex = ownedStash.ToList().FindIndex(x => x.instanceId == id);
                changed = InRange(StashChest.ObjectName, StashChest.InteractRadius)
                    && StashChest.Take(ownedStash, inventory, stashIndex);
                break;
            case ItemAction.Sell:
                changed = InRange(Vendor.VendorObjectName, TradeValidator.VendorRange)
                    && VendorStock.TrySell(index, wallet, inventory);
                break;
            case ItemAction.Discard:
                changed = item != null
                    && LootDrop.SpawnItem(transform.position + transform.forward * 1.5f, item, true) != null
                    && inventory.RemoveAt(index);
                break;
            case ItemAction.Buy:
                int offer = ownedStock.offers.FindIndex(row => row.item.instanceId == id);
                changed = InRange(Vendor.VendorObjectName, TradeValidator.VendorRange)
                    && ownedStock.TryBuy(offer, wallet, inventory);
                vendorChanged = changed;
                break;
            case ItemAction.BuyPotion:
                changed = InRange(Vendor.VendorObjectName, TradeValidator.VendorRange)
                    && VendorStock.BuyPotion(wallet, inventory);
                break;
            case ItemAction.DrinkPotion:
                changed = item != null && item.slot == ItemSlot.Consumable && inventory.RemoveAt(index);
                drank = changed;
                break;
            default: return false;
        }
        if (changed) SendItemState(includeStock: vendorChanged, heal: drank);
        return changed;
    }

    string ClassForOwner() => IsOwner ? RunProfile.ClassId
        : ClassCatalog.All[Mathf.Clamp(classIndex.Value, 0, ClassCatalog.All.Length - 1)].id;

    bool InRange(string objectName, float range)
    {
        var target = GameObject.Find(objectName);
        return target != null && Vendor.CanInteract(transform.position, target.transform.position, range);
    }

    internal bool TryGrantPickup(ItemInstance item)
    {
        if (!IsServer || !itemSessionReady) return false;
        var inventory = GetComponent<Inventory>();
        var id = item.instanceId;
        var equipment = GetComponent<Equipment>();
        if (inventory.IndexOf(id) >= 0 || ownedStash.ToList().Exists(x => x.instanceId == id)
            || equipment.armor?.instanceId == id || equipment.weapon?.instanceId == id
            || equipment.helmet?.instanceId == id || !inventory.Add(item)) return false;
        SendItemState(lootSound: true);
        return true;
    }

    internal bool GrantPickupGold(int amount)
    {
        if (!IsServer || !itemSessionReady) return false;
        var eq = GetComponent<Equipment>();
        Wallet.EnsureWallet(gameObject).Add(Equipment.ApplyGoldFind(amount, eq.GoldFindPct()));
        SendItemState(lootSound: true);
        return true;
    }

    void SendItemState(bool includeStock = false, bool lootSound = false, bool heal = false)
    {
        var equipment = GetComponent<Equipment>();
        visualArmor.Value = VisualId(equipment.armor, ItemSlot.Armor);
        visualWeapon.Value = VisualId(equipment.weapon, ItemSlot.Weapon);
        visualHelmet.Value = VisualId(equipment.helmet, ItemSlot.Helmet);
        string stock = includeStock ? JsonUtility.ToJson(ownedStock) : "";
        if (IsOwner) RefreshOwnedItemState(stock, lootSound, heal);
        else
        {
            string json = JsonUtility.ToJson(NetworkItemOwnership.Capture(gameObject, ownedStash));
            ApplyItemStateClientRpc(json, stock, lootSound, heal, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } },
            });
        }
    }

    static FixedString64Bytes VisualId(ItemInstance item, ItemSlot slot) =>
        item?.rarity == ItemRarity.Unique && HeroGearModels.IsKnown(item.uniqueId, slot)
            ? new FixedString64Bytes(item.uniqueId) : default;

    [ClientRpc]
    void ApplyItemStateClientRpc(string json, string stock, bool lootSound, bool heal,
        ClientRpcParams rpcParams = default)
    {
        if (!IsOwner || IsServer) return;
        var state = JsonUtility.FromJson<NetworkItemOwnership.State>(json);
        (GetComponent<Inventory>() ?? gameObject.AddComponent<Inventory>()).SetContents(state.bag);
        var eq = GetComponent<Equipment>();
        eq.armor = state.armor;
        eq.weapon = state.weapon;
        eq.helmet = state.helmet;
        RunProfile.Stash = Stash.FromList(state.stash);
        Wallet.EnsureWallet(gameObject).SetGold(state.gold);
        RefreshOwnedItemState(stock, lootSound, heal);
    }

    void RefreshOwnedItemState(string stock, bool lootSound, bool heal)
    {
        if (lootSound) DiabloAudio.PlayLoot();
        var health = GetComponent<PlayerHealth>();
        if (heal) health?.Heal(ItemDatabase.PotionHeal);
        health?.Refresh();
        var vendor = GetComponent<DiabloVendorUI>();
        if (!string.IsNullOrEmpty(stock))
        {
            if (!IsServer) ownedStock = JsonUtility.FromJson<VendorStock>(stock);
            vendor?.ApplyServerStock(ownedStock);
        }
        if (vendor != null && vendor.IsOpen) vendor.Refresh();
        var inventory = GetComponent<DiabloInventoryUI>();
        if (inventory != null && inventory.IsOpen) inventory.Refresh();
        var stash = GetComponent<DiabloStashUI>();
        if (stash != null && stash.IsOpen) stash.Refresh();
    }
}
