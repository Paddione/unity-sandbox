using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Hold figures watch the nearest hero (cinderreach-town-npc-identity D2):
/// within <see cref="Radius"/> they turn toward that hero (yaw only), with no
/// hero near they ease back to the pose they were placed in.
/// </summary>
public sealed class NpcFacing : MonoBehaviour
{
    public const float Radius = 6f;
    public const float TurnSpeed = 360f;

    Quaternion rest;
    float poll;
    Transform target;

    /// <summary>Hero currently watched (null = at rest). Test seam.</summary>
    public Transform Target => target;

    void Awake() => rest = transform.rotation;

    void Update()
    {
        poll -= Time.deltaTime;
        if (poll <= 0f)
        {
            poll = 0.2f;
            target = NearestHero(transform.position, Radius);
        }
        var goal = rest;
        if (target != null)
        {
            var d = target.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) goal = Quaternion.LookRotation(d.normalized);
        }
        transform.rotation = Quaternion.RotateTowards(transform.rotation, goal, TurnSpeed * Time.deltaTime);
    }

    /// <summary>Nearest hero within <paramref name="radius"/> on the ground plane (co-op players or the local "Player").</summary>
    public static Transform NearestHero(Vector3 from, float radius)
    {
        Transform best = null;
        float bestSq = radius * radius;
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening && nm.SpawnManager != null)
        {
            foreach (var kv in nm.SpawnManager.SpawnedObjects)
            {
                var nb = kv.Value;
                if (nb == null || !nb.IsPlayerObject) continue;
                Consider(nb.transform, from, ref best, ref bestSq);
            }
        }
        if (best == null)
        {
            var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
            if (hero != null) Consider(hero.transform, from, ref best, ref bestSq);
        }
        return best;
    }

    static void Consider(Transform t, Vector3 from, ref Transform best, ref float bestSq)
    {
        var d = t.position - from;
        d.y = 0f;
        float sq = d.sqrMagnitude;
        if (sq <= bestSq) { bestSq = sq; best = t; }
    }
}
