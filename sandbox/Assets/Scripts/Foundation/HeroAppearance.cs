using UnityEngine;

/// <summary>Hero gender at creation (character-creation). Cosmetic only.</summary>
public enum HeroGender { Female = 0, Male = 1, NonBinary = 2 }

/// <summary>Physical build at creation (character-creation). Cosmetic only: no stats or collision change.</summary>
public enum HeroBuild { Slim = 0, Athletic = 1, Sturdy = 2 }

/// <summary>
/// Serializable hero look chosen at creation: name, gender, hair style/color
/// and build. Stored in the save (v14), held at runtime by
/// <see cref="RunProfile"/> and applied by <see cref="ClassAppearance"/>.
/// </summary>
[System.Serializable]
public struct HeroProfile
{
    public string heroName;
    public int gender;
    public int hairStyle;
    public int hairColor;
    public int build;

    public static HeroProfile CreateDefault(System.Random rng)
    {
        rng ??= new System.Random();
        int g = rng.Next(0, 3);
        return new HeroProfile
        {
            heroName = HeroNames.RandomName((HeroGender)g, rng),
            gender = g,
            hairStyle = rng.Next(0, HeroAppearance.HairStyleCount),
            hairColor = rng.Next(0, HeroAppearance.HairColorCount),
            build = (int)HeroBuild.Athletic,
        };
    }

    /// <summary>Clamp every index into range and replace an unusable name with a random pool name.</summary>
    public static HeroProfile Sanitized(HeroProfile raw, System.Random rng = null)
    {
        rng ??= new System.Random();
        int g = Mathf.Clamp(raw.gender, 0, 2);
        string name = HeroNames.Clean(raw.heroName) ?? HeroNames.RandomName((HeroGender)g, rng);
        return new HeroProfile
        {
            heroName = name,
            gender = g,
            hairStyle = Mathf.Clamp(raw.hairStyle, 0, HeroAppearance.HairStyleCount - 1),
            hairColor = Mathf.Clamp(raw.hairColor, 0, HeroAppearance.HairColorCount - 1),
            build = Mathf.Clamp(raw.build, 0, (int)HeroBuild.Sturdy),
        };
    }

    /// <summary>Deterministic default for migrated pre-v14 saves (seeded by the run, never the account name).</summary>
    public static HeroProfile MigrationDefault(int seed)
    {
        var rng = new System.Random(seed);
        var p = CreateDefault(rng);
        p.gender = (int)HeroGender.NonBinary;
        p.heroName = HeroNames.RandomName(HeroGender.NonBinary, rng);
        return Sanitized(p, rng);
    }
}

/// <summary>German fantasy hero names (character-creation): pools per gender plus neutral names.</summary>
public static class HeroNames
{
    public static readonly string[] FemalePool =
    {
        "Mira", "Ottilie", "Merle", "Ilsabe", "Fenja", "Alrun", "Bruna", "Selma",
        "Wibke", "Runa", "Ida", "Liosa", "Marit", "Sanna", "Tilda", "Vreni",
    };
    public static readonly string[] MalePool =
    {
        "Jorin", "Alrik", "Benno", "Corvin", "Dankwart", "Emmerich", "Falk", "Gernot",
        "Hagen", "Ivo", "Kolmar", "Lorenz", "Merten", "Norwin", "Odo", "Raban",
    };
    public static readonly string[] NeutralPool =
    {
        "Asche", "Birk", "Erl", "Fenn", "Glut", "Hain", "Kien", "Loh",
        "Moor", "Ried", "Sohl", "Tann", "Ulm", "Wacht", "Zinn", "Farn",
    };

    /// <summary>Random name for a gender; non-binary draws from all three pools.</summary>
    public static string RandomName(HeroGender gender, System.Random rng)
    {
        rng ??= new System.Random();
        if (gender == HeroGender.Female) return FemalePool[rng.Next(FemalePool.Length)];
        if (gender == HeroGender.Male) return MalePool[rng.Next(MalePool.Length)];
        int pick = rng.Next(FemalePool.Length + MalePool.Length + NeutralPool.Length);
        if (pick < FemalePool.Length) return FemalePool[pick];
        if (pick < FemalePool.Length + MalePool.Length) return MalePool[pick - FemalePool.Length];
        return NeutralPool[pick - FemalePool.Length - MalePool.Length];
    }

    /// <summary>
    /// Usable hero name (2–16 chars, letters/digits/'-'/'_' and single spaces),
    /// trimmed and collapsed; null when nothing usable survives.
    /// </summary>
    public static string Clean(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var sb = new System.Text.StringBuilder(raw.Length);
        bool lastSpace = true;
        foreach (char c in raw.Trim())
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_') { sb.Append(c); lastSpace = false; }
            else if (char.IsWhiteSpace(c)) { if (!lastSpace) { sb.Append(' '); lastSpace = true; } }
            else return null;
            if (sb.Length > 16) return null;
        }
        string s = sb.ToString().TrimEnd(' ');
        return s.Length >= 2 ? s : null;
    }
}

/// <summary>Hair/build catalog and network packing (character-creation). Pure, EditMode-pinned.</summary>
public static class HeroAppearance
{
    public const int HairStyleCount = 6;
    public const int HairColorCount = 10;
    public const int BuildCount = 3;
    public const string HairRootName = "HeroHair";

    /// <summary>Style ids: 0 Kurz, 1 Lang, 2 Zopf, 3 Kapuze, 4 Glatze, 5 Knoten.</summary>
    public static readonly string[] HairStyleIds = { "short", "long", "ponytail", "hood", "bald", "bun" };

    static readonly Color[] HairColors =
    {
        new Color(0.16f, 0.10f, 0.06f), // dunkelbraun
        new Color(0.42f, 0.24f, 0.10f), // braun
        new Color(0.72f, 0.46f, 0.18f), // hellbraun
        new Color(0.87f, 0.68f, 0.34f), // blond
        new Color(0.93f, 0.88f, 0.76f), // hellblond
        new Color(0.55f, 0.16f, 0.07f), // kupfer
        new Color(0.62f, 0.10f, 0.08f), // rot
        new Color(0.05f, 0.04f, 0.05f), // schwarz
        new Color(0.62f, 0.62f, 0.65f), // grau
        new Color(0.88f, 0.86f, 0.84f), // weiss
    };

    public static Color HairColor(int index) => HairColors[Mathf.Clamp(index, 0, HairColors.Length - 1)];

    /// <summary>
    /// Bone-scale proportions (chest, shoulders, arms) for gender+build.
    /// Purely visual: every factor stays near 1, the CharacterController and
    /// all gameplay values never read this.
    /// </summary>
    public static void GetProportions(HeroGender gender, HeroBuild build, out Vector3 scales)
    {
        float genderChest = gender == HeroGender.Female ? 0.94f : gender == HeroGender.Male ? 1.05f : 1.0f;
        float genderArm = gender == HeroGender.Female ? 0.92f : gender == HeroGender.Male ? 1.06f : 1.0f;
        float buildMul = build == HeroBuild.Slim ? 0.90f : build == HeroBuild.Sturdy ? 1.13f : 1.0f;
        scales = new Vector3(
            Mathf.Clamp(genderChest * buildMul, 0.85f, 1.25f),
            Mathf.Clamp(genderChest * buildMul, 0.85f, 1.25f),
            Mathf.Clamp(genderArm * buildMul, 0.85f, 1.25f));
    }

    /// <summary>Pack the look into one owner-written network int (name rides the name channel).</summary>
    public static int Pack(HeroProfile p)
    {
        var c = HeroProfile.Sanitized(p, new System.Random(1));
        return c.gender * 1000 + c.hairStyle * 100 + c.hairColor * 10 + c.build;
    }

    /// <summary>Inverse of <see cref="Pack"/>; out-of-range codes clamp to a usable profile.</summary>
    public static HeroProfile Unpack(int code, string heroName)
    {
        if (code < 0) code = 0;
        return HeroProfile.Sanitized(new HeroProfile
        {
            heroName = heroName,
            gender = code / 1000,
            hairStyle = (code / 100) % 10,
            hairColor = (code / 10) % 10,
            build = code % 10,
        }, new System.Random(1));
    }
}
