using UnityEngine;

/// <summary>
/// First-Kindled-victory claim (cinderreach-kindled-finale S2): the bossdrop
/// stays untouched; the first Kilnmother kill on Kindled additionally stores
/// a claim for one class-compatible Rare weapon with Kindled affixes. A full
/// bag keeps the claim open. Payout and claim consumption are saved together;
/// a save failure rolls the payout back. After a reload inside the completion
/// window the claim is offered again. Older completed runs get no retroactive
/// claim (the grant fires only on the kill event).
/// </summary>
public static class KindledFinaleReward
{
    /// <summary>Test seam: fail the next persist to verify rollback.</summary>
    public static bool ForceSaveFailure;
    /// <summary>Last offer line for the session screen (test seam).</summary>
    public static string LastOfferLine = "";

    public static bool IsKindledRun() => DifficultyProfile.Current == DifficultyProfile.Kindled;
    public static bool HasPending => RunProfile.KindledRewardPending;

    /// <summary>Kill hook: grant the first-time claim, then try to pay it out.</summary>
    public static bool OnKilnmotherKill(GameObject hero, string path = null)
    {
        if (!IsKindledRun()) return false;
        if (RunProfile.KindledRewardGranted) return false;
        if (!RunProfile.KindledRewardPending)
        {
            RunProfile.KindledRewardPending = true;
            RunProfile.KindledRewardClass = RunProfile.ClassId;
        }
        return TryDeliver(hero, path);
    }

    /// <summary>Pay out a pending claim when the bag has room. False when still pending.</summary>
    public static bool TryDeliver(GameObject hero, string path = null)
    {
        if (!RunProfile.KindledRewardPending) return false;
        string classId = string.IsNullOrEmpty(RunProfile.KindledRewardClass)
            ? RunProfile.ClassId : RunProfile.KindledRewardClass;
        var weapon = ItemDatabase.CreateKindledFinaleWeapon(classId);
        var inv = hero != null ? hero.GetComponent<Inventory>() : null;
        if (inv == null || inv.FreeSlots <= 0)
        {
            PersistPending(path);
            LastOfferLine = Loc.T("reward.kindled.bagfull");
            return false;
        }
        inv.Add(weapon);
        if (!PersistGrant(hero, path))
        {
            // Rollback: payout and consumption saved together or not at all.
            for (int i = inv.Count - 1; i >= 0; i--)
                if (ReferenceEquals(inv.Get(i), weapon)) { inv.RemoveAt(i); break; }
            LastOfferLine = Loc.T("reward.kindled.savefail");
            return false;
        }
        LastOfferLine = Loc.F("reward.kindled.line", LootDrop.PrettyName(weapon.baseType), weapon.GetAffix("Damage"));
        return true;
    }

    /// <summary>Re-offer after a reload inside the completion window.</summary>
    public static void OfferOnSessionScreen(GameObject hero, string path = null)
    {
        if (!RunProfile.KindledRewardPending) return;
        try { TryDeliver(hero, path); }
        catch (System.Exception e) { Debug.LogWarning($"[KindledFinale] Offer failed: {e.Message}"); }
    }

    static void PersistPending(string path)
    {
        try
        {
            if (ForceSaveFailure) throw new System.IO.IOException("forced save failure");
            if (path != null) SaveService.WriteSave(SaveService.CaptureState(LastHeroOrNull(), 1), path);
        }
        catch (System.Exception e) { Debug.LogWarning($"[KindledFinale] Pending persist failed: {e.Message}"); }
    }

    static GameObject lastHeroHint;
    static GameObject LastHeroOrNull()
    {
        if (lastHeroHint != null) return lastHeroHint;
        var found = GameObject.Find(DiabloNetworkPlayer.HeroName);
        return found;
    }

    static bool PersistGrant(GameObject hero, string path)
    {
        lastHeroHint = hero;
        bool wasPending = RunProfile.KindledRewardPending;
        string wasClass = RunProfile.KindledRewardClass;
        bool wasGranted = RunProfile.KindledRewardGranted;
        RunProfile.KindledRewardPending = false;
        RunProfile.KindledRewardGranted = true;
        try
        {
            if (ForceSaveFailure) throw new System.IO.IOException("forced save failure");
            if (path != null) SaveService.WriteSave(SaveService.CaptureState(hero, 1), path);
            else SaveService.SaveNow(hero);
            return true;
        }
        catch (System.Exception e)
        {
            RunProfile.KindledRewardPending = wasPending;
            RunProfile.KindledRewardClass = wasClass;
            RunProfile.KindledRewardGranted = wasGranted;
            Debug.LogWarning($"[KindledFinale] Grant persist failed, rolled back: {e.Message}");
            return false;
        }
        finally { lastHeroHint = null; }
    }

    public static void ResetRun()
    {
        ForceSaveFailure = false;
        LastOfferLine = "";
    }
}
