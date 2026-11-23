using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Start-screen account window (cinderreach-accounts). S1: the name other
/// players see, with rename. S2: sign in, create an account (a guest keeps
/// its identity) and sign out. Opened from the "Playing as" button in the
/// start screen's title row; lives on the start-screen canvas.
/// </summary>
public sealed class AccountPanel
{
    public GameObject Root { get; private set; }
    public InputField NameField { get; private set; }
    public Text Status { get; private set; }
    public InputField UserField { get; private set; }
    public InputField PassField { get; private set; }
    GameObject signedOutRow, signOutRow;
    bool busy;

    public bool IsOpen => Root != null && Root.activeSelf;

    /// <summary>Build the (hidden) panel under the start-screen canvas.</summary>
    public static AccountPanel Build(Transform canvas)
    {
        var p = new AccountPanel();
        p.Root = DiabloUIFactory.MakePanel(canvas, "AccountPanel", new Vector2(560f, 640f), Vector2.zero);
        var v = p.Root.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(24, 24, 18, 20);
        v.spacing = 10f;
        v.childControlWidth = true;
        v.childControlHeight = false;
        DiabloUIFactory.MakeHeader(p.Root.transform, "PLAYER", DiabloMenuArt.MenuArtSprite("CrestCinderreach"), () => p.SetOpen(false));

        var hint = DiabloUIFactory.MakeText(p.Root.transform, "NameHint",
            "Other players see this name above your hero.", 15, DiabloPalette.ParchmentDim, TextAnchor.MiddleLeft);
        DiabloUIFactory.SetLayout(hint.gameObject, prefH: 24f, flexW: 1f);
        p.NameField = Field(p.Root.transform, "NameField", PlayerAccount.DisplayName, PlayerNames.MaxLength);

        var row = DiabloUIFactory.MakeHorizontalRow(p.Root.transform, "NameButtons", 12f, 44f);
        var save = DiabloUIFactory.MakeButton(row.transform, "SaveNameButton", "Save name", 16, true, DiabloPalette.GoldBright, () => p.SaveName());
        DiabloUIFactory.SetLayout(save.gameObject, prefH: 42f, flexW: 1f);
        var close = DiabloUIFactory.MakeButton(row.transform, "CloseAccountButton", "Close", 16, false, null, () => p.SetOpen(false));
        DiabloUIFactory.SetLayout(close.gameObject, prefH: 42f, flexW: 1f);

        // S2: account (username + password); a guest's identity carries over on "Create account".
        DiabloUIFactory.MakeDivider(p.Root.transform, 6f);
        var acctHint = DiabloUIFactory.MakeText(p.Root.transform, "AccountHint",
            "An account keeps your name and hero on every PC.", 15, DiabloPalette.ParchmentDim, TextAnchor.MiddleLeft);
        DiabloUIFactory.SetLayout(acctHint.gameObject, prefH: 24f, flexW: 1f);
        p.UserField = Field(p.Root.transform, "UserField", "", AccountRules.UserMax);
        p.UserField.placeholder = Placeholder(p.UserField, "Username");
        p.PassField = Field(p.Root.transform, "PassField", "", AccountRules.PassMax, password: true);
        p.PassField.placeholder = Placeholder(p.PassField, "Password");

        p.signedOutRow = DiabloUIFactory.MakeHorizontalRow(p.Root.transform, "SignInButtons", 12f, 44f);
        var signIn = DiabloUIFactory.MakeButton(p.signedOutRow.transform, "SignInButton", "Sign in", 16, true, DiabloPalette.GoldBright, () => p.SignIn());
        DiabloUIFactory.SetLayout(signIn.gameObject, prefH: 42f, flexW: 1f);
        var signUp = DiabloUIFactory.MakeButton(p.signedOutRow.transform, "SignUpButton", "Create account", 16, false, null, () => p.SignUp());
        DiabloUIFactory.SetLayout(signUp.gameObject, prefH: 42f, flexW: 1f);
        p.signOutRow = DiabloUIFactory.MakeHorizontalRow(p.Root.transform, "SignOutButtons", 12f, 44f);
        var signOut = DiabloUIFactory.MakeButton(p.signOutRow.transform, "SignOutButton", "Sign out", 16, false, DiabloPalette.ParchmentDim, () => p.SignOut());
        DiabloUIFactory.SetLayout(signOut.gameObject, prefH: 42f, flexW: 1f);

        p.Status = DiabloUIFactory.MakeText(p.Root.transform, "AccountStatus", "", 15, DiabloPalette.Parchment, TextAnchor.MiddleLeft);
        DiabloUIFactory.SetLayout(p.Status.gameObject, prefH: 48f, flexW: 1f);
        PlayerAccount.AccountChanged += p.Refresh;
        p.Root.SetActive(false);
        return p;
    }

    public void SetOpen(bool open)
    {
        if (Root == null) return;
        Root.SetActive(open);
        if (!open) return;
        Refresh();
    }

    /// <summary>Re-read identity: name field, status, which buttons fit (guest vs. account).</summary>
    public void Refresh()
    {
        if (Root == null || busy) return;
        NameField.text = PlayerAccount.DisplayName;
        Status.text = StatusLine();
        bool account = PlayerAccount.SignedIn && !PlayerAccount.IsGuest;
        signedOutRow.SetActive(!account);
        UserField.gameObject.SetActive(!account);
        PassField.gameObject.SetActive(!account);
        signOutRow.SetActive(account);
    }

    async void SignIn()
    {
        if (busy) return;
        DiabloAudio.PlayUIClick();
        busy = true;
        Status.text = "Signing in…";
        var error = await PlayerAccount.SignInAsync(UserField.text.Trim(), PassField.text);
        busy = false;
        if (Root == null) return;
        PassField.text = "";
        Refresh();
        if (error != null) Status.text = error;
    }

    async void SignUp()
    {
        if (busy) return;
        DiabloAudio.PlayUIClick();
        busy = true;
        Status.text = "Creating account…";
        var error = await PlayerAccount.SignUpAsync(UserField.text.Trim(), PassField.text);
        busy = false;
        if (Root == null) return;
        if (error == null) PassField.text = "";
        Refresh();
        if (error != null) Status.text = error;
    }

    async void SignOut()
    {
        if (busy) return;
        DiabloAudio.PlayUIClick();
        busy = true;
        Status.text = "Signing out…";
        await PlayerAccount.SignOutAsync();
        busy = false;
        if (Root != null) Refresh();
    }

    static Text Placeholder(InputField field, string text)
    {
        var t = DiabloUIFactory.MakeText(field.transform, "Placeholder", text, 18, DiabloPalette.ParchmentDim, TextAnchor.MiddleLeft);
        t.fontStyle = FontStyle.Italic;
        var tr = t.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(12f, 2f);
        tr.offsetMax = new Vector2(-12f, -2f);
        return t;
    }

    /// <summary>Who is signed in (pure enough for the panel; offline reads as such).</summary>
    public static string StatusLine()
    {
        if (!PlayerAccount.SignedIn) return "Offline: the name is kept on this PC and shown once you play online.";
        return PlayerAccount.IsGuest
            ? "Signed in as a guest on this PC."
            : $"Signed in as {PlayerAccount.Username}.";
    }

    async void SaveName()
    {
        DiabloAudio.PlayUIClick();
        var wanted = NameField.text;
        if (PlayerNames.Sanitize(wanted) == null)
        {
            Status.text = $"Use {PlayerNames.MinLength}–{PlayerNames.MaxLength} letters, digits, '-' or '_'.";
            return;
        }
        Status.text = "Saving…";
        var shown = await PlayerAccount.SetDisplayNameAsync(wanted);
        if (Root == null) return;
        NameField.text = shown ?? PlayerAccount.DisplayName;
        Status.text = shown != null ? $"You are now {shown}." : "That name did not work.";
    }

    /// <summary>Layout-driven text field in the start-screen style.</summary>
    public static InputField Field(Transform parent, string name, string value, int limit, bool password = false)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        DiabloUIFactory.SetLayout(go, prefH: 44f, flexW: 1f);
        var img = go.AddComponent<Image>();
        img.sprite = DiabloMenuArt.TabBackground(false);
        img.type = Image.Type.Sliced;
        var text = DiabloUIFactory.MakeText(go.transform, "Text", "", 18, DiabloPalette.Parchment, TextAnchor.MiddleLeft);
        text.supportRichText = false;
        var tr = text.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(12f, 2f);
        tr.offsetMax = new Vector2(-12f, -2f);
        var field = go.AddComponent<InputField>();
        field.textComponent = text;
        field.characterLimit = limit;
        if (password) field.contentType = InputField.ContentType.Password;
        field.text = value ?? "";
        return field;
    }
}
