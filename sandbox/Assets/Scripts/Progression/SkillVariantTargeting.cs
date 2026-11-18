using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Planar targeting for skill variants (cinderreach-skill-variants S2).
/// Cleave picks targets with <see cref="InCleaveArea"/>: the round swing
/// keeps the current radius at 360°, the frontal swing trades the back half
/// for <see cref="SkillVariantCatalog.FrontalArcDegrees"/> at
/// <see cref="SkillVariantCatalog.FrontalRangeFactor"/> range. Damage and
/// cooldown paths are untouched (same blow, same gate).
/// </summary>
public static class SkillVariantTargeting
{
    /// <summary>
    /// Planar cone test: within radius and inside arcDegrees total around
    /// forward (arcDegrees &gt;= 360 falls back to a pure radius check).
    /// A target exactly on the rim counts as inside.
    /// </summary>
    public static bool InCleaveArea(Vector3 origin, Vector3 forward, Vector3 target, float radius, float arcDegrees)
    {
        var to = target - origin;
        to.y = 0f;
        if (to.sqrMagnitude > radius * radius) return false;
        if (arcDegrees >= 360f) return true;
        var f = forward;
        f.y = 0f;
        if (f.sqrMagnitude < 0.000001f) return true;
        if (to.sqrMagnitude < 0.000001f) return true;
        float half = arcDegrees * 0.5f * Mathf.Deg2Rad;
        float angle = Vector3.Angle(f, to) * Mathf.Deg2Rad;
        return angle <= half + 0.0001f;
    }
}

/// <summary>
/// Stationary halo ward (Searing Halo variant): anchored at the cast point,
/// ticks the same blow every <see cref="SkillVariantCatalog.HaloTickInterval"/>
/// for <see cref="SkillVariantCatalog.HaloWardDuration"/> over the same
/// area. Recasts replace the old zone; death and owner loss clear it (scene
/// changes destroy scene objects anyway).
/// </summary>
public sealed class HaloZone : MonoBehaviour
{
    public static readonly List<HaloZone> Active = new List<HaloZone>();

    public Transform owner;
    public int damage;
    public float radius = 3.5f;
    public float tickInterval = 1f;
    public float duration = 8f;
    public float knockback;

    Vector3 anchor;
    float endTime;
    float nextTick;
    float deathMark;

    /// <summary>Anchor of the zone (cast point, never follows the hero).</summary>
    public Vector3 Anchor => anchor;

    public static HaloZone Spawn(Transform owner, Vector3 anchor, int damage, float radius,
        float duration, float tickInterval, float knockback)
    {
        var go = new GameObject("HaloWard");
        go.transform.position = anchor;
        var zone = go.AddComponent<HaloZone>();
        zone.owner = owner;
        zone.anchor = anchor;
        zone.damage = damage;
        zone.radius = radius;
        zone.duration = duration;
        zone.tickInterval = Mathf.Max(0.05f, tickInterval);
        zone.knockback = knockback;
        zone.endTime = Time.time + duration;
        zone.nextTick = Time.time + zone.tickInterval;
        zone.deathMark = PlayerHealth.LastDeathTime;
        Active.Add(zone);
        zone.SpawnWardVisual();
        return zone;
    }

    /// <summary>Destroy every zone of an owner (recast replaces the old one).</summary>
    public static void ClearFor(Transform owner)
    {
        if (owner == null) return;
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            var z = Active[i];
            if (z == null) { Active.RemoveAt(i); continue; }
            if (z.owner == owner)
            {
                Active.RemoveAt(i);
                if (Application.isPlaying) Object.Destroy(z.gameObject);
                else Object.DestroyImmediate(z.gameObject);
            }
        }
    }

    /// <summary>Test seam: every live zone of an owner.</summary>
    public static HaloZone[] ForOwner(Transform owner)
    {
        var list = new List<HaloZone>();
        foreach (var z in Active)
            if (z != null && z.owner == owner) list.Add(z);
        return list.ToArray();
    }

    /// <summary>Test seam: apply one tick immediately, returns husks hit.</summary>
    public int TickNow()
    {
        return Tick();
    }

    void Update()
    {
        if (owner == null) { DestroySelf(); return; }
        if (PlayerHealth.LastDeathTime > deathMark) { DestroySelf(); return; }
        transform.position = anchor;
        if (Time.time >= nextTick)
        {
            Tick();
            nextTick += tickInterval;
        }
        if (Time.time >= endTime) DestroySelf();
    }

    int Tick()
    {
        int hits = 0;
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk == null || husk.GetComponent<EnemyHealth>() == null) continue;
            var to = husk.transform.position - anchor;
            to.y = 0f;
            if (to.magnitude > radius) continue;
            DiabloSkills.ApplyHit(husk, damage, anchor, knockback, owner);
            hits++;
        }
        return hits;
    }

    void SpawnWardVisual()
    {
        if (!Application.isPlaying) return;
        var prefab = Resources.Load<GameObject>("SkillArt/CleaveArc");
        GameObject ring;
        if (prefab != null)
        {
            ring = Object.Instantiate(prefab, anchor, Quaternion.identity);
            ring.transform.localScale = Vector3.one * (radius / 2.6f);
            foreach (var r in ring.GetComponentsInChildren<Renderer>())
                r.material.color = new Color(1f, 0.7f, 0.3f, r.material.color.a);
        }
        else ring = new GameObject("HaloWardRing");
        ring.name = "HaloWardRing";
        ring.transform.SetParent(transform, true);
        var light = ring.GetComponent<Light>() ?? ring.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.8f, 0.4f);
        light.range = radius * 2f;
        light.intensity = 4f;
        Object.Destroy(ring, duration);
    }

    void OnDestroy()
    {
        Active.Remove(this);
    }

    void DestroySelf()
    {
        Active.Remove(this);
        if (Application.isPlaying) Object.Destroy(gameObject);
        else Object.DestroyImmediate(gameObject);
    }

    /// <summary>Headless teardown: drop the registry (scene objects die with the test).</summary>
    public static void ResetRun()
    {
        Active.Clear();
    }
}
