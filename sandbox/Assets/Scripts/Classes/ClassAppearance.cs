using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class look of a hero (second-class S4). Fenwalker keeps the Knight body
/// and the starter sword. Lanternbearer re-skins the Mage meshes onto the
/// hero's own skeleton (KayKit Adventurers share one rig, bones match by
/// name), hides the Knight meshes and the sword, and carries a lantern with a
/// warm light on the right hand slot. One hero object and one network prefab
/// serve both classes, so NGO prefab lists and the scene stay unchanged.
/// </summary>
public sealed class ClassAppearance : MonoBehaviour
{
    /// <summary>Mage model asset (assigned by the builder).</summary>
    public GameObject lanternbearerModel;

    public const string SkinRootName = "ClassSkin";
    public const string LanternName = "HeldLantern";

    public string AppliedClassId { get; private set; } = ClassCatalog.FenwalkerId;

    readonly List<Renderer> baseBody = new List<Renderer>();
    GameObject skinRoot;
    GameObject lantern;

    void Start()
    {
        // Mirrors follow the owner's network class; the local hero follows the run profile.
        var net = GetComponent<DiabloNetworkPlayer>();
        if (net != null && net.IsSpawned && !net.IsOwner) return;
        Apply(RunProfile.Class);
    }

    public void Apply(ClassDefinition cls)
    {
        if (cls == null) cls = ClassCatalog.Fenwalker;
        CollectBaseBody();
        bool lanternbearer = cls.id == ClassCatalog.LanternbearerId;
        foreach (var r in baseBody) if (r != null) r.enabled = !lanternbearer;
        SetSword(!lanternbearer);
        if (lanternbearer)
        {
            if (skinRoot == null) skinRoot = BuildSkin();
            if (lantern == null) lantern = BuildLantern();
        }
        if (skinRoot != null) skinRoot.SetActive(lanternbearer);
        if (lantern != null) lantern.SetActive(lanternbearer);
        SwapStarterWeapon(cls);
        AppliedClassId = cls.id;
    }

    void CollectBaseBody()
    {
        if (baseBody.Count > 0) return;
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (skinRoot == null || !smr.transform.IsChildOf(skinRoot.transform))
                baseBody.Add(smr);
    }

    void SetSword(bool visible)
    {
        var sword = FindDeep(transform, DiabloSkills.SwordName);
        if (sword == null) return;
        foreach (var r in sword.GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
    }

    /// <summary>Mage skinned meshes bound to this hero's bones (by name). Null without a model.</summary>
    GameObject BuildSkin()
    {
        if (lanternbearerModel == null) return null;
        var bones = new Dictionary<string, Transform>();
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (!bones.ContainsKey(t.name)) bones[t.name] = t;

        var temp = Instantiate(lanternbearerModel);
        var root = new GameObject(SkinRootName);
        root.transform.SetParent(transform, false);
        foreach (var smr in temp.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mapped = new Transform[smr.bones.Length];
            for (int i = 0; i < mapped.Length; i++)
            {
                var b = smr.bones[i];
                mapped[i] = b != null && bones.TryGetValue(b.name, out var hb) ? hb : b;
            }
            smr.bones = mapped;
            if (smr.rootBone != null && bones.TryGetValue(smr.rootBone.name, out var rb)) smr.rootBone = rb;
            smr.transform.SetParent(root.transform, false);
        }
        // Rigid props (hats, staffs) hang under a bone: move them to the hero's bone of the same name.
        foreach (var mr in temp.GetComponentsInChildren<MeshRenderer>(true))
        {
            var parent = mr.transform.parent;
            if (parent != null && bones.TryGetValue(parent.name, out var hb))
                mr.transform.SetParent(hb, false);
        }
        if (Application.isPlaying) Destroy(temp); else DestroyImmediate(temp);
        return root;
    }

    GameObject BuildLantern()
    {
        var hand = FindDeep(transform, "handslot.r") ?? FindDeep(transform, "hand.r");
        var go = new GameObject(LanternName);
        go.transform.SetParent(hand != null ? hand : transform, false);
        go.transform.localPosition = hand != null ? new Vector3(0f, -0.15f, 0f) : new Vector3(0.35f, 0.8f, 0.1f);
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "LanternBody";
        var col = body.GetComponent<Collider>();
        if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
        body.transform.SetParent(go.transform, false);
        body.transform.localScale = new Vector3(0.14f, 0.2f, 0.14f);
        var r = body.GetComponent<Renderer>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (r != null && shader != null)
        {
            var mat = new Material(shader) { color = new Color(1f, 0.85f, 0.45f) };
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.3f) * 1.5f);
            r.sharedMaterial = mat;
        }
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.8f, 0.45f);
        light.range = 4.5f;
        light.intensity = 1.6f;
        return go;
    }

    /// <summary>The fixed starter weapon follows the class (sword vs. lantern); found gear stays.</summary>
    void SwapStarterWeapon(ClassDefinition cls)
    {
        var eq = GetComponent<Equipment>();
        if (eq == null || eq.weapon == null) return;
        bool isStarter = eq.weapon.baseType == ItemDatabase.StarterSwordBase || eq.weapon.baseType == ItemDatabase.StarterLanternBase;
        if (!isStarter) return;
        var want = cls.id == ClassCatalog.LanternbearerId ? ItemDatabase.CreateStarterLantern() : ItemDatabase.CreateStarterSword();
        if (eq.weapon.baseType != want.baseType) eq.weapon = want;
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var hit = FindDeep(t.GetChild(i), name);
            if (hit != null) return hit;
        }
        return null;
    }
}
