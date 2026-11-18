/// <summary>
/// Runtime holder for save fields that do not live on the hero
/// (replayability S1, save v2): difficulty, its unlock, the layout seed of the
/// current run and the stash. <see cref="SaveService.CaptureState"/> reads it,
/// <see cref="SaveService.ApplyState"/> fills it, so autosaves never drop
/// these fields. Local per client like the rest of the save.
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

    public static ClassDefinition Class => ClassCatalog.Get(ClassId);
    /// <summary>Act I progression (act-one S1, save v4).</summary>
    public static ActProgress Act = new ActProgress();

    /// <summary>Fresh-run defaults (New Game, headless tests).</summary>
    public static void Reset()
    {
        Difficulty = Normal;
        DifficultyUnlocked = false;
        RunSeed = NewSeed();
        Stash = new Stash();
        ClassId = ClassCatalog.FenwalkerId;
        Act = new ActProgress();
        KilnLayout.ResetPlaza(); // fair-fights: fresh run re-arms arrival safety
        ContractProgress.Reset();
        SmithingMaterial = 0;
    }

    /// <summary>Non-negative seed from the system clock-based RNG.</summary>
    public static int NewSeed() => new System.Random().Next(0, int.MaxValue);
}
