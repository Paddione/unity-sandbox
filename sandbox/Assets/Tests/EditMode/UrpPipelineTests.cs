using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UrpPipelineTests
{
    [Test]
    public void DefaultRenderPipeline_IsUniversalRenderPipeline()
    {
        var rp = GraphicsSettings.defaultRenderPipeline;
        Assert.IsNotNull(rp, "GraphicsSettings.defaultRenderPipeline must be assigned.");
        Assert.IsInstanceOf<UniversalRenderPipelineAsset>(rp,
            "Default render pipeline must be UniversalRenderPipelineAsset (URP only).");
    }

    [Test]
    public void BuildSettings_HasGreyboxSceneOnly()
    {
        var scenes = EditorBuildSettings.scenes;
        Assert.IsNotEmpty(scenes, "EditorBuildSettings.scenes must not be empty.");
        Assert.That(scenes[0].path, Is.EqualTo("Assets/Scenes/Greybox.unity"));
        foreach (var s in scenes)
            Assert.That(s.path, Does.Not.Contain("SampleScene"),
                "Template SampleScene leftover must not remain in build settings.");
    }

    [Test]
    public void GreyboxScene_UsesOnlyUrpLitMaterials()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Greybox.unity", OpenSceneMode.Single);
        Assert.IsTrue(scene.IsValid(), "Greybox.unity must open cleanly.");

        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        Assert.IsNotEmpty(renderers, "Greybox scene should contain renderers.");

        foreach (var rend in renderers)
        {
            foreach (var mat in rend.sharedMaterials)
            {
                Assert.IsNotNull(mat, $"Renderer '{rend.name}' has a null material.");
                Assert.IsNotNull(mat.shader, $"Material '{mat.name}' on '{rend.name}' has a null shader.");
                Assert.That(mat.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"),
                    $"Renderer '{rend.name}' uses '{mat.shader.name}' instead of 'Universal Render Pipeline/Lit'.");
            }
        }
    }
}
