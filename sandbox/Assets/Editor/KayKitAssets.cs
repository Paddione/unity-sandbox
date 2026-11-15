using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// The original Unity FBX files share one gradient atlas per pack.
public static class KayKitAssets
{
    const string Root = "Assets/ThirdParty/KayKit";
    static readonly string[] Packs = { "ForestNature", "Dungeon" };
    static readonly int[] ModelCounts = { 105, 211 };

    [MenuItem("Sandbox/KayKit/Configure URP Materials")]
    public static void Configure()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new InvalidOperationException("URP Lit shader is missing.");
        foreach (var pack in Packs)
        {
            var folder = $"{Root}/{pack}";
            var texturePath = AssetDatabase.GUIDToAssetPath(
                AssetDatabase.FindAssets("t:Texture2D", new[] { folder + "/Models" }).Single());
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var materialPath = folder + "/KayKitURP.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0);
            // Several foliage models intentionally have single-sided geometry.
            material.SetFloat("_Cull", 0);
            material.doubleSidedGI = true;
            EditorUtility.SetDirty(material);
            foreach (var path in ModelPaths(folder))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.SaveAndReimport();
                var sources = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>()
                    .Select(m => new AssetImporter.SourceAssetIdentifier(m))
                    .Concat(importer.GetExternalObjectMap().Keys.Where(k => k.type == typeof(Material)))
                    .Distinct().ToArray();
                foreach (var source in sources) importer.AddRemap(source, material);
                importer.SaveAndReimport();
            }
        }
        AssetDatabase.SaveAssets();
        Verify();
    }

    [MenuItem("Sandbox/KayKit/Verify Imported Assets")]
    public static void Verify()
    {
        for (var i = 0; i < Packs.Length; i++)
        {
            var folder = $"{Root}/{Packs[i]}";
            var paths = ModelPaths(folder);
            if (paths.Length != ModelCounts[i])
                throw new InvalidOperationException($"{Packs[i]}: expected {ModelCounts[i]} models, got {paths.Length}.");
            foreach (var path in paths)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) throw new InvalidOperationException($"Model failed to import: {path}");
                var filters = model.GetComponentsInChildren<MeshFilter>(true);
                if (filters.Length == 0 || filters.Any(f => f.sharedMesh == null || f.sharedMesh.vertexCount == 0))
                    throw new InvalidOperationException($"Missing mesh: {path}");
                var renderers = model.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException($"Missing renderer: {path}");
                foreach (var renderer in renderers)
                {
                    if (renderer.sharedMaterials.Length == 0)
                        throw new InvalidOperationException($"Missing materials: {path}");
                    foreach (var material in renderer.sharedMaterials)
                        if (material == null || material.shader == null ||
                            material.shader.name != "Universal Render Pipeline/Lit" ||
                            material.GetTexture("_BaseMap") == null)
                            throw new InvalidOperationException($"Missing URP material or atlas: {path}");
                }
            }
            Debug.Log($"KayKit verified: {Packs[i]}, {paths.Length} models, URP Lit + atlas on every renderer.");
        }
        KayKitCharacters.Verify();
    }

    static string[] ModelPaths(string folder) => Directory.GetFiles(folder + "/Models", "*.fbx")
        .Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToArray();
}
