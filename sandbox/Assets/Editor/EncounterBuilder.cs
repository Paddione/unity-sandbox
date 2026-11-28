using UnityEngine;

/// <summary>
/// Encounter scene wiring (cinderreach-encounter-roles S1, editor only):
/// fixed cover blocks for the Ossuary Warden pair and Kiln Warden spawns
/// for the hunter group. Called from the builders; variant 0 matches the
/// previous layout exactly.
/// </summary>
public static class EncounterBuilder
{
    public const string CoverPrefix = "EncounterCover_O_";

    // Local pack centres. The first position in each zone fills a previously
    // empty pocket inside the old rim; the others cover the new flanks.
    public static readonly Vector2[] FenPacks =
    {
        new(15, 10), new(-27, 24), new(-27, 3), new(-24, -24), new(0, -27),
        new(25, -24), new(28, -2), new(27, 24),
    };
    public static readonly Vector2[] OssuaryPacks =
    {
        new(-12, -12), new(-23, -19), new(-22, 2), new(-21, 22),
        new(7, 25), new(23, 21), new(24, 1), new(22, -22), new(-3, -26),
    };
    public static readonly Vector2[] KilnPacks =
    {
        new(-12, 11), new(-22, 22), new(-23, 0), new(-21, -21),
        new(1, -23), new(23, -20), new(24, 1), new(24, 24), new(-4, 23),
    };
    public static readonly Vector2[] BarrensPacks =
    {
        new(16, 10), new(-29, 27), new(-31, 1), new(-27, -27),
        new(0, -32), new(29, -29), new(31, 1), new(29, 28), new(0, 32),
    };

    public static void PlacePairs(Vector3 zoneCenter, Vector2[] packs, System.Action<int, Vector3> spawn)
    {
        for (int i = 0; i < packs.Length; i++)
        {
            var anchor = zoneCenter + new Vector3(packs[i].x, 0.05f, packs[i].y);
            spawn(2 * i, anchor + Vector3.left);
            spawn(2 * i + 1, anchor + Vector3.right);
        }
    }

    /// <summary>Place one cover block per variant pair on the arrival sight axis.</summary>
    public static void EnsureCover(Transform root)
    {
        for (int v = 0; v < EncounterLayout.Variants; v++)
            for (int k = 0; k < 2; k++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"{CoverPrefix}{v}_{k}";
                go.transform.SetParent(root, true);
                go.transform.position = EncounterLayout.OssuaryCoverAnchor(v, k);
                go.transform.localScale = new Vector3(1.6f, 1.2f, 1.6f);
                var r = go.GetComponent<Renderer>();
                if (r != null)
                    r.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                    {
                        color = new Color(0.36f, 0.34f, 0.38f),
                    };
            }
    }

    /// <summary>Move indexed Kiln enemies to a variant's anchors (variant 0 = builder layout).</summary>
    public static void PlaceVariant(int variant)
    {
        int v = Mathf.Clamp(variant, 0, EncounterLayout.Variants - 1);
        for (int i = 0; i < KilnLayout.ThrallCount; i++)
            MoveChild(KilnLayout.ThrallPrefix + i, EncounterLayout.KilnThrallAnchor(v, i));
        for (int i = 0; i < KilnLayout.LancerCount; i++)
            MoveChild(KilnLayout.LancerPrefix + i, EncounterLayout.KilnLancerAnchor(v, i));
        for (int i = 0; i < KilnLayout.EliteCount; i++)
            MoveChild(KilnLayout.ElitePrefix + i, EncounterLayout.KilnEliteAnchor(v, i));
        for (int k = 0; k < 2; k++)
            MoveChild(KilnWardenName(k), EncounterLayout.KilnWardenAnchor(v, k));
        EncounterLayout.SetVariant(v);
    }

    public static string KilnWardenName(int k) => EncounterLayout.KilnWardenPrefix + k;

    static void MoveChild(string name, Vector3 pos)
    {
        var go = GameObject.Find(name);
        if (go == null || go.GetComponent<EnemyHealth>() == null) return;
        var cc = go.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        go.transform.position = pos;
        if (cc != null) cc.enabled = true;
    }
}
