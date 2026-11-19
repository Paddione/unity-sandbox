using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Solo party roster (cinderreach-party-roles S4). Fills the roles the local
/// hero does not cover with <see cref="PartyCompanion"/> AI, up to a party of
/// three. Boots once per zone scene (marked by CoopSpawn_0, so unit-test
/// scenes never sprout companions); co-op sessions spawn nothing here —
/// joining players replace AI roles one-to-one in S5.
/// </summary>
public sealed class PartyRoster : MonoBehaviour
{
    /// <summary>Full party size: one hero per role (tank, healer, dps).</summary>
    public const int PartySize = 3;

    public static PartyRoster Instance { get; private set; }

    /// <summary>Hero (or host player) the companions follow.</summary>
    public Transform leader;

    readonly List<PartyCompanion> companions = new List<PartyCompanion>();

    /// <summary>Live companions (dead ones waiting at camp included).</summary>
    public IReadOnlyList<PartyCompanion> Companions => companions;

    void Awake() => Instance = this;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        for (int i = companions.Count - 1; i >= 0; i--)
        {
            var c = companions[i];
            if (c == null) { companions.RemoveAt(i); continue; }
            if (c.RejoinDue) c.Rejoin();
        }
    }

    /// <summary>Class id that covers a role (tank holds, healer mends, dps kills).</summary>
    public static string ClassIdForRole(ClassDefinition.PartyRole role)
    {
        if (role == ClassDefinition.PartyRole.Tank) return ClassCatalog.FenwalkerId;
        if (role == ClassDefinition.PartyRole.Healer) return ClassCatalog.LanternbearerId;
        return ClassCatalog.RogueId;
    }

    /// <summary>True while nobody else plays along (host alone counts as solo).</summary>
    public static bool IsSolo()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening) return true;
        if (nm.IsServer) return nm.ConnectedClients.Count <= 1;
        return false; // a client rides someone else's party: S5 owns it
    }

    /// <summary>Zone-scene boot: companions only where the camp spawn exists, never in tests.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!Application.isPlaying) return;
        if (GameObject.Find("CoopSpawn_0") == null) return;
        var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        if (hero == null || hero.GetComponent<PlayerHealth>() == null) return;
        EnsureSoloParty(hero.transform);
    }

    /// <summary>Fill the roles the leader's class does not cover. Idempotent per role.</summary>
    public static PartyRoster EnsureSoloParty(Transform hero)
    {
        if (hero == null || !IsSolo()) return null;
        var roster = Instance;
        if (roster == null)
        {
            var go = new GameObject("PartyRoster");
            roster = go.AddComponent<PartyRoster>();
            roster.leader = hero;
        }
        else if (roster.leader == null)
        {
            roster.leader = hero;
        }
        roster.FillMissingRoles();
        return roster;
    }

    void FillMissingRoles()
    {
        var covered = new HashSet<ClassDefinition.PartyRole> { RunProfile.Class.role };
        foreach (var c in companions)
            if (c != null) covered.Add(c.Role.role);
        int humans = 1; // S4: the local hero; S5 counts co-op peers
        foreach (ClassDefinition.PartyRole role in System.Enum.GetValues(typeof(ClassDefinition.PartyRole)))
        {
            if (covered.Contains(role)) continue;
            if (humans + companions.Count >= PartySize) break;
            SpawnCompanion(ClassIdForRole(role));
            covered.Add(role);
        }
    }

    PartyCompanion SpawnCompanion(string companionClassId)
    {
        var cls = ClassCatalog.Get(companionClassId);
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Companion_" + cls.id;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
        var spawn = leader != null ? leader.position : Vector3.zero;
        float angle = companions.Count * 2.1f;
        go.transform.position = spawn + new Vector3(Mathf.Cos(angle) * 2f, 0.1f, Mathf.Sin(angle) * 2f);
        Tint(go, cls.role);
        go.AddComponent<PlayerHealth>();
        go.AddComponent<DiabloProgression>(); // class scaling for its kit; XP still flows to "Player" only
        go.AddComponent<DiabloSkills>();
        go.AddComponent<DiabloMelee>();
        var comp = go.AddComponent<PartyCompanion>();
        comp.classId = cls.id;
        comp.leader = leader;
        companions.Add(comp);
        Debug.Log($"[Party] {cls.displayName} joins the party ({cls.role}).");
        return comp;
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
