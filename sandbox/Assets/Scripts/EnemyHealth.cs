using System.Collections;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Hit points for Diablo husks (Q5). Death drops a pickup orb (Q3 semantics:
/// yellow sphere + <see cref="ToyTrigger"/> pickup mode) and increments the
/// shared kill counter. KayKit shares one material per pack, so the hit
/// flash uses a material instance (<c>renderer.material</c>), never the
/// shared asset.
/// </summary>
public sealed class EnemyHealth : MonoBehaviour
{
    public int maxHP = 3;
    public LootTier tier = LootTier.Normal;
    /// <summary>Source model for the corpse (builder sets the KayKit FBX; null = clone fallback).</summary>
    public GameObject corpseModel;

    public static int HusksKilled;
    public static int OrbsDropped;
    public static int ItemsDropped;

    int hp;
    bool dead;

    void Awake()
    {
        hp = maxHP;
    }

    /// <summary>Set tier HP after AddComponent (builder: elites, chests).</summary>
    public void SetTier(int tierMaxHP, LootTier lootTier)
    {
        maxHP = tierMaxHP;
        hp = tierMaxHP;
        tier = lootTier;
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

    /// <summary>Apply damage; kills at zero and drops a loot orb.</summary>
    public void TakeDamage(int amount, bool crit = false)
    {
        if (dead) return;
        if (GetComponent<BossController>() != null) return; // G-DIABLO06 fault test: boss invulnerable
        hp -= amount;
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
