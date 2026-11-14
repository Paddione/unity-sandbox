using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;

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

        // Q2 trigger toy: walk-through counter volume over the block line.
        var zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        zone.name = "TriggerZone";
        zone.transform.position = new Vector3(0f, 1.2f, -4f);
        zone.transform.localScale = new Vector3(4f, 2.4f, 3f);
        Paint(zone, new Color(0.95f, 0.55f, 0.15f));
        var zoneCol = zone.GetComponent<BoxCollider>();
        zoneCol.isTrigger = true;
        var counter = zone.AddComponent<ToyTrigger>();
        counter.mode = ToyTrigger.Mode.Counter;

        // Q3 toy-vs-goal loop: 3 pickup orbs, then the goal pad arms.
        Vector3[] orbs =
        {
            new Vector3(-3.6f, 1.6f, -4f),
            new Vector3(1.2f, 2.6f, -4f),
            new Vector3(3.6f, 2.1f, -4f),
        };
        for (int i = 0; i < orbs.Length; i++)
        {
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = $"Orb_{i}";
            orb.transform.position = orbs[i];
            orb.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
            Paint(orb, new Color(1f, 0.85f, 0.2f));
            orb.GetComponent<SphereCollider>().isTrigger = true;
            var pickup = orb.AddComponent<ToyTrigger>();
            pickup.mode = ToyTrigger.Mode.Pickup;
        }

        var goal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        goal.name = "GoalPad";
        goal.transform.position = new Vector3(0f, 0.1f, 12f);
        goal.transform.localScale = new Vector3(3f, 0.2f, 3f);
        Paint(goal, new Color(0.8f, 0.25f, 0.25f));
        goal.GetComponent<BoxCollider>().isTrigger = true;
        var goalToy = goal.AddComponent<ToyTrigger>();
        goalToy.mode = ToyTrigger.Mode.Goal;

        BuildNetworkLayer();

        ToyTrigger.ResetRun();

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[SandboxBuilder] Saved {ScenePath}");
    }

    /// <summary>
    /// Headless proof: open the greybox scene and log the mechanics
    /// inventory (Q1 rig, Q2 trigger, Q3 pickups + goal). Run:
    ///   Unity -batchmode -nographics -openProject sandbox
    ///     -executeMethod SandboxBuilder.VerifyGreybox -quit
    /// </summary>
    public static void VerifyGreybox()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        int blocks = 0, walls = 0, orbs = 0, goals = 0, zones = 0;
        bool player = false, controller = false;
        foreach (var root in scene.GetRootGameObjects())
            CountRecursive(root.transform, ref blocks, ref walls, ref orbs,
                ref goals, ref zones, ref player, ref controller);
        Debug.Log("[SandboxBuilder] Verify: " +
                  $"player={player} controller={controller} " +
                  $"blocks={blocks} walls={walls} " +
                  $"triggerZones={zones} orbs={orbs} goalPads={goals}");
        if (!player || !controller || blocks < 6 || walls < 4
            || zones < 1 || orbs < 3 || goals < 1)
            throw new System.Exception("Greybox verify failed: mechanics inventory incomplete.");
        Debug.Log("[SandboxBuilder] Verify: OK (Q1 rig + Q2 trigger + Q3 toy/goal present).");
    }

    static void CountRecursive(Transform t, ref int blocks, ref int walls,
        ref int orbs, ref int goals, ref int zones,
        ref bool player, ref bool controller)
    {
        string n = t.gameObject.name;
        if (n.StartsWith("Block_")) blocks++;
        if (n.StartsWith("Wall_")) walls++;
        if (n.StartsWith("Orb_")) orbs++;
        if (n == "GoalPad") goals++;
        if (n == "TriggerZone") zones++;
        if (n == "Player")
        {
            player = true;
            controller = t.gameObject.GetComponent<ThirdPersonController>() != null;
        }
        foreach (Transform c in t) CountRecursive(c, ref blocks, ref walls,
            ref orbs, ref goals, ref zones, ref player, ref controller);
    }

    /// <summary>
    /// Q4 co-op layer: NetworkManager + UnityTransport (127.0.0.1:7777),
    /// networked player prefab (owner-only controller), ParrelSync-friendly
    /// auto-starter that hosts in the main editor and connects as client
    /// in clones. Solo Player stays untouched for single-player verify.
    /// </summary>
    public static void BuildNetworkLayer()
    {
        // Networked player prefab (saved once, reused on rebuilds).
        var netPlayer = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        netPlayer.name = "NetworkPlayer";
        netPlayer.transform.position = new Vector3(0f, 1.1f, 6f);
        Paint(netPlayer, new Color(0.25f, 0.6f, 0.95f));
        Object.DestroyImmediate(netPlayer.GetComponent<Collider>());
        netPlayer.AddComponent<CharacterController>();
        netPlayer.AddComponent<NetworkObject>();
        netPlayer.AddComponent<NetworkTransform>();
        netPlayer.AddComponent<ThirdPersonController>();
        const string prefabPath = "Assets/Resources/NetworkPlayer.prefab";
        System.IO.Directory.CreateDirectory("Assets/Resources");
        PrefabUtility.SaveAsPrefabAsset(netPlayer, prefabPath);
        Object.DestroyImmediate(netPlayer);
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        var nmGo = new GameObject("NetworkManager");
        var nm = nmGo.AddComponent<NetworkManager>();
        var transport = nmGo.AddComponent<UnityTransport>();
        transport.SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
        nm.NetworkConfig.NetworkTransport = transport;
        nmGo.AddComponent<MultiplayerAutoStarter>();
        nm.NetworkConfig.PlayerPrefab = playerPrefab;
        nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = playerPrefab });
        Debug.Log("[SandboxBuilder] Network layer: NetworkManager + UnityTransport 127.0.0.1:7777 + NetworkPlayer prefab.");
    }

    /// <summary>
    /// Headless proof for the Q4 co-op layer. Run after BuildScene:
    ///   Unity -batchmode -nographics -projectPath sandbox
    ///     -executeMethod SandboxBuilder.VerifyMultiplayer -quit
    /// </summary>
    public static void VerifyMultiplayer()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        NetworkManager nm = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            nm = root.GetComponentInChildren<NetworkManager>(true);
            if (nm != null) break;
        }
        if (nm == null)
            throw new System.Exception("Multiplayer verify failed: no NetworkManager in scene.");
        if (nm.GetComponent<UnityTransport>() == null)
            throw new System.Exception("Multiplayer verify failed: no UnityTransport on NetworkManager.");
        if (nm.GetComponent<MultiplayerAutoStarter>() == null)
            throw new System.Exception("Multiplayer verify failed: no MultiplayerAutoStarter on NetworkManager.");
        if (nm.NetworkConfig.NetworkTransport == null)
            throw new System.Exception("Multiplayer verify failed: NetworkConfig.NetworkTransport not assigned.");
        if (nm.NetworkConfig.PlayerPrefab == null)
            throw new System.Exception("Multiplayer verify failed: NetworkManager has no PlayerPrefab.");
        var prefab = nm.NetworkConfig.PlayerPrefab;
        if (prefab.GetComponent<NetworkObject>() == null)
            throw new System.Exception("Multiplayer verify failed: NetworkPlayer prefab has no NetworkObject.");
        if (prefab.GetComponent<ThirdPersonController>() == null)
            throw new System.Exception("Multiplayer verify failed: NetworkPlayer prefab has no ThirdPersonController.");
        if (!MultiplayerAutoStarter.IsClonePath("/proj/sandbox_clone_N/Assets"))
            throw new System.Exception("Multiplayer verify failed: clone-path detection broken.");
        if (MultiplayerAutoStarter.IsClonePath("/proj/sandbox/Assets"))
            throw new System.Exception("Multiplayer verify failed: main path misdetected as clone.");
        Debug.Log("[SandboxBuilder] Verify: OK (NetworkManager + transport + auto-starter + NetworkPlayer prefab).");
    }

    /// <summary>Dedicated-server build (no rendering) for hosts like gpu-metal.</summary>
    public static void BuildServer()
    {
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "/home/patrick/unity-sandbox/builds/greybox-server/greybox-server",
            target = BuildTarget.StandaloneLinux64,
            subtarget = (int)StandaloneBuildSubtarget.Server,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log($"[SandboxBuilder] Server build result: {report.summary.result} " +
                  $"in {report.summary.totalTime.TotalSeconds:F1}s, " +
                  $"{report.summary.totalSize / 1048576} MB");
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("Server build failed; see log.");
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
