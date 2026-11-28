using NUnit.Framework;

/// <summary>
/// One automation rule for start screens, the F7 panel, display settings and the
/// UGS profile. Argument vectors mirror scripts/run-coop-pair.sh.
/// </summary>
public class AutomationArgsTests
{
    static readonly string[] Human = { "cinderreach" };
    static readonly string[] CoopFollower =
        { "cinderreach", "-screen-width", "960", "-join", "127.0.0.1:7777", "-autoplay-god", "-coop-autoplay", "client" };
    static readonly string[] CoopHost =
        { "cinderreach", "-host", "-port", "7777", "-autoplay", "-coop-autoplay", "host", "-save-file", "coop-host.json" };
    static readonly string[] Server = { "cinderreach", "-batchmode", "-nographics", "-server", "-port", "7777" };

    [Test]
    public void HumanLaunch_GetsStartScreensAndOwnDisplay()
    {
        Assert.IsFalse(AutomationArgs.IsDriven(Human));
        Assert.IsFalse(MainMenu.SkippedFor(Human));
        Assert.IsTrue(DiabloSettings.BootMayTouchDisplay(Human));
        PlayerAccount.ProfileFor(Human, 42, out bool throwaway);
        Assert.IsFalse(throwaway);
    }

    [Test]
    public void CoopFollower_WithoutAutoplay_SkipsTitleMenu()
    {
        Assert.IsTrue(AutomationArgs.IsDriven(CoopFollower));
        Assert.IsTrue(MainMenu.SkippedFor(CoopFollower), "follower client must not open the legacy title menu");
        Assert.IsFalse(ClassSelectUI.ShouldShow(CoopFollower));
        Assert.IsFalse(DiabloSettings.BootMayTouchDisplay(CoopFollower));
        PlayerAccount.ProfileFor(CoopFollower, 42, out bool throwaway);
        Assert.IsTrue(throwaway);
    }

    [Test]
    public void CoopHostAndServer_AreAutomation()
    {
        Assert.IsTrue(MainMenu.SkippedFor(CoopHost));
        Assert.IsTrue(MainMenu.SkippedFor(Server));
        Assert.IsFalse(ClassSelectUI.ShouldShow(Server));
        Assert.IsFalse(DiabloSettings.BootMayTouchDisplay(Server));
        PlayerAccount.ProfileFor(Server, 7, out bool throwaway);
        Assert.IsTrue(throwaway);
    }

    [Test]
    public void ShotcheckAndBatch_SkipStartScreen()
    {
        Assert.IsTrue(MainMenu.SkippedFor(new[] { "x", DiabloShotCheck.Flag }));
        Assert.IsTrue(MainMenu.SkippedFor(new[] { "x", AutomationArgs.BatchMode }));
        Assert.IsTrue(MainMenu.SkippedFor(new[] { "x", AutomationArgs.LegacyNetClient }));
        Assert.IsFalse(AutomationArgs.Any(null, AutomationArgs.BatchMode));
    }
}
