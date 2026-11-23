using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Start-screen account window (cinderreach-accounts). S1: the name other
/// players see, with rename. Opened from the "Playing as" button in the
/// start screen's title row; lives on the start-screen canvas.
/// </summary>
public sealed class AccountPanel
{
    public GameObject Root { get; private set; }
    public InputField NameField { get; private set; }
    public Text Status { get; private set; }

    public bool IsOpen => Root != null && Root.activeSelf;

    /// <summary>Build the (hidden) panel under the start-screen canvas.</summary>
    public static AccountPanel Build(Transform canvas)
    {
        var p = new AccountPanel();
        p.Root = DiabloUIFactory.MakePanel(canvas, "AccountPanel", new Vector2(560f, 330f), Vector2.zero);
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

        p.Status = DiabloUIFactory.MakeText(p.Root.transform, "AccountStatus", "", 15, DiabloPalette.Parchment, TextAnchor.MiddleLeft);
        DiabloUIFactory.SetLayout(p.Status.gameObject, prefH: 48f, flexW: 1f);
        p.Root.SetActive(false);
        return p;
    }

    public void SetOpen(bool open)
    {
        if (Root == null) return;
        Root.SetActive(open);
        if (!open) return;
        NameField.text = PlayerAccount.DisplayName;
        Status.text = StatusLine();
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
