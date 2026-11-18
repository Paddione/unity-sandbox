using UnityEngine;

/// <summary>
/// Difficulty as a multiplier profile (replayability S4), no second content
/// set. Normal is ×1 everywhere. Kindled (unlocked by the first Hollow Abbot
/// kill): enemy HP and damage ×1.8, tier-2 affixes, unique weight ×2, gold ×1.5.
///
/// <see cref="Current"/> follows <see cref="RunProfile"/>; Kindled only applies
/// while it is unlocked. <c>-difficulty kindled</c> on the command line
/// (nightly autoplay) unlocks and selects it before the scene loads.
/// Enemy HP scales when enemies wake or get their tier at runtime; damage
/// scales at the moment it hits the hero.
/// </summary>
public sealed class DifficultyProfile
{
    public const string Flag = "-difficulty";

    public readonly string name;
    public readonly float hpMul;
    public readonly float damageMul;
    public readonly int affixTier;
    public readonly float uniqueMul;
    public readonly float goldMul;

    DifficultyProfile(string name, float hpMul, float damageMul, int affixTier, float uniqueMul, float goldMul)
    {
        this.name = name;
        this.hpMul = hpMul;
        this.damageMul = damageMul;
        this.affixTier = affixTier;
        this.uniqueMul = uniqueMul;
        this.goldMul = goldMul;
    }

    public static readonly DifficultyProfile Normal = new DifficultyProfile(RunProfile.Normal, 1f, 1f, 1, 1f, 1f);
    public static readonly DifficultyProfile Kindled = new DifficultyProfile(RunProfile.Kindled, 1.8f, 1.8f, 2, 2f, 1.5f);

    public static DifficultyProfile Current =>
        RunProfile.DifficultyUnlocked && RunProfile.Difficulty == RunProfile.Kindled ? Kindled : Normal;

    public int ScaleHP(int hp) => Mathf.Max(1, Mathf.RoundToInt(hp * hpMul));
    public int ScaleDamage(int damage) => damage <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(damage * damageMul));
    public int ScaleGold(int gold) => gold <= 0 ? 0 : Mathf.RoundToInt(gold * goldMul);

    /// <summary>Select a difficulty. Kindled is refused while locked. Returns the active name.</summary>
    public static string Select(string difficulty)
    {
        RunProfile.Difficulty = difficulty == RunProfile.Kindled && RunProfile.DifficultyUnlocked
            ? RunProfile.Kindled
            : RunProfile.Normal;
        return RunProfile.Difficulty;
    }

    /// <summary>First boss kill: unlock Kindled. True only the first time.</summary>
    public static bool UnlockKindled()
    {
        if (RunProfile.DifficultyUnlocked) return false;
        RunProfile.DifficultyUnlocked = true;
        Debug.Log("[Difficulty] Kindled unlocked.");
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ReadCommandLine()
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] != Flag) continue;
            if (string.Equals(args[i + 1], RunProfile.Kindled, System.StringComparison.OrdinalIgnoreCase))
            {
                RunProfile.DifficultyUnlocked = true;
                RunProfile.Difficulty = RunProfile.Kindled;
            }
            else
            {
                RunProfile.Difficulty = RunProfile.Normal;
            }
            Debug.Log($"[Difficulty] command line: {RunProfile.Difficulty}");
        }
    }
}
