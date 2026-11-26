using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Execution of the Fenwalker (Mire Hook, Bog Charge, Mire Ground, Iron Vow)
/// and Rogue (Smoke Pot, Shadow Roll, Venom Vial, Powder Keg) kit skills
/// (cinderreach-class-kits-parity). <see cref="DiabloSkills.CastToward"/>
/// gates class, cooldown and the co-op intent path, then calls
/// <see cref="Execute"/>. Cosmetic calls (co-op intent clients) only draw
/// and move the caster's own hero; mirrors use <see cref="ShowRemote"/>.
/// Damage, statuses and threat only ever apply on the solo/host side.
/// Tuning and combo formulas live in <see cref="ClassCombos"/>.
/// </summary>
public static class ClassKitSkills
{
    /// <summary>Cone half-angle for throw/hook auto-aim (degrees).</summary>
    public const float ThrowConeHalfAngle = 35f;
    /// <summary>Without a husk in the cone a throw lands at this share of its range.</summary>
    public const float ThrowFallbackShare = 0.6f;

    public static bool Handles(DiabloSkills.Skill s) =>
        s >= DiabloSkills.Skill.MireHook && s <= DiabloSkills.Skill.PowderKeg;

    public static void Execute(DiabloSkills skills, DiabloSkills.Skill s, Vector3 dir, bool cosmetic)
    {
        dir = Flat(dir, skills.transform);
        skills.transform.rotation = Quaternion.LookRotation(dir);
        switch (s)
        {
            case DiabloSkills.Skill.MireHook: MireHook(skills, dir, cosmetic); break;
            case DiabloSkills.Skill.BogCharge: BogCharge(skills, dir, cosmetic); break;
            case DiabloSkills.Skill.MireGround: MireGround(skills, cosmetic); break;
            case DiabloSkills.Skill.IronVow: IronVow(skills, cosmetic); break;
            case DiabloSkills.Skill.SmokePot: SmokePot(skills, dir, cosmetic); break;
            case DiabloSkills.Skill.ShadowRoll: ShadowRoll(skills, dir, cosmetic); break;
            case DiabloSkills.Skill.VenomVial: VenomVial(skills, dir, cosmetic); break;
            case DiabloSkills.Skill.PowderKeg: PowderKegCast(skills, dir, cosmetic); break;
        }
    }

    /// <summary>Mirror rendering of another player's kit cast: light and ring only.</summary>
    public static void ShowRemote(DiabloSkills skills, DiabloSkills.Skill s, Vector3 origin, Vector3 dir)
    {
        if (!Application.isPlaying) return;
        dir = Flat(dir, skills.transform);
        var c = CastColor(s);
        switch (s)
        {
            case DiabloSkills.Skill.MireGround:
                ComboVfx.Pool(skills.transform.position, skills.ScaledArea(ClassCombos.MireGroundRadius), 1f, c); break;
            case DiabloSkills.Skill.SmokePot:
            case DiabloSkills.Skill.VenomVial:
            case DiabloSkills.Skill.PowderKeg:
                ComboVfx.Flash(ThrowTarget(skills.transform, dir, RangeOf(s)), c, 4f, 0.5f); break;
            default:
                ComboVfx.Flash(origin, c, 4f, 0.4f); break;
        }
    }

    // ------------------------------------------------------------ Fenwalker

    /// <summary>
    /// Mire Hook (E): chain the first husk on the line, pull it in front of
    /// the tank (bosses hold their ground), mire it and call it out (stun
    /// threat). moorvogt-anchor drags every mired husk around the target too.
    /// Returns the hooked husk (null on a miss).
    /// </summary>
    public static GameObject MireHook(DiabloSkills skills, Vector3 dir, bool cosmetic)
    {
        var hero = skills.transform;
        var from = hero.position;
        var to = from + dir * ClassCombos.MireHookRange;
        GameObject target = null;
        float bestT = float.MaxValue;
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk == null || husk.GetComponent<EnemyHealth>() == null) continue;
            float d = ClassCombos.SegmentDistance(from, to, husk.transform.position, out float t);
            if (d <= ClassCombos.MireHookHitRadius && t < bestT) { bestT = t; target = husk; }
        }
        if (Application.isPlaying)
        {
            var end = target != null ? target.transform.position : to;
            for (int i = 1; i <= 4; i++) PresentationVfx.ArrowTrail(Vector3.Lerp(from, end, i / 4f) + Vector3.up);
        }
        if (cosmetic) return target;
        DiabloAudio.PlayMireHook();
        skills.PlayStrike();
        if (target == null) return null;
        var eq = skills.GetComponent<Equipment>();
        var origin = target.transform.position;
        Pull(target, from + dir * ClassCombos.MireHookPullTo);
        skills.ArmKeenFor(DiabloSkills.Skill.MireHook);
        bool crit = DiabloSkills.ApplyHit(target, skills.EffectiveDamage(DiabloSkills.Skill.MireHook), from, 0f, hero);
        skills.TryKeenReduction(DiabloSkills.Skill.MireHook, crit);
        EnemyStatus.Mire(target, ClassCombos.MiredSeconds);
        ThreatTable.BookStun(target, hero);
        if (BuildUniqueEffects.HasMoorvogtAnchor(eq))
        {
            int n = 0;
            foreach (var husk in ClassCombos.HusksInRadius(origin, BuildUniqueEffects.AnchorDragRadius))
            {
                if (husk == target || !EnemyStatus.IsMired(husk)) continue;
                n++;
                var side = Quaternion.AngleAxis(n % 2 == 0 ? 40f * n : -40f * n, Vector3.up) * dir;
                Pull(husk, from + side * (ClassCombos.MireHookPullTo + 0.4f));
                ThreatTable.BookStun(husk, hero);
            }
        }
        return target;
    }

    /// <summary>
    /// Bog Charge (Space): shoulder-charge up to 6 m (walls stop it), every
    /// husk in the lane takes the blow, a small shove and is mired.
    /// moorvogt-anchor stuns husks that were already mired. Returns hits.
    /// </summary>
    public static int BogCharge(DiabloSkills skills, Vector3 dir, bool cosmetic)
    {
        var hero = skills.transform;
        var from = hero.position;
        float dist = DiabloSkills.VeilClearDistance(from, dir, ClassCombos.BogChargeDistance, hero);
        var cc = hero.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        hero.position = from + dir * dist;
        if (cc != null) cc.enabled = true;
        if (Application.isPlaying)
        {
            PresentationVfx.CleaveArc(hero.position, 1.5f);
            ComboVfx.Flash(hero.position, CastColor(DiabloSkills.Skill.BogCharge), 4f, 0.35f);
        }
        if (cosmetic) return 0;
        DiabloAudio.PlayBogCharge();
        skills.PlayStrike();
        var eq = skills.GetComponent<Equipment>();
        int blow = skills.EffectiveDamage(DiabloSkills.Skill.BogCharge);
        float width = skills.ScaledArea(ClassCombos.BogChargeWidth);
        int hits = 0;
        skills.ArmKeenFor(DiabloSkills.Skill.BogCharge);
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk == null || husk.GetComponent<EnemyHealth>() == null) continue;
            if (ClassCombos.SegmentDistance(from, hero.position, husk.transform.position, out _) > width) continue;
            bool wasMired = EnemyStatus.IsMired(husk);
            bool crit = DiabloSkills.ApplyHit(husk, blow, from, ClassCombos.BogChargeKnockback, hero);
            skills.TryKeenReduction(DiabloSkills.Skill.BogCharge, crit);
            EnemyStatus.Mire(husk, ClassCombos.MiredSeconds);
            if (wasMired && BuildUniqueEffects.HasMoorvogtAnchor(eq))
            {
                var atk = husk.GetComponent<EnemyAttack>();
                if (atk != null) atk.Stun(BuildUniqueEffects.AnchorChargeStun);
                ThreatTable.BookStun(husk, hero);
            }
            hits++;
        }
        return hits;
    }

    /// <summary>Mire Ground (F): a bog pool at the tank's feet, ticking damage and mire.</summary>
    public static MireZone MireGround(DiabloSkills skills, bool cosmetic)
    {
        float radius = skills.ScaledArea(ClassCombos.MireGroundRadius);
        if (cosmetic)
        {
            if (Application.isPlaying) ComboVfx.Pool(skills.transform.position, radius, 1f, CastColor(DiabloSkills.Skill.MireGround));
            return null;
        }
        DiabloAudio.PlayMireGround();
        return MireZone.Spawn(skills.transform, skills.transform.position,
            skills.EffectiveDamage(DiabloSkills.Skill.MireGround), radius, ClassCombos.MireGroundDuration);
    }

    /// <summary>
    /// Iron Vow (G): absorb shield, taunt every husk in 8 m and open the
    /// Retribution store — damage taken meanwhile charges the next Cleave.
    /// Returns the husks taunted.
    /// </summary>
    public static int IronVow(DiabloSkills skills, bool cosmetic)
    {
        var hero = skills.transform;
        if (Application.isPlaying) ComboVfx.Flash(hero.position, CastColor(DiabloSkills.Skill.IronVow), 6f, 0.6f);
        if (cosmetic) return 0;
        DiabloAudio.PlayIronVow();
        var hp = hero.GetComponent<PlayerHealth>();
        if (hp != null) hp.GrantShield(ClassCombos.IronVowShield, ClassCombos.IronVowSeconds);
        HeroComboState.For(hero.gameObject).BeginVow(ClassCombos.IronVowSeconds);
        int n = 0;
        foreach (var husk in ClassCombos.HusksInRadius(hero.position, skills.ScaledArea(ClassCombos.IronVowTauntRadius)))
        {
            ThreatTable.BookStun(husk, hero);
            n++;
        }
        return n;
    }

    // ------------------------------------------------------------ Rogue

    /// <summary>Smoke Pot (R): a cloud at the throw point that blinds husks inside.</summary>
    public static SmokeCloud SmokePot(DiabloSkills skills, Vector3 dir, bool cosmetic)
    {
        var at = ThrowTarget(skills.transform, dir, ClassCombos.SmokeRange);
        float radius = skills.ScaledArea(ClassCombos.SmokeRadius);
        if (cosmetic)
        {
            if (Application.isPlaying) ComboVfx.Pool(at, radius, 1f, CastColor(DiabloSkills.Skill.SmokePot));
            return null;
        }
        DiabloAudio.PlaySmokePot();
        return SmokeCloud.Spawn(skills.transform, at, radius, ClassCombos.SmokeDuration);
    }

    /// <summary>
    /// Shadow Roll (Space): a 5 m roll that sheds the husks' threat on the
    /// rogue and arms the Ambush (next damaging hit crits).
    /// </summary>
    public static float ShadowRoll(DiabloSkills skills, Vector3 dir, bool cosmetic)
    {
        var hero = skills.transform;
        var from = hero.position;
        float dist = DiabloSkills.VeilClearDistance(from, dir, ClassCombos.ShadowRollDistance, hero);
        var cc = hero.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        hero.position = from + dir * dist;
        if (cc != null) cc.enabled = true;
        if (Application.isPlaying)
        {
            PresentationVfx.VeilPuff(from);
            ComboVfx.Flash(hero.position, CastColor(DiabloSkills.Skill.ShadowRoll), 3f, 0.3f);
        }
        if (cosmetic) return dist;
        DiabloAudio.PlayShadowRoll();
        HeroComboState.For(hero.gameObject).ArmAmbush(ClassCombos.AmbushWindow(skills.GetComponent<Equipment>()));
        return dist;
    }

    /// <summary>Venom Vial (F): splash at the throw point, 1 damage and 2 venom stacks per husk.</summary>
    public static int VenomVial(DiabloSkills skills, Vector3 dir, bool cosmetic)
    {
        var hero = skills.transform;
        var at = ThrowTarget(hero, dir, ClassCombos.VenomRange);
        float radius = skills.ScaledArea(ClassCombos.VenomRadius);
        if (Application.isPlaying)
        {
            PresentationVfx.CleaveArc(at, radius);
            ComboVfx.Flash(at, CastColor(DiabloSkills.Skill.VenomVial), radius * 2f, 0.5f);
        }
        if (cosmetic) return 0;
        DiabloAudio.PlayVenomVial();
        int blow = skills.EffectiveDamage(DiabloSkills.Skill.VenomVial);
        var hits = ClassCombos.HusksInRadius(at, radius);
        skills.ArmKeenFor(DiabloSkills.Skill.VenomVial);
        foreach (var husk in hits)
        {
            bool crit = DiabloSkills.ApplyHit(husk, blow, at, 0f, hero);
            skills.TryKeenReduction(DiabloSkills.Skill.VenomVial, crit);
            EnemyStatus.AddVenom(husk, hero, ClassCombos.VenomStacksPerVial);
        }
        return hits.Count;
    }

    /// <summary>Powder Keg (G): a keg on a 1.5 s fuse at the throw point (smugglers-fuse: instant in smoke).</summary>
    public static PowderKeg PowderKegCast(DiabloSkills skills, Vector3 dir, bool cosmetic)
    {
        var hero = skills.transform;
        var at = ThrowTarget(hero, dir, ClassCombos.PowderKegRange);
        if (cosmetic)
        {
            if (Application.isPlaying) ComboVfx.Flash(at, CastColor(DiabloSkills.Skill.PowderKeg), 3f, 0.4f);
            return null;
        }
        DiabloAudio.PlayTwang();
        var keg = PowderKeg.Place(hero, at, skills.EffectiveDamage(DiabloSkills.Skill.PowderKeg),
            skills.ScaledArea(ClassCombos.PowderKegRadius), ClassCombos.PowderKegFuse);
        if (BuildUniqueEffects.HasSmugglersFuse(skills.GetComponent<Equipment>()) && SmokeCloud.Covers(at))
            keg.Detonate(false);
        return keg;
    }

    // ------------------------------------------------------------ helpers

    /// <summary>
    /// Throw landing point: the nearest living husk within <see cref="ThrowConeHalfAngle"/>
    /// of the aim and in range, else <see cref="ThrowFallbackShare"/> of the range
    /// along the aim. Host and client agree because only dir is networked.
    /// </summary>
    public static Vector3 ThrowTarget(Transform hero, Vector3 dir, float range)
    {
        var from = hero.position;
        dir = Flat(dir, hero);
        GameObject best = null;
        float bestD = float.MaxValue;
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
        {
            if (husk == null || husk.GetComponent<EnemyHealth>() == null) continue;
            var to = husk.transform.position - from;
            to.y = 0f;
            float d = to.magnitude;
            if (d > range || d < 0.01f) continue;
            if (Vector3.Angle(dir, to) > ThrowConeHalfAngle) continue;
            if (d < bestD) { bestD = d; best = husk; }
        }
        if (best != null)
        {
            var p = best.transform.position;
            p.y = from.y;
            return p;
        }
        return ClassCombos.ThrowPoint(from, dir, range * ThrowFallbackShare, range);
    }

    static float RangeOf(DiabloSkills.Skill s) =>
        s == DiabloSkills.Skill.SmokePot ? ClassCombos.SmokeRange
        : s == DiabloSkills.Skill.VenomVial ? ClassCombos.VenomRange
        : ClassCombos.PowderKegRange;

    static Vector3 Flat(Vector3 dir, Transform fallback)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = fallback.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        return dir.normalized;
    }

    /// <summary>Pull a husk to a point (bosses and act bosses hold their ground).</summary>
    static void Pull(GameObject husk, Vector3 point)
    {
        if (husk == null || ClassCombos.IsBossLike(husk)) return;
        point.y = husk.transform.position.y;
        var cc = husk.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        husk.transform.position = point;
        if (cc != null) cc.enabled = true;
    }

    public static Color CastColor(DiabloSkills.Skill s)
    {
        switch (s)
        {
            case DiabloSkills.Skill.MireHook: return new Color(0.6f, 0.65f, 0.7f);
            case DiabloSkills.Skill.BogCharge: return new Color(0.7f, 0.6f, 0.45f);
            case DiabloSkills.Skill.MireGround: return new Color(0.35f, 0.3f, 0.15f);
            case DiabloSkills.Skill.IronVow: return new Color(0.75f, 0.8f, 1f);
            case DiabloSkills.Skill.SmokePot: return new Color(0.55f, 0.55f, 0.6f);
            case DiabloSkills.Skill.ShadowRoll: return new Color(0.4f, 0.3f, 0.6f);
            case DiabloSkills.Skill.VenomVial: return new Color(0.45f, 1f, 0.3f);
            default: return new Color(1f, 0.55f, 0.2f);
        }
    }
}
