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
        SyncCompanionLevels();
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

    /// <summary>True when this process will join someone else's session (client boot runs before net starts).</summary>
    public static bool WillJoinAsClient()
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-join" || args[i] == "--net-client" || args[i] == "-join-code"
                || args[i] == ServerDirectory.JoinServerFlag) return true;
            if (args[i] == "-coop-autoplay" && i + 1 < args.Length && args[i + 1] == "client") return true;
        }
        return false;
    }

    /// <summary>
    /// Zone-scene boot: companions only where the camp spawn exists, never in
    /// tests. Solo fills missing roles at once; a listening host keeps an
    /// empty roster so the join gate and role replacement still run. Joining
    /// clients spawn nothing — their party is the host's.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!Application.isPlaying) return;
        if (DedicatedServer.RequestedByProcess) return; // no server hero to lead AI companions
        if (WillJoinAsClient()) return; // the join happens after scene boot, while IsSolo still lies
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
        ApplyRemoteRoles(remote);
    }

    /// <summary>
    /// Join/leave path (party-team test seam, also used by the host): one
    /// same-role companion steps aside per joining role, leavers refill the
    /// roster back to a full party. Returns the removed count, or -1 when the
    /// join gate trips (then nothing changes).
    /// </summary>
    public int ApplyRemoteRoles(IList<ClassDefinition.PartyRole> remoteRoles)
    {
        var remote = remoteRoles ?? new List<ClassDefinition.PartyRole>();
        var humans = new List<ClassDefinition.PartyRole> { RunProfile.Class.role };
        humans.AddRange(remote);
        if (NeedsJoinGate(humans.Count)) return -1;
        var compRoles = new List<ClassDefinition.PartyRole>();
        foreach (var c in companions)
            if (c != null) compRoles.Add(c.Role.role);
        var pending = new List<ClassDefinition.PartyRole>(ReconcileRoles(compRoles, remote));
        int removed = 0;
        for (int i = companions.Count - 1; i >= 0; i--)
        {
            var c = companions[i];
            if (c == null) { companions.RemoveAt(i); continue; }
            if (pending.Contains(c.Role.role)) { pending.Remove(c.Role.role); RemoveCompanion(c); removed++; }
        }
        if (humans.Count + companions.Count < PartySize) FillMissingRoles(humans);
        return removed;
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
        // Full hero body of the class (cinderreach-companion-rendering):
        // the factory clones the live hero's visual rig, so companions show
        // up with the same models, gear and hair as heroes instead of
        // tinted capsule primitives.
        var go = CompanionFactory.BuildBody(cls, CompanionFactory.CompanionProfile(cls, FreeSlot()), leader);
        var comp = go.AddComponent<PartyCompanion>();
        comp.classId = cls.id;
        comp.leader = leader;
        comp.slot = FreeSlot();
        companions.Add(comp);
        if (leader != null)
        {
            go.transform.position = PartyCompanion.SlotPosition(leader.position, leader.rotation, comp.slot);
            go.transform.rotation = leader.rotation;
        }
        // New companions arrive on the protagonist's level (party-team).
        var heroProg = leader != null ? leader.GetComponent<DiabloProgression>() : null;
        if (heroProg != null) go.GetComponent<DiabloProgression>().MirrorHeroProgress(heroProg);
        Debug.Log($"[Party] {cls.displayName} joins the party ({cls.role}).");
        return comp;
    }

    /// <summary>Lowest formation slot no live companion holds.</summary>
    int FreeSlot()
    {
        for (int s = 0; ; s++)
        {
            bool taken = false;
            foreach (var c in companions)
                if (c != null && c.slot == s) { taken = true; break; }
            if (!taken) return s;
        }
    }

    /// <summary>
    /// Zone travel hook (playtest feedback 2026-11-23): when the party leader
    /// ports through a gate or waypoint, every living companion arrives with
    /// it on its formation slot instead of walking (or leashing) after it.
    /// No-op for anyone but the roster's leader.
    /// </summary>
    public static int PortWithLeader(GameObject traveller, Vector3 arrival)
    {
        var roster = Instance;
        if (roster == null || traveller == null || roster.leader != traveller.transform) return 0;
        int ported = 0;
        foreach (var c in roster.companions)
        {
            if (c == null || c.IsDead) continue;
            c.PortTo(arrival, traveller.transform.rotation);
            ported++;
        }
        if (ported > 0) Debug.Log($"[Party] {ported} companion(s) travel with the leader.");
        return ported;
    }

    /// <summary>Keep AI companions on the protagonist's level (party-team).</summary>
    void SyncCompanionLevels()
    {
        if (leader == null) return;
        var heroProg = leader.GetComponent<DiabloProgression>();
        if (heroProg == null) return;
        int key = SyncKey(heroProg);
        if (key == lastSyncKey) return;
        lastSyncKey = key;
        foreach (var c in companions)
        {
            if (c == null) continue;
            var prog = c.GetComponent<DiabloProgression>();
            if (prog != null) prog.MirrorHeroProgress(heroProg);
        }
    }

    int lastSyncKey;

    static int SyncKey(DiabloProgression p)
    {
        var s = p.Sheet;
        int h = p.Level * 397 ^ p.Xp;
        if (s != null)
            h = h * 397 ^ (s.strength * 31 + s.vitality * 17 + s.focus * 13
                + s.freeAttributePoints * 7 + s.freeSkillPoints);
        return h;
    }

}
