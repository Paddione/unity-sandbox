using UnityEngine;

/// <summary>
/// Local-art pilot: faces the loot-icon sprite toward the game camera and
/// bobs it gently above the pilot loot chest. No dependencies, headless-safe
/// (null-guarded, refuses to throw in EditMode).
/// </summary>
public sealed class LootIconBillboard : MonoBehaviour
{
    public float bobAmplitude = 0.08f;
    public float bobFrequency = 1.6f;

    Vector3 baseLocalPos;
    Transform cam;

    void Start()
    {
        baseLocalPos = transform.localPosition;
        var c = Camera.main;
        cam = c != null ? c.transform : null;
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            var c = Camera.main;
            if (c == null) return;
            cam = c.transform;
        }
        transform.rotation = Quaternion.LookRotation(
            transform.position - cam.position, Vector3.up);
        transform.localPosition = baseLocalPos +
            Vector3.up * Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
    }
}
