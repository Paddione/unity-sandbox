using UnityEngine;

/// <summary>Drives the baked Blender clips and the two Fen melee profiles.</summary>
public sealed class MoorbrutCombat : MonoBehaviour
{
    public bool tank;
    public float flankOffset;
    public Animation model;
    public float Speed => tank ? 1.3f : 6f;
    public float Reach => tank ? 2.7f : 1.55f;
    public float ImpactTime => (tank ? 43f : 13f) / 24f;
    public float Recovery => tank ? 1.4f : 0.85f;

    public Vector3 Approach(Vector3 towardsHero, float distance)
    {
        if (tank) return towardsHero;
        // Pack mates approach different sides, then turn inward to bite.
        float spread = Mathf.Clamp01((distance - Reach) / 2f);
        return Quaternion.AngleAxis(flankOffset * spread, Vector3.up) * towardsHero;
    }

    public void Walk(bool moving)
    {
        if (!model || model.IsPlaying("Strike")) return;
        if (moving)
        {
            if (!model.IsPlaying("Walk")) model.Play("Walk");
        }
        else if (model.IsPlaying("Walk")) ResetPose();
    }

    public void BeginStrike() => model.Play("Strike");

    public void ResetPose()
    {
        model.Stop();
        model["Walk"].time = 0f;
        model.Play("Walk");
        model.Sample();
        model.Stop();
    }

    public bool InArc(Vector3 position)
    {
        var offset = position - transform.position;
        offset.y = 0;
        return offset.sqrMagnitude < 0.001f || Vector3.Angle(transform.forward, offset) <= (tank ? 38f : 55f);
    }
}
