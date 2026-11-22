using UnityEngine;

/// <summary>
/// S1 light: per-zone look that cross-fades on <c>ZoneGate</c> teleports.
/// Pure-C# grade data + blend math (headless-testable); the URP
/// <c>VolumeProfile</c> assets under <c>Settings/Volumes/</c> are generated
/// by <c>LightingBuilder</c> and mirror <see cref="ForZone"/>.
/// Wiring note: <c>ZoneGate.Teleport</c> is outside the S1 write-set, so the
/// call <c>ZoneAtmosphere.ShowZone(targetZone)</c> there (and the
/// <c>DiabloAmbience.ApplyZone</c> bed-volume hook) lands with the gate
/// edit follow-up; until then callers drive <c>ShowZone</c> directly.
/// </summary>
public sealed class ZoneAtmosphere : MonoBehaviour
{
    /// <summary>Cross-fade time for the zone grade (S1 acceptance).</summary>
    public const float BlendSeconds = 1.5f;

    /// <summary>Headless-testable grade: the knobs LightingBuilder bakes.</summary>
    [System.Serializable]
    public struct Grade
    {
        public float bloomIntensity;
        public float bloomThreshold;
        public float vignette;
        public float exposure;
        public float saturation;
        public float temperature;
        public float tint;

        public static Grade Lerp(Grade a, Grade b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Grade
            {
                bloomIntensity = Mathf.Lerp(a.bloomIntensity, b.bloomIntensity, t),
                bloomThreshold = Mathf.Lerp(a.bloomThreshold, b.bloomThreshold, t),
                vignette = Mathf.Lerp(a.vignette, b.vignette, t),
                exposure = Mathf.Lerp(a.exposure, b.exposure, t),
                saturation = Mathf.Lerp(a.saturation, b.saturation, t),
                temperature = Mathf.Lerp(a.temperature, b.temperature, t),
                tint = Mathf.Lerp(a.tint, b.tint, t),
            };
        }
    }

    /// <summary>Zone grade table (ActProgress zones; unknown zones fall back to town).</summary>
    public static Grade ForZone(int zone)
    {
        switch (zone)
        {
            case ActProgress.Ossuary: // Sunken Ossuary: near-dark, torch islands pop, vignette.
                return new Grade { bloomIntensity = 0.9f, bloomThreshold = 0.7f, vignette = 0.45f, exposure = -0.45f, saturation = -15f, temperature = -10f, tint = 5f };
            case ActProgress.Kiln: // Cinder Kiln: hot ember dark (shares Ossuary mood, hotter tint).
                return new Grade { bloomIntensity = 1.0f, bloomThreshold = 0.65f, vignette = 0.5f, exposure = -0.35f, saturation = -5f, temperature = 15f, tint = 10f };
            case ActProgress.Barrens: // Glass Barrens: pale cold glare, bright bloom off the glass, desaturated.
                return new Grade { bloomIntensity = 1.2f, bloomThreshold = 0.75f, vignette = 0.35f, exposure = 0.05f, saturation = -20f, temperature = -20f, tint = -5f };
            default: // Lantern Hold / Ashen Fen town: warm 2700K lantern key-light, gentle bloom.
                return new Grade { bloomIntensity = 0.6f, bloomThreshold = 0.9f, vignette = 0.25f, exposure = 0.15f, saturation = 10f, temperature = 25f, tint = 5f };
        }
    }

    static ZoneAtmosphere instance;
    public static ZoneAtmosphere Instance => instance;

    Grade from;
    Grade to;
    float blendT = 1f;
    int zone = ActProgress.Town;

    /// <summary>Current zone (ActProgress id).</summary>
    public int Zone => zone;
    /// <summary>True while a cross-fade is running.</summary>
    public bool Blending => blendT < 1f;
    /// <summary>Current (possibly mid-blend) grade.</summary>
    public Grade Current => Grade.Lerp(from, to, BlendSeconds <= 0f ? 1f : blendT);

    public static ZoneAtmosphere Ensure()
    {
        if (instance == null)
        {
            var go = new GameObject("ZoneAtmosphere");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<ZoneAtmosphere>();
            instance.from = instance.to = ForZone(instance.zone);
        }
        return instance;
    }

    /// <summary>Start the 1.5 s cross-fade to the zone grade (headless-callable).</summary>
    public void ShowZone(int next)
    {
        from = Current;
        to = ForZone(next);
        zone = next;
        blendT = 0f;
        if (from.Equals(to)) blendT = 1f;
    }

    /// <summary>Advance the blend; returns true while blending. Headless-callable.</summary>
    public bool Tick(float dt)
    {
        if (blendT >= 1f) return false;
        blendT = Mathf.Min(1f, blendT + dt / BlendSeconds);
        Apply(Current);
        return blendT < 1f;
    }

    void Update() => Tick(Time.deltaTime);

    /// <summary>Push the grade into the scene volume (guarded: no volume = no-op, stays headless-safe.
    /// URP types via reflection (no compile-time URP asm dependency in Sandbox.Runtime).</summary>
    void Apply(Grade g)
    {
        var volume = GetComponent("Volume") as Behaviour;
        if (volume == null) return;
        try
        {
            var profileProp = volume.GetType().GetProperty("profile");
            var profile = profileProp != null ? profileProp.GetValue(volume, null) : null;
            if (profile == null) return;
            var tryGet = profile.GetType().GetMethods();
            foreach (var kv in new System.Collections.Generic.KeyValuePair<string, System.Action<object>>[]
            {
                new System.Collections.Generic.KeyValuePair<string, System.Action<object>>("Bloom", c => {
                    SetParam(c, "intensity", g.bloomIntensity); SetParam(c, "threshold", g.bloomThreshold); }),
                new System.Collections.Generic.KeyValuePair<string, System.Action<object>>("Vignette", c => {
                    SetParam(c, "intensity", g.vignette); }),
                new System.Collections.Generic.KeyValuePair<string, System.Action<object>>("ColorAdjustments", c => {
                    SetParam(c, "postExposure", g.exposure); SetParam(c, "saturation", g.saturation); }),
                new System.Collections.Generic.KeyValuePair<string, System.Action<object>>("WhiteBalance", c => {
                    SetParam(c, "temperature", g.temperature); SetParam(c, "tint", g.tint); }),
            })
            {
                var comp = FindVolumeComponent(profile, kv.Key);
                if (comp != null) kv.Value(comp);
            }
        }
        catch { }
    }

    static object FindVolumeComponent(object profile, string shortName)
    {
        try
        {
            var tryGetMethods = profile.GetType().GetMethod("TryGet", new System.Type[0]);
            // Generic TryGet<T>(out T) needs type; fall back to components list.
            var compsProp = profile.GetType().GetProperty("components");
            var comps = compsProp != null ? compsProp.GetValue(profile, null) as System.Collections.IList : null;
            if (comps == null) return null;
            foreach (var c in comps)
                if (c != null && c.GetType().Name == shortName) return c;
        }
        catch { }
        return null;
    }

    static void SetParam(object component, string field, float value)
    {
        try
        {
            var f = component.GetType().GetField(field);
            var param = f != null ? f.GetValue(component) : null;
            if (param == null) return;
            var m = param.GetType().GetMethod("Override", new System.Type[] { typeof(float) });
            if (m != null) m.Invoke(param, new object[] { value });
        }
        catch { }
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
