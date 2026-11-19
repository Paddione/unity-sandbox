using UnityEngine;

/// <summary>
/// S5 ground presentation for one <see cref="ItemInstance"/>: a small rarity
/// ball on the floor carries the item's own sprite plus the rarity glow
/// (Magic blue, Rare gold, Unique orange — <see cref="DiabloPalette"/>).
/// Derived purely from the S2 snapshot, so host, client and late join render
/// identically.
/// </summary>
public sealed class LootDropView : MonoBehaviour
{
    public ItemInstance item;

    /// <summary>Ball diameter in metres (test seam).</summary>
    public const float BallDiameter = 0.35f;

    /// <summary>Ball rest height so it sits on the floor (test seam).</summary>
    public const float BallRestY = 0.18f;

    /// <summary>Ground icon scale (sized down to sit on the ball).</summary>
    public const float IconScale = 0.55f;

    /// <summary>Rarity glow scale (sized down to hug the ball).</summary>
    public const float GlowScale = 0.5f;

    /// <summary>Icon ride height above the ball centre (test seam).</summary>
    public const float IconRideY = 0.32f;

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
        // Remove the legacy cube visual if present.
        var legacy = transform.Find("LootVisual");
        if (legacy != null) DestroyImmediate(legacy.gameObject);

        var ball = EnsureBall(item.rarity);

        // Migrate pre-ball drops: art created as root children moves under the ball.
        var glowGo = FindDeep("RarityGlow");
        if (glowGo == null)
        {
            glowGo = new GameObject("RarityGlow").transform;
        }
        glowGo.SetParent(ball, false);
        glowGo.localPosition = Vector3.zero;
        var glow = glowGo.GetComponent<SpriteRenderer>();
        if (glow == null) glow = glowGo.gameObject.AddComponent<SpriteRenderer>();
        glow.sprite = DiabloMenuArt.SlotBackground(item.rarity, 96);
        glow.color = new Color(1f, 1f, 1f, 0.9f);
        glowGo.localScale = Vector3.one * GlowScale;

        var iconGo = FindDeep("ItemIcon");
        if (iconGo == null)
        {
            iconGo = new GameObject("ItemIcon").transform;
        }
        iconGo.SetParent(ball, false);
        iconGo.localPosition = new Vector3(0f, IconRideY, 0f);
        var icon = iconGo.GetComponent<SpriteRenderer>();
        if (icon == null) icon = iconGo.gameObject.AddComponent<SpriteRenderer>();
        icon.sprite = DiabloMenuArt.ItemIcon(item);
        iconGo.localScale = Vector3.one * IconScale;
        // Gentle bob so the drop reads as loot, not scenery.
        var bob = GetComponent<LootBob>() ?? gameObject.AddComponent<LootBob>();
        bob.SetTarget(iconGo, IconRideY);
    }

    /// <summary>Billboard both sprites at the camera (readable at any angle).</summary>
    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var rot = cam.transform.rotation;
        var glow = FindDeep("RarityGlow");
        if (glow != null) glow.rotation = rot;
        var icon = FindDeep("ItemIcon");
        if (icon != null) icon.rotation = rot;
    }

    /// <summary>Find a view child at any depth (art rides on the ball).</summary>
    Transform FindDeep(string name)
    {
        var direct = transform.Find(name);
        if (direct != null) return direct;
        var ball = transform.Find("LootBall");
        if (ball != null)
        {
            var nested = ball.Find(name);
            if (nested != null) return nested;
        }
        return null;
    }

    /// <summary>Ensure the loot ball exists, tinted by rarity.</summary>
    Transform EnsureBall(ItemRarity rarity)
    {
        var ball = transform.Find("LootBall");
        if (ball == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "LootBall";
            var col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            ball = go.transform;
            ball.SetParent(transform, false);
        }
        ball.localPosition = new Vector3(0f, BallRestY, 0f);
        ball.localScale = Vector3.one * BallDiameter;
        ball.localRotation = Quaternion.identity;
        var rend = ball.GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = rend.sharedMaterial;
            if (mat == null || !mat.name.StartsWith("LootBall"))
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Standard");
                if (shader != null)
                {
                    mat = new Material(shader);
                    mat.name = "LootBallRarity";
                    rend.sharedMaterial = mat;
                }
            }
            if (mat != null && mat.name.StartsWith("LootBall"))
                mat.color = DiabloPalette.Rarity(rarity);
        }
        return ball;
    }
}

/// <summary>Idle bob for the ground icon (visual only, no gameplay).</summary>
public sealed class LootBob : MonoBehaviour
{
    public Transform target;
    float baseY = LootDropView.IconRideY;
    float amplitude = 0.03f;
    float t;

    /// <summary>Attach the bob to a target with its ride height.</summary>
    public void SetTarget(Transform next, float rideY, float bobAmplitude = 0.03f)
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
