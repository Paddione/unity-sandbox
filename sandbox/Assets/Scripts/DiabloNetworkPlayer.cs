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

    /// <summary>Packed hero look (character-creation, <see cref="HeroAppearance.Pack"/>): mirrors apply it with the class.</summary>
    public readonly NetworkVariable<int> appearanceCode = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// coop-hp-sync: the owner's (current, max) HP. Enemy hits stay simulated
    /// per process, so only the owner's value is real; mirrors show it
    /// (<see cref="PlayerHealth.SetMirrored"/>), which lets healers find hurt
    /// allies and keeps living/dead checks the same on every process.
    /// </summary>
    public readonly NetworkVariable<Vector2> hpState = new NetworkVariable<Vector2>(Vector2.zero,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>Owner's hero level for the party frames (cinderreach-hud-redesign).</summary>
    public readonly NetworkVariable<int> heroLevel = new NetworkVariable<int>(1,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>HP change below this is not re-sent (float noise from regen ticks).</summary>
    public const float HpSyncEpsilon = 0.05f;

    PlayerHealth ownHealth;
    DiabloProgression ownProgression;

    void Update()
    {
        if (!IsSpawned || !IsOwner) return;
        if (ownHealth == null) ownHealth = GetComponent<PlayerHealth>();
        if (ownHealth == null) return;
        var now = new Vector2(ownHealth.CurrentHP, ownHealth.MaxHP);
        if (NeedsHpSync(hpState.Value, now)) hpState.Value = now;
        if (ownProgression == null) ownProgression = GetComponent<DiabloProgression>();
        if (ownProgression != null && heroLevel.Value != ownProgression.Level) heroLevel.Value = ownProgression.Level;
    }

    /// <summary>Owner-side send rule (pure): first report, or a change beyond <see cref="HpSyncEpsilon"/>.</summary>
    public static bool NeedsHpSync(Vector2 sent, Vector2 now) =>
        sent.y <= 0f || Mathf.Abs(sent.x - now.x) > HpSyncEpsilon || Mathf.Abs(sent.y - now.y) > HpSyncEpsilon
        || (now.x <= 0f) != (sent.x <= 0f) || (now.x >= now.y) != (sent.x >= sent.y);

    void ApplyMirroredHp(Vector2 v)
    {
        var hp = GetComponent<PlayerHealth>();
        if (hp != null) hp.SetMirrored(v.x, v.y);
    }

    public override void OnNetworkSpawn()
    {
        itemSessionReady = false;
        IgnoreAllyCollisions(gameObject);
        SpawnNames();
        if (!IsOwner)
        {
            hpState.OnValueChanged += (_, now) => ApplyMirroredHp(now);
            ApplyMirroredHp(hpState.Value);
        }
        if (IsOwner)
        {
            var solo = TakeOverSoloHero(gameObject);
            // A hero created on the solo body before this client connected (-join at boot)
            // is the character to bring along, not whatever the save held before.
            if (!IsServer && solo != null && ShouldCarryHeroIntoJoin(ClassSelectUI.HeroInWorld, false))
                SaveService.SaveNow(solo);
            // Only a joining client brings its saved character along. The host (and
            // solo, which is a host nobody joined) picks Continue or a new hero on the
            // start screen; loading here made "Begin as <class>" keep the old hero's
            // level and gear and let a stale save override an explicit -class.
            if (ShouldLoadSaveOnSpawn(IsServer) && SaveService.HasSave() && SaveService.TryLoad(gameObject))
                Debug.Log("[Net] loaded local save for co-op join.");
            PublishClass();
            SubmitJoinStatsServerRpc(UnityEngine.JsonUtility.ToJson(CaptureJoinReport(gameObject)));
        }
        else
        {
            classIndex.OnValueChanged += (_, now) => ApplyClassIndex(now);
            appearanceCode.OnValueChanged += (_, __) => ApplyClassIndex(classIndex.Value);
            ApplyClassIndex(classIndex.Value);
        }
    }

    /// <summary>
    /// coop-trio: heroes pass through each other. Each hero's CharacterController
    /// otherwise body-blocks the others, and click-to-move steers in a straight
    /// line, so an ally standing in a lane stalled a teammate for good (host
    /// stuck 120 s behind a client in the trio run). Enemies and walls still block.
    /// Runs on every process for each spawned hero, so all pairs are covered.
    /// </summary>
    public static int IgnoreAllyCollisions(GameObject hero)
    {
        var mine = hero != null ? hero.GetComponent<CharacterController>() : null;
        var others = new System.Collections.Generic.List<CharacterController>();
        foreach (var p in Object.FindObjectsByType<DiabloNetworkPlayer>(FindObjectsSortMode.None))
            if (p != null && p.gameObject != hero) others.Add(p.GetComponent<CharacterController>());
        return IgnorePairs(mine, others);
    }

    /// <summary>Ignore collisions between one hero controller and each ally controller (test seam).</summary>
    public static int IgnorePairs(CharacterController mine, System.Collections.Generic.IEnumerable<CharacterController> allies)
    {
        if (mine == null || allies == null) return 0;
        int n = 0;
        foreach (var other in allies)
        {
            if (other == null || other == mine) continue;
            Physics.IgnoreCollision(mine, other, true);
            n++;
        }
        return n;
    }

    /// <summary>Spawn-time save load rule: joining clients only (pure, EditMode-pinnable).</summary>
    public static bool ShouldLoadSaveOnSpawn(bool isServer) => !isServer;

    static bool ShouldCarryHeroIntoJoin(bool heroInWorld, bool alreadyClient) =>
        MultiplayerAutoStarter.ShouldCarryHeroIntoJoin(heroInWorld, alreadyClient);

    /// <summary>Owner: push the run profile class and hero look to all mirrors.</summary>
    public void PublishClass()
    {
        if (!IsSpawned || !IsOwner) return;
        int idx = System.Array.IndexOf(ClassCatalog.All, RunProfile.Class);
        classIndex.Value = idx < 0 ? 0 : idx;
        appearanceCode.Value = HeroAppearance.Pack(RunProfile.Hero);
    }

    void ApplyClassIndex(int idx)
    {
        var look = GetComponent<ClassAppearance>();
        if (look == null) return;
        var cls = idx >= 0 && idx < ClassCatalog.All.Length ? ClassCatalog.All[idx] : ClassCatalog.Fenwalker;
        look.Apply(cls, HeroAppearance.Unpack(appearanceCode.Value, ShownName));
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
        // Dedicated server: no own player object, any spawned player carries ClientRpcs to everyone.
        if (relay == null && !nm.IsHost) relay = DedicatedServer.AnyPlayer();
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
        public ItemInstance gloves;
        public ItemInstance boots;
        public ItemInstance belt;
        public NetworkItemOwnership.State items;
        /// <summary>cinderreach-team-autoplay K3: the hero's run difficulty (a dedicated server adopts the first one).</summary>
        public string difficulty;
        public bool difficultyUnlocked;
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
            report.gloves = eq.gloves;
            report.boots = eq.boots;
            report.belt = eq.belt;
        }
        report.items = NetworkItemOwnership.Capture(hero, RunProfile.Stash);
        report.difficulty = RunProfile.Difficulty;
        report.difficultyUnlocked = RunProfile.DifficultyUnlocked;
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
        raw.gloves = SanitizeGearItem(raw.gloves, ItemSlot.Gloves);
        raw.boots = SanitizeGearItem(raw.boots, ItemSlot.Boots);
        raw.belt = SanitizeGearItem(raw.belt, ItemSlot.Belt);
        return raw;
    }

    static ItemInstance SanitizeGearItem(ItemInstance item, ItemSlot slot)
    {
        if (item == null || item.slot != slot) return null;
        if (!System.Enum.IsDefined(typeof(ItemRarity), item.rarity)) item.rarity = ItemRarity.Rare;
        ItemDatabase.RetireMagic(item);
        if (item.rarity == ItemRarity.Unique)
        {
            // S4 host catalog: known uniques (legacy, build, matrix) pass,
            // unknown IDs are rejected and never equipped.
            if (ItemDatabase.IsKnownUniqueId(item.uniqueId)) return item;
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
        eq.gloves = report.gloves;
        eq.boots = report.boots;
        eq.belt = report.belt;
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
                eq.EnsureIdentities();
            }
            InitializeItems(raw.items, clean, sender);
            Debug.Log($"[Net] join stats from {sender}: level {clean.level} applied to mirror.");
            if (DedicatedServer.Active) DedicatedServer.AdoptPartyDifficulty(raw.difficulty, raw.difficultyUnlocked, sender);
            return;
        }
    }

    /// <summary>act-two-opening S3: server tells every client the Glasswright fell (act state + autosave).</summary>
    public void BroadcastZoneBossKill()
    {
        if (!IsServer || !IsSpawned) return;
        NotifyZoneBossKillClientRpc();
    }

    [ClientRpc]
    void NotifyZoneBossKillClientRpc()
    {
        if (IsServer) return; // the host books it in GlasswrightController.OnEnemyDeath
        // A client that joined late (or missed a broadcast) catches the act chain up here.
        if (!RunProfile.Act.ActOneComplete) BookKilnmotherKill();
        bool booked = RunProfile.Act.DefeatZoneBoss();
        if (booked) ActBossGate.NoteKill(StoryIds.Glasswright, RunProfile.Story);
        Debug.Log($"[Net] zone boss kill from host: booked={booked} defeated={RunProfile.Act.ZoneBossDefeated}.");
        SaveLocalHero();
        try { MusicDirector.Ensure().PlayZone(ActProgress.Barrens); } catch { }
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
        BookAbbotKill();
        SaveLocalHero();
    }

    /// <summary>
    /// coop-trio: a client books the host's Abbot kill like solo (Cinder Kiln
    /// open), so its own save carries the act state. Kindled unlocks on Thewes.
    /// </summary>
    public static void BookAbbotKill()
    {
        bool kiln = RunProfile.Act.Unlock(ActProgress.Kiln);
        Debug.Log($"[Net] abbot kill from host: kilnOpened={kiln}.");
    }

    /// <summary>
    /// cinderreach-team-autoplay K1: server tells every client in the party
    /// that Thewes fell, so each one unlocks Kindled in its own save.
    /// </summary>
    public void BroadcastFinalBossKill()
    {
        if (!IsServer || !IsSpawned) return;
        NotifyFinalBossKillClientRpc();
    }

    [ClientRpc]
    void NotifyFinalBossKillClientRpc()
    {
        if (IsServer) return; // the host books it in StoryDirector.BookKill
        if (!RunProfile.Finale.FinalBossDefeated) ActBossGate.NoteKill(StoryIds.Thewes, RunProfile.Story);
        bool kindled = StoryDirector.BookFinalBossKill();
        Debug.Log($"[Net] thewes kill from host: kindled={kindled} unlocked={RunProfile.DifficultyUnlocked}.");
        SaveLocalHero();
    }

    /// <summary>coop-trio: a client books the host's Kilnmother kill (Act I complete, Barrens open).</summary>
    public static void BookKilnmotherKill()
    {
        if (!RunProfile.Act.IsUnlocked(ActProgress.Kiln)) RunProfile.Act.Unlock(ActProgress.Kiln);
        bool completed = RunProfile.Act.CompleteAct();
        Debug.Log($"[Net] kilnmother kill from host: actCompleted={completed} barrens={RunProfile.Act.IsUnlocked(ActProgress.Barrens)}.");
    }

    /// <summary>Server: tell every client the Kilnmother fell (Act I state + autosave).</summary>
    public void BroadcastActBossKill()
    {
        if (!IsServer || !IsSpawned) return;
        NotifyActBossKillClientRpc();
    }

    [ClientRpc]
    void NotifyActBossKillClientRpc()
    {
        if (IsServer) return; // the host books it in KilnmotherController.OnEnemyDeath
        if (!RunProfile.Act.ActOneComplete) ActBossGate.NoteKill(StoryIds.Kilnmother, RunProfile.Story);
        BookKilnmotherKill();
        SaveLocalHero();
    }

    /// <summary>cinderreach-deutsch-story S4: host broadcasts its story snapshot; clients max-merge it into their own save.</summary>
    public void BroadcastStory(string snapshot)
    {
        if (!IsServer || !IsSpawned) return;
        NotifyStoryClientRpc(snapshot ?? "");
        // cinderreach-act-boss-gating: the host's quest state releases the act bosses for everyone.
        NotifyActBossesClientRpc(ActBossGate.ReleasedMask(RunProfile.Story));
    }

    [ClientRpc]
    void NotifyActBossesClientRpc(int mask)
    {
        if (IsServer) return; // the host gates from its own quest state
        ActBossGate.HostMask = mask;
    }

    [ClientRpc]
    void NotifyStoryClientRpc(string snapshot)
    {
        if (IsServer) return; // the host books its own talks and kills directly
        bool moved = RunProfile.Story.Merge(snapshot);
        if (moved) { RunProfile.Story.Refresh(RunProfile.Act, catchUp: false); SaveLocalHero(); }
    }

    /// <summary>Client asks the host to book a finished story talk (host state counts).</summary>
    [ServerRpc(RequireOwnership = false)]
    public void SubmitStoryTalkServerRpc(string npc, ServerRpcParams rpcParams = default)
    {
        StoryDirector.BookTalkLocal(npc);
        BroadcastStory(RunProfile.Story.Snapshot());
    }

    /// <summary>cinderreach-team-autoplay K2: client forwards a collect/zone entry; the host/server books it.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void SubmitStoryEventServerRpc(int kind, string id, ServerRpcParams rpcParams = default)
    {
        if (string.IsNullOrEmpty(id) || id.Length > 64) return;
        bool moved = StoryDirector.BookForwarded(kind, id);
        Debug.Log($"[Net] story event {kind}:{id} from {rpcParams.Receive.SenderClientId}: moved={moved}.");
        if (moved) BroadcastStory(RunProfile.Story.Snapshot());
    }

    /// <summary>Autosaves this process's own hero (co-op client or solo).</summary>
    public static void SaveLocalHero()
    {
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

    /// <summary>Accepted skill ids for cast mirrors (the whole enum, appended over time).</summary>
    public static bool IsCastSkillId(int skill) => System.Enum.IsDefined(typeof(DiabloSkills.Skill), skill);

    [ServerRpc]
    void SubmitCastServerRpc(int skill, Vector3 origin, Vector3 dir)
    {
        if (!IsCastSkillId(skill)) return;
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

    /// <summary>
    /// S3 host hit feedback: replicate a host-applied hit (position, amount,
    /// crit flag) so every client shows the same number. The host already
    /// showed its own number locally in TakeDamage; the RPC skips the server.
    /// </summary>
    public void BroadcastHit(Vector3 at, int amount, bool crit)
    {
        if (!IsServer || !IsSpawned) return;
        BroadcastHitClientRpc(at, amount, crit);
    }

    [ClientRpc]
    void BroadcastHitClientRpc(Vector3 at, int amount, bool crit)
    {
        if (IsServer) return;
        DiabloJuice.OnEnemyHit(at, amount, crit);
    }

    /// <summary>Anchor slack for skill intents (lag + cursor aim, mirrors cast visuals).</summary>
    public const float SkillIntentAnchorTolerance = 3f;

    /// <summary>
    /// S3 intent gate (pure, testable without netcode): null when the intent is
    /// valid, else a short reason ("skill", "class", "cooldown", "facing",
    /// "anchor") for logs/tests. Rejects without any mutation. Damage is never
    /// an input, so there is no damage check here: the host rolls it.
    /// </summary>
    public static string ValidateSkillIntent(int skillId, ClassDefinition senderClass,
        Vector3 senderPos, Vector3 origin, Vector3 dir, float cooldownLeft)
    {
        if (!System.Enum.IsDefined(typeof(DiabloSkills.Skill), skillId))
            return $"unknown skill {skillId}";
        var skill = (DiabloSkills.Skill)skillId;
        if (senderClass == null || !senderClass.CanCast(skill))
            return $"class cannot cast {skill}";
        if (cooldownLeft > 0f)
            return $"cooldown {cooldownLeft:0.00}s left";
        var flat = dir;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f || float.IsNaN(flat.x) || float.IsNaN(flat.z))
            return "facing";
        float anchor = HitValidator.PlanarDistance(senderPos, origin);
        if (anchor > SkillIntentAnchorTolerance)
            return $"anchor {anchor:0.00}m > {SkillIntentAnchorTolerance:0.00}m";
        return null;
    }

    /// <summary>
    /// S3 client skill intent: skill id + anchor + facing, never damage. The
    /// host validates (skill id, sender class contract, facing, cooldown,
    /// anchor via HitValidator) and executes from S2 gear + authoritative
    /// skill state, replicating the result including the crit flag.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SubmitSkillCastServerRpc(int skillId, Vector3 origin, Vector3 dir,
        ServerRpcParams rpcParams = default)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;
        if (!System.Enum.IsDefined(typeof(DiabloSkills.Skill), skillId))
        {
            Debug.Log($"[Net] rejected skill cast from {rpcParams.Receive.SenderClientId}: unknown skill {skillId}.");
            return;
        }
        var skill = (DiabloSkills.Skill)skillId;
        ulong sender = rpcParams.Receive.SenderClientId;
        foreach (var kv in nm.SpawnManager.SpawnedObjects)
        {
            var nb = kv.Value;
            if (!nb.IsPlayerObject || nb.OwnerClientId != sender) continue;
            var senderRelay = nb.GetComponent<DiabloNetworkPlayer>();
            int classIdx = senderRelay != null ? senderRelay.classIndex.Value : 0;
            var senderClass = classIdx >= 0 && classIdx < ClassCatalog.All.Length
                ? ClassCatalog.All[classIdx] : ClassCatalog.Fenwalker;
            var skills = nb.GetComponent<DiabloSkills>();
            if (skills == null)
            {
                Debug.Log($"[Net] rejected skill cast from {sender}: no skills on mirror.");
                return;
            }
            string reason = ValidateSkillIntent(skillId, senderClass, nb.transform.position,
                origin, dir, skills.CooldownLeft(skill));
            if (reason != null)
            {
                Debug.Log($"[Net] rejected skill cast from {sender}: {reason}.");
                return;
            }
            skills.ServerExecuteSkill(skill, dir, senderClass);
            return;
        }
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
                if (reason.StartsWith("range")) CorrectEnemy(nb, targetNb, sender, now);
                return;
            }
            lastHit[sender] = now;
            health.TakeDamage(damage, false, nb.transform.position);
            // Server threat (party-roles S5): the sender owns this damage, never anyone else's.
            ThreatTable.BookDamage(targetNb.gameObject, nb.transform, damage);
            return;
        }
    }

    /// <summary>Min seconds between two position corrections of one enemy for one client.</summary>
    public const float EnemyCorrectionInterval = 0.5f;
    static readonly System.Collections.Generic.Dictionary<(ulong, ulong), float> lastCorrection =
        new System.Collections.Generic.Dictionary<(ulong, ulong), float>();

    /// <summary>
    /// cinderreach-team-autoplay K2: enemies move on every process (no NetworkTransform), so a
    /// client's copy can drift from the server's. A hit rejected for range tells the sender
    /// where the enemy really is (throttled per client and enemy); the client snaps its copy.
    /// </summary>
    static void CorrectEnemy(NetworkObject senderPlayer, NetworkObject enemy, ulong sender, float now)
    {
        var key = (sender, enemy.NetworkObjectId);
        if (lastCorrection.TryGetValue(key, out float last) && now - last < EnemyCorrectionInterval) return;
        lastCorrection[key] = now;
        var relay = senderPlayer.GetComponent<DiabloNetworkPlayer>();
        if (relay == null || !relay.IsSpawned) return;
        var to = new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { sender } } };
        relay.SnapEnemyClientRpc(enemy.NetworkObjectId, enemy.transform.position, to);
    }

    [ClientRpc]
    void SnapEnemyClientRpc(ulong enemyNetId, Vector3 serverPos, ClientRpcParams rpcParams = default)
    {
        if (IsServer) return;
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.SpawnManager == null
            || !nm.SpawnManager.SpawnedObjects.TryGetValue(enemyNetId, out var enemy) || enemy == null) return;
        SnapEnemy(enemy.gameObject, serverPos);
    }

    /// <summary>Moves a client's enemy copy to the server position (test seam).</summary>
    public static void SnapEnemy(GameObject enemy, Vector3 serverPos)
    {
        if (enemy == null) return;
        var cc = enemy.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        enemy.transform.position = serverPos;
        if (cc != null) cc.enabled = true;
    }

    /// <summary>Last validated heal per sender (server cooldown gate, party-roles S5).</summary>
    static readonly System.Collections.Generic.Dictionary<ulong, float> lastMendHit =
        new System.Collections.Generic.Dictionary<ulong, float>();
    static readonly System.Collections.Generic.Dictionary<ulong, float> lastRenewHit =
        new System.Collections.Generic.Dictionary<ulong, float>();

    /// <summary>Client heal request: validated on the server, then fanned out to the target's owner.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void SubmitHealServerRpc(ulong targetNetId, float amount, int kind, ServerRpcParams rpcParams = default)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;
        if (nm.SpawnManager == null) return;
        ulong sender = rpcParams.Receive.SenderClientId;
        if (!nm.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var targetNb)) return;
        // Only someone's hero is addressable here — enemies are never player
        // objects, so foreign-heal is impossible by construction. Never self:
        // mend is others-only, group self-heals never relay (DiabloSkills).
        if (!targetNb.IsPlayerObject) return;
        var targetHp = targetNb.GetComponent<PlayerHealth>();
        if (targetHp == null || targetHp.CurrentHP <= 0f) return;
        var senderNb = SenderPlayerObject(nm, sender);
        if (senderNb == null) return;
        if (targetNb.NetworkObjectId == senderNb.NetworkObjectId) return;
        bool renew = kind == (int)DiabloSkills.Skill.RenewingCircle;
        float range = renew ? DiabloSkills.GroupHealBaseRange : DiabloSkills.TargetHealBaseRange;
        float cooldown = renew ? DiabloSkills.RenewBaseCooldown : DiabloSkills.MendBaseCooldown;
        var lastHit = renew ? lastRenewHit : lastMendHit;
        float last = lastHit.TryGetValue(sender, out float t) ? t : float.NegativeInfinity;
        string reason = HitValidator.ValidateHeal(senderNb.transform.position, targetNb.transform.position,
            range, last, cooldown, UnityEngine.Time.time, amount);
        if (reason != null)
        {
            Debug.Log($"[Net] rejected heal from {sender}: {reason}.");
            return;
        }
        lastHit[sender] = UnityEngine.Time.time;
        ThreatTable.BookHeal(senderNb.transform, amount);
        HealTargetClientRpc(targetNetId, amount, sender);
    }

    /// <summary>Someone's player object by owner, or null.</summary>
    static NetworkObject SenderPlayerObject(NetworkManager nm, ulong sender)
    {
        foreach (var kv in nm.SpawnManager.SpawnedObjects)
        {
            var nb = kv.Value;
            if (nb.IsPlayerObject && nb.OwnerClientId == sender) return nb;
        }
        return null;
    }

    /// <summary>
    /// Server heal fan-out (party-roles S5): only the target's owner applies
    /// the HP to its real hero; every other client books the sender's threat
    /// (the sender already booked its own). Host calls this straight after a
    /// local heal, clients reach it through <see cref="SubmitHealServerRpc"/>.
    /// </summary>
    [ClientRpc]
    public void HealTargetClientRpc(ulong targetNetId, float amount, ulong senderClientId)
    {
        if (amount <= 0f || amount > HitValidator.MaxHeal) return;
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.SpawnManager == null) return;
        if (!nm.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var targetNb)) return;
        var hp = targetNb.GetComponent<PlayerHealth>();
        if (hp == null || hp.CurrentHP <= 0f) return;
        if (targetNb.IsOwner)
        {
            float got = hp.Heal(amount);
            Debug.Log($"[Net] heal from {senderClientId}: +{got:0.#} → {hp.CurrentHP:0.#}/{hp.MaxHP:0.#}.");
        }
        if (nm.LocalClientId == senderClientId) return; // sender already booked its own threat
        foreach (var kv in nm.SpawnManager.SpawnedObjects)
        {
            var nb = kv.Value;
            if (!nb.IsPlayerObject || nb.OwnerClientId != senderClientId) continue;
            ThreatTable.BookHeal(nb.transform, amount);
            break;
        }
    }
}
