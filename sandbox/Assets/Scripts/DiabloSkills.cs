using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Hero skills: Fireball (Q, blast damage), Bow (E, fast single target
/// arrow) and Cleave (R, 360° melee arc, S4). Right-click repeats the last
/// used skill. Ranged shots fire toward the cursor aim of
/// <see cref="DiabloClickController"/>; Cleave hits every husk in radius.
/// Damage runs locally solo/host and through a range-validated ServerRpc as
/// a netcode client. Skill ranks (+20 % damage/rank) and focus live on the
/// <see cref="CharacterSheet"/> and scale every cast.
/// </summary>
public sealed class DiabloSkills : MonoBehaviour
{
    /// <summary>
    /// Explicit values: co-op sends skills as int (DiabloNetworkPlayer), so
    /// existing values never move; Lanternbearer skills append (second-class S1).
    /// </summary>
    public enum Skill { Fireball = 0, Arrow = 1, Cleave = 2, LanternBolt = 3, Wisps = 4, SearingHalo = 5, VeilStep = 6 }

    [Header("Fireball (Q)")]
    public int fireballDamage = 3;
    public float fireballBlastRadius = 2.5f;
    public float fireballSpeed = 11f;
    public float fireballRange = 16f;
    public float fireballCooldown = 1.0f;

    [Header("Bow (E)")]
    public int arrowDamage = 2;
    public float arrowSpeed = 24f;
    public float arrowRange = 20f;
    public float arrowCooldown = 0.35f;

    [Header("Cleave (R)")]
    public int cleaveDamage = 4;
    public float cleaveBaseRadius = 2.6f;
    public float cleaveRadiusPerRank = 0.4f;
    public float cleaveCooldown = 6f;
    public float cleaveKnockback = 1.2f;

    /// <summary>Launch height above the hero pivot (chest).</summary>
    public const float LaunchHeight = 1.0f;
    /// <summary>Launch offset in front of the hero (clears its own collider).</summary>
    public const float LaunchForward = 0.8f;

    public Skill lastUsed = Skill.Fireball;

    float lastFireball = float.NegativeInfinity;
    float lastArrow = float.NegativeInfinity;
    float lastCleave = float.NegativeInfinity;
    DiabloClickController controller;
    DiabloNetworkPlayer netPlayer;
    Text hud;

    void Awake()
    {
        controller = GetComponent<DiabloClickController>();
        netPlayer = GetComponent<DiabloNetworkPlayer>();
    }

    bool IsRemoteMirror => netPlayer != null && netPlayer.IsSpawned && !netPlayer.IsOwner;

    // ---------- held weapon swap (bow while shooting, sword otherwise) ----------

    /// <summary>Seconds the bow stays drawn after the last arrow.</summary>
    public const float BowStanceSeconds = 2.0f;
    /// <summary>Scene/prefab name of the builder-attached melee weapon.</summary>
    public const string SwordName = "StarterSword";

    float bowUntil = float.NegativeInfinity;
    GameObject heldBow;
    Renderer[] swordRenderers;

    /// <summary>True while the hero holds the bow instead of the sword.</summary>
    public bool BowDrawn => heldBow != null && heldBow.activeSelf;

    /// <summary>Draw the bow (hide the sword) for BowStanceSeconds.</summary>
    public void EnterBowStance()
    {
        bowUntil = Time.time + BowStanceSeconds;
        if (heldBow == null) heldBow = CreateHeldBow();
        if (heldBow != null) heldBow.SetActive(true);
        SetSwordVisible(false);
    }

    /// <summary>Put the bow away and show the sword again (melee strike or timeout).</summary>
    public void ExitBowStance()
    {
        bowUntil = float.NegativeInfinity;
        if (heldBow != null) heldBow.SetActive(false);
        SetSwordVisible(true);
    }

    GameObject CreateHeldBow()
    {
        var prefab = Resources.Load<GameObject>("SkillArt/HeroBow");
        if (prefab == null) return null;
        // Bows sit in the off hand; KayKit rigs expose a "handslot.l" mount.
        var hand = FindDeep(transform, "handslot.l") ?? FindDeep(transform, "hand.l");
        var bow = Instantiate(prefab);
        bow.name = "HeldBow";
        if (hand != null)
        {
            bow.transform.SetParent(hand, false);
            bow.transform.localPosition = Vector3.zero;
            bow.transform.localRotation = HandMountRotation(bow);
        }
        else
        {
            bow.transform.SetParent(transform, false);
            bow.transform.localPosition = new Vector3(-0.35f, 1.0f, 0.25f);
            bow.transform.localRotation = Quaternion.identity;
        }
        return bow;
    }

    /// <summary>
    /// KayKit's bow on handslot.l faces its string toward the target and its
    /// arc toward the archer (seen in the shotcheck). Turn it 180° about its
    /// own long axis so the arc leads and the string sits on the archer's side.
    /// </summary>
    public static Quaternion HandMountRotation(GameObject bow) =>
        Quaternion.AngleAxis(180f, LongAxis(bow));

    /// <summary>Longest mesh-bounds axis in the bow root's local space (limb to limb).</summary>
    public static Vector3 LongAxis(GameObject bow)
    {
        var min = Vector3.positiveInfinity;
        var max = Vector3.negativeInfinity;
        foreach (var r in bow.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = null;
            if (r is SkinnedMeshRenderer smr) mesh = smr.sharedMesh;
            else
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null) mesh = mf.sharedMesh;
            }
            if (mesh == null) continue;
            var mb = mesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = mb.center + Vector3.Scale(mb.extents, new Vector3(
                    (i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                var local = bow.transform.InverseTransformPoint(r.transform.TransformPoint(corner));
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
        }
        if (float.IsInfinity(min.x)) return Vector3.up;
        var size = max - min;
        if (size.x >= size.y && size.x >= size.z) return Vector3.right;
        return size.y >= size.z ? Vector3.up : Vector3.forward;
    }

    void SetSwordVisible(bool visible)
    {
        if (swordRenderers == null)
        {
            var sword = FindDeep(transform, SwordName);
            swordRenderers = sword != null ? sword.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        }
        foreach (var r in swordRenderers)
            if (r != null) r.enabled = visible;
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var hit = FindDeep(t.GetChild(i), name);
            if (hit != null) return hit;
        }
        return null;
    }

    /// <summary>Base cooldown of a skill (before focus reduction).</summary>
    public float BaseCooldown(Skill s)
    {
        switch (s)
        {
            case Skill.Fireball: return fireballCooldown;
            case Skill.Arrow: return arrowCooldown;
            default: return cleaveCooldown;
        }
    }

    /// <summary>Effective cooldown: base × focus factor (S4, floored at half).</summary>
    public float EffectiveCooldown(Skill s)
    {
        var sheet = CharacterSheet.For(gameObject);
        float factor = sheet != null ? sheet.CooldownMultiplier() : 1f;
        var eq = GetComponent<Equipment>();
        if (eq != null) factor *= eq.CooldownFactor();
        return BaseCooldown(s) * factor;
    }

    /// <summary>Seconds until the skill is ready again (0 when ready).</summary>
    public float CooldownLeft(Skill s)
    {
        float last = s == Skill.Fireball ? lastFireball : (s == Skill.Arrow ? lastArrow : lastCleave);
        return Mathf.Max(0f, EffectiveCooldown(s) - (Time.time - last));
    }

    /// <summary>Current cleave radius: base + per-rank bonus (S4).</summary>
    public float CleaveRadius
    {
        get
        {
            var sheet = CharacterSheet.For(gameObject);
            return sheet != null
                ? sheet.CleaveRadius(cleaveBaseRadius, cleaveRadiusPerRank)
                : cleaveBaseRadius;
        }
    }

    /// <summary>
    /// Damage of one cast: base (+ weapon bonus for Bow/Cleave) × rank × focus.
    /// Pure entry point for tests (uses the live sheet, no cooldown gate).
    /// </summary>
    public int EffectiveDamage(Skill s)
    {
        int baseDmg = s == Skill.Fireball ? fireballDamage
            : s == Skill.Arrow ? arrowDamage + WeaponBonus()
            : cleaveDamage + WeaponBonus();
        var sheet = CharacterSheet.For(gameObject);
        var eq = GetComponent<Equipment>();
        int gearRanks = eq != null ? eq.SkillRankBonus() : 0;
        float mul = sheet != null
            ? sheet.SkillMultiplier(s, gearRanks)
            : 1f + CharacterSheet.RankDamageBonus * gearRanks;
        return Mathf.Max(1, Mathf.RoundToInt(baseDmg * mul));
    }

    /// <summary>Cast toward the current aim. False while on cooldown.</summary>
    public bool Cast(Skill s)
    {
        if (controller == null) controller = GetComponent<DiabloClickController>();
        var dir = controller != null ? controller.AimDirection : transform.forward;
        return CastToward(s, dir);
    }

    /// <summary>Cast in a world direction (tests + external drivers).</summary>
    public bool CastToward(Skill s, Vector3 dir)
    {
        if (CooldownLeft(s) > 0f) return false;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();
        if (s == Skill.Fireball) lastFireball = Time.time;
        else if (s == Skill.Arrow) lastArrow = Time.time;
        else lastCleave = Time.time;
        lastUsed = s;

        if (s == Skill.Cleave)
        {
            DoCleave(dir, cosmetic: false);
            var origin = transform.position + Vector3.up * LaunchHeight + dir * LaunchForward;
            // Co-op: mirrors render the arc visual (no damage there).
            if (netPlayer == null) netPlayer = GetComponent<DiabloNetworkPlayer>();
            var relay = NetworkManager.Singleton;
            if (netPlayer != null && netPlayer.IsSpawned && relay != null && relay.IsListening)
                netPlayer.BroadcastCast((int)s, origin, dir);
            return true;
        }

        var origin2 = transform.position + Vector3.up * LaunchHeight + dir * LaunchForward;
        Launch(s, origin2, dir, cosmetic: false);

        // Co-op: every other client renders a visual-only copy of this cast.
        if (netPlayer == null) netPlayer = GetComponent<DiabloNetworkPlayer>();
        var nm = NetworkManager.Singleton;
        if (netPlayer != null && netPlayer.IsSpawned && nm != null && nm.IsListening)
            netPlayer.BroadcastCast((int)s, origin2, dir);
        return true;
    }

    /// <summary>
    /// Cleave (R, S4): instant 360° melee arc — every husk inside
    /// <see cref="CleaveRadius"/> takes the ranked blow with knockback.
    /// Cosmetic mirrors only spawn the arc visual, never damage.
    /// </summary>
    public void DoCleave(Vector3 dir, bool cosmetic)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();
        transform.rotation = Quaternion.LookRotation(dir);
        float radius = CleaveRadius;
        if (!cosmetic)
        {
            ExitBowStance();
            var driver = GetComponent<DiabloAnimator>();
            if (driver != null) driver.OnStrike();
            int blow = EffectiveDamage(Skill.Cleave);
            foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
            {
                if (husk == null) continue;
                var to = husk.transform.position - transform.position;
                to.y = 0f;
                if (to.magnitude > radius) continue;
                ApplyHit(husk, blow, transform.position, cleaveKnockback, transform);
            }
            DiabloAudio.PlayWhoosh();
        }
        SpawnCleaveVisual(radius);
    }

    void SpawnCleaveVisual(float radius)
    {
        var prefab = Resources.Load<GameObject>("SkillArt/CleaveArc");
        if (prefab == null) return;
        var arc = Instantiate(prefab, transform.position, Quaternion.identity);
        arc.transform.localScale = Vector3.one * (radius / cleaveBaseRadius);
        Destroy(arc, 0.6f);
    }

    /// <summary>
    /// Render another player's cast on this client: same flight and impact
    /// art, no damage (the caster's own client applies it). No cooldown gate.
    /// </summary>
    public DiabloProjectile ShowRemoteCast(Skill s, Vector3 origin, Vector3 dir)
    {
        if (s == Skill.Cleave)
        {
            SpawnCleaveVisual(CleaveRadius);
            return null;
        }
        return Launch(s, origin, dir, cosmetic: true);
    }

    DiabloProjectile Launch(Skill s, Vector3 origin, Vector3 dir, bool cosmetic)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();
        transform.rotation = Quaternion.LookRotation(dir);
        var p = s == Skill.Fireball
            ? DiabloProjectile.Spawn(s, origin, dir, transform, EffectiveDamage(s),
                fireballSpeed, fireballRange, fireballBlastRadius, cosmetic)
            : DiabloProjectile.Spawn(s, origin, dir, transform, EffectiveDamage(s),
                arrowSpeed, arrowRange, 0f, cosmetic);
        var driver = GetComponent<DiabloAnimator>();
        if (s == Skill.Arrow)
        {
            EnterBowStance();
            if (driver != null) driver.OnBowShot();
        }
        else if (driver != null) driver.OnStrike();
        if (s == Skill.Arrow) DiabloAudio.PlayTwang();
        else DiabloAudio.PlayWhoosh();
        return p;
    }

    /// <summary>Weapon Damage affix scales arrows like melee swings.</summary>
    int WeaponBonus()
    {
        var eq = GetComponent<Equipment>();
        return eq != null ? Mathf.Max(0, Mathf.RoundToInt(eq.GetDamage() - Equipment.BaseDamage)) : 0;
    }

    /// <summary>
    /// Apply one projectile hit: direct damage solo/host, ServerRpc relay as a
    /// netcode client (the server re-validates range).
    /// </summary>
    public static void ApplyHit(GameObject target, int damage, Vector3 from, float knockback,
        Transform attacker = null)
    {
        if (target == null) return;
        var health = target.GetComponent<EnemyHealth>();
        if (health == null) return;
        damage = DiabloJuice.RollCrit(attacker, damage, out bool crit);
        var nm = NetworkManager.Singleton;
        var netObj = target.GetComponent<NetworkObject>();
        if (nm != null && nm.IsListening && !nm.IsServer && netObj != null && netObj.IsSpawned)
        {
            var player = nm.SpawnManager.GetLocalPlayerObject();
            var relay = player != null ? player.GetComponent<DiabloNetworkPlayer>() : null;
            if (relay != null) relay.SubmitRangedHitServerRpc(netObj.NetworkObjectId, damage);
            DiabloJuice.OnEnemyHit(target.transform.position, damage, crit);
            return;
        }
        health.TakeDamage(damage, crit);
        Equipment.ApplyLifeSteal(attacker, damage);
        if (health != null)
            health.ApplyKnockback(from, health.tier == LootTier.Normal ? knockback : knockback * 0.3f);
        DiabloAudio.PlayHit();
        DiabloMelee.LastStruck = target;
        DiabloMelee.LastStruckTime = Time.time;
    }

    void Update()
    {
        // Runs on mirrors too: remote casts draw the bow there as well.
        if (BowDrawn && Time.time > bowUntil) ExitBowStance();
        if (IsRemoteMirror) return;
        if (controller == null || !controller.isActiveAndEnabled) return;
        if (DiabloClickController.ExternalDrive) { UpdateHud(); return; }
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null && mouse == null) return; // headless/tests: API only
        if (DiabloClickController.ShouldBlockClicks(GetComponent<DiabloInventoryUI>()))
            return;
        var charUI = GetComponent<DiabloCharacterUI>();
        if (charUI != null && charUI.IsOpen) return;

        if (kb != null && kb.qKey.wasPressedThisFrame) Cast(Skill.Fireball);
        else if (kb != null && kb.eKey.wasPressedThisFrame) Cast(Skill.Arrow);
        else if (kb != null && kb.rKey.wasPressedThisFrame) Cast(Skill.Cleave);
        else if (mouse != null && mouse.rightButton.wasPressedThisFrame) Cast(lastUsed);

        UpdateHud();
    }

    void UpdateHud()
    {
        if (hud == null) BuildHud();
        string lastName = lastUsed == Skill.Fireball ? "Fireball" : lastUsed == Skill.Arrow ? "Bow" : "Cleave";
        hud.text = $"{Slot("Q", "Fireball", Skill.Fireball)}     {Slot("E", "Bow", Skill.Arrow)}     {Slot("R", "Cleave", Skill.Cleave)}     RMB: {lastName}";
    }

    string Slot(string key, string name, Skill s)
    {
        float left = CooldownLeft(s);
        return left > 0f ? $"[{key}] {name} {left:0.0}s" : $"[{key}] {name}";
    }

    void BuildHud()
    {
        var canvasGo = new GameObject("SkillBarCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        var go = new GameObject("SkillBar");
        go.transform.SetParent(canvasGo.transform, false);
        hud = go.AddComponent<Text>();
        hud.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hud.fontSize = 20;
        hud.alignment = TextAnchor.MiddleCenter;
        hud.color = new Color(1f, 0.85f, 0.55f, 1f);
        hud.raycastTarget = false;
        go.AddComponent<Shadow>();
        var r = hud.rectTransform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0f);
        r.sizeDelta = new Vector2(700f, 36f);
        r.anchoredPosition = new Vector2(0f, 24f);
    }
}
