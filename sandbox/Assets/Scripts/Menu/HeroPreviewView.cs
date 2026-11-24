using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Embedded 3D creation preview (Vorschau, cinderreach-companion-rendering):
/// a small turntable stage inside the customize modal. A dedicated camera
/// renders a visual hero clone into a RenderTexture shown in a RawImage.
/// The stage sits far outside the world (no scene camera ever sees it) and
/// needs no extra layer. The rig follows the draft (class, gender, hair,
/// build) through <see cref="Show"/> and is destroyed on <see cref="Hide"/>.
/// </summary>
public sealed class HeroPreviewView : MonoBehaviour
{
    /// <summary>Name of the preview rig (test seam).</summary>
    public const string PreviewObjectName = "HeroPreviewRig";

    /// <summary>Stage far outside the world (main cameras never reach it).</summary>
    public static readonly Vector3 StagePos = new Vector3(5000f, -60f, 5000f);

    /// <summary>Turntable speed, degrees per second.</summary>
    public const float TurntableSpeed = 24f;

    const int TextureSize = 384;

    RawImage image;
    RenderTexture rt;
    Camera cam;
    GameObject rig;
    ClassAppearance look;

    /// <summary>True while a rig stands on the stage.</summary>
    public bool IsLive => rig != null;

    /// <summary>The preview rig (null when hidden; test seam).</summary>
    public GameObject Rig => rig;

    /// <summary>Build the preview box (label + image) into the customize modal.</summary>
    public static HeroPreviewView BuildInto(Transform modal)
    {
        var box = new GameObject("HeroPreviewBox", typeof(RectTransform));
        box.transform.SetParent(modal, false);
        DiabloUIFactory.SetLayout(box, prefH: 240f, flexW: 1f);
        var label = DiabloUIFactory.MakeText(box.transform, "PreviewLabel",
            Loc.T("menu.create.preview"), 14, DiabloPalette.ParchmentDim, TextAnchor.MiddleCenter);
        DiabloUIFactory.SetLayout(label.gameObject, prefH: 20f, flexW: 1f);
        var frame = new GameObject("PreviewFrame", typeof(RectTransform));
        frame.transform.SetParent(box.transform, false);
        DiabloUIFactory.SetLayout(frame, prefH: 212f, flexW: 1f);
        var img = new GameObject("PreviewImage", typeof(RectTransform));
        img.transform.SetParent(frame.transform, false);
        var raw = img.AddComponent<RawImage>();
        raw.raycastTarget = false;
        var rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var view = box.AddComponent<HeroPreviewView>();
        view.image = raw;
        return view;
    }

    /// <summary>Show the draft on the stage (creates rig, camera and texture on demand).</summary>
    public void Show(ClassDefinition cls, HeroProfile profile)
    {
        if (cls == null) cls = ClassCatalog.Fenwalker;
        if (rig == null)
        {
            rig = CompanionFactory.CloneHeroVisual();
            if (rig == null) { SetImageVisible(false); return; }
            rig.name = PreviewObjectName;
            rig.transform.position = StagePos;
            rig.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            look = rig.GetComponent<ClassAppearance>();
            if (look != null) look.autoApply = false;
            EnsureCamera();
        }
        if (look != null) look.Apply(cls, profile);
        else
        {
            look = rig.GetComponent<ClassAppearance>();
            if (look != null) { look.autoApply = false; look.Apply(cls, profile); }
        }
        if (rt != null && image != null)
        {
            image.texture = rt;
            SetImageVisible(true);
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

    void Update()
    {
        if (rig != null) rig.transform.Rotate(0f, TurntableSpeed * Time.deltaTime, 0f);
    }

    void EnsureCamera()
    {
        if (rt == null)
        {
            rt = new RenderTexture(TextureSize, TextureSize, 16, RenderTextureFormat.ARGB32);
            rt.name = "HeroPreviewRT";
        }
        if (cam == null)
        {
            var camGo = new GameObject("HeroPreviewCamera");
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.cullingMask = -1;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 10f;
            cam.fieldOfView = 30f;
            cam.enabled = true;
            cam.transform.position = StagePos + new Vector3(0f, 1.35f, -2.6f);
            cam.transform.LookAt(StagePos + new Vector3(0f, 0.95f, 0f));
            cam.targetTexture = rt;
        }
    }

    void SetImageVisible(bool visible)
    {
        if (image != null && image.gameObject.activeSelf != visible)
            image.gameObject.SetActive(visible);
    }
}
