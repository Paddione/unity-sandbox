using UnityEngine;

/// <summary>
/// Straight-flying hero projectile (fireball or arrow). Hits are found with
/// distance math against "Husk"-tagged <see cref="EnemyHealth"/> objects (same
/// rule as melee, no trigger physics); solid scene colliders stop it. A
/// fireball explodes on impact and damages every husk in its blast radius,
/// an arrow damages the first husk it reaches.
/// </summary>
public sealed class DiabloProjectile : MonoBehaviour
{
    /// <summary>Planar hit radius around the flight segment.</summary>
    public const float HitRadius = 0.6f;

    public DiabloSkills.Skill kind;
    public Vector3 direction = Vector3.forward;
    public float speed = 14f;
    public float maxRange = 16f;
    public int damage = 1;
    public float blastRadius;
    public float knockback = 0.6f;
    public Transform owner;

    public static int Launched;
    public static int Impacts;

    float travelled;
    bool done;

    /// <summary>Spawn a projectile with a procedural greybox look.</summary>
    public static DiabloProjectile Spawn(DiabloSkills.Skill kind, Vector3 origin, Vector3 dir,
        Transform owner, int damage, float speed, float range, float blastRadius)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        dir.Normalize();

        GameObject go;
        if (kind == DiabloSkills.Skill.Fireball)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localScale = Vector3.one * 0.45f;
            Tint(go, new Color(1f, 0.45f, 0.08f));
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.15f);
            light.range = 4f;
            light.intensity = 3f;
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.localScale = new Vector3(0.06f, 0.06f, 0.8f);
            Tint(go, new Color(0.55f, 0.4f, 0.25f));
        }
        Object.Destroy(go.GetComponent<Collider>());
        go.name = kind == DiabloSkills.Skill.Fireball ? "Fireball" : "Arrow";
        go.transform.position = origin;
        go.transform.rotation = Quaternion.LookRotation(dir);

        var p = go.AddComponent<DiabloProjectile>();
        p.kind = kind;
        p.direction = dir;
        p.owner = owner;
        p.damage = damage;
        p.speed = speed;
        p.maxRange = range;
        p.blastRadius = blastRadius;
        p.knockback = kind == DiabloSkills.Skill.Fireball ? 0.9f : 0.3f;
        Launched++;
        return p;
    }

    void Update()
    {
        if (done) return;
        float step = speed * Time.deltaTime;
        if (step <= 0f) return;
        Vector3 from = transform.position;
        Vector3 to = from + direction * step;

        var husk = FirstHuskOnSegment(from, to, out float huskT);
        float wallT = WallDistance(from, step);
        if (husk != null && huskT <= wallT)
        {
            Impact(husk.transform.position, husk);
            return;
        }
        if (wallT < step)
        {
            Impact(from + direction * wallT, null);
            return;
        }

        transform.position = to;
        travelled += step;
        if (travelled >= maxRange) Fizzle();
    }

    /// <summary>Nearest husk whose planar distance to the segment is within HitRadius.</summary>
    GameObject FirstHuskOnSegment(Vector3 from, Vector3 to, out float alongDist)
    {
        alongDist = float.MaxValue;
        GameObject best = null;
        Vector3 a = Flat(from), b = Flat(to);
        Vector3 ab = b - a;
        float len = ab.magnitude;
        if (len < 0.0001f) return null;
        Vector3 dir = ab / len;
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk.GetComponent<EnemyHealth>() == null) continue;
            Vector3 p = Flat(husk.transform.position);
            float t = Mathf.Clamp(Vector3.Dot(p - a, dir), 0f, len);
            if ((a + dir * t - p).magnitude > HitRadius) continue;
            if (t < alongDist)
            {
                alongDist = t;
                best = husk;
            }
        }
        return best;
    }

    /// <summary>Distance to the first solid non-husk collider ahead (MaxValue when clear).</summary>
    float WallDistance(Vector3 from, float step)
    {
        var hits = Physics.RaycastAll(from, direction, step, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        foreach (var h in hits)
        {
            var t = h.collider.transform;
            if (owner != null && (t == owner || t.IsChildOf(owner))) continue;
            if (h.collider.CompareTag("Husk") || h.collider.GetComponentInParent<EnemyHealth>() != null) continue;
            if (h.distance < best) best = h.distance;
        }
        return best;
    }

    void Impact(Vector3 point, GameObject directHit)
    {
        done = true;
        Impacts++;
        Vector3 origin = owner != null ? owner.position : point - direction;
        if (blastRadius > 0f)
        {
            foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
            {
                if (husk == null) continue;
                var d = Flat(husk.transform.position) - Flat(point);
                if (d.magnitude > blastRadius) continue;
                DiabloSkills.ApplyHit(husk, damage, point, knockback);
            }
            Explosion.Spawn(point, blastRadius);
        }
        else if (directHit != null)
        {
            DiabloSkills.ApplyHit(directHit, damage, origin, knockback);
        }
        Destroy(gameObject);
    }

    void Fizzle()
    {
        done = true;
        Destroy(gameObject);
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    static void Tint(GameObject go, Color c)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return;
        go.GetComponent<Renderer>().sharedMaterial = new Material(shader) { color = c };
    }

    /// <summary>Reset run state (scene load / headless tests).</summary>
    public static void ResetRun()
    {
        Launched = 0;
        Impacts = 0;
    }

    /// <summary>Short-lived expanding fire burst with a light flash.</summary>
    sealed class Explosion : MonoBehaviour
    {
        const float Life = 0.25f;
        float age;
        float radius;
        Light flash;

        public static void Spawn(Vector3 at, float radius)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "FireballBlast";
            go.transform.position = at;
            go.transform.localScale = Vector3.one * 0.3f;
            Tint(go, new Color(1f, 0.5f, 0.1f));
            var e = go.AddComponent<Explosion>();
            e.radius = radius;
            e.flash = go.AddComponent<Light>();
            e.flash.type = LightType.Point;
            e.flash.color = new Color(1f, 0.5f, 0.15f);
            e.flash.range = radius * 3f;
            e.flash.intensity = 6f;
        }

        void Update()
        {
            age += Time.deltaTime;
            float k = Mathf.Clamp01(age / Life);
            transform.localScale = Vector3.one * Mathf.Lerp(0.3f, radius * 2f, k);
            if (flash != null) flash.intensity = Mathf.Lerp(6f, 0f, k);
            if (k >= 1f) Destroy(gameObject);
        }
    }
}
