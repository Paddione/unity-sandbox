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
            return $"Username: {UserMin}–{UserMax} characters.";
        foreach (char c in user)
            if (!(c < 128 && char.IsLetterOrDigit(c)) && c != '.' && c != '-' && c != '@' && c != '_')
                return "Username: letters, digits and . - @ _ only.";
        return null;
    }

    /// <summary>Error text for a password, or null when it is valid.</summary>
    public static string CheckPassword(string pass)
    {
        if (string.IsNullOrEmpty(pass) || pass.Length < PassMin || pass.Length > PassMax)
            return $"Password: {PassMin}–{PassMax} characters.";
        bool lower = false, upper = false, digit = false, symbol = false;
        foreach (char c in pass)
        {
            if (char.IsLower(c)) lower = true;
            else if (char.IsUpper(c)) upper = true;
            else if (char.IsDigit(c)) digit = true;
            else symbol = true;
        }
        if (!(lower && upper && digit && symbol))
            return "Password needs a lower-case and an upper-case letter, a digit and a symbol.";
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
            return "Wrong username or password.";
        if (code == CodeAccountAlreadyLinked || m.IndexOf("ENTITY_EXISTS", System.StringComparison.OrdinalIgnoreCase) >= 0
            || m.IndexOf("already exists", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return signUp ? "That username is taken." : "This account is already linked.";
        if (code == CodeBannedUser) return "This account is banned.";
        if (code == CodeTooManyRequests) return "Too many attempts. Wait a minute and try again.";
        if (code == CodeTransportError) return "No connection to the account service.";
        if (code == CodeInvalidParameters) return signUp ? "The account service rejected these details." : "Wrong username or password.";
        return signUp ? "Could not create the account." : "Could not sign in.";
    }
}
