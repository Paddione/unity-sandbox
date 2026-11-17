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

    public override void OnNetworkSpawn()
    {
        if (IsOwner) TakeOverSoloHero(gameObject);
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

    /// <summary>Client attack request: validated on the server, then applied.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void SubmitAttackServerRpc(ulong targetNetId, int damage, ServerRpcParams rpcParams = default)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;
        if (!nm.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var targetNb)) return;
        ApplyValidated(nm, targetNb, damage, rpcParams.Receive.SenderClientId, 4f);
    }

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
        if (skill < 0 || skill > (int)DiabloSkills.Skill.Arrow) return;
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

    /// <summary>Client projectile hit request: validated on the server, then applied.</summary>
    [ServerRpc(RequireOwnership = false)]
    public void SubmitRangedHitServerRpc(ulong targetNetId, int damage, ServerRpcParams rpcParams = default)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;
        if (!nm.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var targetNb)) return;
        ApplyValidated(nm, targetNb, damage, rpcParams.Receive.SenderClientId, RangedValidateRange);
    }

    static void ApplyValidated(NetworkManager nm, NetworkObject targetNb, int damage, ulong sender, float maxRange)
    {
        var health = targetNb.GetComponent<EnemyHealth>();
        if (health == null) return;
        // Range validation against the sender's player object (anti-teleportkill).
        foreach (var kv in nm.SpawnManager.SpawnedObjects)
        {
            var nb = kv.Value;
            if (!nb.IsPlayerObject || nb.OwnerClientId != sender) continue;
            var delta = targetNb.transform.position - nb.transform.position;
            delta.y = 0f;
            if (delta.magnitude > maxRange) return;
            health.TakeDamage(damage);
            return;
        }
    }
}
