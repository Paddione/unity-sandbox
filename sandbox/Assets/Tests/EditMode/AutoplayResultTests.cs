using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>loop-guard S1: autoplay result contract (schema 1) consumed by G-DIABLO06.</summary>
public class AutoplayResultTests
{
    string dir;

    [SetUp]
    public void Setup() =>
        dir = Path.Combine(Path.GetTempPath(), $"cinderreach-autoplay-{System.Guid.NewGuid():N}");

    [TearDown]
    public void Teardown()
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    static AutoplayResult FullRun()
    {
        var r = new AutoplayResult { commit = "abc123", started = "2026-11-18T03:30:00Z", duration_s = 73.5f };
        foreach (var p in AutoplayResult.RequiredPhases) r.Reach(p);
        r.fps_windows.Add(45f);
        r.fps_windows.Add(52.5f);
        return r;
    }

    [Test]
    public void Json_CarriesAllSchemaFields()
    {
        var r = FullRun();
        r.exit = r.ComputeExit();
        string json = r.ToJson();
        foreach (var key in new[] { "schema", "commit", "started", "duration_s", "phases", "exceptions", "fps_windows", "exit" })
            StringAssert.Contains($"\"{key}\"", json, key);
        var back = AutoplayResult.FromJson(json);
        Assert.AreEqual(1, back.schema);
        Assert.AreEqual("abc123", back.commit);
        Assert.AreEqual(73.5f, back.duration_s, 0.001f);
        CollectionAssert.AreEqual(AutoplayResult.RequiredPhases, back.phases);
        CollectionAssert.AreEqual(new[] { 45f, 52.5f }, back.fps_windows);
        Assert.AreEqual(0, back.exit);
    }

    [Test]
    public void Json_CarriesPartyFields()
    {
        var r = FullRun();
        r.partyRoles = new System.Collections.Generic.List<string> { "fenwalker", "lanternbearer", "rogue" };
        r.huskKills = 22;
        r.healsCast = 7;
        r.hpRestored = 84f;
        var back = AutoplayResult.FromJson(r.ToJson());
        CollectionAssert.AreEqual(new[] { "fenwalker", "lanternbearer", "rogue" }, back.partyRoles);
        Assert.AreEqual(22, back.huskKills);
        Assert.AreEqual(7, back.healsCast);
        Assert.AreEqual(84f, back.hpRestored, 0.001f);
    }

    [Test]
    public void RequiredPhases_AreTheEightContractPhases()
    {
        CollectionAssert.AreEqual(
            new[] { "town", "zone1", "gate", "zone2", "boss", "zone3", "actboss", "complete" },
            AutoplayResult.RequiredPhases);
    }

    [Test]
    public void Phases_IncompleteWithoutBossKill()
    {
        var r = new AutoplayResult();
        r.Reach(AutoplayResult.PhaseTown);
        r.Reach(AutoplayResult.PhaseZone1);
        r.Reach(AutoplayResult.PhaseGate);
        r.Reach(AutoplayResult.PhaseZone2);
        Assert.IsFalse(r.AllPhasesReached());
        Assert.AreEqual(1, r.ComputeExit());
        r.Reach(AutoplayResult.PhaseBoss);
        r.Reach(AutoplayResult.PhaseComplete);
        Assert.IsFalse(r.AllPhasesReached(), "act-one: the Kiln and the Kilnmother are part of a full run");
        r.Reach(AutoplayResult.PhaseZone3);
        r.Reach(AutoplayResult.PhaseActBoss);
        Assert.IsTrue(r.AllPhasesReached());
        Assert.AreEqual(0, r.ComputeExit());
    }

    [Test]
    public void Reach_IsIdempotentAndKeepsOrder()
    {
        var r = new AutoplayResult();
        r.Reach("town");
        r.Reach("zone1");
        r.Reach("town");
        r.Reach("");
        CollectionAssert.AreEqual(new[] { "town", "zone1" }, r.phases);
    }

    [Test]
    public void Exceptions_FailTheRun()
    {
        var r = FullRun();
        r.exceptions = 1;
        Assert.AreEqual(1, r.ComputeExit());
    }

    [Test]
    public void ExceptionCounter_CountsOnlyExceptions()
    {
        Assert.IsTrue(AutoplayResult.CountsAsException(LogType.Exception));
        Assert.IsFalse(AutoplayResult.CountsAsException(LogType.Error));
        Assert.IsFalse(AutoplayResult.CountsAsException(LogType.Warning));
        Assert.IsFalse(AutoplayResult.CountsAsException(LogType.Log));
        Assert.IsFalse(AutoplayResult.CountsAsException(LogType.Assert));
    }

    [Test]
    public void WriteTo_CreatesDirectoryAndOverwrites()
    {
        var r = FullRun();
        string path = r.WriteTo(dir);
        Assert.AreEqual(Path.Combine(dir, AutoplayResult.FileName), path);
        r.commit = "def456";
        r.WriteTo(dir);
        Assert.AreEqual("def456", AutoplayResult.FromJson(File.ReadAllText(path)).commit);
        Assert.IsFalse(File.Exists(path + ".tmp"));
    }

    [Test]
    public void FpsReporter_WindowsListIsReadOnlyView()
    {
        Assert.IsNotNull(FpsReporter.Windows);
        Assert.IsFalse(FpsReporter.Windows is System.Collections.Generic.List<float>,
            "callers must not get the mutable backing list");
    }
}
