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
        window = GUI.Window(GetInstanceID(), window, id => DrawWindow(shrine), "Risk Shrine");
    }

    void DrawWindow(RiskShrine shrine)
    {
        var progress = RunProfile.RiskShrine;
        GUILayout.Label($"The fen offers a bargain. ({progress.State})");
        if (shrine == null)
        {
            GUILayout.Label("No shrine in this place.");
        }
        else if (progress.State == RiskShrineState.Unchosen)
        {
            GUILayout.Label("Heal to full now, or face the empowered elite for a rare.");
            if (GUILayout.Button("Accept healing (full HP)")) shrine.ChooseHeal(gameObject);
            if (GUILayout.Button($"Face {RiskShrine.EliteName} ({RiskShrine.EliteHP} HP)")) shrine.ChooseChallenge(gameObject);
        }
        else if (progress.State == RiskShrineState.RewardAvailable)
        {
            GUILayout.Label("The elite is slain. Your reward waits.");
            if (GUILayout.Button("Take the rare")) shrine.ClaimReward(gameObject);
        }
        else
        {
            GUILayout.Label(progress.State == RiskShrineState.Healed
                ? "You walk on, whole."
                : progress.State == RiskShrineState.ChallengeActive
                    ? $"Slay {RiskShrine.EliteName}."
                    : "The bargain is settled.");
        }
        if (GUILayout.Button("Step back")) SetOpen(false);
        GUI.DragWindow();
    }

    static RiskShrine FindShrine()
    {
        var go = GameObject.Find(RiskShrine.ShrineObjectName);
        return go != null ? go.GetComponent<RiskShrine>() : null;
    }
}
