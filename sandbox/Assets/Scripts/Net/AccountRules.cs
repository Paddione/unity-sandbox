/// <summary>
/// Username/password rules and error texts (cinderreach-accounts S2), pure so
/// EditMode pins them. The rules mirror Unity Authentication's: username
/// 3–20 characters of letters, digits and <c>. - @ _</c>; password 8–30
/// characters with at least one lower-case letter, upper-case letter, digit
/// and symbol. Checking locally first saves a round trip and gives a clear text.
/// </summary>
public static class AccountRules
{
    public const int UserMin = 3, UserMax = 20, PassMin = 8, PassMax = 30;

    // Unity Authentication error codes (AuthenticationErrorCodes / CommonErrorCodes).
    public const int CodeInvalidParameters = 10002;
    public const int CodeAccountAlreadyLinked = 10003;
    public const int CodeBannedUser = 10009;
    public const int CodeTransportError = 1;
    public const int CodeTooManyRequests = 50;

    /// <summary>Error text for a username, or null when it is valid.</summary>
    public static string CheckUsername(string user)
    {
        if (string.IsNullOrEmpty(user) || user.Length < UserMin || user.Length > UserMax)
            return Loc.F("account.rule.userlen", UserMin, UserMax);
        foreach (char c in user)
            if (!(c < 128 && char.IsLetterOrDigit(c)) && c != '.' && c != '-' && c != '@' && c != '_')
                return Loc.T("account.rule.userchars");
        return null;
    }

    /// <summary>Error text for a password, or null when it is valid.</summary>
    public static string CheckPassword(string pass)
    {
        if (string.IsNullOrEmpty(pass) || pass.Length < PassMin || pass.Length > PassMax)
            return Loc.F("account.rule.passlen", PassMin, PassMax);
        bool lower = false, upper = false, digit = false, symbol = false;
        foreach (char c in pass)
        {
            if (char.IsLower(c)) lower = true;
            else if (char.IsUpper(c)) upper = true;
            else if (char.IsDigit(c)) digit = true;
            else symbol = true;
        }
        if (!(lower && upper && digit && symbol))
            return Loc.T("account.rule.passmix");
        return null;
    }

    /// <summary>First error for the pair, or null when both are valid.</summary>
    public static string Check(string user, string pass) => CheckUsername(user) ?? CheckPassword(pass);

    /// <summary>Player-facing text for a failed sign-in or sign-up.</summary>
    public static string Explain(int code, string message, bool signUp)
    {
        string m = message ?? "";
        if (m.IndexOf("Invalid username or password", System.StringComparison.OrdinalIgnoreCase) >= 0
            || m.IndexOf("WRONG_USERNAME_PASSWORD", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return Loc.T("account.err.wrong");
        if (code == CodeAccountAlreadyLinked || m.IndexOf("ENTITY_EXISTS", System.StringComparison.OrdinalIgnoreCase) >= 0
            || m.IndexOf("already exists", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return signUp ? Loc.T("account.err.taken") : Loc.T("account.err.linked");
        if (code == CodeBannedUser) return Loc.T("account.err.banned");
        if (code == CodeTooManyRequests) return Loc.T("account.err.toomany");
        if (code == CodeTransportError) return Loc.T("account.err.noconn");
        if (code == CodeInvalidParameters) return signUp ? Loc.T("account.err.rejected") : Loc.T("account.err.wrong");
        return signUp ? Loc.T("account.err.create") : Loc.T("account.err.signin");
    }
}
