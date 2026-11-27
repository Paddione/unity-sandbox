using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time sprite import for the Blender-rendered menu icons
/// (Assets/AddressableAssets/MenuArt/*.png). Unity imports fresh PNGs as plain
/// textures; the menu loads them via Resources.Load&lt;Sprite&gt;, which needs
/// TextureImporterType.Sprite. Run headless:
///   Unity -batchmode -nographics -quit -projectPath sandbox \
///     -executeMethod MenuArtImporter.ImportAsSprites
/// Re-runnable: fixes any icon whose import type drifted back to Default.
/// </summary>
public static class MenuArtImporter
{
    const string Folder = "Assets/AddressableAssets/MenuArt";

    public static void ImportAsSprites()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder });
        Debug.Log($"[MenuArt] Found {guids.Length} icon textures in {Folder}.");
        int fixedCount = 0;
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[MenuArt] No TextureImporter for {path}.");
                continue;
            }
            bool dirty = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || !importer.alphaIsTransparency
                || importer.mipmapEnabled;
            if (dirty)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                fixedCount++;
            }
        }
        AssetDatabase.Refresh();
        Debug.Log($"[MenuArt] ImportAsSprites done: {fixedCount} reimported as Sprite, {guids.Length} total.");
        if (guids.Length == 0)
            throw new System.Exception("[MenuArt] No icon textures found — PNGs missing?");
    }
}
