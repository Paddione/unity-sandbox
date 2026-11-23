using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CinderreachAssetAudit
{
    [MenuItem("Sandbox/Cinderreach/Export Asset Inventory")]
    public static void Export()
    {
        var roots = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var scene in EditorBuildSettings.scenes.Where(scene => scene.enabled))
            roots[scene.path] = "Build scene";
        foreach (var name in EditorBuildSettings.GetConfigObjectNames())
            if (EditorBuildSettings.TryGetConfigObject<UnityEngine.Object>(name, out var config))
                AddRoot(roots, config, "Build configuration");
        foreach (var path in new[] { "ProjectSettings/GraphicsSettings.asset", "ProjectSettings/QualitySettings.asset" })
            AddSettingsRoots(roots, path);
        foreach (var asset in PlayerSettings.GetPreloadedAssets())
            AddRoot(roots, asset, "Player preload");
        foreach (var asset in AssetDatabase.GetAllAssetPaths())
        {
            if (!AssetDatabase.IsValidFolder(asset) && asset.StartsWith("Assets/", StringComparison.Ordinal)
                && (asset.Contains("/Resources/") || asset.StartsWith("Assets/StreamingAssets/", StringComparison.Ordinal)))
                roots[asset] = "Runtime asset";
        }

        var usage = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (var root in roots)
        {
            if (AssetDatabase.LoadMainAssetAtPath(root.Key) == null &&
                !root.Key.StartsWith("Assets/StreamingAssets/", StringComparison.Ordinal))
                throw new InvalidOperationException($"Inventory root is missing: {root.Key}");

            foreach (var path in AssetDatabase.GetDependencies(root.Key, true).Append(root.Key))
            {
                if (!usage.TryGetValue(path, out var sites))
                    usage[path] = sites = new SortedSet<string>(StringComparer.Ordinal);
                sites.Add($"{root.Value}: {root.Key}");
            }
        }

        var output = Environment.GetEnvironmentVariable("CINDERREACH_ASSET_INVENTORY");
        if (string.IsNullOrWhiteSpace(output))
            output = Path.GetFullPath("../docs/cinderreach-asset-inventory.tsv");
        File.WriteAllLines(output, usage.Select(entry =>
            $"{entry.Key}\t{Origin(entry.Key)}\t{string.Join(" | ", entry.Value)}")
            .Prepend("Asset\tHerkunft\tEinsatzort"));
        Debug.Log($"Cinderreach asset inventory: {usage.Count} paths from {roots.Count} roots. {output}");
    }

    static void AddSettingsRoots(IDictionary<string, string> roots, string settingsPath)
    {
        foreach (System.Text.RegularExpressions.Match match in
            System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(settingsPath), @"guid: ([0-9a-f]{32})"))
        {
            var path = AssetDatabase.GUIDToAssetPath(match.Groups[1].Value);
            if (path.StartsWith("Assets/", StringComparison.Ordinal) || path.StartsWith("Packages/", StringComparison.Ordinal))
                roots.TryAdd(path, settingsPath);
        }
    }

    static void AddRoot(IDictionary<string, string> roots, UnityEngine.Object asset, string role)
    {
        var path = AssetDatabase.GetAssetPath(asset);
        if (!string.IsNullOrEmpty(path)) roots[path] = role;
    }

    static string Origin(string path)
    {
        if (path.StartsWith("Assets/ThirdParty/", StringComparison.Ordinal))
            return string.Join("/", path.Split('/').Take(4).Skip(2));
        if (path.StartsWith("Packages/", StringComparison.Ordinal))
            return path.Split('/')[1];
        return path.StartsWith("Assets/", StringComparison.Ordinal) ? "Cinderreach" : "Unity";
    }
}
