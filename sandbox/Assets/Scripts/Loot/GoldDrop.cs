using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Kill gold (cinderreach-loot-click-filter D1): no ground piles any more.
/// <see cref="LootDrop.SpawnLoot"/> rolls the per-tier gold table and
/// credits the amount straight to the hero who landed the kill (fallback:
/// the nearest hero), Gold Find included. Host/solo only: in co-op the
/// server credits through <see cref="DiabloNetworkPlayer.GrantPickupGold"/>,
/// which replicates the purse to the owner.
/// </summary>
public static class GoldDrop
{
    /// <summary>Gold awards this run (test seam).</summary>
    public static int Awards;
    /// <summary>Raw gold rolled this run before Gold Find (test seam).</summary>
    public static int AwardedRaw;

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

    /// <summary>Roll the tier table and credit the killer; returns the raw amount (0 = no gold).</summary>
    public static int AwardKillGold(Vector3 pos, LootTier tier, System.Random rng, Transform killer = null)
    {
        if (!RollDrops(tier, rng)) return 0;
        int amount = DifficultyProfile.Current.ScaleGold(RollAmount(tier, rng));
        var hero = killer != null ? HeroRoot(killer) : null;
        if (hero == null) hero = NearestHero(pos);
        return Credit(hero, amount, pos) ? amount : 0;
    }

    /// <summary>Credit a fixed amount to <paramref name="hero"/> (Gold Find applies).</summary>
    public static bool Credit(GameObject hero, int amount, Vector3 at)
    {
        if (hero == null || amount <= 0) return false;
        var player = hero.GetComponent<DiabloNetworkPlayer>();
        var nm = NetworkManager.Singleton;
        if (player != null && player.IsSpawned && nm != null && nm.IsListening)
        {
            if (!player.GrantPickupGold(amount)) return false;
        }
        else
        {
            var eq = hero.GetComponent<Equipment>();
            Wallet.EnsureWallet(hero).Add(Equipment.ApplyGoldFind(amount, eq != null ? eq.GoldFindPct() : 0f));
            DiabloAudio.PlayGold();
        }
        Awards++;
        AwardedRaw += amount;
        DiabloJuice.ShowText(at + Vector3.up * 2.0f, Loc.F("hud.gold.gain", amount), DiabloPalette.TrimGold);
        Debug.Log($"[Gold] Credited {amount} gold to {hero.name}.");
        return true;
    }

    static GameObject HeroRoot(Transform t)
    {
        var p = t.GetComponentInParent<DiabloNetworkPlayer>();
        if (p != null) return p.gameObject;
        var w = t.GetComponentInParent<Equipment>();
        return w != null ? w.gameObject : null;
    }

    /// <summary>Nearest hero to <paramref name="pos"/> (network players, else the local "Player").</summary>
    public static GameObject NearestHero(Vector3 pos)
    {
        var nm = NetworkManager.Singleton;
        GameObject best = null;
        float bestSq = float.MaxValue;
        if (nm != null && nm.IsListening && nm.SpawnManager != null)
        {
            foreach (var kv in nm.SpawnManager.SpawnedObjects)
            {
                var nb = kv.Value;
                if (nb == null || !nb.IsPlayerObject) continue;
                float sq = (nb.transform.position - pos).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = nb.gameObject; }
            }
        }
        return best != null ? best : GameObject.Find(DiabloNetworkPlayer.HeroName);
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        Awards = 0;
        AwardedRaw = 0;
    }
}
