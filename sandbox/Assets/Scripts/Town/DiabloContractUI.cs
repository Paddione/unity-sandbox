using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hero-side contract board UI (zone-contracts S2). Built at runtime like
/// <see cref="DiabloVendorUI"/>: shows the two offers (target, zone, reward)
/// before the choice, accepts one, and claims the finished contract at the
/// board. Refreshes only after an action completes — never mid-flight — so
/// the display can never show gold that was not persisted.
/// </summary>
public sealed class DiabloContractUI : MonoBehaviour
{
    public bool IsOpen { get; private set; }
    public readonly List<string> Lines = new List<string>();

    ContractOffer[] offers = new ContractOffer[0];

    /// <summary>Offers currently on display (seed + unlock state).</summary>
    public ContractOffer[] ShownOffers => offers;

    public void SetOpen(bool open)
    {
        IsOpen = open;
        if (open) Refresh();
    }

    /// <summary>Accept a shown offer. False when the run already holds a contract.</summary>
    public bool AcceptOffer(int index)
    {
        if (index < 0 || index >= offers.Length) return false;
        if (!ContractProgress.TryAccept(offers[index])) return false;
        ContractObjective.SpawnFor(ContractProgress.ActiveOffer);
        Refresh();
        return true;
    }

    /// <summary>Claim at the board; the display updates only after the claim returns.</summary>
    public bool Claim(ContractBoard board, GameObject hero, string savePath = null)
    {
        if (board == null) return false;
        bool ok = board.TryClaim(hero, savePath);
        Refresh();
        return ok;
    }

    void Refresh()
    {
        Lines.Clear();
        if (ContractProgress.HasContract && ContractProgress.ActiveOffer != null)
        {
            var o = ContractProgress.ActiveOffer;
            Lines.Add($"{o.targetName} — zone {o.zone} — {o.gold} gold — {ContractProgress.Status}");
        }
        else
        {
            offers = ContractBoard.CurrentOffers();
            foreach (var o in offers)
                Lines.Add($"{o.targetName} — zone {o.zone} — {o.gold} gold");
        }
    }
}
