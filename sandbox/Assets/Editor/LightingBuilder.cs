#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// S1 light: generates the three zone <c>VolumeProfile</c> assets under
/// <c>Assets/Settings/Volumes/</c> from <c>ZoneAtmosphere.ForZone</c>, so the
/// baked assets and the headless-tested grade table cannot drift apart.
/// Menu: <c>Cinderreach/Lighting/Build Lighting</c>. No light baking, no
/// scene edits (builders regenerate scenes; S1 only hangs profiles).
/// </summary>
public static class LightingBuilder
{
    public const string VolumeDir = "Assets/Settings/Volumes";
    public const string LanternHoldPath = VolumeDir + "/LanternHold.asset";
    public const string AshenFenPath = VolumeDir + "/AshenFen.asset";
    public const string OssuaryPath = VolumeDir + "/Ossuary.asset";

    [MenuItem("Cinderreach/Lighting/Build Lighting")]
    public static void BuildLighting()
    {
        if (!AssetDatabase.IsValidFolder(VolumeDir))
            AssetDatabase.CreateFolder("Assets/Settings", "Volumes");
        // Lantern Hold (ActProgress.Town): warm 2700K lantern key-light, gentle bloom.
        WriteProfile(LanternHoldPath, ZoneAtmosphere.ForZone(ActProgress.Town));
        // Ashen Fen shares the town grade with a cooler pass: moonlit haze.
        var fen = ZoneAtmosphere.ForZone(ActProgress.Town);
        fen.temperature = -25f;
        fen.tint = -5f;
        fen.saturation = -30f;
        fen.exposure = -0.1f;
        fen.bloomIntensity = 0.3f;
        fen.bloomThreshold = 1.0f;
        fen.vignette = 0.3f;
        WriteProfile(AshenFenPath, fen);
        // Ossuary (+Kiln mood): near-dark, torch islands, vignette.
        WriteProfile(OssuaryPath, ZoneAtmosphere.ForZone(ActProgress.Ossuary));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Lighting] Built 3 zone volume profiles in " + VolumeDir);
    }

    /// <summary>Bake one profile asset from a grade (adds Bloom/Vignette/ColorAdjust/Fog mood).</summary>
    public static VolumeProfile WriteProfile(string path, ZoneAtmosphere.Grade g)
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
        }
        Set(profile, (Bloom b) => { b.intensity.Override(g.bloomIntensity); b.threshold.Override(g.bloomThreshold); });
        Set(profile, (Vignette v) => v.intensity.Override(g.vignette));
        Set(profile, (ColorAdjustments c) => { c.postExposure.Override(g.exposure); c.saturation.Override(g.saturation); });
        Set(profile, (WhiteBalance w) => { w.temperature.Override(g.temperature); w.tint.Override(g.tint); });
        // Fog mood lives on the scene (RenderSettings, exponential): Ashen Fen
        // cold-haze vs Ossuary torch-dark. Documented here so the builder that
        // owns the scene (SandboxBuilder) applies it; profiles stay post-only.
        EditorUtility.SetDirty(profile);
        return profile;
    }

    static void Set<T>(VolumeProfile profile, System.Action<T> init) where T : VolumeComponent
    {
        if (!profile.TryGet(out T comp))
        {
            comp = profile.Add<T>();
        }
        init(comp);
    }
}
#endif
