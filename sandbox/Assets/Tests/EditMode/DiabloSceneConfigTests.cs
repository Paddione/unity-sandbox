using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Q5 Diablo scene: hero rig, husks, goal pad, room, URP materials.</summary>
public class DiabloSceneConfigTests
{
    const string DiabloPath = "Assets/Scenes/Diablo.unity";

    [Test]
    public void BuildSettings_KeepsGreyboxFirstAndAddsDiablo()
    {
        var scenes = EditorBuildSettings.scenes;
        Assert.IsNotEmpty(scenes, "EditorBuildSettings.scenes must not be empty.");
        Assert.That(scenes[0].path, Is.EqualTo("Assets/Scenes/Greybox.unity"),
            "Greybox must stay first (UrpPipelineTests pins scenes[0]).");
        Assert.That(scenes, Has.Some.Matches<EditorBuildSettingsScene>(s => s.path == DiabloPath),
            "Diablo.unity must be in build settings.");
    }

    [Test]
    public void DiabloScene_HasHeroHusksAndGoal()
    {
        var scene = EditorSceneManager.OpenScene(DiabloPath, OpenSceneMode.Single);
        Assert.IsTrue(scene.IsValid(), "Diablo.unity must open cleanly.");

        var player = GameObject.Find("Player");
        Assert.IsNotNull(player, "Diablo scene must contain the Player hero.");
        Assert.IsNotNull(player.GetComponent<CharacterController>(), "Hero needs CharacterController.");
        Assert.IsNotNull(player.GetComponent<DiabloClickController>(), "Hero needs DiabloClickController.");
        Assert.IsNotNull(player.GetComponent<DiabloMelee>(), "Hero needs DiabloMelee.");
        Assert.IsNotNull(
            player.GetComponentInChildren<Renderer>(),
            "Hero must render a KayKit figure (no bare capsule).");

        var husks = GameObject.FindGameObjectsWithTag("Husk");
        Assert.AreEqual(3, husks.Length, "Diablo scene must contain exactly 3 tagged husks.");
        foreach (var husk in husks)
            Assert.IsNotNull(husk.GetComponent<EnemyHealth>(),
                $"Husk '{husk.name}' needs EnemyHealth.");

        var goal = GameObject.Find("GoalPad");
        Assert.IsNotNull(goal, "Diablo scene must contain the GoalPad extraction.");
        var toy = goal.GetComponent<ToyTrigger>();
        Assert.IsNotNull(toy, "GoalPad needs ToyTrigger.");
        Assert.AreEqual(ToyTrigger.Mode.Goal, toy.mode, "GoalPad must run in Goal mode.");
    }

    [Test]
    public void DiabloScene_UsesOnlyUrpLitMaterials()
    {
        var scene = EditorSceneManager.OpenScene(DiabloPath, OpenSceneMode.Single);
        Assert.IsTrue(scene.IsValid(), "Diablo.unity must open cleanly.");

        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        Assert.IsNotEmpty(renderers, "Diablo scene should contain renderers.");

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
