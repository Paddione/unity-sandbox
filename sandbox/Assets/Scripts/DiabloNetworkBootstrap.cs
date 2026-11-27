using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// P2 co-op bootstrap for DiabloZone: NetworkManager + UnityTransport
/// (127.0.0.1:7777, Q4 pattern) + NetworkPlayerDiablo prefab from Resources.
/// Runtime-safe (Res.Get over Addressables, no Editor API); called by the zone builder
/// and safe to call when netcode already exists in the scene.
/// </summary>
public static class DiabloNetworkBootstrap
{
    public static void RegisterLootPrefab(NetworkManager manager)
    {
        var loot = Res.Get<GameObject>("NetworkLootDrop");
        if (loot == null) return;
        var prefabs = manager.NetworkConfig.Prefabs;
        if (prefabs.Contains(loot)) return;
        foreach (var list in prefabs.NetworkPrefabsLists)
            foreach (var entry in list.PrefabList)
                if (entry.Prefab == loot) return;
        prefabs.Add(new NetworkPrefab { Prefab = loot });
    }

    public static void EnsureZoneNetcode()
    {
        var nmGo = GameObject.Find("NetworkManager");
        NetworkManager nm = nmGo != null ? nmGo.GetComponent<NetworkManager>() : null;
        if (nm == null)
        {
            nmGo = new GameObject("NetworkManager");
            nm = nmGo.AddComponent<NetworkManager>();
            var transport = nmGo.AddComponent<UnityTransport>();
            transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
            nm.NetworkConfig = new NetworkConfig { NetworkTransport = transport };
            nmGo.AddComponent<MultiplayerAutoStarter>();
            nmGo.AddComponent<ConnectionStatusLogger>();
        }
        RegisterLootPrefab(nm);
        var prefab = Res.Get<GameObject>("NetworkPlayerDiablo");
        if (prefab != null)
        {
            if (nm.NetworkConfig.PlayerPrefab != prefab)
            {
                nm.NetworkConfig.PlayerPrefab = prefab;
                nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = prefab });
            }
            Debug.Log("[Net] DiabloZone netcode ready (player prefab assigned).");
        }
        else
        {
            Debug.LogWarning("[Net] NetworkPlayerDiablo prefab not found in Resources.");
        }
    }
}
