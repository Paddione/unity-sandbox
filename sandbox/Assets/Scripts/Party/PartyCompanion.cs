using UnityEngine;

/// <summary>
/// AI party member (cinderreach-party-roles S4). Fills a missing party role
/// in solo play: follows the leader, fights the leader's fight with its own
/// class kit (via <see cref="DiabloSkills.classOverride"/>) and — as a
/// healer — mends injured allies. Simple by design: no loot runs, no bag, no
/// dialogue. Gear is equipped onto the companion by the player
/// (<see cref="Equip"/>) and its level mirrors the protagonist
/// (<see cref="DiabloProgression.MirrorHeroProgress"/>). Death waits out
/// <see cref="RejoinDelay"/> at camp, then the companion rejoins at the leader.
/// </summary>
public sealed class PartyCompanion : MonoBehaviour
{
    /// <summary>Husks near the leader (not the companion) are engaged, so the party stays together.</summary>
    public const float EngageRange = 12f;
    /// <summary>Further than this from the leader, the companion catches up by teleport.</summary>
    public const float LeashRange = 15f;
    /// <summary>Companion foot speed (the hero's <see cref="DiabloClickController.walkSpeed"/>).</summary>
    public const float MoveSpeed = 5f;
    /// <summary>Beyond this distance from its formation slot the companion runs to catch up.</summary>
    public const float CatchUpRange = 4f;
    /// <summary>Catch-up speed factor, so a trailing companion closes on a walking hero.</summary>
    public const float CatchUpFactor = 1.6f;
    /// <summary>Further than this from the leader, the companion breaks off its fight and regroups.</summary>
    public const float RegroupRange = 8f;
    /// <summary>Slot arrival tolerance (no jitter around the formation point).</summary>
    public const float SlotTolerance = 0.35f;

    /// <summary>
    /// Formation slots behind the leader, in the leader's local frame
    /// (playtest feedback 2026-11-23: the party walks with the hero).
    /// </summary>
    static readonly Vector3[] FormationSlots =
    {
        new Vector3(-1.8f, 0f, -1.6f),
        new Vector3(1.8f, 0f, -1.6f),
        new Vector3(0f, 0f, -2.8f),
    };

    /// <summary>Formation slot index (set by <see cref="PartyRoster"/> at spawn).</summary>
    public int slot;

    /// <summary>World point of formation slot <paramref name="index"/> around a leader pose.</summary>
    public static Vector3 SlotPosition(Vector3 leaderPos, Quaternion leaderRot, int index)
    {
        var flatFwd = leaderRot * Vector3.forward;
        flatFwd.y = 0f;
        var rot = flatFwd.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(flatFwd) : Quaternion.identity;
        var local = FormationSlots[((index % FormationSlots.Length) + FormationSlots.Length) % FormationSlots.Length];
        return leaderPos + rot * local;
    }

    /// <summary>Live companions (nameplates read this instead of scanning the scene).</summary>
    public static readonly System.Collections.Generic.List<PartyCompanion> Live =
        new System.Collections.Generic.List<PartyCompanion>();

    /// <summary>Name over the companion (its class; companions carry no hero name).</summary>
    public string DisplayName => Role.displayName;

    /// <summary>This companion's formation point around the current leader.</summary>
    public Vector3 FollowPoint => leader != null ? SlotPosition(leader.position, leader.rotation, slot) : transform.position;

    /// <summary>
    /// Zone travel (gate, waypoint, group travel): the companion arrives with
    /// the leader on its formation slot around the arrival point. Dead
    /// companions stay at camp; their rejoin lands at the leader anyway.
    /// </summary>
    public void PortTo(Vector3 leaderArrival, Quaternion leaderRot)
    {
        if (IsDead) return;
        transform.position = Bounded(SlotPosition(leaderArrival, leaderRot, slot), leaderArrival);
        transform.rotation = leaderRot;
        SetMoving(false);
    }
    /// <summary>Group heal fires when this many allies (caster included) are hurt.</summary>
    public const int GroupHealNeed = 2;
    /// <summary>Seconds a dead companion waits at camp before rejoining.</summary>
    public const float RejoinDelay = 15f;

    /// <summary>Class id of this companion (role follows <see cref="ClassCatalog"/>).</summary>
    public string classId = ClassCatalog.FenwalkerId;
    /// <summary>Hero (or host player) this companion follows.</summary>
    public Transform leader;

    public ClassDefinition Role => ClassCatalog.Get(classId);

    /// <summary>Itemized gear (party-team): the companion's own three slots, never null after spawn.</summary>
    public Equipment Gear => GetComponent<Equipment>();

    /// <summary>
    /// Equip an item onto the companion (party-team itemization). Class-bound
    /// weapons only fit the companion's own class, like the hero's bag.
    /// False without gear, without an item, or on a class mismatch.
    /// </summary>
    public bool Equip(ItemInstance item)
    {
        var gear = Gear;
        if (gear == null || item == null) return false;
        if (!Equipment.CanEquip(item, classId)) return false;
        gear.Equip(item);
        var hp = GetComponent<PlayerHealth>();
        if (hp != null) hp.Refresh();
        return true;
    }

    DiabloSkills skills;
    DiabloMelee melee;
    PlayerHealth hp;
    Animator animator;
    DiabloAnimator animDriver;
    LocomotionTwist twist;
    HeroEquipmentAppearance gearAppearance;
    bool wantMoving;
    float stepAccum;

    /// <summary>Time.time when a dead companion may rejoin; negative while alive.</summary>
    float rejoinAt = float.NegativeInfinity;

    /// <summary>True while waiting out death at camp.</summary>
    public bool IsDead => rejoinAt > 0f;

    void Start()
    {
        skills = GetComponent<DiabloSkills>();
        melee = GetComponent<DiabloMelee>();
        hp = GetComponent<PlayerHealth>();
        animator = GetComponent<Animator>();
        animDriver = GetComponent<DiabloAnimator>();
        // Clones and fallback bodies arrive with different component sets;
        // every class gets the same locomotion + worn-gear footwork.
        DiabloAnimator.EnsureFootwork(gameObject);
        animator = GetComponent<Animator>();
        animDriver = GetComponent<DiabloAnimator>();
        twist = GetComponent<LocomotionTwist>();
        if (twist != null) twist.ResolveBones();
        gearAppearance = GetComponent<HeroEquipmentAppearance>();
        if (gearAppearance == null) gearAppearance = gameObject.AddComponent<HeroEquipmentAppearance>();
        var look = GetComponent<ClassAppearance>();
        if (look != null) gearAppearance.Initialize(look);
        if (skills != null) skills.classOverride = Role;
        if (!Live.Contains(this)) Live.Add(this);
    }

    void OnDestroy()
    {
        Live.Remove(this);
    }

    /// <summary>Locomotion bool the companion drives itself (DiabloAnimator has no click controller here).</summary>
    void SetMoving(bool value)
    {
        wantMoving = value;
        ApplyMoving();
    }

    void LateUpdate()
    {
        // Measured motion (catch-up slides, knockbacks, ports) animates the
        // legs even when the brain ordered idle — same rule as the hero.
        ApplyMoving();
    }

    void ApplyMoving()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        bool moving = wantMoving || (twist != null && twist.MovingMeasured);
        animator.SetBool(animDriver != null ? animDriver.moveParam : "Moving", moving);
        if (moving && animator.isActiveAndEnabled)
        {
            // Audible footwork, same stride as the hero; silent while idle.
            stepAccum += MoveSpeed * Time.deltaTime;
            if (stepAccum >= DiabloClickController.StepStride)
            {
                stepAccum = 0f;
                DiabloAudio.PlayFootstep(transform.position);
            }
        }
        else stepAccum = 0f;
    }

    void Update()
    {
        if (leader == null || hp == null) return;
        if (!IsDead && hp.CurrentHP <= 0f) return; // death is routed through Die()
        var toLeader = leader.position - transform.position;
        toLeader.y = 0f;
        if (toLeader.magnitude > LeashRange)
        {
            transform.position = Bounded(FollowPoint, leader.position);
            transform.rotation = leader.rotation;
            SetMoving(false);
            return;
        }
        // A leader walking away wins over the fight: regroup first.
        var target = toLeader.magnitude > RegroupRange ? null : NearestHuskNearLeader();
        bool acted = target != null && ActByRole(target);
        if (!acted) FollowStep();
        else if (target != null) Face(target.transform.position);
    }

    /// <summary>Walk toward the formation slot, running when it trails far behind.</summary>
    void FollowStep()
    {
        var toSlot = FollowPoint - transform.position;
        toSlot.y = 0f;
        float dist = toSlot.magnitude;
        if (dist <= SlotTolerance)
        {
            // Idle in formation: look where the leader looks.
            var fwd = leader.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(fwd), 8f * Time.deltaTime);
            SetMoving(false);
            return;
        }
        float speed = MoveSpeed * (dist > CatchUpRange ? CatchUpFactor : 1f);
        var step = toSlot / dist * Mathf.Min(speed * Time.deltaTime, dist);
        var next = transform.position + step;
        next.y = leader.position.y;
        transform.position = Bounded(next, leader.position);
        transform.rotation = Quaternion.LookRotation(new Vector3(step.x, 0f, step.z));
        SetMoving(true);
    }

    /// <summary>Nearest living husk within engage range of the leader.</summary>
    GameObject NearestHuskNearLeader()
    {
        GameObject best = null;
        float bestD = float.MaxValue;
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk == null) continue;
            var health = husk.GetComponent<EnemyHealth>();
            if (health == null || !health.IsAlive) continue;
            var to = husk.transform.position - leader.position;
            to.y = 0f;
            float d = to.magnitude;
            if (d <= EngageRange && d < bestD) { bestD = d; best = husk; }
        }
        return best;
    }

    bool ActByRole(GameObject target)
    {
        var role = Role.role;
        var toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;
        var dir = dist > 0.0001f ? toTarget / dist : transform.forward;
        if (skills == null) return false;
        bool Ready(DiabloSkills.Skill s) => skills.CooldownLeft(s) <= 0f;
        if (role == ClassDefinition.PartyRole.Tank)
        {
            // class-kits-parity combo order: mire the pack, slam it (Fen Shackle), cleave the stunned (Shatter).
            if (dist <= skills.slamRadius && Ready(DiabloSkills.Skill.SeismicSlam))
                return skills.CastToward(DiabloSkills.Skill.SeismicSlam, dir);
            if (Ready(DiabloSkills.Skill.MireGround)
                && ClassCombos.HusksInRadius(transform.position, ClassCombos.MireGroundRadius).Count >= 2)
                return skills.CastToward(DiabloSkills.Skill.MireGround, dir);
            if (dist > meleeRange() && dist <= ClassCombos.MireHookRange && Ready(DiabloSkills.Skill.MireHook))
                return skills.CastToward(DiabloSkills.Skill.MireHook, dir);
            if (dist <= skills.CleaveRadius && Ready(DiabloSkills.Skill.Cleave)
                && target.GetComponent<EnemyAttack>() is EnemyAttack ta && ta.IsStunned)
                return skills.CastToward(DiabloSkills.Skill.Cleave, dir);
            if (Ready(DiabloSkills.Skill.IronVow) && hp != null && hp.CurrentHP < hp.MaxHP * 0.7f)
                return skills.CastToward(DiabloSkills.Skill.IronVow, dir);
            if (dist <= meleeRange() && melee != null)
                return melee.TryConeAttack(transform, dir) > 0;
            Approach(target, dir, dist);
            return true;
        }
        if (role == ClassDefinition.PartyRole.Dps)
        {
            // class-kits-parity combo order: venom, then the keg burns it off (Venom Burn).
            if (dist <= ClassCombos.PowderKegRange && Ready(DiabloSkills.Skill.PowderKeg)
                && EnemyStatus.Venom(target) >= ClassCombos.VenomStacksPerVial)
                return skills.CastToward(DiabloSkills.Skill.PowderKeg, dir);
            if (dist <= ClassCombos.VenomRange && Ready(DiabloSkills.Skill.VenomVial))
                return skills.CastToward(DiabloSkills.Skill.VenomVial, dir);
            if (dist <= ClassCombos.SmokeRange && Ready(DiabloSkills.Skill.SmokePot)
                && ClassCombos.HusksInRadius(target.transform.position, ClassCombos.SmokeRadius).Count >= 2)
                return skills.CastToward(DiabloSkills.Skill.SmokePot, dir);
            if (dist <= skills.quickShotRange && skills.CooldownLeft(DiabloSkills.Skill.QuickShot) <= 0f)
                return skills.CastToward(DiabloSkills.Skill.QuickShot, dir);
            if (dist <= skills.flurryRadius && skills.CooldownLeft(DiabloSkills.Skill.BladeFlurry) <= 0f)
                return skills.CastToward(DiabloSkills.Skill.BladeFlurry, dir);
            Approach(target, dir, dist);
            return true;
        }
        // Healer: mend first, smite when nobody is hurt.
        int hurt = HurtAlliesIn(skills.groupHealRadius);
        if (hurt >= GroupHealNeed && skills.CooldownLeft(DiabloSkills.Skill.RenewingCircle) <= 0f)
            return skills.CastToward(DiabloSkills.Skill.RenewingCircle, dir);
        if (skills.FindHealTarget(skills.targetHealRange) != null
            && skills.CooldownLeft(DiabloSkills.Skill.MendingLight) <= 0f)
            return skills.CastToward(DiabloSkills.Skill.MendingLight, dir);
        if (dist <= skills.lanternBoltRange && skills.CooldownLeft(DiabloSkills.Skill.LanternBolt) <= 0f)
            return skills.CastToward(DiabloSkills.Skill.LanternBolt, dir);
        return false;
    }

    float meleeRange() => melee != null ? melee.attackRange : DiabloMelee.OutreachRange;

    void Approach(GameObject target, Vector3 dir, float dist)
    {
        var toLeader = leader.position - transform.position;
        toLeader.y = 0f;
        float step = Mathf.Min(MoveSpeed * Time.deltaTime, dist);
        var next = transform.position + dir * step;
        var fromLeader = next - leader.position;
        fromLeader.y = 0f;
        // Never chase past the regroup line, or fight and regroup would flip-flop.
        if (fromLeader.magnitude > RegroupRange - 0.5f) return;
        transform.position = Bounded(next, leader.position);
        Face(target.transform.position);
        SetMoving(true);
    }

    /// <summary>
    /// Companions move without a collider, so they clamp to the zone floor
    /// (<see cref="WorldBounds"/>) whenever the leader stands on one; a slot
    /// behind a hero at the wall stays inside the wall.
    /// </summary>
    static Vector3 Bounded(Vector3 p, Vector3 leaderPos) =>
        WorldBounds.IsWalkable(leaderPos, -1f) ? WorldBounds.Clamp(p) : p;

    void Face(Vector3 at)
    {
        var d = at - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(d);
    }

    int HurtAlliesIn(float radius)
    {
        int n = 0;
        foreach (var h in HeroLocator.AllHeroes())
        {
            var php = h != null ? h.GetComponent<PlayerHealth>() : null;
            if (php == null) continue;
            var to = h.position - transform.position;
            to.y = 0f;
            if (to.magnitude <= radius && php.CurrentHP < php.MaxHP) n++;
        }
        return n;
    }

    /// <summary>Dies at camp: gone from the fight, back at the leader after the delay.</summary>
    public void Die()
    {
        if (IsDead) return;
        rejoinAt = Time.time + RejoinDelay;
        gameObject.SetActive(false);
    }

    /// <summary>Rejoins at the leader with full HP (also used by the roster timer).</summary>
    public void Rejoin()
    {
        gameObject.SetActive(true);
        if (leader != null)
        {
            transform.position = leader.position;
            transform.rotation = leader.rotation;
        }
        if (hp == null) hp = GetComponent<PlayerHealth>();
        if (hp != null) hp.Heal(hp.MaxHP);
        rejoinAt = float.NegativeInfinity;
    }

    /// <summary>True while dead and due back.</summary>
    public bool RejoinDue => IsDead && Time.time >= rejoinAt;
}
