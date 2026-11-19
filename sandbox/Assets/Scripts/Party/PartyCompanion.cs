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
    /// <summary>Follow distance to the leader.</summary>
    public const float FollowRange = 3f;
    /// <summary>Husks near the leader (not the companion) are engaged, so the party stays together.</summary>
    public const float EngageRange = 12f;
    /// <summary>Further than this from the leader, the companion catches up by teleport.</summary>
    public const float LeashRange = 15f;
    /// <summary>Companion foot speed.</summary>
    public const float MoveSpeed = 5f;
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

    /// <summary>Time.time when a dead companion may rejoin; negative while alive.</summary>
    float rejoinAt = float.NegativeInfinity;

    /// <summary>True while waiting out death at camp.</summary>
    public bool IsDead => rejoinAt > 0f;

    void Start()
    {
        skills = GetComponent<DiabloSkills>();
        melee = GetComponent<DiabloMelee>();
        hp = GetComponent<PlayerHealth>();
        if (skills != null) skills.classOverride = Role;
    }

    void Update()
    {
        if (leader == null || hp == null) return;
        if (!IsDead && hp.CurrentHP <= 0f) return; // death is routed through Die()
        var toLeader = leader.position - transform.position;
        toLeader.y = 0f;
        if (toLeader.magnitude > LeashRange)
        {
            transform.position = leader.position;
            transform.rotation = leader.rotation;
            return;
        }
        var target = NearestHuskNearLeader();
        bool acted = target != null && ActByRole(target);
        if (!acted && toLeader.magnitude > FollowRange)
        {
            var step = toLeader.normalized * Mathf.Min(MoveSpeed * Time.deltaTime, toLeader.magnitude - FollowRange);
            transform.position += step;
            if (step.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(new Vector3(step.x, 0f, step.z));
        }
        else if (target != null)
        {
            Face(target.transform.position);
        }
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
        if (role == ClassDefinition.PartyRole.Tank)
        {
            if (dist <= skills.slamRadius && skills.CooldownLeft(DiabloSkills.Skill.SeismicSlam) <= 0f)
                return skills.CastToward(DiabloSkills.Skill.SeismicSlam, dir);
            if (dist <= meleeRange() && melee != null)
                return melee.TryConeAttack(transform, dir) > 0;
            Approach(target, dir, dist);
            return true;
        }
        if (role == ClassDefinition.PartyRole.Dps)
        {
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
        if (fromLeader.magnitude > LeashRange) return; // never outrun the party
        transform.position = next;
        Face(target.transform.position);
    }

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
