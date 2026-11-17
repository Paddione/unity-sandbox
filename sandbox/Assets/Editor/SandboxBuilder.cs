using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
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
        // Same owner-authority fix as the DiabloZone prefab (no snap-back).
        netPlayer.AddComponent<NetworkTransform>().AuthorityMode = NetworkTransform.AuthorityModes.Owner;
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
        nmGo.AddComponent<ConnectionStatusLogger>();
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
        if (nm.GetComponent<ConnectionStatusLogger>() == null)
            throw new System.Exception("Multiplayer verify failed: no ConnectionStatusLogger on NetworkManager.");
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

    const string DiabloScenePath = "Assets/Scenes/Diablo.unity";

    static GameObject LoadModel(string relativePath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ThirdParty/KayKit/" + relativePath);
        if (prefab == null)
            throw new System.Exception($"Diablo build: model missing: {relativePath}");
        return prefab;
    }

    static float Footprint(GameObject prefab)
    {
        var mf = prefab.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            var s = mf.sharedMesh.bounds.size;
            return Mathf.Max(s.x, s.z);
        }
        var skinned = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinned != null && skinned.sharedMesh != null)
        {
            var s = skinned.sharedMesh.bounds.size;
            return Mathf.Max(s.x, s.z);
        }
        return 2f;
    }

    /// <summary>
    /// Q5 Diablo room (Greybox untouched): KayKit tile floor + wall ring,
    /// torches, chest-dressed extraction pad, Adventurers hero + 3 husks.
    /// </summary>
    public static void BuildDiabloScene()
    {
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Diablo";

        // Click plane (gameplay ground; tile dressing sits just above it).
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(3f, 1f, 3f);
        Paint(ground, new Color(0.16f, 0.14f, 0.16f));

        // Tile floor, self-calibrated to the model footprint.
        var tilePrefab = LoadModel("Dungeon/Models/floor_tile_large.fbx");
        float tile = Footprint(tilePrefab);
        int half = Mathf.CeilToInt(8f / tile);
        for (int ix = -half; ix <= half; ix++)
            for (int iz = -half; iz <= half; iz++)
            {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab);
                t.name = $"DTile_{ix}_{iz}";
                t.transform.position = new Vector3(ix * tile, 0.02f, iz * tile);
            }

        // Wall ring with corners (origins assumed at base; visual fit is a
        // human follow-up, not a headless criterion).
        var wallPrefab = LoadModel("Dungeon/Models/wall.fbx");
        var cornerPrefab = LoadModel("Dungeon/Models/wall_corner.fbx");
        float w = Footprint(wallPrefab);
        int n = Mathf.CeilToInt(16f / w);
        float edge = 8f;
        for (int i = 0; i <= n; i++)
        {
            float p = -edge + i * w;
            PlaceWall(wallPrefab, $"DWall_N_{i}", new Vector3(p, 0f, -edge), 0f);
            PlaceWall(wallPrefab, $"DWall_S_{i}", new Vector3(p, 0f, edge), 180f);
            PlaceWall(wallPrefab, $"DWall_W_{i}", new Vector3(-edge, 0f, p), 90f);
            PlaceWall(wallPrefab, $"DWall_E_{i}", new Vector3(edge, 0f, p), 270f);
        }
        PlaceWall(cornerPrefab, "DWall_Corner_NE", new Vector3(edge, 0f, -edge), 0f);
        PlaceWall(cornerPrefab, "DWall_Corner_NW", new Vector3(-edge, 0f, -edge), 90f);
        PlaceWall(cornerPrefab, "DWall_Corner_SE", new Vector3(edge, 0f, edge), 270f);
        PlaceWall(cornerPrefab, "DWall_Corner_SW", new Vector3(-edge, 0f, edge), 180f);

        // Torch dressing near the corners.
        var torchPrefab = LoadModel("Dungeon/Models/torch_lit.fbx");
        var torchSpots = new[]
        {
            new Vector3(-6f, 0f, -6f), new Vector3(6f, 0f, -6f),
            new Vector3(-6f, 0f, 6f), new Vector3(6f, 0f, 6f),
        };
        for (int i = 0; i < torchSpots.Length; i++)
        {
            var torch = (GameObject)PrefabUtility.InstantiatePrefab(torchPrefab);
            torch.name = $"DTorch_{i}";
            torch.transform.position = torchSpots[i];
        }

        // Light + camera (controller drives it at runtime).
        var light = new GameObject("Directional Light");
        var dl = light.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.intensity = 1.1f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var cam = new GameObject("Main Camera");
        cam.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.transform.position = new Vector3(0f, 12f, -4f);

        // Hero: Adventurers Knight, keeps the "Player" name for ToyTrigger.
        var heroPrefab = LoadModel("Adventurers/Characters/Knight.fbx");
        var hero = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab);
        hero.name = "Player";
        hero.transform.position = new Vector3(0f, 0.05f, 4f);
        var cc = hero.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        hero.AddComponent<DiabloClickController>();
        hero.AddComponent<DiabloMelee>();

        // Husks: 3 Skeletons with builder colliders + HP.
        var huskPrefabs = new[]
        {
            LoadModel("Skeletons/Characters/Skeleton_Warrior.fbx"),
            LoadModel("Skeletons/Characters/Skeleton_Minion.fbx"),
            LoadModel("Skeletons/Characters/Skeleton_Rogue.fbx"),
        };
        var huskSpots = new[]
        {
            new Vector3(-3f, 0.05f, -2f),
            new Vector3(3f, 0.05f, -2f),
            new Vector3(0f, 0.05f, -3.5f),
        };
        for (int i = 0; i < huskPrefabs.Length; i++)
        {
            var husk = (GameObject)PrefabUtility.InstantiatePrefab(huskPrefabs[i]);
            husk.name = $"Husk_{i}";
            husk.tag = "Husk";
            husk.transform.position = huskSpots[i];
            var col = husk.AddComponent<CapsuleCollider>();
            col.height = 1.7f;
            col.radius = 0.35f;
            col.center = new Vector3(0f, 0.85f, 0f);
            husk.AddComponent<EnemyHealth>().corpseModel = huskPrefabs[i];
            WireHuskAnimator(husk);
        }

        // Extraction: GoalPad (Q3 semantics) + chest dressing.
        var goal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        goal.name = "GoalPad";
        goal.transform.position = new Vector3(0f, 0.1f, 6f);
        goal.transform.localScale = new Vector3(3f, 0.2f, 3f);
        Paint(goal, new Color(0.8f, 0.25f, 0.25f));
        goal.GetComponent<BoxCollider>().isTrigger = true;
        goal.AddComponent<ToyTrigger>().mode = ToyTrigger.Mode.Goal;
        var chestPrefab = LoadModel("Dungeon/Models/chest.fbx");
        var chest = (GameObject)PrefabUtility.InstantiatePrefab(chestPrefab);
        chest.name = "DChest";
        chest.transform.position = new Vector3(2.4f, 0f, 6f);

        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();

        // Greybox stays first (UrpPipelineTests pins scenes[0]); Diablo appended.
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(DiabloScenePath, true),
        };
        EditorSceneManager.SaveScene(scene, DiabloScenePath);
        Debug.Log($"[SandboxBuilder] Saved {DiabloScenePath}");
    }

    static void PlaceWall(GameObject prefab, string name, Vector3 pos, float yaw)
    {
        var wall = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        wall.name = name;
        wall.transform.position = pos;
        wall.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    /// <summary>
    /// Headless proof for the Q5 Diablo scene: hero rig, 3 tagged husks
    /// with HP, goal pad, tile floor and wall ring.
    /// </summary>
    public static void VerifyDiablo()
    {
        var scene = EditorSceneManager.OpenScene(DiabloScenePath);
        bool player = false, clicker = false, melee = false, goal = false;
        bool light = false, camera = false;
        int husks = 0, tiles = 0, walls = 0;
        foreach (var root in scene.GetRootGameObjects())
            CountDiabloRecursive(root.transform, ref player, ref clicker, ref melee,
                ref goal, ref light, ref camera, ref husks, ref tiles, ref walls);
        Debug.Log("[SandboxBuilder] VerifyDiablo: " +
                  $"player={player} clicker={clicker} melee={melee} goal={goal} " +
                  $"husks={husks} tiles={tiles} walls={walls} light={light} camera={camera}");
        if (!player || !clicker || !melee || !goal || husks < 3
            || tiles < 25 || walls < 4 || !light || !camera)
            throw new System.Exception("Diablo verify failed: mechanics inventory incomplete.");
        Debug.Log("[SandboxBuilder] VerifyDiablo: OK (hero rig + 3 husks + goal + room present).");
    }

    static void CountDiabloRecursive(Transform t, ref bool player, ref bool clicker,
        ref bool melee, ref bool goal, ref bool light, ref bool camera,
        ref int husks, ref int tiles, ref int walls)
    {
        string n = t.gameObject.name;
        if (n.StartsWith("DTile_")) tiles++;
        if (n.StartsWith("DWall_")) walls++;
        if (n == "GoalPad" && t.gameObject.GetComponent<ToyTrigger>() != null
            && t.gameObject.GetComponent<ToyTrigger>().mode == ToyTrigger.Mode.Goal) goal = true;
        if (n == "Player")
        {
            player = true;
            clicker = t.gameObject.GetComponent<DiabloClickController>() != null;
            melee = t.gameObject.GetComponent<DiabloMelee>() != null;
            player = player && t.gameObject.GetComponent<CharacterController>() != null;
        }
        if (t.gameObject.CompareTag("Husk")
            && t.gameObject.GetComponent<EnemyHealth>() != null) husks++;
        if (t.gameObject.GetComponent<Light>() != null) light = true;
        if (t.gameObject.GetComponent<Camera>() != null) camera = true;
        foreach (Transform c in t) CountDiabloRecursive(c, ref player,
            ref clicker, ref melee, ref goal, ref light, ref camera,
            ref husks, ref tiles, ref walls);
    }

    public static void BuildDiablo()
    {
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { DiabloScenePath },
            locationPathName = "/home/patrick/unity-sandbox/builds/diablo/diablo",
            target = BuildTarget.StandaloneLinux64,
            subtarget = (int)StandaloneBuildSubtarget.Player,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log($"[SandboxBuilder] Diablo build result: {report.summary.result} " +
                  $"in {report.summary.totalTime.TotalSeconds:F1}s, " +
                  $"{report.summary.totalSize / 1048576} MB");
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("Diablo build failed; see log.");
    }

    /// <summary>Dedicated-server build for DiabloZone (no rendering) for hosts like gpu-metal.</summary>
    public static void BuildDiabloZoneServer()
    {
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { DiabloZoneScenePath },
            locationPathName = "/home/patrick/unity-sandbox/builds/diablo-server/diablo-server",
            target = BuildTarget.StandaloneLinux64,
            subtarget = (int)StandaloneBuildSubtarget.Server,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log($"[SandboxBuilder] DiabloZone server build result: {report.summary.result} " +
                  $"in {report.summary.totalTime.TotalSeconds:F1}s, " +
                  $"{report.summary.totalSize / 1048576} MB");
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("DiabloZone server build failed; see log.");
    }

    /// <summary>Dedicated-server build for Diablo scene (no rendering) for hosts like gpu-metal.</summary>
    public static void BuildDiabloServer()
    {
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { DiabloScenePath },
            locationPathName = "/home/patrick/unity-sandbox/builds/diablo-server/diablo-server",
            target = BuildTarget.StandaloneLinux64,
            subtarget = (int)StandaloneBuildSubtarget.Server,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log($"[SandboxBuilder] Diablo server build result: {report.summary.result} " +
                  $"in {report.summary.totalTime.TotalSeconds:F1}s, " +
                  $"{report.summary.totalSize / 1048576} MB");
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("Diablo server build failed; see log.");
    }

    /// <summary>
    /// Scene for the dedicated-server build: `-serverScene diablo` selects
    /// the Diablo room, `diablo-zone` the P2 zone, anything else (or no
    /// argument) the Greybox.
    /// </summary>
    public static string ServerScenePath(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "-serverScene")
            {
                if (args[i + 1] == "diablo") return DiabloScenePath;
                if (args[i + 1] == "diablo-zone") return DiabloZoneScenePath;
                return ScenePath;
            }
        return ScenePath;
    }

    /// <summary>
    /// Dedicated-server build (no rendering) for hosts like gpu-metal. Run:
    ///   Unity -batchmode -nographics -quit -projectPath sandbox
    ///     -executeMethod SandboxBuilder.BuildServer [-serverScene greybox|diablo|diablo-zone]
    /// </summary>
    public static void BuildServer()
    {
        string scenePath = ServerScenePath(System.Environment.GetCommandLineArgs());
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { scenePath },
            locationPathName = "/home/patrick/unity-sandbox/builds/greybox-server/greybox-server",
            target = BuildTarget.StandaloneLinux64,
            subtarget = (int)StandaloneBuildSubtarget.Server,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log($"[SandboxBuilder] Server build result: {report.summary.result} " +
                  $"(scene {System.IO.Path.GetFileName(scenePath)}) " +
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

    const string DiabloZoneScenePath = "Assets/Scenes/DiabloZone.unity";

    /// <summary>
    /// Ashen Fen (reworked DiabloZone): Lantern Hold (palisade ring,
    /// campfire, spawns) in the south, open fen with 10 husks + 1 brute,
    /// west alcove elite + chest, east rubble, north Fen Gate extraction.
    /// 4 co-op spawns, netcode, starter sword, inventory, campfire heal.
    /// </summary>
    public static void BuildDiabloZone()
    {
        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "DiabloZone";

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(4f, 1f, 4f);
        Paint(ground, new Color(0.20f, 0.28f, 0.17f));

        BuildMoorField();

        // South stairs (entry).
        for (int i = 0; i < 3; i++)
        {
            var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = $"ZStep_{i}";
            step.transform.position = new Vector3(0f, 0.15f * (2 - i), 10f + i * 1.2f);
            step.transform.localScale = new Vector3(4f, 0.3f, 1.2f);
            Paint(step, new Color(0.35f, 0.33f, 0.36f));
        }

        // Entry hint trigger (onboarding: where to go, what to do).
        var entry = new GameObject("ZoneEntry");
        entry.transform.position = new Vector3(0f, 1f, 11f);
        var entryCol = entry.AddComponent<BoxCollider>();
        entryCol.isTrigger = true;
        entryCol.size = new Vector3(5f, 2f, 4f);
        entry.AddComponent<ZoneEntryHint>();

        // West alcove: elite arena + chest.
        var alcoveTiles = LoadModel("Dungeon/Models/floor_tile_large.fbx");
        float tile = Footprint(alcoveTiles);
        for (int ix = -2; ix <= 2; ix++)
            for (int iz = -2; iz <= 2; iz++)
            {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(alcoveTiles);
                t.name = $"ZTile_Alcove_{ix}_{iz}";
                t.transform.position = new Vector3(-12f + ix * tile, 0.02f, iz * tile);
            }
        var chestPrefab = LoadModel("Dungeon/Models/chest.fbx");
        var chest = (GameObject)PrefabUtility.InstantiatePrefab(chestPrefab);
        chest.name = "DChestZone";
        chest.transform.position = new Vector3(-12f, 0f, 0f);
        var chestCol = chest.AddComponent<BoxCollider>();
        chestCol.isTrigger = true;
        chestCol.size = new Vector3(2f, 2f, 2f);
        var chestLoot = chest.AddComponent<LootDrop>();
        chestLoot.isChest = true;
        chest.AddComponent<NetworkObject>();
        chest.AddComponent<LootNetRelay>();

        // East rubble (collapsed passage, decoration only).
        for (int i = 0; i < 4; i++)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = $"ZRubble_{i}";
            rock.transform.position = new Vector3(10f + (i % 2), 0.4f, -1f + i);
            rock.transform.localScale = new Vector3(1.2f, 0.8f, 1.2f);
            rock.transform.rotation = Quaternion.Euler(0f, i * 20f, 8f);
            Paint(rock, new Color(0.4f, 0.38f, 0.4f));
        }

        // North crypt gate (extraction, Q3 semantics).
        var goal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        goal.name = "GoalPad";
        goal.transform.position = new Vector3(0f, 0.1f, -6f);
        goal.transform.localScale = new Vector3(3f, 0.2f, 3f);
        Paint(goal, new Color(0.8f, 0.25f, 0.25f));
        goal.GetComponent<BoxCollider>().isTrigger = true;
        goal.AddComponent<ToyTrigger>().mode = ToyTrigger.Mode.Goal;
        for (int i = 0; i < 2; i++)
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = $"ZGatePillar_{i}";
            pillar.transform.position = new Vector3(i == 0 ? -2.2f : 2.2f, 1.5f, -6f);
            pillar.transform.localScale = new Vector3(0.8f, 3f, 0.8f);
            Paint(pillar, new Color(0.45f, 0.42f, 0.48f));
        }

        // Light + camera.
        var light = new GameObject("Directional Light");
        var dl = light.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.intensity = 1.1f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var cam = new GameObject("Main Camera");
        cam.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.transform.position = new Vector3(0f, 12f, -4f);

        // Solo hero (verify + single-player), with Equipment for loot.
        var heroPrefab = LoadModel("Adventurers/Characters/Knight.fbx");
        var hero = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab);
        hero.name = "Player";
        hero.transform.position = new Vector3(0f, 0.05f, 11f);
        var cc = hero.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        // Ignore Raycast is per-object: hero meshes live on children.
        SetLayerRecursive(hero, LayerMask.NameToLayer("Ignore Raycast"));
        hero.AddComponent<DiabloClickController>();
        hero.AddComponent<DiabloMelee>();
        hero.AddComponent<Equipment>();
        hero.AddComponent<Inventory>();
        hero.AddComponent<PlayerHealth>();
        hero.AddComponent<DiabloInventoryUI>();
        hero.GetComponent<Equipment>().Equip(ItemDatabase.CreateStarterSword());
        hero.AddComponent<FpsReporter>();
        WireLocomotion(hero, "Idle_A", "Running_A", "Melee_1H_Attack_Chop");
        AttachStarterSword(hero);
        SetLayerRecursive(hero, LayerMask.NameToLayer("Ignore Raycast"));

        // 10 husks scattered across the fen (3 m+ apart, off the camp).
        var huskModels = new[]
        {
            "Skeletons/Characters/Skeleton_Warrior.fbx",
            "Skeletons/Characters/Skeleton_Minion.fbx",
            "Skeletons/Characters/Skeleton_Rogue.fbx",
        };
        var huskSpots = new[]
        {
            new Vector3(-5f, 0.05f, 2f), new Vector3(5f, 0.05f, 2f),
            new Vector3(-2f, 0.05f, -1f), new Vector3(3f, 0.05f, -2f),
            new Vector3(0f, 0.05f, 5f), new Vector3(-7f, 0.05f, -3f),
            new Vector3(7f, 0.05f, -3f), new Vector3(-4f, 0.05f, -6f),
            new Vector3(4f, 0.05f, -6f), new Vector3(0f, 0.05f, -8f),
        };
        // Normal husks take two starter hits (5 HP): the first survives long
        // enough to telegraph retaliation instead of dying on contact.
        for (int i = 0; i < huskSpots.Length; i++)
            SpawnZoneHusk(huskModels[i % huskModels.Length], $"Husk_{i}",
                huskSpots[i], 5, LootTier.Normal, 1f, null);

        // Elite (west alcove).
        SpawnZoneHusk("Skeletons/Characters/Skeleton_Minion.fbx", "Ash Elite",
            new Vector3(-12f, 0.05f, 3f), 15, LootTier.Elite, 1.3f,
            new Color(1f, 0.3f, 0.3f));

        // Brute (northeast fen, second elite).
        SpawnZoneHusk("Skeletons/Characters/Skeleton_Warrior.fbx", "Fen Brute",
            new Vector3(9f, 0.05f, -6f), 12, LootTier.Elite, 1.25f,
            new Color(0.8f, 0.2f, 0.2f));

        // UI input for the code-built inventory panel. InputSystem module:
        // StandaloneInputModule reads the legacy Input API and throws every
        // frame (project uses Input System only) — it also spammed the
        // dedicated server log at ~2 MB/s.
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var eventSys = new GameObject("EventSystem");
            eventSys.AddComponent<EventSystem>();
            eventSys.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // D2 target bar (shared, follows the last husk struck).
        if (GameObject.Find("DTargetBar") == null)
        {
            var bar = new GameObject("DTargetBar");
            bar.AddComponent<DiabloTargetBar>();
        }

        // 4 co-op spawn markers (south stairs, circle formation).
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI / 2f;
            var spawn = new GameObject($"CoopSpawn_{i}");
            spawn.transform.position = new Vector3(Mathf.Cos(a) * 1.5f, 0.1f, 11f + Mathf.Sin(a) * 1.5f);
        }

        // Blender-built Ashen Fen props replace the greybox look (names kept).
        WorldArtBuilder.DressDiabloZone();

        BuildDiabloZonePlayerPrefab();
        AssetDatabase.Refresh();
        DiabloNetworkBootstrap.EnsureZoneNetcode();

        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(DiabloScenePath, true),
            new EditorBuildSettingsScene(DiabloZoneScenePath, true),
        };
        EditorSceneManager.SaveScene(scene, DiabloZoneScenePath);
        Debug.Log($"[SandboxBuilder] Saved {DiabloZoneScenePath}");
    }

    static void BuildZoneHall()
    {
        var tilePrefab = LoadModel("Dungeon/Models/floor_tile_large.fbx");
        float tile = Footprint(tilePrefab);
        int half = Mathf.CeilToInt(8f / tile);
        for (int ix = -half; ix <= half; ix++)
            for (int iz = -half; iz <= half; iz++)
            {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab);
                t.name = $"ZTile_{ix}_{iz}";
                t.transform.position = new Vector3(ix * tile, 0.02f, iz * tile);
            }
        var wallPrefab = LoadModel("Dungeon/Models/wall.fbx");
        var cornerPrefab = LoadModel("Dungeon/Models/wall_corner.fbx");
        float w = Footprint(wallPrefab);
        int n = Mathf.CeilToInt(16f / w);
        float edge = 8f;
        for (int i = 0; i <= n; i++)
        {
            float p = -edge + i * w;
            PlaceZoneWall(wallPrefab, $"ZWall_N_{i}", new Vector3(p, 0f, -edge), 0f);
            PlaceZoneWall(wallPrefab, $"ZWall_S_{i}", new Vector3(p, 0f, edge), 180f);
            PlaceZoneWall(wallPrefab, $"ZWall_W_{i}", new Vector3(-edge, 0f, p), 90f);
            PlaceZoneWall(wallPrefab, $"ZWall_E_{i}", new Vector3(edge, 0f, p), 270f);
        }
        PlaceZoneWall(cornerPrefab, "ZWall_Corner_NE", new Vector3(edge, 0f, -edge), 0f);
        PlaceZoneWall(cornerPrefab, "ZWall_Corner_NW", new Vector3(-edge, 0f, -edge), 90f);
        PlaceZoneWall(cornerPrefab, "ZWall_Corner_SE", new Vector3(edge, 0f, edge), 270f);
        PlaceZoneWall(cornerPrefab, "ZWall_Corner_SW", new Vector3(edge, 0f, edge), 180f);
        var torchPrefab = LoadModel("Dungeon/Models/torch_lit.fbx");
        var torchSpots = new[]
        {
            new Vector3(-6f, 0f, -6f), new Vector3(6f, 0f, -6f),
            new Vector3(-6f, 0f, 6f), new Vector3(6f, 0f, 6f),
        };
        for (int i = 0; i < torchSpots.Length; i++)
        {
            var torch = (GameObject)PrefabUtility.InstantiatePrefab(torchPrefab);
            torch.name = $"ZTorch_{i}";
            torch.transform.position = torchSpots[i];
        }
    }

    static void PlaceZoneWall(GameObject prefab, string name, Vector3 pos, float yaw)
    {
        var wall = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        wall.name = name;
        wall.transform.position = pos;
        wall.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    /// <summary>
    /// Ashen Fen field: earthwork bounds, Lantern Hold palisade ring +
    /// campfire in the south, scattered rocks and bloodstains across the fen.
    /// Camp torches keep the ZTorch_* names (verify counts them).
    /// </summary>
    static void BuildMoorField()
    {
        // Earthwork bounds (keeps the hero on the 40 m plane).
        var bounds = new[]
        {
            ("MoorEdge_N", new Vector3(0f, 1f, -18f), new Vector3(40f, 2f, 1f)),
            ("MoorEdge_S", new Vector3(0f, 1f, 18f), new Vector3(40f, 2f, 1f)),
            ("MoorEdge_E", new Vector3(18f, 1f, 0f), new Vector3(1f, 2f, 40f)),
            ("MoorEdge_W", new Vector3(-18f, 1f, 0f), new Vector3(1f, 2f, 40f)),
        };
        foreach (var (name, pos, scale) in bounds)
        {
            var edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = name;
            edge.transform.position = pos;
            edge.transform.localScale = scale;
            Paint(edge, new Color(0.25f, 0.22f, 0.15f));
        }

        // Palisade ring around the camp (center 0,11, radius 6, gap north).
        var wood = new Color(0.35f, 0.24f, 0.13f);
        int logs = 0;
        for (int i = 0; i < 20; i++)
        {
            float a = i * Mathf.PI * 2f / 20f;
            float sx = Mathf.Cos(a), sz = Mathf.Sin(a);
            if (sz < -0.7f) continue; // north gate toward the moor
            var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = $"Palisade_{logs}";
            log.transform.position = new Vector3(sx * 6f, 1.25f, 11f + sz * 6f);
            log.transform.localScale = new Vector3(0.5f, 2.5f, 0.5f);
            Paint(log, wood);
            logs++;
        }

        // Campfire: stone base, ember cube, logs + heal trigger.
        var firePos = new Vector3(3.5f, 0f, 11f);
        var stone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stone.name = "CampfireBase";
        stone.transform.position = firePos + Vector3.up * 0.1f;
        stone.transform.localScale = new Vector3(1.6f, 0.2f, 1.6f);
        Paint(stone, new Color(0.4f, 0.38f, 0.38f));
        var ember = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ember.name = "Campfire";
        ember.transform.position = firePos + Vector3.up * 0.5f;
        ember.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        Paint(ember, new Color(1f, 0.45f, 0.1f));
        for (int i = 0; i < 4; i++)
        {
            var fuel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fuel.name = $"CampfireLog_{i}";
            float a = i * Mathf.PI / 2f + Mathf.PI / 4f;
            fuel.transform.position = firePos + new Vector3(Mathf.Cos(a) * 0.6f, 0.25f, Mathf.Sin(a) * 0.6f);
            fuel.transform.rotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 80f);
            fuel.transform.localScale = new Vector3(0.25f, 1.2f, 0.25f);
            Paint(fuel, wood);
        }
        var trigger = new GameObject("CampfireHeal");
        trigger.transform.position = firePos + Vector3.up;
        var triggerCol = trigger.AddComponent<BoxCollider>();
        triggerCol.isTrigger = true;
        triggerCol.size = new Vector3(CampfireHeal.Radius * 2f, 3f, CampfireHeal.Radius * 2f);
        trigger.AddComponent<CampfireHeal>();

        // Camp torches (verify counts ZTorch_*).
        var torchPrefab = LoadModel("Dungeon/Models/torch_lit.fbx");
        var torchSpots = new[]
        {
            new Vector3(-5f, 0f, 6f), new Vector3(5f, 0f, 6f),
            new Vector3(-5f, 0f, 15f), new Vector3(5f, 0f, 15f),
        };
        for (int i = 0; i < torchSpots.Length; i++)
        {
            var torch = (GameObject)PrefabUtility.InstantiatePrefab(torchPrefab);
            torch.name = $"ZTorch_{i}";
            torch.transform.position = torchSpots[i];
        }

        // Scattered moor rocks.
        var rockSpots = new[]
        {
            new Vector3(-3f, 0.3f, 0f), new Vector3(6f, 0.4f, -1f),
            new Vector3(-8f, 0.25f, 1f), new Vector3(2f, 0.35f, -4f),
            new Vector3(-5f, 0.3f, -8f), new Vector3(7f, 0.4f, 3f),
            new Vector3(-1f, 0.25f, 7f), new Vector3(-9f, 0.35f, -6f),
        };
        for (int i = 0; i < rockSpots.Length; i++)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = $"MoorRock_{i}";
            rock.transform.position = rockSpots[i];
            rock.transform.localScale = new Vector3(1.1f, 0.7f, 1f);
            rock.transform.rotation = Quaternion.Euler(0f, i * 25f, 5f);
            Paint(rock, new Color(0.38f, 0.36f, 0.36f));
        }

        // Bloodstains where the fen earned its name.
        var stainSpots = new[]
        {
            new Vector3(-4.5f, 0f, 1.5f), new Vector3(4.5f, 0f, 1f),
            new Vector3(0f, 0f, -3f), new Vector3(-6.5f, 0f, -3.5f),
            new Vector3(3.5f, 0f, -6.5f), new Vector3(8.5f, 0f, -5.5f),
        };
        for (int i = 0; i < stainSpots.Length; i++)
        {
            var stain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stain.name = $"BloodPatch_{i}";
            stain.transform.position = stainSpots[i] + Vector3.up * 0.02f;
            stain.transform.localScale = new Vector3(1.8f, 0.02f, 1.8f);
            Paint(stain, new Color(0.45f, 0.08f, 0.08f));
        }
    }

    /// <summary>
    /// Wire KayKit Rig_Medium take clips onto a hero via a shared Mecanim
    /// controller (retargeted): Idle_A default, Running_A on "Moving".
    /// Warns instead of throwing when a take is missing (pack drift).
    /// </summary>
    static void WireLocomotion(GameObject go, string idleTake, string runTake, string strikeTake)
    {
        var idle = FindTakeClip(
            "Assets/ThirdParty/KayKit/CharacterAnimations/Rig_Medium/Rig_Medium_General.fbx", idleTake);
        var run = FindTakeClip(
            "Assets/ThirdParty/KayKit/CharacterAnimations/Rig_Medium/Rig_Medium_MovementBasic.fbx", runTake);
        var strike = FindTakeClip(
            "Assets/ThirdParty/KayKit/CharacterAnimations/Rig_Medium/Rig_Medium_CombatMelee.fbx", strikeTake);
        if (idle == null || run == null || strike == null)
        {
            Debug.LogWarning($"[SandboxBuilder] Locomotion clips missing (idle={idleTake}, run={runTake}, strike={strikeTake}); hero stays static.");
            return;
        }
        var animator = go.GetComponent<Animator>();
        if (animator == null) animator = go.AddComponent<Animator>();
        animator.runtimeAnimatorController = LocomotionController(idle, run, strike);
        go.AddComponent<DiabloAnimator>();
    }

    static RuntimeAnimatorController sharedLocomotion;

    /// <summary>Build once per session: rebuilding would orphan earlier refs.</summary>
    static RuntimeAnimatorController LocomotionController(AnimationClip idle, AnimationClip run, AnimationClip strike)
    {
        if (sharedLocomotion == null) sharedLocomotion = BuildLocomotionController(idle, run, strike);
        return sharedLocomotion;
    }

    static RuntimeAnimatorController BuildLocomotionController(AnimationClip idle, AnimationClip run, AnimationClip strike)
    {
        const string path = "Assets/Resources/DiabloLocomotion.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Strike", AnimatorControllerParameterType.Trigger);
        var sm = controller.layers[0].stateMachine;
        foreach (var s in sm.states) sm.RemoveState(s.state);
        var idleState = sm.AddState("Idle");
        idleState.motion = idle;
        var runState = sm.AddState("Run");
        runState.motion = run;
        sm.defaultState = idleState;
        var strikeState = sm.AddState("Strike");
        strikeState.motion = strike;
        var anyToStrike = sm.AddAnyStateTransition(strikeState);
        anyToStrike.hasExitTime = false;
        anyToStrike.duration = 0.1f;
        anyToStrike.AddCondition(AnimatorConditionMode.If, 0, "Strike");
        var strikeToIdle = strikeState.AddTransition(idleState);
        strikeToIdle.hasExitTime = true;
        strikeToIdle.duration = 0.15f;
        var toRun = idleState.AddTransition(runState);
        toRun.hasExitTime = false;
        toRun.duration = 0.15f;
        toRun.AddCondition(AnimatorConditionMode.If, 0, "Moving");
        var toIdle = runState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.15f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "Moving");
        SkillArtBuilder.AddBowStates(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    const string RigPath = "Assets/ThirdParty/KayKit/CharacterAnimations/Rig_Medium/";

    /// <summary>
    /// Husk Mecanim rig (Skeletons share Rig_Medium with the hero): Idle_A,
    /// Walking_A on "Moving", Melee_1H_Attack_Chop on "Attack", Hit_A on
    /// "Hit", Death_A (no exits; corpses Play it). Warns, never throws, when
    /// a take is missing — the husk then stays static but fully playable.
    /// </summary>
    public static void WireHuskAnimator(GameObject husk)
    {
        var controller = HuskController();
        if (controller == null) return;
        var animator = husk.GetComponent<Animator>();
        if (animator == null) animator = husk.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        if (husk.GetComponent<HuskAnimator>() == null) husk.AddComponent<HuskAnimator>();
    }

    static RuntimeAnimatorController sharedHusk;

    static RuntimeAnimatorController HuskController()
    {
        if (sharedHusk != null) return sharedHusk;
        var idle = FindTakeClip(RigPath + "Rig_Medium_General.fbx", "Idle_A");
        var walk = FindTakeClip(RigPath + "Rig_Medium_MovementBasic.fbx", "Walking_A");
        var attack = FindTakeClip(RigPath + "Rig_Medium_CombatMelee.fbx", "Melee_1H_Attack_Chop");
        var hit = FindTakeClip(RigPath + "Rig_Medium_General.fbx", "Hit_A");
        var death = FindTakeClip(RigPath + "Rig_Medium_General.fbx", "Death_A");
        if (idle == null || walk == null || attack == null || hit == null || death == null)
        {
            Debug.LogWarning("[SandboxBuilder] Husk takes missing; husks stay static.");
            return null;
        }
        const string path = "Assets/Resources/HuskLocomotion.controller";
        AssetDatabase.DeleteAsset(path);
        var c = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
        c.AddParameter(HuskAnimator.MoveParam, AnimatorControllerParameterType.Bool);
        c.AddParameter(HuskAnimator.AttackParam, AnimatorControllerParameterType.Trigger);
        c.AddParameter(HuskAnimator.HitParam, AnimatorControllerParameterType.Trigger);
        var sm = c.layers[0].stateMachine;
        foreach (var st in sm.states) sm.RemoveState(st.state);
        var idleS = sm.AddState("Idle");
        idleS.motion = idle;
        var walkS = sm.AddState("Walk");
        walkS.motion = walk;
        var attackS = sm.AddState("Attack");
        attackS.motion = attack;
        var hitS = sm.AddState("Hit");
        hitS.motion = hit;
        var deathS = sm.AddState(HuskAnimator.DeathState);
        deathS.motion = death;
        sm.defaultState = idleS;
        var toWalk = idleS.AddTransition(walkS);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.15f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0, HuskAnimator.MoveParam);
        var toIdle = walkS.AddTransition(idleS);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.15f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, HuskAnimator.MoveParam);
        foreach (var (state, param) in new[] { (attackS, HuskAnimator.AttackParam), (hitS, HuskAnimator.HitParam) })
        {
            var enter = sm.AddAnyStateTransition(state);
            enter.hasExitTime = false;
            enter.duration = 0.08f;
            enter.canTransitionToSelf = true;
            enter.AddCondition(AnimatorConditionMode.If, 0, param);
            var exit = state.AddTransition(idleS);
            exit.hasExitTime = true;
            exit.duration = 0.15f;
        }
        AssetDatabase.SaveAssets();
        sharedHusk = c;
        return c;
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        foreach (var tr in go.GetComponentsInChildren<Transform>(true))
            tr.gameObject.layer = layer;
    }

    static AnimationClip FindTakeClip(string fbxPath, string takeName)
    {
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            if (obj is AnimationClip clip && clip.name == takeName)
                return clip;
        return null;
    }

    /// <summary>
    /// Visible starter sword (KayKit 1-handed) in the hero's right hand.
    /// The rig is generic (no Humanoid avatar), so the hand bone is found by
    /// name: KayKit ships a weapon mount ("handslot.r"). Hip fallback only
    /// when the rig has neither (pinned by scene tests — never silent).
    /// </summary>
    static void AttachStarterSword(GameObject hero)
    {
        var prefab = LoadModel("Adventurers/Accessories/sword_1handed.fbx");
        var sword = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        sword.name = "StarterSword";
        Transform hand = FindDeep(hero.transform, "handslot.r")
            ?? FindDeep(hero.transform, "hand.r");
        if (hand != null)
        {
            sword.transform.SetParent(hand, false);
            sword.transform.localPosition = Vector3.zero;
            sword.transform.localRotation = Quaternion.identity;
        }
        else
        {
            sword.transform.SetParent(hero.transform, false);
            sword.transform.localPosition = new Vector3(0.35f, 0.9f, 0.1f);
            sword.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
        }
    }

    /// <summary>Recursive case-insensitive descendant search (rig bone lookup).</summary>
    static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform child in root)
        {
            if (string.Equals(child.name, name, System.StringComparison.OrdinalIgnoreCase))
                return child;
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    static void SpawnZoneHusk(string model, string name, Vector3 pos, int hp, LootTier tier, float scale, Color? tint)
    {
        var prefab = LoadModel(model);
        var husk = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        husk.name = name;
        husk.tag = "Husk";
        husk.transform.position = pos;
        husk.transform.localScale = Vector3.one * scale;
        var col = husk.AddComponent<CapsuleCollider>();
        col.height = 1.7f;
        col.radius = 0.35f;
        col.center = new Vector3(0f, 0.85f, 0f);
        // Networked scene object: the dedicated server auto-spawns it, damage
        // and death replicate instead of diverging per client.
        husk.AddComponent<NetworkObject>();
        var health = husk.AddComponent<EnemyHealth>();
        health.SetTier(hp, tier);
        health.corpseModel = prefab;
        husk.AddComponent<EnemyAttack>().damage = tier == LootTier.Normal ? 1 : 2;
        WireHuskAnimator(husk);
        if (tint.HasValue)
        {
            var rend = husk.GetComponentInChildren<Renderer>();
            if (rend != null) rend.material.color = tint.Value;
        }
    }

    static void BuildDiabloZonePlayerPrefab()
    {
        var heroPrefab = LoadModel("Adventurers/Characters/Knight.fbx");
        var netPlayer = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab);
        netPlayer.name = "NetworkPlayerDiablo";
        // Spawn at the entry stairs so players walk INTO the zone.
        netPlayer.transform.position = new Vector3(0f, 0.05f, 11f);
        var cc = netPlayer.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        netPlayer.AddComponent<NetworkObject>();
        // Owner authority: the owning client pushes movement; server authority
        // would replicate the server's stale copy back and rubber-band the owner.
        netPlayer.AddComponent<NetworkTransform>().AuthorityMode = NetworkTransform.AuthorityModes.Owner;
        netPlayer.AddComponent<DiabloClickController>();
        netPlayer.AddComponent<DiabloMelee>();
        SetLayerRecursive(netPlayer, LayerMask.NameToLayer("Ignore Raycast"));
        netPlayer.AddComponent<DiabloNetworkPlayer>();
        netPlayer.AddComponent<Equipment>();
        netPlayer.AddComponent<Inventory>();
        netPlayer.AddComponent<PlayerHealth>();
        netPlayer.AddComponent<DiabloInventoryUI>();
        netPlayer.GetComponent<Equipment>().Equip(ItemDatabase.CreateStarterSword());
        netPlayer.AddComponent<FpsReporter>();
        WireLocomotion(netPlayer, "Idle_A", "Running_A", "Melee_1H_Attack_Chop");
        AttachStarterSword(netPlayer);
        SetLayerRecursive(netPlayer, LayerMask.NameToLayer("Ignore Raycast"));
        const string prefabPath = "Assets/Resources/NetworkPlayerDiablo.prefab";
        System.IO.Directory.CreateDirectory("Assets/Resources");
        PrefabUtility.SaveAsPrefabAsset(netPlayer, prefabPath);
        Object.DestroyImmediate(netPlayer);
        Debug.Log("[SandboxBuilder] NetworkPlayerDiablo prefab saved to Resources.");
    }

    /// <summary>Headless proof for the Ashen Fen: camp, husks + elites, loot, netcode.</summary>
    public static void VerifyDiabloZone()
    {
        var scene = EditorSceneManager.OpenScene(DiabloZoneScenePath);
        bool player = false, clicker = false, melee = false, equip = false;
        bool inv = false, health = false, ui = false, starterSword = false;
        bool goal = false, campfire = false, targetBar = false;
        bool light = false, camera = false, elite = false, brute = false, chest = false;
        bool allHusksArmed = true;
        bool net = false, transport = false, autostart = false, prefab = false;
        int husks = 0, palisade = 0, torches = 0, spawns = 0, edges = 0, patches = 0;
        foreach (var root in scene.GetRootGameObjects())
            CountZoneRecursive(root.transform, ref player, ref clicker, ref melee,
                ref equip, ref inv, ref health, ref ui, ref starterSword,
                ref goal, ref campfire, ref targetBar, ref light, ref camera, ref elite,
                ref brute, ref chest, ref net, ref transport, ref autostart,
                ref prefab, ref allHusksArmed, ref husks, ref palisade, ref torches,
                ref spawns, ref edges, ref patches);
        Debug.Log("[SandboxBuilder] VerifyDiabloZone: " +
                  $"player={player} husks={husks} armed={allHusksArmed} elite={elite} brute={brute} " +
                  $"chest={chest} goal={goal} campfire={campfire} targetBar={targetBar} " +
                  $"starterSword={starterSword} spawns={spawns} palisade={palisade} " +
                  $"torches={torches} edges={edges} patches={patches} net={net}");
        if (!player || !clicker || !melee || !equip || !inv || !health || !ui
            || !starterSword || !goal || !campfire || !targetBar || !allHusksArmed
            || !elite || !brute || !chest
            || husks < 10 || palisade < 12 || spawns != 4 || torches < 4
            || edges != 4 || patches < 4
            || !light || !camera || !net || !transport || !autostart || !prefab)
            throw new System.Exception("DiabloZone verify failed: zone inventory incomplete.");
        Debug.Log("[SandboxBuilder] VerifyDiabloZone: OK (ashen fen + loot + netcode present).");
    }

    /// <summary>Husk can fight back and is animated (attack + husk rig + controller).</summary>
    static bool HuskArmed(GameObject g)
    {
        var a = g.GetComponent<Animator>();
        return g.GetComponent<EnemyAttack>() != null && g.GetComponent<HuskAnimator>() != null
            && a != null && a.runtimeAnimatorController != null;
    }

    static void CountZoneRecursive(Transform t, ref bool player, ref bool clicker,
        ref bool melee, ref bool equip, ref bool inv, ref bool health, ref bool ui,
        ref bool starterSword, ref bool goal, ref bool campfire, ref bool targetBar,
        ref bool light, ref bool camera, ref bool elite, ref bool brute, ref bool chest,
        ref bool net, ref bool transport, ref bool autostart, ref bool prefab,
        ref bool allHusksArmed, ref int husks,
        ref int palisade, ref int torches, ref int spawns, ref int edges, ref int patches)
    {
        string n = t.gameObject.name;
        if (n.StartsWith("Palisade_")) palisade++;
        if (n.StartsWith("MoorEdge_")) edges++;
        if (n.StartsWith("BloodPatch_")) patches++;
        if (n.StartsWith("ZTorch_")) torches++;
        if (n.StartsWith("CoopSpawn_")) spawns++;
        if (n == "GoalPad" && t.gameObject.GetComponent<ToyTrigger>() != null
            && t.gameObject.GetComponent<ToyTrigger>().mode == ToyTrigger.Mode.Goal) goal = true;
        if (n == "CampfireHeal" && t.gameObject.GetComponent<CampfireHeal>() != null) campfire = true;
        if (n == "DTargetBar" && t.gameObject.GetComponent<DiabloTargetBar>() != null) targetBar = true;
        if (n == "Player")
        {
            player = true;
            clicker = t.gameObject.GetComponent<DiabloClickController>() != null;
            melee = t.gameObject.GetComponent<DiabloMelee>() != null;
            equip = t.gameObject.GetComponent<Equipment>() != null;
            inv = t.gameObject.GetComponent<Inventory>() != null;
            health = t.gameObject.GetComponent<PlayerHealth>() != null;
            ui = t.gameObject.GetComponent<DiabloInventoryUI>() != null;
            player = player && t.gameObject.GetComponent<CharacterController>() != null;
            var eq = t.gameObject.GetComponent<Equipment>();
            starterSword = eq != null && eq.weapon != null
                && eq.GetDamage() >= Equipment.BaseDamage + 2f;
        }
        if (n == "Ash Elite" && t.gameObject.GetComponent<EnemyHealth>() != null)
        {
            elite = true;
            if (!HuskArmed(t.gameObject)) allHusksArmed = false;
        }
        else if (n == "Fen Brute" && t.gameObject.GetComponent<EnemyHealth>() != null)
        {
            brute = true;
            if (!HuskArmed(t.gameObject)) allHusksArmed = false;
        }
        else if (t.gameObject.CompareTag("Husk")
            && t.gameObject.GetComponent<EnemyHealth>() != null)
        {
            husks++;
            if (!HuskArmed(t.gameObject)) allHusksArmed = false;
        }
        if (n == "DChestZone" && t.gameObject.GetComponent<LootDrop>() != null) chest = true;
        if (n == "NetworkManager")
        {
            var nm = t.gameObject.GetComponent<NetworkManager>();
            if (nm != null)
            {
                net = true;
                transport = nm.GetComponent<UnityTransport>() != null
                    && nm.NetworkConfig.NetworkTransport != null;
                autostart = nm.GetComponent<MultiplayerAutoStarter>() != null;
                var pp = nm.NetworkConfig.PlayerPrefab;
                prefab = pp != null && pp.GetComponent<NetworkObject>() != null
                    && pp.GetComponent<DiabloNetworkPlayer>() != null
                    && pp.GetComponent<Equipment>() != null
                    && pp.GetComponent<Inventory>() != null
                    && pp.GetComponent<PlayerHealth>() != null
                    && pp.GetComponent<DiabloInventoryUI>() != null;
            }
        }
        if (t.gameObject.GetComponent<Light>() != null) light = true;
        if (t.gameObject.GetComponent<Camera>() != null) camera = true;
        foreach (Transform c in t) CountZoneRecursive(c, ref player, ref clicker,
            ref melee, ref equip, ref inv, ref health, ref ui, ref starterSword,
            ref goal, ref campfire, ref targetBar, ref light, ref camera, ref elite,
            ref brute, ref chest, ref net, ref transport, ref autostart,
            ref prefab, ref allHusksArmed, ref husks, ref palisade, ref torches,
            ref spawns, ref edges, ref patches);
    }

    /// <summary>
    /// Windows player build of DiabloZone for GPU playtests (run the .exe
    /// on Windows; same scene + netcode as the Linux build).
    /// </summary>
    public static void BuildDiabloZoneWindows()
    {
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { DiabloZoneScenePath },
            locationPathName = "/home/patrick/unity-sandbox/builds/cinderreach-windows/cinderreach.exe",
            target = BuildTarget.StandaloneWindows64,
            subtarget = (int)StandaloneBuildSubtarget.Player,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log($"[SandboxBuilder] DiabloZone Windows build result: {report.summary.result} " +
                  $"in {report.summary.totalTime.TotalSeconds:F1}s, " +
                  $"{report.summary.totalSize / 1048576} MB");
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("DiabloZone Windows build failed; see log.");
    }

    public static void BuildDiabloZoneBuild()
    {
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { DiabloZoneScenePath },
            locationPathName = "/home/patrick/unity-sandbox/builds/diablo-zone/diablo-zone",
            target = BuildTarget.StandaloneLinux64,
            subtarget = (int)StandaloneBuildSubtarget.Player,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log($"[SandboxBuilder] DiabloZone build result: {report.summary.result} " +
                  $"in {report.summary.totalTime.TotalSeconds:F1}s, " +
                  $"{report.summary.totalSize / 1048576} MB");
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("DiabloZone build failed; see log.");
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
