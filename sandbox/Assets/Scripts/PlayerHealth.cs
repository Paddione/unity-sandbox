using UnityEngine;

/// <summary>
/// Hero hit points for the Ashen Fen (campfire heal target). Max tracks
/// <see cref="Equipment"/> so armor still matters. Husk strikes
/// (<see cref="EnemyAttack"/>) drain it; at 0 the hero respawns at camp.
/// Local-only per client, like <see cref="Equipment"/>. Shown by
/// <see cref="DiabloLifeGlobe"/>, which the first hero brings up.
/// </summary>
public sealed class PlayerHealth : MonoBehaviour
{
    float currentHP = -1f;

    Equipment equipment;

    /// <summary>Time.time of the last death, -inf before any (HUD message seam).</summary>
    public static float LastDeathTime = float.NegativeInfinity;

    void Awake()
    {
        equipment = GetComponent<Equipment>();
        Refresh();
        if (Application.isPlaying) DiabloLifeGlobe.Ensure();
    }

    public float MaxHP => equipment != null ? equipment.GetMaxHP() : Equipment.BaseMaxHP;

    public float CurrentHP
    {
        get
        {
            if (currentHP < 0f) currentHP = MaxHP;
            return Mathf.Min(currentHP, MaxHP);
        }
    }

    /// <summary>
    /// Mercy-shroud absorb (S4): granted by Mending Light while the healer
    /// wears the vest. Server-authoritative: the non-cosmetic heal runs on the
    /// authority (solo/host) and the absorb lives on the target's own health,
    /// so host and client agree via the replicated HP. Runtime-only: a reload
    /// rebuilds the hero without a shield.
    /// </summary>
    float shieldAmount;
    float shieldUntil = float.NegativeInfinity;

    /// <summary>Live absorb (0 when expired).</summary>
    public float ShieldAmount => Time.time <= shieldUntil ? shieldAmount : 0f;

    /// <summary>Grant an absorb shield for seconds (refreshes any running shield).</summary>
    public void GrantShield(float amount, float duration)
    {
        if (amount <= 0f || duration <= 0f) return;
        shieldAmount = amount;
        shieldUntil = Time.time + duration;
    }

    /// <summary>Eat the shield first, return the unabsorbed rest.</summary>
    float AbsorbShield(float amount)
    {
        float live = ShieldAmount;
        if (live <= 0f || amount <= 0f) return amount;
        float eaten = Mathf.Min(live, amount);
        shieldAmount = live - eaten;
        return amount - eaten;
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        currentHP = Mathf.Clamp(CurrentHP + amount, 0f, MaxHP);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;
        amount = AbsorbShield(amount);
        if (amount <= 0f) return;
        currentHP = Mathf.Clamp(CurrentHP - amount, 0f, MaxHP);
    }

    /// <summary>
    /// Directional damage: a held frontal <see cref="DiabloBlock"/> halves
    /// the hit, anything else lands unreduced. Undirected callers keep the
    /// full-damage path above. S4 hooks share this path: the mercy shield
    /// absorbs first, then the bulwark-carrier books threat on the attacker
    /// (nearest husk at the reported attacker position) when the hit was
    /// actually blocked. No static cross-owner state: threat lands on the
    /// attacker's own table with this hero as source.
    /// </summary>
    public void TakeDamage(float amount, Vector3 fromPos)
    {
        if (amount <= 0f) return;
        var block = GetComponent<DiabloBlock>();
        bool blocked = block != null && block.BlocksFrom(fromPos);
        if (blocked)
            amount = DiabloBlock.Reduce(amount);
        amount = AbsorbShield(amount);
        if (blocked)
        {
            var eq = GetComponent<Equipment>();
            if (BuildUniqueEffects.HasBulwark(eq))
            {
                var attacker = HuskAt(fromPos);
                if (attacker != null) BuildUniqueEffects.BookBulwarkThreat(attacker, transform);
            }
        }
        if (amount <= 0f) return;
        currentHP = Mathf.Clamp(CurrentHP - amount, 0f, MaxHP);
    }

    /// <summary>
    /// Attacker lookup for the bulwark hook: melee strikers report their own
    /// position (<see cref="EnemyAttack"/>, lancer/thrall/boss slams), so the
    /// nearest husk at that point is the attacker. Hostile projectile impacts
    /// report the blast point near the hero and match nothing, so blocked
    /// arrows never misbook threat.
    /// </summary>
    static GameObject HuskAt(Vector3 pos)
    {
        GameObject best = null;
        float bestD = 0.75f;
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk == null || husk.GetComponent<EnemyHealth>() == null) continue;
            var d = husk.transform.position - pos;
            d.y = 0f;
            if (d.magnitude <= bestD) { bestD = d.magnitude; best = husk; }
        }
        return best;
    }

    /// <summary>Clamp current HP after equipment swaps change the max.</summary>
    public void Refresh()
    {
        currentHP = Mathf.Clamp(CurrentHP, 0f, MaxHP);
    }

    /// <summary>
    /// Death: respawn at Lantern Hold with full HP (D2 corpse run
    /// is out of scope). CC-safe teleport (disable/move/enable + sync).
    /// </summary>
    public void DieAndRespawn()
    {
        var spawn = GameObject.Find("CoopSpawn_0");
        Vector3 pos = spawn != null ? spawn.transform.position : new Vector3(0f, 0.1f, 11f);
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = pos;
        if (cc != null) cc.enabled = true;
        Physics.SyncTransforms();
        currentHP = MaxHP;
        LastDeathTime = Time.time;
        Debug.Log("[Diablo] Hero died — respawned at Lantern Hold.");
    }
}
