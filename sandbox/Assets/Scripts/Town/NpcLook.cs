using UnityEngine;

/// <summary>Applies the NPC tint from <see cref="NpcAppearance"/> to every renderer (property block, shared materials untouched).</summary>
public sealed class NpcLook : MonoBehaviour
{
    public string npcId;
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int Color = Shader.PropertyToID("_Color");

    /// <summary>Tint applied (test seam).</summary>
    public Color AppliedTint { get; private set; } = UnityEngine.Color.white;

    void Awake() => Apply();

    public void Apply()
    {
        if (!NpcAppearance.TryGet(npcId, out var look)) return;
        var block = new MaterialPropertyBlock();
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (r == null || r is ParticleSystemRenderer || r.GetComponent<TextMesh>() != null) continue;
            r.GetPropertyBlock(block);
            block.SetColor(BaseColor, look.tint);
            block.SetColor(Color, look.tint);
            r.SetPropertyBlock(block);
        }
        AppliedTint = look.tint;
    }
}
