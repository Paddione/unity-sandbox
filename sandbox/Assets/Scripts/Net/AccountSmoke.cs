using System.Collections;
using UnityEngine;
using Unity.Services.Authentication;

/// <summary>
/// Live account check against the real UGS project (cinderreach-accounts S2),
/// driven by <c>scripts/account-smoke.sh</c>. Two player starts on one
/// explicit <c>-profile</c>:
/// <c>-account-smoke create</c>: guest → create account (same player id) → name;
/// <c>-account-smoke restore</c>: restored account session → sign out (new
/// guest) → sign in (original id again) → delete the test account.
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
            Check(wrong.Result == "Wrong username or password.", $"wrong password text ({wrong.Result})");
            Check(PlayerAccount.IsGuest && PlayerAccount.PlayerId != id, "failed sign-in stays a guest");
            var taken = PlayerAccount.SignUpAsync(user, pass);
            yield return Await(taken);
            Check(taken.Result == "That username is taken.", $"taken username text ({taken.Result})");
            var right = PlayerAccount.SignInAsync(user, pass);
            yield return Await(right);
            Check(right.Result == null && PlayerAccount.PlayerId == id && PlayerAccount.DisplayName == "SmokeHero",
                "sign-in returns the original player id and name");
            var del = AuthenticationService.Instance.DeleteAccountAsync();
            yield return Await(del);
            Check(!del.IsFaulted, "test account deleted");
            PlayerPrefs.DeleteKey("cinderreach.smokeId");
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
