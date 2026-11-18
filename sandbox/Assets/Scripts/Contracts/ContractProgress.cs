using UnityEngine;

/// <summary>
/// Run-scoped contract state (zone-contracts S1). Holds the accepted offer
/// frozen at accept time with status <see cref="ContractStatus"/>: Active,
/// Complete or Claimed. One contract per run — after a claim the board is
/// done for this run. Pure statics so EditMode tests need no scene; the save
/// snapshot round-trips through <see cref="SaveGame.contract"/> (save v5).
/// </summary>
public static class ContractProgress
{
    public static bool HasContract { get; private set; }
    public static ContractOffer ActiveOffer { get; private set; }
    public static ContractStatus Status { get; private set; }

    /// <summary>Accept a board offer. False for null offers or when a contract already exists this run.</summary>
    public static bool TryAccept(ContractOffer offer)
    {
        if (offer == null || string.IsNullOrEmpty(offer.id)) return false;
        if (HasContract) return false;
        HasContract = true;
        ActiveOffer = offer.Clone();
        Status = ContractStatus.Active;
        Debug.Log($"[Contract] Accepted {offer.id} ({offer.targetName}, {offer.gold} gold).");
        return true;
    }

    /// <summary>Complete the active contract. False unless active and the id matches.</summary>
    public static bool TryComplete(string objectiveId)
    {
        if (!HasContract || Status != ContractStatus.Active) return false;
        if (string.IsNullOrEmpty(objectiveId) || ActiveOffer == null) return false;
        if (objectiveId != ActiveOffer.id) return false;
        Status = ContractStatus.Complete;
        Debug.Log($"[Contract] Objective {objectiveId} complete — return to the board.");
        return true;
    }

    /// <summary>Mark the reward paid (board claim path only).</summary>
    public static bool MarkClaimed()
    {
        if (!HasContract || Status != ContractStatus.Complete) return false;
        Status = ContractStatus.Claimed;
        return true;
    }

    /// <summary>Restore a pre-claim state (claim write-failure rollback).</summary>
    public static void Restore(bool hasContract, ContractOffer offer, ContractStatus status)
    {
        HasContract = hasContract;
        ActiveOffer = offer != null ? offer.Clone() : null;
        Status = status;
    }

    /// <summary>Fresh-run defaults (New Game, headless tests).</summary>
    public static void Reset()
    {
        HasContract = false;
        ActiveOffer = null;
        Status = ContractStatus.Active;
    }

    public static void ToSnapshot(ContractSnapshot snap)
    {
        if (snap == null) return;
        snap.hasContract = HasContract;
        snap.offerId = ActiveOffer != null ? ActiveOffer.id : string.Empty;
        snap.type = ActiveOffer != null ? (int)ActiveOffer.type : 0;
        snap.zone = ActiveOffer != null ? ActiveOffer.zone : ActProgress.Town;
        snap.position = ActiveOffer != null ? ActiveOffer.position : Vector3.zero;
        snap.gold = ActiveOffer != null ? ActiveOffer.gold : 0;
        snap.targetName = ActiveOffer != null ? ActiveOffer.targetName : string.Empty;
        snap.status = (int)Status;
    }

    public static void FromSnapshot(ContractSnapshot snap)
    {
        Reset();
        if (snap == null || !snap.hasContract || string.IsNullOrEmpty(snap.offerId)) return;
        HasContract = true;
        ActiveOffer = new ContractOffer
        {
            id = snap.offerId,
            type = (ContractType)snap.type,
            zone = snap.zone,
            position = snap.position,
            gold = Mathf.Max(0, snap.gold),
            targetName = snap.targetName ?? string.Empty,
        };
        Status = snap.status >= 0 && snap.status <= (int)ContractStatus.Claimed
            ? (ContractStatus)snap.status
            : ContractStatus.Active;
    }
}
