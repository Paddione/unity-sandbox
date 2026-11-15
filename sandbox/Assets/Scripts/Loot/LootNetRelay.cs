using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Server relay for client loot pickups (P2): the server equips the
/// collector and despawns the loot so all clients converge.
/// </summary>
public sealed class LootNetRelay : NetworkBehaviour
{
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
            var equip = nb.GetComponent<Equipment>();
            if (loot != null && !loot.collected && equip != null && loot.item != null)
            {
                loot.collected = true;
                equip.Equip(loot.item);
            }
        }
        var no = GetComponent<NetworkObject>();
        if (no != null && no.IsSpawned) no.Despawn();
        else Destroy(gameObject);
    }
}
