using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Baseline import rules for the asset pipeline (Goal unity-asset-pipeline-iteration).
///
/// Keeps altered visual and audio assets from regressing on reimport:
/// - Voice/music clips stay mono Vorbis (quality 0.6), matching docs/audio/manifest.json
///   (22050 Hz mono sources, oggQuality 0.6, budget &lt;= 60MB). SFX need no files:
///   DiabloAudio synthesizes them at runtime.
/// - Menu/Skill icons stay Sprites (Resources.Load&lt;Sprite&gt; contract, see MenuArtImporter).
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
    public const string RulesVersion = "1.0";
    public const float VorbisQuality = 0.6f;

    void OnPreprocessAudio()
    {
        if (!IsVoiceOrMusic(assetPath)) return;
        var importer = (AudioImporter)assetImporter;
        bool dirty = false;
        if (!importer.forceToMono) { importer.forceToMono = true; dirty = true; }
        var settings = importer.defaultSampleSettings;
        if (settings.compressionFormat != AudioCompressionFormat.Vorbis)
        { settings.compressionFormat = AudioCompressionFormat.Vorbis; dirty = true; }
        if (settings.loadType != AudioClipLoadType.CompressedInMemory)
        { settings.loadType = AudioClipLoadType.CompressedInMemory; dirty = true; }
        if (Math.Abs(settings.quality - VorbisQuality) > 0.001f)
        { settings.quality = VorbisQuality; dirty = true; }
        if (dirty)
        {
            importer.defaultSampleSettings = settings;
            Debug.Log($"[AssetPipeline] Audio rules applied ({RulesVersion}): {assetPath}");
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

    static bool IsVoiceOrMusic(string path) =>
        path.StartsWith("Assets/Resources/Voice/", StringComparison.Ordinal)
        || path.StartsWith("Assets/Resources/Music/", StringComparison.Ordinal)
        || path.StartsWith("Assets/StreamingAssets/", StringComparison.Ordinal);

    static bool IsUiSpriteFolder(string path) =>
        path.StartsWith("Assets/Resources/MenuArt/", StringComparison.Ordinal)
        || path.StartsWith("Assets/Resources/SkillArt/", StringComparison.Ordinal);

    static bool IsArtFolder(string path) =>
        path.StartsWith("Assets/Art/", StringComparison.Ordinal);

    static bool IsThirdParty(string path) =>
        path.StartsWith("Assets/ThirdParty/", StringComparison.Ordinal)
        || path.StartsWith("Packages/", StringComparison.Ordinal);
}
