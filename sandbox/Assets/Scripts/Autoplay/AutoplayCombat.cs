using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Autoplay-only combat profile, target selection, and husk census helpers
/// extracted from <see cref="SessionAutoplay"/> (CA-05). Keeps the session
/// driver focused on route orchestration while preserving exact combat order.
/// </summary>
public static class AutoplayCombat
{
    public static float Hp(GameObject go)
    {
        var h = go != null ? go.GetComponent<EnemyHealth>() : null;
        return h != null ? h.CurrentHP : 0f;
    }

    public static bool IsBoss(GameObject go) =>
        go != null && (go.GetComponent<BossController>() != null || go.GetComponent<KilnmotherController>() != null
            || go.GetComponent<GlasswrightController>() != null || go.name == StoryIds.Thewes);

    public static int CountHusks(bool excludeBoss)
    {
        int n = 0;
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk == null || husk.GetComponent<EnemyHealth>() == null) continue;
            if (excludeBoss && IsBoss(husk)) continue;
            n++;
        }
        return n;
    }

    /// <summary>Diagnostic: remaining husks, nearest planar/3D distance and its position.</summary>
    public static string HuskCensus(Vector3 from, bool excludeBoss)
    {
        int n = 0;
        float best = float.PositiveInfinity;
        Vector3 at = Vector3.zero;
        string name = "-";
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk == null || husk.GetComponent<EnemyHealth>() == null) continue;
            if (excludeBoss && IsBoss(husk)) continue;
            n++;
            float d = Vector3.Distance(from, husk.transform.position);
            if (d < best) { best = d; at = husk.transform.position; name = husk.name; }
        }
        return n == 0 ? "census: none left" : $"census: {n} left, nearest {name} {best:0.0} m at ({at.x:0},{at.y:0},{at.z:0})";
    }

    public static GameObject NearestHusk(Vector3 from, float maxDist, bool excludeBoss, HashSet<GameObject> aside = null)
    {
        GameObject best = null;
        float bestDist = maxDist;
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk == null || husk.GetComponent<EnemyHealth>() == null) continue;
            if (excludeBoss && IsBoss(husk)) continue;
            if (aside != null && aside.Contains(husk)) continue;
            float d = Vector3.Distance(from, husk.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = husk;
            }
        }
        return best;
    }

    public static GameObject NearestHusk(Vector3 at) =>
        NearestHusk(at, 1.5f, excludeBoss: false);

    public static GameObject GlassPillarOr(GameObject boss)
    {
        foreach (var h in GameObject.FindGameObjectsWithTag("Husk"))
            if (h != null && h.name.StartsWith(GlasswrightController.PillarPrefix) && h.GetComponent<EnemyHealth>() != null)
                return h;
        return boss;
    }

    /// <summary>
    /// Class combat profile (second-class S5, rogue party-roles S6, class-kits-parity).
    /// </summary>
    public static void CastProfile(DiabloSkills skills, Vector3 toTarget, bool inRange, bool boss = false)
    {
        var pos = skills.transform.position;
        float dist = new Vector3(toTarget.x, 0f, toTarget.z).magnitude;
        if (RunProfile.ClassId == ClassCatalog.RogueId)
        {
            CastRogue(skills, toTarget, pos, dist, boss);
            return;
        }
        if (RunProfile.ClassId == ClassCatalog.LanternbearerId)
        {
            CastLanternbearer(skills, toTarget, inRange, boss);
            return;
        }
        CastFenwalker(skills, toTarget, pos, dist, inRange, boss);
    }

    static bool TryCast(DiabloSkills skills, DiabloSkills.Skill skill, Vector3 dir) =>
        skills.CooldownLeft(skill) <= 0f && skills.CastToward(skill, dir);

    static void CastRogue(DiabloSkills skills, Vector3 toTarget, Vector3 pos, float dist, bool boss)
    {
        var target = NearestHusk(pos + toTarget);
        int close = ClassCombos.HusksInRadius(pos, 2.2f).Count;
        if (close >= 2 && TryCast(skills, DiabloSkills.Skill.ShadowRoll, -toTarget)) return;
        if (target != null && EnemyStatus.Venom(target) >= ClassCombos.VenomStacksPerVial
            && dist <= ClassCombos.PowderKegRange && TryCast(skills, DiabloSkills.Skill.PowderKeg, toTarget)) return;
        if (dist <= ClassCombos.VenomRange && TryCast(skills, DiabloSkills.Skill.VenomVial, toTarget)) return;
        if (dist <= ClassCombos.SmokeRange
            && (boss || ClassCombos.HusksInRadius(pos + toTarget, ClassCombos.SmokeRadius).Count >= 2)
            && TryCast(skills, DiabloSkills.Skill.SmokePot, toTarget)) return;
        if (ClassCombos.HusksInRadius(pos, skills.flurryRadius).Count >= (boss ? 1 : 2)
            && TryCast(skills, DiabloSkills.Skill.BladeFlurry, toTarget)) return;
        TryCast(skills, DiabloSkills.Skill.QuickShot, toTarget);
    }

    static void CastLanternbearer(DiabloSkills skills, Vector3 toTarget, bool inRange, bool boss)
    {
        // coop-hp-sync: mend hurt allies first (co-op mirrors show their owner's HP).
        if (HurtAllies(skills) >= 2 && TryCast(skills, DiabloSkills.Skill.RenewingCircle, toTarget)) return;
        if (TryCast(skills, DiabloSkills.Skill.MendingLight, toTarget)) return;
        if (TryCast(skills, DiabloSkills.Skill.Wisps, toTarget)) return;
        int near = 0;
        foreach (var h in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (h == null || h.GetComponent<EnemyHealth>() == null) continue;
            var d = h.transform.position - skills.transform.position;
            d.y = 0f;
            if (d.magnitude <= skills.haloRadius) near++;
        }
        if ((near >= 3 || (boss && inRange)) && TryCast(skills, DiabloSkills.Skill.SearingHalo, toTarget)) return;
        TryCast(skills, DiabloSkills.Skill.LanternBolt, toTarget);
    }

    static void CastFenwalker(DiabloSkills skills, Vector3 toTarget, Vector3 pos, float dist, bool inRange, bool boss)
    {
        var hp = skills.GetComponent<PlayerHealth>();
        if (hp != null && hp.CurrentHP < hp.MaxHP * 0.7f && TryCast(skills, DiabloSkills.Skill.IronVow, toTarget)) return;
        if (boss && inRange && TryCast(skills, DiabloSkills.Skill.IronVow, toTarget)) return;
        int nearPack = ClassCombos.HusksInRadius(pos, ClassCombos.MireGroundRadius).Count;
        if (nearPack >= 2 && TryCast(skills, DiabloSkills.Skill.MireGround, toTarget)) return;
        if ((nearPack >= 2 || (boss && inRange)) && dist <= skills.slamRadius
            && TryCast(skills, DiabloSkills.Skill.SeismicSlam, toTarget)) return;
        if (dist <= skills.CleaveRadius && TryCast(skills, DiabloSkills.Skill.Cleave, toTarget)) return;
        if (dist > 3f && dist <= ClassCombos.BogChargeDistance
            && TryCast(skills, DiabloSkills.Skill.BogCharge, toTarget)) return;
        if (dist > 2.5f && dist <= ClassCombos.MireHookRange)
            TryCast(skills, DiabloSkills.Skill.MireHook, toTarget);
    }

    /// <summary>Hurt living allies (not the caster) inside the group-heal radius.</summary>
    public static int HurtAllies(DiabloSkills skills)
    {
        int n = 0;
        foreach (var hp in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            if (hp == null || hp.transform == skills.transform || !hp.gameObject.activeInHierarchy) continue;
            if (hp.CurrentHP <= 0f || hp.CurrentHP >= hp.MaxHP) continue;
            var d = hp.transform.position - skills.transform.position;
            d.y = 0f;
            if (d.magnitude <= skills.groupHealRadius) n++;
        }
        return n;
    }
}
