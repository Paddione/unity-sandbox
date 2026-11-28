using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>Shotcheck part: Class ability VFX poses (-ability-vfx-shots).</summary>
public sealed partial class DiabloShotCheck
{
    IEnumerator AbilityVfxShots(DiabloClickController hero, DiabloSkills skills, Vector3 camFwd, Vector3 camRight)
    {
        Vector3 anchor = hero.transform.position;
        Vector3 aim = camRight;
        hero.distance = 8.5f;
        hero.pitch = 44f;

        var dummy = new GameObject("ShotDummyHusk");
        dummy.tag = "Husk";
        dummy.transform.position = anchor + aim * 3.5f;
        var dummyHp = dummy.AddComponent<EnemyHealth>();
        dummyHp.SetTier(5000, LootTier.Normal);

        var ally = new GameObject("ShotDummyAlly");
        ally.transform.position = anchor + aim * 2.2f + camFwd * 1.4f;
        var allyHp = ally.AddComponent<PlayerHealth>();

        var look = hero.GetComponent<ClassAppearance>();

        // --- Fenwalker (6) ---
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        if (look != null) look.Apply(ClassCatalog.Fenwalker);
        TeleportHero(hero, anchor);
        Face(hero, aim);
        yield return Wait(0.25f);

        KitCast(skills, DiabloSkills.Skill.Cleave, aim);
        yield return Wait(0.14f);
        yield return Shot("fenwalker_01_cleave");
        yield return Wait(0.35f);

        KitCast(skills, DiabloSkills.Skill.MireHook, aim);
        yield return Wait(0.10f);
        yield return Shot("fenwalker_02_mirehook");
        yield return Wait(0.35f);

        KitCast(skills, DiabloSkills.Skill.SeismicSlam, aim);
        yield return Wait(0.14f);
        yield return Shot("fenwalker_03_seismicslam");
        yield return Wait(0.45f);

        TeleportHero(hero, anchor);
        KitCast(skills, DiabloSkills.Skill.BogCharge, aim);
        yield return Wait(0.14f);
        yield return Shot("fenwalker_04_bogcharge");
        yield return Wait(0.35f);

        TeleportHero(hero, anchor);
        KitCast(skills, DiabloSkills.Skill.MireGround, aim);
        yield return Wait(0.28f);
        yield return Shot("fenwalker_05_mireground");
        yield return Wait(0.35f);

        KitCast(skills, DiabloSkills.Skill.IronVow, aim);
        yield return Wait(0.25f);
        yield return Shot("fenwalker_06_ironvow");
        ClearActiveAbilityVfx();
        yield return Wait(0.15f);

        // --- Lanternbearer (6) ---
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        if (look != null) look.Apply(ClassCatalog.Lanternbearer);
        TeleportHero(hero, anchor);
        Face(hero, aim);
        yield return Wait(0.25f);

        KitCast(skills, DiabloSkills.Skill.LanternBolt, aim);
        yield return Wait(0.14f);
        yield return Shot("lanternbearer_01_lanternbolt");
        yield return Wait(0.35f);

        KitCast(skills, DiabloSkills.Skill.Wisps, aim);
        yield return Wait(0.45f);
        yield return Shot("lanternbearer_02_wisps");
        yield return Wait(0.35f);

        KitCast(skills, DiabloSkills.Skill.SearingHalo, aim);
        yield return Wait(0.16f);
        yield return Shot("lanternbearer_03_searinghalo");
        yield return Wait(0.45f);

        TeleportHero(hero, anchor);
        KitCast(skills, DiabloSkills.Skill.VeilStep, aim);
        yield return Wait(0.12f);
        yield return Shot("lanternbearer_04_veilstep");
        yield return Wait(0.35f);

        TeleportHero(hero, anchor);
        allyHp.Heal(100f);
        allyHp.TakeDamage(5f);
        KitCast(skills, DiabloSkills.Skill.MendingLight, aim);
        yield return Wait(0.20f);
        yield return Shot("lanternbearer_05_mendinglight");
        yield return Wait(0.55f);

        KitCast(skills, DiabloSkills.Skill.RenewingCircle, aim);
        yield return Wait(0.22f);
        yield return Shot("lanternbearer_06_renewingcircle");
        ClearActiveAbilityVfx();
        yield return Wait(0.15f);

        // --- Rogue (6) ---
        RunProfile.ClassId = ClassCatalog.RogueId;
        if (look != null) look.Apply(ClassCatalog.Rogue);
        Wisp.ResetRun();
        TeleportHero(hero, anchor);
        Face(hero, aim);
        yield return Wait(0.25f);

        KitCast(skills, DiabloSkills.Skill.QuickShot, aim);
        yield return Wait(0.10f);
        yield return Shot("rogue_01_quickshot");
        yield return Wait(0.35f);

        KitCast(skills, DiabloSkills.Skill.BladeFlurry, aim);
        yield return Wait(0.10f);
        yield return Shot("rogue_02_bladeflurry");
        yield return Wait(0.35f);

        KitCast(skills, DiabloSkills.Skill.SmokePot, aim);
        yield return Wait(0.32f);
        yield return Shot("rogue_03_smokepot");
        yield return Wait(0.35f);

        TeleportHero(hero, anchor);
        KitCast(skills, DiabloSkills.Skill.ShadowRoll, aim);
        yield return Wait(0.12f);
        yield return Shot("rogue_04_shadowroll");
        yield return Wait(0.35f);

        TeleportHero(hero, anchor);
        KitCast(skills, DiabloSkills.Skill.VenomVial, aim);
        yield return Wait(0.22f);
        yield return Shot("rogue_05_venomvial");
        yield return Wait(0.45f);

        var keg = ClassKitSkills.PowderKegCast(skills, aim, cosmetic: false);
        yield return Wait(0.20f);
        if (keg != null) keg.Detonate(false);
        yield return Wait(0.10f);
        yield return Shot("rogue_06_powderkeg_blast");

        DestroyImmediate(dummy);
        DestroyImmediate(ally);
        RunProfile.Reset();
    }

    static void ClearActiveAbilityVfx()
    {
        foreach (var anim in FindObjectsByType<AbilityVfxAnim>(FindObjectsSortMode.None))
            if (anim != null) Destroy(anim.gameObject);
        foreach (var m in MireZone.Active.ToArray())
            if (m != null) m.Kill();
        foreach (var sc in SmokeCloud.Active.ToArray())
            if (sc != null) sc.Kill();
        Wisp.ResetRun();
    }
}
