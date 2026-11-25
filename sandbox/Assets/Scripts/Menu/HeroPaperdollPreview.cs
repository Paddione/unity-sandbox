using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Live 3D hero in the inventory (paperdoll). A visual clone of the owning
/// hero stands on a stage far outside the world; a dedicated camera renders
/// it into a RenderTexture shown by this RawImage. The clone mirrors the
/// hero's live <see cref="Equipment"/> through its own
/// <see cref="HeroEquipmentAppearance"/>, so equipping shows on the model
/// at once. Dragging on the image turns the hero. Rig and camera exist only
/// while the inventory is open (<see cref="Show"/>/<see cref="Hide"/>).
/// </summary>
[RequireComponent(typeof(RawImage))]
public sealed class HeroPaperdollPreview : MonoBehaviour, IDragHandler
{
    public const string RigName = "HeroPaperdollRig";

    /// <summary>Own stage, apart from the creation preview's (HeroPreviewView.StagePos).</summary>
    public static readonly Vector3 StagePos = new Vector3(-5000f, -60f, 5000f);

    const int TexWidth = 512;
    const int TexHeight = 640;
    const float DragDegreesPerPixel = 0.45f;
    // Faces the camera (which looks along +Z) with a slight three-quarter turn.
    const float RestYaw = 160f;

    RawImage image;
    RenderTexture rt;
    Camera cam;
    GameObject rig;
    GameObject hero;
    float yaw = RestYaw;

    public bool IsLive => rig != null && rig.activeSelf;
    public GameObject Rig => rig;
    public RenderTexture Texture => rt;

    public void Bind(GameObject owner) => hero = owner;

    static bool Headless => SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;

    /// <summary>Spawn (or wake) the rig and camera. No-op outside play mode.</summary>
    public void Show()
    {
        if (!Application.isPlaying || hero == null) return;
        if (image == null) image = GetComponent<RawImage>();
        if (rig == null && !BuildRig()) { image.enabled = false; return; }
        rig.SetActive(true);
        yaw = RestYaw;
        rig.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        EnsureCamera();
        // Headless runs (-nographics) have no device to render with; the rig still mirrors gear.
        cam.enabled = !Headless;
        image.texture = rt;
        image.enabled = true;
    }

    /// <summary>Park the rig and stop rendering (kept for the next open).</summary>
    public void Hide()
    {
        if (rig != null) rig.SetActive(false);
        if (cam != null) cam.enabled = false;
    }

    public void OnDrag(PointerEventData e)
    {
        if (rig == null) return;
        yaw -= e.delta.x * DragDegreesPerPixel;
        rig.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    bool BuildRig()
    {
        rig = CompanionFactory.CloneHeroVisual(hero);
        if (rig == null) return false;
        rig.name = RigName;
        rig.transform.SetParent(null, false);
        rig.transform.position = StagePos;
        rig.transform.localScale = Vector3.one;
        var look = rig.GetComponent<ClassAppearance>();
        if (look != null)
        {
            look.autoApply = false;
            look.Apply(RunProfile.Class, RunProfile.Hero);
            var gear = rig.AddComponent<HeroEquipmentAppearance>();
            if (!gear.Initialize(look, hero.GetComponent<Equipment>())) Destroy(gear);
        }
        return true;
    }

    void EnsureCamera()
    {
        if (rt == null)
        {
            // MSAA only on a real device: headless (-nographics) runs cannot create it.
            int aa = Headless ? 1 : 2;
            rt = new RenderTexture(TexWidth, TexHeight, 16, RenderTextureFormat.ARGB32) { name = "HeroPaperdollRT", antiAliasing = aa };
        }
        if (cam != null) return;
        var camGo = new GameObject("HeroPaperdollCamera");
        cam = camGo.AddComponent<Camera>();
        cam.enabled = false;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.cullingMask = -1;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 12f;
        cam.fieldOfView = 28f;
        cam.transform.position = StagePos + new Vector3(0f, 1.05f, -4.1f);
        cam.transform.LookAt(StagePos + new Vector3(0f, 0.92f, 0f));
        cam.targetTexture = rt;
        // Rim light so the model reads against the dark panel regardless of the zone's sun.
        var lightGo = new GameObject("HeroPaperdollLight");
        lightGo.transform.SetParent(camGo.transform, false);
        lightGo.transform.position = StagePos + new Vector3(-1.6f, 2.6f, -2.2f);
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 7f;
        l.intensity = 2.2f;
        l.color = new Color(1f, 0.86f, 0.66f);
    }

    void OnDestroy()
    {
        if (rig != null) Destroy(rig);
        if (cam != null) Destroy(cam.gameObject);
        if (rt != null)
        {
            if (image != null && image.texture == rt) image.texture = null;
            Destroy(rt);
        }
    }
}
