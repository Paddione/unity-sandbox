using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class KayKitCharacters
{
    const string Root = "Assets/ThirdParty/KayKit/";
    static readonly string[] Packs = { "Adventurers", "Skeletons" };
    static readonly int[] CharacterCounts = { 6, 4 }; // Rogue includes a hooded variant.
    static readonly int[] AccessoryCounts = { 31, 13 };

    [MenuItem("Sandbox/KayKit/Configure Characters")]
    public static void Configure()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (var pack in Packs)
        {
            var folder = Root + pack;
            var textures = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => !p.EndsWith("contents.png"))
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>).ToArray();
            foreach (var path in Directory.GetFiles(folder, "*.fbx", SearchOption.AllDirectories))
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                var character = path.Contains("/Characters/");
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                if (character)
                {
                    importer.animationType = ModelImporterAnimationType.Generic;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.importAnimation = true;
                }
                importer.SaveAndReimport();
                var sources = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>()
                    .Select(m => new AssetImporter.SourceAssetIdentifier(m))
                    .Concat(importer.GetExternalObjectMap().Keys.Where(k => k.type == typeof(Material)))
                    .Distinct().ToArray();
                var key = character ? Path.GetFileNameWithoutExtension(path).Split('_')[0].ToLowerInvariant()
                    : pack == "Skeletons" ? "skeleton" : "barbarian";
                var texture = textures.FirstOrDefault(t => t.name == key + "_texture");
                if (texture == null) throw new InvalidOperationException($"Missing atlas for {path}");
                var materialPath = folder + "/" + texture.name + "_URP.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                material.SetTexture("_BaseMap", texture);
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Smoothness", 0);
                EditorUtility.SetDirty(material);
                foreach (var source in sources) importer.AddRemap(source, material);
                importer.SaveAndReimport();
            }
        }
        foreach (var path in AnimationPaths())
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        Verify();
    }

    public static void Verify()
    {
        for (var i = 0; i < Packs.Length; i++)
        {
            var folder = Root + Packs[i];
            var characters = Directory.GetFiles(folder + "/Characters", "*.fbx");
            var accessories = Directory.GetFiles(folder + "/Accessories", "*.fbx");
            if (characters.Length != CharacterCounts[i] || accessories.Length != AccessoryCounts[i])
                throw new InvalidOperationException($"Incomplete {Packs[i]} import.");
            foreach (var path in characters.Concat(accessories))
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null) throw new InvalidOperationException($"Missing model: {path}");
                var renderers = model.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException($"Missing renderer: {path}");
                foreach (var renderer in renderers)
                    foreach (var material in renderer.sharedMaterials)
                        if (material == null || material.shader.name != "Universal Render Pipeline/Lit" ||
                            material.GetTexture("_BaseMap") == null)
                            throw new InvalidOperationException($"Missing URP material or atlas: {path}");
                if (!path.Contains("/Characters/")) continue;
                var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (skins.Length == 0 || skins.Any(s => s.sharedMesh == null || s.bones.Length == 0 ||
                    s.bones.Any(b => b == null) || s.sharedMesh.bindposes.Length == 0))
                    throw new InvalidOperationException($"Invalid skinned character: {path}");
                var animator = model.GetComponent<Animator>();
                if (animator == null || animator.avatar == null || !animator.avatar.isValid)
                    throw new InvalidOperationException($"Missing valid Generic avatar: {path}");
            }
            Debug.Log($"KayKit characters verified: {Packs[i]}, {characters.Length} rigged models, {accessories.Length} accessories, URP materials.");
        }
        foreach (var path in AnimationPaths())
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToArray();
            if (clips.Length == 0 || clips.Any(c => c.length <= 0))
                throw new InvalidOperationException($"Missing animation clips: {path}");
            Debug.Log($"KayKit animations verified: {Path.GetFileName(path)}, {clips.Length} clips: {string.Join(", ", clips.Select(c => c.name))}");
        }
        var melee = AssetDatabase.LoadAllAssetsAtPath(Root +
            "CharacterAnimations/Rig_Medium/Rig_Medium_CombatMelee.fbx")
            .OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
        var bindings = AnimationUtility.GetCurveBindings(melee)
            .Where(b => b.type == typeof(Transform)).ToArray();
        if (bindings.Length == 0) throw new InvalidOperationException("Melee clip has no bone curves.");
        foreach (var pack in Packs)
            foreach (var path in Directory.GetFiles(Root + pack + "/Characters", "*.fbx"))
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var binding in bindings)
                    if (binding.path.Length > 0 && model.transform.Find(binding.path) == null)
                        throw new InvalidOperationException($"Animation bone {binding.path} missing on {path}");
            }
        Debug.Log("KayKit melee animation bone paths match all ten character models.");
    }

    static string[] AnimationPaths()
    {
        var paths = Directory.GetFiles(Root + "CharacterAnimations/Rig_Medium", "*.fbx");
        if (paths.Length != 8) throw new InvalidOperationException("Expected eight Rig_Medium animation sets.");
        return paths;
    }
}
