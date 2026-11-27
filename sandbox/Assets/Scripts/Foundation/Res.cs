using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>Synchronous lookup over Addressables game content (Task 6).
/// Addresses equal the old Resources paths (e.g. "Voice/line", "MenuArt/icon",
/// "NetworkPlayerDiablo"), so call-site rewrites are one-liners
/// (Resources.Load → Res.Get). The first touch of an address loads
/// synchronously via WaitForCompletion — same stall profile as the old
/// Resources.Load — later touches are dictionary hits. No preload gates,
/// no flow changes, tests work unchanged (sync).
/// </summary>
public static class Res
{
    static readonly Dictionary<string, Object> cache = new();
    static bool initialized;

    static void EnsureInit()
    {
        if (initialized) return;
        initialized = true;
        Addressables.InitializeAsync().WaitForCompletion();
    }

    public static T Get<T>(string address) where T : Object
    {
        if (string.IsNullOrEmpty(address)) return null;
        if (cache.TryGetValue(address, out var hit)) return hit as T;
        EnsureInit();
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
        T asset = handle.WaitForCompletion();
        if (handle.Status == AsyncOperationStatus.Succeeded && asset != null)
            cache[address] = asset;
        else
            Debug.LogWarning($"[Res] address not found: {address}");
        return asset;
    }

    public static int CachedCount => cache.Count;

    /// <summary>Test hook: drop the lookup cache (Addressables keeps refcounts).</summary>
    public static void ClearCache() => cache.Clear();
}
