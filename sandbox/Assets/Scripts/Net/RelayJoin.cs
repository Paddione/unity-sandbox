using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;

/// <summary>
/// Relay join-code path (cinderreach-coop-session S6, UGS project
/// "Cinderreach"). Host: UGS init → anonymous sign-in → session as host with
/// Relay (max 4 players); the join code goes to the JoinPanel and the pair
/// log. Client: same init → join by code. The Sessions SDK owns transport
/// setup and the NGO start; this file only validates input, reports failures
/// and falls back to direct UTP with a warning (never silently).
/// UGS calls run at runtime only (Play Mode / player); the batchmode editor
/// throws ServicesInitializationException.
/// </summary>
public static class RelayJoin
{
    /// <summary>Max co-op party (goal contract: 2–4 players).</summary>
    public const int MaxPlayers = 4;

    /// <summary>Session name shown in the UGS dashboard.</summary>
    public const string SessionName = "Cinderreach";

    /// <summary>Minimum join-code length we accept (relay codes are 6 chars).</summary>
    public const int MinJoinCodeLength = 4;

    /// <summary>Log marker the pair harness greps for the host's code.</summary>
    public const string JoinCodeLogPrefix = "RELAY_JOIN_CODE=";

    // NOTE: awaits below deliberately stay on Unity's main thread (no
    // ConfigureAwait): the Sessions SDK drives NGO startup on the continuation
    // thread, and the transport allocator forbids background threads
    // (Allocator.Temp NRE inside StartHost).

    static bool ugsReady;
    static ISession activeSession;

    /// <summary>Last hosted join code this process created (panel display).</summary>
    public static string LastJoinCode { get; private set; }

    /// <summary>Trim + upper-case; null when missing or too short.</summary>
    public static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        code = code.Trim().ToUpperInvariant();
        return code.Length >= MinJoinCodeLength ? code : null;
    }

    /// <summary>
    /// UGS init + sign-in, once per process (cinderreach-accounts S1: identity,
    /// profiles and names live in <see cref="PlayerAccount"/>). False when the
    /// services are unreachable; callers fall back to direct UTP with a warning.
    /// </summary>
    public static async Task<bool> EnsureUgsAsync()
    {
        if (ugsReady) return true;
        ugsReady = await PlayerAccount.EnsureSignedInAsync();
        if (!ugsReady) Debug.LogWarning("[Relay] UGS unavailable, falling back to direct UTP.");
        return ugsReady;
    }

    /// <summary>
    /// Host a relay session. Returns the join code, or null when UGS or the
    /// allocation failed (caller falls back to direct hosting with a warning).
    /// </summary>
    public static async Task<string> HostWithRelayAsync()
    {
        if (!await EnsureUgsAsync()) return null;
        try
        {
            var session = await MultiplayerService.Instance.CreateSessionAsync(
                new SessionOptions { Name = SessionName, MaxPlayers = MaxPlayers }.WithRelayNetwork())
                ;
            activeSession = session;
            LastJoinCode = session.Code;
            Debug.Log($"[Relay] hosted session '{SessionName}' (max {MaxPlayers}).");
            return session.Code;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Relay] host session failed, falling back to direct UTP ({e.Message}).");
            return null;
        }
    }

    /// <summary>
    /// Join a relay session by code. False without touching UGS when the code
    /// is malformed, and false with a warning when UGS or the join failed.
    /// </summary>
    public static async Task<bool> JoinWithCodeAsync(string code)
    {
        string normalized = NormalizeCode(code);
        if (normalized == null)
        {
            Debug.LogWarning("[Relay] malformed join code, falling back to direct UTP.");
            return false;
        }
        if (!await EnsureUgsAsync()) return false;
        try
        {
            activeSession = await MultiplayerService.Instance
                .JoinSessionByCodeAsync(normalized);
            Debug.Log($"[Relay] joined session by code {normalized}.");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Relay] join {normalized} failed, falling back to direct UTP ({e.Message}).");
            return false;
        }
    }

    /// <summary>True once this process drives NGO over relay (session-owned).</summary>
    public static bool SessionActive()
    {
        var nm = NetworkManager.Singleton;
        return nm != null && nm.IsListening && (nm.IsHost || nm.IsConnectedClient);
    }
}
