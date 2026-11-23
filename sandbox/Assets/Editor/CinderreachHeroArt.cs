using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CinderreachHeroArt
{
    const string SourcePath = "Assets/Resources/CinderreachHeroArt.asset";

    [MenuItem("Sandbox/Cinderreach/Configure Hero Art")]
    public static void Configure()
    {
        var source = AssetDatabase.LoadAssetAtPath<HeroArtSource>(SourcePath);
        if (source == null)
        {
            source = ScriptableObject.CreateInstance<HeroArtSource>();
            AssetDatabase.CreateAsset(source, SourcePath);
        }
        source.LightClothing = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ThirdParty/KayKit/Adventurers/Characters/Ranger.fbx");
        EditorUtility.SetDirty(source);
        AssetDatabase.SaveAssets();
        Verify();
    }

    public static void Verify()
    {
        var source = AssetDatabase.LoadAssetAtPath<HeroArtSource>(SourcePath);
        if (source == null || source.LightClothing == null ||
            !source.LightClothing.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any())
            throw new InvalidOperationException("Cinderreach light clothing is missing or has no skin.");
        var signatures = new HashSet<Hash128>();
        var ids = new HashSet<string>(ItemDatabase.UniqueIds);
        if (!ids.SetEquals(HeroGearModels.Ids))
            throw new InvalidOperationException("Unique model coverage differs from the item catalog.");
        foreach (var id in ids)
        {
            var item = ItemDatabase.CreateUnique(id);
            var model = HeroGearModels.Create(id, item.slot, null);
            if (model == null) throw new InvalidOperationException($"Missing model for {id} in {item.slot}.");
            try
            {
                var filters = model.GetComponentsInChildren<MeshFilter>();
                if (filters.Length == 0 || filters.Any(f => f.sharedMesh == null || f.sharedMesh.vertexCount < 3) ||
                    model.GetComponentsInChildren<MeshRenderer>().Any(r => r.sharedMaterial == null))
                    throw new InvalidOperationException($"Invalid geometry or material for {id}.");
                var signature = new Hash128();
                foreach (var filter in filters)
                    foreach (var vertex in filter.sharedMesh.vertices)
                    {
                        var point = filter.transform.localToWorldMatrix.MultiplyPoint3x4(vertex);
                        signature.Append(point.x);
                        signature.Append(point.y);
                        signature.Append(point.z);
                    }
                if (!signatures.Add(signature))
                    throw new InvalidOperationException($"Duplicate Unique geometry: {id}.");
            }
            finally { UnityEngine.Object.DestroyImmediate(model); }
        }
        Debug.Log($"Cinderreach hero art verified: light outfit and {ids.Count} distinct Unique models.");
    }
}
