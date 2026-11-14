using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Headless sandbox construction. Run:
///   Unity -batchmode -nographics -openProject sandbox -executeMethod SandboxBuilder.BuildAll -quit
/// </summary>
public static class SandboxBuilder
{
    const string ScenePath = "Assets/Scenes/Greybox.unity";

    public static void BuildAll()
    {
        UpgradeMaterials();
        BuildScene();
        BuildPlayer();
    }

    public static void UpgradeMaterials()
    {
        // Best effort: exact menu path varies by version; cosmetic only.
        bool ok = EditorApplication.ExecuteMenuItem(
            "Edit/Render Pipeline/Universal Render Pipeline/Upgrade Project Materials");
        Debug.Log($"[SandboxBuilder] Material upgrade menu executed: {ok}");
        AssetDatabase.SaveAssets();
    }

    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Greybox";

        // Ground.
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(5f, 1f, 5f);
        Paint(ground, new Color(0.45f, 0.45f, 0.45f));

        // Jump/flow blocks: varying heights in a loose line.
        float[] heights = { 0.5f, 1f, 1.5f, 2f, 1f, 0.5f };
        for (int i = 0; i < heights.Length; i++)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = $"Block_{i}";
            box.transform.position = new Vector3(-6f + i * 2.4f, heights[i] / 2f, -4f);
            box.transform.localScale = new Vector3(2f, heights[i], 2f);
            Paint(box, new Color(0.6f, 0.6f, 0.62f));
        }

        // Boundary walls.
        AddWall("Wall_N", new Vector3(0f, 1.5f, -25f), new Vector3(50f, 3f, 1f));
        AddWall("Wall_S", new Vector3(0f, 1.5f, 25f), new Vector3(50f, 3f, 1f));
        AddWall("Wall_E", new Vector3(25f, 1.5f, 0f), new Vector3(1f, 3f, 50f));
        AddWall("Wall_W", new Vector3(-25f, 1.5f, 0f), new Vector3(1f, 3f, 50f));

        // Light.
        var light = new GameObject("Directional Light");
        var dl = light.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.intensity = 1.1f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Camera (controller drives it at runtime).
        var cam = new GameObject("Main Camera");
        cam.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.transform.position = new Vector3(0f, 3f, -7f);

        // Player capsule at spawn.
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.1f, 6f);
        Paint(player, new Color(0.2f, 0.55f, 0.9f));
        player.AddComponent<CharacterController>();
        player.AddComponent<ThirdPersonController>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[SandboxBuilder] Saved {ScenePath}");
    }

    public static void BuildPlayer()
    {
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath }, // project-relative ("Assets/...")
            locationPathName = "/home/patrick/unity-sandbox/builds/greybox/greybox",
            target = BuildTarget.StandaloneLinux64,
            subtarget = (int)StandaloneBuildSubtarget.Player,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log($"[SandboxBuilder] Build result: {report.summary.result} " +
                  $"in {report.summary.totalTime.TotalSeconds:F1}s, " +
                  $"{report.summary.totalSize / 1048576} MB");
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("Player build failed; see log.");
    }

    static void AddWall(string name, Vector3 pos, Vector3 scale)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        Paint(wall, new Color(0.5f, 0.5f, 0.52f));
    }

    static void Paint(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            color = color
        };
        renderer.sharedMaterial = mat;
    }
}
