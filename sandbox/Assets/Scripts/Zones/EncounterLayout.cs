using UnityEngine;

/// <summary>
/// O1 encounters (cinderreach-encounter-roles S1): three deterministic
/// encounter variants with anchors, cover and retreat routes. Variant 0 is
/// the builder layout (Ossuary ring, Kiln ring anchors from
/// <see cref="KilnLayout"/>), so existing config tests keep passing;
/// variants 1-2 rotate the roof.
/// <para/>
/// Ossuary: a Warden pair on interrupted sight axes (opposite ring wardens,
/// a fixed cover block between arrival and each of them). Kiln: thrall
/// packs 0-2 are the front group (independent trigger), packs 3-5 the back
/// group behind the choke, plus a hunter group (2 Ash Lancers + 2
/// zone-bound Kiln Wardens).
/// Pure data + planar math: EditMode-testable without a scene.
/// </summary>
public static class EncounterLayout
{
    public const int Variants = 3;

    /// <summary>Cover blocks shots inside this planar radius.</summary>
    public const float CoverRadius = 1.1f;

    /// <summary>Choke line (relative to <see cref="KilnLayout.KilnOffset"/>): the hero has crossed it once past this Z.</summary>
    public const float ChokeKilnZ = -2f;

    public const int FrontGroup = 0;
    public const int BackGroup = 1;

    /// <summary>Name prefix of the Kiln hunter-group wardens (builder + seeder share it).</summary>
    public const string KilnWardenPrefix = "Kiln Warden_";

    /// <summary>A back-group enemy this close to the hero answers anyway (poked the nest).</summary>
    public const float PersonalSpace = 4f;

    /// <summary>Encounter variant driving group release this run (LayoutSeeder sets it).</summary>
    public static int ActiveVariant { get; private set; }

    /// <summary>Back group released (choke crossed). Reset per run/seed.</summary>
    public static bool BackGroupReleased { get; private set; }

    public static void ResetEncounter()
    {
        ActiveVariant = 0;
        BackGroupReleased = false;
    }

    public static void SetVariant(int variant)
    {
        ActiveVariant = Mathf.Clamp(variant, 0, Variants - 1);
        BackGroupReleased = false;
    }

    /// <summary>Hero crossed the Kiln choke: release the back group (latched).</summary>
    public static void NoteHeroPosition(Vector3 heroPos)
    {
        if (PassedChoke(heroPos)) BackGroupReleased = true;
    }

    public static void ReleaseBackGroup() => BackGroupReleased = true;

    /// <summary>Group 0 always active; group 1 after the choke (or when poked, checked by the enemy).</summary>
    public static bool GroupActive(int group, Vector3 heroPos) =>
        group == FrontGroup || BackGroupReleased || PassedChoke(heroPos);

    // ---- Ossuary ----

    public static Vector3 OssuaryArrival => KilnLayout.OssuaryOffset + new Vector3(0f, 1f, 2f);
    public static Vector3 OssuaryRetreat => KilnLayout.OssuaryOffset + new Vector3(0f, 1f, 10f);

    /// <summary>Full 8-warden ring; variant 0 pairs are spots 0 and 4 (the builder layout).</summary>
    public static Vector3 OssuaryRingAnchor(int i)
    {
        float a = i * Mathf.PI * 2f / 8f;
        return KilnLayout.OssuaryOffset + new Vector3(Mathf.Cos(a) * 7f, 0.05f, Mathf.Sin(a) * 7f);
    }

    /// <summary>Opposite ring wardens per variant. Pairs are explicit (not a
    /// stride): v0 {0,4} is the builder layout, v1 {3,7} rotates the roof,
    /// v2 {5,1} mirrors v1 — the south slot (index 6, on the boss axis) is
    /// never used, so cover always blocks the warden and never the boss.
    /// </summary>
    static readonly int[,] WardenPairs = { { 0, 4 }, { 3, 7 }, { 5, 1 } };

    public static Vector3 OssuaryWardenAnchor(int variant, int pair)
    {
        int idx = WardenPairs[ClampV(variant), Mathf.Clamp(pair, 0, 1)];
        float a = idx * Mathf.PI * 2f / 8f;
        return KilnLayout.OssuaryOffset + new Vector3(Mathf.Cos(a) * 7f, 0.05f, Mathf.Sin(a) * 7f);
    }

    /// <summary>Fixed cover on the arrival→warden sight axis (55 % of the way).</summary>
    public static Vector3 OssuaryCoverAnchor(int variant, int pair)
    {
        var arrival = OssuaryArrival;
        var warden = OssuaryWardenAnchor(variant, pair);
        var p = arrival + (warden - arrival) * 0.55f;
        p.y = 0.6f;
        return p;
    }

    // ---- Kiln ----

    public static Vector3 KilnArrival => KilnLayout.KilnArrival;
    public static Vector3 KilnRetreat => KilnLayout.KilnReturnPos;
    public static Vector3 KilnChoke(int variant) =>
        KilnLayout.KilnOffset + new Vector3(0f, 0.05f, ChokeKilnZ + ClampV(variant) * 0f);

    public static Vector3 KilnThrallAnchor(int variant, int i) => KilnLayout.ThrallAnchor(ClampV(variant), i);
    public static Vector3 KilnLancerAnchor(int variant, int i) => KilnLayout.LancerAnchor(ClampV(variant), i);
    public static Vector3 KilnEliteAnchor(int variant, int i) => KilnLayout.EliteAnchor(ClampV(variant), i);

    /// <summary>Front group: packs 0-2 (thrall indices 0-8). Back group: packs 3-5.</summary>
    public static bool IsKilnFront(int thrallIndex) => thrallIndex < KilnLayout.ThrallCount / 2;

    /// <summary>Hunter group: lancer indices 0-1 fight with the Kiln Wardens.</summary>
    public static bool IsKilnHunterLancer(int lancerIndex) => lancerIndex < 2;

    public static Vector3 KilnWardenAnchor(int variant, int k)
    {
        int v = ClampV(variant);
        return KilnLayout.KilnOffset + new Vector3((k == 0 ? -1f : 1f) * (5.5f + v), 0.05f, -1.5f + v * 2f);
    }

    public static bool PassedChoke(Vector3 heroPos) =>
        (heroPos.x > 300f && heroPos.x < 500f) && (heroPos.z - KilnLayout.KilnOffset.z) < ChokeKilnZ;

    // ---- shared math ----

    /// <summary>Planar segment→circle test: is the from→to sight axis interrupted by cover?</summary>
    public static bool BlockedByCover(Vector3 from, Vector3 to, Vector3 cover, float radius)
    {
        var a = new Vector2(from.x, from.z);
        var b = new Vector2(to.x, to.z);
        var c = new Vector2(cover.x, cover.z);
        var ab = b - a;
        float lenSq = ab.sqrMagnitude;
        if (lenSq < 0.0001f) return (c - a).magnitude <= radius;
        float t = Mathf.Clamp01(Vector2.Dot(c - a, ab) / lenSq);
        return (a + ab * t - c).magnitude <= radius;
    }

    /// <summary>Zone of a world position by annex offset (Town &lt; 100 m, Ossuary &lt; 300 m, Kiln &lt; 500 m, else Barrens).</summary>
    public static int CurrentZone(Vector3 pos)
    {
        if (pos.x >= 500f) return ActProgress.Barrens;
        if (pos.x >= 300f) return ActProgress.Kiln;
        if (pos.x >= 100f) return ActProgress.Ossuary;
        return ActProgress.Town;
    }

    static int ClampV(int v) => Mathf.Clamp(v, 0, Variants - 1);
}
