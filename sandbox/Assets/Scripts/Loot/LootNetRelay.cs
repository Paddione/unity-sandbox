using UnityEngine;
using Unity.Collections;
using Unity.Netcode;

/// <summary>
/// Server relay for client loot pickups (P2): the server equips the
/// collector and despawns the loot so all clients converge.
/// </summary>
public sealed class LootNetRelay : NetworkBehaviour
{
    readonly NetworkVariable<FixedString4096Bytes> itemSnapshot = new();
    internal void SetItem(ItemInstance item)
    {
        itemSnapshot.Value = new FixedString4096Bytes(JsonUtility.ToJson(item));
    }

    public override void OnNetworkSpawn()
    {
        if (!itemSnapshot.Value.IsEmpty)
            GetComponent<LootDrop>().ShowItem(JsonUtility.FromJson<ItemInstance>(itemSnapshot.Value.ToString()));
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestChestServerRpc(ServerRpcParams rpcParams = default)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;
        var loot = GetComponent<LootDrop>();
        if (loot == null || !loot.isChest || loot.collected) return;
        var heroT = SenderHero(nm, rpcParams.Receive.SenderClientId);
        if (heroT == null) return;
        if (TradeValidator.CanPickup(Planar(heroT.position, transform.position), loot.collected) != null)
            return;
        loot.collected = true;
        LootDrop.SpawnLoot(transform.position + Vector3.up * 1f,
            LootTier.Chest, new System.Random(), true);
        Debug.Log("[Loot] Chest opened (guaranteed Rare+).");
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestPickupServerRpc(ServerRpcParams rpcParams = default)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;
        var loot = GetComponent<LootDrop>();
        var sender = rpcParams.Receive.SenderClientId;
        var heroT = SenderHero(nm, sender);
        if (heroT == null) return;
        if (loot == null || loot.collected || loot.item == null) return;
        if (TradeValidator.CanPickup(Planar(heroT.position, transform.position), loot.collected) != null)
            return;
        var player = heroT.GetComponent<DiabloNetworkPlayer>();
        if (player == null || !player.TryGrantPickup(loot.item)) return;
        loot.collected = true;
        LootDrop.RecordPickup();
        var no = GetComponent<NetworkObject>();
        if (no != null && no.IsSpawned) no.Despawn();
        else Destroy(gameObject);
    }

    /// <summary>Sender's hero transform on this process (range validation).</summary>
    static Transform SenderHero(NetworkManager nm, ulong sender)
    {
        if (nm == null || nm.SpawnManager == null) return null;
        foreach (var kv in nm.SpawnManager.SpawnedObjects)
        {
            var nb = kv.Value;
            if (!nb.IsPlayerObject || nb.OwnerClientId != sender) continue;
            return nb.transform;
        }
        return null;
    }

    static float Planar(Vector3 a, Vector3 b)
    {
        var d = a - b;
        d.y = 0f;
        return d.magnitude;
    }
}
