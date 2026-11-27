using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

/// <summary>Deterministic Addressables project setup (Task 6).
/// Assets live under Assets/AddressableAssets (moved out of Resources);
/// every entry address equals the old Resources path (extensionless), so
/// Res.Get call sites keep working 1:1.
///
/// Run: Unity -batchmode -nographics -quit -projectPath sandbox
///   -executeMethod AddressablesSetup.Ensure
/// Idempotent: reuses settings/group/entries, moves nothing twice.
/// </summary>
public static class AddressablesSetup
{
    public const string GroupName = "game-content";
    public const string SourceRoot = "Assets/AddressableAssets";

    public static void Ensure()
    {
        var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        settings.BuildAddressablesWithPlayerBuild =
            AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;
        var group = settings.FindGroup(GroupName)
            ?? settings.CreateGroup(GroupName, false, false, false, null,
                typeof(BundledAssetGroupSchema));
        int touched = 0;
        foreach (var file in Directory.GetFiles(SourceRoot, "*", SearchOption.AllDirectories))
        {
            if (file.EndsWith(".meta")) continue;
            string assetPath = file.Replace('\\', '/');
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid)) continue;
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.SetAddress(ToAddress(assetPath), false);
            touched++;
        }
        // Prune: entries whose assets left the source root (e.g. package-owned
        // files like BillingMode.json that Unity IAP regenerates under
        // Resources/ and must stay there).
        int pruned = 0;
        foreach (var entry in new System.Collections.Generic.List<AddressableAssetEntry>(group.entries))
        {
            string path = AssetDatabase.GUIDToAssetPath(entry.guid);
            if (string.IsNullOrEmpty(path) || !path.Replace('\\', '/').StartsWith(SourceRoot + "/"))
            {
                group.RemoveAssetEntry(entry, false);
                pruned++;
            }
        }
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        Debug.Log($"[AddressablesSetup] group '{GroupName}': {touched} entries, {pruned} pruned, BuildWithPlayer on.");
        if (settings.groups.Count == 0 || touched == 0)
            throw new System.Exception("AddressablesSetup: no entries created; check " + SourceRoot);
    }

    /// <summary>Full content build (same as the automatic player-build step).</summary>
    public static void BuildContent()
    {
        AddressableAssetSettings.BuildPlayerContent();
        Debug.Log("[AddressablesSetup] content build done.");
    }

    /// <summary>Content-update build from a previous state .bin.
    /// Pass the state path via command line: -updateState &lt;path&gt;.</summary>
    public static void BuildUpdate()
    {
        string state = null;
        var argv = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < argv.Length - 1; i++)
            if (argv[i] == "-updateState") state = argv[i + 1];
        if (string.IsNullOrEmpty(state) || !File.Exists(state))
            throw new System.Exception("AddressablesSetup.BuildUpdate needs -updateState <addressables_content_state.bin>.");
        var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        var result = ContentUpdateScript.BuildContentUpdate(settings, state);
        string suffix = result == null ? "(null result — see log for errors)"
            : "output: " + (result.OutputPath ?? "(no output path)");
        Debug.Log("[AddressablesSetup] content update done, " + suffix);
    }

    /// <summary>Assets/AddressableAssets/Voice/x.ogg -> Voice/x (old Resources key).</summary>
    static string ToAddress(string assetPath)
    {
        string rel = assetPath.StartsWith(SourceRoot + "/")
            ? assetPath.Substring(SourceRoot.Length + 1)
            : Path.GetFileName(assetPath);
        string ext = Path.GetExtension(rel);
        return string.IsNullOrEmpty(ext) ? rel : rel.Substring(0, rel.Length - ext.Length);
    }
}
