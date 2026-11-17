using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Ranged hero skills: Fireball (Q, blast damage) and Bow (E, fast single
/// target arrow). Right-click repeats the last used skill. Both fire toward
/// the cursor aim of <see cref="DiabloClickController"/>. Damage runs locally
/// solo/host and through a range-validated ServerRpc as a netcode client.
/// </summary>
public sealed class DiabloSkills : MonoBehaviour
{
    public enum Skill { Fireball, Arrow }

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

    /// <summary>Launch height above the hero pivot (chest).</summary>
    public const float LaunchHeight = 1.0f;
    /// <summary>Launch offset in front of the hero (clears its own collider).</summary>
    public const float LaunchForward = 0.8f;

    public Skill lastUsed = Skill.Fireball;

    float lastFireball = float.NegativeInfinity;
    float lastArrow = float.NegativeInfinity;
    DiabloClickController controller;
    DiabloNetworkPlayer netPlayer;
    Text hud;

    void Awake()
    {
        controller = GetComponent<DiabloClickController>();
        netPlayer = GetComponent<DiabloNetworkPlayer>();
    }

    bool IsRemoteMirror => netPlayer != null && netPlayer.IsSpawned && !netPlayer.IsOwner;

    /// <summary>Seconds until the skill is ready again (0 when ready).</summary>
    public float CooldownLeft(Skill s)
    {
        float last = s == Skill.Fireball ? lastFireball : lastArrow;
        float cd = s == Skill.Fireball ? fireballCooldown : arrowCooldown;
        return Mathf.Max(0f, cd - (Time.time - last));
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
        if (s == Skill.Fireball) lastFireball = Time.time; else lastArrow = Time.time;
        lastUsed = s;

        transform.rotation = Quaternion.LookRotation(dir);
        var origin = transform.position + Vector3.up * LaunchHeight + dir * LaunchForward;
        if (s == Skill.Fireball)
            DiabloProjectile.Spawn(s, origin, dir, transform, fireballDamage,
                fireballSpeed, fireballRange, fireballBlastRadius);
        else
            DiabloProjectile.Spawn(s, origin, dir, transform, arrowDamage + WeaponBonus(),
                arrowSpeed, arrowRange, 0f);

        var driver = GetComponent<DiabloAnimator>();
        if (driver != null) driver.OnStrike();
        DiabloAudio.PlaySwing();
        return true;
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
    public static void ApplyHit(GameObject target, int damage, Vector3 from, float knockback)
    {
        if (target == null) return;
        var health = target.GetComponent<EnemyHealth>();
        if (health == null) return;
        var nm = NetworkManager.Singleton;
        var netObj = target.GetComponent<NetworkObject>();
        if (nm != null && nm.IsListening && !nm.IsServer && netObj != null && netObj.IsSpawned)
        {
            var player = nm.SpawnManager.GetLocalPlayerObject();
            var relay = player != null ? player.GetComponent<DiabloNetworkPlayer>() : null;
            if (relay != null) relay.SubmitRangedHitServerRpc(netObj.NetworkObjectId, damage);
            return;
        }
        health.TakeDamage(damage);
        if (health != null)
            health.ApplyKnockback(from, health.tier == LootTier.Normal ? knockback : knockback * 0.3f);
        DiabloAudio.PlayHit();
        DiabloMelee.LastStruck = target;
        DiabloMelee.LastStruckTime = Time.time;
    }

    void Update()
    {
        if (IsRemoteMirror) return;
        if (controller == null || !controller.isActiveAndEnabled) return;
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null && mouse == null) return; // headless/tests: API only
        if (DiabloClickController.ShouldBlockClicks(GetComponent<DiabloInventoryUI>()))
            return;

        if (kb != null && kb.qKey.wasPressedThisFrame) Cast(Skill.Fireball);
        else if (kb != null && kb.eKey.wasPressedThisFrame) Cast(Skill.Arrow);
        else if (mouse != null && mouse.rightButton.wasPressedThisFrame) Cast(lastUsed);

        UpdateHud();
    }

    void UpdateHud()
    {
        if (hud == null) BuildHud();
        hud.text = $"{Slot("Q", "Fireball", Skill.Fireball)}     {Slot("E", "Bow", Skill.Arrow)}     RMB: {(lastUsed == Skill.Fireball ? "Fireball" : "Bow")}";
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
        canvasGo.AddComponent<CanvasScaler>();
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
