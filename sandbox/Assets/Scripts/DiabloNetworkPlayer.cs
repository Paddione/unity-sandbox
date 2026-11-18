using UnityEngine;
using Unity.Netcode;

/// <summary>
/// P2 co-op player stub: owner gate + server-authoritative attack relay.
/// Solo (no NetworkManager): dormant, local melee path is used instead.
/// </summary>
[RequireComponent(typeof(Equipment))]
public sealed class DiabloNetworkPlayer : NetworkBehaviour
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
        if (IsOwner)
        {
            TakeOverSoloHero(gameObject);
            PublishClass();
            var ui = GetComponent<DiabloVendorUI>();
            if (ui != null) ui.RestockForOwner(); // per-player stock (S4, seed + id)
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
    /// Remaining per-player vendor rows (S4): base-roll indices still for sale.
    /// The host owns one list per client (seed + id); the client mirrors it by
    /// removing rows only through approved buys, so both sides map row numbers
    /// to the same base offers.
    /// </summary>
    static readonly System.Collections.Generic.Dictionary<ulong, System.Collections.Generic.List<int>>
        vendorRemaining = new System.Collections.Generic.Dictionary<ulong, System.Collections.Generic.List<int>>();

    /// <summary>Clear per-player vendor state (tests / new session).</summary>
    public static void ResetCoopEconomy() => vendorRemaining.Clear();

    static System.Collections.Generic.List<int> RemainingFor(ulong sender, int baseCount)
    {
        if (!vendorRemaining.TryGetValue(sender, out var list) || list == null)
        {
            list = new System.Collections.Generic.List<int>(baseCount);
            for (int i = 0; i < baseCount; i++) list.Add(i);
            vendorRemaining[sender] = list;
        }
        return list;
    }

    /// <summary>
    /// Vendor UI entry (S4): solo/offline buys straight from the purse; a host
    /// with clients validates and applies directly; a client sends a ServerRpc
    /// and applies on approval. Funds and bag space are enforced at apply time
    /// by <see cref="VendorStock.TryBuy"/>; the host owns range, existence and
    /// once-only over the per-player row lists.
    /// </summary>
    public void RequestVendorBuy(int offerIndex, DiabloVendorUI ui)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening || !IsSpawned
            || (nm.IsServer && !ZoneGate.HasClientMirrors()))
        {
            ui?.ApplyApprovedBuy(offerIndex);
            return;
        }
        if (nm.IsServer)
        {
            if (TryServerBuy(nm.LocalClientId, offerIndex)) ui?.ApplyApprovedBuy(offerIndex);
            return;
        }
        SubmitVendorBuyServerRpc(offerIndex);
    }

    /// <summary>Vendor UI sell entry (S4): same routing as buys.</summary>
    public void RequestVendorSell(int bagIndex, DiabloVendorUI ui)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening || !IsSpawned
            || (nm.IsServer && !ZoneGate.HasClientMirrors()))
        {
            ui?.ApplyApprovedSell(bagIndex);
            return;
        }
        var inv = GetComponent<Inventory>();
        int price = 0;
        if (inv != null)
        {
            var item = bagIndex >= 0 && bagIndex < inv.Count ? inv.Get(bagIndex) : null;
            if (item != null) price = VendorStock.SellPrice(item);
        }
        if (nm.IsServer)
        {
            if (TryServerSell(nm.LocalClientId, bagIndex, price)) ui?.ApplyApprovedSell(bagIndex);
            return;
        }
        SubmitVendorSellServerRpc(bagIndex, price);
    }

    /// <summary>Server buy validation: range, row exists, row not sold yet.</summary>
    public static bool TryServerBuy(ulong sender, int offerIndex)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening || !nm.IsServer || nm.SpawnManager == null) return false;
        var vendorGo = GameObject.Find(Vendor.VendorObjectName);
        var vendor = vendorGo != null ? vendorGo.GetComponent<Vendor>() : null;
        if (vendor == null) return false;
        var heroT = SenderHeroTransform(nm, sender);
        if (heroT == null) return false;
        var delta = heroT.position - vendor.transform.position;
        delta.y = 0f;
        if (TradeValidator.CanBuyRange(delta.magnitude) != null) return false;
        var baseOffers = VendorStock.Roll(Vendor.StockSeed(vendor.zoneSeed, sender)).offers;
        var remaining = RemainingFor(sender, baseOffers.Count);
        if (offerIndex < 0 || offerIndex >= remaining.Count) return false;
        remaining.RemoveAt(offerIndex);
        return true;
    }

    /// <summary>Server sell validation: range and a sane resale price.</summary>
    public static bool TryServerSell(ulong sender, int bagIndex, int price)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening || !nm.IsServer || nm.SpawnManager == null) return false;
        if (bagIndex < 0) return false;
        var vendorGo = GameObject.Find(Vendor.VendorObjectName);
        var vendor = vendorGo != null ? vendorGo.GetComponent<Vendor>() : null;
        if (vendor == null) return false;
        var heroT = SenderHeroTransform(nm, sender);
        if (heroT == null) return false;
        var delta = heroT.position - vendor.transform.position;
        delta.y = 0f;
        return TradeValidator.CanSell(delta.magnitude, price) == null;
    }

    static Transform SenderHeroTransform(NetworkManager nm, ulong sender)
    {
        foreach (var kv in nm.SpawnManager.SpawnedObjects)
        {
            var nb = kv.Value;
            if (!nb.IsPlayerObject || nb.OwnerClientId != sender) continue;
            return nb.transform;
        }
        return null;
    }

    [ServerRpc(RequireOwnership = false)]
    void SubmitVendorBuyServerRpc(int offerIndex, ServerRpcParams rpcParams = default)
    {
        if (!IsServer || !IsSpawned) return;
        ulong sender = rpcParams.Receive.SenderClientId;
        if (TryServerBuy(sender, offerIndex)) ApproveBuyClientRpc(offerIndex, sender);
        else Debug.Log($"[Vendor] rejected buy {offerIndex} from {sender}.");
    }

    [ClientRpc]
    void ApproveBuyClientRpc(int offerIndex, ulong buyer)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.LocalClientId != buyer) return;
        var local = nm.SpawnManager != null ? nm.SpawnManager.GetLocalPlayerObject() : null;
        var ui = local != null ? local.GetComponent<DiabloVendorUI>()
            : GameObject.Find(DiabloNetworkPlayer.HeroName)?.GetComponent<DiabloVendorUI>();
        if (ui != null) ui.ApplyApprovedBuy(offerIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    void SubmitVendorSellServerRpc(int bagIndex, int price, ServerRpcParams rpcParams = default)
    {
        if (!IsServer || !IsSpawned) return;
        ulong sender = rpcParams.Receive.SenderClientId;
        if (TryServerSell(sender, bagIndex, price)) ApproveSellClientRpc(bagIndex, sender);
        else Debug.Log($"[Vendor] rejected sell {bagIndex} from {sender}.");
    }

    [ClientRpc]
    void ApproveSellClientRpc(int bagIndex, ulong seller)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || nm.LocalClientId != seller) return;
        var local = nm.SpawnManager != null ? nm.SpawnManager.GetLocalPlayerObject() : null;
        var ui = local != null ? local.GetComponent<DiabloVendorUI>()
            : GameObject.Find(DiabloNetworkPlayer.HeroName)?.GetComponent<DiabloVendorUI>();
        if (ui != null) ui.ApplyApprovedSell(bagIndex);
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
