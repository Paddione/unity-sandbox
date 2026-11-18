using System.Collections;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Hit points for Diablo husks (Q5). Death drops a pickup orb (Q3 semantics:
/// yellow sphere + <see cref="ToyTrigger"/> pickup mode) and increments the
/// shared kill counter. KayKit shares one material per pack, so the hit
/// flash uses a material instance (<c>renderer.material</c>), never the
/// shared asset.
/// Co-op S2: host-authoritative HP. The server owns <see cref="NetHP"/>
/// (read everyone, write server); clients mirror it and never apply damage
/// locally (they send intents via ServerRpc, validated by HitValidator).
/// Offline / unspawned (EditMode tests): plain local HP, same as before.
/// </summary>
public sealed class EnemyHealth : NetworkBehaviour
{
    public int maxHP = 3;
    public LootTier tier = LootTier.Normal;
    /// <summary>Source model for the corpse (builder sets the KayKit FBX; null = clone fallback).</summary>
    public GameObject corpseModel;

    /// <summary>Replicated HP (server writes, everyone reads). 0 = dead.</summary>
    public readonly NetworkVariable<int> NetHP = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public static int HusksKilled;
    public static int OrbsDropped;
    public static int ItemsDropped;

    int hp;
    bool dead;

    /// <summary>Local HP (mirrors NetHP when spawned).</summary>
    public int CurrentHP => hp;

    /// <summary>True while alive (validator seam).</summary>
    public bool IsAlive => !dead;

    void Awake()
    {
        // replayability S4: scene enemies scale by difficulty when they wake
        // (play mode only, so builder/edit-mode values stay unscaled).
        if (Application.isPlaying) maxHP = DifficultyProfile.Current.ScaleHP(maxHP);
        hp = maxHP;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetHP.Value = Mathf.Max(hp, 0);
        }
        else if (NetHP.Value > 0)
        {
            hp = NetHP.Value;
        }
        NetHP.OnValueChanged += OnNetHPChanged;
    }

    public override void OnNetworkDespawn()
    {
        NetHP.OnValueChanged -= OnNetHPChanged;
    }

    void OnNetHPChanged(int oldV, int newV)
    {
        if (IsServer) return; // server already applied locally
        hp = newV;
        if (newV <= 0)
        {
            if (!dead)
            {
                dead = true;
                DiabloJuice.OnEnemyDeath(transform.position);
            }
            return;
        }
        var anim = GetComponent<HuskAnimator>();
        if (anim != null) anim.OnHit();
        StartCoroutine(Flash());
    }

    /// <summary>Set tier HP after AddComponent (builder: elites, chests).</summary>
    public void SetTier(int tierMaxHP, LootTier lootTier)
    {
        // Runtime spawns (boss summons) scale here; the builder runs in edit mode.
        maxHP = Application.isPlaying ? DifficultyProfile.Current.ScaleHP(tierMaxHP) : tierMaxHP;
        hp = maxHP;
        tier = lootTier;
        if (IsSpawned && IsServer) NetHP.Value = Mathf.Max(hp, 0);
    }

    /// <summary>Damage entry point: local solo/host, server relay as client.</summary>
    public void RequestDamage(int amount, ulong attackerClientId, bool crit = false)
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening && !nm.IsServer)
        {
            var player = nm.SpawnManager.GetLocalPlayerObject();
            var relay = player != null ? player.GetComponent<DiabloNetworkPlayer>() : null;
            var netObj = GetComponent<NetworkObject>();
            if (relay != null && netObj != null && netObj.IsSpawned)
            {
                relay.SubmitAttackServerRpc(netObj.NetworkObjectId, amount);
                DiabloJuice.OnEnemyHit(transform.position, amount, crit); // attacker sees own numbers
                return;
            }
        }
        TakeDamage(amount, crit);
    }

    /// <summary>Fraction of max HP left (target bar), clamped.</summary>
    public float HPFraction() => Mathf.Clamp01((float)hp / maxHP);

    /// <summary>
    /// Shove away from the attacker (D2 pop, no physics: render transform
    /// follows immediately, queries use distance math, never triggers).
    /// </summary>
    public void ApplyKnockback(Vector3 fromPos, float distance)
    {
        if (dead || distance <= 0f) return;
        var push = transform.position - fromPos;
        push.y = 0f;
        if (push.sqrMagnitude < 0.0001f) push = Vector3.forward;
        push.Normalize();
        var p = transform.position + push * distance;
        p.y = transform.position.y;
        transform.position = p;
    }

    /// <summary>
    /// Apply damage; kills at zero and drops a loot orb.
    /// S2 host-authoritative: spawned clients never apply directly (they mirror
    /// NetHP); the server applies and replicates. Offline/unspawned stays local.
    /// </summary>
    public void TakeDamage(int amount, bool crit = false)
    {
        if (dead) return;
        if (IsSpawned && NetworkManager.Singleton != null
            && NetworkManager.Singleton.IsListening && !IsServer) return;
        hp -= amount;
        if (IsSpawned && IsServer) NetHP.Value = Mathf.Max(hp, 0);
        DiabloJuice.OnEnemyHit(transform.position, amount, crit);
        Debug.Log($"[Diablo] {gameObject.name} took {amount} dmg ({Mathf.Max(hp, 0)}/{maxHP}).");
        if (hp <= 0)
        {
            Die();
            return;
        }
        var anim = GetComponent<HuskAnimator>();
        if (anim != null) anim.OnHit();
        StartCoroutine(Flash());
    }

    void Die()
    {
        dead = true;
        HusksKilled++;
        DiabloAudio.PlayDeath();
        DiabloJuice.OnEnemyDeath(transform.position);
        DiabloProgression.AwardKill(tier, transform.position);
        Debug.Log($"[Diablo] {gameObject.name} died (kills={HusksKilled}).");
        DropLoot();
        // Bosses must bookkeep on death, not on the next Update poll: the
        // deferred Destroy can win the race and skip the overlay forever.
        var boss = GetComponent<BossController>();
        if (boss != null) boss.OnBossDeath(boss.HeroObject());
        foreach (var hook in GetComponents<IEnemyDeathHook>()) hook.OnEnemyDeath();
        DiabloCorpse.SpawnFrom(gameObject);
        var netObj = GetComponent<NetworkObject>();
        var nm = NetworkManager.Singleton;
        if (netObj != null && netObj.IsSpawned && nm != null && nm.IsServer)
            netObj.Despawn();
        else
            Destroy(gameObject);
    }

    void DropLoot()
    {
        var rng = new System.Random();
        bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        var loot = LootDrop.SpawnLoot(transform.position + Vector3.up * 0.8f, tier, rng, networked);
        if (loot == null) DropOrb(); // no-drop roll: legacy orb keeps the Q3 loop alive
    }

    void DropOrb()
    {
        var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        OrbsDropped++;
        orb.name = $"DiabloOrb_{OrbsDropped}";
        orb.transform.position = transform.position + Vector3.up * 0.8f;
        orb.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            color = new Color(1f, 0.85f, 0.2f)
        };
        orb.GetComponent<Renderer>().sharedMaterial = mat;
        orb.GetComponent<SphereCollider>().isTrigger = true;
        orb.AddComponent<ToyTrigger>().mode = ToyTrigger.Mode.Pickup;
    }

    /// <summary>Flashes counted (test seam: proves the flash reached a renderer).</summary>
    public int flashCount;

    IEnumerator Flash()
    {
        // KayKit husks carry their meshes on children; the root has no renderer.
        var rends = GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) yield break;
        flashCount++;
        var mats = new Material[rends.Length];
        var colors = new Color[rends.Length];
        for (int i = 0; i < rends.Length; i++)
        {
            mats[i] = rends[i].material;
            colors[i] = mats[i].color;
            mats[i].color = new Color(1f, 0.25f, 0.25f);
        }
        yield return new WaitForSeconds(0.08f);
        for (int i = 0; i < mats.Length; i++)
            if (mats[i] != null) mats[i].color = colors[i];
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        HusksKilled = 0;
        OrbsDropped = 0;
        ItemsDropped = 0;
    }
}
