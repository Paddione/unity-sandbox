using System.Collections;
using UnityEngine;
using Unity.Services.Authentication;

/// <summary>
/// Live account check against the real UGS project (cinderreach-accounts S2),
/// driven by <c>scripts/account-smoke.sh</c>. Two player starts on one
/// explicit <c>-profile</c>:
/// <c>-account-smoke create</c>: guest → create account (same player id) → name;
/// <c>-account-smoke restore</c>: restored account session → sign out (new
/// guest) → sign in (original id again);
/// <c>-account-smoke pc2</c> (second profile and save file = another PC): a
/// guest hero meets the account's cloud hero → conflict → keep the account
/// hero → it arrives locally → delete the test account.
/// Credentials come from CR_SMOKE_USER / CR_SMOKE_PASS, never from the command line.
/// Every verdict is one <c>ACCOUNT_SMOKE</c> log line; exit 0 = all good.
/// </summary>
public sealed class AccountSmoke : MonoBehaviour
{
    public const string Flag = "-account-smoke";
    const string Tag = "ACCOUNT_SMOKE";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == Flag)
            {
                var go = new GameObject("AccountSmoke");
                DontDestroyOnLoad(go);
                go.AddComponent<AccountSmoke>().mode = args[i + 1];
                return;
            }
    }

    string mode;
    int failures;

    void Check(bool ok, string what)
    {
        if (!ok) failures++;
        Debug.Log($"{Tag} {(ok ? "ok" : "FAIL")} {what}");
    }

    static IEnumerator Await(System.Threading.Tasks.Task t)
    {
        while (!t.IsCompleted) yield return null;
    }

    IEnumerator Start()
    {
        string user = System.Environment.GetEnvironmentVariable("CR_SMOKE_USER");
        string pass = System.Environment.GetEnvironmentVariable("CR_SMOKE_PASS");
        var signIn = PlayerAccount.EnsureSignedInAsync();
        yield return Await(signIn);
        Check(signIn.Result, $"signed in profile={PlayerAccount.Profile} throwaway={PlayerAccount.ThrowawayProfile}");
        if (!signIn.Result || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass)) { Finish(); yield break; }

        if (mode == "create")
        {
            Check(PlayerAccount.IsGuest, "starts as a guest");
            string guestId = PlayerAccount.PlayerId;
            var up = PlayerAccount.SignUpAsync(user, pass);
            yield return Await(up);
            Check(up.Result == null, $"create account ({up.Result ?? "ok"})");
            Check(PlayerAccount.PlayerId == guestId, "guest keeps its player id");
            Check(!PlayerAccount.IsGuest && string.Equals(PlayerAccount.Username, user, System.StringComparison.OrdinalIgnoreCase), $"account user={PlayerAccount.Username}");
            var taken = PlayerAccount.SignUpAsync(user, pass);
            yield return Await(taken);
            Check(taken.Result != null, $"second sign-up refused ({taken.Result})");
            var name = PlayerAccount.SetDisplayNameAsync("SmokeHero");
            yield return Await(name);
            Check(name.Result == "SmokeHero", $"display name {name.Result}");
            // S3: a hero written while signed in reaches the account.
            var hero = new SaveGame { level = 7, gold = 123, classId = ClassCatalog.RogueId };
            SaveService.WriteSave(hero, SaveService.DefaultPath);
            var flush = CloudHeroSync.FlushAsync();
            yield return Await(flush);
            Check(CloudHeroSync.Enabled, "cloud sync enabled for the account");
            var meta = CloudHeroSync.ReadMeta(SaveService.DefaultPath);
            Check(meta.owner == guestId && meta.syncedTicks == meta.savedTicks && meta.savedTicks > 0,
                $"hero uploaded (owner={meta.owner}, synced={meta.syncedTicks == meta.savedTicks})");
            PlayerPrefs.SetString("cinderreach.smokeId", guestId);
            PlayerPrefs.Save();
        }
        else if (mode == "restore")
        {
            string id = PlayerPrefs.GetString("cinderreach.smokeId", "");
            Check(!PlayerAccount.IsGuest && PlayerAccount.PlayerId == id, $"restart restores the account (user={PlayerAccount.Username})");
            Check(PlayerAccount.DisplayName == "SmokeHero", $"name survives restart ({PlayerAccount.DisplayName})");
            var outTask = PlayerAccount.SignOutAsync();
            yield return Await(outTask);
            Check(PlayerAccount.IsGuest && PlayerAccount.PlayerId != id, "sign-out continues as a new guest");
            var wrong = PlayerAccount.SignInAsync(user, pass + "x");
            yield return Await(wrong);
            Check(wrong.Result == Loc.T("account.err.wrong"), $"wrong password text ({wrong.Result})");
            Check(PlayerAccount.IsGuest && PlayerAccount.PlayerId != id, "failed sign-in stays a guest");
            var taken = PlayerAccount.SignUpAsync(user, pass);
            yield return Await(taken);
            Check(taken.Result == Loc.T("account.err.taken"), $"taken username text ({taken.Result})");
            var right = PlayerAccount.SignInAsync(user, pass);
            yield return Await(right);
            Check(right.Result == null && PlayerAccount.PlayerId == id && PlayerAccount.DisplayName == "SmokeHero",
                "sign-in returns the original player id and name");
        }
        else if (mode == "pc2")
        {
            Check(PlayerAccount.IsGuest, "second PC starts as a guest");
            Check(!CloudHeroSync.Enabled, "guests never sync");
            SaveService.WriteSave(new SaveGame { level = 2, gold = 5, classId = ClassCatalog.FenwalkerId }, SaveService.DefaultPath);
            SaveGame conflictCloud = null, conflictLocal = null;
            CloudHeroSync.Conflict += (c, l) => { conflictCloud = c; conflictLocal = l; };
            var signIn2 = PlayerAccount.SignInAsync(user, pass); // AccountChanged starts the sync
            yield return Await(signIn2);
            Check(signIn2.Result == null, "second PC signs in");
            float until = Time.realtimeSinceStartup + 20f;
            while (!CloudHeroSync.ConflictPending && Time.realtimeSinceStartup < until) yield return null;
            Check(CloudHeroSync.ConflictPending && conflictCloud != null && conflictLocal != null
                  && conflictCloud.level == 7 && conflictLocal.level == 2,
                $"guest hero vs. account hero asks (cloud lv {conflictCloud?.level}, pc lv {conflictLocal?.level})");
            var keep = CloudHeroSync.ResolveConflictAsync(keepCloud: true);
            yield return Await(keep);
            Check(SaveService.TryPeek(out var arrived) && arrived.level == 7 && arrived.gold == 123
                  && arrived.classId == ClassCatalog.RogueId, $"account hero arrived (lv {arrived?.level}, {arrived?.gold} gold)");
            Check(System.IO.File.Exists(SaveService.DefaultPath + ".pc-hero.bak"), "this PC's hero is backed up");
            var del = AuthenticationService.Instance.DeleteAccountAsync();
            yield return Await(del);
            Check(!del.IsFaulted, "test account deleted");
        }
        else Check(false, $"unknown mode {mode}");
        Finish();
    }

    void Finish()
    {
        Debug.Log($"{Tag} DONE mode={mode} failures={failures}");
        Application.Quit(failures == 0 ? 0 : 1);
    }
}
