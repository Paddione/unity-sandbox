using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Group story playback (cinderreach-group-quest-map): a hero's NPC dialog runs
/// on every other player's screen with its voice (<see cref="StoryDialogShare"/>),
/// and the host's boss and story barks reach every client
/// (<see cref="StoryBarks.Relay"/>). The host validates and relays; listeners
/// only show, they never book.
/// </summary>
public sealed partial class DiabloNetworkPlayer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void WireStoryRelays()
    {
        StoryDialogShare.Send = (op, dialog, npc, lines, index) =>
        {
            var local = LocalRelay();
            if (local != null) local.ShareDialog(op, dialog, npc, lines, index);
        };
        StoryBarks.Relay = id =>
        {
            var host = HostRelay();
            if (host != null) host.BroadcastBark(id);
        };
    }

    /// <summary>The spawned player object this process owns, null solo or on a dedicated server.</summary>
    static DiabloNetworkPlayer LocalRelay()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening || nm.SpawnManager == null) return null;
        var obj = nm.SpawnManager.GetLocalPlayerObject();
        var relay = obj != null ? obj.GetComponent<DiabloNetworkPlayer>() : null;
        return relay != null && relay.IsSpawned && relay.IsOwner ? relay : null;
    }

    /// <summary>Owner: tell the group about this hero's dialog window.</summary>
    public void ShareDialog(SharedDialogOp op, string dialogId, string npc, string lines, int index)
    {
        if (!IsSpawned || !IsOwner) return;
        if (IsServer) RelayDialog(op, dialogId, npc, lines, index);
        else SubmitDialogServerRpc((int)op, dialogId ?? "", npc ?? "", lines ?? "", index);
    }

    [ServerRpc]
    void SubmitDialogServerRpc(int op, string dialogId, string npc, string lines, int index)
    {
        if (op < 0 || op > (int)SharedDialogOp.Close) return;
        RelayDialog((SharedDialogOp)op, dialogId, npc, lines, index);
    }

    void RelayDialog(SharedDialogOp op, string dialogId, string npc, string lines, int index)
    {
        if (!StoryDialogShare.Valid(op, dialogId, lines, index)) return;
        if (npc != null && npc.Length > 24) npc = npc.Substring(0, 24);
        DialogClientRpc((int)op, dialogId ?? "", npc ?? "", lines ?? "", index);
    }

    [ClientRpc]
    void DialogClientRpc(int op, string dialogId, string npc, string lines, int index)
    {
        if (IsOwner) return; // the speaker runs the window itself
        StoryDialogShare.Receive(OwnerClientId, ShownName, (SharedDialogOp)op, dialogId, npc, lines, index);
    }

    /// <summary>Server: every client queues bark <paramref name="id"/>.</summary>
    public void BroadcastBark(string id)
    {
        if (!IsServer || !IsSpawned || StoryBarks.Get(id) == null) return;
        BarkClientRpc(id);
    }

    [ClientRpc]
    void BarkClientRpc(string id)
    {
        if (IsServer) return; // the host said it already
        StoryBarks.SayRemote(id);
    }
}
