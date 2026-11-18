using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contract type: a named elite hunt or a relic recovery.
/// </summary>
public enum ContractType { Hunt, Recovery }

/// <summary>Acceptance state of the frozen run contract.</summary>
public enum ContractStatus { Active, Complete, Claimed }

/// <summary>
/// One board offer: stable id, type, zone, validated target position,
/// gold reward and display target name. Pure data, JsonUtility-friendly.
/// </summary>
[System.Serializable]
public sealed class ContractOffer
{
    public string id = string.Empty;
    public ContractType type;
    public int zone = ActProgress.Town;
    public Vector3 position;
    public int gold;
    public string targetName = string.Empty;

    public ContractOffer Clone() => new ContractOffer
    {
        id = id,
        type = type,
        zone = zone,
        position = position,
        gold = gold,
        targetName = targetName,
    };
}

/// <summary>
/// Board offer catalog (zone-contracts S1). <c>Offers(seed, act)</c> returns
/// exactly two offers — one <see cref="ContractType.Hunt"/>, one
/// <see cref="ContractType.Recovery"/> — filtered by unlocked zones, with the
/// Ashen Fen as fallback. Uses its own deterministic seed channel
/// (<c>seed ^ Salt</c> drawn in a fixed order), so
/// <see cref="LayoutSeeder.ForSeed"/> keeps its existing mapping.
/// </summary>
public static class ContractCatalog
{
    const int Salt = unchecked((int)0xC07A4C7);

    /// <summary>Scene object name of the relic prop (pinned by tests).</summary>
    public const string RelicObjectName = "ContractRelic";

    struct Candidate
    {
        public string key;
        public ContractType type;
        public int zone;
        public string target;
        public int baseGold;
    }

    static readonly Candidate[] Hunts =
    {
        new Candidate { key = "ash-elite", type = ContractType.Hunt, zone = ActProgress.Town, target = "Ash Elite", baseGold = 120 },
        new Candidate { key = "fen-brute", type = ContractType.Hunt, zone = ActProgress.Town, target = "Fen Brute", baseGold = 150 },
        new Candidate { key = "bone-warden", type = ContractType.Hunt, zone = ActProgress.Ossuary, target = "Bone Warden", baseGold = 200 },
        new Candidate { key = "kiln-elite", type = ContractType.Hunt, zone = ActProgress.Kiln, target = "Kiln Elite", baseGold = 260 },
    };

    static readonly Candidate[] Recoveries =
    {
        new Candidate { key = "fen-cache", type = ContractType.Recovery, zone = ActProgress.Town, target = "Fen Cache", baseGold = 90 },
        new Candidate { key = "crypt-relic", type = ContractType.Recovery, zone = ActProgress.Town, target = "Crypt Relic", baseGold = 110 },
        new Candidate { key = "sunken-relic", type = ContractType.Recovery, zone = ActProgress.Ossuary, target = "Sunken Relic", baseGold = 160 },
        new Candidate { key = "kiln-relic", type = ContractType.Recovery, zone = ActProgress.Kiln, target = "Kiln Relic", baseGold = 220 },
    };

    static readonly Vector3 FenFallback = new Vector3(-12f, 0.05f, 0f);

    /// <summary>Zone anchor: Fen = origin, Ossuary/Kiln = their +200/+400 m X annexes.</summary>
    public static Vector3 ZoneCenter(int zone)
    {
        if (zone == ActProgress.Ossuary) return KilnLayout.OssuaryOffset;
        if (zone == ActProgress.Kiln) return KilnLayout.KilnOffset;
        return Vector3.zero;
    }

    /// <summary>Reachability guard: near the ground inside the zone's radius.</summary>
    public static bool IsValidTarget(Vector3 pos, int zone)
    {
        if (pos.y < -1f || pos.y > 3f) return false;
        var flat = pos - ZoneCenter(zone);
        flat.y = 0f;
        return flat.sqrMagnitude <= 40f * 40f;
    }

    static Vector3 HuntAnchor(string key, System.Random rng)
    {
        switch (key)
        {
            case "ash-elite": return LayoutSeeder.EliteSpots[rng.Next(LayoutSeeder.VariantCount)];
            case "fen-brute": return LayoutSeeder.BruteSpots[rng.Next(LayoutSeeder.VariantCount)];
            case "bone-warden":
                return KilnLayout.OssuaryOffset + new Vector3(0f, 0.05f, -6f + rng.Next(LayoutSeeder.VariantCount));
            default:
                return KilnLayout.EliteAnchor(rng.Next(KilnLayout.Variants), rng.Next(KilnLayout.EliteCount));
        }
    }

    static Vector3 RecoveryAnchor(string key, System.Random rng)
    {
        switch (key)
        {
            case "fen-cache": return new Vector3(-12f + rng.Next(3), 0.05f, 0f);
            case "crypt-relic": return new Vector3(rng.Next(-4, 5), 0.05f, -6f + rng.Next(3));
            case "sunken-relic":
                return KilnLayout.OssuaryOffset + new Vector3(-5f + rng.Next(3), 0.05f, 10f);
            default:
                return KilnLayout.KilnOffset + new Vector3(3.5f, 0.05f, 9.5f);
        }
    }

    static ContractOffer Build(Candidate c, int seed, System.Random rng)
    {
        var pos = c.type == ContractType.Hunt ? HuntAnchor(c.key, rng) : RecoveryAnchor(c.key, rng);
        if (!IsValidTarget(pos, c.zone)) pos = FenFallback;
        return new ContractOffer
        {
            id = $"contract-{seed}-{c.key}",
            type = c.type,
            zone = ValidOrFen(c.zone),
            position = ValidOrFenPos(pos, c.zone),
            gold = c.baseGold + rng.Next(0, 21),
            targetName = c.target,
        };
    }

    static int ValidOrFen(int zone) =>
        zone >= ActProgress.Town && zone <= ActProgress.Kiln ? zone : ActProgress.Town;

    static Vector3 ValidOrFenPos(Vector3 pos, int zone) =>
        IsValidTarget(pos, zone) ? pos : FenFallback;

    /// <summary>Two deterministic offers for this seed and unlock state.</summary>
    public static ContractOffer[] Offers(int seed, ActProgress act)
    {
        if (act == null) act = new ActProgress();
        var rng = new System.Random(seed ^ Salt);
        var hunts = new List<Candidate>();
        var recs = new List<Candidate>();
        foreach (var c in Hunts) if (act.IsUnlocked(c.zone)) hunts.Add(c);
        foreach (var c in Recoveries) if (act.IsUnlocked(c.zone)) recs.Add(c);
        if (hunts.Count == 0) hunts.Add(Hunts[0]);
        if (hunts.Count == 0) hunts.Add(Hunts[1]);
        if (recs.Count == 0) recs.Add(Recoveries[0]);
        if (recs.Count == 0) recs.Add(Recoveries[1]);
        var hunt = hunts[rng.Next(hunts.Count)];
        var rec = recs[rng.Next(recs.Count)];
        return new[] { Build(hunt, seed, rng), Build(rec, seed, rng) };
    }
}
