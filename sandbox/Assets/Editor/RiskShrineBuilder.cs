using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Risk-shrine scene wiring (cinderreach-risk-shrines S2). Places the optional
/// solo shrine on a free fen anchor and verifies it. Called from
/// <see cref="SandboxBuilder.BuildDiabloZone"/>; the scene itself is only ever
/// touched through the builder.
/// </summary>
public static class RiskShrineBuilder
{
    /// <summary>Free fen anchor: clear of husk sets, rocks, camp and the gate.
    /// Single source: <see cref="RiskShrine.ShrineAnchor"/> (runtime copy for PlayMode).</summary>
    public static Vector3 ShrineAnchor => RiskShrine.ShrineAnchor;

    /// <summary>
    /// Place (or re-place) the shrine in the open DiabloZone scene: a
    /// decorated dungeon pillar with a lit torch beside it and a violet
    /// rune disc at its base (keeps the shrine's violet identity, but as a
    /// real prop instead of the purple placeholder cylinder).
    /// </summary>
    public static GameObject BuildRiskShrine()
    {
        var old = GameObject.Find(RiskShrine.ShrineObjectName);
        if (old != null) Object.DestroyImmediate(old);
        var shrine = new GameObject(RiskShrine.ShrineObjectName);
        shrine.transform.position = ShrineAnchor;
        var pillarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ThirdParty/KayKit/Dungeon/Models/pillar_decorated.fbx");
        if (pillarPrefab != null)
        {
            var pillar = (GameObject)PrefabUtility.InstantiatePrefab(pillarPrefab);
            pillar.name = "ShrinePillar";
            pillar.transform.SetParent(shrine.transform, false);
        }
        var torchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ThirdParty/KayKit/Dungeon/Models/torch_lit.fbx");
        if (torchPrefab != null)
        {
            var torch = (GameObject)PrefabUtility.InstantiatePrefab(torchPrefab);
            torch.name = "ShrineTorch";
            torch.transform.SetParent(shrine.transform, false);
            torch.transform.localPosition = new Vector3(1.1f, 0f, 0.3f);
        }
        var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "ShrineRune";
        var discCol = disc.GetComponent<Collider>();
        if (discCol != null) Object.DestroyImmediate(discCol);
        disc.transform.SetParent(shrine.transform, false);
        disc.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        disc.transform.localScale = new Vector3(2.2f, 0.02f, 2.2f);
        var discRend = disc.GetComponent<Renderer>();
        var discShader = Shader.Find("Universal Render Pipeline/Lit");
        if (discRend != null && discShader != null)
        {
            var discMat = new Material(discShader) { color = new Color(0.55f, 0.15f, 0.55f) };
            discMat.EnableKeyword("_EMISSION");
            discMat.SetColor("_EmissionColor", new Color(0.55f, 0.15f, 0.55f) * 1.5f);
            discRend.sharedMaterial = discMat;
        }
        // Click target: the shrine answers raycasts on its own collider.
        var box = shrine.AddComponent<BoxCollider>();
        box.size = new Vector3(1.6f, 2.6f, 1.6f);
        box.center = new Vector3(0f, 1.1f, 0f);
        var glow = shrine.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(0.65f, 0.35f, 1f);
        glow.range = 5f;
        glow.intensity = 1.2f;
        var risk = shrine.AddComponent<RiskShrine>();
        risk.eliteModel = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ThirdParty/KayKit/Skeletons/Characters/Skeleton_Warrior.fbx");
        Debug.Log($"[RiskShrineBuilder] Placed {RiskShrine.ShrineObjectName} at {ShrineAnchor}.");
        return shrine;
    }

    /// <summary>Headless proof: exactly one shrine with its script on the free anchor.</summary>
    public static void VerifyRiskShrine(UnityEngine.SceneManagement.Scene scene)
    {
        var go = null as GameObject;
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == RiskShrine.ShrineObjectName) go = root;
        if (go == null)
            throw new System.Exception("RiskShrine verify failed: no RiskShrine in scene.");
        if (go.GetComponent<RiskShrine>() == null)
            throw new System.Exception("RiskShrine verify failed: no RiskShrine script.");
        if ((go.transform.position - ShrineAnchor).magnitude > 0.01f)
            throw new System.Exception("RiskShrine verify failed: shrine off its anchor.");
        Debug.Log("[RiskShrineBuilder] Verify: OK (risk shrine on its fen anchor).");
    }
}
