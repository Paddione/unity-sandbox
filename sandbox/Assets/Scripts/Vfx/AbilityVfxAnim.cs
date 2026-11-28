using UnityEngine;

/// <summary>
/// Lightweight, MaterialPropertyBlock-driven animator for class ability VFX
/// prefabs. Avoids per-cast Material allocations (ServerShaderGuardTests and
/// GC-friendly). Supports timed bursts, persistent zones and stretched chains.
/// </summary>
public sealed class AbilityVfxAnim : MonoBehaviour
{
    public enum Mode
    {
        TimedBurst,
        PersistentZone,
        ChainStretch,
    }

    static readonly int PropDissolve = Shader.PropertyToID("_Dissolve");
    static readonly int PropProgress = Shader.PropertyToID("_Progress");
    static readonly int PropColor = Shader.PropertyToID("_Color");

    Mode mode;
    float duration = 0.6f;
    float elapsed;
    Vector3 startScale = Vector3.one;
    Vector3 endScale = Vector3.one;
    float spinDegPerSec;
    bool fadeDissolve = true;
    bool initialized;

    Renderer[] renderers;
    Light[] lights;
    float[] baseLightIntensities;
    Color[] baseColors;
    MaterialPropertyBlock mpb;

    void CacheComponents()
    {
        if (renderers != null) return;
        renderers = GetComponentsInChildren<Renderer>(true);
        lights = GetComponentsInChildren<Light>(true);
        baseLightIntensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
            baseLightIntensities[i] = lights[i].intensity;
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            var mat = renderers[i].sharedMaterial;
            baseColors[i] = mat != null && mat.HasProperty(PropColor)
                ? mat.GetColor(PropColor)
                : Color.white;
        }
        mpb = new MaterialPropertyBlock();
    }

    public void InitBurst(float burstDuration, Vector3 fromScale, Vector3 toScale, float spinDeg = 0f, bool dissolveOut = true)
    {
        CacheComponents();
        mode = Mode.TimedBurst;
        duration = Mathf.Max(0.1f, burstDuration);
        elapsed = 0f;
        startScale = fromScale;
        endScale = toScale;
        spinDegPerSec = spinDeg;
        fadeDissolve = dissolveOut;
        transform.localScale = startScale;
        initialized = true;
        ApplyProgress(0f);
    }

    public void InitZone(float zoneDuration, Vector3 targetScale)
    {
        CacheComponents();
        mode = Mode.PersistentZone;
        duration = Mathf.Max(0.25f, zoneDuration);
        elapsed = 0f;
        startScale = targetScale * 0.65f;
        endScale = targetScale;
        spinDegPerSec = 0f;
        fadeDissolve = false;
        transform.localScale = startScale;
        initialized = true;
        ApplyZoneProgress(0f);
    }

    public void InitChain(Vector3 origin, Vector3 end, float chainDuration)
    {
        CacheComponents();
        mode = Mode.ChainStretch;
        duration = Mathf.Max(0.1f, chainDuration);
        elapsed = 0f;
        Vector3 delta = end - origin;
        float len = Mathf.Max(0.25f, delta.magnitude);
        if (delta.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        transform.position = origin;
        startScale = new Vector3(1f, 1f, len);
        endScale = new Vector3(0.85f, 0.85f, len);
        transform.localScale = startScale;
        spinDegPerSec = 0f;
        fadeDissolve = true;
        initialized = true;
        ApplyProgress(0f);
    }

    public void ApplyTintOverride(Color tint)
    {
        CacheComponents();
        for (int i = 0; i < baseColors.Length; i++)
            baseColors[i] = new Color(tint.r, tint.g, tint.b, baseColors[i].a);
        for (int i = 0; i < lights.Length; i++)
            lights[i].color = tint;
        ApplyProgress(duration > 0f ? Mathf.Clamp01(elapsed / duration) : 0f);
    }

    void Update()
    {
        if (!initialized) return;
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        if (mode == Mode.PersistentZone)
        {
            ApplyZoneProgress(t);
            if (elapsed >= duration)
                Destroy(gameObject);
            return;
        }

        float ease = 1f - Mathf.Pow(1f - t, 2.2f);
        transform.localScale = Vector3.Lerp(startScale, endScale, ease);
        if (spinDegPerSec != 0f)
            transform.Rotate(0f, spinDegPerSec * Time.deltaTime, 0f, Space.Self);
        ApplyProgress(t);

        if (elapsed >= duration)
            Destroy(gameObject);
    }

    void ApplyProgress(float t)
    {
        float dissolve = fadeDissolve ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.2f) / 0.8f)) : 0f;
        float fade = t < 0.12f ? t / 0.12f : 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.45f) / 0.55f));
        float lightFade = 1f - t;

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null || r is ParticleSystemRenderer) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetFloat(PropDissolve, dissolve);
            mpb.SetFloat(PropProgress, fade);
            var c = baseColors[i];
            c.a *= fade;
            mpb.SetColor(PropColor, c);
            r.SetPropertyBlock(mpb);
        }

        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null)
                lights[i].intensity = baseLightIntensities[i] * lightFade;
    }

    void ApplyZoneProgress(float t)
    {
        float fadeIn = Mathf.Clamp01(elapsed / 0.22f);
        float fadeOut = Mathf.Clamp01((duration - elapsed) / 0.35f);
        float env = Mathf.Min(fadeIn, fadeOut);
        transform.localScale = Vector3.Lerp(startScale, endScale, fadeIn);

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null || r is ParticleSystemRenderer) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetFloat(PropProgress, env);
            mpb.SetFloat(PropDissolve, 1f - env);
            var c = baseColors[i];
            c.a *= env;
            mpb.SetColor(PropColor, c);
            r.SetPropertyBlock(mpb);
        }

        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null)
                lights[i].intensity = baseLightIntensities[i] * env;
    }
}
