using System;
using UnityEngine;

/// <summary>
/// Lantern Hold contract board (zone-contracts S2). The town interaction:
/// click inside <see cref="InteractRadius"/> opens the hero's
/// <see cref="DiabloContractUI"/>, like <see cref="Vendor"/> and
/// <see cref="StashChest"/>. Exactly one objective exists per active contract
/// (spawned by <see cref="ContractObjective.SpawnFor"/>). The claim pays the
/// frozen gold and flips to Claimed in the same save; on a write failure both
/// live values roll back so the reward stays available.
/// </summary>
public sealed class ContractBoard : MonoBehaviour, IUsable
{
    /// <summary>Trade reach around the board (click opens only inside).</summary>
    public const float InteractRadius = 3.5f;

    /// <summary>Scene object name (pinned by VerifyDiabloZone).</summary>
    public const string BoardObjectName = "ContractBoard";

    /// <summary>Injectable save writer (tests force failures through here).</summary>
    public static Func<SaveGame, string, bool> WriteHook;

    /// <summary>Pure claim-reach rule (EditMode-pinnable, no scene).</summary>
    public static bool CanClaim(Vector3 heroPos, Vector3 boardPos, ContractStatus status, bool hasContract)
    {
        if (!hasContract || status != ContractStatus.Complete) return false;
        var d = heroPos - boardPos;
        d.y = 0f;
        return d.sqrMagnitude <= InteractRadius * InteractRadius;
    }

    /// <summary>Current board offers for this run (seed + unlock state).</summary>
    public static ContractOffer[] CurrentOffers() =>
        ContractCatalog.Offers(RunProfile.RunSeed, RunProfile.Act);

    /// <summary>
    /// Pay the frozen reward and persist gold + Claimed in one save. False
    /// when out of reach, not complete, or the write failed (live gold and
    /// status are restored, the UI must refresh only after this returns).
    /// </summary>
    public bool TryClaim(GameObject hero, string savePath = null)
    {
        if (hero == null) return false;
        if (!ContractProgress.HasContract || ContractProgress.Status != ContractStatus.Complete) return false;
        if (!CanClaim(hero.transform.position, transform.position, ContractProgress.Status, true)) return false;
        var wallet = Wallet.EnsureWallet(hero);
        if (wallet == null) return false;
        var offer = ContractProgress.ActiveOffer;
        int goldBefore = wallet.Gold;
        wallet.Add(offer.gold);
        ContractProgress.MarkClaimed();
        try
        {
            var save = SaveService.CaptureState(hero);
            string path = savePath ?? SaveService.DefaultPath;
            bool ok = WriteHook != null ? WriteHook(save, path) : WriteDefault(save, path);
            if (!ok) throw new InvalidOperationException("contract claim write rejected");
            Debug.Log($"[Contract] Claimed {offer.id} (+{offer.gold} gold).");
            return true;
        }
        catch (Exception e)
        {
            wallet.SetGold(goldBefore);
            ContractProgress.Restore(true, offer, ContractStatus.Complete);
            Debug.LogWarning($"[Contract] Claim write failed, reward kept: {e.Message}");
            return false;
        }
    }

    static bool WriteDefault(SaveGame save, string path)
    {
        SaveService.WriteSave(save, path);
        return true;
    }

    // Perf: Camera.main sucht pro Aufruf per Tag durch die Szene (~fps im
    // Autoplay gekostet). Einmal cachen; Kameras leben über die Szene.
    static Camera s_main;

    void Update()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null) return;
        if (!mouse.leftButton.wasPressedThisFrame) return;
        if (s_main == null) s_main = Camera.main;
        if (s_main == null) return;
        var hero = GameObject.Find("Player"); // nur auf Klick-Frames, ok
        if (hero == null) return;
        var heroUI = hero.GetComponent<DiabloContractUI>();
        if (heroUI != null && heroUI.IsOpen) return; // UI eats its own clicks
        var ray = s_main.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out var hit, 200f)) return;
        if (hit.collider == null || hit.collider.gameObject != gameObject) return;
        if (!Vendor.CanInteract(hero.transform.position, transform.position, InteractRadius)) return;
        Use(hero);
    }

    public float UseRadius => InteractRadius;
    public string UseLabel => Loc.T("use.contract");
    void OnEnable() => Usables.Register(this);
    void OnDisable() => Usables.Unregister(this);

    public void Use(GameObject hero)
    {
        if (hero == null) return;
        var heroUI = hero.GetComponent<DiabloContractUI>();
        if (heroUI == null) heroUI = hero.AddComponent<DiabloContractUI>();
        heroUI.SetOpen(true);
        Debug.Log("[Contract] Board opened.");
    }
}
