using System.Threading.Tasks;
using NUnit.Framework;

/// <summary>
/// S6 relay join-code (cinderreach-coop-session): code validation, flag
/// parsing and the offline error path. Live hosting/joining runs against the
/// real UGS project and is proven by the 2-process relay pair run (logs),
/// not by unit tests.
/// </summary>
public class RelayJoinTests
{
    [Test]
    public void NormalizeCode_AcceptsRelayCodes()
    {
        Assert.AreEqual("BJR8KP", RelayJoin.NormalizeCode("BJR8KP"));
        Assert.AreEqual("BJR8KP", RelayJoin.NormalizeCode("  bjr8kp  "));
    }

    [Test]
    public void NormalizeCode_RejectsGarbage()
    {
        Assert.IsNull(RelayJoin.NormalizeCode(null));
        Assert.IsNull(RelayJoin.NormalizeCode(""));
        Assert.IsNull(RelayJoin.NormalizeCode("   "));
        Assert.IsNull(RelayJoin.NormalizeCode("abc"));
    }

    [Test]
    public void JoinCodeFlag_SelectsClientWithCode()
    {
        var args = JoinArgs.Parse(new[] { "-join-code", "BJR8KP" });
        Assert.AreEqual(JoinArgs.NetMode.Client, args.Mode);
        Assert.AreEqual("BJR8KP", args.JoinCode);
        Assert.IsFalse(args.RelayHost);
    }

    [Test]
    public void JoinCodeFlag_WithoutCode_WarnsAndStaysClient()
    {
        var args = JoinArgs.Parse(new[] { "-join-code" });
        Assert.AreEqual(JoinArgs.NetMode.Client, args.Mode);
        Assert.IsNull(args.JoinCode);
        Assert.IsNotNull(args.Warning);
    }

    [Test]
    public void HostRelayFlag_SelectsHost()
    {
        var args = JoinArgs.Parse(new[] { "-host-relay" });
        Assert.AreEqual(JoinArgs.NetMode.Host, args.Mode);
        Assert.IsTrue(args.RelayHost);
        Assert.IsNull(args.JoinCode);
    }

    [Test]
    public void DirectFlags_StayRelayFree()
    {
        var args = JoinArgs.Parse(new[] { "-host", "-port", "7777" });
        Assert.IsNull(args.JoinCode);
        Assert.IsFalse(args.RelayHost);
        var join = JoinArgs.Parse(new[] { "-join", "127.0.0.1:7777" });
        Assert.IsNull(join.JoinCode);
        Assert.IsFalse(join.RelayHost);
    }

    [Test]
    public async Task JoinWithBadCode_FailsWithoutTouchingUgs()
    {
        Assert.IsFalse(await RelayJoin.JoinWithCodeAsync(null));
        Assert.IsFalse(await RelayJoin.JoinWithCodeAsync(""));
        Assert.IsFalse(await RelayJoin.JoinWithCodeAsync("ab"));
    }
}
