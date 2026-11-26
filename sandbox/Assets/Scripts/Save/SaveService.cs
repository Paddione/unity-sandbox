using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Solo-local save/load (S2 persist). One JSON file at
/// <c>Application.persistentDataPath/cinderreach-save.json</c>, written
/// atomically (tmp + rename) through <see cref="SaveNow"/>. Autosave
/// triggers: level-up (<see cref="DiabloProgression.OnLevelUp"/>),
/// application quit (hooked below), and — once those stages land —
/// the zone transition (S5: <c>SaveNow(hero, zoneReached: 2)</c>) and the
/// vendor window close (S3: <c>SaveNow(hero)</c>).
/// A corrupt file is backed up to <c>*.corrupt</c> with a warning and never
/// silently deleted (fresh start instead). Older versions are migrated by
/// <see cref="SaveMigrations"/> (original kept as <c>*.v1.bak</c> before the
/// first newer write); an unknown newer version is not loaded and stays
/// untouched.
/// There is no main-menu scene yet: Continue/New-Game wire to
/// <see cref="HasSave"/>, <see cref="TryLoad"/> and <see cref="DeleteSave"/>
/// (New Game deletes only after the caller's overwrite confirmation).
/// </summary>
public static class SaveService
{
    public const string FileName = "cinderreach-save.json";
    /// <summary>
    /// <c>-save-file &lt;name&gt;</c> picks another file name in persistentDataPath
    /// (coop-session S1: host and client on one machine must not share a save).
    /// </summary>
    public const string SaveFileFlag = "-save-file";

    public static string DefaultPath =>
        Path.Combine(Application.persistentDataPath, FileNameFor(Environment.GetCommandLineArgs()));

    /// <summary>Save file name for these arguments: the -save-file value (file name only) or <see cref="FileName"/>.</summary>
    public static string FileNameFor(string[] args)
    {
        if (args != null)
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == SaveFileFlag)
                {
                    var name = Path.GetFileName(args[i + 1]);
                    if (!string.IsNullOrWhiteSpace(name) && !name.StartsWith("-")) return name;
                }
        return FileName;
    }

    /// <summary>Furthest zone of the last load (S5 gate reads this).</summary>
    public static int LoadedZoneReached { get; private set; } = 1;

    static GameObject lastHero;
    static bool quitHooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void HookQuit()
    {
        if (quitHooked) return;
        quitHooked = true;
        Application.quitting += () =>
        {
            // Unity null: hero destroyed since the last save.
            if (lastHero == null) return;
            try
            {
                if (HasSave() || HasProgress(lastHero)) SaveNow(lastHero);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Quit autosave failed: {e.Message}");
            }
        };
    }

    static bool HasProgress(GameObject hero)
    {
        if (hero == null) return false;
        var prog = hero.GetComponent<DiabloProgression>();
        var wallet = hero.GetComponent<Wallet>();
        var inv = hero.GetComponent<Inventory>();
        var eq = hero.GetComponent<Equipment>();
        if (prog != null && (prog.Level > 1 || prog.Xp > 0)) return true;
        if (wallet != null && wallet.Gold > 0) return true;
        if (inv != null && inv.Count > 0) return true;
        if (eq == null) return false;
        foreach (var slot in Equipment.GearSlots)
            if (!Equipment.IsBlank(eq.GetEquipped(slot))) return true;
        return false;
    }

    public static bool HasSave(string path = null) => File.Exists(path ?? DefaultPath);

    /// <summary>
    /// Set before an in-game scene reload ("New run"): the next loaded scene
    /// continues the saved hero instead of starting fresh (spawn never loads
    /// on the host, see <see cref="DiabloNetworkPlayer.ShouldLoadSaveOnSpawn"/>).
    /// Path null = <see cref="DefaultPath"/> (test seam).
    /// </summary>
    public static bool ContinueAfterReload;
    /// <summary>Class id of a fresh hero to start after an in-game reload (null = none).</summary>
    public static string NewHeroAfterReload;
    /// <summary>Hero profile of a fresh hero to start after an in-game reload.</summary>
    public static HeroProfile NewHeroProfile;
    public static string ContinueAfterReloadPath;

    /// <summary>Delete the save (New Game, after the caller's confirmation).</summary>
    public static void DeleteSave(string path = null)
    {
        string p = path ?? DefaultPath;
        if (File.Exists(p)) File.Delete(p);
        CloudHeroSync.NotifyLocalDelete(p);
        LoadedZoneReached = 1;
        RunProfile.Reset();
        ContractProgress.Reset();
    }

    /// <summary>
    /// Capture and atomically persist the hero. zoneReached &lt; 0 keeps the
    /// previously saved zone (level-up/quit path), otherwise sets it (S5 gate).
    /// </summary>
    public static void SaveNow(GameObject hero, int zoneReached = -1, string path = null)
    {
        if (hero == null) return;
        int zone = zoneReached >= 0 ? zoneReached : ReadZoneOrDefault(path ?? DefaultPath);
        WriteSave(CaptureState(hero, zone), path ?? DefaultPath);
        lastHero = hero;
    }

    public static SaveGame CaptureState(GameObject hero, int zoneReached = 1)
    {
        var heroProfile = HeroProfile.Sanitized(RunProfile.Hero);
        RunProfile.Hero = heroProfile;
        var save = new SaveGame
        {
            zoneReached = Mathf.Max(1, zoneReached),
            difficultyUnlocked = RunProfile.DifficultyUnlocked,
            difficulty = RunProfile.Difficulty,
            runSeed = RunProfile.RunSeed,
            stash = RunProfile.Stash.ToList(),
            classId = RunProfile.ClassId,
            highestUnlockedZone = RunProfile.Act.HighestUnlocked,
            actOneComplete = RunProfile.Act.ActOneComplete,
            actTwoBossDefeated = RunProfile.Act.ZoneBossDefeated,
            waypoints = new System.Collections.Generic.List<string>(RunProfile.Act.Waypoints),
            smithingMaterial = System.Math.Max(0, RunProfile.SmithingMaterial),
            skillVariants = CaptureVariants(),
            riskShrineState = (int)RunProfile.RiskShrine.State,
            riskReward = RunProfile.RiskShrine.PendingReward,
            kindledRewardPending = RunProfile.KindledRewardPending,
            kindledRewardClass = RunProfile.KindledRewardClass ?? "",
            kindledRewardGranted = RunProfile.KindledRewardGranted,
            storyQuests = RunProfile.Story.ToSave(),
            storyFlags = new System.Collections.Generic.List<string>(RunProfile.Story.Flags),
            heroName = heroProfile.heroName,
            heroGender = heroProfile.gender,
            heroHairStyle = heroProfile.hairStyle,
            heroHairColor = heroProfile.hairColor,
            heroBuild = heroProfile.build,
        };
        ContractProgress.ToSnapshot(save.contract);
        if (hero == null) return save;
        var prog = hero.GetComponent<DiabloProgression>();
        if (prog != null)
        {
            prog.GetState(out int level, out int xp, out int freeAttr, out int freeSkill,
                out int str, out int vit, out int foc, out var ranks);
            save.level = level;
            save.xp = xp;
            save.freeAttributePoints = freeAttr;
            save.skillRanks = ranks;
            save.strength = str;
            save.vitality = vit;
            save.focus = foc;
            save.freeSkillPoints = freeSkill;
        }
        var wallet = hero.GetComponent<Wallet>();
        if (wallet != null) save.gold = wallet.Gold;
        var eq = hero.GetComponent<Equipment>();
        if (eq != null)
        {
            save.equipmentArmor = eq.armor;
            save.equipmentWeapon = eq.weapon;
            save.equipmentHelmet = eq.helmet;
            save.equipmentGloves = eq.gloves;
            save.equipmentBoots = eq.boots;
            save.equipmentBelt = eq.belt;
        }
        save.uniquePity = UniquePity.Current;
        var inv = hero.GetComponent<Inventory>();
        if (inv != null)
            for (int i = 0; i < inv.Count; i++)
                save.bag.Add(inv.Get(i));
        return save;
    }

    /// <summary>Load the save onto the hero. False when missing/corrupt (fresh start).</summary>
    public static bool TryLoad(GameObject hero, string path = null)
    {
        string p = path ?? DefaultPath;
        if (!TryRead(p, out SaveGame save)) return false;
        ApplyState(hero, save);
        LoadedZoneReached = Mathf.Max(1, save.zoneReached);
        lastHero = hero;
        Debug.Log($"[Save] Loaded level {save.level} ({save.gold} gold, zone {LoadedZoneReached}).");
        return true;
    }

    public static void ApplyState(GameObject hero, SaveGame save)
    {
        if (hero == null || save == null) return;
        var prog = hero.GetComponent<DiabloProgression>();
        if (prog == null) prog = hero.AddComponent<DiabloProgression>();
        prog.SetState(save.level, save.xp, save.freeAttributePoints, save.freeSkillPoints,
            save.strength, save.vitality, save.focus, save.skillRanks);
        var wallet = Wallet.EnsureWallet(hero);
        if (wallet != null) wallet.SetGold(save.gold);
        var eq = hero.GetComponent<Equipment>();
        if (eq == null) eq = hero.AddComponent<Equipment>();
        eq.armor = save.equipmentArmor;
        eq.weapon = save.equipmentWeapon;
        eq.helmet = save.equipmentHelmet;
        eq.gloves = save.equipmentGloves;
        eq.boots = save.equipmentBoots;
        eq.belt = save.equipmentBelt;
        eq.NormalizeEmptySlots();
        UniquePity.Current = Mathf.Max(0, save.uniquePity);
        var inv = hero.GetComponent<Inventory>();
        if (inv != null) UnityEngine.Object.DestroyImmediate(inv);
        inv = hero.AddComponent<Inventory>();
        if (save.bag != null)
            foreach (var item in save.bag)
                if (item != null) inv.Add(item);
        RunProfile.DifficultyUnlocked = save.difficultyUnlocked;
        RunProfile.Difficulty = string.IsNullOrEmpty(save.difficulty) ? RunProfile.Normal : save.difficulty;
        RunProfile.RunSeed = save.runSeed;
        RunProfile.Stash = Stash.FromList(save.stash);
        RunProfile.ClassId = ClassCatalog.Get(save.classId).id;
        // class-kits-parity: ranks on skills the class no longer carries come back as points
        // (old Fenwalker/Rogue saves with Fireball/Arrow ranks). Idempotent on later loads.
        int refunded = prog.Sheet != null ? prog.Sheet.RefundForeignRanks(RunProfile.Class) : 0;
        if (refunded > 0) Debug.Log($"[Save] Refunded {refunded} skill point(s) from skills outside the {RunProfile.ClassId} kit.");
        RunProfile.Hero = HeroProfile.Sanitized(new HeroProfile
        {
            heroName = save.heroName, gender = save.heroGender, hairStyle = save.heroHairStyle,
            hairColor = save.heroHairColor, build = save.heroBuild,
        });
        var heroLook = hero.GetComponent<ClassAppearance>();
        if (heroLook != null) heroLook.Apply(RunProfile.Class, RunProfile.Hero);
        RunProfile.Act = ActProgress.FromSave(save.highestUnlockedZone, save.actOneComplete, save.actTwoBossDefeated, save.waypoints);
        RunProfile.Story = StoryState.FromSave(save.storyQuests, save.storyFlags);
        RunProfile.Story.Refresh(RunProfile.Act);
        RunProfile.Story.TakeChanges();
        ContractProgress.FromSnapshot(save.contract);
        RunProfile.SmithingMaterial = System.Math.Max(0, save.smithingMaterial);
        ApplyVariants(save);
        RunProfile.RiskShrine.State = (RiskShrineState)UnityEngine.Mathf.Clamp(save.riskShrineState, 0, 4);
        RunProfile.RiskShrine.PendingReward = save.riskReward;
        // A claimed shrine never carries a leftover reward; an interrupted
        // payout (crash between bag write and save) re-opens the claim.
        if (RunProfile.RiskShrine.State == RiskShrineState.RewardClaimed)
            RunProfile.RiskShrine.PendingReward = null;
        else if (RunProfile.RiskShrine.State == RiskShrineState.RewardAvailable
                 && RunProfile.RiskShrine.PendingReward == null)
            RunProfile.RiskShrine.PendingReward =
                RiskShrineProgress.RollRare(RunProfile.RunSeed);
    }

    static System.Collections.Generic.List<SkillVariantEntry> CaptureVariants()
    {
        var list = new System.Collections.Generic.List<SkillVariantEntry>();
        foreach (var kv in RunProfile.SkillVariants)
        {
            var parts = kv.Key.Split(':');
            if (parts.Length != 2) continue;
            list.Add(new SkillVariantEntry { classId = parts[0], skill = parts[1], variant = kv.Value });
        }
        return list;
    }

    static void ApplyVariants(SaveGame save)
    {
        RunProfile.SkillVariants.Clear();
        if (save.skillVariants == null) return;
        foreach (var e in save.skillVariants)
        {
            if (e == null || string.IsNullOrEmpty(e.classId) || string.IsNullOrEmpty(e.skill)) continue;
            if (!System.Enum.TryParse<DiabloSkills.Skill>(e.skill, out var skill)) continue;
            if (!SkillVariantCatalog.IsKnownVariant(e.classId, skill, e.variant)) continue;
            RunProfile.SkillVariants[e.classId + ":" + skill] = e.variant;
        }
        RunProfile.KindledRewardPending = save.kindledRewardPending;
        RunProfile.KindledRewardClass = save.kindledRewardClass ?? "";
        RunProfile.KindledRewardGranted = save.kindledRewardGranted;
    }

    /// <summary>Persist an explicit snapshot (S4 point writes go through here).</summary>
    public static void WriteSave(SaveGame save, string path)
    {
        // Dedicated server: players own their characters, the server keeps no save.
        if (DedicatedServer.RequestedByProcess) return;
        foreach (var item in SaveMigrations.StoredItems(save))
            if (item != null && !string.IsNullOrEmpty(item.baseType)) item.EnsureIdentity();
        SaveMigrations.BackupOlderVersion(path);
        string json = JsonUtility.ToJson(save, prettyPrint: true);
        string tmp = path + ".tmp";
        File.WriteAllText(tmp, json);
        if (File.Exists(path)) File.Delete(path);
        File.Move(tmp, path);
        Debug.Log($"[Save] Saved level {save.level} ({save.gold} gold, zone {save.zoneReached}).");
        CloudHeroSync.NotifyLocalWrite(path); // accounts S3: stamp + queue the cloud upload
    }

    /// <summary>
    /// Read-only look at the save for menus (class, level, gold). Never backs
    /// up, migrates on disk or marks corrupt; false for missing/unreadable/future.
    /// </summary>
    public static bool TryPeek(out SaveGame save, string path = null)
    {
        save = null;
        string json;
        try { json = File.ReadAllText(path ?? DefaultPath); }
        catch (Exception) { return false; }
        try
        {
            var outcome = SaveMigrations.Migrate(json, out save, out _);
            if (outcome == SaveMigrations.Outcome.Current || outcome == SaveMigrations.Outcome.Migrated) return save != null;
        }
        catch (Exception) { }
        save = null;
        return false;
    }

    static bool TryRead(string path, out SaveGame save)
    {
        save = null;
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception)
        {
            return false; // missing/unreadable: clean fresh start, nothing to back up
        }
        switch (SaveMigrations.Migrate(json, out save, out int fromVersion))
        {
            case SaveMigrations.Outcome.Current:
                return true;
            case SaveMigrations.Outcome.Migrated:
                SaveMigrations.BackupOlderVersion(path);
                Debug.Log($"[Save] Migrated save v{fromVersion} -> v{SaveGame.CurrentVersion} " +
                          $"(backup {SaveMigrations.BackupPath(path, fromVersion)}).");
                return true;
            case SaveMigrations.Outcome.FutureVersion:
                Debug.LogWarning($"[Save] Save version v={fromVersion} is newer than " +
                                 $"v{SaveGame.CurrentVersion}: not loaded, file left untouched.");
                save = null;
                return false;
            default:
                Debug.LogWarning("[Save] Save unreadable: backing up to *.corrupt, starting fresh.");
                MarkCorrupt(path);
                save = null;
                return false;
        }
    }

    static int ReadZoneOrDefault(string path)
    {
        try
        {
            if (!File.Exists(path)) return 1;
            var save = JsonUtility.FromJson<SaveGame>(File.ReadAllText(path));
            return save != null && save.zoneReached >= 1 ? save.zoneReached : 1;
        }
        catch (Exception)
        {
            return 1;
        }
    }

    static void MarkCorrupt(string path)
    {
        try
        {
            string dest = path + ".corrupt";
            if (File.Exists(dest)) File.Delete(dest);
            File.Move(path, dest);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Save] Could not back up corrupt save: {e.Message}");
        }
    }
}
