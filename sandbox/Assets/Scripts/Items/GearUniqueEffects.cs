using UnityEngine;

/// <summary>
/// Effects of the 18 gear-slots uniques (S4 role pieces for gloves/boots/belt,
/// S5 zone pieces for weapon/armor/helmet). Every check reads the live
/// <see cref="Equipment"/>, so taking a piece off ends its effect at once and
/// a reload keeps it only while the piece stays equipped. Timed state lives on
/// the wearer's <see cref="GearEffectState"/> and is never saved.
///
/// Authority follows the existing split: hits, crits and kills resolve where
/// the combat runs (solo or host, on the attacker's mirror for client
/// intents); damage taken, shields and HP ticks resolve on the owner, which
/// writes its own HP. <see cref="ApplyToOwner"/> forwards owner-side results
/// from the host.
/// </summary>
public static class GearUniqueEffects
{
    // ---- tuning ------------------------------------------------------------
    public const float GripShieldAmount = 4f;
    public const float GripShieldSeconds = 3f;
    public const float GripShieldInterval = 3f;
    public const float MendersHealMul = 1.2f;
    public const float CutpurseCritHeal = 2f;
    public const float BastionStillSeconds = 1f;
    public const float BastionDamageMul = 0.8f;
    public const float PilgrimAllyRadius = 6f;
    public const float PilgrimAllyShare = 0.5f;
    public const float GhostwalkHasteFactor = 1.25f;
    public const float GhostwalkHasteSeconds = 2f;
    public const float WardenShieldAmount = 15f;
    public const float WardenShieldSeconds = 5f;
    public const int VialSashCharges = 2;
    public const float QuickdrawCooldown = 40f;
    public const float CinderfangBurstDamage = 3f;
    public const float CinderfangBurstRadius = 2.5f;
    public const float ThuribleKillHeal = 3f;
    public const float GlassthornCritMultiplier = 2.5f;
    public const float FenWrapQuietSeconds = 4f;
    public const float FenWrapRegenPerSecond = 1f;
    public const float HauberkThreshold = 5f;
    public const float HauberkReduction = 2f;
    public const float KilnheartThornsMul = 2f;
    public const float ProspectorShare = 0.5f;
    public const float BonewatchRadius = 3f;
    public const float BonewatchDamageMul = 0.85f;
    public const float EmberwreathCdrCap = 50f;

    public static bool Wears(Equipment eq, string uniqueId) => eq != null && eq.Wears(uniqueId);
    static bool Wears(GameObject hero, string uniqueId) =>
        hero != null && Wears(hero.GetComponent<Equipment>(), uniqueId);

    /// <summary>Player-facing effect line (empty for items without a gear-slots effect).</summary>
    public static string EffectText(string uniqueId)
    {
        if (string.IsNullOrEmpty(uniqueId)) return "";
        if (System.Array.IndexOf(ItemDatabase.GearSlotUniqueIds, uniqueId) < 0) return "";
        return Loc.T("unique.effect." + uniqueId.Replace("-", ""));
    }

    // ---- flask (S2 hooks) --------------------------------------------------

    public static int FlaskCharges(Equipment eq) => Wears(eq, ItemDatabase.VialSashId) ? VialSashCharges : 1;

    public static float FlaskCooldown(Equipment eq) =>
        Wears(eq, ItemDatabase.QuickdrawBeltId) ? QuickdrawCooldown : HealingFlask.CooldownSeconds;

    /// <summary>Authority side of a drink: pilgrim-steps mends nearby allies at once.</summary>
    public static void OnFlaskDrunk(GameObject hero, float amount)
    {
        if (!Wears(hero, ItemDatabase.PilgrimStepsId)) return;
        foreach (var hp in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            if (hp == null || hp.gameObject == hero || hp.CurrentHP <= 0f) continue;
            var to = hp.transform.position - hero.transform.position;
            to.y = 0f;
            if (to.magnitude > PilgrimAllyRadius) continue;
            ApplyToOwner(hp.gameObject, GearEffect.Heal, amount * PilgrimAllyShare);
        }
    }

    /// <summary>Owner side of a drink: the warden's girdle grants its absorb shield.</summary>
    public static void OnFlaskHealBegins(GameObject hero)
    {
        if (!Wears(hero, ItemDatabase.WardensGirdleId)) return;
        hero.GetComponent<PlayerHealth>()?.GrantShield(WardenShieldAmount, WardenShieldSeconds);
    }

    // ---- outgoing hits (crit path, DiabloJuice.RollCrit) --------------------

    public static float CritMultiplier(Equipment eq, float baseMultiplier) =>
        Wears(eq, ItemDatabase.GlassthornBowId) ? GlassthornCritMultiplier : baseMultiplier;

    /// <summary>Called once per landed crit by the attacker's combat authority.</summary>
    public static void OnCrit(Transform attacker)
    {
        if (attacker == null || !Wears(attacker.gameObject, ItemDatabase.CutpurseWrapsId)) return;
        ApplyToOwner(attacker.gameObject, GearEffect.Heal, CutpurseCritHeal);
    }

    /// <summary>Heal factor for Mending Light and Renewing Circle.</summary>
    public static float HealMultiplier(Equipment eq) => Wears(eq, ItemDatabase.MendersTouchId) ? MendersHealMul : 1f;

    // ---- kills (EnemyHealth.Die, striker from the same frame) ----------------

    public static void OnKill(Transform killer, Vector3 at)
    {
        if (killer == null) return;
        var hero = killer.gameObject;
        if (Wears(hero, ItemDatabase.OssuaryThuribleId)) ApplyToOwner(hero, GearEffect.Heal, ThuribleKillHeal);
        if (Wears(hero, ItemDatabase.GhostwalkBootsId)) ApplyToOwner(hero, GearEffect.Haste, GhostwalkHasteSeconds);
        if (Wears(hero, ItemDatabase.CinderfangCleaverId)) CinderBurst(at);
    }

    /// <summary>Cinderfang: the slain enemy bursts, burning every other husk close by.</summary>
    public static int CinderBurst(Vector3 at)
    {
        int hits = 0;
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk == null) continue;
            var enemy = husk.GetComponent<EnemyHealth>();
            if (enemy == null || enemy.IsDead) continue;
            var to = husk.transform.position - at;
            to.y = 0f;
            if (to.magnitude > CinderfangBurstRadius) continue;
            enemy.TakeDamage(Mathf.RoundToInt(CinderfangBurstDamage));
            hits++;
        }
        if (hits > 0) PresentationVfx.CleaveArc(at, CinderfangBurstRadius);
        return hits;
    }

    // ---- incoming damage (owner, PlayerHealth) --------------------------------

    /// <summary>
    /// Shape a hit on the wearer before shields: bastion-treads (standing
    /// still), bonewatch-visor (attacker close), ashen-hauberk (heavy hits).
    /// <paramref name="fromPos"/> is null for undirected damage.
    /// </summary>
    public static float ShapeIncoming(PlayerHealth health, float amount, Vector3? fromPos)
    {
        if (health == null || amount <= 0f) return amount;
        var eq = health.GetComponent<Equipment>();
        var state = GearEffectState.For(health.gameObject);
        if (Wears(eq, ItemDatabase.BastionTreadsId) && state.StillFor >= BastionStillSeconds)
            amount *= BastionDamageMul;
        if (fromPos.HasValue && Wears(eq, ItemDatabase.BonewatchVisorId))
        {
            var to = fromPos.Value - health.transform.position;
            to.y = 0f;
            if (to.magnitude <= BonewatchRadius) amount *= BonewatchDamageMul;
        }
        if (Wears(eq, ItemDatabase.AshenHauberkId) && amount > HauberkThreshold)
            amount = Mathf.Max(HauberkThreshold, amount - HauberkReduction);
        return amount;
    }

    /// <summary>After a hit landed: start the fen-wrap quiet timer, grant the grip shield.</summary>
    public static void OnDamaged(PlayerHealth health)
    {
        if (health == null) return;
        var state = GearEffectState.For(health.gameObject);
        state.LastDamageTime = Time.time;
        if (Wears(health.GetComponent<Equipment>(), ItemDatabase.IronboundGripsId)
            && Time.time >= state.NextGripShield)
        {
            state.NextGripShield = Time.time + GripShieldInterval;
            health.GrantShield(GripShieldAmount, GripShieldSeconds);
        }
    }

    // ---- passive stats (Equipment getters) ------------------------------------

    public static float ThornsMultiplier(Equipment eq) => Wears(eq, ItemDatabase.KilnheartMailId) ? KilnheartThornsMul : 1f;

    public static float ExtraMagicFind(Equipment eq, float goldFind) =>
        Wears(eq, ItemDatabase.ProspectorsHoodId) ? goldFind * ProspectorShare : 0f;

    public static float CooldownCap(Equipment eq, float baseCap) =>
        Wears(eq, ItemDatabase.EmberwreathCrownId) ? EmberwreathCdrCap : baseCap;

    // ---- owner forwarding ------------------------------------------------------

    /// <summary>
    /// Apply an owner-side result. Solo and the owner apply locally; the host
    /// forwards to a remote owner. A client never forwards for others.
    /// </summary>
    public static void ApplyToOwner(GameObject hero, GearEffect effect, float value)
    {
        if (hero == null || value <= 0f) return;
        var net = hero.GetComponent<DiabloNetworkPlayer>();
        if (net != null && net.IsSpawned && !net.IsOwner)
        {
            if (net.IsServer) net.SendGearEffect(effect, value);
            return;
        }
        ApplyLocal(hero, effect, value);
    }

    public static void ApplyLocal(GameObject hero, GearEffect effect, float value)
    {
        if (hero == null) return;
        switch (effect)
        {
            case GearEffect.Heal:
                hero.GetComponent<PlayerHealth>()?.Heal(value);
                break;
            case GearEffect.Haste:
                GearEffectState.For(hero).GrantHaste(value);
                break;
        }
    }
}

/// <summary>Owner-side results the host forwards (gear-slots S4/S5).</summary>
public enum GearEffect { Heal = 0, Haste = 1 }

/// <summary>
/// Per-wearer runtime state of the gear-slots uniques: stillness and quiet
/// timers, the grip shield interval, the fen-wrap regeneration and the
/// ghostwalk haste. Dies with the hero, so a reload starts clean.
/// </summary>
public sealed class GearEffectState : MonoBehaviour
{
    public float LastDamageTime = float.NegativeInfinity;
    public float NextGripShield = float.NegativeInfinity;
    Vector3 lastPos;
    float stillSince;
    float hasteUntil = float.NegativeInfinity;
    float baseWalk;
    bool hasted;

    public static GearEffectState For(GameObject hero)
    {
        var state = hero.GetComponent<GearEffectState>();
        if (state != null) return state;
        state = hero.AddComponent<GearEffectState>();
        state.lastPos = hero.transform.position;
        state.stillSince = Time.time;
        return state;
    }

    /// <summary>Seconds the hero has stood still (bastion-treads).</summary>
    public float StillFor
    {
        get
        {
            Track();
            return Time.time - stillSince;
        }
    }

    public bool Hasted => hasted;

    void Track()
    {
        var pos = transform.position;
        var d = pos - lastPos;
        d.y = 0f;
        if (d.sqrMagnitude > 0.0004f) stillSince = Time.time;
        lastPos = pos;
    }

    public void GrantHaste(float seconds)
    {
        var controller = GetComponent<DiabloClickController>();
        if (controller == null) return;
        if (!hasted)
        {
            baseWalk = controller.walkSpeed;
            controller.walkSpeed = baseWalk * GearUniqueEffects.GhostwalkHasteFactor;
            hasted = true;
        }
        hasteUntil = Mathf.Max(hasteUntil, Time.time + seconds);
    }

    void EndHaste()
    {
        if (!hasted) return;
        var controller = GetComponent<DiabloClickController>();
        if (controller != null) controller.walkSpeed = baseWalk;
        hasted = false;
    }

    void Update()
    {
        Track();
        var eq = GetComponent<Equipment>();
        if (hasted && (Time.time >= hasteUntil || !GearUniqueEffects.Wears(eq, ItemDatabase.GhostwalkBootsId)))
            EndHaste();
        if (!GearUniqueEffects.Wears(eq, ItemDatabase.FenWrapId)) return;
        var health = GetComponent<PlayerHealth>();
        if (health == null || health.IsMirror || health.CurrentHP <= 0f) return;
        if (Time.time - LastDamageTime < GearUniqueEffects.FenWrapQuietSeconds) return;
        health.Heal(GearUniqueEffects.FenWrapRegenPerSecond * Time.deltaTime);
    }

    void OnDestroy() => EndHaste();
}
