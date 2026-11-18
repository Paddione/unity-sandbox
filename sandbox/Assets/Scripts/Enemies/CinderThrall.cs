using UnityEngine;

/// <summary>
/// Cinder Thrall (act-one S3): fast melee swarm enemy of the Cinder Kiln
/// (moves and strikes through <see cref="EnemyAttack"/>). On death it leaves
/// a <see cref="ThrallBlast"/>: 0.6 s telegraph, then 2 m blast for 2 damage
/// to the hero only. Blasts never hit other thralls, so packs do not chain.
/// </summary>
public sealed class CinderThrall : MonoBehaviour, IEnemyDeathHook
{
    public const float BlastDelay = 0.6f;
    public const float BlastRadius = 2f;
    public const int BlastDamage = 2;

    public void OnEnemyDeath() => ThrallBlast.Spawn(transform.position);
}

/// <summary>Delayed death blast of a Cinder Thrall (telegraph disc, then damage).</summary>
public sealed class ThrallBlast : MonoBehaviour
{
    public static int Blasts;
    public static int HeroHits;

    float bornAt;
    bool fired;
    Transform disc;

    public static ThrallBlast Spawn(Vector3 at)
    {
        var go = new GameObject("ThrallBlast");
        go.transform.position = at;
        var b = go.AddComponent<ThrallBlast>();
        b.bornAt = Time.time;
        var discGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        discGo.name = "BlastTelegraph";
        var col = discGo.GetComponent<Collider>();
        if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
        discGo.transform.SetParent(go.transform, false);
        discGo.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        discGo.transform.localScale = new Vector3(0.2f, 0.01f, 0.2f);
        var r = discGo.GetComponent<Renderer>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (r != null && shader != null)
        {
            var m = new Material(shader) { color = new Color(1f, 0.35f, 0.1f) };
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(1f, 0.3f, 0.05f) * 2f);
            r.sharedMaterial = m;
        }
        b.disc = discGo.transform;
        return b;
    }

    /// <summary>Pure blast rule: hero inside the radius (planar).</summary>
    public static bool Hits(Vector3 center, Vector3 hero)
    {
        var d = hero - center;
        d.y = 0f;
        return d.magnitude <= CinderThrall.BlastRadius;
    }

    void Update() => Tick(Time.time);

    /// <summary>Grow the telegraph, fire once after the delay (headless-callable). True when it fired.</summary>
    public bool Tick(float now)
    {
        if (fired) return false;
        float t = Mathf.Clamp01((now - bornAt) / CinderThrall.BlastDelay);
        if (disc != null)
        {
            float s = CinderThrall.BlastRadius * 2f * t;
            disc.localScale = new Vector3(s, 0.01f, s);
        }
        if (now - bornAt < CinderThrall.BlastDelay) return false;
        fired = true;
        Blasts++;
        if (Diablo.IsLocalAuthority())
        {
            var hero = GameObject.Find(DiabloNetworkPlayer.HeroName);
            var hp = hero != null ? hero.GetComponent<PlayerHealth>() : null;
            if (hp != null && Hits(transform.position, hero.transform.position))
            {
                int dmg = DifficultyProfile.Current.ScaleDamage(CinderThrall.BlastDamage);
                hp.TakeDamage(dmg, transform.position);
                HeroHits++;
                DiabloJuice.OnPlayerHurt(hero.transform.position, dmg);
            }
        }
        DiabloJuice.OnBlast(transform.position);
        if (Application.isPlaying) Destroy(gameObject, 0.05f); else DestroyImmediate(gameObject);
        return true;
    }

    public static void ResetRun()
    {
        Blasts = 0;
        HeroHits = 0;
    }
}
