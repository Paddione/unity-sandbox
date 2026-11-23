/// <summary>What a sign-in does with the local hero and the account's cloud hero.</summary>
public enum CloudHeroAction
{
    /// <summary>Both sides agree (or neither has a hero).</summary>
    None,
    /// <summary>The local hero is newer: push it to the account.</summary>
    Upload,
    /// <summary>The cloud hero is newer or the PC has none: pull it.</summary>
    Download,
    /// <summary>Both changed, or a guest hero meets an account hero: the player picks.</summary>
    Ask,
    /// <summary>The local hero belongs to another account and this one has none: set it aside.</summary>
    StartFresh,
}

/// <summary>Local save side-car (<c>&lt;save&gt;.meta.json</c>): who owns the file and what the cloud has seen.</summary>
[System.Serializable]
public struct LocalHeroMeta
{
    public bool exists;
    /// <summary>Account player id that wrote the hero; "" for guest or pre-account heroes.</summary>
    public string owner;
    /// <summary>UTC ticks of the last local write.</summary>
    public long savedTicks;
    /// <summary>UTC ticks of the local write the cloud last matched (0 = never synced).</summary>
    public long syncedTicks;
}

/// <summary>Cloud side of the comparison.</summary>
public struct CloudHeroMeta
{
    public bool exists;
    public long savedTicks;
}

/// <summary>
/// Sign-in decision for the cloud hero (cinderreach-accounts S3), pure so
/// EditMode pins every case. The account is the source of truth; nothing
/// is ever lost silently: a hero that gets replaced is backed up first, and
/// only a real conflict asks the player.
/// </summary>
public static class CloudHeroResolver
{
    public static CloudHeroAction Decide(string me, LocalHeroMeta local, CloudHeroMeta cloud)
    {
        if (!local.exists) return cloud.exists ? CloudHeroAction.Download : CloudHeroAction.None;
        string owner = local.owner ?? "";
        bool mine = owner.Length > 0 && owner == me;
        bool otherAccount = owner.Length > 0 && owner != me;
        if (!cloud.exists)
            return otherAccount ? CloudHeroAction.StartFresh : CloudHeroAction.Upload;
        if (otherAccount) return CloudHeroAction.Download;
        if (!mine) return CloudHeroAction.Ask; // guest/pre-account hero meets the account's hero
        bool localChanged = local.savedTicks > local.syncedTicks;
        bool cloudChanged = cloud.savedTicks > local.syncedTicks;
        if (localChanged && cloudChanged) return CloudHeroAction.Ask;
        if (localChanged) return CloudHeroAction.Upload;
        if (cloudChanged) return CloudHeroAction.Download;
        return CloudHeroAction.None;
    }
}
