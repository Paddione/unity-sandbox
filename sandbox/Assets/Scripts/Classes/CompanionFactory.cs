using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

/// <summary>
/// Companion bodies (cinderreach-companion-rendering): CPU party members
/// render as full KayKit heroes of their class instead of tinted capsule
/// primitives. The factory clones the live hero's visual rig (Knight-based
/// meshes, Animator with the shared locomotion controller, ClassAppearance
/// with its model references, starter sword, hair) and strips every
/// gameplay component, so the companion looks exactly like a hero while
/// owning fresh, hero-independent state (Equipment, health, progression,
/// skills). No colliders: like before, companions move without a collider
/// and clamp to the zone floor.
/// </summary>
public static class CompanionFactory
{
    /// <summary>
    /// Full companion body for a class: visual hero clone with fresh
    /// gameplay state and the class look applied. Positioned at the leader
    /// (the roster moves it onto its formation slot). Falls back to a
    /// URP-tinted capsule — never a bare primitive — when no hero exists
    /// (headless/EditMode scenes without a "Player").
    /// </summary>
    public static GameObject BuildBody(ClassDefinition cls, HeroProfile profile, Transform leader)
    {
        if (cls == null) cls = ClassCatalog.Fenwalker;
        var go = CloneHeroVisual();
        if (go == null) return BuildFallbackCapsule(cls);
        go.name = "Companion_" + cls.id;
        go.tag = "Untagged";
        go.transform.localScale = Vector3.one;
        if (leader != null) go.transform.position = leader.position;
        go.transform.rotation = leader != null ? leader.rotation : Quaternion.identity;
        go.AddComponent<Equipment>(); // before PlayerHealth: MaxHP counts gear from the start
        go.AddComponent<PlayerHealth>();
        go.AddComponent<DiabloProgression>(); // class scaling for its kit; XP still flows to "Player" only
        go.AddComponent<DiabloSkills>();
        go.AddComponent<DiabloMelee>();
        var look = go.GetComponent<ClassAppearance>();
        if (look != null)
        {
            look.autoApply = false; // the clone keeps this look; Start must not snap it to the hero
            look.Apply(cls, profile);
        }
        return go;
    }

    /// <summary>
    /// Deterministic companion look (pure): the class model of the class,
    /// but a fixed per-slot gender/hair/build so the two AI partners read
    /// as distinct characters instead of hero copies.
    /// </summary>
    public static HeroProfile CompanionProfile(ClassDefinition cls, int slot)
    {
        var rng = new System.Random((cls != null ? cls.id.GetHashCode() : 0) * 31 + slot * 101 + 7);
        var p = HeroProfile.CreateDefault(rng);
        p.gender = slot % HeroAppearance.BuildCount;
        p.hairStyle = (slot * 2 + 1) % HeroAppearance.HairStyleCount;
        p.hairColor = (slot * 3 + 2) % HeroAppearance.HairColorCount;
        p.build = slot % HeroAppearance.BuildCount;
        return HeroProfile.Sanitized(p, rng);
    }

    /// <summary>
    /// Visual-only hero clone (also used by the creation preview): the live
    /// "Player" instantiated with every gameplay component, collider and
    /// built UI canvas stripped. Null when no hero stands (headless scenes).
    /// </summary>
    public static GameObject CloneHeroVisual() => CloneHeroVisual(GameObject.Find(DiabloNetworkPlayer.HeroName));

    /// <summary>Visual-only clone of a specific hero (the inventory paperdoll clones its own owner).</summary>
    public static GameObject CloneHeroVisual(GameObject hero)
    {
        if (hero == null) return null;
        var go = Object.Instantiate(hero);
        // Instantiate serializes the source, which turns its null [Serializable]
        // slots into blank ItemInstances; restore the hero's real empty slots.
        var heroEquipment = hero.GetComponent<Equipment>();
        if (heroEquipment != null) heroEquipment.NormalizeEmptySlots();
        go.name = hero.name + "_Visual";
        StripGameplay(go);
        return go;
    }

    /// <summary>Hero-only components, all colliders and built UI canvases.</summary>
    static readonly System.Type[] GameplayTypes =
    {
        typeof(DiabloClickController),
        typeof(DiabloNetworkPlayer),
        typeof(NetworkObject),
        typeof(NetworkTransform),
        typeof(CharacterController),
        typeof(Inventory),
        typeof(Wallet),
        typeof(DiabloInventoryUI),
        typeof(DiabloVendorUI),
        typeof(DiabloWaypointUI),
        typeof(DiabloContractUI),
        typeof(DiabloSmithUI),
        typeof(DiabloStashUI),
        typeof(DiabloRiskShrineUI),
        typeof(DiabloCharacterUI),
        typeof(FpsReporter),
        typeof(Equipment),
        typeof(PlayerHealth),
        typeof(DiabloProgression),
        typeof(DiabloSkills),
        typeof(DiabloMelee),
        typeof(HeroEquipmentAppearance),
        typeof(PartyCompanion),
        typeof(ItemTooltip),
        typeof(HeroPaperdollPreview),
        // cinderreach-group-quest-map: a clone must not run the story or its own
        // tracker, minimap and zone map (M would open one map per companion).
        typeof(StoryDirector),
        typeof(StoryDialogUI),
        typeof(StoryQuestUI),
        typeof(UseKey),
        typeof(MinimapHud),
        typeof(WorldMapUI),
    };

    /// <summary>Remove gameplay state from a hero clone; visuals, Animator and ClassAppearance stay.</summary>
    public static void StripGameplay(GameObject go)
    {
        if (go == null) return;
        foreach (var t in GameplayTypes)
        {
            var c = go.GetComponent(t);
            if (c != null) Object.DestroyImmediate(c);
        }
        foreach (var col in go.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);
        // Collect first: destroying a canvas parent also destroys its
        // children, and touching those dead transforms mid-iteration throws.
        var doomed = new System.Collections.Generic.List<GameObject>();
        foreach (var tr in go.GetComponentsInChildren<Transform>(true))
        {
            if (tr != null && tr.gameObject != go && tr.name.Contains("Canvas"))
                doomed.Add(tr.gameObject);
            // The hero's worn-gear art belongs to the hero's class and items, not the clone's.
            else if (tr != null && System.Array.IndexOf(HeroEquipmentAppearance.MountNames, tr.name) >= 0)
                doomed.Add(tr.gameObject);
        }
        foreach (var d in doomed)
            if (d != null) Object.DestroyImmediate(d);
    }

    /// <summary>
    /// Tinted fallback body (pure-primitive scenes only): a capsule with a
    /// proper URP Lit material per role — never the bare default material
    /// that renders magenta in the URP player.
    /// </summary>
    public static GameObject BuildFallbackCapsule(ClassDefinition cls)
    {
        if (cls == null) cls = ClassCatalog.Fenwalker;
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Companion_" + cls.id;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
        Tint(go, cls.role);
        go.AddComponent<Equipment>();
        go.AddComponent<PlayerHealth>();
        go.AddComponent<DiabloProgression>();
        go.AddComponent<DiabloSkills>();
        go.AddComponent<DiabloMelee>();
        var look = go.AddComponent<ClassAppearance>();
        look.Apply(cls, CompanionProfile(cls, 0));
        return go;
    }

    static void Tint(GameObject go, ClassDefinition.PartyRole role)
    {
        var rend = go.GetComponent<Renderer>();
        if (rend == null) return;
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return;
        var color = role == ClassDefinition.PartyRole.Tank ? new Color(0.45f, 0.55f, 0.7f)
            : role == ClassDefinition.PartyRole.Healer ? new Color(1f, 0.85f, 0.45f)
            : new Color(0.45f, 0.75f, 0.5f);
        rend.sharedMaterial = new Material(shader) { color = color };
    }
}
