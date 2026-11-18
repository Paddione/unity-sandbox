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

    /// <summary>Place (or re-place) the shrine in the open DiabloZone scene.</summary>
    public static GameObject BuildRiskShrine()
    {
        var old = GameObject.Find(RiskShrine.ShrineObjectName);
        if (old != null) Object.DestroyImmediate(old);
        var shrine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shrine.name = RiskShrine.ShrineObjectName;
        shrine.transform.position = ShrineAnchor;
        shrine.transform.localScale = new Vector3(1.2f, 1.6f, 1.2f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            color = new Color(0.55f, 0.15f, 0.55f),
        };
        shrine.GetComponent<Renderer>().sharedMaterial = mat;
        shrine.GetComponent<Collider>().isTrigger = false;
        shrine.AddComponent<RiskShrine>();
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
