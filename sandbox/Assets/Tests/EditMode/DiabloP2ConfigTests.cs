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

        var elite = GameObject.Find("Ash Elite");
        Assert.IsNotNull(elite, "DiabloZone must contain the Ash Elite.");
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
        int ignoreLayer = UnityEngine.LayerMask.NameToLayer("Ignore Raycast");
        Assert.AreEqual(ignoreLayer, pp.gameObject.layer, "Network player prefab must ignore aim raycasts.");
        foreach (var tr in pp.GetComponentsInChildren<Transform>(true))
            Assert.AreEqual(ignoreLayer, tr.gameObject.layer,
                $"Prefab child '{tr.gameObject.name}' must ignore aim raycasts.");
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
    public void ZoneHero_HasRunAnimation()
    {
        var scene = EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        Assert.IsTrue(scene.IsValid(), "DiabloZone.unity must open cleanly.");
        var player = GameObject.Find("Player");
        Assert.IsNotNull(player, "DiabloZone must contain the solo hero.");
        Assert.IsNull(player.GetComponent<Animation>(),
            "Hero must not carry a legacy Animation next to Mecanim.");
        var animator = player.GetComponent<Animator>();
        Assert.IsNotNull(animator, "Hero needs a Mecanim Animator (KayKit rig).");
        var ctrl = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
        Assert.IsNotNull(ctrl, "Hero Animator needs the locomotion controller.");
        var states = new System.Collections.Generic.List<string>();
        foreach (var s in ctrl.layers[0].stateMachine.states) states.Add(s.state.name);
        Assert.Contains("Idle", states, "Locomotion controller needs an Idle state.");
        Assert.Contains("Run", states, "Locomotion controller needs a Run state.");
        bool hasMoving = false;
        foreach (var p in ctrl.parameters)
            if (p.name == "Moving" && p.type == AnimatorControllerParameterType.Bool) hasMoving = true;
        Assert.IsTrue(hasMoving, "Locomotion controller needs a bool param 'Moving'.");
        var driver = player.GetComponent<DiabloAnimator>();
        Assert.IsNotNull(driver, "Hero needs a DiabloAnimator driver.");
        Assert.AreEqual("Moving", driver.moveParam, "Driver must write the 'Moving' param.");
        Assert.AreEqual("Strike", driver.strikeParam, "Driver must write the 'Strike' trigger.");
        bool hasStrike = false;
        foreach (var p in ctrl.parameters)
            if (p.name == "Strike" && p.type == AnimatorControllerParameterType.Trigger) hasStrike = true;
        Assert.IsTrue(hasStrike, "Locomotion controller needs a 'Strike' trigger for whiffs.");
        var states2 = new System.Collections.Generic.List<string>();
        foreach (var s in ctrl.layers[0].stateMachine.states) states2.Add(s.state.name);
        Assert.Contains("Strike", states2, "Locomotion controller needs a Strike state.");
        int ignoreRaycast = UnityEngine.LayerMask.NameToLayer("Ignore Raycast");
        Assert.AreEqual(ignoreRaycast, player.layer, "Hero must sit on the Ignore Raycast layer (self must not block aim).");
        foreach (var tr in player.GetComponentsInChildren<Transform>(true))
            Assert.AreEqual(ignoreRaycast, tr.gameObject.layer,
                $"Hero child '{tr.gameObject.name}' must ignore aim raycasts (layer is per-object).");
    }

    [Test]
    public void ZoneEntry_HintExistsAtStairs()
    {
        var scene = EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        Assert.IsTrue(scene.IsValid(), "DiabloZone.unity must open cleanly.");
        var entry = GameObject.Find("ZoneEntry");
        Assert.IsNotNull(entry, "DiabloZone must contain the ZoneEntry trigger at the stairs.");
        var col = entry.GetComponent<Collider>();
        Assert.IsNotNull(col, "ZoneEntry needs a Collider.");
        Assert.IsTrue(col.isTrigger, "ZoneEntry collider must be a trigger.");
        Assert.IsNotNull(entry.GetComponent<ZoneEntryHint>(), "ZoneEntry needs the hint component.");
        Assert.That(entry.transform.position.z, Is.InRange(9f, 13f), "ZoneEntry must sit at the south stairs.");
    }

    [Test]
    public void NetworkPrefab_SpawnsAtStairs()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/NetworkPlayerDiablo.prefab");
        Assert.IsNotNull(prefab, "NetworkPlayerDiablo prefab must exist in Resources.");
        Assert.That(prefab.transform.position.z, Is.InRange(9f, 13f),
            "Network players must spawn at the south stairs, facing the zone.");
    }

    [Test]
    public void SoloHero_YieldsToNetcode()
    {
        Assert.IsTrue(DiabloClickController.ShouldYieldToNetcode(false, true),
            "Scene hero must yield when the scene runs netcode.");
        Assert.IsFalse(DiabloClickController.ShouldYieldToNetcode(true, true),
            "Network player must never yield to itself.");
        Assert.IsFalse(DiabloClickController.ShouldYieldToNetcode(false, false),
            "Solo hero stays active without netcode (Diablo scene).");
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
