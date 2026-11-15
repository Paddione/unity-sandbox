using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

/// <summary>P2 zone/loot/netcode config (headless gate, deterministic).</summary>
public class DiabloP2ConfigTests
{
    const string ZonePath = "Assets/Scenes/DiabloZone.unity";

    [Test]
    public void BuildSettings_KeepsGreyboxFirstAndAddsDiabloZone()
    {
        var scenes = EditorBuildSettings.scenes;
        Assert.IsNotEmpty(scenes, "EditorBuildSettings.scenes must not be empty.");
        Assert.That(scenes[0].path, Is.EqualTo("Assets/Scenes/Greybox.unity"),
            "Greybox must stay first (UrpPipelineTests pins scenes[0]).");
        Assert.That(scenes, Has.Some.Matches<EditorBuildSettingsScene>(s => s.path == "Assets/Scenes/Diablo.unity"),
            "Diablo.unity must stay in build settings (Q5 intact).");
        Assert.That(scenes, Has.Some.Matches<EditorBuildSettingsScene>(s => s.path == ZonePath),
            "DiabloZone.unity must be in build settings.");
    }

    [Test]
    public void DiabloZoneScene_HasSpawnsHusksEliteGoalAndNetcode()
    {
        var scene = EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        Assert.IsTrue(scene.IsValid(), "DiabloZone.unity must open cleanly.");

        Assert.AreEqual(4, CountSpawns(),
            "DiabloZone must contain exactly 4 CoopSpawn markers.");

        var husks = GameObject.FindGameObjectsWithTag("Husk");
        Assert.GreaterOrEqual(husks.Length, 6, "DiabloZone must contain 6+ husks.");
        foreach (var husk in husks)
            Assert.IsNotNull(husk.GetComponent<EnemyHealth>(), $"Husk '{husk.name}' needs EnemyHealth.");

        var elite = GameObject.Find("Husk_Elite");
        Assert.IsNotNull(elite, "DiabloZone must contain the Husk_Elite.");
        Assert.AreEqual(15, elite.GetComponent<EnemyHealth>().maxHP, "Elite must have 15 HP.");

        var player = GameObject.Find("Player");
        Assert.IsNotNull(player, "DiabloZone must contain the solo Player hero.");
        Assert.IsNotNull(player.GetComponent<Equipment>(), "Zone hero needs Equipment for loot.");

        var goal = GameObject.Find("GoalPad");
        Assert.IsNotNull(goal, "DiabloZone must contain the GoalPad extraction.");
        Assert.AreEqual(ToyTrigger.Mode.Goal, goal.GetComponent<ToyTrigger>().mode);

        var chest = GameObject.Find("DChestZone");
        Assert.IsNotNull(chest, "DiabloZone must contain the DChestZone.");
        Assert.IsTrue(chest.GetComponent<LootDrop>().isChest, "DChestZone must run in chest mode.");

        var huskObjs = GameObject.FindGameObjectsWithTag("Husk");
        foreach (var husk in huskObjs)
            Assert.IsNotNull(husk.GetComponent<NetworkObject>(),
                $"Husk '{husk.name}' needs a NetworkObject (server-authoritative kills).");

        var diabloPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/NetworkPlayerDiablo.prefab");
        Assert.IsNotNull(diabloPrefab, "NetworkPlayerDiablo prefab must exist in Resources.");
        var prefabTransform = diabloPrefab.GetComponent<NetworkTransform>();
        Assert.IsNotNull(prefabTransform, "Player prefab needs NetworkTransform.");
        Assert.AreEqual(NetworkTransform.AuthorityModes.Owner, prefabTransform.AuthorityMode,
            "Player prefab must use owner authority (server authority rubber-bands the owner).");

        NetworkManager nm = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            nm = root.GetComponentInChildren<NetworkManager>(true);
            if (nm != null) break;
        }
        Assert.IsNotNull(nm, "DiabloZone must contain a NetworkManager.");
        Assert.IsNotNull(nm.NetworkConfig.PlayerPrefab, "NetworkManager needs the Diablo player prefab.");
        var pp = nm.NetworkConfig.PlayerPrefab;
        Assert.IsNotNull(pp.GetComponent<DiabloNetworkPlayer>(), "Player prefab needs DiabloNetworkPlayer.");
        Assert.IsNotNull(pp.GetComponent<Equipment>(), "Player prefab needs Equipment.");
    }

    static int CountSpawns()
    {
        int n = 0;
        for (int i = 0; i < 4; i++)
            if (GameObject.Find($"CoopSpawn_{i}") != null) n++;
        return n;
    }

    [Test]
    public void DroptableWeights_SumTo100()
    {
        foreach (LootTier tier in System.Enum.GetValues(typeof(LootTier)))
        {
            DroptableDatabase.GetTable(tier, out int chance, out int magic, out int rare, out int unique);
            Assert.AreEqual(100, magic + rare + unique, $"Tier {tier}: weights must sum to 100.");
            Assert.Greater(chance, 0, $"Tier {tier}: drop chance must be positive.");
        }
    }

    [Test]
    public void ItemDatabase_NineCombosAndThreeFixedUniques()
    {
        foreach (ItemSlot slot in System.Enum.GetValues(typeof(ItemSlot)))
            Assert.AreEqual(3, ItemDatabase.GetBaseTypes(slot).Count, $"Slot {slot}: 3 base types.");
        var uniques = ItemDatabase.GetUniques();
        Assert.AreEqual(3, uniques.Count, "Exactly 3 uniques.");
        Assert.AreEqual("Emberbrand Blade", uniques[0].uniqueId);
        Assert.AreEqual("Cryptwarden Plate", uniques[1].uniqueId);
        Assert.AreEqual("Seer's Circlet", uniques[2].uniqueId);
        foreach (var u in uniques)
            Assert.AreEqual(ItemRarity.Unique, u.rarity, $"Unique {u.uniqueId} must be Unique.");
    }

    [Test]
    public void DiabloZoneScene_UsesOnlyUrpLitMaterials()
    {
        var scene = EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        Assert.IsTrue(scene.IsValid(), "DiabloZone.unity must open cleanly.");
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        Assert.IsNotEmpty(renderers, "DiabloZone should contain renderers.");
        foreach (var rend in renderers)
            foreach (var mat in rend.sharedMaterials)
            {
                Assert.IsNotNull(mat, $"Renderer '{rend.name}' has a null material.");
                Assert.That(mat.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"),
                    $"Renderer '{rend.name}' uses '{mat.shader.name}' instead of URP/Lit.");
            }
    }
}
