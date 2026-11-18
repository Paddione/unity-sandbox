using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lanternbearer companion (second-class S2). Orbits its owner, strikes the
/// nearest husk inside <see cref="reach"/> every <see cref="interval"/>
/// seconds and fades after <see cref="lifetime"/>. Cosmetic wisps (co-op
/// mirrors) never deal damage; damage only on the local authority.
/// </summary>
public sealed class Wisp : MonoBehaviour
{
    public static readonly List<Wisp> Active = new List<Wisp>();

    public Transform owner;
    public int damage = 1;
    public float interval = 0.8f;
    public float reach = 6f;
    public float lifetime = 20f;
    public float orbitRadius = 1.4f;
    public float phase;
    public bool cosmetic;

    public int Strikes { get; private set; }
    float born;
    float lastStrike = float.NegativeInfinity;

    public static Wisp Spawn(Transform owner, int index, int count, int damage, float lifetime, bool cosmetic)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = $"Wisp_{index}";
        var col = go.GetComponent<Collider>();
        if (Application.isPlaying) Object.Destroy(col); else Object.DestroyImmediate(col);
        go.transform.localScale = Vector3.one * 0.22f;
        var r = go.GetComponent<Renderer>();
        if (r != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                var mat = new Material(shader) { color = new Color(1f, 0.95f, 0.7f) };
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.45f) * 2f);
                r.sharedMaterial = mat;
            }
        }
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.85f, 0.5f);
        light.range = 3f;
        light.intensity = 1.5f;
        var w = go.AddComponent<Wisp>();
        w.owner = owner;
        w.damage = damage;
        w.lifetime = lifetime;
        w.cosmetic = cosmetic;
        w.phase = count > 0 ? index * Mathf.PI * 2f / count : 0f;
        w.born = Time.time;
        if (owner != null) go.transform.position = w.OrbitPoint(Time.time);
        return w;
    }

    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    public Vector3 OrbitPoint(float t)
    {
        var center = owner != null ? owner.position : transform.position;
        float a = phase + t * 2.2f;
        return center + new Vector3(Mathf.Cos(a) * orbitRadius, 1.6f, Mathf.Sin(a) * orbitRadius);
    }

    void Update()
    {
        if (owner == null || Time.time - born >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
        transform.position = OrbitPoint(Time.time);
        TryStrike(Time.time);
    }

    /// <summary>Strike the nearest husk in reach when the interval allows (headless-callable).</summary>
    public GameObject TryStrike(float now)
    {
        if (now - lastStrike < interval) return null;
        var center = owner != null ? owner.position : transform.position;
        GameObject best = null;
        float bestD = reach;
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk == null || husk.GetComponent<EnemyHealth>() == null) continue;
            var d = husk.transform.position - center;
            d.y = 0f;
            if (d.magnitude <= bestD) { bestD = d.magnitude; best = husk; }
        }
        if (best == null) return null;
        lastStrike = now;
        Strikes++;
        if (!cosmetic) DiabloSkills.ApplyHit(best, damage, center, 0f, owner);
        return best;
    }

    /// <summary>Remove all wisps (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        foreach (var w in Active.ToArray())
            if (w != null) Object.DestroyImmediate(w.gameObject);
        Active.Clear();
    }
}
