using UnityEngine;

/// <summary>
/// Refillable healing flask (gear-slots S2), replaces the bought potion.
/// Key 1 drinks: <see cref="HealFraction"/> of max HP over
/// <see cref="HealSeconds"/> in even ticks (never crits, damage never cancels
/// it). A spent charge returns after <see cref="CooldownSeconds"/>; the Lantern
/// Hold and every Risk Shrine refill the flask at once.
///
/// Authority: solo and the host decide charges and cooldown. A client asks the
/// host (<see cref="DiabloNetworkPlayer"/> flask RPCs) and only starts the heal
/// when the host confirms, because HP is written by the owning client. Nothing
/// is saved: a fresh session starts with a full flask.
/// </summary>
public sealed class HealingFlask : MonoBehaviour
{
    public const float HealFraction = 0.4f;
    public const float HealSeconds = 5f;
    public const float TickSeconds = 0.5f;
    public const int Ticks = 10; // HealSeconds / TickSeconds
    public const float CooldownSeconds = 60f;
    /// <summary>Seconds between refill checks (town and shrine reach).</summary>
    public const float RefillCheckSeconds = 0.5f;
    /// <summary>Sanity cap on a relayed flask heal (40 % of a capped hero stays far below).</summary>
    public const float MaxNetHeal = 2000f;

    int charges = -1;
    float nextChargeAt = float.PositiveInfinity;
    int ticksLeft;
    float tickAmount;
    float nextTickAt;
    float nextRefillCheck;

    /// <summary>Drinks the owner confirmed (test and autoplay seam).</summary>
    public int Drinks { get; private set; }
    /// <summary>Drinks this authority granted (host-side test seam).</summary>
    public int Granted { get; private set; }
    /// <summary>HP restored by flask ticks on this hero (test seam).</summary>
    public float HealedTotal { get; private set; }

    public static HealingFlask For(GameObject hero)
    {
        if (hero == null) return null;
        var flask = hero.GetComponent<HealingFlask>();
        return flask != null ? flask : hero.AddComponent<HealingFlask>();
    }

    Equipment Gear => GetComponent<Equipment>();

    public int MaxCharges => GearUniqueEffects.FlaskCharges(Gear);
    public float Cooldown => GearUniqueEffects.FlaskCooldown(Gear);

    public int Charges
    {
        get
        {
            if (charges < 0) charges = MaxCharges;
            return Mathf.Min(charges, MaxCharges);
        }
    }

    public bool IsFull => Charges >= MaxCharges;
    public bool IsHealing => ticksLeft > 0;

    /// <summary>Seconds until the next charge returns (0 when full).</summary>
    public float CooldownRemaining =>
        IsFull || float.IsPositiveInfinity(nextChargeAt) ? 0f : Mathf.Max(0f, nextChargeAt - Time.time);

    /// <summary>0 right after a drink, 1 when ready (HUD sweep).</summary>
    public float ReadyFraction => IsFull ? 1f : 1f - Mathf.Clamp01(CooldownRemaining / Mathf.Max(1f, Cooldown));

    /// <summary>Full HP restored by one drink for this hero (belt affix and uniques included).</summary>
    public static float HealAmount(PlayerHealth health, Equipment gear)
    {
        float max = health != null ? health.MaxHP : Equipment.BaseMaxHP;
        float bonus = gear != null ? gear.GearTotal(AffixLibrary.FlaskHealing) : 0f;
        return max * HealFraction * (1f + Mathf.Max(0f, bonus) / 100f);
    }

    DiabloNetworkPlayer Net
    {
        get
        {
            var net = GetComponent<DiabloNetworkPlayer>();
            return net != null && net.IsSpawned ? net : null;
        }
    }

    bool IsAuthority
    {
        get
        {
            var net = Net;
            return net == null || net.IsServer;
        }
    }

    bool IsLocalHero
    {
        get
        {
            var net = Net;
            if (net != null) return net.IsOwner;
            return gameObject.name == DiabloNetworkPlayer.HeroName;
        }
    }

    /// <summary>
    /// Key-1 entry for the local hero. Solo and host drink at once; a client
    /// sends the request and returns true while it waits for the host.
    /// </summary>
    public bool TryDrink()
    {
        var net = Net;
        if (net != null && !net.IsServer)
        {
            if (Charges <= 0) return false;
            net.RequestFlaskDrink();
            return true;
        }
        return DrinkAuthoritative();
    }

    /// <summary>Host/solo rule: spend a charge, start the cooldown, start the heal.</summary>
    public bool DrinkAuthoritative()
    {
        if (!IsAuthority) return false;
        var health = GetComponent<PlayerHealth>();
        if (health == null || health.CurrentHP <= 0f || Charges <= 0) return false;
        charges = Charges - 1;
        Granted++;
        if (float.IsPositiveInfinity(nextChargeAt)) nextChargeAt = Time.time + Cooldown;
        float amount = HealAmount(health, Gear);
        GearUniqueEffects.OnFlaskDrunk(gameObject, amount);
        var net = Net;
        if (net != null && !net.IsOwner) net.ConfirmFlaskToOwner(amount, Charges, CooldownRemaining);
        else BeginHeal(amount);
        return true;
    }

    /// <summary>Owner side of a confirmed drink: start the ticks and owner-only effects.</summary>
    public void BeginHeal(float amount)
    {
        if (amount <= 0f) return;
        ticksLeft = Ticks;
        tickAmount = amount / Ticks;
        nextTickAt = Time.time + TickSeconds;
        Drinks++;
        GearUniqueEffects.OnFlaskHealBegins(gameObject);
        DiabloAudio.PlayHeal();
        Debug.Log($"[Flask] Drank: +{amount:0.#} HP over {HealSeconds:0} s ({Charges}/{MaxCharges} left).");
    }

    /// <summary>Client mirror of the host's flask state (charges + cooldown).</summary>
    public void ApplyState(int newCharges, float cooldownRemaining)
    {
        charges = Mathf.Clamp(newCharges, 0, MaxCharges);
        nextChargeAt = charges >= MaxCharges ? float.PositiveInfinity : Time.time + Mathf.Max(0f, cooldownRemaining);
    }

    /// <summary>Fill every charge and clear the cooldown (town, shrine). True when it changed.</summary>
    public bool Refill()
    {
        if (IsFull) return false;
        charges = MaxCharges;
        nextChargeAt = float.PositiveInfinity;
        var net = Net;
        if (net != null && net.IsServer && !net.IsOwner) net.SendFlaskState(Charges, 0f);
        Debug.Log("[Flask] Refilled.");
        return true;
    }

    /// <summary>True inside the Lantern Hold or in reach of a Risk Shrine.</summary>
    public static bool AtRefillPoint(Vector3 pos)
    {
        if (SkillVariantCatalog.IsInLanternHold(pos)) return true;
        foreach (var shrine in RiskShrine.Live)
            if (shrine != null && RiskShrine.CanInteract(pos, shrine.transform.position)) return true;
        return false;
    }

    /// <summary>Advance cooldown, heal ticks and refill checks (tests drive it with a clock).</summary>
    public void Tick(float now)
    {
        if (charges >= 0 && charges < MaxCharges && now >= nextChargeAt)
        {
            charges++;
            nextChargeAt = charges < MaxCharges ? now + Cooldown : float.PositiveInfinity;
        }
        if (charges > MaxCharges) charges = MaxCharges;
        if (ticksLeft > 0 && now >= nextTickAt)
        {
            var health = GetComponent<PlayerHealth>();
            ticksLeft--;
            nextTickAt += TickSeconds;
            if (health != null && health.CurrentHP > 0f) HealedTotal += health.Heal(tickAmount);
            else ticksLeft = 0;
        }
        if (IsAuthority && now >= nextRefillCheck)
        {
            nextRefillCheck = now + RefillCheckSeconds;
            if (!IsFull && AtRefillPoint(transform.position)) Refill();
        }
    }

    void Update()
    {
        Tick(Time.time);
        if (!IsLocalHero) return;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null || !kb.digit1Key.wasPressedThisFrame) return;
        if (DiabloUIFactory.IsMenuCapturingKeys(gameObject)) return;
        TryDrink();
    }
}
