using UnityEngine;

/// <summary>
/// The body a husk leaves behind (D2 corpses). A stripped visual clone of the
/// dying husk: renderers + Animator only — no collider, no tag, no gameplay
/// components — so arc targeting, aggro and kill asserts never see it. Plays
/// the Death take (primitives tip over instead), lies for
/// <see cref="LieSeconds"/>, sinks into the ground and destroys itself.
/// Local-only: co-op clients keep the existing despawn path.
/// </summary>
public sealed class DiabloCorpse : MonoBehaviour
{
    public const float LieSeconds = 8f;
    public const float SinkSeconds = 2f;
    public const float SinkDepth = 0.8f;

    public static int Spawned;

    float age;
    Vector3 restPos;

    /// <summary>
    /// Leave a body for this husk. Prefers a fresh instance of the husk's
    /// source model (<see cref="EnemyHealth.corpseModel"/>: no netcode
    /// components, safe on a solo host); falls back to an inactive clone for
    /// model-less husks offline. Null on dedicated servers and clients.
    /// </summary>
    public static GameObject SpawnFrom(GameObject husk)
    {
        if (husk == null || !Diablo.IsLocalAuthority()) return null;
        var health = husk.GetComponent<EnemyHealth>();
        GameObject corpse;
        if (health != null && health.corpseModel != null)
        {
            corpse = Object.Instantiate(health.corpseModel, husk.transform.position, husk.transform.rotation);
            corpse.SetActive(false);
            corpse.transform.localScale = husk.transform.localScale;
            CopyLook(husk, corpse);
        }
        else
        {
            if (Diablo.NetcodeListening()) return null; // never clone a spawned NetworkObject
            bool wasActive = husk.activeSelf;
            // Clone inactive so none of the gameplay components ever Awake on the copy.
            husk.SetActive(false);
            corpse = Object.Instantiate(husk, husk.transform.position, husk.transform.rotation);
            husk.SetActive(wasActive);
        }
        corpse.name = "Corpse_" + husk.name;
        corpse.tag = "Untagged";
        Strip(corpse);
        int ignore = LayerMask.NameToLayer("Ignore Raycast");
        foreach (var t in corpse.GetComponentsInChildren<Transform>(true))
            t.gameObject.layer = ignore;
        corpse.AddComponent<DiabloCorpse>();
        corpse.SetActive(true);
        Spawned++;
        return corpse;
    }

    /// <summary>Carry the husk's controller and (tinted) materials onto the fresh model.</summary>
    static void CopyLook(GameObject husk, GameObject corpse)
    {
        var src = husk.GetComponent<Animator>();
        if (src != null && src.runtimeAnimatorController != null)
        {
            var dst = corpse.GetComponent<Animator>();
            if (dst == null) dst = corpse.AddComponent<Animator>();
            dst.runtimeAnimatorController = src.runtimeAnimatorController;
            dst.applyRootMotion = false;
        }
        var from = husk.GetComponentsInChildren<Renderer>(true);
        var to = corpse.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < from.Length && i < to.Length; i++)
            to[i].sharedMaterials = from[i].sharedMaterials;
    }

    static void Strip(GameObject corpse)
    {
        // Netcode behaviours before NetworkObject, then everything else.
        foreach (var b in corpse.GetComponentsInChildren<MonoBehaviour>(true))
            if (!(b is Unity.Netcode.NetworkObject)) Object.DestroyImmediate(b);
        foreach (var b in corpse.GetComponentsInChildren<MonoBehaviour>(true))
            Object.DestroyImmediate(b);
        foreach (var c in corpse.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(c);
        foreach (var r in corpse.GetComponentsInChildren<Rigidbody>(true))
            Object.DestroyImmediate(r);
    }

    void Start()
    {
        restPos = transform.position;
        var animator = GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null
            && animator.HasState(0, Animator.StringToHash(HuskAnimator.DeathState)))
        {
            animator.Play(HuskAnimator.DeathState, 0, 0f);
        }
        else
        {
            if (animator != null) animator.enabled = false;
            transform.rotation = Quaternion.Euler(-90f, transform.eulerAngles.y, 0f);
            restPos.y = Mathf.Max(0.3f, restPos.y - 0.6f);
            transform.position = restPos;
        }
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age <= LieSeconds) return;
        float t = (age - LieSeconds) / SinkSeconds;
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }
        transform.position = restPos + Vector3.down * (SinkDepth * t);
    }

    /// <summary>Seconds lived (test seam).</summary>
    public float Age => age;

    /// <summary>Fast-forward the clock (test seam, avoids 10 s waits).</summary>
    public void Advance(float seconds) => age += seconds;

    public static void ResetRun() => Spawned = 0;
}
