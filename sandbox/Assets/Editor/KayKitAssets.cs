using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Model packs share one gradient atlas per pack, except the character packs
// (per-class materials) and the animations pack (clips, no meshes).
public static class KayKitAssets
{
    const string Root = "Assets/ThirdParty/KayKit";
    static readonly string[] Packs = { "ForestNature", "Dungeon", "Adventurers", "Skeletons", "CharacterAnimations" };
    static readonly string[] ModelDirs = { "Models", "Models", "Characters", "Characters", "Rig_Medium" };
    static readonly int[] ModelCounts = { 105, 211, 6, 4, 8 };
    static readonly bool[] SingleAtlas = { true, true, false, false, false };
    static readonly bool[] AnimationsOnly = { false, false, false, false, true };

    [MenuItem("Sandbox/KayKit/Configure URP Materials")]
    public static void Configure()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new InvalidOperationException("URP Lit shader is missing.");
        for (var i = 0; i < Packs.Length; i++)
        {
            var folder = $"{Root}/{Packs[i]}";
            var sub = $"{folder}/{ModelDirs[i]}";
            if (SingleAtlas[i])
            {
                // Best effort: exact menu path varies by version; cosmetic only.
                var texturePath = AssetDatabase.GUIDToAssetPath(
                    AssetDatabase.FindAssets("t:Texture2D", new[] { sub }).Single());
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
                foreach (var path in ModelPaths(sub))
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
                AssetDatabase.SaveAssets();
            }
            else if (!AnimationsOnly[i])
            {
                // Character packs keep their per-class materials; only enforce URP Lit.
                foreach (var matPath in Directory.GetFiles(folder, "*.mat", SearchOption.AllDirectories)
                    .Select(p => p.Replace('\\', '/')).OrderBy(p => p))
                {
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (mat == null) continue;
                    if (mat.shader == null || mat.shader.name != "Universal Render Pipeline/Lit")
                    {
                        mat.shader = shader;
                        EditorUtility.SetDirty(mat);
                    }
                }
                AssetDatabase.SaveAssets();
            }
        }
        Verify();
    }

    [MenuItem("Sandbox/KayKit/Verify Imported Assets")]
    public static void Verify()
    {
        for (var i = 0; i < Packs.Length; i++)
        {
            var sub = $"{Root}/{Packs[i]}/{ModelDirs[i]}";
            var paths = ModelPaths(sub);
            if (paths.Length != ModelCounts[i])
                throw new InvalidOperationException($"{Packs[i]}: expected {ModelCounts[i]} models, got {paths.Length}.");
            foreach (var path in paths)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) throw new InvalidOperationException($"Model failed to import: {path}");
                if (AnimationsOnly[i])
                {
                    var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().ToArray();
                    if (clips.Length == 0)
                        throw new InvalidOperationException($"Missing animation clips: {path}");
                    continue;
                }
                var filters = model.GetComponentsInChildren<MeshFilter>(true);
                var skinned = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (filters.Length == 0 && skinned.Length == 0)
                    throw new InvalidOperationException($"Missing mesh: {path}");
                if (filters.Any(f => f.sharedMesh == null || f.sharedMesh.vertexCount == 0))
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
    }

    static string[] ModelPaths(string folder) => Directory.GetFiles(folder, "*.fbx")
        .Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToArray();
}
