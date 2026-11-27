using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>Q4 co-op layer: NGO packages, NetworkManager wiring, prefab, clone detection.</summary>
public class MultiplayerConfigTests
{
    [Test]
    public void GreyboxScene_HasNetworkManagerWithTransportAndAutoStarter()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Greybox.unity", OpenSceneMode.Single);
        Assert.IsTrue(scene.IsValid(), "Greybox.unity must open cleanly.");

        NetworkManager nm = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            nm = root.GetComponentInChildren<NetworkManager>(true);
            if (nm != null) break;
        }
        Assert.IsNotNull(nm, "Greybox scene must contain a NetworkManager (Q4 co-op).");
        Assert.IsNotNull(nm.GetComponent<UnityTransport>(), "NetworkManager needs UnityTransport.");
        Assert.IsNotNull(nm.GetComponent<MultiplayerAutoStarter>(), "NetworkManager needs MultiplayerAutoStarter.");
        Assert.IsNotNull(nm.NetworkConfig.PlayerPrefab, "NetworkManager needs a PlayerPrefab.");
    }

    [Test]
    public void NetworkPlayerPrefab_HasNetworkObjectAndOwnerOnlyController()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AddressableAssets/NetworkPlayer.prefab");
        Assert.IsNotNull(prefab, "Assets/AddressableAssets/NetworkPlayer.prefab must exist.");
        Assert.IsNotNull(prefab.GetComponent<NetworkObject>(), "NetworkPlayer needs NetworkObject.");
        Assert.IsNotNull(prefab.GetComponent<ThirdPersonController>(), "NetworkPlayer needs ThirdPersonController.");
    }

    [Test]
    public void CloneDetection_MainVsClonePaths()
    {
        Assert.IsTrue(MultiplayerAutoStarter.IsClonePath("/proj/sandbox_clone_N/Assets"));
        Assert.IsFalse(MultiplayerAutoStarter.IsClonePath("/home/patrick/unity-sandbox/sandbox/Assets"));
    }
}
