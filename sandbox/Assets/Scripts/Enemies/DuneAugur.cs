using UnityEngine;

/// <summary>
/// Caster payload seam (act-two-opening S2): a <see cref="RangedCaster"/>
/// with one of these casts it instead of its fireball. Keeps the caster
/// brain (hold band, leash, cover check, telegraph orb) shared.
/// </summary>
public interface ICasterPayload
{
    /// <summary>Cast at the hero's position (authority and visuals alike).</summary>
    void Cast(Vector3 at);
}

/// <summary>
/// Dune Augur (act-two-opening S2): Glass Barrens area caster. Uses the
/// Bone Warden brain (<see cref="RangedCaster"/>: 6–8 m hold band, leash,
/// 0.6 s orb telegraph) but calls a <see cref="ShardField"/> under the
/// hero instead of a projectile: a growing ground ring warns for
/// <see cref="ShardField.Telegraph"/> seconds, then glass erupts and cuts
/// anyone who stayed inside. Stepping out is the counter; blocking does not help.
/// </summary>
public sealed class DuneAugur : MonoBehaviour, ICasterPayload
{
    public static int Casts;

    public void Cast(Vector3 at)
    {
        Casts++;
        ShardField.Spawn(at);
    }

    public static void ResetRun() => Casts = 0;
}

/// <summary>
/// Glass shard field: <see cref="Telegraph"/> s warning ring, then
/// <see cref="Active"/> s of shards dealing <see cref="Damage"/> every
/// <see cref="TickInterval"/> s to a hero inside <see cref="Radius"/>.
/// Every peer spawns the field (host directly, clients per ClientRpc) and
/// resolves it against its own hero (per-client hero HP, act-two-opening S3).
/// </summary>
public sealed class ShardField : MonoBehaviour
{
    public const float Radius = 2.2f;
    public const float Telegraph = 1.0f;
    public const float Active = 3f;
    public const float TickInterval = 0.75f;
    public const int Damage = 1;

    public static int Fields;
    public static int HeroHits;

    float bornAt;
    float lastTick;
    Transform ring;
    Transform shards;

    public static ShardField Spawn(Vector3 at)
    {
        at.y = 0.03f;
        var go = new GameObject("ShardField");
        go.transform.position = at;
        var f = go.AddComponent<ShardField>();
        f.bornAt = Time.time;
        f.lastTick = float.NegativeInfinity;
        Fields++;
        f.ring = Disc(go.transform, "ShardFieldTelegraph", new Color(0.35f, 0.85f, 0.8f), 0.6f);
        f.shards = new GameObject("ShardFieldSpikes").transform;
        f.shards.SetParent(go.transform, false);
        for (int i = 0; i < 7; i++)
        {
            float a = i * Mathf.PI * 2f / 7f;
            float r = i == 0 ? 0f : Radius * 0.6f;
            var spike = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spike.name = "Spike";
            Kill(spike.GetComponent<Collider>());
            spike.transform.SetParent(f.shards, false);
            spike.transform.localPosition = new Vector3(Mathf.Cos(a) * r, 0.45f, Mathf.Sin(a) * r);
            spike.transform.localRotation = Quaternion.Euler(12f, i * 51f, -8f);
            spike.transform.localScale = new Vector3(0.25f, 0.9f, 0.25f);
            Glow(spike.GetComponent<Renderer>(), new Color(0.55f, 1f, 0.95f), 2.5f);
        }
        f.shards.gameObject.SetActive(false);
        return f;
    }

    static Transform Disc(Transform parent, string name, Color c, float emission)
    {
        var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        d.name = name;
        Kill(d.GetComponent<Collider>());
        d.transform.SetParent(parent, false);
        d.transform.localScale = new Vector3(0.2f, 0.01f, 0.2f);
        Glow(d.GetComponent<Renderer>(), c, emission);
        return d.transform;
    }

    static void Glow(Renderer r, Color c, float emission)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (r == null || shader == null) return;
        var m = new Material(shader) { color = c };
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * emission);
        r.sharedMaterial = m;
    }

    static void Kill(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }

    public static bool Inside(Vector3 center, Vector3 hero)
    {
        var d = hero - center;
        d.y = 0f;
        return d.magnitude <= Radius;
    }

    public bool Erupted(float now) => now - bornAt >= Telegraph;

    void Update() => Tick(Time.time, HeroLocator.Solo());

    /// <summary>Grow the warning ring, then cut on an interval (headless-callable). True when spent.</summary>
    public bool Tick(float now, Transform hero)
    {
        float t = Mathf.Clamp01((now - bornAt) / Telegraph);
        if (ring != null)
        {
            float s = Radius * 2f * Mathf.Lerp(0.15f, 1f, t);
            ring.localScale = new Vector3(s, 0.01f, s);
        }
        if (now - bornAt >= Telegraph + Active)
        {
            Kill(gameObject);
            return true;
        }
        if (!Erupted(now)) return false;
        if (shards != null && !shards.gameObject.activeSelf)
        {
            shards.gameObject.SetActive(true);
            DiabloJuice.OnBlast(transform.position);
        }
        if (now - lastTick < TickInterval) return false;
        lastTick = now;
        if (hero == null || !Inside(transform.position, hero.position)) return false;
        var hp = hero.GetComponent<PlayerHealth>();
        if (hp == null || hp.CurrentHP <= 0f) return false;
        int dmg = DifficultyProfile.Current.ScaleDamage(Damage);
        hp.TakeDamage(dmg); // ground hazard: unblockable (no origin)
        HeroHits++;
        DiabloJuice.OnPlayerHurt(hero.position, dmg);
        return false;
    }

    public static void ResetRun()
    {
        Fields = 0;
        HeroHits = 0;
    }
}
