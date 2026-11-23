using System.Text;

/// <summary>
/// Display-name rules (cinderreach-accounts S1), pure so EditMode pins them.
/// UGS Player Names stores "Name#1234"; the suffix keeps names unique there
/// but never shows in game. Every name a peer reports is cleaned again on the
/// server before it reaches the other players. UGS rejects whitespace in
/// names, so spaces become '_'.
/// </summary>
public static class PlayerNames
{
    public const int MinLength = 3;
    public const int MaxLength = 16;

    /// <summary>Shown while a hero has not reported a name yet.</summary>
    public const string Unnamed = "Wanderer";

    /// <summary>
    /// Cleans a raw name: drops the UGS "#1234" suffix, keeps letters, digits,
    /// '-' and '_', turns runs of whitespace into one '_', trims, cuts to
    /// <see cref="MaxLength"/>. Returns null when fewer than
    /// <see cref="MinLength"/> characters survive.
    /// </summary>
    public static string Sanitize(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        int hash = raw.IndexOf('#');
        if (hash >= 0) raw = raw.Substring(0, hash);
        var sb = new StringBuilder(raw.Length);
        bool lastSpace = true; // drops leading whitespace
        foreach (char c in raw)
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
            {
                sb.Append(c);
                lastSpace = false;
            }
            else if (char.IsWhiteSpace(c) && !lastSpace)
            {
                sb.Append('_');
                lastSpace = true;
            }
            if (sb.Length >= MaxLength) break;
        }
        string s = sb.ToString().TrimEnd('_');
        return s.Length >= MinLength ? s : null;
    }

    /// <summary>Name to show for a hero: the cleaned name, else <see cref="Unnamed"/>.</summary>
    public static string Display(string raw) => Sanitize(raw) ?? Unnamed;
}
