using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;

/// <summary>
/// Player identity (cinderreach-accounts S1): one UGS initialisation per
/// process with an auth profile, a guest (anonymous) sign-in that survives
/// restarts, and the display name other players see.
///
/// Profiles replace the old "clear the credential cache on every start":
/// two game copies on one PC share one PlayerPrefs cache, and the same
/// identity cannot join its own session twice. A normal start uses profile
/// <see cref="DefaultProfile"/> (or <c>-profile NAME</c>) and keeps its
/// identity; automation and server runs get a throwaway profile per process.
/// </summary>
public static class PlayerAccount
{
    public const string ProfileFlag = "-profile";
    public const string PlayerNameFlag = "-player-name";
    public const string DefaultProfile = "main";

    /// <summary>PlayerPrefs key of the local fallback name (offline or before UGS answers).</summary>
    public const string LocalNameKey = "cinderreach.playerName";

    /// <summary>Flags that mark a run as automation (no lasting identity).</summary>
    static readonly string[] AutomationFlags =
        { "-autoplay", "-coop-autoplay", "-shotcheck", "-server", "--net-server", "-server-relay", SaveService.SaveFileFlag };

    static bool ready;
    static Task<bool> initTask;

    /// <summary>True once UGS is initialised and a player is signed in.</summary>
    public static bool SignedIn => ready && AuthenticationService.Instance.IsSignedIn;

    /// <summary>UGS player id, or "" while offline.</summary>
    public static string PlayerId => SignedIn ? AuthenticationService.Instance.PlayerId : "";

    /// <summary>Profile this process signed in with (set by <see cref="EnsureSignedInAsync"/>).</summary>
    public static string Profile { get; private set; } = "";

    /// <summary>True when the profile is a throwaway (automation, server).</summary>
    public static bool ThrowawayProfile { get; private set; }

    /// <summary>The name this player shows to others (cleaned, never null).</summary>
    public static string DisplayName
    {
        get => displayName ??= LocalName(); // lazy: PlayerPrefs is off-limits during type init
        private set => displayName = value;
    }

    static string displayName;

    /// <summary>Raised whenever <see cref="DisplayName"/> changes.</summary>
    public static event System.Action NameChanged;

    /// <summary>
    /// Profile for these arguments (pure): <c>-profile NAME</c> when given and
    /// valid, else a throwaway <c>auto&lt;pid&gt;</c> for automation/server
    /// runs, else <see cref="DefaultProfile"/>.
    /// </summary>
    public static string ProfileFor(string[] args, int pid, out bool throwaway)
    {
        throwaway = false;
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == ProfileFlag && IsValidProfile(args[i + 1])) return args[i + 1];
        foreach (var a in args)
            foreach (var f in AutomationFlags)
                if (a == f)
                {
                    throwaway = true;
                    return "auto" + pid;
                }
        return DefaultProfile;
    }

    /// <summary>UGS profile rule: 1–30 chars of letters, digits, '-' or '_'.</summary>
    public static bool IsValidProfile(string p)
    {
        if (string.IsNullOrEmpty(p) || p.Length > 30) return false;
        foreach (char c in p)
            if (!(char.IsLetterOrDigit(c) && c < 128) && c != '-' && c != '_') return false;
        return true;
    }

    /// <summary>
    /// UGS init + sign-in, once per process. Reuses a cached session (guest or
    /// account) on the profile, else signs in as a guest. False when the
    /// services are unreachable (callers fall back to offline/direct play).
    /// </summary>
    public static Task<bool> EnsureSignedInAsync()
    {
        if (ready && AuthenticationService.Instance.IsSignedIn) return Task.FromResult(true);
        if (initTask == null || initTask.IsCompleted) initTask = InitAsync();
        return initTask;
    }

    static async Task<bool> InitAsync()
    {
        try
        {
            var args = System.Environment.GetCommandLineArgs();
            Profile = ProfileFor(args, System.Diagnostics.Process.GetCurrentProcess().Id, out bool throwaway);
            ThrowawayProfile = throwaway;
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
                await UnityServices.InitializeAsync(new InitializationOptions().SetProfile(Profile));
            var auth = AuthenticationService.Instance;
            if (throwaway)
            {
                try { auth.SignOut(clearCredentials: true); }
                catch { /* nothing cached yet */ }
            }
            if (!auth.IsSignedIn) await auth.SignInAnonymouslyAsync();
            ready = true;
            Debug.Log($"[Account] signed in profile={Profile} player={auth.PlayerId} guest={IsGuest}");
            await LoadNameAsync(ArgValue(args, PlayerNameFlag));
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Account] online services unavailable, playing offline ({e.Message}).");
            return false;
        }
    }

    /// <summary>True for an anonymous identity (no username attached).</summary>
    public static bool IsGuest
    {
        get
        {
            if (!SignedIn) return true;
            var info = AuthenticationService.Instance.PlayerInfo;
            return info == null || string.IsNullOrEmpty(info.Username);
        }
    }

    /// <summary>Account username, or "" for guests and offline.</summary>
    public static string Username
    {
        get
        {
            if (!SignedIn) return "";
            var info = AuthenticationService.Instance.PlayerInfo;
            return info != null && info.Username != null ? info.Username : "";
        }
    }

    /// <summary>
    /// Pick the display name after sign-in: an explicit <c>-player-name</c>
    /// wins (and is stored), else the UGS name (auto-generated on first use).
    /// </summary>
    static async Task LoadNameAsync(string forced)
    {
        var clean = PlayerNames.Sanitize(forced);
        if (clean != null)
        {
            if (!ThrowawayProfile) await SetDisplayNameAsync(clean);
            else SetLocal(clean); // automation: no UGS name writes for throwaway players
            return;
        }
        try
        {
            string ugs = await AuthenticationService.Instance.GetPlayerNameAsync();
            var shown = PlayerNames.Sanitize(ugs);
            if (shown != null) SetLocal(shown);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Account] player name unavailable ({e.Message}).");
        }
    }

    /// <summary>
    /// Rename: cleans the name, stores it in UGS (when signed in) and locally.
    /// Returns the name now shown, or null when the input was not a valid name.
    /// </summary>
    public static async Task<string> SetDisplayNameAsync(string raw)
    {
        var clean = PlayerNames.Sanitize(raw);
        if (clean == null) return null;
        if (SignedIn)
        {
            try
            {
                string stored = await AuthenticationService.Instance.UpdatePlayerNameAsync(clean);
                clean = PlayerNames.Sanitize(stored) ?? clean;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Account] name not stored online ({e.Message}); kept locally.");
            }
        }
        SetLocal(clean);
        return clean;
    }

    static void SetLocal(string clean)
    {
        if (clean == DisplayName) return;
        DisplayName = clean;
        if (!ThrowawayProfile)
        {
            PlayerPrefs.SetString(LocalNameKey, clean);
            PlayerPrefs.Save();
        }
        Debug.Log($"[Account] display name: {clean}");
        NameChanged?.Invoke();
    }

    /// <summary>Name before (or without) UGS: <c>-player-name</c>, else the stored local name.</summary>
    static string LocalName() =>
        PlayerNames.Sanitize(ArgValue(System.Environment.GetCommandLineArgs(), PlayerNameFlag))
        ?? PlayerNames.Display(PlayerPrefs.GetString(LocalNameKey, ""));

    static string ArgValue(string[] args, string flag)
    {
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == flag) return args[i + 1];
        return null;
    }
}
