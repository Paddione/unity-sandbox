using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Baseline import rules for the asset pipeline (Goal unity-asset-pipeline-iteration).
///
/// Keeps altered visual and audio assets from regressing on reimport:
/// - Voice/music clips stay mono Vorbis (quality 0.6, CompressedInMemory), matching
///   docs/audio/manifest.json.
/// - Authored SFX clips under Assets/AddressableAssets/Sfx/ stay mono Vorbis
///   (quality 0.75), DecompressOnLoad for low-latency one-shots (CompressedInMemory
///   for Ambient* loops); missing SFX cues fall back to DiabloAudio runtime synth.
/// - Menu/Skill icons stay Sprites (Res.Get&lt;Sprite&gt; contract, see MenuArtImporter).
/// - New art under Assets/Art is capped at 2048px (no hi-res bloat in the repo);
///   color space stays the artist's choice (data maps like *Height/*Normal must
///   stay linear). ThirdParty/KayKit packs are excluded (KayKitAssets owns
///   their atlas/material contract); model material modes are owned per-pack
///   (copy KayKitAssets.Configure for new packs) and not forced here.
/// Runs at import time so external round-trips (Assistant Generator, local-AI audio)
/// cannot silently drift settings back.
/// </summary>
public class AssetPipelineImportRules : AssetPostprocessor
{
    public const string RulesVersion = "1.1";
    public const float VorbisQuality = 0.6f;
    public const float SfxVorbisQuality = 0.75f;

    void OnPreprocessAudio()
    {
        bool isSfx = IsSfx(assetPath);
        if (!isSfx && !IsVoiceOrMusic(assetPath)) return;
        var importer = (AudioImporter)assetImporter;
        bool dirty = false;
        if (!importer.forceToMono) { importer.forceToMono = true; dirty = true; }
        var settings = importer.defaultSampleSettings;
        float targetQuality = isSfx ? SfxVorbisQuality : VorbisQuality;
        AudioClipLoadType targetLoad = isSfx && !IsAmbientSfx(assetPath)
            ? AudioClipLoadType.DecompressOnLoad
            : AudioClipLoadType.CompressedInMemory;
        if (settings.compressionFormat != AudioCompressionFormat.Vorbis)
        { settings.compressionFormat = AudioCompressionFormat.Vorbis; dirty = true; }
        if (settings.loadType != targetLoad)
        { settings.loadType = targetLoad; dirty = true; }
        if (Math.Abs(settings.quality - targetQuality) > 0.001f)
        { settings.quality = targetQuality; dirty = true; }
        if (dirty)
        {
            importer.defaultSampleSettings = settings;
            string kind = isSfx ? "SFX" : "Audio";
            Debug.Log($"[AssetPipeline] {kind} rules applied ({RulesVersion}): {assetPath}");
        }
    }

    void OnPreprocessTexture()
    {
        if (IsThirdParty(assetPath)) return;
        var importer = (TextureImporter)assetImporter;
        if (IsUiSpriteFolder(assetPath))
        {
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
                Debug.Log($"[AssetPipeline] Sprite rules applied ({RulesVersion}): {assetPath}");
            }
            return;
        }
        if (IsArtFolder(assetPath))
        {
            if (importer.maxTextureSize > 2048)
            {
                importer.maxTextureSize = 2048;
                Debug.Log($"[AssetPipeline] Art texture size capped ({RulesVersion}): {assetPath}");
            }
        }
    }

    static bool IsSfx(string path) =>
        path.StartsWith("Assets/AddressableAssets/Sfx/", StringComparison.Ordinal);

    static bool IsAmbientSfx(string path)
    {
        int slash = path.LastIndexOf('/');
        string file = slash >= 0 ? path.Substring(slash + 1) : path;
        return file.StartsWith("Ambient", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsVoiceOrMusic(string path) =>
        path.StartsWith("Assets/AddressableAssets/Voice/", StringComparison.Ordinal)
        || path.StartsWith("Assets/AddressableAssets/Music/", StringComparison.Ordinal)
        || path.StartsWith("Assets/StreamingAssets/", StringComparison.Ordinal);

    static bool IsUiSpriteFolder(string path) =>
        path.StartsWith("Assets/AddressableAssets/MenuArt/", StringComparison.Ordinal)
        || path.StartsWith("Assets/AddressableAssets/SkillArt/", StringComparison.Ordinal);

    static bool IsArtFolder(string path) =>
        path.StartsWith("Assets/Art/", StringComparison.Ordinal);

    static bool IsThirdParty(string path) =>
        path.StartsWith("Assets/ThirdParty/", StringComparison.Ordinal)
        || path.StartsWith("Packages/", StringComparison.Ordinal);
}
