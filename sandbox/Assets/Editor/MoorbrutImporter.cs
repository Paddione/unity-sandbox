using UnityEditor;
using UnityEngine;

public sealed class MoorbrutImporter : AssetPostprocessor
{
    bool IsMoorbrut => assetPath.StartsWith("Assets/Art/Moorbrut/") && assetPath.EndsWith(".fbx");

    void OnPreprocessModel()
    {
        if (!IsMoorbrut) return;
        bool runner = assetPath.EndsWith("Glutlaeufer.fbx");
        var model = (ModelImporter)assetImporter;
        model.preserveHierarchy = true;
        model.animationType = ModelImporterAnimationType.Legacy;
        model.clipAnimations = new[]
        {
            new ModelImporterClipAnimation { name = "Walk", takeName = "Scene", firstFrame = 0,
                lastFrame = runner ? 12 : 48, wrapMode = WrapMode.Loop, loopTime = true },
            new ModelImporterClipAnimation { name = "Strike", takeName = "Scene", firstFrame = 49,
                lastFrame = runner ? 81 : 121, wrapMode = WrapMode.Once }
        };
    }

    void OnPostprocessModel(GameObject root)
    {
        if (IsMoorbrut && root.TryGetComponent<Animation>(out var animation))
            animation.playAutomatically = false;
    }

    void OnPostprocessMaterial(Material material)
    {
        if (!IsMoorbrut) return;
        var color = material.color;
        material.shader = Shader.Find("Universal Render Pipeline/Lit");
        material.color = color;
    }
}
