using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>S1 identity gate: no legacy D2-era names in player-facing strings
/// or the scene name catalog (Cinderreach / Ashen Fen / Lantern Hold / Fen Gate
/// / Fen Brute / Ash Elite). Journal history and asset paths are exempt.</summary>
public class IdentityTests
{
    const string ZonePath = "Assets/Scenes/DiabloZone.unity";

    static readonly Regex Legacy = new Regex("blood ?moor|rogue encampment|moor gate",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [Test]
    public void SceneNameCatalog_HasNoLegacyNames()
    {
        var scene = EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        Assert.IsTrue(scene.IsValid(), "DiabloZone.unity must open cleanly.");
        var bad = new System.Collections.Generic.List<string>();
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (Legacy.IsMatch(go.name)) bad.Add(go.name);
        Assert.IsEmpty(bad, "Scene objects must not carry legacy names: " + string.Join(", ", bad));
    }

    [Test]
    public void RenamedElites_ExistUnderNewNames()
    {
        EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        Assert.IsNotNull(GameObject.Find("Ash Elite"), "West alcove elite must be named 'Ash Elite'.");
        Assert.IsNotNull(GameObject.Find("Fen Brute"), "Northeast brute must be named 'Fen Brute'.");
        Assert.IsNull(GameObject.Find("Husk_Elite"), "Legacy 'Husk_Elite' must be gone.");
        Assert.IsNull(GameObject.Find("Husk_Brute"), "Legacy 'Husk_Brute' must be gone.");
    }

    [Test]
    public void HintSources_HaveNoLegacyNames()
    {
        string dir = Path.Combine(Application.dataPath, "Scripts");
        var bad = new System.Collections.Generic.List<string>();
        foreach (var file in new[] { "ZoneEntryHint.cs", "DiabloLifeGlobe.cs", "PlayerHealth.cs" })
        {
            string text = File.ReadAllText(Path.Combine(dir, file));
            foreach (Match m in Legacy.Matches(text)) bad.Add($"{file}: {m.Value}");
        }
        Assert.IsEmpty(bad, "Hint sources must not carry legacy names: " + string.Join(", ", bad));
    }
}
