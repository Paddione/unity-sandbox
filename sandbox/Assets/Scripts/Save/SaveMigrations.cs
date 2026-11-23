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
        if (save.v == 5) V5ToV6(save);
        if (save.v == 6) V6ToV7(save);
        if (save.v == 7) V7ToV8(save);
        if (save.v == 8) V8ToV9(save);
        if (save.v == 9) V9ToV10(save, json);
        if (save.v == 10) V10ToV11(save);
        if (save.v == 11) V11ToV12(save);
        if (save.v == 12) V12ToV13(save);
        if (save.v == 13) V13ToV14(save);
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

    /// <summary>
    /// v5 → v6: v5-Felder (Zone-Contracts, unbekannt) bleiben unberührt;
    /// fehlende v6-Felder erhalten Defaults. Kompatibel, egal ob v5 zuerst landet.
    /// </summary>
    static void V5ToV6(SaveGame save)
    {
        save.smithingMaterial = System.Math.Max(0, save.smithingMaterial);
        EnsureSmithingFields(save);
        save.v = 6;
    }

    /// <summary>Tier-1-Default für Einträge ohne Stufe (0 = vor-smithing).</summary>
    static void EnsureSmithingFields(SaveGame save)
    {
        foreach (var item in AllEquippedAndStored(save))
        {
            if (item == null) continue;
            if (item.affixTier < 1) item.affixTier = 1;
            if (item.reforgeIndexPlusOne < 0) item.reforgeIndexPlusOne = 0;
        }
    }

    static System.Collections.Generic.IEnumerable<ItemInstance> AllEquippedAndStored(SaveGame save)
    {
        if (save.equipmentArmor != null) yield return save.equipmentArmor;
        if (save.equipmentWeapon != null) yield return save.equipmentWeapon;
        if (save.equipmentHelmet != null) yield return save.equipmentHelmet;
        if (save.bag != null) foreach (var i in save.bag) yield return i;
        if (save.stash != null) foreach (var i in save.stash) yield return i;
    }
    /// <summary>
    /// v6 → v7: v6 fields (contracts, smithing) stay untouched; default only
    /// the variant picks we add. v5 always flows through v6 first.
    /// </summary>
    static void V6ToV7(SaveGame save)
    {
        if (save.skillVariants == null)
            save.skillVariants = new System.Collections.Generic.List<SkillVariantEntry>();
        save.v = 7;
    }

    /// <summary>
    /// v7 → v8: the risk shrine starts unchosen (older saves predate it).
    /// v5–v7 fields stay untouched.
    /// </summary>
    static void V7ToV8(SaveGame save)
    {
        save.riskShrineState = (int)RiskShrineState.Unchosen;
        save.riskReward = null;
        save.v = 8;
    }

    /// <summary>
    /// v8 → v9: first-Kindled-victory claim defaults to closed; older
    /// completed runs get no retroactive claim.
    /// </summary>
    static void V8ToV9(SaveGame save)
    {
        if (save.kindledRewardClass == null) save.kindledRewardClass = "";
        save.v = 9;
    }

    static void V9ToV10(SaveGame save, string originalJson)
    {
        int index = 0;
        using var hash = System.Security.Cryptography.SHA256.Create();
        string fingerprint = System.Convert.ToBase64String(hash.ComputeHash(
            System.Text.Encoding.UTF8.GetBytes(originalJson)));
        foreach (var item in StoredItems(save))
        {
            if (item == null || string.IsNullOrEmpty(item.baseType)) continue;
            if (string.IsNullOrEmpty(item.instanceId))
            {
                // Loading the same pre-v10 save before its next write yields the same IDs.
                var bytes = System.Text.Encoding.UTF8.GetBytes($"{fingerprint}:{index}");
                var digest = hash.ComputeHash(bytes);
                System.Array.Resize(ref digest, 16);
                item.instanceId = new System.Guid(digest).ToString("N");
            }
            index++;
        }
        save.v = 10;
    }

    /// <summary>
    /// v10 → v11 (act-two-opening S1): a hero who already felled the Kilnmother
    /// finds the Glass Barrens open; nobody has beaten the new zone boss yet.
    /// </summary>
    static void V10ToV11(SaveGame save)
    {
        if (save.actOneComplete && save.highestUnlockedZone >= ActProgress.Kiln)
            save.highestUnlockedZone = ActProgress.Barrens;
        save.actTwoBossDefeated = false;
        save.v = 11;
    }

    /// <summary>
    /// v11 → v12 (playtest feedback 2026-11-23): Magic items are retired, every
    /// stored one (gear, bag, stash, shrine reward) becomes Rare with its affixes.
    /// </summary>
    static void V11ToV12(SaveGame save)
    {
        foreach (var item in StoredItems(save)) ItemDatabase.RetireMagic(item);
        save.v = 12;
    }

    /// <summary>
    /// v12 → v13 (cinderreach-deutsch-story S4): no story progress yet; the
    /// act state already in the save drives StoryState catch-up on first load.
    /// </summary>
    static void V12ToV13(SaveGame save)
    {
        if (save.storyQuests == null) save.storyQuests = new System.Collections.Generic.List<StoryQuestEntry>();
        if (save.storyFlags == null) save.storyFlags = new System.Collections.Generic.List<string>();
        save.v = 13;
    }

    /// <summary>
    /// v13 → v14 (character-creation): pre-creation heroes get a default look
    /// and a random hero name seeded by the run (account name stays untouched).
    /// </summary>
    static void V13ToV14(SaveGame save)
    {
        var p = HeroProfile.MigrationDefault(save.runSeed ^ (save.level * 397));
        save.heroName = p.heroName;
        save.heroGender = p.gender;
        save.heroHairStyle = p.hairStyle;
        save.heroHairColor = p.hairColor;
        save.heroBuild = p.build;
        save.v = 14;
    }

    internal static System.Collections.Generic.IEnumerable<ItemInstance> StoredItems(SaveGame save)
    {
        foreach (var item in AllEquippedAndStored(save)) yield return item;
        yield return save.riskReward;
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
