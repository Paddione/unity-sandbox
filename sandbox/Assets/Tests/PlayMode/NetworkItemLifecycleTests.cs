using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class NetworkItemLifecycleTests
{
    readonly List<GameObject> managers = new();

    [UnityTearDown]
    public IEnumerator Shutdown()
    {
        foreach (var go in managers)
        {
            if (go == null) continue;
            go.GetComponent<NetworkManager>()?.Shutdown();
            Object.Destroy(go);
        }
        managers.Clear();
        yield return null;
    }

    NetworkManager NewManager(string label, ushort port)
    {
        var go = new GameObject(label);
        managers.Add(go);
        var manager = go.AddComponent<NetworkManager>();
        var transport = go.AddComponent<UnityTransport>();
        transport.SetConnectionData("127.0.0.1", port);
        manager.NetworkConfig = new NetworkConfig
        {
            NetworkTransport = transport,
            EnableSceneManagement = false,
        };
        DiabloNetworkBootstrap.RegisterLootPrefab(manager);
        return manager;
    }

    [UnityTest]
    public IEnumerator LateJoinReceivesExactRolledDrop()
    {
        ushort port;
        using (var socket = new System.Net.Sockets.UdpClient(0))
            port = (ushort)((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;

        var host = NewManager("item test host", port);
        Assert.That(host.StartHost(), Is.True);
        var rolled = ItemDatabase.RollUnique(ItemDatabase.CreateUnique(ItemDatabase.LancerWardId), 10, 2);
        var pickup = LootDrop.SpawnItem(Vector3.forward * 4f, rolled, true);
        Assert.That(pickup, Is.Not.Null);
        var networkId = pickup.GetComponent<NetworkObject>().NetworkObjectId;

        var client = NewManager("item test client", port);
        Assert.That(client.StartClient(), Is.True);
        float deadline = Time.realtimeSinceStartup + 10f;
        while ((client.SpawnManager == null || !client.SpawnManager.SpawnedObjects.ContainsKey(networkId))
            && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.That(client.SpawnManager.SpawnedObjects.TryGetValue(networkId, out var replica), Is.True);
        var received = replica.GetComponent<LootDrop>().item;
        Assert.That(received.instanceId, Is.EqualTo(rolled.instanceId));
        Assert.That(JsonUtility.ToJson(received), Is.EqualTo(JsonUtility.ToJson(rolled)));
        Assert.That(replica.transform.position, Is.EqualTo(pickup.transform.position));
    }

    [UnityTest]
    public IEnumerator FullPickupAndStaleSaleLeaveStateIntact()
    {
        ushort port;
        using (var socket = new System.Net.Sockets.UdpClient(0))
            port = (ushort)((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;

        var host = NewManager("item ownership host", port);
        var playerPrefab = Res.Get<GameObject>("NetworkPlayerDiablo");
        host.NetworkConfig.PlayerPrefab = playerPrefab;
        Assert.That(host.StartHost(), Is.True);
        yield return null;
        var hero = host.SpawnManager.GetLocalPlayerObject();
        Assert.That(hero, Is.Not.Null);
        var player = hero.GetComponent<DiabloNetworkPlayer>();
        var inventory = hero.GetComponent<Inventory>();
        var wallet = hero.GetComponent<Wallet>();
        if (wallet == null) wallet = Wallet.EnsureWallet(hero.gameObject);
        var vendor = new GameObject(Vendor.VendorObjectName);
        vendor.transform.position = hero.transform.position;
        managers.Add(vendor);

        var armor = ItemDatabase.CreateUnique(ItemDatabase.ShardbackAegisId);
        Assert.That(inventory.Add(armor), Is.True);
        Assert.That(player.RequestItemAction(DiabloNetworkPlayer.ItemAction.Equip, armor), Is.True);
        Assert.That(player.visualArmor.Value.ToString(), Is.EqualTo(armor.uniqueId));
        Assert.That(player.RequestItemAction(DiabloNetworkPlayer.ItemAction.Unequip, armor), Is.True);
        Assert.That(player.visualArmor.Value.ToString(), Is.Empty);
        Assert.That(inventory.Get(inventory.Count - 1).instanceId, Is.EqualTo(armor.instanceId));
        Assert.That(inventory.RemoveAt(inventory.Count - 1), Is.True);

        while (inventory.FreeSlots > 0) inventory.Add(ItemDatabase.CreateStarterSword());
        var item = ItemDatabase.RollUnique(ItemDatabase.CreateUnique(ItemDatabase.LancerWardId), 10, 2);
        var pile = LootDrop.SpawnItem(hero.transform.position + Vector3.right * 1.5f, item, true);
        var relay = pile.GetComponent<LootNetRelay>();
        relay.RequestPickupServerRpc();
        Assert.That(pile.GetComponent<NetworkObject>().IsSpawned, Is.True);
        Assert.That(inventory.Count, Is.EqualTo(Inventory.Capacity));

        inventory.RemoveAt(0);
        relay.RequestPickupServerRpc();
        var bagged = inventory.Get(Inventory.Capacity - 1);
        Assert.That(bagged?.instanceId, Is.EqualTo(item.instanceId));
        Assert.That(inventory.Count, Is.EqualTo(Inventory.Capacity));
        Assert.That(pile.GetComponent<NetworkObject>().IsSpawned, Is.False);

        Assert.That(player.RequestItemAction(DiabloNetworkPlayer.ItemAction.Discard, bagged), Is.True);
        Assert.That(inventory.FreeSlots, Is.EqualTo(1));
        LootDrop discarded = null;
        foreach (var netObject in host.SpawnManager.SpawnedObjects.Values)
        {
            var drop = netObject.GetComponent<LootDrop>();
            if (drop != null && drop.item?.instanceId == item.instanceId) discarded = drop;
        }
        Assert.That(discarded, Is.Not.Null);
        Assert.That(JsonUtility.ToJson(discarded.item), Is.EqualTo(JsonUtility.ToJson(item)));
        discarded.GetComponent<LootNetRelay>().RequestPickupServerRpc();

        int before = wallet.Gold;
        Assert.That(player.RequestItemAction(DiabloNetworkPlayer.ItemAction.Sell, item), Is.True);
        Assert.That(player.RequestItemAction(DiabloNetworkPlayer.ItemAction.Sell, item), Is.False);
        Assert.That(wallet.Gold, Is.EqualTo(before + VendorStock.SellPrice(item)));
    }

    [Test]
    public void ImportedEquipmentRejectsDuplicateAndKeepsClassSpecificBases()
    {
        var ids = new HashSet<string>();
        var starter = ItemDatabase.CreateStarterLantern();
        Assert.That(NetworkItemOwnership.Validate(starter, ids), Is.SameAs(starter));
        Assert.That(NetworkItemOwnership.Validate(JsonUtility.FromJson<ItemInstance>(JsonUtility.ToJson(starter)), ids), Is.Null);
        var unknown = ItemDatabase.CreateStarterLantern();
        unknown.baseType = "unknown base";
        Assert.That(NetworkItemOwnership.Validate(unknown, ids), Is.Null);
    }

    [UnityTest]
    public IEnumerator ClientFlaskDrink_IsGrantedByTheHostAndHealsTheOwner()
    {
        ushort port;
        using (var socket = new System.Net.Sockets.UdpClient(0))
            port = (ushort)((System.Net.IPEndPoint)socket.Client.LocalEndPoint).Port;
        var playerPrefab = Res.Get<GameObject>("NetworkPlayerDiablo");
        var host = NewManager("flask host", port);
        host.NetworkConfig.PlayerPrefab = playerPrefab;
        Assert.That(host.StartHost(), Is.True);
        yield return null;
        var client = NewManager("flask client", port);
        client.NetworkConfig.PlayerPrefab = playerPrefab;
        Assert.That(client.StartClient(), Is.True);
        float deadline = Time.realtimeSinceStartup + 10f;
        NetworkObject owned = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            owned = client.SpawnManager?.GetLocalPlayerObject();
            if (owned != null && client.IsConnectedClient) break;
            yield return null;
        }
        Assert.That(owned, Is.Not.Null, "Client player spawned.");
        NetworkObject mirror = null;
        foreach (var kv in host.SpawnManager.SpawnedObjects)
            if (kv.Value.IsPlayerObject && kv.Value.OwnerClientId == client.LocalClientId) mirror = kv.Value;
        Assert.That(mirror, Is.Not.Null, "Host holds the client's mirror.");
        // Out in the field: the Lantern Hold would refill at once.
        owned.transform.position = new Vector3(200f, 0f, 0f);
        deadline = Time.realtimeSinceStartup + 5f;
        while (Time.realtimeSinceStartup < deadline && HealingFlask.AtRefillPoint(mirror.transform.position))
        {
            owned.transform.position = new Vector3(200f, 0f, 0f);
            yield return null;
        }
        Assert.That(HealingFlask.AtRefillPoint(mirror.transform.position), Is.False, "The mirror left the hold.");

        var health = owned.GetComponent<PlayerHealth>();
        health.TakeDamage(10f);
        float hurt = health.CurrentHP;
        var ownerFlask = HealingFlask.For(owned.gameObject);
        var hostFlask = HealingFlask.For(mirror.gameObject);
        Assert.That(ownerFlask.TryDrink(), Is.True, "The client asks the host.");
        deadline = Time.realtimeSinceStartup + 5f;
        while (ownerFlask.Drinks == 0 && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(hostFlask.Granted, Is.EqualTo(1), "The host spent the charge.");
        Assert.That(ownerFlask.Drinks, Is.EqualTo(1), "The owner started the heal on the host's word.");
        Assert.That(ownerFlask.Charges, Is.EqualTo(0), "The owner mirrors the empty flask.");
        Assert.That(ownerFlask.TryDrink(), Is.False, "An empty flask does not even ask.");
        yield return new WaitForSeconds(HealingFlask.TickSeconds + 0.2f);
        Assert.That(health.CurrentHP, Is.GreaterThan(hurt), "Ticks heal the owner's own HP.");
        Assert.That(hostFlask.DrinkAuthoritative(), Is.False, "The host refuses a second drink within the cooldown.");
    }
}
