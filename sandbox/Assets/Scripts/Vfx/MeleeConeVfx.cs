using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Melee readability (playtest feedback 2026-11-23): every swing paints its
/// real damage cone on the ground (same range and half-angle the hit test
/// uses), and every husk that takes a hit gets an expanding impact ring at
/// its feet. Gold cone = landed, pale cone = whiff. Additive, pooled, runs on
/// unscaled time so it stays visible through <see cref="HitStop"/>.
/// Static counters are test seams, like <see cref="PresentationVfx"/>.
/// </summary>
public static class MeleeConeVfx
{
    /// <summary>Cone visible time (fade out over the whole span).</summary>
    public const float ConeLifetime = 0.28f;
    /// <summary>Impact ring visible time.</summary>
    public const float RingLifetime = 0.3f;
    /// <summary>Impact ring radius at start and end of its expansion.</summary>
    public const float RingStartRadius = 0.35f;
    public const float RingEndRadius = 1.15f;
    /// <summary>Lift above the floor so the decal never z-fights.</summary>
    const float GroundLift = 0.06f;
    const int PoolSize = 16;

    public static int ConesShown;
    public static int RingsShown;

    /// <summary>Last shown cone (test seam): range and half-angle as drawn.</summary>
    public static float LastConeRange { get; private set; }
    public static float LastConeHalfAngle { get; private set; }
    public static bool LastConeLanded { get; private set; }

    static readonly Color LandedColor = new Color(1f, 0.78f, 0.32f, 0.85f);
    static readonly Color WhiffColor = new Color(0.85f, 0.82f, 0.74f, 0.35f);
    static readonly Color RingColor = new Color(1f, 0.35f, 0.18f, 0.95f);

    static Runner runner;
    static Material baseMaterial;
    static Mesh ringMesh;
    static readonly Dictionary<long, Mesh> coneMeshes = new Dictionary<long, Mesh>();

    /// <summary>Paint the swing's damage cone at the attacker's feet.</summary>
    public static void ShowCone(Vector3 origin, Vector3 dir, float range, float halfAngle, bool landed)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f || range <= 0f) return;
        ConesShown++;
        LastConeRange = range;
        LastConeHalfAngle = halfAngle;
        LastConeLanded = landed;
        if (DedicatedServer.RequestedByProcess) return; // nobody looks at the server
        var r = EnsureRunner();
        if (r == null) return;
        r.Spawn(ConeMesh(range, halfAngle), origin + Vector3.up * GroundLift,
            Quaternion.LookRotation(dir.normalized), Vector3.one,
            landed ? LandedColor : WhiffColor, ConeLifetime, false);
    }

    /// <summary>Impact ring under a husk that just took a hit.</summary>
    public static void ShowHitRing(Vector3 at)
    {
        RingsShown++;
        if (DedicatedServer.RequestedByProcess) return;
        var r = EnsureRunner();
        if (r == null) return;
        var feet = new Vector3(at.x, at.y + GroundLift, at.z);
        r.Spawn(RingMesh(), feet, Quaternion.identity, Vector3.one * RingStartRadius,
            RingColor, RingLifetime, true);
    }

    /// <summary>Live decals (test seam).</summary>
    public static int ActiveDecals => runner != null ? runner.ActiveCount : 0;

    public static void ResetRun()
    {
        ConesShown = 0;
        RingsShown = 0;
        LastConeRange = 0f;
        LastConeHalfAngle = 0f;
        LastConeLanded = false;
        if (runner != null) runner.HideAll();
    }

    // ------------------------------------------------------------- meshes

    /// <summary>
    /// Flat fan in the XZ plane pointing +Z: apex at the origin, rim at
    /// <paramref name="range"/>. Vertex alpha rises toward the rim so the
    /// edge of the reach reads as a bright slash line.
    /// </summary>
    public static Mesh ConeMesh(float range, float halfAngle)
    {
        long key = ((long)Mathf.RoundToInt(range * 100f) << 20) | (long)Mathf.RoundToInt(halfAngle * 10f);
        if (coneMeshes.TryGetValue(key, out var cached) && cached != null) return cached;
        int segments = Mathf.Max(6, Mathf.CeilToInt(halfAngle * 2f / 6f));
        // Rings: apex, inner body (55 %), rim band (92 %), outer edge (100 %).
        float[] radii = { 0.55f, 0.92f, 1f };
        float[] alphas = { 0.35f, 0.75f, 1f };
        var verts = new List<Vector3> { Vector3.zero };
        var colors = new List<Color> { new Color(1f, 1f, 1f, 0.15f) };
        for (int ring = 0; ring < radii.Length; ring++)
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Deg2Rad * Mathf.Lerp(-halfAngle, halfAngle, i / (float)segments);
                verts.Add(new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (range * radii[ring]));
                colors.Add(new Color(1f, 1f, 1f, alphas[ring]));
            }
        var tris = new List<int>();
        int row = segments + 1;
        for (int i = 0; i < segments; i++) tris.AddRange(new[] { 0, 1 + i, 2 + i });
        for (int ring = 0; ring < radii.Length - 1; ring++)
        {
            int a0 = 1 + ring * row, b0 = 1 + (ring + 1) * row;
            for (int i = 0; i < segments; i++)
                tris.AddRange(new[] { a0 + i, b0 + i, b0 + i + 1, a0 + i, b0 + i + 1, a0 + i + 1 });
        }
        var mesh = new Mesh { name = $"MeleeCone_{range:0.0}_{halfAngle:0}" };
        mesh.SetVertices(verts);
        mesh.SetColors(colors);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        coneMeshes[key] = mesh;
        return mesh;
    }

    /// <summary>Unit annulus (inner 0.7, outer 1.0) in the XZ plane.</summary>
    static Mesh RingMesh()
    {
        if (ringMesh != null) return ringMesh;
        const int segments = 32;
        var verts = new List<Vector3>();
        var colors = new List<Color>();
        var tris = new List<int>();
        for (int i = 0; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            verts.Add(d * 0.7f);
            colors.Add(new Color(1f, 1f, 1f, 0.2f));
            verts.Add(d);
            colors.Add(Color.white);
        }
        for (int i = 0; i < segments; i++)
        {
            int a = i * 2, b = a + 2;
            tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
        }
        ringMesh = new Mesh { name = "MeleeHitRing" };
        ringMesh.SetVertices(verts);
        ringMesh.SetColors(colors);
        ringMesh.SetTriangles(tris, 0);
        ringMesh.RecalculateBounds();
        return ringMesh;
    }

    /// <summary>
    /// Additive URP particle material (Resources, so it ships in every build),
    /// with a white base map so the vertex colours define the shape.
    /// </summary>
    static Material BaseMaterial()
    {
        if (baseMaterial != null) return baseMaterial;
        var src = Res.Get<Material>("SkillArt/FireAdditive");
        if (src == null) return null;
        baseMaterial = new Material(src) { name = "MeleeDecal" };
        if (baseMaterial.HasProperty("_BaseMap")) baseMaterial.SetTexture("_BaseMap", Texture2D.whiteTexture);
        baseMaterial.mainTexture = Texture2D.whiteTexture;
        return baseMaterial;
    }

    static Runner EnsureRunner()
    {
        if (runner != null) return runner;
        if (!Application.isPlaying) return null; // edit-mode tests: counters only, no scene objects
        if (BaseMaterial() == null) return null;
        var go = new GameObject("MeleeConeVfx");
        Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<Runner>();
        return runner;
    }

    sealed class Decal
    {
        public GameObject go;
        public MeshFilter filter;
        public MeshRenderer renderer;
        public MaterialPropertyBlock block;
        public Color color;
        public float born;
        public float life;
        public bool expands;
    }

    sealed class Runner : MonoBehaviour
    {
        readonly List<Decal> pool = new List<Decal>();
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public int ActiveCount
        {
            get
            {
                int n = 0;
                foreach (var d in pool) if (d.go != null && d.go.activeSelf) n++;
                return n;
            }
        }

        public void Spawn(Mesh mesh, Vector3 pos, Quaternion rot, Vector3 scale, Color color, float life, bool expands)
        {
            var d = Acquire();
            if (d == null) return;
            d.filter.sharedMesh = mesh;
            d.go.transform.SetPositionAndRotation(pos, rot);
            d.go.transform.localScale = scale;
            d.color = color;
            d.born = Time.unscaledTime;
            d.life = life;
            d.expands = expands;
            Apply(d, 0f);
            d.go.SetActive(true);
        }

        public void HideAll()
        {
            foreach (var d in pool) if (d.go != null) d.go.SetActive(false);
        }

        Decal Acquire()
        {
            Decal oldest = null;
            foreach (var d in pool)
            {
                if (d.go == null) continue;
                if (!d.go.activeSelf) return d;
                if (oldest == null || d.born < oldest.born) oldest = d;
            }
            if (pool.Count >= PoolSize) return oldest;
            var go = new GameObject("MeleeDecal");
            go.transform.SetParent(transform, false);
            var decal = new Decal
            {
                go = go,
                filter = go.AddComponent<MeshFilter>(),
                renderer = go.AddComponent<MeshRenderer>(),
                block = new MaterialPropertyBlock(),
            };
            decal.renderer.sharedMaterial = BaseMaterial();
            decal.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            decal.renderer.receiveShadows = false;
            go.SetActive(false);
            pool.Add(decal);
            return decal;
        }

        void Apply(Decal d, float t)
        {
            // Quick flash in, ease out: full for the first fifth, then fade.
            float fade = t < 0.2f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.2f) / 0.8f);
            var c = d.color;
            c.a *= fade;
            d.block.SetColor(BaseColorId, c);
            d.block.SetColor(ColorId, c);
            d.renderer.SetPropertyBlock(d.block);
            if (d.expands)
            {
                float r = Mathf.Lerp(RingStartRadius, RingEndRadius, 1f - (1f - t) * (1f - t));
                d.go.transform.localScale = Vector3.one * r;
            }
        }

        void LateUpdate()
        {
            float now = Time.unscaledTime;
            foreach (var d in pool)
            {
                if (d.go == null || !d.go.activeSelf) continue;
                float t = (now - d.born) / d.life;
                if (t >= 1f) { d.go.SetActive(false); continue; }
                Apply(d, t);
            }
        }
    }
}
