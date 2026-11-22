using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>coop-session S1: start parameters, per-process save file, client result contract, co-op window.</summary>
public class JoinArgsTests
{
    [Test]
    public void NoFlags_IsSoloHostOnDefaultPort()
    {
        var a = JoinArgs.Parse(new[] { "cinderreach" });
        Assert.AreEqual(JoinArgs.NetMode.Host, a.Mode);
        Assert.AreEqual("127.0.0.1", a.Address);
        Assert.AreEqual(7777, a.Port);
        Assert.IsNull(a.Warning);
    }

    [Test]
    public void Host_WithPort()
    {
        var a = JoinArgs.Parse(new[] { "x", "-host", "-port", "7790" });
        Assert.AreEqual(JoinArgs.NetMode.Host, a.Mode);
        Assert.AreEqual(7790, a.Port);
    }

    [Test]
    public void Join_IpAndPort_AndIpOnly()
    {
        var a = JoinArgs.Parse(new[] { "x", "-join", "192.168.1.20:7790" });
        Assert.AreEqual(JoinArgs.NetMode.Client, a.Mode);
        Assert.AreEqual("192.168.1.20", a.Address);
        Assert.AreEqual(7790, a.Port);

        var b = JoinArgs.Parse(new[] { "x", "-join", "gpu-metal", "-port", "7800" });
        Assert.AreEqual("gpu-metal", b.Address);
        Assert.AreEqual(7800, b.Port, "-port applies when -join has none");

        var c = JoinArgs.Parse(new[] { "x", "-join", "10.0.0.5" });
        Assert.AreEqual(7777, c.Port);
    }

    [Test]
    public void Join_BadPortOrMissingAddress_FallsBackWithWarning()
    {
        var a = JoinArgs.Parse(new[] { "x", "-join", "10.0.0.5:abc" });
        Assert.AreEqual(JoinArgs.NetMode.Client, a.Mode);
        Assert.AreEqual("10.0.0.5", a.Address);
        Assert.AreEqual(7777, a.Port);
        StringAssert.Contains("invalid port", a.Warning);

        var b = JoinArgs.Parse(new[] { "x", "-join" });
        Assert.AreEqual(JoinArgs.NetMode.Client, b.Mode);
        Assert.AreEqual("127.0.0.1", b.Address);
        Assert.IsNotNull(b.Warning);
    }

    [Test]
    public void LegacyP2Flags_AndClone_StillWork()
    {
        var a = JoinArgs.Parse(new[] { "x", "--net-client", "--net-address", "100.64.0.2", "--net-port", "7801" });
        Assert.AreEqual(JoinArgs.NetMode.Client, a.Mode);
        Assert.AreEqual("100.64.0.2", a.Address);
        Assert.AreEqual(7801, a.Port);
        Assert.AreEqual(JoinArgs.NetMode.Server, JoinArgs.Parse(new[] { "x", "--net-server" }).Mode);
        Assert.AreEqual(JoinArgs.NetMode.Client, JoinArgs.Parse(new[] { "x" }, isClone: true).Mode);
    }

    [Test]
    public void SaveFile_FlagPicksFileNameOnly()
    {
        Assert.AreEqual(SaveService.FileName, SaveService.FileNameFor(new[] { "x" }));
        Assert.AreEqual("client-a.json", SaveService.FileNameFor(new[] { "x", "-save-file", "client-a.json" }));
        Assert.AreEqual("evil.json", SaveService.FileNameFor(new[] { "x", "-save-file", "../../evil.json" }),
            "no path escape out of persistentDataPath");
    }

    [Test]
    public void CoopRole_AndClientResultContract()
    {
        Assert.AreEqual("client", CoopAutoplay.RoleFor(new[] { "x", "-coop-autoplay", "client" }));
        Assert.AreEqual("host", CoopAutoplay.RoleFor(new[] { "x", "-coop-autoplay", "HOST" }));
        Assert.IsNull(CoopAutoplay.RoleFor(new[] { "x", "-coop-autoplay", "spectator" }));
        Assert.IsNull(CoopAutoplay.RoleFor(new[] { "x" }));

        var client = new AutoplayResult { role = AutoplayResult.RoleClient, exceptions = 0 };
        client.Reach(AutoplayResult.PhaseTown);
        Assert.AreEqual(1, client.ComputeExit(), "client needs zone1 too");
        client.Reach(AutoplayResult.PhaseZone1);
        Assert.AreEqual(1, client.ComputeExit(), "act-two-opening: the client books the Glasswright kill too");
        client.Reach(AutoplayResult.PhaseBoss3);
        Assert.AreEqual(0, client.ComputeExit());
        StringAssert.Contains("\"role\": \"client\"", client.ToJson());

        var host = new AutoplayResult { role = AutoplayResult.RoleHost };
        host.Reach(AutoplayResult.PhaseTown);
        host.Reach(AutoplayResult.PhaseZone1);
        Assert.AreEqual(1, host.ComputeExit(), "host plays the full route");
        Assert.AreEqual("solo", new AutoplayResult().role);
    }

    [Test]
    public void ClassSelect_SkipsCoopPairRuns()
    {
        Assert.IsFalse(ClassSelectUI.ShouldShow(new[] { "x", "-coop-autoplay", "client" }));
    }

    [Test]
    public void JoinPanel_BuildsWithHdScalerAndDefaultAddress()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var panel = JoinPanel.Open();
        Assert.IsTrue(panel.IsOpen);
        Assert.AreEqual("127.0.0.1:7777", panel.AddressField.text);
        var scaler = panel.GetComponentInChildren<CanvasScaler>();
        Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);
        Assert.AreEqual(0.5f, scaler.matchWidthOrHeight);
        foreach (var name in new[] { "Button_Host", "Button_Join", "Button_Close" })
            Assert.IsNotNull(panel.transform.Find("JoinCanvas/Panel/" + name), name);
        StringAssert.Contains("Offline", panel.Status.text, "no NetworkManager in an empty scene");
        Assert.AreSame(panel, JoinPanel.Open(), "one window");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
}
