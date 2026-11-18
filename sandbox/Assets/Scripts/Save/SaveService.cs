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

    public static string DefaultPath =>
        Path.Combine(Application.persistentDataPath, FileName);

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
        return eq != null && (eq.armor != null || eq.weapon != null || eq.helmet != null);
    }

    public static bool HasSave(string path = null) => File.Exists(path ?? DefaultPath);

    /// <summary>Delete the save (New Game, after the caller's confirmation).</summary>
    public static void DeleteSave(string path = null)
    {
        string p = path ?? DefaultPath;
        if (File.Exists(p)) File.Delete(p);
        LoadedZoneReached = 1;
        RunProfile.Reset();
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
        var save = new SaveGame
        {
            zoneReached = Mathf.Max(1, zoneReached),
            difficultyUnlocked = RunProfile.DifficultyUnlocked,
            difficulty = RunProfile.Difficulty,
            runSeed = RunProfile.RunSeed,
            stash = RunProfile.Stash.ToList(),
        };
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
        }
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
    }

    /// <summary>Persist an explicit snapshot (S4 point writes go through here).</summary>
    public static void WriteSave(SaveGame save, string path)
    {
        SaveMigrations.BackupOlderVersion(path);
        string json = JsonUtility.ToJson(save, prettyPrint: true);
        string tmp = path + ".tmp";
        File.WriteAllText(tmp, json);
        if (File.Exists(path)) File.Delete(path);
        File.Move(tmp, path);
        Debug.Log($"[Save] Saved level {save.level} ({save.gold} gold, zone {save.zoneReached}).");
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
