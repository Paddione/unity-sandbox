/// <summary>
/// One source for "is this process an automation run" checks. Start screens,
/// the F7 join panel, display settings and the UGS profile used to keep their
/// own flag lists, and they drifted apart (the co-op follower client
/// <c>-coop-autoplay client</c> without <c>-autoplay</c> still opened the legacy
/// title menu). Call sites keep their own extra conditions on top.
/// </summary>
public static class AutomationArgs
{
    public const string BatchMode = "-batchmode";
    public const string NoGraphics = "-nographics";
    public const string LegacyNetClient = "--net-client";

    /// <summary>Flags of the in-game drivers (solo autoplay, co-op autoplay, shotcheck).</summary>
    public static readonly string[] Drivers = { SessionAutoplay.Flag, CoopAutoplay.Flag, DiabloShotCheck.Flag };

    /// <summary>Explicit network launches that boot straight into gameplay or a headless server.</summary>
    public static readonly string[] NetLaunch = { LegacyNetClient, DedicatedServer.LegacyFlag, DedicatedServer.Flag };

    /// <summary>True when an in-game automation driver runs this process.</summary>
    public static bool IsDriven(string[] args) => Any(args, Drivers);

    /// <summary>True when no start screen may open: a driver, batch mode or an explicit net launch.</summary>
    public static bool SkipsStartScreen(string[] args) =>
        IsDriven(args) || Any(args, BatchMode) || Any(args, NetLaunch);

    /// <summary>True when any argument equals one of <paramref name="flags"/>.</summary>
    public static bool Any(string[] args, params string[] flags)
    {
        if (args == null) return false;
        foreach (var a in args)
            foreach (var f in flags)
                if (a == f) return true;
        return false;
    }
}
