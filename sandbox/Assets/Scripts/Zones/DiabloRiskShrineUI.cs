using UnityEngine;

/// <summary>
/// Risk-shrine choice window (cinderreach-risk-shrines S2). Opens on shrine
/// click while the hero is in reach; offers heal-to-max or the named elite,
/// then the waiting rare for pickup. Open window locks click-movement like the
/// vendor/inventory guards; closing restores it.
/// </summary>
public sealed class DiabloRiskShrineUI : MonoBehaviour
{
    public bool IsOpen { get; private set; }

    Rect window = new Rect(20, 120, 300, 170);

    /// <summary>Open/close the shrine window (click guard + autosave on close).</summary>
    public void SetOpen(bool open)
    {
        if (IsOpen == open) return;
        IsOpen = open;
        DiabloClickController.ExternalDrive = open;
        if (!open && Diablo.IsLocalAuthority()) SaveService.SaveNow(gameObject);
    }

    void OnGUI()
    {
        if (!IsOpen) return;
        var shrine = FindShrine();
        window = GUI.Window(GetInstanceID(), window, id => DrawWindow(shrine), Loc.T("name.shrine.risk"));
    }

    void DrawWindow(RiskShrine shrine)
    {
        var progress = RunProfile.RiskShrine;
        GUILayout.Label(Loc.T("shrine.risk.offer"));
        if (shrine == null)
        {
            GUILayout.Label(Loc.T("shrine.risk.none"));
        }
        else if (progress.State == RiskShrineState.Unchosen)
        {
            GUILayout.Label(Loc.T("shrine.risk.choice"));
            if (GUILayout.Button(Loc.T("shrine.risk.heal"))) shrine.ChooseHeal(gameObject);
            if (GUILayout.Button(Loc.F("shrine.risk.face", Loc.Display(RiskShrine.EliteName), RiskShrine.EliteHP))) shrine.ChooseChallenge(gameObject);
        }
        else if (progress.State == RiskShrineState.RewardAvailable)
        {
            GUILayout.Label(Loc.T("shrine.risk.slain"));
            if (GUILayout.Button(Loc.T("shrine.risk.take"))) shrine.ClaimReward(gameObject);
        }
        else
        {
            GUILayout.Label(progress.State == RiskShrineState.Healed
                ? Loc.T("shrine.risk.healed")
                : progress.State == RiskShrineState.ChallengeActive
                    ? Loc.F("shrine.risk.slay", Loc.Display(RiskShrine.EliteName))
                    : Loc.T("shrine.risk.settled"));
        }
        if (GUILayout.Button(Loc.T("shrine.risk.back"))) SetOpen(false);
        GUI.DragWindow();
    }

    static RiskShrine FindShrine()
    {
        var go = GameObject.Find(RiskShrine.ShrineObjectName);
        return go != null ? go.GetComponent<RiskShrine>() : null;
    }
}
