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
        ServerReconcile();
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

    /// <summary>True on a listening host (the only peer that runs party logic).</summary>
    public static bool IsHost()
    {
        var nm = NetworkManager.Singleton;
        return nm != null && nm.IsListening && nm.IsServer;
    }

    /// <summary>
    /// Zone-scene boot: companions only where the camp spawn exists, never in
    /// tests. Solo fills missing roles at once; a listening host keeps an
    /// empty roster so the join gate and role replacement still run.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!Application.isPlaying) return;
        if (!IsSolo() && !IsHost()) return; // clients run no party logic
        if (GameObject.Find("CoopSpawn_0") == null) return;
        var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        if (hero == null || hero.GetComponent<PlayerHealth>() == null) return;
        var roster = Instance;
        if (roster == null)
        {
            var go = new GameObject("PartyRoster");
            roster = go.AddComponent<PartyRoster>();
            roster.leader = hero.transform;
        }
        else if (roster.leader == null)
        {
            roster.leader = hero.transform;
        }
        if (IsSolo()) roster.FillMissingRoles(new[] { RunProfile.Class.role });
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
        roster.FillMissingRoles(new[] { RunProfile.Class.role });
        return roster;
    }

    void FillMissingRoles(IList<ClassDefinition.PartyRole> humanRoles)
    {
        var covered = new HashSet<ClassDefinition.PartyRole>(humanRoles);
        foreach (var c in companions)
            if (c != null) covered.Add(c.Role.role);
        foreach (ClassDefinition.PartyRole role in System.Enum.GetValues(typeof(ClassDefinition.PartyRole)))
        {
            if (covered.Contains(role)) continue;
            if (humanRoles.Count + companions.Count >= PartySize) break;
            SpawnCompanion(ClassIdForRole(role));
            covered.Add(role);
        }
    }

    /// <summary>Host reconciliation (party-roles S5): join gate plus role replacement.</summary>
    void ServerReconcile()
    {
        if (!IsHost()) return;
        var remote = RemoteRoles(NetworkManager.Singleton);
        var humans = new List<ClassDefinition.PartyRole> { RunProfile.Class.role };
        humans.AddRange(remote);
        if (NeedsJoinGate(humans.Count))
        {
            DisconnectNewestExtra(NetworkManager.Singleton);
            return;
        }
        var compRoles = new List<ClassDefinition.PartyRole>();
        foreach (var c in companions)
            if (c != null) compRoles.Add(c.Role.role);
        var pending = new List<ClassDefinition.PartyRole>(ReconcileRoles(compRoles, remote));
        for (int i = companions.Count - 1; i >= 0; i--)
        {
            var c = companions[i];
            if (c == null) { companions.RemoveAt(i); continue; }
            if (pending.Contains(c.Role.role)) { pending.Remove(c.Role.role); RemoveCompanion(c); }
        }
        if (humans.Count + companions.Count < PartySize) FillMissingRoles(humans);
    }

    /// <summary>
    /// Pure mapping (testable): which companion roles step aside when these
    /// remote roles join — one companion per joining role.
    /// </summary>
    public static List<ClassDefinition.PartyRole> ReconcileRoles(
        IList<ClassDefinition.PartyRole> companionRoles,
        IList<ClassDefinition.PartyRole> remoteRoles)
    {
        var gone = new List<ClassDefinition.PartyRole>();
        var taken = new List<ClassDefinition.PartyRole>(remoteRoles);
        foreach (var r in companionRoles)
        {
            if (taken.Contains(r)) { gone.Add(r); taken.Remove(r); }
        }
        return gone;
    }

    /// <summary>True when the human party overflows (the join gate must refuse).</summary>
    public static bool NeedsJoinGate(int humanCount) => humanCount > PartySize;

    /// <summary>Roles of connected remote players, read from their class mirrors (host side).</summary>
    public static List<ClassDefinition.PartyRole> RemoteRoles(NetworkManager nm)
    {
        var roles = new List<ClassDefinition.PartyRole>();
        if (nm == null || nm.SpawnManager == null) return roles;
        foreach (var kv in nm.SpawnManager.SpawnedObjects)
        {
            var nb = kv.Value;
            if (nb == null || !nb.IsPlayerObject || nb.IsLocalPlayer) continue;
            var relay = nb.GetComponent<DiabloNetworkPlayer>();
            int idx = relay != null ? relay.classIndex.Value : 0;
            var cls = idx >= 0 && idx < ClassCatalog.All.Length ? ClassCatalog.All[idx] : ClassCatalog.Fenwalker;
            roles.Add(cls.role);
        }
        return roles;
    }

    void RemoveCompanion(PartyCompanion c)
    {
        companions.Remove(c);
        Debug.Log($"[Party] {c.Role.displayName} steps aside for a joining hero.");
        Object.Destroy(c.gameObject);
    }

    void DisconnectNewestExtra(NetworkManager nm)
    {
        ulong newest = 0;
        bool found = false;
        foreach (var id in nm.ConnectedClientsIds)
        {
            if (id == nm.LocalClientId) continue;
            if (!found || id > newest) { newest = id; found = true; }
        }
        if (!found) return;
        Debug.Log($"[Party] party is full ({PartySize}): refusing client {newest}.");
        nm.DisconnectClient(newest);
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
