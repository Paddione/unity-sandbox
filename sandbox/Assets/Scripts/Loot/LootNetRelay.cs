using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Server relay for client loot pickups (P2): the server equips the
/// collector and despawns the loot so all clients converge.
/// </summary>
public sealed class LootNetRelay : NetworkBehaviour
{
    [ServerRpc(RequireOwnership = false)]
    public void RequestChestServerRpc(ServerRpcParams rpcParams = default)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;
        var loot = GetComponent<LootDrop>();
        if (loot == null || !loot.isChest || loot.collected) return;
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
        foreach (var kv in nm.SpawnManager.SpawnedObjects)
        {
            var nb = kv.Value;
            if (!nb.IsPlayerObject || nb.OwnerClientId != sender) continue;
            if (loot != null && !loot.collected && loot.item != null)
            {
                loot.collected = true;
                LootDrop.CollectInto(nb.gameObject, loot.item);
            }
        }
        var no = GetComponent<NetworkObject>();
        if (no != null && no.IsSpawned) no.Despawn();
        else Destroy(gameObject);
    }
}
