using UnityEngine;
using Unity.Netcode;

/// <summary>
/// P2 co-op player stub: owner gate + server-authoritative attack relay.
/// Solo (no NetworkManager): dormant, local melee path is used instead.
/// </summary>
[RequireComponent(typeof(Equipment))]
public sealed partial class DiabloNetworkPlayer : NetworkBehaviour
{
    /// <summary>Scene-local name every solo lookup uses (husks, target bar, pickups).</summary>
    public const string HeroName = "Player";

    /// <summary>
    /// Owner's class index into <see cref="ClassCatalog.All"/> (second-class S4):
    /// mirrors render the right body, nothing else depends on it.
    /// </summary>
    public readonly NetworkVariable<int> classIndex = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        itemSessionReady = false;
        if (IsOwner)
        {
            TakeOverSoloHero(gameObject);
            if (SaveService.HasSave() && SaveService.TryLoad(gameObject))
                Debug.Log("[Net] loaded local save for co-op join.");
            PublishClass();
            SubmitJoinStatsServerRpc(UnityEngine.JsonUtility.ToJson(CaptureJoinReport(gameObject)));
        }
        else
        {
            classIndex.OnValueChanged += (_, now) => ApplyClassIndex(now);
            ApplyClassIndex(classIndex.Value);
        }
    }

    /// <summary>Owner: push the run profile class to all mirrors.</summary>
    public void PublishClass()
    {
        if (!IsSpawned || !IsOwner) return;
        int idx = System.Array.IndexOf(ClassCatalog.All, RunProfile.Class);
        classIndex.Value = idx < 0 ? 0 : idx;
    }

    void ApplyClassIndex(int idx)
    {
        var look = GetComponent<ClassAppearance>();
        if (look == null) return;
        var cls = idx >= 0 && idx < ClassCatalog.All.Length ? ClassCatalog.All[idx] : ClassCatalog.Fenwalker;
        look.Apply(cls);
        Debug.Log($"[Class] mirror {gameObject.name} (client {OwnerClientId}) shows {cls.displayName}.");
    }

    /// <summary>
    /// Zone scenes ship a solo hero named "Player" at the same spawn as the
    /// network player. Once this process owns a network hero, the solo one
    /// must vanish (it only stood its controller down and stayed visible as a
    /// double) and the owned hero takes over the name locally — names are not
    /// replicated, so remote mirrors keep their clone names.
    /// Returns the deactivated solo hero (null when there was none).
    /// </summary>
    public static GameObject TakeOverSoloHero(GameObject ownedHero)
    {
        if (ownedHero == null) return null;
        var solo = GameObject.Find(HeroName);
        GameObject stoodDown = null;
        if (solo != null && solo != ownedHero && solo.GetComponent<DiabloNetworkPlayer>() == null)
        {
            solo.SetActive(false);
            stoodDown = solo;
        }
        ownedHero.name = HeroName;
        Debug.Log($"[Net] Owned hero took over '{HeroName}' (solo hero stood down={stoodDown != null}).");
        return stoodDown;
    }

    /// <summary>Attack a husk: local melee solo/host, ServerRpc as client.</summary>
    public void Attack(GameObject target, DiabloMelee melee, Transform attacker)
    {
        if (target == null || attacker == null) return;
        var health = target.GetComponent<EnemyHealth>();
        if (health == null) return;
        int damage = melee != null ? melee.CurrentDamage(attacker) : 1;

        var nm = NetworkManager.Singleton;
        var netObj = target.GetComponent<NetworkObject>();
        if (nm == null || !nm.IsListening || netObj == null || !netObj.IsSpawned)
        {
            if (melee != null) melee.TryAttack(target, attacker);
            return;
        }

        if (IsServer)
        {
            if (melee != null) melee.TryAttack(target, attacker);
        }
        else
        {
            damage = DiabloJuice.RollCrit(attacker, damage, out bool crit);
            health.RequestDamage(damage, nm.LocalClientId, crit);
        }
    }

    /// <summary>Host's player relay (server broadcast entry), null when unavailable.</summary>
    public static DiabloNetworkPlayer HostRelay()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening || !nm.IsServer || nm.SpawnManager == null) return null;
        var hostObj = nm.SpawnManager.GetLocalPlayerObject();
        var relay = hostObj != null ? hostObj.GetComponent<DiabloNetworkPlayer>() : null;
        return relay != null && relay.IsSpawned ? relay : null;
    }

    /// <summary>Server: share one kill's XP with every client (party XP).</summary>
    public void ShareKillXP(int xp, Vector3 at)
    {
        if (!IsServer || !IsSpawned) return;
        ShareKillXPClientRpc(xp, at);
    }

    [ClientRpc]
    void ShareKillXPClientRpc(int xp, Vector3 at)
    {
        if (xp <= 0 || xp > DiabloProgression.XpElite) return;
        DiabloProgression.GrantLocal(xp, at);
    }

    /// <summary>
    /// Server: broadcast a gate travel to every side (S3). Call on the host's
    /// player object (like <see cref="ShareKillXP"/>): each process fades and
    /// teleports its own hero with a spawn offset.
    /// </summary>
    public void BroadcastTravel(string gateName, Vector3 baseDest, int targetZone)
    {
        if (!IsServer || !IsSpawned) return;
        TravelClientRpc(gateName, baseDest, targetZone);
    }

    [ClientRpc]
    void TravelClientRpc(string gateName, Vector3 baseDest, int targetZone)
    {
        StartCoroutine(ZoneTravelRelay.TravelRoutine(gateName, baseDest, targetZone));
    }

    /// <summary>
    /// Server: replay a husk death on clients so they rebuild the corpse
    /// from their own husk copy (S3). The host already spawned its corpse.
    /// </summary>
    public void BroadcastCorpse(ulong huskNetId)
    {
        if (!IsServer || !IsSpawned) return;
        CorpseClientRpc(huskNetId);
    }

    [ClientRpc]
    void CorpseClientRpc(ulong huskNetId)
    {
        if (IsServer) return; // the host already spawned its corpse in Die
        ZoneTravelRelay.ApplyRemoteCorpse(huskNetId);
    }

    /// <summary>
    /// Join handshake (S5): what a client reports about its hero (level,
    /// points, gear). Serialized as JSON over the ServerRpc; the host
    /// sanitizes (clamps) before applying to the mirror.
    /// </summary>
    [System.Serializable]
    public struct JoinReport
    {
        public int level;
        public int xp;
        public int freeAttr;
        public int freeSkill;
        public int strength;
        public int vitality;
        public int focus;
        public System.Collections.Generic.List<SkillRankEntry> ranks;
        public ItemInstance armor;
        public ItemInstance weapon;
        public ItemInstance helmet;
        public NetworkItemOwnership.State items;
    }

    /// <summary>Capture this hero's join report (owner side).</summary>
    public static JoinReport CaptureJoinReport(GameObject hero)
    {
        var report = new JoinReport();
        if (hero == null) return report;
        var prog = hero.GetComponent<DiabloProgression>();
        if (prog != null)
            prog.GetState(out report.level, out report.xp, out report.freeAttr, out report.freeSkill,
                out report.strength, out report.vitality, out report.focus, out report.ranks);
        else
        {
            report.level = 1;
            report.ranks = new System.Collections.Generic.List<SkillRankEntry>();
        }
        var eq = hero.GetComponent<Equipment>();
        if (eq != null)
        {
            report.armor = eq.armor;
            report.weapon = eq.weapon;
            report.helmet = eq.helmet;
        }
        report.items = NetworkItemOwnership.Capture(hero, RunProfile.Stash);
        return report;
    }

    /// <summary>
    /// Clamp a join report to plausible values (S5 trust model: co-op among
    /// friends — the host enforces bounds, not honesty). Level 1..30, points
    /// non-negative, gear either known-catalog or dropped.
    /// </summary>
    public static JoinReport SanitizeJoinReport(JoinReport raw)
    {
        raw.level = UnityEngine.Mathf.Clamp(raw.level, 1, DiabloProgression.MaxLevel);
        if (raw.level >= DiabloProgression.MaxLevel) raw.xp = 0;
        else raw.xp = System.Math.Max(0, raw.xp);
        raw.freeAttr = System.Math.Max(0, raw.freeAttr);
        raw.freeSkill = System.Math.Max(0, raw.freeSkill);
        raw.strength = System.Math.Max(0, raw.strength);
        raw.vitality = System.Math.Max(0, raw.vitality);
        raw.focus = System.Math.Max(0, raw.focus);
        raw.armor = SanitizeGearItem(raw.armor, ItemSlot.Armor);
        raw.weapon = SanitizeGearItem(raw.weapon, ItemSlot.Weapon);
        raw.helmet = SanitizeGearItem(raw.helmet, ItemSlot.Helmet);
        return raw;
    }

    static ItemInstance SanitizeGearItem(ItemInstance item, ItemSlot slot)
    {
        if (item == null || item.slot != slot) return null;
        if (!System.Enum.IsDefined(typeof(ItemRarity), item.rarity)) item.rarity = ItemRarity.Magic;
        if (item.rarity == ItemRarity.Unique)
        {
            foreach (var u in ItemDatabase.GetUniques())
                if (u != null && u.uniqueId == item.uniqueId) return item;
            return null;
        }
        if (string.IsNullOrEmpty(item.baseType)) return null;
        if (slot == ItemSlot.Weapon && !string.IsNullOrEmpty(ItemDatabase.ClassRestriction(item.baseType)))
            return item;
        foreach (var b in ItemDatabase.GetBaseTypes(slot))
            if (b == item.baseType) return item;
        return null;
    }

    /// <summary>Apply a sanitized report to a hero (mirror on the host).</summary>
    public static void ApplyJoinReport(GameObject hero, JoinReport report)
    {
        if (hero == null) return;
        var prog = hero.GetComponent<DiabloProgression>();
        if (prog == null) prog = hero.AddComponent<DiabloProgression>();
        prog.SetState(report.level, report.xp, report.freeAttr, report.freeSkill,
            report.strength, report.vitality, report.focus, report.ranks);
        var eq = hero.GetComponent<Equipment>();
        if (eq == null) eq = hero.AddComponent<Equipment>();
        eq.armor = report.armor;
        eq.weapon = report.weapon;
        eq.helmet = report.helmet;
    }

    /// <summary>Client join report, validated and applied to the sender's mirror.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void SubmitJoinStatsServerRpc(string json, ServerRpcParams rpcParams = default)
    {
        if (!IsServer || !IsSpawned || itemSessionReady
            || OwnerClientId != rpcParams.Receive.SenderClientId) return;
        if (string.IsNullOrEmpty(json) || json.Length > 65536) return;
        JoinReport raw;
        try
        {
            raw = UnityEngine.JsonUtility.FromJson<JoinReport>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Net] rejected join report: unreadable ({e.Message}).");
            return;
        }
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.SpawnManager == null) return;
        ulong sender = rpcParams.Receive.SenderClientId;
        foreach (var kv in nm.SpawnManager.SpawnedObjects)
        {
            var nb = kv.Value;
            if (!nb.IsPlayerObject || nb.OwnerClientId != sender) continue;
            var clean = SanitizeJoinReport(raw);
            // Remote players import once from local saves. All later moves use host state.
            if (!IsOwner) ApplyJoinReport(nb.gameObject, clean);
            else
            {
                var eq = GetComponent<Equipment>();
                eq.armor?.EnsureIdentity();
                eq.weapon?.EnsureIdentity();
                eq.helmet?.EnsureIdentity();
            }
            InitializeItems(raw.items, clean, sender);
            Debug.Log($"[Net] join stats from {sender}: level {clean.level} applied to mirror.");
            return;
        }
    }

    /// <summary>Server: tell every client to autosave its own hero (boss kill).</summary>
    public void BroadcastBossKill()
    {
        if (!IsServer || !IsSpawned) return;
        NotifyBossKillClientRpc();
    }

    [ClientRpc]
    void NotifyBossKillClientRpc()
    {
        if (IsServer) return; // the host saves through OnBossDeath directly
        var nm = NetworkManager.Singleton;
        GameObject hero = null;
        if (nm != null && nm.IsListening && nm.SpawnManager != null)
        {
            var local = nm.SpawnManager.GetLocalPlayerObject();
            if (local != null) hero = local.gameObject;
        }
        if (hero == null) hero = GameObject.Find(HeroName);
        if (hero != null) SaveService.SaveNow(hero);
    }

    /// <summary>Melee skill range for validation (attack arc + tolerance in ApplyValidated).</summary>
    public const float MeleeValidateRange = 4f;

    /// <summary>Last validated melee hit per sender (cooldown gate, S2).</summary>
    static readonly System.Collections.Generic.Dictionary<ulong, float> lastMeleeHit =
        new System.Collections.Generic.Dictionary<ulong, float>();

    /// <summary>Client attack request: validated on the server, then applied.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void SubmitAttackServerRpc(ulong targetNetId, int damage, ServerRpcParams rpcParams = default)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;
        if (!nm.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var targetNb)) return;
        ApplyValidated(nm, targetNb, damage, rpcParams.Receive.SenderClientId,
            MeleeValidateRange, DiabloMeleeCooldown, lastMeleeHit);
    }

    /// <summary>Melee cooldown for the server gate (matches DiabloMelee).</summary>
    const float DiabloMeleeCooldown = 0.5f;

    /// <summary>
    /// Share a local skill cast so every other client renders it (visual only;
    /// damage stays on the caster's hit path). Host: straight to clients.
    /// </summary>
    public void BroadcastCast(int skill, Vector3 origin, Vector3 dir)
    {
        if (!IsSpawned) return;
        if (IsServer) ShowCastClientRpc(skill, origin, dir);
        else SubmitCastServerRpc(skill, origin, dir);
    }

    [ServerRpc]
    void SubmitCastServerRpc(int skill, Vector3 origin, Vector3 dir)
    {
        if (skill < 0 || skill > (int)DiabloSkills.Skill.Cleave) return;
        // Anchor the visual to the caster: reject origins far from the player.
        var delta = origin - transform.position;
        delta.y = 0f;
        if (delta.magnitude > 3f) return;
        ShowCastClientRpc(skill, origin, dir);
    }

    [ClientRpc]
    void ShowCastClientRpc(int skill, Vector3 origin, Vector3 dir)
    {
        if (IsOwner) return; // the caster already spawned the real projectile
        var skills = GetComponent<DiabloSkills>();
        if (skills == null) skills = gameObject.AddComponent<DiabloSkills>();
        skills.ShowRemoteCast((DiabloSkills.Skill)skill, origin, dir);
    }

    /// <summary>Server re-check radius for fireball/arrow hits (max skill range + slack).</summary>
    public const float RangedValidateRange = 24f;

    /// <summary>Ranged cooldown floor for the server gate (fastest skill: arrow 0.35 s).</summary>
    const float RangedCooldownFloor = 0.3f;

    /// <summary>Last validated ranged hit per sender (cooldown gate, S2).</summary>
    static readonly System.Collections.Generic.Dictionary<ulong, float> lastRangedHit =
        new System.Collections.Generic.Dictionary<ulong, float>();

    /// <summary>Client projectile hit request: validated on the server, then applied.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void SubmitRangedHitServerRpc(ulong targetNetId, int damage, ServerRpcParams rpcParams = default)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;
        if (!nm.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var targetNb)) return;
        ApplyValidated(nm, targetNb, damage, rpcParams.Receive.SenderClientId,
            RangedValidateRange, RangedCooldownFloor, lastRangedHit);
    }

    /// <summary>
    /// S2 validated hit path: HitValidator (range + cooldown + alive + damage)
    /// against the sender's player object, then host-authoritative TakeDamage.
    /// </summary>
    static void ApplyValidated(NetworkManager nm, NetworkObject targetNb, int damage, ulong sender,
        float skillRange, float cooldown,
        System.Collections.Generic.Dictionary<ulong, float> lastHit)
    {
        var health = targetNb.GetComponent<EnemyHealth>();
        if (health == null) return;
        // Range validation against the sender's player object (anti-teleportkill).
        foreach (var kv in nm.SpawnManager.SpawnedObjects)
        {
            var nb = kv.Value;
            if (!nb.IsPlayerObject || nb.OwnerClientId != sender) continue;
            float last = lastHit.TryGetValue(sender, out float t) ? t : float.NegativeInfinity;
            float now = UnityEngine.Time.time;
            string reason = HitValidator.Validate(nb.transform.position, targetNb.transform.position,
                skillRange, last, cooldown, now, health.IsAlive, damage);
            if (reason != null)
            {
                Debug.Log($"[Net] rejected hit from {sender}: {reason}.");
                return;
            }
            lastHit[sender] = now;
            health.TakeDamage(damage);
            return;
        }
    }
}
