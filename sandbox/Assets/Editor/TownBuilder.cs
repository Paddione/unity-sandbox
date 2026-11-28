using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Lantern Hold town construction (S3). The camp block (palisade ring,
/// campfire + heal trigger, camp torches) moved here verbatim out of
/// <c>SandboxBuilder.BuildMoorField</c>: same objects, same names, same
/// order — <c>VerifyDiabloZone</c> counts are the refactor proof. New in S3:
/// the vendor NPC (KayKit Adventurer, idle take) with click/radius trade.
/// </summary>
public static class TownBuilder
{
    /// <summary>Vendor model: a hooded trader, distinct from the Knight hero.</summary>
    public const string VendorModelPath = "Adventurers/Characters/Rogue_Hooded.fbx";

    const string RigGeneralPath =
        "Assets/ThirdParty/KayKit/CharacterAnimations/Rig_Medium/Rig_Medium_General.fbx";

    /// <summary>
    /// Camp: palisade ring (center 0,17, radius 12, north gap), campfire stone
    /// + ember + logs + heal trigger, camp torches (ZTorch_*). Moved
    /// verbatim from BuildMoorField — do not rename (verify counts them).
    /// </summary>
    public static void BuildCamp()
    {
        // Expand towards the rear meadow, keeping the entrance at z=5.
        var wood = new Color(0.35f, 0.24f, 0.13f);
        int logs = 0;
        for (int i = 0; i < 40; i++)
        {
            float a = i * Mathf.PI * 2f / 40f;
            float sx = Mathf.Cos(a), sz = Mathf.Sin(a);
            if (sz < -0.7f) continue; // north gate toward the moor
            var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = $"Palisade_{logs}";
            log.transform.position = new Vector3(sx * 12f, 1.25f, 17f + sz * 12f);
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
    }

    /// <summary>
    /// Vendor NPC at the camp's west side: KayKit Adventurer (hooded rogue),
    /// capsule click target, idle take on a shared controller, trade stock
    /// for zone-visit seed 7 (re-rolled per visit via
    /// <see cref="Vendor.Restock"/>).
    /// </summary>
    public static void SpawnVendor()
    {
        var prefab = LoadModel(VendorModelPath);
        var vendor = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        vendor.name = Vendor.VendorObjectName;
        vendor.transform.position = new Vector3(-3.5f, 0f, 11f);
        var col = vendor.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.radius = 0.4f;
        col.center = new Vector3(0f, 0.9f, 0f);
        vendor.AddComponent<Vendor>().zoneSeed = 7;
        var npc = vendor.AddComponent<StoryNpc>();
        npc.npcId = StoryIds.Ilsabe;
        npc.shopNpc = true;
        WireVendorIdle(vendor);
    }

    /// <summary>
    /// cinderreach-deutsch-story S5: Ottilie Wachs by the campfire; Merle
    /// Sandkamp at the north gate (visible only once Act I is done, see
    /// <see cref="StoryNpc"/>).
    /// </summary>
    public static void SpawnStoryNpcs()
    {
        SpawnStoryNpc("Mage.fbx", StoryIds.Ottilie, new Vector3(1.5f, 0f, 14.5f), false);
        SpawnStoryNpc("Rogue.fbx", StoryIds.Merle, new Vector3(-2f, 0f, 16f), false);
    }

    static void SpawnStoryNpc(string model, string npcId, Vector3 pos, bool shop)
    {
        var prefab = LoadModel("Adventurers/Characters/" + model);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "StoryNpc_" + npcId;
        go.transform.position = pos;
        var col = go.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.radius = 0.4f;
        col.center = new Vector3(0f, 0.9f, 0f);
        var npc = go.AddComponent<StoryNpc>();
        npc.npcId = npcId;
        npc.shopNpc = shop;
        WireVendorIdle(go);
    }

    /// <summary>
    /// cinderreach-deutsch-story S5: story finds — cart letter and ledger by
    /// the fen chest, three grave tablets on the ossuary sarcophagi.
    /// cinderreach-akt1-vertiefung S2: R1/R2 finds — two cart scraps and the
    /// seal ribbon at the second cart in the fen, three tablet fragments and
    /// Thewes' grave light in the ossuary.
    /// </summary>
    public static void SpawnStoryPickups()
    {
        SpawnStoryPickup(StoryIds.Fuhrbrief, new Vector3(-10.6f, 0.5f, 1.6f), 0);
        SpawnStoryPickup(StoryIds.Lieferbuch, new Vector3(-13.4f, 0.5f, -1.6f), 0);
        // S2: zweiter Karren im Moor (R1). Positionen paarweise entzerrt,
        // damit je ein Trigger anspricht (Box 2,4 m + Heldradius).
        SpawnStoryPickup(StoryIds.Fuhrfetzen, new Vector3(-8.5f, 0.5f, 3.5f), 0);
        SpawnStoryPickup(StoryIds.Fuhrfetzen, new Vector3(-4.0f, 0.5f, 8.0f), 1);
        SpawnStoryPickup(StoryIds.Siegelband, new Vector3(-5.0f, 0.5f, 4.5f), 0);
        var o = KilnLayout.OssuaryOffset;
        SpawnStoryPickup(StoryIds.Grabtafel, o + new Vector3(-6.8f, 0.7f, -6f), 0);
        SpawnStoryPickup(StoryIds.Grabtafel, o + new Vector3(9.2f, 0.7f, -3f), 1);
        SpawnStoryPickup(StoryIds.Grabtafel, o + new Vector3(-6.8f, 0.7f, 0f), 2);
        // S2: siebte Tafel in Stuecken plus Thewes' Grablicht (R2).
        SpawnStoryPickup(StoryIds.Tafelbruch, o + new Vector3(-5.6f, 0.7f, -6f), 0);
        SpawnStoryPickup(StoryIds.Tafelbruch, o + new Vector3(8.0f, 0.7f, -3f), 1);
        SpawnStoryPickup(StoryIds.Tafelbruch, o + new Vector3(-5.6f, 0.7f, 0f), 2);
        SpawnStoryPickup(StoryIds.Grablicht, o + new Vector3(10.4f, 0.7f, -2f), 0);
        // S3: Fuhrliste am Aufgang, Rußseite auf dem Weg zur Ofenmutter (R3).
        // Positionen paarweise entzerrt (Test-Reise nutzt dieselben Stellen).
        var k = KilnLayout.KilnOffset;
        SpawnStoryPickup(StoryIds.Fuhrliste, k + new Vector3(10f, 0.7f, 8f), 0);
        SpawnStoryPickup(StoryIds.Russseite, k + new Vector3(3f, 0.7f, -5f), 0);
        // Akt II Vertiefung S2: Glasbrache (R4 Sehgläser, R5 Scherben, N12-Rad, N15-Seite).
        // Positionen paarweise entzerrt (Test-Reise nutzt dieselben Stellen).
        var b = BarrensLayout.BarrensOffset;
        SpawnStoryPickup(StoryIds.Sehglas, b + new Vector3(-5f, 0.7f, 11f), 0);
        SpawnStoryPickup(StoryIds.Sehglas, b + new Vector3(5f, 0.7f, 11f), 1);
        // Off BarrensDune_3 (its collider swallowed the old spot at (8, -7), n13 was unreachable).
        SpawnStoryPickup(StoryIds.Sehglas, b + new Vector3(11.5f, 0.7f, -6f), 2);
        SpawnStoryPickup(StoryIds.Glasscherbe, b + new Vector3(-2f, 0.7f, 7f), 0);
        SpawnStoryPickup(StoryIds.Glasscherbe, b + new Vector3(5f, 0.7f, -2f), 1);
        SpawnStoryPickup(StoryIds.Glasscherbe, b + new Vector3(-6f, 0.7f, 5f), 2);
        SpawnStoryPickup(StoryIds.Glasscherbe, b + new Vector3(-8f, 0.7f, -11f), 3);
        SpawnStoryPickup(StoryIds.Speichenrad, b + new Vector3(11f, 0.7f, -10f), 0);
        SpawnStoryPickup(StoryIds.Spendenseite, b + new Vector3(-12f, 0.7f, -1f), 0);
        // Akt III Finale S3b: Moortor-Kette (R7 Glutspuren am Moortor, N6
        // Lanzenspitzen, N7 letzte Spendenseite Index 1). Positionen paarweise
        // entzerrt (Test-Reise nutzt dieselben Stellen), an Barrens-Geometrie
        // angelehnt: Sued-Lane frei von Spiren/Duenen, Funde fern der Bestand-Finds.
        SpawnStoryPickup(StoryIds.Glutspur, b + new Vector3(-3f, 0.7f, -16f), 0);
        SpawnStoryPickup(StoryIds.Glutspur, b + new Vector3(1f, 0.7f, -17f), 1);
        SpawnStoryPickup(StoryIds.Glutspur, b + new Vector3(5f, 0.7f, -15.5f), 2);
        SpawnStoryPickup(StoryIds.Lanzenspitze, b + new Vector3(8f, 0.7f, 2f), 0);
        SpawnStoryPickup(StoryIds.Lanzenspitze, b + new Vector3(8.5f, 0.7f, -2.5f), 1);
        SpawnStoryPickup(StoryIds.Lanzenspitze, b + new Vector3(4f, 0.7f, -13f), 2);
        SpawnStoryPickup(StoryIds.Lanzenspitze, b + new Vector3(8.5f, 0.7f, -13.5f), 3);
        SpawnStoryPickup(StoryIds.Spendenseite, b + new Vector3(12f, 0.7f, 2f), 1);
    }

    static void SpawnStoryPickup(string itemId, Vector3 pos, int index)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"StoryPickup_{itemId}_{index}";
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.45f, 0.12f, 0.6f);
        go.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
        Paint(go, new Color(0.85f, 0.68f, 0.30f));
        var col = go.GetComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(2.4f, 2f, 2.4f);
        go.AddComponent<StoryPickup>().itemId = itemId;
    }

    /// <summary>
    /// replayability S5: stash chest at the camp's north-west corner (KayKit
    /// gold chest, box collider as click target, <see cref="StashChest"/>).
    /// </summary>
    public static void SpawnStashChest()
    {
        var prefab = LoadModel("Dungeon/Models/chest_gold.fbx");
        var chest = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        chest.name = StashChest.ObjectName;
        chest.transform.position = new Vector3(-3.5f, 0f, 14f);
        chest.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        var col = chest.AddComponent<BoxCollider>();
        col.size = new Vector3(1.4f, 1.1f, 1f);
        col.center = new Vector3(0f, 0.55f, 0f);
        chest.AddComponent<StashChest>();
    }

    /// <summary>
    /// zone-contracts S2: contract board at the camp's east side (plain
    /// notice post, box collider as click target, <see cref="ContractBoard"/>).
    /// </summary>
    public static void SpawnContractBoard()
    {
        var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = ContractBoard.BoardObjectName;
        post.transform.position = new Vector3(3.5f, 0.9f, 14f);
        post.transform.localScale = new Vector3(1.6f, 1.8f, 0.2f);
        Paint(post, new Color(0.42f, 0.30f, 0.16f));
        var col = post.GetComponent<BoxCollider>();
        col.isTrigger = false;
        post.AddComponent<ContractBoard>();
    }

    /// <summary>
    /// smithing S2: forge at the camp's east side, south of the board (dark
    /// anvil block, box collider as click target, <see cref="SmithStation"/>).
    /// </summary>
    public static void SpawnSmithStation()
    {
        var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = SmithStation.SmithObjectName;
        post.transform.position = new Vector3(5.5f, 0.6f, 11f);
        post.transform.localScale = new Vector3(1.8f, 1.2f, 1.2f);
        Paint(post, new Color(0.25f, 0.25f, 0.28f));
        var col = post.GetComponent<BoxCollider>();
        col.isTrigger = false;
        post.AddComponent<SmithStation>();
        var npc = post.AddComponent<StoryNpc>();
        npc.npcId = StoryIds.Jorin;
        npc.shopNpc = true;
    }

    /// <summary>Idle-only Mecanim rig (shared controller, built once per session).</summary>
    static void WireVendorIdle(GameObject vendor)
    {
        var controller = VendorIdleController();
        if (controller == null) return;
        var animator = vendor.GetComponent<Animator>();
        if (animator == null) animator = vendor.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
    }

    static RuntimeAnimatorController sharedVendorIdle;

    static RuntimeAnimatorController VendorIdleController()
    {
        if (sharedVendorIdle != null) return sharedVendorIdle;
        AnimationClip idle = null;
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(RigGeneralPath))
            if (obj is AnimationClip clip && clip.name == "Idle_A")
                idle = clip;
        if (idle == null)
        {
            Debug.LogWarning("[TownBuilder] Idle_A take missing; vendor stays static.");
            return null;
        }
        const string path = "Assets/AddressableAssets/VendorIdle.controller";
        AssetDatabase.DeleteAsset(path);
        var c = AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = c.layers[0].stateMachine;
        foreach (var s in sm.states) sm.RemoveState(s.state);
        var idleS = sm.AddState("Idle");
        idleS.motion = idle;
        sm.defaultState = idleS;
        AssetDatabase.SaveAssets();
        sharedVendorIdle = c;
        return c;
    }

    static GameObject LoadModel(string relativePath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ThirdParty/KayKit/" + relativePath);
        if (prefab == null)
            throw new System.Exception($"Town build: model missing: {relativePath}");
        return prefab;
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
