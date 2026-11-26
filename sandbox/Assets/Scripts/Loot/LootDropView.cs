using UnityEngine;

/// <summary>
/// S5 ground presentation for one <see cref="ItemInstance"/>: a small
/// emissive rarity ball on the floor (Magic blue, Rare gold, Unique orange —
/// <see cref="DiabloPalette"/>) with a soft rarity glow pool beneath it and
/// the item's own sprite floating above it. Derived purely from the S2
/// snapshot, so host, client and late join render identically.
/// </summary>
public sealed class LootDropView : MonoBehaviour
{
    public ItemInstance item;

    /// <summary>Ball mesh diameter in metres (test seam).</summary>
    public const float BallDiameter = 0.4f;

    /// <summary>Ball centre height so it sits on the floor (test seam).</summary>
    public const float BallRestY = 0.2f;

    /// <summary>Ground icon world scale (large enough for thin glyphs).</summary>
    public const float IconScale = 0.6f;

    /// <summary>Ground glow pool diameter in metres (test seam).</summary>
    public const float GlowDiameter = 1.0f;

    /// <summary>Icon ride height above the drop root (test seam).</summary>
    public const float IconRideY = 0.62f;

    /// <summary>Glow pool opacity (test seam).</summary>
    public const float GlowAlpha = 0.5f;

    /// <summary>Rarity glow color (test seam; host == client == late join).</summary>
    public static Color GlowColor(ItemRarity rarity) => DiabloPalette.Rarity(rarity);

    /// <summary>Sprite key identity for host/client parity tests.</summary>
    public static string ArtKey(ItemInstance it) => DiabloMenuArt.ArtKeyFor(it);

    /// <summary>Attach-or-reuse the view on a drop root and show the item.</summary>
    public static LootDropView ShowOn(GameObject root, ItemInstance rolled)
    {
        if (root == null) return null;
        var view = root.GetComponent<LootDropView>() ?? root.AddComponent<LootDropView>();
        view.Show(rolled);
        return view;
    }

    public void Show(ItemInstance rolled)
    {
        item = rolled;
        if (item == null) return;
        // Remove legacy visuals (cube, billboarded dark panel) if present.
        foreach (var legacyName in new[] { "LootVisual", "RarityGlow" })
        {
            var legacy = transform.Find(legacyName);
            if (legacy != null) DestroyImmediate(legacy.gameObject);
            var ballLegacy = transform.Find("LootBall");
            if (ballLegacy != null)
            {
                var nested = ballLegacy.Find(legacyName);
                if (nested != null) DestroyImmediate(nested.gameObject);
            }
        }

        // Unscaled group: children keep world scale == local scale, so the
        // art is sized exactly as designed (no inherited shrink).
        var ball = transform.Find("LootBall");
        if (ball == null)
        {
            ball = new GameObject("LootBall").transform;
            ball.SetParent(transform, false);
        }
        ball.localPosition = Vector3.zero;
        ball.localScale = Vector3.one;
        ball.localRotation = Quaternion.identity;

        EnsureBallMesh(ball, item.rarity);
        EnsureGroundGlow(ball, item.rarity);

        var iconGo = ball.Find("ItemIcon");
        if (iconGo == null)
        {
            iconGo = new GameObject("ItemIcon").transform;
            iconGo.SetParent(ball, false);
        }
        iconGo.localPosition = new Vector3(0f, IconRideY, 0f);
        var icon = iconGo.GetComponent<SpriteRenderer>();
        if (icon == null) icon = iconGo.gameObject.AddComponent<SpriteRenderer>();
        icon.sprite = DiabloMenuArt.ItemIcon(item);
        icon.color = Color.white;
        iconGo.localScale = Vector3.one * IconScale;
        iconGo.localRotation = Quaternion.identity;
        // Gentle bob so the drop reads as loot, not scenery.
        var bob = GetComponent<LootBob>() ?? gameObject.AddComponent<LootBob>();
        bob.SetTarget(iconGo, IconRideY);
        cachedBall = ball;
        cachedIcon = iconGo;
    }

    Transform cachedBall;
    Transform cachedIcon;
    Camera cachedCam;

    void Awake()
    {
        CacheChildren();
    }

    void CacheChildren()
    {
        cachedBall = transform.Find("LootBall");
        cachedIcon = cachedBall != null ? cachedBall.Find("ItemIcon") : null;
    }

    Camera ResolveCamera()
    {
        if (cachedCam == null || !cachedCam.isActiveAndEnabled)
            cachedCam = Camera.main;
        return cachedCam;
    }

    /// <summary>Billboard the icon at the camera (readable at any angle).</summary>
    void LateUpdate()
    {
        if (cachedIcon == null || !cachedIcon.gameObject.activeInHierarchy) return;
        var cam = ResolveCamera();
        if (cam == null) return;
        cachedIcon.rotation = cam.transform.rotation;
    }

    /// <summary>Emissive rarity sphere; the drop's anchor and color signal.</summary>
    static void EnsureBallMesh(Transform ball, ItemRarity rarity)
    {
        var mesh = ball.Find("BallMesh");
        if (mesh == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "BallMesh";
            var col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            mesh = go.transform;
            mesh.SetParent(ball, false);
        }
        mesh.localPosition = new Vector3(0f, BallRestY, 0f);
        mesh.localScale = Vector3.one * BallDiameter;
        mesh.localRotation = Quaternion.identity;
        var rend = mesh.GetComponent<Renderer>();
        if (rend == null) return;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null) return;
        var mat = rend.sharedMaterial;
        if (mat == null || !mat.name.StartsWith("LootBall"))
        {
            mat = new Material(shader);
            mat.name = "LootBallRarity";
            rend.sharedMaterial = mat;
        }
        var tint = DiabloPalette.Rarity(rarity);
        mat.color = tint;
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", tint * 0.45f);
        }
    }

    /// <summary>Soft rarity pool lying flat under the ball (never billboarded).</summary>
    static void EnsureGroundGlow(Transform ball, ItemRarity rarity)
    {
        var glowGo = ball.Find("GroundGlow");
        if (glowGo == null)
        {
            glowGo = new GameObject("GroundGlow").transform;
            glowGo.SetParent(ball, false);
        }
        glowGo.localPosition = new Vector3(0f, 0.03f, 0f);
        glowGo.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        glowGo.localScale = Vector3.one * GlowDiameter;
        var glow = glowGo.GetComponent<SpriteRenderer>();
        if (glow == null) glow = glowGo.gameObject.AddComponent<SpriteRenderer>();
        glow.sprite = GlowSprite();
        var tint = DiabloPalette.Rarity(rarity);
        glow.color = new Color(tint.r, tint.g, tint.b, GlowAlpha);
    }

    static Sprite glowSprite;

    /// <summary>Soft radial pool sprite (procedural, headless-safe).</summary>
    internal static Sprite GlowSprite()
    {
        if (glowSprite != null) return glowSprite;
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float c = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - c) / c, dy = (y - c) / c;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(1f - r);
                a *= a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return glowSprite;
    }
}

/// <summary>Idle bob for the ground icon (visual only, no gameplay).</summary>
public sealed class LootBob : MonoBehaviour
{
    public Transform target;
    float baseY = LootDropView.IconRideY;
    float amplitude = 0.05f;
    float t;

    /// <summary>Attach the bob to a target with its ride height.</summary>
    public void SetTarget(Transform next, float rideY, float bobAmplitude = 0.05f)
    {
        target = next;
        baseY = rideY;
        amplitude = bobAmplitude;
        t = 0f;
    }

    void Update()
    {
        if (target == null) return;
        t += Time.deltaTime;
        var p = target.localPosition;
        p.y = baseY + Mathf.Sin(t * 2f) * amplitude;
        target.localPosition = p;
    }
}
