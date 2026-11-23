using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Dresses the Ashen Fen with the Blender-built props from
/// docs/world-art/build_world_props.py (FBX under Assets/Art/BloodMoor/Models).
/// Imports map the FBX material names onto URP/Lit materials in the art bible
/// palette; the greybox primitives keep their names, positions and colliders
/// (tests and gameplay read them) and only hand their look to a prop.
/// Decor scatter is seeded, so rebuilding the scene is deterministic.
/// </summary>
public static class WorldArtBuilder
{
    public const string Root = "Assets/Art/BloodMoor";
    const string Models = Root + "/Models";
    const string Materials = Root + "/Materials";

    /// <summary>FBX material name → (albedo sRGB, smoothness, emission).</summary>
    static readonly Dictionary<string, (Color albedo, float smooth, Color emission)> Look =
        new Dictionary<string, (Color, float, Color)>
        {
            { "Bark", (Hex("#4A3220"), 0.1f, Color.black) },
            { "DarkWood", (Hex("#5A3A22"), 0.15f, Color.black) },
            { "Stone", (Hex("#5A5560"), 0.12f, Color.black) },
            { "Moss", (Hex("#46592E"), 0.05f, Color.black) },
            { "Bone", (Hex("#D8CCB0"), 0.3f, Color.black) },
            { "Iron", (Hex("#3A3A40"), 0.55f, Color.black) },
            { "Flame", (Hex("#FF7A1A"), 0.0f, Hex("#FF5A00") * 2.6f) },
            { "Coal", (Hex("#5A1200"), 0.0f, Hex("#C02800") * 1.4f) },
            { "BloodCloth", (Hex("#8E1620"), 0.1f, Color.black) },
            { "Gold", (Hex("#F2B134"), 0.6f, Color.black) },
            { "Heather", (Hex("#55603A"), 0.05f, Color.black) },
            { "HeatherBloom", (Hex("#8A4A7E"), 0.1f, Color.black) },
            { "Grass", (Hex("#66733A"), 0.05f, Color.black) },
            { "Rune", (Hex("#C0180F"), 0.2f, Hex("#FF2A1A") * 2.2f) },
            { "Blood", (Hex("#3E0507"), 0.78f, Color.black) },
        };

    public static readonly string[] Props =
    {
        "Campfire", "Stake_0", "Stake_1", "Stake_2", "Rock_0", "Rock_1", "Rock_2",
        "DeadTree_0", "DeadTree_1", "Gravestone_0", "Gravestone_1", "BonePile",
        "BrokenPillar", "Brazier", "Banner", "Heather_0", "Heather_1", "Grass_0",
        "StoneWall_0", "StoneWall_1", "StoneStep", "CryptSeal", "BloodSplat_0", "BloodSplat_1",
    };

    [MenuItem("Sandbox/World Art/Import Ashen Fen Props")]
    public static void EnsureAssets()
    {
        Directory.CreateDirectory(Materials);
        // Textured surfaces (ZoneSurfaceBuilder) win; flat palette materials stay for
        // glow, metal accents and foliage that have no texture set.
        ZoneSurfaceBuilder.EnsureAssets();
        var mats = new Dictionary<string, Material>();
        foreach (var kv in Look)
            mats[kv.Key] = ZoneSurfaceBuilder.Load(kv.Key)
                ?? EnsureMaterial(kv.Key, kv.Value.albedo, kv.Value.smooth, kv.Value.emission);

        foreach (var prop in Props)
        {
            string path = $"{Models}/{prop}.fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[WorldArt] Missing {path}; rerun docs/world-art/build_world_props.py.");
                continue;
            }
            importer.importCameras = false;
            importer.importLights = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            foreach (var kv in mats)
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), kv.Key), kv.Value);
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
    }

    static Material EnsureMaterial(string name, Color albedo, float smooth, Color emission)
    {
        string path = $"{Materials}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_BaseColor", albedo);
        m.SetFloat("_Smoothness", smooth);
        m.enableInstancing = true;
        if (emission.maxColorComponent > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else m.DisableKeyword("_EMISSION");
        EditorUtility.SetDirty(m);
        return m;
    }

    // ------------------------------------------------------------------ scene

    /// <summary>Dress the open DiabloZone scene (call after the greybox is built).</summary>
    public static void DressDiabloZone()
    {
        EnsureAssets();
        var root = new GameObject("WorldDressing").transform;
        var rng = new System.Random(1337);

        // Ground texture: ZoneSurfaceBuilder.Apply (FenMoor) after all builders.

        // Greybox holders keep name/position/collider, hand their look to a prop.
        int i = 0;
        foreach (var go in FindByPrefix("Palisade_"))
            Dress(go, $"Stake_{i++ % 3}", (float)rng.NextDouble() * 360f, 1f, root);
        i = 0;
        foreach (var go in FindByPrefix("MoorRock_"))
            Dress(go, $"Rock_{i++ % 3}", (float)rng.NextDouble() * 360f, 0.9f, root);
        foreach (var go in FindByPrefix("ZRubble_"))
            Dress(go, $"Rock_{i++ % 3}", (float)rng.NextDouble() * 360f, 0.8f, root);
        foreach (var go in FindByPrefix("ZGatePillar_"))
            Dress(go, "BrokenPillar", go.transform.position.x < 0 ? 15f : 200f, 1.15f, root);

        var edgeRng = new System.Random(4242); // own seed: the decor scatter below stays put

        // Moor edge: 4 m drystone segments replace the 40 m boundary cubes (colliders stay).
        foreach (var edge in FindByPrefix("MoorEdge_"))
        {
            Hide(edge);
            var t = edge.transform;
            bool alongX = t.localScale.x > t.localScale.z;
            float length = alongX ? t.localScale.x : t.localScale.z;
            int segments = Mathf.RoundToInt(length / 4f);
            string side = edge.name.Substring("MoorEdge_".Length);
            for (int s = 0; s < segments; s++)
            {
                float along = -length / 2f + 2f + s * 4f;
                var at = Flat(t.position) + (alongX ? new Vector3(along, 0f, 0f) : new Vector3(0f, 0f, along));
                Place($"StoneWall_{(s + side.Length) % 2}", at, (alongX ? 0f : 90f) + Jitter(edgeRng, 2.5f),
                    1f, root, $"MoorWall_{side}_{s}");
            }
        }

        // Entry steps: stone blocks scaled to each step's top (holders keep the walkable boxes).
        foreach (var step in FindByPrefix("ZStep_"))
        {
            Hide(step);
            var t = step.transform;
            float top = t.position.y + t.localScale.y / 2f;
            var model = Place("StoneStep", Flat(t.position), step.name == "ZStep_1" ? 180f : 0f, 1f, root,
                "Art_" + step.name);
            model.transform.localScale = new Vector3(t.localScale.x / 4f, top, t.localScale.z / 1.2f);
        }

        // Crypt gate: rune seal on the extraction pad, lit blood-red from within.
        var pad = GameObject.Find("GoalPad");
        if (pad != null)
        {
            Hide(pad);
            var seal = Place("CryptSeal", Flat(pad.transform.position), 0f, 1f, root, "CryptSeal");
            var glow = new GameObject("SealGlow");
            glow.transform.SetParent(seal.transform, false);
            glow.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            var l = glow.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.16f, 0.1f);
            l.range = 4.5f;
            l.intensity = 1.6f;
            l.shadows = LightShadows.None;
        }

        // Blood patches: wet splat meshes instead of flat red cubes.
        i = 0;
        foreach (var stain in FindByPrefix("BloodPatch_"))
            Dress(stain, $"BloodSplat_{i++ % 2}", Jitter(edgeRng, 180f), 1f, root);

        // Campfire: one Blender model replaces base, ember cube and logs.
        var fire = GameObject.Find("Campfire");
        if (fire != null)
        {
            var at = fire.transform.position;
            foreach (var part in new[] { "Campfire", "CampfireBase" })
                Hide(GameObject.Find(part));
            foreach (var log in FindByPrefix("CampfireLog_")) Hide(log);
            var model = Place("Campfire", new Vector3(at.x, 0f, at.z), 0f, 1.3f, root, "CampfireModel");
            AddFire(model.transform, new Vector3(0f, 0.9f, 0f), 7f, 2.6f, 7f);
        }
        foreach (var torch in FindByPrefix("ZTorch_"))
            AddFire(torch.transform, new Vector3(0f, 1.6f, 0f), 5f, 1.6f, 3f);

        // Crypt gate braziers and camp banners.
        foreach (var x in new[] { -3.6f, 3.6f })
        {
            var b = Place("Brazier", new Vector3(x, 0f, -5.2f), 0f, 1.1f, root, $"GateBrazier_{(x < 0 ? 0 : 1)}");
            AddCollider(b, 0.35f, 1.3f);
            AddFire(b.transform, new Vector3(0f, 1.8f, 0f), 6f, 2.2f, 5f);
        }
        foreach (var x in new[] { -2.4f, 2.4f })
            Place("Banner", new Vector3(x, 0f, 5.4f), x < 0 ? 10f : -10f, 1f, root, $"CampBanner_{(x < 0 ? 0 : 1)}");
        Place("Banner", new Vector3(-12f, 0f, -4.2f), 0f, 1f, root, "AlcoveBanner");
        foreach (var z in new[] { -4f, 4f })
        {
            var b = Place("Brazier", new Vector3(-9.5f, 0f, z), 0f, 1f, root, $"AlcoveBrazier_{(z < 0 ? 0 : 1)}");
            AddCollider(b, 0.35f, 1.3f);
            AddFire(b.transform, new Vector3(0f, 1.7f, 0f), 5f, 1.8f, 4f);
        }

        // Graveyard in the northeast corner, ruins northwest.
        var graves = new[]
        {
            new Vector3(11.5f, 0f, -11f), new Vector3(13.2f, 0f, -11.4f), new Vector3(14.8f, 0f, -10.8f),
            new Vector3(12.0f, 0f, -13.3f), new Vector3(13.8f, 0f, -13.6f), new Vector3(15.3f, 0f, -13.1f),
            new Vector3(-2.8f, 0f, -9.5f), new Vector3(2.9f, 0f, -9.8f),
        };
        for (int g = 0; g < graves.Length; g++)
        {
            var gs = Place($"Gravestone_{g % 2}", graves[g], 180f + Jitter(rng, 12f), 1.1f, root, $"Grave_{g}");
            AddBox(gs, new Vector3(0.7f, 1.2f, 0.4f));
        }
        foreach (var p in new[] { new Vector3(-12.5f, 0f, -11f), new Vector3(-14.5f, 0f, -8.5f), new Vector3(-10.5f, 0f, -14f) })
            AddBox(Place("BrokenPillar", p, Jitter(rng, 180f), 1f + (float)rng.NextDouble() * 0.3f, root, "RuinPillar"),
                new Vector3(0.9f, 2.5f, 0.9f));

        // Dead trees along the moor edge.
        var trees = new[]
        {
            new Vector3(-15f, 0f, -15f), new Vector3(-8f, 0f, -16f), new Vector3(7f, 0f, -16f),
            new Vector3(16f, 0f, -4f), new Vector3(15.5f, 0f, 5f), new Vector3(-15.5f, 0f, 3f),
            new Vector3(-14f, 0f, 14f), new Vector3(13.5f, 0f, 15f), new Vector3(9f, 0f, 9f),
            new Vector3(-9.5f, 0f, 8.5f),
        };
        for (int t = 0; t < trees.Length; t++)
            AddCollider(Place($"DeadTree_{t % 2}", trees[t], Jitter(rng, 180f), 0.9f + (float)rng.NextDouble() * 0.4f,
                root, $"DeadTree_{t}"), 0.35f, 3f);

        // Bones and moor plants (no colliders: walk-through decor).
        for (int b = 0; b < 9; b++)
            Place("BonePile", RandomMoorSpot(rng), Jitter(rng, 180f), 1f, root, $"Bones_{b}");
        for (int h = 0; h < 150; h++)
        {
            string kind = h % 5 == 0 ? "Grass_0" : (h % 2 == 0 ? "Heather_0" : "Heather_1");
            Place(kind, RandomMoorSpot(rng), Jitter(rng, 180f), 0.8f + (float)rng.NextDouble() * 0.7f, root, $"Tuft_{h}");
        }
        // Heather hugging the palisade and rocks makes them sit in the ground.
        foreach (var go in FindByPrefix("MoorRock_"))
            for (int k = 0; k < 3; k++)
            {
                var off = new Vector3(Jitter(rng, 1.1f), 0f, Jitter(rng, 1.1f));
                Place("Heather_1", Flat(go.transform.position) + off, Jitter(rng, 180f), 1f, root, "RockTuft");
            }

        // Stair-flank dressing: pebbles + tufts hugging the entry steps so
        // the arrival reads detailed up close. Additive pass with its own
        // seed (leaves the main scatter stream untouched), no colliders.
        var detailRng = new System.Random(4242);
        int detail = 0;
        foreach (var side in new[] { -1f, 1f })
            for (int k = 0; k < 6; k++)
            {
                float x = side * (2.6f + (float)detailRng.NextDouble() * 1.1f);
                float z = 9.4f + (float)detailRng.NextDouble() * 3.4f;
                Place("Rock_0", new Vector3(x, 0.05f, z), (float)detailRng.NextDouble() * 360f,
                    0.22f + (float)detailRng.NextDouble() * 0.2f, root, $"FenDetail_Pebble_{detail++}");
            }
        for (int k = 0; k < 10; k++)
        {
            float x = (detailRng.Next(2) == 0 ? -1f : 1f) * (2.4f + (float)detailRng.NextDouble() * 1.6f);
            float z = 9.2f + (float)detailRng.NextDouble() * 4f;
            string kind = k % 3 == 0 ? "Grass_0" : (k % 3 == 1 ? "Heather_0" : "Heather_1");
            Place(kind, new Vector3(x, 0f, z), (float)detailRng.NextDouble() * 360f,
                0.8f + (float)detailRng.NextDouble() * 0.5f, root, $"FenDetail_Tuft_{k}");
        }

        // Atmosphere: moor haze, cool moon key so the fires read warm.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.10f, 0.11f, 0.14f);
        RenderSettings.fogStartDistance = 16f;
        RenderSettings.fogEndDistance = 42f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.30f, 0.31f, 0.38f);
        var sun = GameObject.Find("Directional Light");
        if (sun != null)
        {
            var l = sun.GetComponent<Light>();
            l.color = new Color(0.82f, 0.86f, 1f);
            l.intensity = 0.95f;
            l.shadows = LightShadows.Soft;
        }
        // Beyond the fen wall: night haze instead of the unfogged default skybox.
        var cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            var c = cam.GetComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor;
            c.backgroundColor = RenderSettings.fogColor;
        }
    }

    public static GameObject Place(string prop, Vector3 pos, float yaw, float scale, Transform parent, string name)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Models}/{prop}.fbx");
        if (asset == null) return new GameObject(name);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = Vector3.one * scale;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        return go;
    }

    static void Dress(GameObject holder, string prop, float yaw, float scale, Transform parent)
    {
        Hide(holder);
        Place(prop, Flat(holder.transform.position), yaw, scale, parent, "Art_" + holder.name);
    }

    static void Hide(GameObject go)
    {
        if (go == null) return;
        var r = go.GetComponent<Renderer>();
        if (r != null) r.enabled = false;
    }

    static void AddFire(Transform parent, Vector3 local, float range, float intensity, float embers)
    {
        var go = new GameObject("FireLight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.58f, 0.25f);
        l.range = range;
        l.intensity = intensity;
        l.shadows = LightShadows.None;
        var f = go.AddComponent<DiabloFire>();
        f.baseIntensity = intensity;
        f.embersPerSecond = embers;
    }

    static void AddCollider(GameObject go, float radius, float height)
    {
        var c = go.AddComponent<CapsuleCollider>();
        c.radius = radius / Mathf.Max(0.01f, go.transform.localScale.x);
        c.height = height / Mathf.Max(0.01f, go.transform.localScale.y);
        c.center = new Vector3(0f, c.height / 2f, 0f);
    }

    static void AddBox(GameObject go, Vector3 size)
    {
        var c = go.AddComponent<BoxCollider>();
        var s = go.transform.localScale;
        c.size = new Vector3(size.x / s.x, size.y / s.y, size.z / s.z);
        c.center = new Vector3(0f, c.size.y / 2f, 0f);
    }

    /// <summary>A fen spot outside the camp ring, gate pad, alcove and arena paths.</summary>
    static Vector3 RandomMoorSpot(System.Random rng)
    {
        for (int tries = 0; tries < 40; tries++)
        {
            var p = new Vector3(Jitter(rng, 16.5f), 0f, Jitter(rng, 16.5f));
            if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(0f, 11f)) < 6.8f) continue; // camp
            if (Mathf.Abs(p.x) < 3f && p.z > -8f && p.z < -4f) continue; // crypt gate pad
            if (Mathf.Abs(p.x + 12f) < 3.2f && Mathf.Abs(p.z) < 3.2f) continue; // alcove tiles
            return p;
        }
        return new Vector3(15f, 0f, -15f);
    }

    static IEnumerable<GameObject> FindByPrefix(string prefix)
    {
        var list = new List<GameObject>();
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (go.name.StartsWith(prefix)) list.Add(go);
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return list;
    }

    static float Jitter(System.Random rng, float range) => ((float)rng.NextDouble() * 2f - 1f) * range;

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }
}
