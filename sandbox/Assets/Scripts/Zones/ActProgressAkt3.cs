/// <summary>
/// Akt III finale draft (cinderreach-akt3-finale S1, bibel+akt). Finale state
/// for the Wacht/Moortor run-through (docs/story-bible.md §Akt III): the
/// Moortor opens once the Barrens' zone boss has fallen, the final boss
/// (Thewes, der Heimkehrer) falls at the Moortor, and the H13 decision
/// (zerschlagen/halten) books exactly one closing line on the same run.
/// Mirrors the Abbot→Kiln / Kilnmother→Brache pattern on
/// <see cref="ActProgress"/> (gated first-time transitions, no skipping):
/// <see cref="OpenMoortor"/> needs <see cref="ActProgress.ZoneBossDefeated"/>,
/// <see cref="DefeatFinalBoss"/> needs the Moortor open, <see cref="Decide"/>
/// needs the final boss down. Pure C#, no balance/loot/gear changes and no
/// save migration (SaveGame v16 untouched) — Save/Koop wiring follows in S3.
/// </summary>
public sealed class ActProgressAkt3
{
    public const int NoDecision = 0;
    /// <summary>H13: die Laternen zerschlagen.</summary>
    public const int Shatter = 1;
    /// <summary>H13: die Laternen halten.</summary>
    public const int Hold = 2;

    /// <summary>Moortor run-through open (H10/H11 playable).</summary>
    public bool MoortorOpen { get; private set; }
    /// <summary>Thewes, der Heimkehrer, down (H12 done).</summary>
    public bool FinalBossDefeated { get; private set; }
    /// <summary>H13 closing line: <see cref="NoDecision"/>, <see cref="Shatter"/> or <see cref="Hold"/>.</summary>
    public int Decision { get; private set; } = NoDecision;

    /// <summary>
    /// Open the Moortor. Only valid once the Barrens' zone boss has fallen
    /// (the Glasmacher down, like the Kilnmother opens the Barrens). True the
    /// first time.
    /// </summary>
    public bool OpenMoortor(ActProgress act)
    {
        if (MoortorOpen || act == null || !act.ZoneBossDefeated) return false;
        MoortorOpen = true;
        return true;
    }

    /// <summary>Final boss down. Only valid with the Moortor open. True the first time.</summary>
    public bool DefeatFinalBoss()
    {
        if (FinalBossDefeated || !MoortorOpen) return false;
        FinalBossDefeated = true;
        return true;
    }

    /// <summary>
    /// H13 decision: <paramref name="shatter"/> true zerschlagen, false halten.
    /// Exactly one closing line on the same run-through: true the first time,
    /// later calls are no-ops.
    /// </summary>
    public bool Decide(bool shatter)
    {
        if (Decision != NoDecision || !FinalBossDefeated) return false;
        Decision = shatter ? Shatter : Hold;
        return true;
    }
}
