using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Textured world surfaces for all four zones (cinderreach-world-textures).
/// Maps come from docs/world-art/build_zone_textures.py under Assets/Art/Zones/Textures:
/// albedo (alpha = smoothness), greyscale height imported as normal map, optional emission.
/// <see cref="Apply"/> runs after all builders and the Fen dressing: it hands the greybox
/// floors, pillars, domes and spires a URP/Lit surface material and a mesh copy with
/// world-space box UVs, so texel density stays constant on stretched primitives and
/// neighbouring blocks do not repeat the same tile phase. Names, colliders and positions
/// stay untouched (tests and gameplay read them).
/// </summary>
public static class ZoneSurfaceBuilder
{
    public const string Root = "Assets/Art/Zones";
    public const string Textures = Root + "/Textures";
    const string Materials = Root + "/Materials";

    /// <summary>Surface → (normal strength, emission intensity; 0 = none).</summary>
    public static readonly Dictionary<string, (float bump, float emission)> Surfaces =
        new Dictionary<string, (float, float)>
        {
            { "FenMoor", (0.9f, 0f) },
            { "CryptFlagstone", (1.2f, 0f) },
            { "CryptAshlar", (1.2f, 0f) },
            { "KilnBasalt", (1.1f, 1.1f) },
            { "KilnBrick", (1.0f, 0f) },
            { "Slag", (1.0f, 0f) },
            { "BarrensSand", (0.45f, 0f) },
            { "BarrensStone", (1.1f, 0f) },
            { "Planks", (1.0f, 0f) },
            { "Stone", (1.0f, 0f) },
            { "Bark", (1.2f, 0f) },
            { "DarkWood", (0.8f, 0f) },
            { "Moss", (0.8f, 0f) },
            { "Bone", (0.6f, 0f) },
            { "Iron", (0.7f, 0f) },
            { "BloodCloth", (0.6f, 0f) },
        };

    /// <summary>Scene object name (exact or prefix ending in '_') → (surface, metres per texture repeat).</summary>
    public static readonly (string name, string surface, float tile)[] Rules =
    {
        ("Ground", "FenMoor", 7f),
        ("OssuaryFloor", "CryptFlagstone", 4f),
        ("OssuaryPillar_", "CryptAshlar", 2.5f),
        ("OssuarySarcophagus_", "CryptAshlar", 2.5f),
        (EncounterBuilder.CoverPrefix, "CryptAshlar", 2.5f),
        ("OssuaryTorch_", "Planks", 1.5f),
        ("KilnFloor", "KilnBasalt", 8f),
        ("KilnDome_", "KilnBrick", 3f),
        ("KilnDomeCap_", "KilnBrick", 3f),
        ("KilnSlag_", "Slag", 2f),
        ("BarrensFloor", "BarrensSand", 10f),
        (BarrensBuilder.DunePrefix, "BarrensSand", 10f),
        (BarrensBuilder.SpirePrefix, "BarrensStone", 2.5f),
        ("WaypointShrine_", "Stone", 1.5f),
        (ZoneEdgeBuilder.Prefix + "Ossuary_", "CryptAshlar", 2.5f),
        (ZoneEdgeBuilder.Prefix + "Kiln_", "KilnBrick", 3f),
        (ZoneEdgeBuilder.Prefix + "Barrens_", "BarrensStone", 2.5f),
        (ContractBoard.BoardObjectName, "Planks", 1.5f),
        (SmithStation.SmithObjectName, "CryptAshlar", 2.5f),
    };

    [MenuItem("Sandbox/World Art/Import Zone Surfaces")]
    public static void EnsureAssets()
    {
        Directory.CreateDirectory(Materials);
        foreach (var kv in Surfaces) EnsureMaterial(kv.Key, kv.Value.bump, kv.Value.emission);
        AssetDatabase.SaveAssets();
    }

    public static Material Load(string surface) =>
        AssetDatabase.LoadAssetAtPath<Material>($"{Materials}/{surface}.mat");

    static Material EnsureMaterial(string name, float bump, float emission)
    {
        string albedoPath = $"{Textures}/{name}_Albedo.png";
        string heightPath = $"{Textures}/{name}_Height.png";
        string emissionPath = $"{Textures}/{name}_Emission.png";
        if (!File.Exists(albedoPath))
        {
            Debug.LogWarning($"[ZoneSurfaces] Missing {albedoPath}; run docs/world-art/build_zone_textures.py.");
            return null;
        }
        ConfigureTexture(albedoPath, srgb: true, normal: false);
        ConfigureTexture(heightPath, srgb: false, normal: true);
        bool glows = emission > 0f && File.Exists(emissionPath);
        if (glows) ConfigureTexture(emissionPath, srgb: true, normal: false);

        string path = $"{Materials}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath));
        m.SetColor("_BaseColor", Color.white);
        // Smoothness lives in the albedo alpha; the slider scales it.
        m.SetFloat("_SmoothnessTextureChannel", 1f);
        m.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
        m.SetFloat("_Smoothness", 1f);
        m.SetFloat("_Metallic", 0f);
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(heightPath);
        m.SetTexture("_BumpMap", normal);
        m.SetFloat("_BumpScale", bump);
        if (normal != null) m.EnableKeyword("_NORMALMAP");
        else m.DisableKeyword("_NORMALMAP");
        if (glows)
        {
            m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(emissionPath));
            m.SetColor("_EmissionColor", Color.white * emission);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            m.SetTexture("_EmissionMap", null);
            m.SetColor("_EmissionColor", Color.black);
            m.DisableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }
        m.enableInstancing = true;
        EditorUtility.SetDirty(m);
        return m;
    }

    static void ConfigureTexture(string path, bool srgb, bool normal)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null)
        {
            AssetDatabase.ImportAsset(path);
            ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
        }
        bool dirty = false;
        void Set<T>(T current, T wanted, System.Action<T> apply)
        {
            if (EqualityComparer<T>.Default.Equals(current, wanted)) return;
            apply(wanted);
            dirty = true;
        }
        Set(ti.textureType, normal ? TextureImporterType.NormalMap : TextureImporterType.Default, v => ti.textureType = v);
        Set(ti.convertToNormalmap, normal, v => ti.convertToNormalmap = v);
        if (normal)
        {
            Set(ti.heightmapScale, 0.12f, v => ti.heightmapScale = v);
            Set(ti.normalmapFilter, TextureImporterNormalFilter.Sobel, v => ti.normalmapFilter = v);
        }
        Set(ti.sRGBTexture, srgb && !normal, v => ti.sRGBTexture = v);
        Set(ti.wrapMode, TextureWrapMode.Repeat, v => ti.wrapMode = v);
        Set(ti.mipmapEnabled, true, v => ti.mipmapEnabled = v);
        Set(ti.anisoLevel, 4, v => ti.anisoLevel = v);
        Set(ti.alphaIsTransparency, false, v => ti.alphaIsTransparency = v);
        if (dirty) ti.SaveAndReimport();
    }

    // ------------------------------------------------------------------ scene

    /// <summary>Texture the greybox surfaces of the open DiabloZone scene (after all builders).</summary>
    public static void Apply()
    {
        EnsureAssets();
        int applied = 0;
        foreach (var go in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var rule = Match(go.gameObject.name);
            if (rule.surface == null) continue;
            var r = go.GetComponent<MeshRenderer>();
            if (r == null || !r.enabled || go.sharedMesh == null) continue;
            var mat = Load(rule.surface);
            if (mat == null) continue;
            go.sharedMesh = WorldUvMesh(go.sharedMesh, go.transform, rule.tile);
            r.sharedMaterial = mat;
            applied++;
        }
        Debug.Log($"[ZoneSurfaces] Textured {applied} surfaces.");
    }

    public static (string surface, float tile) Match(string objectName)
    {
        foreach (var (name, surface, tile) in Rules)
        {
            bool prefix = name.EndsWith("_");
            if (prefix ? objectName.StartsWith(name) : objectName == name) return (surface, tile);
        }
        return (null, 0f);
    }

    /// <summary>
    /// Copy of <paramref name="src"/> with UV0 in world metres / tile. Flat faces use box
    /// projection on the dominant world normal axis (V = world up on walls); round sides of
    /// built-in cylinders and spheres keep their seam-split UVs, scaled to whole repeats
    /// around the circumference so the texel size matches the flat faces.
    /// </summary>
    public static Mesh WorldUvMesh(Mesh src, Transform t, float tile)
    {
        var mesh = Object.Instantiate(src);
        mesh.name = src.name + "_WorldUV";
        var verts = mesh.vertices;
        var normals = mesh.normals;
        var uvs = mesh.uv;
        bool round = src.name == "Cylinder" || src.name == "Sphere" || src.name == "Capsule";
        var size = Vector3.Scale(src.bounds.size, t.lossyScale);
        float circumference = Mathf.PI * Mathf.Max(size.x, size.z);
        float repeatsU = Mathf.Max(1f, Mathf.Round(circumference / tile));
        float meridian = src.name == "Sphere" ? Mathf.PI * 0.5f * (size.x + size.y) * 0.5f : size.y;
        var result = new Vector2[verts.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            var wp = t.TransformPoint(verts[i]);
            var wn = t.TransformDirection(normals.Length == verts.Length ? normals[i] : Vector3.up).normalized;
            if (round && Mathf.Abs(wn.y) < 0.95f && uvs.Length == verts.Length)
            {
                result[i] = new Vector2(uvs[i].x * repeatsU, uvs[i].y * meridian / tile);
                continue;
            }
            float ax = Mathf.Abs(wn.x), ay = Mathf.Abs(wn.y), az = Mathf.Abs(wn.z);
            Vector2 p;
            if (ay >= ax && ay >= az) p = new Vector2(wp.x, wp.z);
            else if (ax >= az) p = new Vector2(wp.z, wp.y);
            else p = new Vector2(wp.x, wp.y);
            result[i] = p / tile;
        }
        mesh.uv = result;
        mesh.RecalculateTangents();
        return mesh;
    }

    /// <summary>Box UVs in mesh space (FBX props): same projection, no transform.</summary>
    public static void BoxUvInPlace(Mesh mesh, float tile)
    {
        var verts = mesh.vertices;
        var normals = mesh.normals;
        if (normals.Length != verts.Length) return;
        var uv = new Vector2[verts.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            var v = verts[i];
            var n = normals[i];
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
            Vector2 p;
            if (ay >= ax && ay >= az) p = new Vector2(v.x, v.z);
            else if (ax >= az) p = new Vector2(v.z, v.y);
            else p = new Vector2(v.x, v.y);
            uv[i] = p / tile;
        }
        mesh.uv = uv;
        mesh.RecalculateTangents();
    }
}

/// <summary>Blender props ship without a UV layout: project box UVs at import (1 m per repeat).</summary>
public class WorldPropUvPostprocessor : AssetPostprocessor
{
    public override uint GetVersion() => 1;

    void OnPostprocessModel(GameObject root)
    {
        if (!assetPath.StartsWith(WorldArtBuilder.Root + "/Models/")) return;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
            if (mf.sharedMesh != null) ZoneSurfaceBuilder.BoxUvInPlace(mf.sharedMesh, 1f);
    }
}
