using Unity.Networking.Transport;
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>cinderreach-dedicated-server S1: server flag, leader choice, quit rule, result roles.</summary>
public class DedicatedServerTests
{
    [Test]
    public void ServerFlag_AndLegacyFlag_StartAServerOnTheGivenPort()
    {
        var a = JoinArgs.Parse(new[] { "x", "-server", "-port", "7790" });
        Assert.AreEqual(JoinArgs.NetMode.Server, a.Mode);
        Assert.AreEqual(7790, a.Port);
        Assert.AreEqual(JoinArgs.NetMode.Server, JoinArgs.Parse(new[] { "x", "--net-server" }).Mode);
        Assert.IsTrue(DedicatedServer.Requested(new[] { "x", "-server" }));
        Assert.IsFalse(DedicatedServer.Requested(new[] { "x", "-host" }));
        Assert.IsFalse(DedicatedServer.Requested(null));
    }

    [Test]
    public void Leader_IsLowestLivingClient_FallsBackToLowestWhenAllDown()
    {
        var ids = new List<ulong> { 5, 2, 9 };
        Assert.AreEqual(1, DedicatedServer.LeaderIndex(ids, new List<bool> { true, true, true }));
        Assert.AreEqual(0, DedicatedServer.LeaderIndex(ids, new List<bool> { true, false, true }));
        Assert.AreEqual(1, DedicatedServer.LeaderIndex(ids, new List<bool> { false, false, false }));
        Assert.AreEqual(-1, DedicatedServer.LeaderIndex(new List<ulong>(), new List<bool>()));
    }

    [Test]
    public void Quit_OnlyAfterASessionStayedEmptyForTheGrace()
    {
        float g = DedicatedServer.EmptyGraceSeconds;
        Assert.IsFalse(DedicatedServer.ShouldQuit(false, 0, 0f, 1000f), "fresh server waits for players forever");
        Assert.IsFalse(DedicatedServer.ShouldQuit(true, 1, 0f, 1000f), "players still connected");
        Assert.IsFalse(DedicatedServer.ShouldQuit(true, 0, 100f, 100f + g - 0.1f), "inside the reconnect grace");
        Assert.IsTrue(DedicatedServer.ShouldQuit(true, 0, 100f, 100f + g));
        Assert.IsFalse(DedicatedServer.ShouldQuit(true, 0, -1f, 1000f), "empty time not started");
    }

    [Test]
    public void ResultRoles_LeadAndFollow_HaveTheirOwnPhases()
    {
        Assert.AreEqual("lead", CoopAutoplay.RoleFor(new[] { "x", "-coop-autoplay", "lead" }));
        Assert.AreEqual("follow", CoopAutoplay.RoleFor(new[] { "x", "-coop-autoplay", "follow" }));
        var lead = new AutoplayResult { role = AutoplayResult.RoleLead };
        foreach (var p in new[] { "town", "zone1", "gate" }) lead.Reach(p);
        Assert.IsFalse(lead.AllPhasesReached());
        lead.Reach("zone2");
        Assert.IsTrue(lead.AllPhasesReached());
        var follow = new AutoplayResult { role = AutoplayResult.RoleFollow };
        foreach (var p in new[] { "town", "zone1", "zone2" }) follow.Reach(p);
        Assert.IsTrue(follow.AllPhasesReached());
    }

    [Test]
    public void JoinServerAndRelayServerFlags_SetTheirModes()
    {
        var c = JoinArgs.Parse(new[] { "x", "-join-server" });
        Assert.AreEqual(JoinArgs.NetMode.Client, c.Mode);
        Assert.IsTrue(c.JoinServer);
        var s = JoinArgs.Parse(new[] { "x", "-server", "-server-relay" });
        Assert.AreEqual(JoinArgs.NetMode.Server, s.Mode);
        Assert.IsTrue(s.RelayServer);
        Assert.IsFalse(JoinArgs.Parse(new[] { "x" }).JoinServer);
    }

    [Test]
    public void ServerName_DefaultsAndOverrides_QueryFiltersByIt()
    {
        Assert.AreEqual("Cinderreach Server", ServerDirectory.NameFor(new[] { "x" }));
        Assert.AreEqual("Cinderreach Server test-1", ServerDirectory.NameFor(new[] { "x", "-server-name", "Cinderreach Server test-1" }));
        var q = ServerDirectory.Query("Cinderreach Server test-1");
        Assert.AreEqual(1, q.Filters.Count);
        Assert.AreEqual("Cinderreach Server test-1", q.Filters[0].Value);
        var o = ServerDirectory.LobbyOptions("ABC123", "0.1.0");
        Assert.AreEqual(false, o.IsPrivate);
        Assert.AreEqual("ABC123", o.Data[ServerDirectory.JoinCodeKey].Value);
    }

    [Test]
    public void PanelStatus_ShowsServerSearch()
    {
        var args = new JoinArgs(JoinArgs.NetMode.Client, null, 0, joinServer: true);
        Assert.AreEqual("Offline (no NetworkManager)", JoinPanel.StatusText(null, args));
    }

    [Test]
    public void Reachable_NeedsListening_AndAFreshHeartbeatInRelayMode()
    {
        Assert.IsFalse(DedicatedServerHost.Reachable(false, false, -1f, 10f));
        Assert.IsTrue(DedicatedServerHost.Reachable(true, false, -1f, 10f), "direct mode: listening is enough");
        Assert.IsFalse(DedicatedServerHost.Reachable(true, true, -1f, 10f), "relay mode before publish");
        Assert.IsTrue(DedicatedServerHost.Reachable(true, true, 100f, 100f + DedicatedServerHost.StaleHeartbeatSeconds));
        Assert.IsFalse(DedicatedServerHost.Reachable(true, true, 100f, 101f + DedicatedServerHost.StaleHeartbeatSeconds));
    }

    [Test]
    public void PacketCeiling_StaysUnderTheOverlayMtu()
    {
        // devmesh pods: Flannel WireGuard MTU 1420 minus IPv4 (20) and UDP (8) headers.
        Assert.LessOrEqual(CompactPacketDriver.MaxPacketBytes, 1420 - 28);
        var settings = new Unity.Networking.Transport.NetworkSettings(Unity.Collections.Allocator.Temp);
        CompactPacketDriver.ApplyPacketLimit(ref settings);
        Assert.AreEqual(CompactPacketDriver.MaxPacketBytes, settings.GetNetworkConfigParameters().maxMessageSize);
        settings.Dispose();
    }
}

