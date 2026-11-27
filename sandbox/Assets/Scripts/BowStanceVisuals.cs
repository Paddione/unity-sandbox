using UnityEngine;

/// <summary>
/// Held weapon swap for the hero (bow while shooting, sword otherwise),
/// extracted from <see cref="DiabloSkills"/> (cinderreach-skills-modularization S2).
/// Plain class owned by DiabloSkills, which keeps the public facade
/// (BowDrawn, EnterBowStance, ExitBowStance, HandMountRotation, LongAxis).
/// Pure visuals: no combat, cooldown or network state lives here.
/// </summary>
public sealed class BowStanceVisuals
{
    readonly Transform hero;

    float bowUntil = float.NegativeInfinity;
    GameObject heldBow;
    Renderer[] bowRenderers;
    bool showBowModel = true;
    Renderer[] swordRenderers;

    public BowStanceVisuals(Transform hero) { this.hero = hero; }

    /// <summary>True while the hero holds the bow instead of the sword.</summary>
    public bool BowDrawn => heldBow != null && heldBow.activeSelf;

    /// <summary>True once the drawn bow outlived <see cref="DiabloSkills.BowStanceSeconds"/>.</summary>
    public bool StanceExpired => BowDrawn && Time.time > bowUntil;

    public void SetStarterBowModelVisible(bool visible)
    {
        if (showBowModel == visible) return;
        showBowModel = visible;
        if (bowRenderers == null) return;
        foreach (var renderer in bowRenderers)
            renderer.enabled = visible;
    }

    /// <summary>Draw the bow (hide the sword) for BowStanceSeconds.</summary>
    public void Enter()
    {
        bowUntil = Time.time + DiabloSkills.BowStanceSeconds;
        if (heldBow == null) heldBow = CreateHeldBow();
        if (heldBow != null) heldBow.SetActive(true);
        SetSwordVisible(false);
    }

    /// <summary>Put the bow away and show the sword again (melee strike or timeout).</summary>
    public void Exit()
    {
        bowUntil = float.NegativeInfinity;
        if (heldBow != null) heldBow.SetActive(false);
        SetSwordVisible(true);
    }

    GameObject CreateHeldBow()
    {
        var prefab = Resources.Load<GameObject>("SkillArt/HeroBow");
        if (prefab == null) return null;
        // Bows sit in the off hand; KayKit rigs expose a "handslot.l" mount.
        var hand = FindDeep(hero, "handslot.l") ?? FindDeep(hero, "hand.l");
        var bow = Object.Instantiate(prefab);
        bow.name = "HeldBow";
        if (hand != null)
        {
            bow.transform.SetParent(hand, false);
            bow.transform.localPosition = Vector3.zero;
            bow.transform.localRotation = HandMountRotation(bow);
        }
        else
        {
            bow.transform.SetParent(hero, false);
            bow.transform.localPosition = new Vector3(-0.35f, 1.0f, 0.25f);
            bow.transform.localRotation = Quaternion.identity;
        }
        bowRenderers = bow.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in bowRenderers)
            renderer.enabled = showBowModel;
        return bow;
    }

    /// <summary>
    /// KayKit's bow on handslot.l faces its string toward the target and its
    /// arc toward the archer (seen in the shotcheck). Turn it 180° about its
    /// own long axis so the arc leads and the string sits on the archer's side.
    /// </summary>
    public static Quaternion HandMountRotation(GameObject bow) =>
        Quaternion.AngleAxis(180f, LongAxis(bow));

    /// <summary>Longest mesh-bounds axis in the bow root's local space (limb to limb).</summary>
    public static Vector3 LongAxis(GameObject bow)
    {
        var min = Vector3.positiveInfinity;
        var max = Vector3.negativeInfinity;
        foreach (var r in bow.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh = null;
            if (r is SkinnedMeshRenderer smr) mesh = smr.sharedMesh;
            else
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null) mesh = mf.sharedMesh;
            }
            if (mesh == null) continue;
            var mb = mesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = mb.center + Vector3.Scale(mb.extents, new Vector3(
                    (i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                var local = bow.transform.InverseTransformPoint(r.transform.TransformPoint(corner));
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
        }
        if (float.IsInfinity(min.x)) return Vector3.up;
        var size = max - min;
        if (size.x >= size.y && size.x >= size.z) return Vector3.right;
        return size.y >= size.z ? Vector3.up : Vector3.forward;
    }

    void SetSwordVisible(bool visible)
    {
        if (swordRenderers == null)
        {
            var sword = FindDeep(hero, DiabloSkills.SwordName);
            swordRenderers = sword != null ? sword.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        }
        foreach (var r in swordRenderers)
            if (r != null) r.enabled = visible;
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var hit = FindDeep(t.GetChild(i), name);
            if (hit != null) return hit;
        }
        return null;
    }
}
