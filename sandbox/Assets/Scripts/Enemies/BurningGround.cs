using UnityEngine;

/// <summary>
/// Burning ground field (act-one S4, Kilnmother phase 2): 1 s telegraph, then
/// 6 s of fire dealing 1 damage per second to a hero standing inside the
/// 2 m radius.
/// </summary>
public sealed class BurningGround : MonoBehaviour
{
    public const float Radius = 2f;
    public const float Telegraph = 1f;
    public const float Burn = 6f;
    public const int DamagePerSecond = 1;

    public static int Ticks;

    float bornAt;
    float lastTick;
    Renderer disc;

    public static BurningGround Spawn(Vector3 at)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "BurningGround";
        var col = go.GetComponent<Collider>();
        if (Application.isPlaying) Destroy(col); else DestroyImmediate(col);
        at.y = 0.03f;
        go.transform.position = at;
        go.transform.localScale = new Vector3(Radius * 2f, 0.01f, Radius * 2f);
        var b = go.AddComponent<BurningGround>();
        b.bornAt = Time.time;
        b.lastTick = Time.time + Telegraph;
        b.disc = go.GetComponent<Renderer>();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (b.disc != null && shader != null)
        {
            var m = new Material(shader) { color = new Color(0.6f, 0.25f, 0.08f) };
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", new Color(0.4f, 0.12f, 0.02f));
            b.disc.sharedMaterial = m;
        }
        return b;
    }

    public static bool Inside(Vector3 center, Vector3 hero)
    {
        var d = hero - center;
        d.y = 0f;
        return d.magnitude <= Radius;
    }

    public bool Burning(float now) => now - bornAt >= Telegraph;

    void Update()
    {
        var go = GameObject.Find(DiabloNetworkPlayer.HeroName);
        Tick(Time.time, go != null ? go.transform : null);
    }

    /// <summary>Damage once per second while burning (headless-callable). True when burnt out.</summary>
    public bool Tick(float now, Transform hero)
    {
        if (disc != null && Burning(now) && disc.sharedMaterial != null)
            disc.sharedMaterial.SetColor("_EmissionColor", new Color(1f, 0.4f, 0.05f) * 2f);
        if (now - bornAt >= Telegraph + Burn)
        {
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
            return true;
        }
        if (!Burning(now) || now - lastTick < 1f) return false;
        lastTick = now;
        if (hero == null || !Inside(transform.position, hero.position) || !Diablo.IsLocalAuthority()) return false;
        Ticks++;
        int dmg = DifficultyProfile.Current.ScaleDamage(DamagePerSecond);
        var hp = hero.GetComponent<PlayerHealth>();
        if (hp != null) hp.TakeDamage(dmg);
        return false;
    }

    public static void ResetRun() => Ticks = 0;
}
