using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Gold pickup (S2 economy). Spawned beside the item orb by
/// <see cref="LootDrop.SpawnLoot"/> on an independent per-tier roll
/// (constants in <see cref="DroptableDatabase"/>). Local-only like the
/// legacy pickup orbs. S4 co-op: the host owns every pile (spawned with a
/// <see cref="LootNetRelay"/>, like item loot); clients request pickup per
/// ServerRpc and are credited per ClientRpc, so a shared pile pays exactly
/// once. Solo/offline keeps the direct local path.
/// </summary>
public sealed class GoldDrop : MonoBehaviour
{
    public int amount;
    public bool collected;

    /// <summary>Piles spawned this run (test seam; item orbs use their own counters).</summary>
    public static int PilesDropped;

    /// <summary>Independent gold roll for the tier (Normal 70 %, Elite/Chest 100 %).</summary>
    public static bool RollDrops(LootTier tier, System.Random rng)
    {
        DroptableDatabase.GetGoldTable(tier, out int chance, out _, out _);
        return rng.Next(100) < chance;
    }

    /// <summary>Gold amount for the tier (inclusive bounds).</summary>
    public static int RollAmount(LootTier tier, System.Random rng)
    {
        DroptableDatabase.GetGoldTable(tier, out _, out int min, out int max);
        return min + rng.Next(max - min + 1);
    }

    /// <summary>Roll the tier table and spawn a pile; null when nothing drops.</summary>
    public static GameObject SpawnGold(Vector3 pos, LootTier tier, System.Random rng)
    {
        if (!RollDrops(tier, rng)) return null;
        return SpawnPile(pos, DifficultyProfile.Current.ScaleGold(RollAmount(tier, rng)));
    }

    /// <summary>Spawn a fixed-amount pile (deterministic tests + callers).</summary>
    public static GameObject SpawnPile(Vector3 pos, int amount)
    {
        var nmSpawn = NetworkManager.Singleton;
        var prefab = Resources.Load<GameObject>("NetworkLootDrop");
        if (amount > 0 && nmSpawn != null && nmSpawn.IsListening && nmSpawn.IsServer
            && prefab != null && nmSpawn.NetworkConfig.Prefabs.Contains(prefab))
        {
            var netPile = Instantiate(prefab, pos, Quaternion.identity);
            netPile.GetComponent<LootNetRelay>().SetGold(amount);
            netPile.GetComponent<NetworkObject>().Spawn();
            netPile.name = $"GoldPile_{++PilesDropped}";
            Debug.Log($"[Gold] Dropped {amount} gold.");
            return netPile;
        }
        var pile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        PilesDropped++;
        pile.name = $"GoldPile_{PilesDropped}";
        pile.transform.position = pos;
        pile.transform.localScale = new Vector3(0.35f, 0.22f, 0.35f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            color = new Color(1f, 0.8f, 0.2f)
        };
        pile.GetComponent<Renderer>().sharedMaterial = mat;
        pile.GetComponent<BoxCollider>().isTrigger = true;
        var drop = pile.AddComponent<GoldDrop>();
        drop.amount = Mathf.Max(0, amount);
        Debug.Log($"[Gold] Dropped {drop.amount} gold.");
        return pile;
    }

    internal static void ShowNetworkPile(GameObject pickup, int amount)
    {
        var gold = pickup.AddComponent<GoldDrop>();
        gold.amount = amount;
        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.transform.SetParent(pickup.transform, false);
        visual.transform.localScale = new Vector3(0.35f, 0.22f, 0.35f);
        Destroy(visual.GetComponent<Collider>());
        var color = new MaterialPropertyBlock();
        color.SetColor("_BaseColor", new Color(1f, 0.8f, 0.2f));
        visual.GetComponent<Renderer>().SetPropertyBlock(color);
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected || other == null || other.gameObject == null) return;
        var go = other.gameObject;
        if (go.name != "Player" && !go.CompareTag("Player")) return;
        CollectLocal(go);
    }

    /// <summary>Host/solo apply: purse credit on this process, then converge.</summary>
    public void CollectLocal(GameObject go)
    {
        if (collected || go == null) return;
        var relay = GetComponent<LootNetRelay>();
        if (relay != null && relay.IsSpawned)
        {
            if (go.GetComponent<DiabloNetworkPlayer>() is { IsOwner: true })
                relay.RequestGoldServerRpc();
            return;
        }
        collected = true;
        var wallet = Wallet.EnsureWallet(go);
        var eq = go.GetComponent<Equipment>();
        if (wallet != null) wallet.Add(Equipment.ApplyGoldFind(amount, eq != null ? eq.GoldFindPct() : 0f));
        DiabloAudio.PlayLoot();
        var netObj = GetComponent<NetworkObject>();
        var nm = NetworkManager.Singleton;
        if (netObj != null && netObj.IsSpawned && nm != null && nm.IsServer) netObj.Despawn();
        else Destroy(gameObject);
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        PilesDropped = 0;
    }
}
