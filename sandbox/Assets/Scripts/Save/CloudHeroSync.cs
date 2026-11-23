using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.CloudSave;

/// <summary>
/// Cloud hero (cinderreach-accounts S3): the account's hero lives in UGS
/// Cloud Save under <see cref="Key"/> as a <see cref="CloudHeroEnvelope"/>;
/// the local save stays the working copy and the offline fallback.
/// Every local write marks the side-car meta and, for a signed-in account,
/// uploads shortly after. Each sign-in compares both sides through
/// <see cref="CloudHeroResolver"/>. Guests, throwaway profiles (automation)
/// and the dedicated server never sync.
/// </summary>
public static class CloudHeroSync
{
    public const string Key = "hero";
    public const float UploadDelaySeconds = 2f;

    /// <summary>Raised after a sync changed or confirmed the local hero (menus refresh Continue).</summary>
    public static event System.Action Synced;

    /// <summary>Raised when the player must pick between the account's hero and this PC's hero.</summary>
    public static event System.Action<SaveGame, SaveGame> Conflict;

    /// <summary>Last sync outcome for menus and logs.</summary>
    public static string Status { get; private set; } = "";

    /// <summary>Last decision taken (test seam).</summary>
    public static CloudHeroAction LastAction { get; private set; }

    /// <summary>True while a conflict waits for <see cref="ResolveConflictAsync"/>.</summary>
    public static bool ConflictPending { get; private set; }

    static string pendingCloudJson;
    static long pendingCloudTicks;
    static Task uploadTask;
    static float uploadDue = -1f;
    static Pump pump;
    static bool hooked;

    /// <summary>True when this process syncs: a signed-in account, no throwaway profile, no server.</summary>
    public static bool Enabled =>
        PlayerAccount.SignedIn && !PlayerAccount.IsGuest && !PlayerAccount.ThrowawayProfile
        && !DedicatedServer.RequestedByProcess;

    public static string MetaPath(string savePath) => savePath + ".meta.json";

    // ------------------------------------------------------------------ meta

    public static LocalHeroMeta ReadMeta(string savePath)
    {
        var meta = new LocalHeroMeta { exists = File.Exists(savePath), owner = "" };
        try
        {
            string p = MetaPath(savePath);
            if (File.Exists(p))
            {
                var stored = JsonUtility.FromJson<LocalHeroMeta>(File.ReadAllText(p));
                meta.owner = stored.owner ?? "";
                meta.savedTicks = stored.savedTicks;
                meta.syncedTicks = stored.syncedTicks;
            }
            else if (meta.exists)
            {
                meta.savedTicks = File.GetLastWriteTimeUtc(savePath).Ticks; // pre-cloud save: never synced
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CloudHero] meta unreadable ({e.Message}); treating the hero as never synced.");
        }
        return meta;
    }

    public static void WriteMeta(string savePath, LocalHeroMeta meta)
    {
        meta.exists = false; // derived from the save file, never stored as truth
        File.WriteAllText(MetaPath(savePath), JsonUtility.ToJson(meta));
    }

    /// <summary>
    /// <see cref="SaveService.WriteSave"/> hook: stamp the write and, for an
    /// account, queue an upload. A guest write keeps the previous owner, so
    /// the account that owned the hero can still claim the progress.
    /// </summary>
    public static void NotifyLocalWrite(string savePath)
    {
        try
        {
            var meta = ReadMeta(savePath);
            meta.savedTicks = System.DateTime.UtcNow.Ticks;
            if (Enabled) meta.owner = PlayerAccount.PlayerId;
            WriteMeta(savePath, meta);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CloudHero] meta write failed ({e.Message}).");
        }
        if (Enabled && savePath == SaveService.DefaultPath) ScheduleUpload();
    }

    /// <summary><see cref="SaveService.DeleteSave"/> hook: the next hero starts unowned.</summary>
    public static void NotifyLocalDelete(string savePath)
    {
        try
        {
            if (File.Exists(MetaPath(savePath))) File.Delete(MetaPath(savePath));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CloudHero] meta delete failed ({e.Message}).");
        }
    }

    // --------------------------------------------------------------- sign-in

    /// <summary>Start screen: sign in (cached session or guest), then reconcile the hero.</summary>
    public static async Task SignInAndSyncAsync()
    {
        HookAccountChanges();
        if (await PlayerAccount.EnsureSignedInAsync()) await SyncAsync();
    }

    /// <summary>Every sign-in, sign-up or sign-out reconciles the hero, with or without the start screen.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void HookAccountChanges()
    {
        if (hooked) return;
        hooked = true;
        PlayerAccount.AccountChanged += () => _ = SyncAsync();
    }

    /// <summary>Compare local and cloud hero and act (no-op unless <see cref="Enabled"/>).</summary>
    public static async Task SyncAsync()
    {
        HookAccountChanges();
        if (!Enabled) { Status = ""; return; }
        string path = SaveService.DefaultPath;
        string me = PlayerAccount.PlayerId;
        CloudHeroEnvelope cloud;
        try
        {
            cloud = await LoadCloudAsync();
        }
        catch (System.Exception e)
        {
            Status = "Cloud hero unavailable, playing from this PC.";
            Debug.LogWarning($"[CloudHero] load failed ({e.Message}).");
            return;
        }
        var local = ReadMeta(path);
        var cloudMeta = new CloudHeroMeta { exists = cloud != null, savedTicks = cloud != null ? cloud.savedTicks : 0 };
        var action = CloudHeroResolver.Decide(me, local, cloudMeta);
        LastAction = action;
        Debug.Log($"[CloudHero] sync player={me} local(exists={local.exists} owner={(local.owner.Length > 0 ? local.owner : "-")}) " +
                  $"cloud(exists={cloud != null}) -> {action}");
        switch (action)
        {
            case CloudHeroAction.Upload:
                await UploadNowAsync();
                break;
            case CloudHeroAction.Download:
                ApplyCloud(path, cloud, me, backupTag: "pre-cloud");
                Status = "Hero loaded from your account.";
                break;
            case CloudHeroAction.StartFresh:
                SetAside(path, "owner-" + local.owner);
                Status = "This PC's hero belongs to another account; you start fresh.";
                break;
            case CloudHeroAction.Ask:
                ConflictPending = true;
                pendingCloudJson = cloud.save;
                pendingCloudTicks = cloud.savedTicks;
                Status = "Two heroes found: choose which one to keep.";
                SaveService.TryPeek(out var localSave, path);
                ParseSave(cloud.save, out var cloudSave);
                Conflict?.Invoke(cloudSave, localSave);
                return;
            default:
                Status = cloud != null ? "Hero is in sync with your account." : "";
                break;
        }
        Synced?.Invoke();
    }

    /// <summary>The player's answer to <see cref="Conflict"/>: keep the account's hero or this PC's.</summary>
    public static async Task ResolveConflictAsync(bool keepCloud)
    {
        if (!ConflictPending) return;
        ConflictPending = false;
        string path = SaveService.DefaultPath;
        if (keepCloud)
        {
            ApplyCloud(path, new CloudHeroEnvelope { save = pendingCloudJson, savedTicks = pendingCloudTicks },
                PlayerAccount.PlayerId, backupTag: "pc-hero");
            Status = "Kept your account's hero (this PC's hero is backed up).";
        }
        else
        {
            var meta = ReadMeta(path);
            meta.owner = PlayerAccount.PlayerId;
            if (meta.savedTicks <= pendingCloudTicks) meta.savedTicks = System.DateTime.UtcNow.Ticks;
            WriteMeta(path, meta);
            await UploadNowAsync();
            Status = "This PC's hero is now your account's hero.";
        }
        pendingCloudJson = null;
        Synced?.Invoke();
    }

    // ------------------------------------------------------------- transfer

    static async Task<CloudHeroEnvelope> LoadCloudAsync()
    {
        var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { Key });
        if (data == null || !data.TryGetValue(Key, out var item) || item == null) return null;
        string json = item.Value.GetAsString();
        var env = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<CloudHeroEnvelope>(json);
        return env != null && !string.IsNullOrEmpty(env.save) ? env : null;
    }

    /// <summary>Upload the local hero now (also the debounce target). False when nothing was sent.</summary>
    public static async Task<bool> UploadNowAsync()
    {
        uploadDue = -1f;
        if (!Enabled) return false;
        string path = SaveService.DefaultPath;
        if (!File.Exists(path)) return false;
        var meta = ReadMeta(path);
        var env = new CloudHeroEnvelope
        {
            v = SaveGame.CurrentVersion,
            savedTicks = meta.savedTicks,
            owner = PlayerAccount.PlayerId,
            save = File.ReadAllText(path),
        };
        try
        {
            await CloudSaveService.Instance.Data.Player.SaveAsync(
                new Dictionary<string, object> { { Key, JsonUtility.ToJson(env) } });
        }
        catch (System.Exception e)
        {
            Status = "Upload failed; your hero is safe on this PC and syncs next time.";
            Debug.LogWarning($"[CloudHero] upload failed ({e.Message}).");
            return false;
        }
        meta = ReadMeta(path); // a write may have landed during the upload
        if (meta.savedTicks == env.savedTicks) meta.syncedTicks = env.savedTicks;
        meta.owner = env.owner;
        WriteMeta(path, meta);
        Status = "Hero saved to your account.";
        Debug.Log($"[CloudHero] uploaded ({env.save.Length} chars, player={env.owner}).");
        return true;
    }

    /// <summary>Wait for a queued or running upload (smoke/automation seam).</summary>
    public static async Task FlushAsync()
    {
        if (uploadDue >= 0f) await UploadNowAsync();
        if (uploadTask != null) await uploadTask;
    }

    static void ScheduleUpload()
    {
        uploadDue = Time.realtimeSinceStartup + UploadDelaySeconds;
        if (pump == null)
        {
            var go = new GameObject("CloudHeroSync");
            Object.DontDestroyOnLoad(go);
            pump = go.AddComponent<Pump>();
        }
    }

    sealed class Pump : MonoBehaviour
    {
        void Update()
        {
            if (uploadDue < 0f || Time.realtimeSinceStartup < uploadDue) return;
            if (uploadTask != null && !uploadTask.IsCompleted) return;
            uploadTask = UploadNowAsync();
        }
    }

    static void ApplyCloud(string path, CloudHeroEnvelope cloud, string me, string backupTag)
    {
        SetAside(path, backupTag);
        string tmp = path + ".tmp";
        File.WriteAllText(tmp, cloud.save);
        if (File.Exists(path)) File.Delete(path);
        File.Move(tmp, path);
        WriteMeta(path, new LocalHeroMeta { owner = me, savedTicks = cloud.savedTicks, syncedTicks = cloud.savedTicks });
        Debug.Log($"[CloudHero] hero restored from the account ({cloud.save.Length} chars).");
    }

    /// <summary>Move the local hero aside as <c>&lt;save&gt;.&lt;tag&gt;.bak</c> (never deleted).</summary>
    static void SetAside(string path, string tag)
    {
        if (!File.Exists(path)) return;
        string bak = $"{path}.{tag}.bak";
        if (File.Exists(bak)) File.Delete(bak);
        File.Move(path, bak);
        NotifyLocalDelete(path);
        Debug.Log($"[CloudHero] local hero set aside -> {Path.GetFileName(bak)}");
    }

    static void ParseSave(string json, out SaveGame save)
    {
        save = null;
        try { SaveMigrations.Migrate(json, out save, out _); }
        catch (System.Exception) { save = null; }
    }

    /// <summary>Reset statics between tests.</summary>
    public static void ResetForTests()
    {
        ConflictPending = false;
        pendingCloudJson = null;
        uploadDue = -1f;
        Status = "";
        LastAction = CloudHeroAction.None;
    }
}

/// <summary>Cloud Save value under <see cref="CloudHeroSync.Key"/>.</summary>
[System.Serializable]
public sealed class CloudHeroEnvelope
{
    /// <summary><see cref="SaveGame.CurrentVersion"/> of the writer.</summary>
    public int v;
    /// <summary>UTC ticks of the local write this hero came from.</summary>
    public long savedTicks;
    /// <summary>Account player id that uploaded.</summary>
    public string owner;
    /// <summary>The <see cref="SaveGame"/> JSON as written to disk.</summary>
    public string save;
}
