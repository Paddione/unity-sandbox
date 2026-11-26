using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Embedded 3D hero preview (Vorschau, cinderreach-companion-rendering): a
/// turntable stage inside the creation step and the class cards. A dedicated
/// camera renders a visual hero clone into a portrait RenderTexture shown in a
/// RawImage that keeps the texture's aspect (no stretching). The camera frames
/// the whole figure from its renderer bounds, so feet, head and hair always
/// fit. Every view gets its own stage far outside the world (no scene camera
/// ever sees it, stages never see each other). The rig follows the draft
/// (class, gender, hair, build) through <see cref="Show"/> and is destroyed on
/// <see cref="Hide"/>.
/// </summary>
public sealed class HeroPreviewView : MonoBehaviour
{
    /// <summary>Name of the preview rig (test seam).</summary>
    public const string PreviewObjectName = "HeroPreviewRig";

    /// <summary>Name of a class card figure rig (test seam).</summary>
    public const string CardObjectName = "HeroCardRig";

    /// <summary>First stage, far outside the world (main cameras never reach it).</summary>
    public static readonly Vector3 StagePos = new Vector3(5000f, -60f, 5000f);

    /// <summary>Distance between two stages (beyond every preview camera's far plane).</summary>
    public const float StageSpacing = 40f;

    /// <summary>Turntable sway: the figure turns ±<see cref="SwayDegrees"/> around facing the camera.</summary>
    public const float SwayDegrees = 25f;
    public const float SwaySpeed = 0.6f;
    /// <summary>Yaw that faces the preview camera (KayKit heroes look along +z, the camera sits at -z).</summary>
    public const float FacingYaw = 180f;
    /// <summary>Fixed three-quarter turn of the card figures.</summary>
    public const float CardYaw = 200f;

    /// <summary>Portrait texture: a standing figure fills it without stretching.</summary>
    public const int TextureWidth = 384;
    public const int TextureHeight = 512;
    public const float TextureAspect = (float)TextureWidth / TextureHeight;

    /// <summary>Vertical field of view of the preview camera.</summary>
    public const float FieldOfView = 30f;

    /// <summary>Free space around the figure (fraction of its size).</summary>
    public const float FrameMargin = 0.08f;

    static int nextStage;

    RawImage image;
    RenderTexture rt;
    Camera cam;
    GameObject rig;
    ClassAppearance look;
    Vector3 stage;
    bool stageAssigned;
    int reframeFrames;
    float swayTime;
    Light keyLight;

    /// <summary>Rotate the rig (creation step) or keep a fixed three-quarter view (cards).</summary>
    public bool turntable = true;

    /// <summary>Name the rig gets on the stage.</summary>
    public string rigName = PreviewObjectName;

    /// <summary>Optional 2D stand-in shown while no rig stands (card portrait art).</summary>
    public Graphic fallback;

    /// <summary>True while a rig stands on the stage.</summary>
    public bool IsLive => rig != null;

    /// <summary>The preview rig (null when hidden; test seam).</summary>
    public GameObject Rig => rig;

    /// <summary>The preview camera (null when hidden; test seam).</summary>
    public Camera PreviewCamera => cam;

    /// <summary>The image showing the render (test seam).</summary>
    public RawImage Image => image;

    /// <summary>Build the creation preview box (label + portrait image) into a layout parent.</summary>
    public static HeroPreviewView BuildInto(Transform parent, float height = 240f, float width = -1f)
    {
        var box = new GameObject("HeroPreviewBox", typeof(RectTransform));
        box.transform.SetParent(parent, false);
        DiabloUIFactory.SetLayout(box, prefH: height, prefW: width, flexW: width > 0f ? -1f : 1f);
        var v = box.AddComponent<VerticalLayoutGroup>();
        v.spacing = 4f;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        var label = DiabloUIFactory.MakeText(box.transform, "PreviewLabel",
            Loc.T("menu.create.preview"), 14, DiabloPalette.ParchmentDim, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(label.gameObject, prefH: 20f, flexW: 1f);
        var frame = new GameObject("PreviewFrame", typeof(RectTransform));
        frame.transform.SetParent(box.transform, false);
        DiabloUIFactory.SetLayout(frame, prefH: Mathf.Max(40f, height - 24f), flexW: 1f, flexH: 1f);
        var view = box.AddComponent<HeroPreviewView>();
        view.image = MakeImage(frame.transform);
        return view;
    }

    /// <summary>Attach a preview that fills an existing frame (class card portrait).</summary>
    public static HeroPreviewView AttachTo(RectTransform frame, Graphic fallback, bool turntable)
    {
        var view = frame.gameObject.AddComponent<HeroPreviewView>();
        view.image = MakeImage(frame);
        view.fallback = fallback;
        view.turntable = turntable;
        view.rigName = CardObjectName;
        view.SetImageVisible(false);
        return view;
    }

    static RawImage MakeImage(Transform frame)
    {
        var img = new GameObject("PreviewImage", typeof(RectTransform));
        img.transform.SetParent(frame, false);
        var raw = img.AddComponent<RawImage>();
        raw.raycastTarget = false;
        var r = img.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        // Keep the portrait aspect: a stretched texture squashed the figure.
        var fit = img.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = TextureAspect;
        return raw;
    }

    /// <summary>Show the draft on the stage (creates rig, camera and texture on demand).</summary>
    public void Show(ClassDefinition cls, HeroProfile profile)
    {
        if (cls == null) cls = ClassCatalog.Fenwalker;
        if (!stageAssigned)
        {
            stage = StagePos + new Vector3(StageSpacing * (nextStage++ % 64), 0f, 0f);
            stageAssigned = true;
        }
        if (rig == null)
        {
            rig = CompanionFactory.CloneHeroVisual();
            if (rig == null) { SetImageVisible(false); return; }
            rig.name = rigName;
            StripWornGear(rig);
            rig.transform.position = stage;
            rig.transform.rotation = Quaternion.Euler(0f, turntable ? FacingYaw : CardYaw, 0f);
            swayTime = 0f;
            look = rig.GetComponent<ClassAppearance>();
            if (look != null) look.autoApply = false;
            EnsureCamera();
        }
        if (look == null)
        {
            look = rig.GetComponent<ClassAppearance>();
            if (look != null) look.autoApply = false;
        }
        if (look != null) look.Apply(cls, profile);
        if (turntable)
        {
            // Measure facing the camera; the sweep then covers the whole sway.
            swayTime = 0f;
            rig.transform.rotation = Quaternion.Euler(0f, FacingYaw, 0f);
        }
        Reframe();
        reframeFrames = 3; // new skin and hair settle their bounds over the next frames
        if (cam != null && !cam.enabled && isActiveAndEnabled) cam.enabled = true;
        if (rt != null && image != null)
        {
            image.texture = rt;
            SetImageVisible(true);
        }
    }

    /// <summary>
    /// A new hero starts without the current hero's worn gear: drop the
    /// mounted item models the clone copied (the class look stays).
    /// </summary>
    public static void StripWornGear(GameObject clone)
    {
        if (clone == null) return;
        var doomed = new System.Collections.Generic.List<GameObject>();
        foreach (var t in clone.GetComponentsInChildren<Transform>(true))
            if (System.Array.IndexOf(HeroEquipmentAppearance.MountNames, t.name) >= 0)
                doomed.Add(t.gameObject);
        foreach (var d in doomed)
        {
            d.SetActive(false);
            if (Application.isPlaying) Destroy(d); else DestroyImmediate(d);
        }
    }

    /// <summary>Remove the rig, camera and texture.</summary>
    public void Hide()
    {
        if (rig != null) { if (Application.isPlaying) Destroy(rig); else DestroyImmediate(rig); rig = null; }
        look = null;
        if (cam != null)
        {
            var camGo = cam.gameObject;
            if (Application.isPlaying) Destroy(camGo); else DestroyImmediate(camGo);
            cam = null;
        }
        if (rt != null)
        {
            if (image != null && image.texture == rt) image.texture = null;
            if (Application.isPlaying) Destroy(rt); else DestroyImmediate(rt);
            rt = null;
        }
        SetImageVisible(false);
    }

    void OnDestroy() => Hide();

    // A hidden card or modal stops rendering its stage.
    void OnDisable() { if (cam != null) cam.enabled = false; }
    void OnEnable() { if (cam != null) cam.enabled = true; }

    void Update()
    {
        if (rig == null || !turntable) return;
        swayTime += Time.unscaledDeltaTime;
        rig.transform.rotation = Quaternion.Euler(0f, FacingYaw + SwayDegrees * Mathf.Sin(swayTime * SwaySpeed), 0f);
    }

    void LateUpdate()
    {
        if (reframeFrames <= 0 || rig == null) return;
        reframeFrames--;
        Reframe();
    }

    /// <summary>No graphics device (batchmode -nographics): RenderTextures cannot be created.</summary>
    public static bool Headless => SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;

    void EnsureCamera()
    {
        if (rt == null && !Headless)
        {
            rt = new RenderTexture(TextureWidth, TextureHeight, 16, RenderTextureFormat.ARGB32);
            rt.name = "HeroPreviewRT";
        }
        if (cam == null)
        {
            var camGo = new GameObject("HeroPreviewCamera");
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.cullingMask = Headless ? 0 : -1; // headless: nothing to draw into
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 20f;
            cam.fieldOfView = FieldOfView;
            cam.aspect = TextureAspect;
            cam.enabled = isActiveAndEnabled;
            cam.targetTexture = rt;
        }
        if (keyLight == null)
        {
            // Stage key light: the world light alone left hair colours murky. Its range
            // stays inside the stage, far from the world and from the other stages.
            var lightGo = new GameObject("HeroPreviewKeyLight");
            lightGo.transform.SetParent(cam.transform, false);
            lightGo.transform.localPosition = new Vector3(0.8f, 1.2f, 0f);
            keyLight = lightGo.AddComponent<Light>();
            keyLight.type = LightType.Point;
            keyLight.color = new Color(1f, 0.93f, 0.82f);
            keyLight.intensity = 2.2f;
            keyLight.shadows = LightShadows.None;
        }
    }

    /// <summary>Point the camera so the whole rig fits the frame.</summary>
    void Reframe()
    {
        if (rig == null || cam == null) return;
        if (!TryFigureBounds(rig, out var bounds)) bounds = new Bounds(stage + Vector3.up * 1.2f, new Vector3(1.6f, 2.4f, 1.6f));
        if (turntable) bounds = SweptBounds(bounds, rig.transform.position, SwayDegrees);
        FrameFigure(bounds, FieldOfView, TextureAspect, out var pos, out var target);
        cam.transform.position = pos;
        cam.transform.LookAt(target);
        float reach = Vector3.Distance(pos, target);
        cam.farClipPlane = Mathf.Min(StageSpacing * 0.5f, Mathf.Max(10f, reach * 2.5f));
        if (keyLight != null) keyLight.range = reach * 1.6f;
    }

    /// <summary>World bounds of the visible meshes of a rig (lights and particles excluded).</summary>
    public static bool TryFigureBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer)) continue;
            if (!any) { bounds = r.bounds; any = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return any;
    }

    /// <summary>
    /// Bounds the figure sweeps while it turns by up to ±<paramref name="swayDegrees"/>
    /// around <paramref name="pivot"/> (pure; the figure was measured facing the camera).
    /// </summary>
    public static Bounds SweptBounds(Bounds figure, Vector3 pivot, float swayDegrees)
    {
        var swept = figure;
        if (swayDegrees <= 0f) return swept;
        int steps = Mathf.Max(2, Mathf.CeilToInt(swayDegrees / 5f));
        for (int s = -steps; s <= steps; s++)
        {
            var rot = Quaternion.Euler(0f, swayDegrees * s / steps, 0f);
            for (int i = 0; i < 8; i++)
            {
                var c = new Vector3((i & 1) == 0 ? figure.min.x : figure.max.x,
                    (i & 2) == 0 ? figure.min.y : figure.max.y, (i & 4) == 0 ? figure.min.z : figure.max.z);
                swept.Encapsulate(pivot + rot * (c - pivot));
            }
        }
        return swept;
    }

    /// <summary>
    /// Camera pose that fits a standing figure seen from -z (pure, EditMode-pinnable):
    /// the height spans the vertical field of view and the width the horizontal
    /// one, both with <see cref="FrameMargin"/>. The camera looks slightly down
    /// at the figure centre from in front of its nearest face.
    /// </summary>
    public static void FrameFigure(Bounds figure, float verticalFov, float aspect,
        out Vector3 cameraPos, out Vector3 lookAt)
    {
        float halfHeight = figure.extents.y * (1f + FrameMargin);
        float halfWidth = Mathf.Max(0.2f, figure.extents.x) * (1f + FrameMargin);
        float tanV = Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad);
        float tanH = tanV * Mathf.Max(0.1f, aspect);
        // Perspective: the front half of the figure sits closer than the centre.
        float distance = Mathf.Max(halfHeight / tanV, halfWidth / tanH) + figure.extents.z * 0.75f;
        lookAt = figure.center;
        var dir = new Vector3(0f, 0.12f, -1f).normalized;
        cameraPos = lookAt + dir * distance;
    }

    void SetImageVisible(bool visible)
    {
        if (image != null && image.gameObject.activeSelf != visible)
            image.gameObject.SetActive(visible);
        if (fallback != null) fallback.enabled = !visible;
    }
}
