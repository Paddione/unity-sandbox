using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Zone maps (cinderreach-group-quest-map). Each zone floor
/// (<see cref="WorldBounds.Zones"/>) gets one top-down picture, rendered once on
/// first use by an orthographic camera turned to the isometric yaw, so "up" on
/// the map is "up" on screen. Heroes, enemies, NPCs, pickups and floating
/// text are hidden for that one frame; the HUD draws them as live icons.
/// Pure helpers (<see cref="ToMap"/>, <see cref="IndexOf"/>, <see cref="NextHop"/>)
/// carry the math the tests pin.
/// </summary>
public static class ZoneMap
{
    /// <summary>Fixed isometric camera yaw (<see cref="DiabloClickController.yaw"/>).</summary>
    public const float Yaw = 45f;

    /// <summary>Floor margin around the walkable rectangle (edge walls, gate arches).</summary>
    public const float Margin = 2.5f;

    /// <summary>Render texture edge in pixels.</summary>
    public const int TextureSize = 512;

    /// <summary>Background of the map outside the floor and with no picture (batchmode).</summary>
    public static readonly Color Background = new Color(0.07f, 0.06f, 0.05f, 1f);

    /// <summary>Zone index of a <see cref="WorldBounds"/> zone name (0 when unknown).</summary>
    public static int IndexOf(string zoneName)
    {
        switch (zoneName)
        {
            case "Fen": return ActProgress.Town;
            case StoryIds.ZoneOssuary: return ActProgress.Ossuary;
            case StoryIds.ZoneKiln: return ActProgress.Kiln;
            case StoryIds.ZoneBarrens: return ActProgress.Barrens;
            default: return 0;
        }
    }

    public static int IndexAt(Vector3 p) => IndexOf(WorldBounds.ZoneAt(p).name);

    /// <summary>Zone floor of a zone index (the Fen for unknown indices).</summary>
    public static WorldBounds.Zone Zone(int index)
    {
        foreach (var z in WorldBounds.Zones)
            if (IndexOf(z.name) == index) return z;
        return WorldBounds.Zones[0];
    }

    /// <summary>Next zone on the gate chain Town ↔ Ossuary ↔ Kiln ↔ Barrens.</summary>
    public static int NextHop(int from, int to)
    {
        if (from == to) return from;
        return from + (to > from ? 1 : -1);
    }

    /// <summary>Edge length in metres the square picture covers (the floor turned by the yaw fits inside).</summary>
    public static float Span(WorldBounds.Zone z) => 2f * (z.halfExtent + Margin) * 1.41421356f;

    /// <summary>
    /// Normalised map position (0..1 on both axes, y up) of a world point on
    /// zone <paramref name="z"/>'s picture. Map right is camera right and map up
    /// is camera forward on the ground.
    /// </summary>
    public static Vector2 ToMap(Vector3 world, WorldBounds.Zone z, float yaw = Yaw)
    {
        var d = world - z.center;
        var right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        var fwd = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        float span = Span(z);
        return new Vector2((d.x * right.x + d.z * right.z) / span + 0.5f, (d.x * fwd.x + d.z * fwd.z) / span + 0.5f);
    }

    /// <summary>UI z-rotation of an icon for a world heading (degrees, Y euler): 0 = facing map up.</summary>
    public static float IconRotation(float worldYaw, float yaw = Yaw) => -(worldYaw - yaw);

    // ---- pictures ----

    static readonly Dictionary<int, RenderTexture> pictures = new Dictionary<int, RenderTexture>();

    /// <summary>Pictures rendered this session (test seam).</summary>
    public static int Renders;

    /// <summary>False in batchmode or without a graphics device: maps fall back to a plain floor.</summary>
    public static bool CanRender
    {
        get
        {
            try { return !Application.isBatchMode && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null; }
            catch { return false; }
        }
    }

    /// <summary>The zone's picture, rendered on first request; null when nothing can render.</summary>
    public static Texture Picture(int index)
    {
        if (pictures.TryGetValue(index, out var rt) && rt != null && rt.IsCreated()) return rt;
        if (!CanRender) return null;
        rt = Render(Zone(index));
        if (rt != null) pictures[index] = rt;
        return rt;
    }

    static RenderTexture Render(WorldBounds.Zone z)
    {
        var rt = new RenderTexture(TextureSize, TextureSize, 24, RenderTextureFormat.ARGB32)
        {
            name = "ZoneMap_" + z.name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };
        rt.Create();
        var go = new GameObject("ZoneMapCamera");
        var cam = go.AddComponent<Camera>();
        cam.enabled = false;
        cam.orthographic = true;
        cam.orthographicSize = Span(z) * 0.5f;
        cam.transform.position = z.center + Vector3.up * 80f;
        cam.transform.rotation = Quaternion.Euler(90f, Yaw, 0f);
        cam.nearClipPlane = 1f;
        cam.farClipPlane = 120f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Background;
        int ui = LayerMask.NameToLayer("UI");
        cam.cullingMask = ui >= 0 ? ~(1 << ui) : ~0;
        cam.targetTexture = rt;

        var hidden = HideDynamic();
        bool fog = RenderSettings.fog;
        var ambient = RenderSettings.ambientLight;
        var ambientMode = RenderSettings.ambientMode;
        try
        {
            // Crypt zones are lit by torches for the iso camera; from straight above
            // the floor would stay black, so the picture gets flat ambient light.
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.52f, 0.48f);
            cam.Render();
            Renders++;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Map] zone picture failed: " + e.Message);
        }
        finally
        {
            RenderSettings.fog = fog;
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientLight = ambient;
            foreach (var r in hidden) if (r != null) r.enabled = true;
            cam.targetTexture = null;
            Object.Destroy(go);
        }
        return rt;
    }

    /// <summary>Disables every renderer the map draws as an icon instead; returns them for restoring.</summary>
    static List<Renderer> HideDynamic()
    {
        var list = new List<Renderer>();
        void HideUnder(Component c)
        {
            if (c == null) return;
            foreach (var r in c.GetComponentsInChildren<Renderer>())
                if (r != null && r.enabled) { r.enabled = false; list.Add(r); }
        }
        foreach (var c in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None)) HideUnder(c);
        foreach (var c in Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None)) HideUnder(c);
        foreach (var c in Object.FindObjectsByType<StoryNpc>(FindObjectsSortMode.None)) HideUnder(c);
        foreach (var c in Object.FindObjectsByType<StoryPickup>(FindObjectsSortMode.None)) HideUnder(c);
        foreach (var c in Object.FindObjectsByType<LootDrop>(FindObjectsSortMode.None)) HideUnder(c);
        foreach (var t in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
        {
            var r = t.GetComponent<Renderer>();
            if (r != null && r.enabled) { r.enabled = false; list.Add(r); }
        }
        foreach (var p in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
        {
            var r = p.GetComponent<Renderer>();
            if (r != null && r.enabled) { r.enabled = false; list.Add(r); }
        }
        return list;
    }

    /// <summary>Drops cached pictures (tests, scene rebuilds).</summary>
    public static void ResetPictures()
    {
        foreach (var rt in pictures.Values) if (rt != null) rt.Release();
        pictures.Clear();
    }
}
