using UnityEngine;

public sealed class GeneratedGearMesh : MonoBehaviour
{
    public Mesh Mesh { get; set; }

    void OnDestroy()
    {
        if (Application.isPlaying) Destroy(Mesh);
        else DestroyImmediate(Mesh);
    }
}
