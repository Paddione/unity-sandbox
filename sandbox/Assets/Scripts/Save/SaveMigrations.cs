using UnityEngine;

/// <summary>
/// Save-format migrations (replayability S1). Reads the version header
/// <c>v</c> first, lifts older saves step by step to
/// <see cref="SaveGame.CurrentVersion"/> and refuses unknown future
/// versions without touching the file.
/// </summary>
public static class SaveMigrations
{
    public enum Outcome { Current, Migrated, FutureVersion, Corrupt }

    [System.Serializable]
    sealed class Header { public int v; }

    /// <summary>Parse and migrate <paramref name="json"/>. Only Current and Migrated yield a save.</summary>
    public static Outcome Migrate(string json, out SaveGame save, out int fromVersion)
    {
        save = null;
        fromVersion = 0;
        if (string.IsNullOrEmpty(json)) return Outcome.Corrupt;
        Header header;
        try
        {
            header = JsonUtility.FromJson<Header>(json);
        }
        catch (System.Exception)
        {
            return Outcome.Corrupt;
        }
        if (header == null || header.v < 1) return Outcome.Corrupt;
        fromVersion = header.v;
        if (header.v > SaveGame.CurrentVersion) return Outcome.FutureVersion;

        try
        {
            save = JsonUtility.FromJson<SaveGame>(json);
        }
        catch (System.Exception)
        {
            save = null;
        }
        if (save == null) return Outcome.Corrupt;
        if (header.v == SaveGame.CurrentVersion) return Outcome.Current;

        if (save.v == 1) V1ToV2(save);
        if (save.v == 2) V2ToV3(save);
        if (save.v == 3) V3ToV4(save);
        if (save.v == 4) V4ToV5(save);
        return Outcome.Migrated;
    }

    /// <summary>v1 → v2: Kindled locked, Normal, fresh seed, empty stash.</summary>
    static void V1ToV2(SaveGame save)
    {
        save.difficultyUnlocked = false;
        save.difficulty = RunProfile.Normal;
        save.runSeed = RunProfile.NewSeed();
        save.stash = new System.Collections.Generic.List<ItemInstance>();
        save.v = 2;
    }

    /// <summary>v2 → v3: every pre-class hero is a Fenwalker.</summary>
    static void V2ToV3(SaveGame save)
    {
        save.classId = ClassCatalog.FenwalkerId;
        save.v = 3;
    }

    /// <summary>
    /// v3 → v4: the Ossuary was always open; a save that already unlocked Kindled
    /// has beaten the Hollow Abbot, so the Cinder Kiln opens for it too.
    /// </summary>
    static void V3ToV4(SaveGame save)
    {
        save.highestUnlockedZone = save.difficultyUnlocked ? ActProgress.Kiln : ActProgress.Ossuary;
        save.actOneComplete = false;
        save.waypoints = new System.Collections.Generic.List<string>();
        save.v = 4;
    }

    /// <summary>
    /// v4 → v5: empty contract state; older saves never held a contract.
    /// An active target position set by newer code survives verbatim.
    /// </summary>
    static void V4ToV5(SaveGame save)
    {
        if (save.contract == null) save.contract = new ContractSnapshot();
        save.v = 5;
    }

    /// <summary>Version header of an existing file, 0 when missing/unreadable.</summary>
    public static int ReadVersion(string path)
    {
        try
        {
            if (!System.IO.File.Exists(path)) return 0;
            var h = JsonUtility.FromJson<Header>(System.IO.File.ReadAllText(path));
            return h != null ? h.v : 0;
        }
        catch (System.Exception)
        {
            return 0;
        }
    }

    /// <summary>Backup path for a save of version <paramref name="v"/> (<c>*.v1.bak</c>).</summary>
    public static string BackupPath(string path, int v) => $"{path}.v{v}.bak";

    /// <summary>Copy an older-version save aside once, before it is overwritten.</summary>
    public static void BackupOlderVersion(string path)
    {
        int v = ReadVersion(path);
        if (v < 1 || v >= SaveGame.CurrentVersion) return;
        string bak = BackupPath(path, v);
        try
        {
            if (!System.IO.File.Exists(bak)) System.IO.File.Copy(path, bak);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Save] Could not back up v{v} save: {e.Message}");
        }
    }
}
