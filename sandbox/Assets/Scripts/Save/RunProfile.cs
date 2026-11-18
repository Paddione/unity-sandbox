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

    /// <summary>Fresh-run defaults (New Game, headless tests).</summary>
    public static void Reset()
    {
        Difficulty = Normal;
        DifficultyUnlocked = false;
        RunSeed = NewSeed();
        Stash = new Stash();
    }

    /// <summary>Non-negative seed from the system clock-based RNG.</summary>
    public static int NewSeed() => new System.Random().Next(0, int.MaxValue);
}
