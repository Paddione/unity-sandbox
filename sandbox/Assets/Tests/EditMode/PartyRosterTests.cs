using NUnit.Framework;

/// <summary>cinderreach-party-roles S4: role coverage mapping and solo detection.</summary>
public class PartyRosterTests
{
    [Test]
    public void ClassIdForRole_CoversAllRoles()
    {
        Assert.AreEqual(ClassCatalog.FenwalkerId, PartyRoster.ClassIdForRole(ClassDefinition.PartyRole.Tank));
        Assert.AreEqual(ClassCatalog.LanternbearerId, PartyRoster.ClassIdForRole(ClassDefinition.PartyRole.Healer));
        Assert.AreEqual(ClassCatalog.RogueId, PartyRoster.ClassIdForRole(ClassDefinition.PartyRole.Dps));
        foreach (ClassDefinition.PartyRole role in System.Enum.GetValues(typeof(ClassDefinition.PartyRole)))
            Assert.AreEqual(role, ClassCatalog.Get(PartyRoster.ClassIdForRole(role)).role);
    }

    [Test]
    public void PartySize_IsThree()
    {
        Assert.AreEqual(3, PartyRoster.PartySize);
    }

    [Test]
    public void IsSolo_WithoutNetwork()
    {
        Assert.IsTrue(PartyRoster.IsSolo(), "no network manager means solo play");
    }

    [Test]
    public void EnsureSoloParty_NeedsAHero()
    {
        Assert.IsNull(PartyRoster.EnsureSoloParty(null));
    }

    [Test]
    public void WillJoinAsClient_FalseWithoutJoinFlags()
    {
        // Test runs never join: no -join/--net-client/-join-code/-coop-autoplay client on the line.
        Assert.IsFalse(PartyRoster.WillJoinAsClient());
    }
}
