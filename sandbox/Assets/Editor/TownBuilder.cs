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
    /// Camp: palisade ring (center 0,11, radius 6, north gap), campfire stone
    /// + ember + logs + heal trigger, camp torches (ZTorch_*). Moved
    /// verbatim from BuildMoorField — do not rename (verify counts them).
    /// </summary>
    public static void BuildCamp()
    {
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
        WireVendorIdle(vendor);
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
        const string path = "Assets/Resources/VendorIdle.controller";
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
