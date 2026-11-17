using UnityEngine;

/// <summary>
/// Gold pickup (S2 economy). Spawned beside the item orb by
/// <see cref="LootDrop.SpawnLoot"/> on an independent per-tier roll
/// (constants in <see cref="DroptableDatabase"/>). Local-only like the
/// legacy pickup orbs: no NetworkObject, each client collects its own.
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
        return SpawnPile(pos, RollAmount(tier, rng));
    }

    /// <summary>Spawn a fixed-amount pile (deterministic tests + callers).</summary>
    public static GameObject SpawnPile(Vector3 pos, int amount)
    {
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

    void OnTriggerEnter(Collider other)
    {
        if (collected || other == null || other.gameObject == null) return;
        var go = other.gameObject;
        if (go.name != "Player" && !go.CompareTag("Player")) return;
        collected = true;
        var wallet = Wallet.EnsureWallet(go);
        if (wallet != null) wallet.Add(amount);
        DiabloAudio.PlayLoot();
        Destroy(gameObject);
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        PilesDropped = 0;
    }
}
