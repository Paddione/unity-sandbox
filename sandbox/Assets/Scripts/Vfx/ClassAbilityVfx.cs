using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime facade for all 18 Cinderreach class ability VFX (+ variants).
/// Instantiates Addressable/Resources prefabs under SkillVfx/ in play mode,
/// drives MaterialPropertyBlock animations via <see cref="AbilityVfxAnim"/>,
/// respects <see cref="PresentationVfx.MaxBudget"/> and keeps headless-safe
/// per-skill counters for automated verification.
/// </summary>
public static class ClassAbilityVfx
{
    public const string Cleave360Address = "SkillVfx/FenCleave360";
    public const string CleaveCone90Address = "SkillVfx/FenCleaveCone90";
    public const string MireHookAddress = "SkillVfx/FenMireHook";
    public const string SeismicSlamAddress = "SkillVfx/FenSeismicSlam";
    public const string BogChargeAddress = "SkillVfx/FenBogCharge";
    public const string MireGroundAddress = "SkillVfx/FenMireGround";
    public const string IronVowAddress = "SkillVfx/FenIronVow";

    public const string LanternBoltAddress = "SkillVfx/LanternBoltVfx";
    public const string LanternWispAddress = "SkillVfx/LanternWispVfx";
    public const string LanternWispStrikeAddress = "SkillVfx/LanternWispStrike";
    public const string SearingHaloAddress = "SkillVfx/LanternSearingHalo";
    public const string SearingHaloWardAddress = "SkillVfx/LanternHaloWard";
    public const string VeilStepAddress = "SkillVfx/LanternVeilStep";
    public const string MendingLightAddress = "SkillVfx/LanternMendingLight";
    public const string RenewingCircleAddress = "SkillVfx/LanternRenewingCircle";

    public const string QuickShotTrailAddress = "SkillVfx/RogueQuickShotTrail";
    public const string BladeFlurryAddress = "SkillVfx/RogueBladeFlurry";
    public const string SmokePotAddress = "SkillVfx/RogueSmokePot";
    public const string ShadowRollAddress = "SkillVfx/RogueShadowRoll";
    public const string VenomVialAddress = "SkillVfx/RogueVenomVial";
    public const string PowderKegBodyAddress = "SkillVfx/RoguePowderKegBody";
    public const string PowderKegBlastAddress = "SkillVfx/RoguePowderKegBlast";

    static readonly Dictionary<DiabloSkills.Skill, int> spawnCounts = new Dictionary<DiabloSkills.Skill, int>();

    public static int TotalSpawns
    {
        get
        {
            int total = 0;
            foreach (var kv in spawnCounts) total += kv.Value;
            return total;
        }
    }

    public static int CountFor(DiabloSkills.Skill skill) =>
        spawnCounts.TryGetValue(skill, out int n) ? n : 0;

    public static void ResetRun() => spawnCounts.Clear();

    public static string PrefabAddressFor(DiabloSkills.Skill skill)
    {
        switch (skill)
        {
            case DiabloSkills.Skill.Cleave: return Cleave360Address;
            case DiabloSkills.Skill.MireHook: return MireHookAddress;
            case DiabloSkills.Skill.SeismicSlam: return SeismicSlamAddress;
            case DiabloSkills.Skill.BogCharge: return BogChargeAddress;
            case DiabloSkills.Skill.MireGround: return MireGroundAddress;
            case DiabloSkills.Skill.IronVow: return IronVowAddress;
            case DiabloSkills.Skill.LanternBolt: return LanternBoltAddress;
            case DiabloSkills.Skill.Wisps: return LanternWispAddress;
            case DiabloSkills.Skill.SearingHalo: return SearingHaloAddress;
            case DiabloSkills.Skill.VeilStep: return VeilStepAddress;
            case DiabloSkills.Skill.MendingLight: return MendingLightAddress;
            case DiabloSkills.Skill.RenewingCircle: return RenewingCircleAddress;
            case DiabloSkills.Skill.QuickShot: return QuickShotTrailAddress;
            case DiabloSkills.Skill.BladeFlurry: return BladeFlurryAddress;
            case DiabloSkills.Skill.SmokePot: return SmokePotAddress;
            case DiabloSkills.Skill.ShadowRoll: return ShadowRollAddress;
            case DiabloSkills.Skill.VenomVial: return VenomVialAddress;
            case DiabloSkills.Skill.PowderKeg: return PowderKegBodyAddress;
            default: return null;
        }
    }

    static void Book(DiabloSkills.Skill skill)
    {
        spawnCounts[skill] = CountFor(skill) + 1;
    }

    static GameObject SpawnPrefab(string address, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (!Application.isPlaying) return null;
        if (PresentationVfx.ActiveParticles() >= PresentationVfx.MaxBudget) return null;
        var prefab = Res.Get<GameObject>(address);
        if (prefab == null) return null;
        return parent != null
            ? Object.Instantiate(prefab, pos, rot, parent)
            : Object.Instantiate(prefab, pos, rot);
    }

    // ---------------- Fenwalker ----------------

    public static void SpawnCleave(Vector3 pos, Vector3 dir, float radius, float arcDegrees, bool retribution)
    {
        Book(DiabloSkills.Skill.Cleave);
        bool frontal = arcDegrees < 360f;
        var rot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z).normalized) : Quaternion.identity;
        var go = SpawnPrefab(frontal ? CleaveCone90Address : Cleave360Address, pos + Vector3.up * 0.85f, rot);
        if (go == null) return;
        float r = Mathf.Max(0.8f, radius);
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitBurst(0.42f, Vector3.one * (r * 0.65f), Vector3.one * r, frontal ? 0f : 320f, true);
        if (retribution) anim.ApplyTintOverride(DiabloPalette.TrimGold);
    }

    public static void SpawnMireHook(Vector3 origin, Vector3 end, bool hit)
    {
        Book(DiabloSkills.Skill.MireHook);
        var go = SpawnPrefab(MireHookAddress, origin + Vector3.up * 0.9f, Quaternion.identity);
        if (go == null) return;
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitChain(origin + Vector3.up * 0.9f, end + Vector3.up * 0.9f, hit ? 0.38f : 0.25f);
        if (hit) anim.ApplyTintOverride(DiabloPalette.TrimGold);
    }

    public static void SpawnSeismicSlam(Vector3 pos, float radius)
    {
        Book(DiabloSkills.Skill.SeismicSlam);
        var go = SpawnPrefab(SeismicSlamAddress, pos + Vector3.up * 0.05f, Quaternion.identity);
        if (go == null) return;
        float r = Mathf.Max(1f, radius);
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitBurst(0.55f, Vector3.one * (r * 0.35f), Vector3.one * r, 0f, true);
    }

    public static void SpawnBogCharge(Vector3 from, Vector3 to, Vector3 dir)
    {
        Book(DiabloSkills.Skill.BogCharge);
        var flat = new Vector3(dir.x, 0f, dir.z);
        var rot = flat.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(flat.normalized) : Quaternion.identity;
        var mid = Vector3.Lerp(from, to, 0.5f) + Vector3.up * 0.7f;
        var go = SpawnPrefab(BogChargeAddress, mid, rot);
        if (go == null) return;
        float len = Mathf.Clamp((to - from).magnitude * 0.35f, 1.0f, 2.2f);
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitBurst(0.42f, new Vector3(0.9f, 0.9f, len * 0.7f), new Vector3(1.25f, 1.1f, len * 1.15f), 0f, true);
    }

    public static void SpawnMireGround(Vector3 pos, float radius, float duration)
    {
        Book(DiabloSkills.Skill.MireGround);
        var go = SpawnPrefab(MireGroundAddress, pos + Vector3.up * 0.04f, Quaternion.identity);
        if (go == null) return;
        float r = Mathf.Max(1f, radius);
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitZone(duration, new Vector3(r, 1f, r));
    }

    public static void AttachMireGround(Transform parent, float radius, float duration)
    {
        Book(DiabloSkills.Skill.MireGround);
        var go = SpawnPrefab(MireGroundAddress, parent.position + Vector3.up * 0.04f, Quaternion.identity, parent);
        if (go == null) return;
        float r = Mathf.Max(1f, radius);
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitZone(duration, new Vector3(r, 1f, r));
    }

    public static void SpawnIronVow(Transform hero, float tauntRadius, float duration)
    {
        Book(DiabloSkills.Skill.IronVow);
        Vector3 pos = hero != null ? hero.position : Vector3.zero;
        var go = SpawnPrefab(IronVowAddress, pos + Vector3.up * 0.06f, Quaternion.identity, hero);
        if (go == null) return;
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        float scale = Mathf.Clamp(tauntRadius * 0.25f, 1.2f, 2.4f);
        anim.InitZone(Mathf.Min(duration, 2.2f), Vector3.one * scale);
    }

    // ---------------- Lanternbearer ----------------

    public static GameObject InstantiateLanternBolt(Vector3 origin, Vector3 dir)
    {
        Book(DiabloSkills.Skill.LanternBolt);
        var rot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir.normalized) : Quaternion.identity;
        return SpawnPrefab(LanternBoltAddress, origin, rot);
    }

    public static void SpawnPowderKegCosmetic(Vector3 at, float fuseSeconds)
    {
        Book(DiabloSkills.Skill.PowderKeg);
        var go = SpawnPrefab(PowderKegBodyAddress, at, Quaternion.identity);
        if (go == null) return;
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitZone(Mathf.Max(0.25f, fuseSeconds), Vector3.one);
    }

    public static GameObject AttachWispVisual(Transform wispTransform)
    {
        Book(DiabloSkills.Skill.Wisps);
        return SpawnPrefab(LanternWispAddress, wispTransform.position, Quaternion.identity, wispTransform);
    }

    public static void SpawnWispStrike(Vector3 from, Vector3 to)
    {
        Book(DiabloSkills.Skill.Wisps);
        var go = SpawnPrefab(LanternWispStrikeAddress, from, Quaternion.identity);
        if (go == null) return;
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitChain(from, to + Vector3.up * 0.9f, 0.24f);
    }

    public static void SpawnSearingHalo(Vector3 pos, float radius)
    {
        Book(DiabloSkills.Skill.SearingHalo);
        var go = SpawnPrefab(SearingHaloAddress, pos + Vector3.up * 0.06f, Quaternion.identity);
        if (go == null) return;
        float r = Mathf.Max(1f, radius);
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitBurst(0.6f, Vector3.one * (r * 0.55f), Vector3.one * r, 140f, true);
    }

    public static void AttachHaloWard(Transform parent, float radius, float duration)
    {
        Book(DiabloSkills.Skill.SearingHalo);
        var go = SpawnPrefab(SearingHaloWardAddress, parent.position + Vector3.up * 0.05f, Quaternion.identity, parent);
        if (go == null) return;
        float r = Mathf.Max(1f, radius);
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitZone(duration, new Vector3(r, 1f, r));
    }

    public static void SpawnVeilStep(Vector3 from, Vector3 to)
    {
        Book(DiabloSkills.Skill.VeilStep);
        foreach (var pt in new[] { from, to })
        {
            var go = SpawnPrefab(VeilStepAddress, pt + Vector3.up * 0.1f, Quaternion.identity);
            if (go == null) continue;
            var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
            anim.InitBurst(0.45f, Vector3.one * 0.7f, Vector3.one * 1.15f, 90f, true);
        }
    }

    public static void SpawnMendingLight(Vector3 casterPos, Vector3 targetPos)
    {
        Book(DiabloSkills.Skill.MendingLight);
        var go = SpawnPrefab(MendingLightAddress, targetPos + Vector3.up * 0.05f, Quaternion.identity);
        if (go == null) return;
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitBurst(0.85f, new Vector3(0.75f, 0.6f, 0.75f), new Vector3(1.1f, 1.1f, 1.1f), 60f, true);
    }

    public static void SpawnRenewingCircle(Vector3 pos, float radius)
    {
        Book(DiabloSkills.Skill.RenewingCircle);
        var go = SpawnPrefab(RenewingCircleAddress, pos + Vector3.up * 0.05f, Quaternion.identity);
        if (go == null) return;
        float r = Mathf.Max(1.5f, radius);
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitBurst(1.0f, new Vector3(r * 0.6f, 1f, r * 0.6f), new Vector3(r, 1f, r), 35f, true);
    }

    // ---------------- Rogue ----------------

    public static void NoteQuickShotMuzzle(Vector3 origin, Vector3 dir)
    {
        Book(DiabloSkills.Skill.QuickShot);
        var rot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir.normalized) : Quaternion.identity;
        var go = SpawnPrefab(QuickShotTrailAddress, origin, rot);
        if (go == null) return;
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitBurst(0.24f, Vector3.one * 0.9f, Vector3.one * 1.2f, 0f, true);
    }

    public static GameObject AttachQuickShotTrail(Transform projectile)
    {
        if (!Application.isPlaying || projectile == null) return null;
        return SpawnPrefab(QuickShotTrailAddress, projectile.position, projectile.rotation, projectile);
    }

    public static void SpawnBladeFlurry(Vector3 pos, Vector3 dir, float radius)
    {
        Book(DiabloSkills.Skill.BladeFlurry);
        var flat = new Vector3(dir.x, 0f, dir.z);
        var rot = flat.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(flat.normalized) : Quaternion.identity;
        var go = SpawnPrefab(BladeFlurryAddress, pos + Vector3.up * 0.9f, rot);
        if (go == null) return;
        float r = Mathf.Max(1f, radius);
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitBurst(0.32f, Vector3.one * (r * 0.65f), Vector3.one * r, 0f, true);
    }

    public static void SpawnSmokePot(Vector3 pos, float radius, float duration)
    {
        Book(DiabloSkills.Skill.SmokePot);
        var go = SpawnPrefab(SmokePotAddress, pos + Vector3.up * 0.05f, Quaternion.identity);
        if (go == null) return;
        float r = Mathf.Max(1f, radius);
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitZone(duration, new Vector3(r, 1f, r));
    }

    public static void AttachSmokePot(Transform parent, float radius, float duration)
    {
        Book(DiabloSkills.Skill.SmokePot);
        var go = SpawnPrefab(SmokePotAddress, parent.position + Vector3.up * 0.05f, Quaternion.identity, parent);
        if (go == null) return;
        float r = Mathf.Max(1f, radius);
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitZone(duration, new Vector3(r, 1f, r));
    }

    public static void SpawnShadowRoll(Vector3 from, Vector3 to, Vector3 dir)
    {
        Book(DiabloSkills.Skill.ShadowRoll);
        var flat = new Vector3(dir.x, 0f, dir.z);
        var rot = flat.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(flat.normalized) : Quaternion.identity;
        var mid = Vector3.Lerp(from, to, 0.5f) + Vector3.up * 0.55f;
        var go = SpawnPrefab(ShadowRollAddress, mid, rot);
        if (go == null) return;
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitBurst(0.36f, new Vector3(0.8f, 0.8f, 1.0f), new Vector3(1.1f, 1.0f, 1.6f), 0f, true);
    }

    public static void SpawnVenomVial(Vector3 origin, Vector3 target, float radius)
    {
        Book(DiabloSkills.Skill.VenomVial);
        var go = SpawnPrefab(VenomVialAddress, target + Vector3.up * 0.05f, Quaternion.identity);
        if (go == null) return;
        float r = Mathf.Max(1f, radius);
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitBurst(0.75f, new Vector3(r * 0.45f, 1f, r * 0.45f), new Vector3(r, 1f, r), 0f, true);
    }

    public static GameObject AttachPowderKeg(Transform kegTransform)
    {
        Book(DiabloSkills.Skill.PowderKeg);
        return SpawnPrefab(PowderKegBodyAddress, kegTransform.position, Quaternion.identity, kegTransform);
    }

    public static void SpawnPowderKegBlast(Vector3 pos, float radius, bool venomBurn)
    {
        Book(DiabloSkills.Skill.PowderKeg);
        var go = SpawnPrefab(PowderKegBlastAddress, pos + Vector3.up * 0.1f, Quaternion.identity);
        if (go == null) return;
        float r = Mathf.Max(1.2f, radius);
        var anim = go.GetComponent<AbilityVfxAnim>() ?? go.AddComponent<AbilityVfxAnim>();
        anim.InitBurst(0.65f, Vector3.one * (r * 0.45f), Vector3.one * r, 0f, true);
        if (venomBurn) anim.ApplyTintOverride(DiabloPalette.RogueVerdant);
    }
}
