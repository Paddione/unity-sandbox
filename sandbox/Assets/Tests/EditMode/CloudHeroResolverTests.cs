using System.IO;
using NUnit.Framework;

/// <summary>cinderreach-accounts S3: every sign-in case of the cloud hero, plus the side-car meta.</summary>
public class CloudHeroResolverTests
{
    const string Me = "me";

    static LocalHeroMeta Local(bool exists, string owner = "", long saved = 0, long synced = 0) =>
        new LocalHeroMeta { exists = exists, owner = owner, savedTicks = saved, syncedTicks = synced };

    static CloudHeroMeta Cloud(bool exists, long saved = 0) => new CloudHeroMeta { exists = exists, savedTicks = saved };

    [Test] public void NothingAnywhere_DoesNothing() =>
        Assert.AreEqual(CloudHeroAction.None, CloudHeroResolver.Decide(Me, Local(false), Cloud(false)));

    [Test] public void NewPc_PullsTheAccountHero() =>
        Assert.AreEqual(CloudHeroAction.Download, CloudHeroResolver.Decide(Me, Local(false), Cloud(true, 5)));

    [Test] public void FirstSignIn_AdoptsTheGuestHero() =>
        Assert.AreEqual(CloudHeroAction.Upload, CloudHeroResolver.Decide(Me, Local(true, "", 10), Cloud(false)));

    [Test] public void OwnHero_EmptyCloud_Uploads() =>
        Assert.AreEqual(CloudHeroAction.Upload, CloudHeroResolver.Decide(Me, Local(true, Me, 10, 10), Cloud(false)));

    [Test] public void OtherAccountsHero_EmptyCloud_StartsFresh() =>
        Assert.AreEqual(CloudHeroAction.StartFresh, CloudHeroResolver.Decide(Me, Local(true, "other", 10, 10), Cloud(false)));

    [Test] public void OtherAccountsHero_CloudHero_Downloads() =>
        Assert.AreEqual(CloudHeroAction.Download, CloudHeroResolver.Decide(Me, Local(true, "other", 50, 50), Cloud(true, 5)));

    [Test] public void GuestHero_MeetsAccountHero_Asks() =>
        Assert.AreEqual(CloudHeroAction.Ask, CloudHeroResolver.Decide(Me, Local(true, "", 50), Cloud(true, 5)));

    [Test] public void InSync_DoesNothing() =>
        Assert.AreEqual(CloudHeroAction.None, CloudHeroResolver.Decide(Me, Local(true, Me, 10, 10), Cloud(true, 10)));

    [Test] public void LocalProgress_CloudUntouched_Uploads() =>
        Assert.AreEqual(CloudHeroAction.Upload, CloudHeroResolver.Decide(Me, Local(true, Me, 20, 10), Cloud(true, 10)));

    [Test] public void CloudProgress_LocalUntouched_Downloads() =>
        Assert.AreEqual(CloudHeroAction.Download, CloudHeroResolver.Decide(Me, Local(true, Me, 10, 10), Cloud(true, 30)));

    [Test] public void BothProgressed_Asks() =>
        Assert.AreEqual(CloudHeroAction.Ask, CloudHeroResolver.Decide(Me, Local(true, Me, 20, 10), Cloud(true, 30)));

    [Test]
    public void Meta_RoundTripsAndDerivesExistence()
    {
        string dir = Path.Combine(Path.GetTempPath(), "cr-meta-" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string save = Path.Combine(dir, "hero.json");
        try
        {
            Assert.IsFalse(CloudHeroSync.ReadMeta(save).exists);
            File.WriteAllText(save, "{}");
            var fresh = CloudHeroSync.ReadMeta(save);
            Assert.IsTrue(fresh.exists);
            Assert.AreEqual("", fresh.owner);
            Assert.AreEqual(0, fresh.syncedTicks, "a pre-cloud save was never synced");
            Assert.Greater(fresh.savedTicks, 0, "a pre-cloud save dates from its file time");
            CloudHeroSync.WriteMeta(save, new LocalHeroMeta { owner = "p1", savedTicks = 42, syncedTicks = 40 });
            var back = CloudHeroSync.ReadMeta(save);
            Assert.AreEqual("p1", back.owner);
            Assert.AreEqual(42, back.savedTicks);
            Assert.AreEqual(40, back.syncedTicks);
            CloudHeroSync.NotifyLocalDelete(save);
            Assert.IsFalse(File.Exists(CloudHeroSync.MetaPath(save)), "a deleted hero leaves no owner behind");
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void HeroChoice_DescribesBothHeroes()
    {
        var text = ClassSelectUI.HeroChoiceText(
            new SaveGame { classId = ClassCatalog.RogueId, level = 7, gold = 123 },
            new SaveGame { classId = ClassCatalog.FenwalkerId, level = 2, gold = 5 });
        StringAssert.Contains("Rogue, level 7, 123 gold", text);
        StringAssert.Contains("Fenwalker, level 2, 5 gold", text);
    }
}
