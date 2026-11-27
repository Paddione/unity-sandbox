using UnityEngine;

/// <summary>
/// Gold purse on the hero (Ashen Fen, S2 economy). Local-only per client like
/// <see cref="Inventory"/>: the server never syncs gold, and the save file is
/// per machine (see <see cref="SaveService"/>). Add/TrySpend are pure;
/// pickups arrive via <see cref="GoldDrop"/> (lazy <see cref="EnsureWallet"/>)
/// the same way items arrive via <see cref="LootDrop.CollectInto"/>.
/// </summary>
public sealed class Wallet : MonoBehaviour
{
    public int Gold { get; private set; }

    /// <summary>Run-scoped gold earned (test/journal seam, like XpGranted).</summary>
    public static int GoldEarned;

    /// <summary>Get the hero's wallet, adding one when the hero has none.</summary>
    public static Wallet EnsureWallet(GameObject hero)
    {
        if (hero == null) return null;
        var wallet = hero.GetComponent<Wallet>();
        if (wallet == null) wallet = hero.AddComponent<Wallet>();
        return wallet;
    }

    /// <summary>Add gold. Non-positive amounts are ignored.</summary>
    public void Add(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
        GoldEarned += amount;
        Debug.Log($"[Gold] +{amount} (purse={Gold}).");
    }

    /// <summary>Spend gold. False when the purse is short (nothing deducted).</summary>
    public bool TrySpend(int price)
    {
        if (price <= 0) return true;
        if (Gold < price) return false;
        Gold -= price;
        Debug.Log($"[Gold] -{price} (purse={Gold}).");
        return true;
    }

    /// <summary>Direct set for save-load (see <see cref="SaveService"/>).</summary>
    public void SetGold(int amount)
    {
        Gold = Mathf.Max(0, amount);
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        GoldEarned = 0;
    }
}
