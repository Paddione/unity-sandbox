using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class look of a hero (second-class S4, gender bases S5). Male and
/// non-binary Fenwalkers keep the Knight body and the starter sword;
/// everyone else re-skins a stock KayKit model onto the hero's own skeleton
/// (Adventurers share one rig, bones match by name): Lanternbearers the
/// Mage, male/non-binary Rogues the Rogue, female Rogues the hooded Rogue,
/// female Fenwalkers the Ranger. The healer hides the Knight meshes and the
/// sword and carries a lantern with a warm light on the right hand slot.
/// One hero object and one network prefab serve all classes, so NGO prefab
/// lists and the scene stay unchanged.
/// </summary>
public sealed class ClassAppearance : MonoBehaviour
{
    /// <summary>Mage model asset (assigned by the builder).</summary>
    public GameObject lanternbearerModel;

    /// <summary>Rogue model asset (assigned by the builder).</summary>
    public GameObject rogueModel;

    /// <summary>Ranger model asset, the female Fenwalker base (assigned by the builder).</summary>
    public GameObject rangerModel;

    /// <summary>Hooded Rogue model asset, the female Rogue base (assigned by the builder).</summary>
    public GameObject rogueHoodedModel;

    /// <summary>Ranger base path (no ClassDefinition carries it).</summary>
    public const string RangerModelPath = "Adventurers/Characters/Ranger.fbx";

    /// <summary>Hooded Rogue base path (no ClassDefinition carries it).</summary>
    public const string RogueHoodedModelPath = "Adventurers/Characters/Rogue_Hooded.fbx";

    /// <summary>
    /// Scene boot applies the run profile here (hero). Factory clones
    /// (companions, creation preview) apply their own look and set this to
    /// false, so the next-frame Start never snaps them back to the hero.
    /// </summary>
    public bool autoApply = true;

    /// <summary>
    /// Body model for a class+gender (pure): the Knight base stays for
    /// male/non-binary Fenwalkers (null = no skin); everyone else re-skins
    /// onto the hero rig. Female bases come from the KayKit stock pack, so
    /// no new import is needed for distinct silhouettes.
    /// </summary>
    public static string SkinModelPath(ClassDefinition cls, HeroGender gender)
    {
        if (cls == null) cls = ClassCatalog.Fenwalker;
        if (cls.id == ClassCatalog.LanternbearerId) return ClassCatalog.Lanternbearer.modelPath;
        if (cls.id == ClassCatalog.RogueId)
            return gender == HeroGender.Female ? RogueHoodedModelPath : ClassCatalog.Rogue.modelPath;
        return gender == HeroGender.Female ? RangerModelPath : null;
    }

    public const string SkinRootName = "ClassSkin";
    public const string LanternName = "HeldLantern";

    public string AppliedClassId { get; private set; } = ClassCatalog.FenwalkerId;

    /// <summary>Look chosen at creation (character-creation), applied with the class.</summary>
    public HeroProfile AppliedProfile { get; private set; } = HeroProfile.Sanitized(new HeroProfile { heroName = "Wanderer" });
    public GameObject HairObject => hair;

    readonly List<Renderer> baseBody = new List<Renderer>();
    readonly Dictionary<Transform, Vector3> boneScales = new Dictionary<Transform, Vector3>();
    GameObject skinRoot;
    GameObject builtSkin;
    GameObject lantern;
    GameObject hair;
    HeroEquipmentAppearance gear;

    void Start()
    {
        // Mirrors follow the owner's network class; the local hero follows the run profile.
        var net = GetComponent<DiabloNetworkPlayer>();
        if (net != null && net.IsSpawned && !net.IsOwner) return;
        if (!autoApply) return; // factory clones carry their own look already
        Apply(RunProfile.Class, RunProfile.Hero);
    }

    public void Apply(ClassDefinition cls) => Apply(cls, AppliedProfile);

    /// <summary>Model asset for a skin path (null when the builder never assigned it).</summary>
    GameObject SkinModelAsset(ClassDefinition cls, HeroGender gender)
    {
        string path = SkinModelPath(cls, gender);
        if (path == null) return null;
        if (path == ClassCatalog.Lanternbearer.modelPath) return lanternbearerModel;
        if (path == ClassCatalog.Rogue.modelPath) return rogueModel;
        if (path == RangerModelPath) return rangerModel;
        if (path == RogueHoodedModelPath) return rogueHoodedModel;
        return null;
    }

    public void Apply(ClassDefinition cls, HeroProfile profile)
    {
        if (cls == null) cls = ClassCatalog.Fenwalker;
        AdoptClonedSkinRoot();
        CollectBaseBody();
        bool healer = cls.id == ClassCatalog.LanternbearerId;
        var gender = (HeroGender)Mathf.Clamp(profile.gender, 0, 2);
        var skin = SkinModelAsset(cls, gender);
        if (Application.isPlaying && gear == null)
        {
            if (GetComponent<Equipment>() != null)
            {
                var candidate = gameObject.AddComponent<HeroEquipmentAppearance>();
                if (candidate.Initialize(this)) gear = candidate;
                else Destroy(candidate);
            }
        }
        // The class+gender body stays visible under gear: worn items mount on
        // top of it, so every class keeps its own silhouette.
        // The Knight's built-in helmet is body, not gear: it would hide the chosen
        // hair for good. Worn helmets (HeroEquipmentAppearance) still cover it.
        foreach (var r in baseBody) if (r != null) r.enabled = skin == null && !IsBuiltInHelmet(r.name);
        SetSword(!healer);
        if (skin != builtSkin)
        {
            // The root stays stable across class switches (callers may hold
            // it); only its mapped contents are rebuilt.
            ClearSkin();
            builtSkin = skin;
        }
        if (skin != null)
        {
            if (skinRoot == null)
            {
                skinRoot = new GameObject(SkinRootName);
                skinRoot.transform.SetParent(transform, false);
            }
            if (skinRoot.transform.childCount == 0) MapSkin(skin, skinRoot);
        }
        if (healer && lantern == null) lantern = BuildLantern();
        if (skinRoot != null) skinRoot.SetActive(skin != null);
        if (lantern != null) lantern.SetActive(healer);
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

    /// <summary>Stock helmet meshes of the KayKit Knight body (hidden so the hair shows).</summary>
    public static bool IsBuiltInHelmet(string rendererName) =>
        !string.IsNullOrEmpty(rendererName) && rendererName.Contains("Helmet");

    void RebuildHair()
    {
        if (hair != null) { if (Application.isPlaying) Destroy(hair); else DestroyImmediate(hair); hair = null; }
        var head = FindDeep(transform, "head");
        if (head == null) return;
        // Clones (creation preview, companions) copy the hero's hair; drop it so only the draft hair stands.
        for (int i = head.childCount - 1; i >= 0; i--)
        {
            var old = head.GetChild(i).gameObject;
            if (old.name != HeroAppearance.HairRootName) continue;
            old.SetActive(false);
            if (Application.isPlaying) Destroy(old); else DestroyImmediate(old);
        }
        if (AppliedProfile.hairStyle == 4) return; // Glatze
        var headBounds = HeadBounds(head, out bool hooded);
        hair = BuildHair(head, AppliedProfile, headBounds, hooded);
    }

    /// <summary>Head size of a KayKit chibi base in head-bone space (used when no head mesh is found).</summary>
    public static readonly Bounds DefaultHeadBounds = new Bounds(new Vector3(0f, 0.42f, 0.01f), new Vector3(1.1f, 1.08f, 1.05f));

    /// <summary>
    /// Head mesh bounds in head-bone space per KayKit base, measured from the
    /// bind pose (the FBX imports are not readable at runtime). The chibi heads
    /// are about 1.1 m wide, so hair sized to the bone alone sat inside the head.
    /// </summary>
    static readonly Dictionary<string, Bounds> HeadBoundsByMesh = new Dictionary<string, Bounds>
    {
        { "Knight_Head", new Bounds(new Vector3(0f, 0.51f, 0.02f), new Vector3(1.09f, 1.07f, 1.01f)) },
        { "Ranger_Head", new Bounds(new Vector3(0f, 0.49f, 0.02f), new Vector3(1.09f, 1.08f, 1.02f)) },
        { "Mage_Head", new Bounds(new Vector3(0f, 0.38f, 0.01f), new Vector3(1.14f, 1.08f, 1.03f)) },
        { "Rogue_Head", new Bounds(new Vector3(0.01f, 0.37f, -0.01f), new Vector3(1.14f, 1.13f, 1.14f)) },
        { "RogueHooded_Head", new Bounds(new Vector3(0f, 0.37f, 0.01f), new Vector3(1.04f, 1.12f, 1.10f)) },
    };

    /// <summary>Head bounds of the visible base head (head-bone space); hooded bases keep their hood.</summary>
    Bounds HeadBounds(Transform head, out bool hooded)
    {
        hooded = false;
        foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (smr == null || !smr.enabled || !smr.name.EndsWith("_Head")) continue;
            hooded = smr.name.Contains("Hooded");
            return HeadBoundsFor(smr.name);
        }
        return DefaultHeadBounds;
    }

    /// <summary>Head bounds for a head mesh name (pure; unknown names get the default).</summary>
    public static Bounds HeadBoundsFor(string headMeshName) =>
        headMeshName != null && HeadBoundsByMesh.TryGetValue(headMeshName, out var b) ? b : DefaultHeadBounds;

    /// <summary>
    /// Hair parts sized to the head (head-bone space, +y up, +z face):
    /// a cap over crown and sides, side locks that frame the face (visible
    /// under hats and hoods) and the style piece behind or on top.
    /// </summary>
    public static GameObject BuildHair(Transform head, HeroProfile profile, Bounds h, bool hooded)
    {
        var root = new GameObject(HeroAppearance.HairRootName);
        root.transform.SetParent(head, false);
        var mat = HairMaterial(profile.hairColor);
        Vector3 c = h.center, s = h.size;
        Vector3 P(float x, float y, float z) => new Vector3(c.x + x * s.x, c.y + y * s.y, c.z + z * s.z);
        Vector3 S(float x, float y, float z) => new Vector3(x * s.x, y * s.y, z * s.z);
        bool longHair = profile.hairStyle == 1;
        if (!hooded)
            AddHairPart(root, "HairCap", PrimitiveType.Sphere, P(0f, 0.10f, -0.05f), S(1.06f, 0.86f, 1.04f), mat);
        float lockLen = longHair ? 0.78f : 0.42f;
        float lockY = longHair ? -0.30f : -0.12f;
        AddHairPart(root, "HairLockL", PrimitiveType.Cube, P(-0.50f, lockY, 0.12f), S(0.10f, lockLen, 0.30f), mat);
        AddHairPart(root, "HairLockR", PrimitiveType.Cube, P(0.50f, lockY, 0.12f), S(0.10f, lockLen, 0.30f), mat);
        switch (profile.hairStyle)
        {
            case 1: // Lang
                AddHairPart(root, "HairBack", PrimitiveType.Cube, P(0f, -0.30f, -0.46f), S(0.92f, 0.85f, 0.16f), mat);
                break;
            case 2: // Zopf
                AddHairPart(root, "HairTie", PrimitiveType.Sphere, P(0f, 0.02f, -0.55f), S(0.20f, 0.18f, 0.18f), mat);
                AddHairPart(root, "HairTail", PrimitiveType.Capsule, P(0f, -0.38f, -0.60f), S(0.20f, 0.36f, 0.20f), mat);
                break;
            case 3: // Kapuze: a shell over the back of the head plus a drape, the face stays open
                AddHairPart(root, "HairHood", PrimitiveType.Sphere, P(0f, 0.08f, -0.16f), S(1.10f, 0.98f, 0.92f), mat);
                AddHairPart(root, "HairDrape", PrimitiveType.Cube, P(0f, -0.40f, -0.40f), S(0.96f, 0.50f, 0.24f), mat);
                break;
            case 5: // Knoten
                AddHairPart(root, "HairBun", PrimitiveType.Sphere, P(0f, 0.50f, -0.22f), S(0.34f, 0.30f, 0.32f), mat);
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
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null) return null; // dedicated server builds ship no shaders
        return new Material(shader) { color = HeroAppearance.HairColor(colorIndex), name = "HeroHairMaterial" };
    }

    internal void SetStarterLanternVisible(bool visible)
    {
        if (lantern != null) lantern.SetActive(visible);
    }

    /// <summary>
    /// A clone (creation preview, companion) copies the hero's mapped skin
    /// but not this component's private state: take the copied root over so
    /// it is cleared instead of counted as base body.
    /// </summary>
    void AdoptClonedSkinRoot()
    {
        if (skinRoot != null) return;
        var copied = transform.Find(SkinRootName);
        if (copied != null) skinRoot = copied.gameObject;
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

    /// <summary>Drop the mapped skin contents; the root itself stays.</summary>
    void ClearSkin()
    {
        if (skinRoot == null) return;
        // Collect first: destroying a child mid-iteration invalidates the walk.
        var doomed = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in skinRoot.transform) doomed.Add(child.gameObject);
        foreach (var d in doomed)
        {
            // Destroy is deferred in play mode: detach now so the rebuild
            // (childCount == 0) maps the new skin in the same Apply.
            d.SetActive(false);
            d.transform.SetParent(null, false);
            if (Application.isPlaying) Destroy(d);
            else DestroyImmediate(d);
        }
    }

    /// <summary>Model skinned meshes bound to this hero's bones (by name) under <paramref name="root"/>.</summary>
    void MapSkin(GameObject model, GameObject root)
    {
        if (model == null || root == null) return;
        var bones = new Dictionary<string, Transform>();
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (!bones.ContainsKey(t.name)) bones[t.name] = t;

        var temp = Instantiate(model);
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
