using Unity.Collections;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using UnityEngine;

/// <summary>
/// Unity Transport driver with a smaller packet ceiling (cinderreach-dedicated-server S4).
/// UTP sends UDP payloads up to 1400 bytes, 1428 on the wire. The devmesh pods sit behind
/// a Flannel WireGuard overlay with MTU 1420, so full-size packets never reached the server:
/// handshakes passed, then the first fragmented reliable message stalled and the client timed
/// out after 30 to 60 s. Local servers (MTU 1500) never showed it. <see cref="MaxPacketBytes"/>
/// stays under every common tunnel MTU (WireGuard, Tailscale 1280, PPPoE). Installed for every
/// mode (direct, relay host, dedicated server, client), so both directions stay small.
/// </summary>
public sealed class CompactPacketDriver : INetworkStreamDriverConstructor
{
    /// <summary>Largest UDP payload the transport sends (Unity Transport default: 1400).</summary>
    public const int MaxPacketBytes = 1200;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        if (UnityTransport.s_DriverConstructor == null) UnityTransport.s_DriverConstructor = new CompactPacketDriver();
    }

    /// <summary>Clamp the packet size in <paramref name="settings"/> (test seam).</summary>
    public static void ApplyPacketLimit(ref NetworkSettings settings)
    {
        var p = settings.GetNetworkConfigParameters();
        p.maxMessageSize = Mathf.Min(p.maxMessageSize <= 0 ? MaxPacketBytes : p.maxMessageSize, MaxPacketBytes);
        settings.AddRawParameterStruct(ref p);
    }

    public void CreateDriver(UnityTransport transport, out NetworkDriver driver,
        out NetworkPipeline unreliableFragmentedPipeline,
        out NetworkPipeline unreliableSequencedFragmentedPipeline,
        out NetworkPipeline reliableSequencedPipeline)
    {
        var settings = transport.GetDefaultNetworkSettings();
        ApplyPacketLimit(ref settings);
        driver = NetworkDriver.Create(new UDPNetworkInterface(), settings);
        transport.GetDefaultPipelineConfigurations(out var unreliableFragmented,
            out var unreliableSequencedFragmented, out var reliableSequenced);
        unreliableFragmentedPipeline = driver.CreatePipeline(unreliableFragmented);
        unreliableSequencedFragmentedPipeline = driver.CreatePipeline(unreliableSequencedFragmented);
        reliableSequencedPipeline = driver.CreatePipeline(reliableSequenced);
        Debug.Log($"[Net] transport packet ceiling {MaxPacketBytes} bytes.");
    }
}
