using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Runtime husk bodies (cinderreach-companion-rendering): enemies spawned
/// after scene build (RiskShrine elite, boss summons, kiln thralls) render
/// as KayKit skeleton models instead of bare capsule primitives (whose
/// default material renders magenta in the URP player). Templates come
/// from the scene itself — every builder-placed husk carries its FBX in
/// <see cref="EnemyHealth.corpseModel"/> — with an explicit model field on
/// the spawner winning over the scene template and a URP-tinted capsule
/// as the last resort (never a bare primitive).
/// </summary>
public static class HuskModelLibrary
{
    /// <summary>Built by the editor (HuskLocomotion.controller in Resources).</summary>
    public const string HuskControllerPath = "HuskLocomotion";

    /// <summary>Fallback tint when no model exists anywhere (ash grey, URP Lit).</summary>
    public static readonly Color FallbackTint = new Color(0.32f, 0.29f, 0.27f);

    /// <summary>First scene husk FBX (null when no husk stands).</summary>
    public static GameObject TemplateModel()
    {
        foreach (var h in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
        {
            if (h != null && h.corpseModel != null) return h.corpseModel;
        }
        return null;
    }

    /// <summary>Shared husk locomotion controller (null when never built).</summary>
    public static RuntimeAnimatorController HuskController() =>
        Res.Get<RuntimeAnimatorController>(HuskControllerPath);

    /// <summary>
    /// Spawn a fighting husk: skeleton model when one is known (explicit
    /// model, else the scene template), else a URP-tinted capsule.
    /// Server spawn replicates to clients; solo stays local.
    /// </summary>
    public static GameObject SpawnHusk(string name, Vector3 pos, float scale,
        int hp, LootTier tier, int damage, GameObject model, bool netServer)
    {
        model = model != null ? model : TemplateModel();
        GameObject go;
        if (model != null)
        {
            go = Object.Instantiate(model);
            go.name = name;
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var rend = go.GetComponent<Renderer>();
            if (shader != null && rend != null)
                rend.sharedMaterial = new Material(shader) { color = FallbackTint };
        }
        go.tag = "Husk";
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        var col = go.GetComponent<Collider>();
        if (col == null)
        {
            var cap = go.AddComponent<CapsuleCollider>();
            cap.height = 1.7f;
            cap.radius = 0.35f;
            cap.center = new Vector3(0f, 0.85f, 0f);
        }
        var health = go.GetComponent<EnemyHealth>();
        if (health == null) health = go.AddComponent<EnemyHealth>();
        health.SetTier(hp, tier);
        health.corpseModel = model; // host-safe corpse path (like the builder husks)
        var attack = go.GetComponent<EnemyAttack>();
        if (attack == null) attack = go.AddComponent<EnemyAttack>();
        attack.damage = damage;
        var animator = go.GetComponent<Animator>();
        if (animator == null) animator = go.AddComponent<Animator>();
        var controller = HuskController();
        if (controller != null) animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        if (go.GetComponent<HuskAnimator>() == null) go.AddComponent<HuskAnimator>();
        if (netServer) go.AddComponent<NetworkObject>().Spawn();
        return go;
    }
}
