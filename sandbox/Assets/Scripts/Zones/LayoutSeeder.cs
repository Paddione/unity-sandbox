using UnityEngine;

/// <summary>
/// Seeded Ashen Fen layouts (replayability S3). Three anchor sets per
/// category (husk group, Ash Elite spot, Fen Brute spot, prop cluster); a seed
/// picks one per category, so a run sees one of 81 combinations. Variant 0 is
/// exactly the builder layout.
///
/// Runs once after the zone scene loads. Seed: <c>-seed N</c> on the command
/// line (autoplay, nightly rotation), otherwise <see cref="RunProfile.RunSeed"/>.
/// Co-op peers (<c>--net-client</c>, <c>--net-server</c>, ParrelSync clones)
/// keep variant 0 everywhere: enemy positions are not replicated yet
/// (cinderreach-coop-session S2), so a per-client seed would split the world.
/// </summary>
public static class LayoutSeeder
{
    public const int VariantCount = 3;
    public const string PropRootPrefix = "LayoutProps_";
    public const string SeedFlag = "-seed";
    public const string EliteName = "Ash Elite";
    public const string BruteName = "Fen Brute";
    public const int HuskCount = 10;
    const float Y = 0.05f;

    static Vector3 P(float x, float z) => new Vector3(x, Y, z);

    /// <summary>Husk_0..Husk_9 per variant. Variant 0 = SandboxBuilder.BuildDiabloZone.</summary>
    public static readonly Vector3[][] HuskSets =
    {
        new[] { P(-5, 2), P(5, 2), P(-2, -1), P(3, -2), P(0, 5), P(-7, -3), P(7, -3), P(-4, -6), P(4, -6), P(0, -8) },
        new[] { P(8, -2), P(10, 1), P(12, -5), P(6, -9), P(11, -11), P(3, -12), P(14, -8), P(9, -14), P(5, -4), P(13, 2) },
        new[] { P(-8, -2), P(-10, 3), P(-13, -5), P(-6, -10), P(-11, -12), P(-3, -13), P(-15, -8), P(-9, -15), P(-5, -4), P(-14, 1) },
    };

    public static readonly Vector3[] EliteSpots = { P(-12, 3), P(12, -2), P(0, -14) };
    public static readonly Vector3[] BruteSpots = { P(9, -6), P(-10, -9), P(6, -15) };

    /// <summary>Chosen variant per category.</summary>
    public struct Layout
    {
        public int husks, elite, brute, props;
        public override string ToString() => $"husks={husks} elite={elite} brute={brute} props={props}";
    }

    public static Layout ForSeed(int seed)
    {
        var rng = new System.Random(seed);
        return new Layout
        {
            husks = rng.Next(VariantCount),
            elite = rng.Next(VariantCount),
            brute = rng.Next(VariantCount),
            props = rng.Next(VariantCount),
        };
    }

    public static readonly Layout Baseline = new Layout();

    /// <summary>Last applied layout this run (logs, autoplay, tests).</summary>
    public static Layout Current { get; private set; }
    public static int CurrentSeed { get; private set; } = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (GameObject.Find(PropRootPrefix + "0") == null) return; // not the zone scene
        if (IsCoopPeer())
        {
            Apply(Baseline);
            Debug.Log("[Layout] co-op peer: baseline layout (positions not replicated yet).");
            return;
        }
        int seed = EffectiveSeed();
        Apply(seed);
    }

    public static bool IsCoopPeer() =>
        HasArg("--net-client") || HasArg("--net-server") || MultiplayerAutoStarter.IsClone();

    /// <summary><c>-seed N</c> when given (also stored as the run seed), else the run profile seed.</summary>
    public static int EffectiveSeed()
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == SeedFlag && int.TryParse(args[i + 1], out int s) && s >= 0)
            {
                RunProfile.RunSeed = s;
                return s;
            }
        return RunProfile.RunSeed;
    }

    public static Layout Apply(int seed)
    {
        var layout = ForSeed(seed);
        Apply(layout);
        CurrentSeed = seed;
        Debug.Log($"[Layout] seed={seed} {layout}");
        return layout;
    }

    /// <summary>Move enemies to the chosen anchors and show one prop cluster. Returns moved enemies.</summary>
    public static int Apply(Layout layout)
    {
        int moved = 0;
        var husks = HuskSets[Clamp(layout.husks)];
        for (int i = 0; i < HuskCount; i++)
            if (Place($"Husk_{i}", husks[i])) moved++;
        if (Place(EliteName, EliteSpots[Clamp(layout.elite)])) moved++;
        if (Place(BruteName, BruteSpots[Clamp(layout.brute)])) moved++;
        for (int v = 0; v < VariantCount; v++)
        {
            var root = FindIncludingInactive(PropRootPrefix + v);
            if (root != null) root.SetActive(v == Clamp(layout.props));
        }
        Current = layout;
        return moved;
    }

    static int Clamp(int v) => Mathf.Clamp(v, 0, VariantCount - 1);

    static bool Place(string name, Vector3 pos)
    {
        var go = GameObject.Find(name);
        if (go == null || go.GetComponent<EnemyHealth>() == null) return false;
        var cc = go.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        go.transform.position = pos;
        if (cc != null) cc.enabled = true;
        return true;
    }

    /// <summary>GameObject.Find skips inactive objects; prop roots start inactive.</summary>
    public static GameObject FindIncludingInactive(string name)
    {
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            if (t != null && t.name == name && t.parent == null && t.gameObject.scene.IsValid())
                return t.gameObject;
        return null;
    }

    static bool HasArg(string flag)
    {
        foreach (var a in System.Environment.GetCommandLineArgs())
            if (a == flag) return true;
        return false;
    }

    /// <summary>Prop cluster rock positions per variant (builder + reachability test share them).</summary>
    public static readonly Vector3[][] PropSets =
    {
        new[] { new Vector3(15f, 0.35f, -15f), new Vector3(16f, 0.35f, -13f), new Vector3(-14f, 0.35f, -16f), new Vector3(-16f, 0.35f, -14f) },
        new[] { new Vector3(-1f, 0.35f, -16.5f), new Vector3(1.5f, 0.35f, -16.5f), new Vector3(15f, 0.35f, 5f), new Vector3(16f, 0.35f, 7f) },
        new[] { new Vector3(9f, 0.35f, -17f), new Vector3(11f, 0.35f, -16.5f), new Vector3(-16f, 0.35f, 5f), new Vector3(-16f, 0.35f, 7f) },
    };
}
