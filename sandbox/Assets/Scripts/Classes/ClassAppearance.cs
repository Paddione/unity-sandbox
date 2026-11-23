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

    /// <summary>Look chosen at creation (character-creation), applied with the class.</summary>
    public HeroProfile AppliedProfile { get; private set; } = HeroProfile.Sanitized(new HeroProfile { heroName = "Wanderer" });
    public GameObject HairObject => hair;

    readonly List<Renderer> baseBody = new List<Renderer>();
    readonly Dictionary<Transform, Vector3> boneScales = new Dictionary<Transform, Vector3>();
    GameObject skinRoot;
    GameObject lantern;
    GameObject hair;
    HeroEquipmentAppearance gear;

    void Start()
    {
        // Mirrors follow the owner's network class; the local hero follows the run profile.
        var net = GetComponent<DiabloNetworkPlayer>();
        if (net != null && net.IsSpawned && !net.IsOwner) return;
        Apply(RunProfile.Class, RunProfile.Hero);
    }

    public void Apply(ClassDefinition cls) => Apply(cls, AppliedProfile);

    public void Apply(ClassDefinition cls, HeroProfile profile)
    {
        if (cls == null) cls = ClassCatalog.Fenwalker;
        CollectBaseBody();
        bool lanternbearer = cls.id == ClassCatalog.LanternbearerId;
        if (Application.isPlaying && gear == null)
        {
            var source = Resources.Load<HeroArtSource>("CinderreachHeroArt");
            if (source != null && source.LightClothing != null && GetComponent<Equipment>() != null)
            {
                var candidate = gameObject.AddComponent<HeroEquipmentAppearance>();
                if (candidate.Initialize(this, source.LightClothing)) gear = candidate;
                else Destroy(candidate);
            }
        }
        foreach (var r in baseBody) if (r != null) r.enabled = gear == null && !lanternbearer;
        SetSword(!lanternbearer);
        if (lanternbearer)
        {
            if (gear == null && skinRoot == null) skinRoot = BuildSkin();
            if (lantern == null) lantern = BuildLantern();
        }
        if (skinRoot != null) skinRoot.SetActive(gear == null && lanternbearer);
        if (lantern != null) lantern.SetActive(lanternbearer);
        SwapStarterWeapon(cls);
        AppliedClassId = cls.id;
        AppliedProfile = HeroProfile.Sanitized(profile);
        ApplyProportions();
        RebuildHair();
    }

    void LateUpdate()
    {
        // A worn helmet covers the hair; unequipping brings it back.
        if (hair == null) return;
        var eq = GetComponent<Equipment>();
        bool covered = eq != null && !Equipment.IsBlank(eq.helmet);
        if (hair.activeSelf == covered) hair.SetActive(!covered);
    }

    /// <summary>Gender+build bone scales (cosmetic only; controller and stats never read them).</summary>
    void ApplyProportions()
    {
        HeroAppearance.GetProportions((HeroGender)AppliedProfile.gender, (HeroBuild)AppliedProfile.build, out var scales);
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            bool chest = t.name == "chest" || t.name == "spine";
            bool arm = t.name.StartsWith("shoulder") || t.name.StartsWith("upperarm") || t.name.StartsWith("arm");
            if (!chest && !arm) continue;
            if (!boneScales.TryGetValue(t, out var original)) { original = t.localScale; boneScales[t] = original; }
            float f = chest ? scales.x : scales.z;
            t.localScale = new Vector3(original.x * f, original.y * (chest ? scales.y : f), original.z * f);
        }
    }

    void RebuildHair()
    {
        if (hair != null) { if (Application.isPlaying) Destroy(hair); else DestroyImmediate(hair); hair = null; }
        if (AppliedProfile.hairStyle == 4) return; // Glatze
        var head = FindDeep(transform, "head");
        if (head == null) return;
        hair = BuildHair(head, AppliedProfile);
    }

    static GameObject BuildHair(Transform head, HeroProfile profile)
    {
        var root = new GameObject(HeroAppearance.HairRootName);
        root.transform.SetParent(head, false);
        var mat = HairMaterial(profile.hairColor);
        AddHairPart(root, "HairCap", PrimitiveType.Sphere, new Vector3(0f, 0.10f, -0.01f), new Vector3(0.24f, 0.17f, 0.24f), mat);
        switch (profile.hairStyle)
        {
            case 1: // Lang
                AddHairPart(root, "HairBack", PrimitiveType.Cube, new Vector3(0f, -0.08f, -0.11f), new Vector3(0.20f, 0.34f, 0.07f), mat);
                break;
            case 2: // Zopf
                AddHairPart(root, "HairTail", PrimitiveType.Capsule, new Vector3(0f, -0.10f, -0.13f), new Vector3(0.08f, 0.22f, 0.08f), mat);
                break;
            case 3: // Kapuze
                AddHairPart(root, "HairHood", PrimitiveType.Sphere, new Vector3(0f, 0.06f, -0.03f), new Vector3(0.30f, 0.26f, 0.30f), mat);
                break;
            case 5: // Knoten
                AddHairPart(root, "HairBun", PrimitiveType.Sphere, new Vector3(0f, 0.22f, -0.05f), new Vector3(0.11f, 0.10f, 0.11f), mat);
                break;
        }
        return root;
    }

    static void AddHairPart(GameObject root, string name, PrimitiveType kind, Vector3 pos, Vector3 scale, Material mat)
    {
        var part = GameObject.CreatePrimitive(kind);
        part.name = name;
        var col = part.GetComponent<Collider>();
        if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
        part.transform.SetParent(root.transform, false);
        part.transform.localPosition = pos;
        part.transform.localScale = scale;
        var r = part.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = mat;
    }

    static Material HairMaterial(int colorIndex)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
        { color = HeroAppearance.HairColor(colorIndex), name = "HeroHairMaterial" };
        return mat;
    }

    internal void SetStarterLanternVisible(bool visible)
    {
        if (lantern != null) lantern.SetActive(visible);
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

    /// <summary>The fixed starter weapon follows the class (sword vs. lantern vs. bow); found gear stays.</summary>
    void SwapStarterWeapon(ClassDefinition cls)
    {
        var eq = GetComponent<Equipment>();
        if (eq == null || eq.weapon == null) return;
        bool isStarter = eq.weapon.baseType == ItemDatabase.StarterSwordBase
            || eq.weapon.baseType == ItemDatabase.StarterLanternBase
            || eq.weapon.baseType == ItemDatabase.StarterBowBase;
        if (!isStarter) return;
        var want = cls.id == ClassCatalog.LanternbearerId ? ItemDatabase.CreateStarterLantern()
            : cls.id == ClassCatalog.RogueId ? ItemDatabase.CreateStarterBow()
            : ItemDatabase.CreateStarterSword();
        if (eq.weapon.baseType == want.baseType) return;
        want.instanceId = eq.weapon.instanceId;
        eq.weapon = want;
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
