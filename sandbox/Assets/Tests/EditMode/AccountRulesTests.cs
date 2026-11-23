using NUnit.Framework;

/// <summary>cinderreach-accounts S2: username/password rules and player-facing error texts.</summary>
public class AccountRulesTests
{
    [TestCase("mira")]
    [TestCase("mira.k-77@x_y")]
    [TestCase("abc")]
    [TestCase("a2345678901234567890")]
    public void Username_Valid(string u) => Assert.IsNull(AccountRules.CheckUsername(u));

    [TestCase(null)]
    [TestCase("ab")]
    [TestCase("a23456789012345678901")]
    [TestCase("mira k")]
    [TestCase("zoë")]
    public void Username_Invalid(string u) => Assert.IsNotNull(AccountRules.CheckUsername(u));

    [TestCase("Ash-fen1")]
    [TestCase("Kiln!Mother2026")]
    public void Password_Valid(string p) => Assert.IsNull(AccountRules.CheckPassword(p));

    [TestCase("short1!")]          // too short
    [TestCase("alllowercase1!")]   // no upper
    [TestCase("ALLUPPERCASE1!")]   // no lower
    [TestCase("NoDigitsHere!")]    // no digit
    [TestCase("NoSymbols123")]     // no symbol
    [TestCase("Aa1!aaaaaaaaaaaaaaaaaaaaaaaaaaa")] // 31 chars
    public void Password_Invalid(string p) => Assert.IsNotNull(AccountRules.CheckPassword(p));

    [Test]
    public void Explain_MapsServerFailures()
    {
        Assert.AreEqual("Wrong username or password.", AccountRules.Explain(0, "Invalid username or password", false));
        Assert.AreEqual("That username is taken.", AccountRules.Explain(AccountRules.CodeAccountAlreadyLinked, "", true));
        Assert.AreEqual("No connection to the account service.", AccountRules.Explain(AccountRules.CodeTransportError, null, false));
        Assert.AreEqual("Too many attempts. Wait a minute and try again.", AccountRules.Explain(AccountRules.CodeTooManyRequests, "", false));
        Assert.AreEqual("Could not create the account.", AccountRules.Explain(0, "boom", true));
    }
}
