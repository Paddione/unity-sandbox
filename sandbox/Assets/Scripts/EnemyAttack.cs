using UnityEngine;

/// <summary>
/// Husk counterattack (Ashen Fen retaliation, D2 Fallen Ones). Distance-based
/// (never triggers — autosync-safe): in melee range the husk crouches
/// (telegraph), then strikes after the windup. Dodging out of range whiffs.
/// Damage goes to local <see cref="PlayerHealth"/> (solo-first, like HP).
/// </summary>
public sealed class EnemyAttack : MonoBehaviour
{
    public const float Range = 2.0f;
    public const float Windup = 0.4f;
    public const float Cooldown = 1.5f;
    public const float DodgeLeeway = 0.5f;

    /// <summary>Shuffle toward the hero when near but out of reach (D2 roam).</summary>
    public const float AggroRange = 8f;
    public const float ApproachSpeed = 1.5f;
    public const float StopDistance = 1.8f;

    /// <summary>Set by the builder per tier (1 normal, 2 elite/brute).</summary>
    public int damage = 1;

    /// <summary>Facing slerp rate toward the target (act-two-opening S2: Shardbacks turn slowly, see <see cref="ShardGuard"/>).</summary>
    public float turnRate = 10f;

    /// <summary>
    /// Encounter group (encounter-roles S1): 0 = front/always active,
    /// 1 = back/hunter, released at the Kiln choke
    /// (see <see cref="EncounterLayout"/>). Outside its zone the husk
    /// stays passive even when the hero teleports away mid-fight.
    /// </summary>
    public int encounterGroup;

    Vector3 homePos;
    bool homeSet;

    /// <summary>Husks closer than this push apart while approaching (no stacking).</summary>
    public const float SeparationRadius = 1.1f;
    /// <summary>Probe radius/height for the dressing check (capsule waist).</summary>
    public const float ProbeRadius = 0.35f;
    public const float ProbeHeight = 0.6f;
    static readonly float[] DetourAngles = { 0f, 35f, -35f, 70f, -70f, 105f, -105f };

    static readonly System.Collections.Generic.List<EnemyAttack> live =
        new System.Collections.Generic.List<EnemyAttack>();

    /// <summary>Times the approach had to turn off the straight line (test seam).</summary>
    public int detours;

    MoorbrutCombat brood;
    float StrikeRange => brood ? brood.Reach : Range;
    float RecoveryTime => brood ? brood.Recovery : Cooldown;

    Transform victim;
    HuskAnimator anim;
    float windupStart = -1f;
    float lastStrike = float.NegativeInfinity;
    Vector3 baseScale;

    /// <summary>Stun expiry (party-roles S2 tank slam): stunned husks hold still and never strike.</summary>
    float stunnedUntil = float.NegativeInfinity;

    /// <summary>True while a tank slam stuns this husk.</summary>
    public bool IsStunned => Time.time < stunnedUntil;

    /// <summary>Seconds of stun left (0 when free); test seam for combo stun lengths.</summary>
    public float StunLeft => Mathf.Max(0f, stunnedUntil - Time.time);

    /// <summary>Stun for seconds (keeps the longest running stun).</summary>
    public void Stun(float seconds)
    {
        if (seconds <= 0f) return;
        stunnedUntil = Mathf.Max(stunnedUntil, Time.time + seconds);
    }

    void Awake()
    {
        brood = GetComponent<MoorbrutCombat>();
        baseScale = transform.localScale;
        SetHome(transform.position);
        anim = GetComponent<HuskAnimator>();
        if (anim == null) anim = gameObject.AddComponent<HuskAnimator>();
    }

    /// <summary>Anchor this husk to a zone (builder/seeder call it after placing).</summary>
    public void SetHome(Vector3 at)
    {
        homePos = at;
        homeSet = true;
    }

    void OnEnable() => live.Add(this);
    void OnDisable() => live.Remove(this);

    void Update()
    {
        var hero = AcquireHero();
        if (hero == null)
        {
            CancelWindup();
            SetMoving(false);
            return;
        }
        if (IsStunned)
        {
            // Tank slam: hold still, cancel any windup, never strike while stunned.
            CancelWindup();
            SetMoving(false);
            return;
        }
        if (KilnLayout.PlazaSafe(hero.position) || BarrensLayout.PlazaSafe(hero.position))
        {
            // Fair-fights arrival plaza: hold while the hero orients.
            CancelWindup();
            SetMoving(false);
            return;
        }
        if (!homeSet) SetHome(transform.position);
        EncounterLayout.NoteHeroPosition(hero.position);
        // Zone change aborts aggro: a Kiln husk ignores a hero who left for the Fen.
        if (EncounterLayout.CurrentZone(hero.position) != EncounterLayout.CurrentZone(homePos))
        {
            CancelWindup();
            SetMoving(false);
            return;
        }
        float dist = PlanarDistance(hero);
        // Back/hunter group: dormant until the choke, unless the hero pokes it.
        if (encounterGroup == EncounterLayout.BackGroup
            && !EncounterLayout.GroupActive(encounterGroup, hero.position)
            && dist > EncounterLayout.PersonalSpace)
        {
            CancelWindup();
            SetMoving(false);
            return;
        }
        if (!brood || windupStart < 0f) FaceHero(hero);
        if (dist > StrikeRange && dist <= AggroRange && windupStart < 0f
            && Time.time - lastStrike >= RecoveryTime)
        {
            var step = hero.position - transform.position;
            step.y = 0f;
            step.Normalize();
            if (brood) step = brood.Approach(step, dist);
            float speed = brood ? brood.Speed : ApproachSpeed;
            float stop = brood ? StrikeRange * 0.9f : StopDistance;
            float move = Mathf.Min(speed * EnemyStatus.MoveFactor(gameObject) * Time.deltaTime, dist - stop);
            bool walked = false;
            if (move > 0f)
            {
                var dir = Steer(step, move);
                if (dir.sqrMagnitude > 0f)
                {
                    transform.position += dir * move;
                    walked = true;
                }
            }
            SetMoving(walked);
            return;
        }
        SetMoving(false);
        if (windupStart >= 0f)
        {
            if (dist > StrikeRange + DodgeLeeway + 1.5f)
            {
                // Ran far away: cancel, short cooldown before re-engaging.
                CancelWindup();
                lastStrike = Time.time;
            }
            else if (Time.time - windupStart >= (brood ? brood.ImpactTime : Windup))
            {
                ResolveWindup(hero, dist);
            }
            return;
        }
        if (dist <= StrikeRange && Time.time - lastStrike >= RecoveryTime
            && (!brood || brood.InArc(hero.position)))
        {
            victim = hero;
            windupStart = Time.time;
            anim.OnAttack();
            if (brood) brood.BeginStrike();
            else transform.localScale = new Vector3(
                baseScale.x * 1.1f, baseScale.y * 0.85f, baseScale.z * 1.1f);
        }
    }

    void SetMoving(bool moving)
    {
        anim.SetMoving(moving);
        if (brood) brood.Walk(moving);
    }

    void ResolveWindup(Transform hero, float dist)
    {
        transform.localScale = baseScale;
        windupStart = -1f;
        lastStrike = Time.time;
        if (hero == null || dist > StrikeRange + DodgeLeeway) return; // dodged
        if (brood && !brood.InArc(hero.position)) return;
        if (EnemyStatus.IsBlinded(gameObject)) return; // Smoke Pot: a blinded swing misses
        var hp = hero.GetComponent<PlayerHealth>();
        if (hp == null) return;
        int dealt = DifficultyProfile.Current.ScaleDamage(damage);
        hp.TakeDamage(dealt, transform.position);
        // replayability S2: Thorns reflect flat damage onto the striker.
        var heroEq = hero.GetComponent<Equipment>();
        int thorns = heroEq != null ? heroEq.ThornsDamage() : 0;
        var self = GetComponent<EnemyHealth>();
        if (thorns > 0 && self != null) self.TakeDamage(thorns);
        DiabloAudio.PlayHurt();
        DiabloJuice.OnPlayerHurt(hero.position, dealt);
        if (hp.CurrentHP <= 0f)
        {
            var comp = hp.GetComponent<PartyCompanion>();
            if (comp != null) comp.Die(); // AI waits out death at camp, then rejoins (S4)
            else
            {
                hp.DieAndRespawn();
                DiabloAudio.PlayPlayerDeath();
            }
        }
    }

    /// <summary>
    /// Walk direction around static dressing plus husk separation. Probes the
    /// straight line first, then fans out; heroes (Ignore Raycast), triggers
    /// and other husks never block (husks separate by distance math, since
    /// their transform-moved colliders are not synced). Zero = boxed in.
    /// </summary>
    Vector3 Steer(Vector3 toHero, float move)
    {
        var desired = toHero + Separation();
        desired.y = 0f;
        if (desired.sqrMagnitude < 0.0001f) desired = toHero;
        desired.Normalize();
        var origin = transform.position + Vector3.up * ProbeHeight;
        float probe = move + ProbeRadius + 0.25f;
        int mask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        for (int i = 0; i < DetourAngles.Length; i++)
        {
            var dir = Quaternion.Euler(0f, DetourAngles[i], 0f) * desired;
            if (!Blocked(origin, dir, probe, mask))
            {
                if (i > 0) detours++;
                return dir;
            }
        }
        return Vector3.zero;
    }

    bool Blocked(Vector3 origin, Vector3 dir, float probe, int mask)
    {
        var hits = Physics.SphereCastAll(origin, ProbeRadius, dir, probe, mask,
            QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (h.collider.GetComponentInParent<EnemyHealth>() != null) continue;
            if (h.collider.GetComponentInParent<PlayerHealth>() != null) continue;
            if (h.distance <= 0f) continue; // started inside (ground seam): not ahead
            return true;
        }
        return false;
    }

    Vector3 Separation()
    {
        var push = Vector3.zero;
        foreach (var other in live)
        {
            if (other == this) continue;
            var d = transform.position - other.transform.position;
            d.y = 0f;
            float m = d.magnitude;
            if (m >= SeparationRadius || m < 0.0001f) continue;
            push += d / m * (1f - m / SeparationRadius);
        }
        return push;
    }

    void CancelWindup()
    {
        if (windupStart < 0f) return;
        windupStart = -1f;
        victim = null;
        if (brood) brood.ResetPose();
        transform.localScale = baseScale;
    }

    float PlanarDistance(Transform hero)
    {
        var d = hero.position - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    void FaceHero(Transform hero)
    {
        var d = hero.position - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(d), turnRate * Time.deltaTime);
    }

    /// <summary>
    /// S2 co-op targeting: nearest living hero (replaces name-based "Player"
    /// lookup; the cached victim is kept only while still alive).
    /// Party-roles S2: the highest-threat living source wins first, so the
    /// tank slam pulls aggro; an empty table keeps the old nearest logic, so
    /// solo play without any threat behaves exactly like before.
    /// Runs locally on every peer for movement; the server owns hero-to-enemy
    /// damage via HitValidator, enemy-to-hero stays per-client HP.
    /// </summary>
    public Transform AcquireHero()
    {
        var table = GetComponent<ThreatTable>();
        if (table != null)
        {
            var top = table.TopSource();
            if (top != null)
            {
                victim = top;
                return top;
            }
        }
        if (victim != null)
        {
            var php = victim.GetComponent<PlayerHealth>();
            if (php != null && php.CurrentHP > 0f && victim.gameObject.activeInHierarchy) return victim;
            victim = null;
        }
        // Nearest living among all heroes (solo: the single "Player").
        var living = HitValidator.NearestLivingPlayer(transform.position);
        if (living != null) return living;
        var solo = HeroLocator.Solo(); // fallback: scene search cache (act-one perf)
        if (solo != null) return solo;
        float best = float.MaxValue;
        Transform bestT = null;
        foreach (var h in GameObject.FindObjectsOfType<PlayerHealth>())
        {
            var d = h.transform.position - transform.position;
            d.y = 0f;
            if (d.magnitude < best)
            {
                best = d.magnitude;
                bestT = h.transform;
            }
        }
        return bestT;
    }
}
