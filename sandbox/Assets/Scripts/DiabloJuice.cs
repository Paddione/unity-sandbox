using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Combat feedback layer: one place every hit, crit, kill and hurt reports
/// to. Owns screenshake (trauma model, honours <see cref="DiabloSettings.ScreenShake"/>),
/// floating damage numbers (pooled uGUI texts) and impact particles (one
/// pooled ParticleSystem per look, fed by <c>Emit</c>). All motion runs on
/// unscaled time so it keeps moving through <see cref="HitStop"/>.
/// Static counters are test seams, like <see cref="DiabloAudio"/>.
/// </summary>
public static class DiabloJuice
{
    // Tunables (one place, iterate by looking).
    public const float CritMultiplier = 2f;
    public const float MaxShakeOffset = 0.35f;
    public const float TraumaDecayPerSecond = 1.6f;
    public const float ShakeHearingRange = 20f;
    public const float NumberLifetime = 0.8f;
    public const float NumberRise = 1.1f;
    public const int NumberPoolSize = 32;

    public static int NumbersShown;
    public static int CritsRolled;
    public static int BurstsEmitted;
    public static int ParticleSystemsCreated;

    /// <summary>Test seam: forces every crit roll (null = roll normally).</summary>
    public static bool? ForceCrit;

    static float trauma;
    static float lastTraumaTime;
    static Runner runner;

    // ---------------------------------------------------------------- crits

    /// <summary>
    /// Roll the attacker's crit chance (<see cref="Equipment.GetCrit"/>, percent).
    /// Returns the possibly doubled damage.
    /// </summary>
    /// <summary>Last direct striker and its frame (gear-slots S5 kill effects resolve in the same frame).</summary>
    static Transform lastStriker;
    static int lastStrikeFrame = -1;

    public static Transform StrikerThisFrame() =>
        lastStrikeFrame == Time.frameCount && lastStriker != null ? lastStriker : null;

    public static int RollCrit(Transform attacker, int damage, out bool crit)
    {
        crit = false;
        lastStriker = attacker;
        lastStrikeFrame = Time.frameCount;
        var eq = attacker != null ? attacker.GetComponent<Equipment>() : null;
        if (ForceCrit.HasValue) crit = ForceCrit.Value;
        else if (HeroComboState.TryAmbush(attacker)) crit = true; // Ambush (class-kits-parity)
        else if (eq != null) crit = Random.value * 100f < eq.GetCrit();
        if (!crit) return damage;
        CritsRolled++;
        GearUniqueEffects.OnCrit(attacker);
        return Mathf.Max(damage + 1, Mathf.RoundToInt(damage * GearUniqueEffects.CritMultiplier(eq, CritMultiplier)));
    }

    // ---------------------------------------------------------------- events

    /// <summary>A husk took damage (solo/host apply, or a client's own relayed hit).</summary>
    public static void OnEnemyHit(Vector3 at, int amount, bool crit)
    {
        var head = at + Vector3.up * 1.6f;
        if (DiabloSettings.ShowDamageNumbers)
            ShowNumber(head, crit ? $"{amount}!" : amount.ToString(),
                crit ? DiabloPalette.TrimGold : DiabloPalette.Parchment, crit ? 1.5f : 1f);
        Burst(at + Vector3.up * 1.0f, crit ? 30 : 18, BloodSpray, 3.4f, 0.65f);
        MeleeConeVfx.ShowHitRing(at); // playtest 2026-11-23: see who got hit
        if (crit)
        {
            Burst(at + Vector3.up * 1.0f, 16, CritSparks, 5f, 0.45f);
            DiabloAudio.PlayCrit();
        }
        AddTrauma(at, crit ? 0.4f : 0.22f);
    }

    /// <summary>A husk died: bone dust and a heavier kick.</summary>
    public static void OnEnemyDeath(Vector3 at)
    {
        Burst(at + Vector3.up * 0.8f, 30, BoneDust, 2.6f, 0.9f);
        Burst(at + Vector3.up * 1.0f, 24, BloodSpray, 3.8f, 0.7f);
        AddTrauma(at, 0.3f);
    }

    /// <summary>The local hero was struck.</summary>
    public static void OnPlayerHurt(Vector3 at, float amount)
    {
        if (DiabloSettings.ShowDamageNumbers)
            ShowNumber(at + Vector3.up * 2f, $"-{amount:0}", DiabloPalette.Blood, 1.1f);
        Burst(at + Vector3.up * 1.0f, 16, BloodSpray, 3f, 0.6f);
        AddTrauma(at, 0.35f);
    }

    /// <summary>Fireball blast at a point.</summary>
    public static void OnBlast(Vector3 at)
    {
        DiabloAudio.PlayBlast();
        AddTrauma(at, 0.55f);
    }

    /// <summary>Loot or XP text that is not damage (e.g. "+12 XP").</summary>
    public static void ShowText(Vector3 at, string text, Color color, float scale = 0.85f) =>
        ShowNumber(at, text, color, scale);

    // ---------------------------------------------------------------- shake

    /// <summary>Current trauma 0..1 after decay (test seam).</summary>
    public static float Trauma
    {
        get
        {
            float t = trauma - (Time.unscaledTime - lastTraumaTime) * TraumaDecayPerSecond;
            return Mathf.Clamp01(t);
        }
    }

    /// <summary>Add shake from an event at a world point; fades with distance to the camera pivot.</summary>
    public static void AddTrauma(Vector3 at, float amount)
    {
        float falloff = 1f;
        var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
        if (hero != null)
        {
            var d = hero.transform.position - at;
            d.y = 0f;
            falloff = Mathf.Clamp01(1f - d.magnitude / ShakeHearingRange);
        }
        trauma = Mathf.Clamp01(Trauma + amount * falloff);
        lastTraumaTime = Time.unscaledTime;
    }

    /// <summary>
    /// Camera offset for this frame: trauma² × Perlin, so small hits nudge and
    /// big blasts kick. Zero when the player switched shake off.
    /// </summary>
    public static Vector3 ShakeOffset
    {
        get
        {
            if (!DiabloSettings.ScreenShake) return Vector3.zero;
            float t = Trauma;
            if (t <= 0f) return Vector3.zero;
            float k = t * t * MaxShakeOffset;
            float time = Time.unscaledTime * 28f;
            return new Vector3(
                (Mathf.PerlinNoise(time, 0.1f) - 0.5f) * 2f * k,
                (Mathf.PerlinNoise(0.3f, time) - 0.5f) * 2f * k,
                (Mathf.PerlinNoise(time, 0.7f) - 0.5f) * 2f * k);
        }
    }

    // ---------------------------------------------------------------- particles

    struct Look
    {
        public string material;
        public Color a, b;
        public float size, gravity;
    }

    static readonly Look BloodSpray = new Look
    {
        material = "SkillArt/FireSmoke", a = DiabloPalette.Blood,
        b = new Color(0.45f, 0.02f, 0.04f, 1f), size = 0.32f, gravity = 1.4f
    };
    static readonly Look CritSparks = new Look
    {
        material = "SkillArt/FireAdditive", a = DiabloPalette.TrimGold,
        b = new Color(1f, 0.95f, 0.7f, 1f), size = 0.18f, gravity = 0.6f
    };
    static readonly Look BoneDust = new Look
    {
        material = "SkillArt/FireSmoke", a = DiabloPalette.ParchmentDim,
        b = new Color(0.32f, 0.29f, 0.26f, 1f), size = 0.5f, gravity = -0.15f
    };

    static readonly Dictionary<string, ParticleSystem> systems = new Dictionary<string, ParticleSystem>();

    /// <summary>Emit a radial burst from the pooled system for this look.</summary>
    static void Burst(Vector3 at, int count, Look look, float speed, float lifetime)
    {
        if (!Application.isPlaying) return;
        var ps = SystemFor(look);
        if (ps == null) return;
        BurstsEmitted++;
        var ep = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            var dir = Random.onUnitSphere;
            dir.y = Mathf.Abs(dir.y) * 0.8f + 0.2f;
            ep.position = at;
            ep.velocity = dir * speed * Random.Range(0.5f, 1f);
            ep.startLifetime = lifetime * Random.Range(0.7f, 1f);
            ep.startSize = look.size * Random.Range(0.7f, 1.3f);
            ep.startColor = Color.Lerp(look.a, look.b, Random.value);
            ps.Emit(ep, 1);
        }
    }

    static ParticleSystem SystemFor(Look look)
    {
        string key = look.material + look.gravity;
        if (systems.TryGetValue(key, out var ps) && ps != null) return ps;
        var go = new GameObject("JuiceFX_" + key.Replace("SkillArt/", ""));
        Object.DontDestroyOnLoad(go);
        ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = look.gravity;
        main.maxParticles = 600;
        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = false;
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        var mat = Res.Get<Material>(look.material);
        if (mat != null) r.sharedMaterial = mat;
        ps.Play();
        systems[key] = ps;
        ParticleSystemsCreated++;
        return ps;
    }

    // ---------------------------------------------------------------- numbers

    sealed class Entry
    {
        public Text text;
        public Vector3 world;
        public float age = -1f;
        public float scale = 1f;
        public float drift;
    }

    sealed class Runner : MonoBehaviour
    {
        public readonly List<Entry> pool = new List<Entry>();
        public Canvas canvas;
        int next;

        public Entry Take()
        {
            if (pool.Count < NumberPoolSize)
            {
                var go = new GameObject("DmgNumber");
                go.transform.SetParent(canvas.transform, false);
                var t = go.AddComponent<Text>();
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize = 26;
                t.fontStyle = FontStyle.Bold;
                t.alignment = TextAnchor.MiddleCenter;
                t.raycastTarget = false;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.rectTransform.sizeDelta = new Vector2(160f, 40f);
                var o = go.AddComponent<Outline>();
                o.effectColor = new Color(0f, 0f, 0f, 0.85f);
                o.effectDistance = new Vector2(1.5f, -1.5f);
                var e = new Entry { text = t };
                pool.Add(e);
                return e;
            }
            // Reuse the oldest slot when all are busy.
            var reuse = pool[next];
            next = (next + 1) % pool.Count;
            return reuse;
        }

        void Update()
        {
            var cam = Camera.main;
            float dt = Time.unscaledDeltaTime;
            foreach (var e in pool)
            {
                if (e.age < 0f) continue;
                e.age += dt / NumberLifetime;
                if (e.age >= 1f)
                {
                    e.age = -1f;
                    e.text.enabled = false;
                    continue;
                }
                float rise = 1f - Mathf.Pow(1f - e.age, 3f); // OutCubic
                var w = e.world + Vector3.up * NumberRise * rise + Vector3.right * e.drift * rise;
                if (cam != null)
                {
                    var sp = cam.WorldToScreenPoint(w);
                    e.text.enabled = sp.z > 0f;
                    e.text.rectTransform.position = sp;
                }
                // Pop: overshoot in the first 15 %, then settle; fade the last 40 %.
                float pop = e.age < 0.15f ? Mathf.Lerp(1.6f, 1f, e.age / 0.15f) : 1f;
                e.text.rectTransform.localScale = Vector3.one * e.scale * pop;
                var c = e.text.color;
                c.a = e.age < 0.6f ? 1f : 1f - (e.age - 0.6f) / 0.4f;
                e.text.color = c;
            }
        }
    }

    /// <summary>Number entries currently on screen (test seam).</summary>
    public static int ActiveNumbers
    {
        get
        {
            if (runner == null) return 0;
            int n = 0;
            foreach (var e in runner.pool) if (e.age >= 0f) n++;
            return n;
        }
    }

    /// <summary>Text of the most recent number (test seam).</summary>
    public static string LastNumberText { get; private set; }

    static void ShowNumber(Vector3 world, string text, Color color, float scale)
    {
        if (!Application.isPlaying) return;
        EnsureRunner();
        var e = runner.Take();
        e.world = world;
        e.age = 0f;
        e.scale = scale;
        e.drift = Random.Range(-0.4f, 0.4f);
        e.text.text = text;
        e.text.color = color;
        e.text.enabled = Camera.main != null;
        NumbersShown++;
        LastNumberText = text;
    }

    static void EnsureRunner()
    {
        if (runner != null) return;
        var go = new GameObject("DiabloJuice");
        Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<Runner>();
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5; // above the world, below menus
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        runner.canvas = canvas;
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        NumbersShown = 0;
        CritsRolled = 0;
        BurstsEmitted = 0;
        ForceCrit = null;
        trauma = 0f;
        LastNumberText = null;
        if (runner != null)
            foreach (var e in runner.pool)
            {
                e.age = -1f;
                e.text.enabled = false;
            }
    }
}
