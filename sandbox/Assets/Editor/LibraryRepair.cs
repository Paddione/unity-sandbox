using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Self-heal for unbound runtime scripts. A session whose first asset refresh
/// recompiles every assembly (typical for a worktree whose Library was copied
/// from the main checkout) can leave MonoScript import artifacts of
/// Assets/Scripts without a class binding: <c>MonoScript.GetClass()</c> is
/// null, scenes and prefabs load those components as null without any
/// "missing script" warning, and every later session inherits the cache.
/// Seen as 14 red EditMode scene tests, a failing VerifyDiabloZone and an
/// autoplay player without a controllable hero (reproduced and repaired
/// 2026-11-18, docs/dev-journal.md). A forced recursive reimport of
/// Assets/Scripts rebinds them from the next session on.
/// <see cref="SandboxBuilder.CompileCheck"/> runs this check;
/// scripts/unity-warmup.sh repeats that session until it reports
/// "scripts bound" and runs before every test run and autoplay build.
/// </summary>
public static class LibraryRepair
{
    public const string ScriptsRoot = "Assets/Scripts";

    /// <summary>MonoBehaviour/NetworkBehaviour scripts under Assets/Scripts whose MonoScript has no class.</summary>
    public static int CountUnbound()
    {
        int unbound = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:MonoScript", new[] { ScriptsRoot }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (ms == null || ms.GetClass() != null) continue;
            var name = Path.GetFileNameWithoutExtension(path);
            var pattern = $@"class\s+{Regex.Escape(name)}\s*:\s*(MonoBehaviour|NetworkBehaviour)\b";
            if (Regex.IsMatch(File.ReadAllText(path), pattern)) unbound++;
        }
        return unbound;
    }

    /// <summary>Reimport Assets/Scripts when any behaviour script is unbound; returns the count found.</summary>
    public static int EnsureScriptsBound()
    {
        int unbound = CountUnbound();
        if (unbound == 0)
        {
            Debug.Log("[LibraryRepair] scripts bound");
            return 0;
        }
        // The rebinding only shows in the next editor session (verified
        // 2026-11-18): scripts/unity-warmup.sh runs a second check session.
        Debug.LogWarning($"[LibraryRepair] {unbound} behaviour scripts unbound, reimported {ScriptsRoot} (effective next session)");
        AssetDatabase.ImportAsset(ScriptsRoot, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        return unbound;
    }
}
