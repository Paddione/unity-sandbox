using UnityEngine;

/// <summary>
/// Straight-flying hero projectile (fireball or arrow). Hits are found with
/// distance math against "Husk"-tagged <see cref="EnemyHealth"/> objects (same
/// rule as melee, no trigger physics); solid scene colliders stop it. A
/// fireball explodes on impact and damages every husk in its blast radius,
/// an arrow damages the first husk it reaches. A hostile projectile (Bone
/// Warden, Hollow Abbot) targets heroes with <see cref="PlayerHealth"/>
/// instead and never damages husks.
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
    /// <summary>Extra husks a direct-hit projectile passes through (Lantern Bolt: 1).</summary>
    public int pierce;
    readonly System.Collections.Generic.List<GameObject> struck = new System.Collections.Generic.List<GameObject>();

    public static int Launched;
    public static int Impacts;

    float travelled;
    bool done;

    /// <summary>Visual-only copy (co-op mirror of another player's cast): never deals damage.</summary>
    public bool cosmetic;

    /// <summary>Enemy-fired: hits heroes (PlayerHealth), passes through husks.</summary>
    public bool hostile;

    /// <summary>Hostile projectile hits on a hero this run (test seam).</summary>
    public static int HeroHits;

    /// <summary>Seconds an arrow stays stuck in a wall after impact.</summary>
    public const float StuckArrowSeconds = 3f;

    /// <summary>
    /// Spawn a projectile. Art comes from Resources/SkillArt (built by
    /// SkillArtBuilder); a primitive stands in when the asset is missing.
    /// </summary>
    public static DiabloProjectile Spawn(DiabloSkills.Skill kind, Vector3 origin, Vector3 dir,
        Transform owner, int damage, float speed, float range, float blastRadius, bool cosmetic = false,
        bool hostile = false)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        dir.Normalize();

        bool fire = kind == DiabloSkills.Skill.Fireball;
        bool lantern = kind == DiabloSkills.Skill.LanternBolt;
        var prefab = lantern ? null
            : Resources.Load<GameObject>(fire ? "SkillArt/FireballProjectile" : "SkillArt/ArrowProjectile");
        GameObject go;
        if (lantern)
        {
            go = LanternShape();
            go.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(dir));
        }
        else if (prefab != null)
        {
            go = Object.Instantiate(prefab, origin, Quaternion.LookRotation(dir));
        }
        else
        {
            go = FallbackShape(fire);
            go.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(dir));
        }
        go.name = lantern ? "LanternBolt" : fire ? "Fireball" : "Arrow";

        var p = go.AddComponent<DiabloProjectile>();
        p.kind = kind;
        p.direction = dir;
        p.owner = owner;
        p.damage = damage;
        p.speed = speed;
        p.maxRange = range;
        p.blastRadius = blastRadius;
        p.knockback = fire ? 0.9f : lantern ? 0.2f : 0.3f;
        p.cosmetic = cosmetic;
        p.hostile = hostile;
        Launched++;
        return p;
    }

    /// <summary>Lantern Bolt art (second-class S2): pale-gold glowing orb with a warm light.</summary>
    static GameObject LanternShape()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Despawn(go.GetComponent<Collider>());
        go.transform.localScale = Vector3.one * 0.32f;
        Tint(go, new Color(1f, 0.92f, 0.6f));
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.85f, 0.5f);
        light.range = 5f;
        light.intensity = 2.5f;
        return go;
    }

    static GameObject FallbackShape(bool fire)
    {
        var go = GameObject.CreatePrimitive(fire ? PrimitiveType.Sphere : PrimitiveType.Cube);
        Despawn(go.GetComponent<Collider>());
        if (fire)
        {
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
            go.transform.localScale = new Vector3(0.06f, 0.06f, 0.8f);
            Tint(go, new Color(0.55f, 0.4f, 0.25f));
        }
        return go;
    }

    void Update() => Step(Time.deltaTime);

    /// <summary>Advance one frame (headless-callable with explicit dt).</summary>
    public void Step(float dt)
    {
        if (done) return;
        float step = speed * dt;
        if (step <= 0f) return;
        Vector3 from = transform.position;
        Vector3 to = from + direction * step;

        if (hostile)
        {
            var hero = FirstHeroOnSegment(from, to, out float heroT);
            float wall = WallDistance(from, step);
            if (hero != null && heroT <= wall)
            {
                var block = hero.GetComponent<DiabloBlock>();
                if (block != null && block.BlocksFrom(transform.position))
                {
                    DiabloAudio.PlayBlock();
                    Reflect(hero);
                    return;
                }
                Impact(hero.transform.position, hero.gameObject);
                return;
            }
            if (wall < step) { Impact(from + direction * wall, null); return; }
            transform.position = to;
            travelled += step;
            if (travelled >= maxRange) Fizzle();
            return;
        }

        // Spark Shot (class-kits-parity): a Quick Shot through a powder keg sets it off at once.
        if (kind == DiabloSkills.Skill.QuickShot && !cosmetic)
        {
            var keg = PowderKeg.OnSegment(from, to, ClassCombos.IgniteRadius);
            if (keg != null) keg.Detonate(true);
        }

        var husk = FirstHuskOnSegment(from, to, out float huskT);
        float wallT = WallDistance(from, step);
        if (husk != null && huskT <= wallT)
        {
            if (pierce > 0)
            {
                // Pass through: hit this husk once, keep flying.
                pierce--;
                struck.Add(husk);
                if (!cosmetic)
                {
                    bool crit = DiabloSkills.ApplyHit(husk, damage, from, knockback, owner);
                    NotifyKeen(crit);
                    NotifyNightfang(husk);
                }
                transform.position = to;
                travelled += step;
                if (travelled >= maxRange) Fizzle();
                return;
            }
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
            if (struck.Contains(husk)) continue;
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

    /// <summary>Nearest hero (PlayerHealth) whose planar distance to the segment is within HitRadius.</summary>
    static PlayerHealth FirstHeroOnSegment(Vector3 from, Vector3 to, out float alongDist)
    {
        alongDist = float.MaxValue;
        PlayerHealth best = null;
        Vector3 a = Flat(from), b = Flat(to);
        Vector3 ab = b - a;
        float len = ab.magnitude;
        if (len < 0.0001f) return null;
        Vector3 dir = ab / len;
        foreach (var hero in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            Vector3 p = Flat(hero.transform.position);
            float t = Mathf.Clamp(Vector3.Dot(p - a, dir), 0f, len);
            if ((a + dir * t - p).magnitude > HitRadius) continue;
            if (t < alongDist)
            {
                alongDist = t;
                best = hero;
            }
        }
        return best;
    }

    /// <summary>Distance to the first solid non-husk, non-hero collider ahead (MaxValue when clear).</summary>
    float WallDistance(Vector3 from, float step)
    {
        var hits = Physics.RaycastAll(from, direction, step, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        foreach (var h in hits)
        {
            var t = h.collider.transform;
            if (owner != null && (t == owner || t.IsChildOf(owner))) continue;
            if (h.collider.CompareTag("Husk") || h.collider.GetComponentInParent<EnemyHealth>() != null) continue;
            if (h.collider.GetComponentInParent<PlayerHealth>() != null) continue;
            if (h.distance < best) best = h.distance;
        }
        return best;
    }

    void Impact(Vector3 point, GameObject directHit)
    {
        done = true;
        Impacts++;
        Vector3 origin = owner != null ? owner.position : point - direction;
        if (hostile)
        {
            if (!cosmetic) HurtHeroes(point, directHit);
            if (blastRadius > 0f) SpawnBlast(point, blastRadius);
        }
        else if (blastRadius > 0f)
        {
            if (!cosmetic)
            {
                foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
                {
                    if (husk == null) continue;
                    var d = Flat(husk.transform.position) - Flat(point);
                    if (d.magnitude > blastRadius) continue;
                    DiabloSkills.ApplyHit(husk, damage, point, knockback, owner);
                }
            }
            SpawnBlast(point, blastRadius);
        }
        else if (directHit != null && !cosmetic)
        {
            bool keenCrit = DiabloSkills.ApplyHit(directHit, damage, origin, knockback, owner);
            NotifyKeen(keenCrit);
            NotifyNightfang(directHit);
        }

        // A wall-struck arrow stays stuck for a moment; everything else goes now.
        if (kind == DiabloSkills.Skill.Arrow && directHit == null)
        {
            transform.position = point - direction * 0.3f;
            var trail = GetComponent<TrailRenderer>();
            if (trail != null) trail.emitting = false;
            Despawn(gameObject, StuckArrowSeconds);
            return;
        }
        ReleaseParticles();
        Despawn(gameObject);
    }

    /// <summary>Hostile impact: the struck hero, plus every hero in the blast radius.</summary>
    void HurtHeroes(Vector3 point, GameObject directHit)
    {
        int dmg = DifficultyProfile.Current.ScaleDamage(damage);
        foreach (var hero in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            bool hit = hero.gameObject == directHit
                || (blastRadius > 0f && (Flat(hero.transform.position) - Flat(point)).magnitude <= blastRadius);
            if (!hit) continue;
            hero.TakeDamage(dmg, point);
            HeroHits++;
            if (Application.isPlaying) DiabloJuice.OnPlayerHurt(hero.transform.position, dmg);
        }
    }

    /// <summary>nightfang (class-kits-parity): every Quick Shot hit leaves a venom stack.</summary>
    void NotifyNightfang(GameObject husk)
    {
        if (kind != DiabloSkills.Skill.QuickShot || owner == null || husk == null) return;
        if (!BuildUniqueEffects.HasNightfang(owner.GetComponent<Equipment>())) return;
        EnemyStatus.AddVenom(husk, owner, BuildUniqueEffects.NightfangVenomStacks);
    }

    /// <summary>
    /// S4 keen-cadence: a Quick Shot crit consumes the cast's one cooldown cut.
    /// Base projectiles never qualify (the helm only answers special casts).
    /// </summary>
    void NotifyKeen(bool crit)
    {
        if (kind != DiabloSkills.Skill.QuickShot || !crit || owner == null) return;
        var skills = owner.GetComponent<DiabloSkills>();
        if (skills != null) skills.TryKeenReduction(DiabloSkills.Skill.QuickShot, true);
    }

    /// <summary>
    /// Frontal block: turn the hostile projectile around. It flies back as a
    /// hero-owned projectile and damages husks instead of heroes.
    /// </summary>
    void Reflect(PlayerHealth hero)
    {
        hostile = false;
        owner = hero.transform;
        var back = transform.position - hero.transform.position;
        back.y = 0f;
        if (back.sqrMagnitude < 0.0001f) back = -direction;
        back.y = 0f;
        direction = back.normalized;
        Debug.Log($"[Diablo] {hero.name} blocked and reflected a projectile.");
    }

    void Fizzle()
    {
        done = true;
        ReleaseParticles();
        Despawn(gameObject);
    }

    /// <summary>Detach trail particles so they fade out instead of popping.</summary>
    void ReleaseParticles()
    {
        foreach (var ps in GetComponentsInChildren<ParticleSystem>())
        {
            if (ps.main.simulationSpace != ParticleSystemSimulationSpace.World) continue;
            ps.transform.SetParent(null, true);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Despawn(ps.gameObject, ps.main.startLifetime.constantMax + 0.1f);
        }
    }

    /// <summary>Blast art: Resources prefab, procedural sphere when missing.</summary>
    public static GameObject SpawnBlast(Vector3 at, float radius)
    {
        DiabloJuice.OnBlast(at);
        var prefab = Resources.Load<GameObject>("SkillArt/FireballBlast");
        if (prefab == null) return Explosion.Spawn(at, radius);
        var go = Object.Instantiate(prefab, at, Quaternion.identity);
        go.name = "FireballBlast";
        go.AddComponent<BlastLight>();
        Despawn(go, 2f);
        return go;
    }

    /// <summary>Destroy in play mode, DestroyImmediate in edit mode (headless EditMode tests).</summary>
    static void Despawn(Object o, float delay = 0f)
    {
        if (o == null) return;
        if (Application.isPlaying) Object.Destroy(o, delay);
        else Object.DestroyImmediate(o);
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
        HeroHits = 0;
    }

    /// <summary>Short-lived expanding fire burst with a light flash.</summary>
    sealed class Explosion : MonoBehaviour
    {
        const float Life = 0.25f;
        float age;
        float radius;
        Light flash;

        public static GameObject Spawn(Vector3 at, float radius)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Despawn(go.GetComponent<Collider>());
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
            return go;
        }

        void Update()
        {
            age += Time.deltaTime;
            float k = Mathf.Clamp01(age / Life);
            transform.localScale = Vector3.one * Mathf.Lerp(0.3f, radius * 2f, k);
            if (flash != null) flash.intensity = Mathf.Lerp(6f, 0f, k);
            if (k >= 1f) Despawn(gameObject);
        }
    }

    /// <summary>Fades the blast prefab's light over its first 0.3 s.</summary>
    sealed class BlastLight : MonoBehaviour
    {
        Light flash;
        float start;
        float age;

        void Start()
        {
            flash = GetComponent<Light>();
            if (flash != null) start = flash.intensity;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (flash != null) flash.intensity = Mathf.Lerp(start, 0f, Mathf.Clamp01(age / 0.3f));
        }
    }
}
