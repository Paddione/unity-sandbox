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
    static readonly HashSet<string> missing = new();
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

    /// <summary>
    /// Optional asset lookup that checks ResourceLocators before loading so
    /// absent keys return false without logging warnings or InvalidKeyExceptions.
    /// </summary>
    public static bool TryGet<T>(string address, out T asset) where T : Object
    {
        asset = null;
        if (string.IsNullOrEmpty(address)) return false;
        if (cache.TryGetValue(address, out var hit))
        {
            asset = hit as T;
            return asset != null;
        }
        if (missing.Contains(address)) return false;
        EnsureInit();
        bool exists = false;
        foreach (var locator in Addressables.ResourceLocators)
        {
            if (locator != null && locator.Locate(address, typeof(T), out var locs) && locs != null && locs.Count > 0)
            {
                exists = true;
                break;
            }
        }
        if (!exists)
        {
            missing.Add(address);
            return false;
        }
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
        asset = handle.WaitForCompletion();
        if (handle.Status == AsyncOperationStatus.Succeeded && asset != null)
        {
            cache[address] = asset;
            return true;
        }
        missing.Add(address);
        asset = null;
        return false;
    }

    public static int CachedCount => cache.Count;

    /// <summary>Test hook: drop the lookup cache (Addressables keeps refcounts).</summary>
    public static void ClearCache()
    {
        cache.Clear();
        missing.Clear();
    }
}
