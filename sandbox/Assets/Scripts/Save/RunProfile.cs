/// <summary>
/// Runtime holder for save fields that do not live on the hero
/// (replayability S1, save v2): difficulty, its unlock, the layout seed of the
/// current run and the stash. <see cref="SaveService.CaptureState"/> reads it,
/// <see cref="SaveService.ApplyState"/> fills it, so autosaves never drop
/// these fields. Local per client like the rest of the save. Skill variant
/// picks (skill-variants S1, save v7) ride along the same way: switching a
/// variant never touches points or ranks.
/// </summary>
public static class RunProfile
{
    public const string Normal = "Normal";
    public const string Kindled = "Kindled";

    public static string Difficulty = Normal;
    public static bool DifficultyUnlocked;
    public static int RunSeed = NewSeed();
    public static Stash Stash = new Stash();
    /// <summary>Playable class (second-class S1, save v3).</summary>
    public static string ClassId = ClassCatalog.FenwalkerId;
    /// <summary>Schmiede-Material (smithing S1, save v6).</summary>
    public static int SmithingMaterial;
    /// <summary>Skill variant picks by "classId:Skill" (skill-variants S1, save v7).</summary>
    public static System.Collections.Generic.Dictionary<string, string> SkillVariants =
        new System.Collections.Generic.Dictionary<string, string>();

    public static ClassDefinition Class => ClassCatalog.Get(ClassId);
    /// <summary>Act I progression (act-one S1, save v4).</summary>
    public static ActProgress Act = new ActProgress();
    /// <summary>Kindled-finale claim (S2, save v9): pending class + one-time grant.</summary>
    public static bool KindledRewardPending;
    public static string KindledRewardClass = "";
    public static bool KindledRewardGranted;

    /// <summary>Risk-shrine choice (risk-shrines S1, save v8).</summary>
    public static RiskShrineProgress RiskShrine = new RiskShrineProgress();

    /// <summary>Story quest progress (cinderreach-deutsch-story S4, save v13).</summary>
    public static StoryState Story = new StoryState();
    /// <summary>Akt III finale run-through (cinderreach-akt3-finale S3): Moortor,
    /// final boss, H13 decision. Persists via the generic story lists (v16 stays).</summary>
    public static ActProgressAkt3 Finale = new ActProgressAkt3();

    /// <summary>Hero look + name (character-creation, save v14). Separate from the account name.</summary>
    public static HeroProfile Hero = HeroProfile.CreateDefault(new System.Random());

    /// <summary>Fresh-run defaults (New Game, headless tests).</summary>
    public static void Reset()
    {
        Difficulty = Normal;
        DifficultyUnlocked = false;
        RunSeed = NewSeed();
        Stash = new Stash();
        ClassId = ClassCatalog.FenwalkerId;
        SkillVariants = new System.Collections.Generic.Dictionary<string, string>();
        Act = new ActProgress();
        KilnLayout.ResetPlaza(); // fair-fights: fresh run re-arms arrival safety
        BarrensLayout.ResetPlaza();
        ContractProgress.Reset();
        Story = new StoryState();
        Finale = new ActProgressAkt3();
        StoryDirector.ResetFinaleForTests();
        SmithingMaterial = 0;
        RiskShrine.Reset();
        KindledRewardPending = false;
        KindledRewardClass = "";
        KindledRewardGranted = false;
        Hero = HeroProfile.CreateDefault(new System.Random());
    }

    static string VariantKey(string classId, DiabloSkills.Skill skill) => classId + ":" + skill;

    /// <summary>Active variant: stored pick when known, else the legacy default.</summary>
    public static string GetSkillVariant(string classId, DiabloSkills.Skill skill)
    {
        SkillVariants.TryGetValue(VariantKey(classId, skill), out string stored);
        return SkillVariantCatalog.Resolve(classId, skill, stored);
    }

    /// <summary>
    /// Store a variant pick. True when the id is known and the switch happens
    /// solo inside the Lantern Hold; points and ranks stay untouched.
    /// </summary>
    public static bool TrySetSkillVariant(string classId, DiabloSkills.Skill skill,
        string variantId, UnityEngine.Vector3 heroPos, bool solo)
    {
        if (!SkillVariantCatalog.IsKnownVariant(classId, skill, variantId)) return false;
        if (!SkillVariantCatalog.CanChangeAt(heroPos, solo)) return false;
        SkillVariants[VariantKey(classId, skill)] = variantId;
        return true;
    }

    /// <summary>Non-negative seed from the system clock-based RNG.</summary>
    public static int NewSeed() => new System.Random().Next(0, int.MaxValue);
}
