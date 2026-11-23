using NUnit.Framework;

/// <summary>cinderreach-accounts S1: name cleaning, auth profiles, server name rule, plate colours.</summary>
public class PlayerNamesTests
{
    [TestCase("Patrick", "Patrick")]
    [TestCase("ClassicSpeakingVibraphone#11008", "ClassicSpeakingV")] // UGS suffix dropped, cut to 16
    [TestCase("  Dark   Knight  ", "Dark_Knight")]
    [TestCase("Ash<b>en</b>", "Ashbenb")]
    [TestCase("x_-9", "x_-9")]
    [TestCase("Zoë", "Zoë")]
    public void Sanitize_KeepsSafeCharacters(string raw, string expected)
    {
        Assert.AreEqual(expected, PlayerNames.Sanitize(raw));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("ab")]
    [TestCase("#1234")]
    [TestCase("  !!  ")]
    public void Sanitize_RejectsTooShort(string raw)
    {
        Assert.IsNull(PlayerNames.Sanitize(raw));
        Assert.AreEqual(PlayerNames.Unnamed, PlayerNames.Display(raw));
    }

    [Test]
    public void Sanitize_NeverExceedsMaxLength()
    {
        var s = PlayerNames.Sanitize(new string('a', 64));
        Assert.AreEqual(PlayerNames.MaxLength, s.Length);
    }

    [Test]
    public void Profile_NormalStartKeepsTheMainIdentity()
    {
        Assert.AreEqual(PlayerAccount.DefaultProfile, PlayerAccount.ProfileFor(new[] { "Cinderreach.exe" }, 42, out bool t));
        Assert.IsFalse(t);
        Assert.AreEqual("laptop2", PlayerAccount.ProfileFor(new[] { "x", "-profile", "laptop2" }, 42, out t));
        Assert.IsFalse(t, "an explicit profile keeps its cache");
    }

    [Test]
    public void Profile_AutomationAndServerRunsAreThrowaway()
    {
        foreach (var flag in new[] { "-autoplay", "-coop-autoplay", "-shotcheck", "-server", "-server-relay", "-save-file" })
        {
            Assert.AreEqual("auto77", PlayerAccount.ProfileFor(new[] { "x", flag, "v" }, 77, out bool t), flag);
            Assert.IsTrue(t, flag);
        }
        Assert.AreEqual("p1", PlayerAccount.ProfileFor(new[] { "-autoplay", "-profile", "p1" }, 77, out bool keep));
        Assert.IsFalse(keep, "an explicit profile wins over automation");
    }

    [Test]
    public void Profile_InvalidNamesFallBack()
    {
        Assert.IsFalse(PlayerAccount.IsValidProfile("has space"));
        Assert.IsFalse(PlayerAccount.IsValidProfile(new string('a', 31)));
        Assert.IsTrue(PlayerAccount.IsValidProfile("auto_12-x"));
        Assert.AreEqual(PlayerAccount.DefaultProfile, PlayerAccount.ProfileFor(new[] { "-profile", "bad name" }, 1, out _));
    }

    [Test]
    public void ServerName_CleansOrFallsBackPerClient()
    {
        Assert.AreEqual("Host", DiabloNetworkPlayer.ServerCleanName("Host", 0));
        Assert.AreEqual("Wanderer_3", DiabloNetworkPlayer.ServerCleanName("<>", 3));
        Assert.AreEqual("Wanderer_5", DiabloNetworkPlayer.ServerCleanName(null, 5));
    }

    [Test]
    public void Nameplate_ColoursOwnAlliesAndDead()
    {
        Assert.AreEqual(DiabloPalette.GoldBright, HeroNameplates.ColorFor(own: true, alive: true));
        Assert.AreEqual(DiabloPalette.Parchment, HeroNameplates.ColorFor(own: false, alive: true));
        Assert.AreEqual(DiabloPalette.ParchmentDim, HeroNameplates.ColorFor(own: false, alive: false));
        Assert.AreEqual("Playing as Mira", ClassSelectUI.AccountLabel("Mira"));
    }
}
