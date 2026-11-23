using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Player names over the network (cinderreach-accounts S1). The owner reports
/// its <see cref="PlayerAccount.DisplayName"/>; the server cleans it
/// (<see cref="PlayerNames.Sanitize"/>) and publishes it to everyone. Join and
/// leave lines name the hero instead of a client id.
/// </summary>
public sealed partial class DiabloNetworkPlayer
{
    /// <summary>Cleaned display name, written by the server only.</summary>
    public readonly NetworkVariable<FixedString32Bytes> playerName = new NetworkVariable<FixedString32Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Name to show over this hero (never empty).</summary>
    public string ShownName => PlayerNames.Display(playerName.Value.ToString());

    /// <summary>Live network heroes (nameplates read this instead of scanning the scene).</summary>
    public static readonly System.Collections.Generic.List<DiabloNetworkPlayer> Live =
        new System.Collections.Generic.List<DiabloNetworkPlayer>();

    bool announcedJoin;

    void SpawnNames()
    {
        if (!Live.Contains(this)) Live.Add(this);
        playerName.OnValueChanged += OnNameChanged;
        if (IsOwner)
        {
            PlayerAccount.NameChanged += ReportName;
            ReportName();
        }
        else if (playerName.Value.Length > 0)
        {
            OnNameChanged(default, playerName.Value);
        }
    }

    void DespawnNames()
    {
        Live.Remove(this);
        playerName.OnValueChanged -= OnNameChanged;
        if (IsOwner) PlayerAccount.NameChanged -= ReportName;
        if (!IsOwner && announcedJoin)
        {
            Debug.Log($"[Net] {ShownName} left the party.");
            DiabloJuice.ShowText(transform.position + Vector3.up * 2.4f, $"{ShownName} left", DiabloPalette.Parchment, 0.9f);
        }
    }

    public override void OnNetworkDespawn()
    {
        DespawnNames();
        base.OnNetworkDespawn();
    }

    void ReportName()
    {
        if (!IsSpawned || !IsOwner) return;
        SubmitNameServerRpc(PlayerAccount.DisplayName);
    }

    [ServerRpc]
    void SubmitNameServerRpc(string raw)
    {
        playerName.Value = new FixedString32Bytes(ServerCleanName(raw, OwnerClientId));
    }

    /// <summary>Server rule (pure): cleaned name, else "Wanderer &lt;client id&gt;" so heroes stay apart.</summary>
    public static string ServerCleanName(string raw, ulong clientId) =>
        PlayerNames.Sanitize(raw) ?? $"{PlayerNames.Unnamed}_{clientId}";

    void OnNameChanged(FixedString32Bytes before, FixedString32Bytes now)
    {
        string shown = PlayerNames.Display(now.ToString());
        Debug.Log($"[Net] client {OwnerClientId} is {shown}{(IsOwner ? " (you)" : "")}.");
        if (IsOwner || announcedJoin || now.Length == 0) return;
        announcedJoin = true;
        Debug.Log($"[Net] {shown} joined the party.");
        DiabloJuice.ShowText(transform.position + Vector3.up * 2.4f, $"{shown} joined", DiabloPalette.GoldBright, 0.9f);
    }
}
