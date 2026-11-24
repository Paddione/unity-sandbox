using UnityEngine;

/// <summary>
/// Co-op start parameters (coop-session S1), parsed from the command line.
/// <c>-host [-port N]</c> hosts (listening on all interfaces), <c>-join ip[:port]</c>
/// joins. No flag means solo, which is a host nobody joined. The P2 flags
/// <c>--net-client</c>, <c>--net-server</c>, <c>--net-address</c> and
/// <c>--net-port</c> keep working, and a ParrelSync clone joins.
/// Pure data so EditMode tests pin the rules.
/// </summary>
public readonly struct JoinArgs
{
    public enum NetMode { Host, Client, Server }

    public const string HostFlag = "-host";
    public const string JoinFlag = "-join";
    public const string PortFlag = "-port";
    /// <summary>Join a relay session by code (S6, UGS project "Cinderreach").</summary>
    public const string JoinCodeFlag = "-join-code";
    /// <summary>Host via relay instead of direct UTP (S6).</summary>
    public const string HostRelayFlag = "-host-relay";
    public const string DefaultAddress = "127.0.0.1";
    public const ushort DefaultPort = 7777;
    /// <summary>
    /// Resolve a hostname to an IP for UnityTransport (it rejects DNS names with
    /// "Invalid server address"). IPs pass through untouched, so headless tests
    /// never touch DNS; unresolvable names come back unchanged and fail loudly
    /// at transport start instead of hanging a lookup.
    /// </summary>
    public static string ResolveAddress(string host)
    {
        if (string.IsNullOrEmpty(host)) return host;
        if (System.Net.IPAddress.TryParse(host?.Trim(), out _)) return host.Trim();
        try
        {
            var entry = System.Net.Dns.GetHostEntry(host);
            foreach (var ip in entry.AddressList)
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return ip.ToString();
            if (entry.AddressList.Length > 0) return entry.AddressList[0].ToString();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning($"[Net] DNS resolve failed for '{host}': {e.Message}.");
        }
        return host;
    }
    /// <summary>Public direct-connect address of the cluster server (unity.korczewski.de),
    /// baked into the Windows installer so a fresh client finds it straight away.</summary>
    public const string PublicServerAddress = "unity.korczewski.de";
    /// <summary>Join argument baked into the installer shortcuts.</summary>
    public const string PublicServerJoinArg = "-join unity.korczewski.de";

    public readonly NetMode Mode;
    public readonly string Address;
    public readonly ushort Port;
    /// <summary>Why a value fell back to its default (null when none did).</summary>
    public readonly string Warning;
    /// <summary>Relay join code from <c>-join-code</c> (null when direct).</summary>
    public readonly string JoinCode;
    /// <summary>Host via relay instead of direct UTP (<c>-host-relay</c>).</summary>
    public readonly bool RelayHost;
    /// <summary>Client: find and join the published dedicated server (<c>-join-server</c>).</summary>
    public readonly bool JoinServer;
    /// <summary>Dedicated server: publish through Relay + Lobby (<c>-server-relay</c>).</summary>
    public readonly bool RelayServer;

    public JoinArgs(NetMode mode, string address, ushort port, string warning = null,
        string joinCode = null, bool relayHost = false, bool joinServer = false, bool relayServer = false)
    {
        Mode = mode;
        Address = string.IsNullOrWhiteSpace(address) ? DefaultAddress : address.Trim();
        Port = port == 0 ? DefaultPort : port;
        Warning = warning;
        JoinCode = string.IsNullOrWhiteSpace(joinCode) ? null : joinCode.Trim();
        RelayHost = relayHost;
        JoinServer = joinServer;
        RelayServer = relayServer;
    }

    public bool IsClient => Mode == NetMode.Client;

    public override string ToString() => $"{Mode} {Address}:{Port}";

    public static JoinArgs Parse(string[] args, bool isClone = false)
    {
        args = args ?? new string[0];
        var mode = NetMode.Host;
        string address = DefaultAddress;
        ushort port = DefaultPort;
        string warning = null;
        bool portGiven = false;

        string legacyAddress = Value(args, "--net-address");
        if (legacyAddress != null) address = legacyAddress;
        string portText = Value(args, PortFlag) ?? Value(args, "--net-port");
        if (portText != null)
        {
            if (TryPort(portText, out var p)) { port = p; portGiven = true; }
            else warning = $"invalid port '{portText}', using {DefaultPort}";
        }

        if (DedicatedServer.Requested(args)) mode = NetMode.Server;
        if (isClone || Has(args, "--net-client")) mode = NetMode.Client;
        if (Has(args, HostFlag)) mode = NetMode.Host;

        string join = Value(args, JoinFlag);
        if (join != null)
        {
            mode = NetMode.Client;
            if (TrySplit(join, out var host, out var joinPort, out var splitWarning))
            {
                address = host;
                if (joinPort != 0) port = joinPort;
            }
            warning = splitWarning ?? warning;
        }
        else if (Has(args, JoinFlag))
        {
            mode = NetMode.Client;
            warning = "-join without address, using " + DefaultAddress;
        }
        if (mode != NetMode.Client && !portGiven) port = DefaultPort;
        string joinCode = Value(args, JoinCodeFlag);
        if (joinCode != null) mode = NetMode.Client;
        else if (Has(args, JoinCodeFlag))
        {
            mode = NetMode.Client;
            warning = "-join-code without a code, using " + DefaultAddress;
        }
        bool relayHost = Has(args, HostRelayFlag);
        if (relayHost) mode = NetMode.Host;
        bool relayServer = Has(args, ServerDirectory.RelayFlag);
        if (relayServer) mode = NetMode.Server;
        bool joinServer = Has(args, ServerDirectory.JoinServerFlag);
        if (joinServer) mode = NetMode.Client;
        return new JoinArgs(mode, address, port, warning, joinCode, relayHost, joinServer, relayServer);
    }

    /// <summary>Split "host[:port]" (the JoinPanel field uses the same rule).</summary>
    public static bool TrySplit(string text, out string host, out ushort port, out string warning)
    {
        host = DefaultAddress;
        port = 0;
        warning = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            warning = "empty address, using " + DefaultAddress;
            return false;
        }
        text = text.Trim();
        int colon = text.LastIndexOf(':');
        if (colon > 0 && text.IndexOf(':') == colon)
        {
            var portText = text.Substring(colon + 1);
            host = text.Substring(0, colon);
            if (!TryPort(portText, out port))
            {
                port = 0;
                warning = $"invalid port '{portText}', using {DefaultPort}";
            }
        }
        else
        {
            host = text;
        }
        return true;
    }

    static bool TryPort(string text, out ushort port) =>
        ushort.TryParse(text, out port) && port > 0;

    static bool Has(string[] args, string flag)
    {
        foreach (var a in args)
            if (a == flag) return true;
        return false;
    }

    static string Value(string[] args, string flag)
    {
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == flag && !args[i + 1].StartsWith("-")) return args[i + 1];
        return null;
    }
}
