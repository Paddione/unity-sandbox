using UnityEditor;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Sunken Ossuary construction (S5): abgesetzter Bereich bei
/// <see cref="OssuaryOffset"/> (+200 m X), Bodenplatte + Pfeilerring,
/// Sarkophag- und Fackel-Dressing (KayKit Dungeon da wo vorhanden,
/// Primitive-Fallbacks sonst), 8 Bone Wardens + 2 Elites, The Hollow
/// Abbot (Boss) und das Tor-Paar (FenGate ↔ OssuaryGate).
/// </summary>
public static class OssuaryBuilder
{
    public static readonly Vector3 OssuaryOffset = new Vector3(200f, 0f, 0f);
    public const string RootName = "OssuaryRoot";
    public const int CasterCount = 8;
    public const int CasterEliteCount = 2;

    /// <summary>Build the ossuary annex (called from BuildDiabloZone).</summary>
    public static void BuildOssuary()
    {
        var root = new GameObject(RootName);
        root.transform.position = OssuaryOffset;

        // Floor plate + pillar ring (crypt hall, 30×30 m).
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "OssuaryFloor";
        floor.transform.SetParent(root.transform, false);
        floor.transform.localPosition = Vector3.zero;
        floor.transform.localScale = new Vector3(3f, 1f, 3f);
        Paint(floor, new Color(0.13f, 0.12f, 0.16f));
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2f / 8f;
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = $"OssuaryPillar_{i}";
            pillar.transform.SetParent(root.transform, false);
            pillar.transform.localPosition = new Vector3(Mathf.Cos(a) * 12f, 2f, Mathf.Sin(a) * 12f);
            pillar.transform.localScale = new Vector3(1.2f, 4f, 1.2f);
            Paint(pillar, new Color(0.30f, 0.28f, 0.34f));
        }

        // Sarcophagi + braziers (Blender-Props-Seam: primitives keep the
        // headless build green; WorldArt dresses over them when present).
        for (int i = 0; i < 4; i++)
        {
            var sarc = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sarc.name = $"OssuarySarcophagus_{i}";
            sarc.transform.SetParent(root.transform, false);
            sarc.transform.localPosition = new Vector3(i % 2 == 0 ? -8f : 8f, 0.5f, -6f + i * 3f);
            sarc.transform.localScale = new Vector3(2.2f, 1f, 1.2f);
            Paint(sarc, new Color(0.36f, 0.34f, 0.38f));
        }
        TryKayKitDressing(root);

        // Torches (primitive braziers, 4 Stück).
        for (int i = 0; i < 4; i++)
        {
            var braz = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            braz.name = $"OssuaryTorch_{i}";
            braz.transform.SetParent(root.transform, false);
            float a = i * Mathf.PI / 2f + Mathf.PI / 4f;
            braz.transform.localPosition = new Vector3(Mathf.Cos(a) * 10f, 0.8f, Mathf.Sin(a) * 10f);
            braz.transform.localScale = new Vector3(0.4f, 1.6f, 0.4f);
            Paint(braz, new Color(0.9f, 0.5f, 0.12f));
        }

        // Gates: FenGate in town (GoalPad-Nähe) → Ossuary; return gate back.
        // Arrival lands 8 m clear of the return gate: identical coordinates
        // bounced arrivals straight back to town (unreachable ossuary).
        SpawnGate(ZoneGate.FenGateName, new Vector3(0f, 1f, -6f),
            OssuaryOffset + new Vector3(0f, 1f, 2f), 2);
        var ret = SpawnGate(ZoneGate.OssuaryGateName, OssuaryOffset + new Vector3(0f, 1f, 10f),
            new Vector3(0f, 1f, -4f), 1);
        ret.transform.SetParent(root.transform, true);

        // 8 Bone Wardens (normal) + 2 elites, host-authoritative casters.
        // Anchors come from the encounter layout (variant 0 = this ring).
        for (int i = 0; i < CasterCount; i++)
            SpawnCaster("Bone Warden", EncounterLayout.OssuaryRingAnchor(i),
                12, LootTier.Normal, 1f, root.transform);
        EncounterBuilder.EnsureCover(root.transform);
        SpawnCaster("Bone Warden Elite", OssuaryOffset + new Vector3(-5f, 0.05f, -8f),
            24, LootTier.Elite, 1.2f, root.transform);
        SpawnCaster("Bone Warden Elite", OssuaryOffset + new Vector3(5f, 0.05f, -8f),
            24, LootTier.Elite, 1.2f, root.transform);

        // The Hollow Abbot (boss, north apse).
        SpawnBoss(OssuaryOffset + new Vector3(0f, 0.05f, -10f), root.transform);
    }

    static GameObject SpawnGate(string name, Vector3 at, Vector3 dest, int zone)
    {
        var gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = name;
        gate.transform.position = at;
        gate.transform.localScale = new Vector3(2.4f, 2.4f, 0.6f);
        Paint(gate, new Color(0.2f, 0.5f, 0.7f));
        gate.GetComponent<BoxCollider>().isTrigger = true;
        var zg = gate.AddComponent<ZoneGate>();
        zg.destination = dest;
        zg.targetZone = zone;
        return gate;
    }

    static void SpawnCaster(string name, Vector3 pos, int hp, LootTier tier, float scale, Transform parent)
    {
        GameObject husk;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ThirdParty/KayKit/Skeletons/Characters/Skeleton_Mage.fbx")
            ?? AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ThirdParty/KayKit/Skeletons/Characters/Skeleton_Minion.fbx");
        if (prefab != null)
        {
            husk = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var col = husk.AddComponent<CapsuleCollider>();
            col.height = 1.7f;
            col.radius = 0.35f;
            col.center = new Vector3(0f, 0.85f, 0f);
        }
        else
        {
            husk = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        }
        husk.name = name;
        husk.tag = "Husk";
        husk.transform.SetParent(parent, true);
        husk.transform.position = pos;
        husk.transform.localScale = Vector3.one * scale;
        husk.AddComponent<NetworkObject>();
        var casterHealth = husk.AddComponent<EnemyHealth>();
        casterHealth.SetTier(hp, tier);
        casterHealth.corpseModel = prefab; // host-safe corpse path (feel contract, like fen husks)
        husk.AddComponent<RangedCaster>();
        husk.AddComponent<EnemyAttack>().damage = tier == LootTier.Normal ? 1 : 2;
        SandboxBuilder.WireHuskAnimator(husk);
    }

    static void SpawnBoss(Vector3 pos, Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ThirdParty/KayKit/Skeletons/Characters/Skeleton_Brute.fbx")
            ?? AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ThirdParty/KayKit/Skeletons/Characters/Skeleton_Warrior.fbx");
        GameObject boss = prefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
            : GameObject.CreatePrimitive(PrimitiveType.Capsule);
        if (boss.GetComponent<Collider>() == null)
        {
            var col = boss.AddComponent<CapsuleCollider>();
            col.height = 2.2f;
            col.radius = 0.5f;
            col.center = new Vector3(0f, 1.1f, 0f);
        }
        boss.name = BossController.BossName;
        boss.tag = "Husk";
        boss.transform.SetParent(parent, true);
        boss.transform.position = pos;
        boss.transform.localScale = Vector3.one * 1.6f;
        boss.AddComponent<NetworkObject>();
        var health = boss.AddComponent<EnemyHealth>();
        health.SetTier(BossController.BossMaxHP, LootTier.Boss);
        health.corpseModel = prefab; // host-safe corpse path (feel contract, like fen husks)
        boss.AddComponent<BossController>();
        boss.AddComponent<EnemyAttack>().damage = 3;
        SandboxBuilder.WireHuskAnimator(boss);
    }

    /// <summary>KayKit dungeon dressing where the pack has the models (warn-only).</summary>
    static void TryKayKitDressing(GameObject root)
    {
        var torch = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/ThirdParty/KayKit/Dungeon/Models/torch_lit.fbx");
        if (torch == null)
        {
            Debug.LogWarning("[OssuaryBuilder] torch_lit.fbx missing; primitive braziers stand in.");
            return;
        }
        for (int i = 0; i < 2; i++)
        {
            var t = (GameObject)PrefabUtility.InstantiatePrefab(torch);
            t.name = $"OssuaryDressTorch_{i}";
            t.transform.SetParent(root.transform, false);
            t.transform.localPosition = new Vector3(i == 0 ? -11f : 11f, 0f, -10f);
        }
    }

    static void Paint(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        renderer.sharedMaterial = mat;
    }

    /// <summary>Headless proof: ossuary root, gate pair, ≥8 casters, boss.</summary>
    public static void VerifyOssuary(UnityEngine.SceneManagement.Scene scene)
    {
        bool root = false, fen = false, ret = false, boss = false;
        int casters = 0, elites = 0;
        foreach (var r in scene.GetRootGameObjects())
            CountOssuary(r.transform, ref root, ref fen, ref ret, ref boss, ref casters, ref elites);
        Debug.Log($"[OssuaryBuilder] Verify: root={root} fen={fen} ret={ret} " +
                  $"casters={casters} elites={elites} boss={boss}");
        if (!root || !fen || !ret || casters < CasterCount || elites < CasterEliteCount || !boss)
            throw new System.Exception("Ossuary verify failed: annex inventory incomplete.");
        Debug.Log("[OssuaryBuilder] Verify: OK (ossuary + gates + casters + boss present).");
    }

    static void CountOssuary(Transform t, ref bool root, ref bool fen, ref bool ret,
        ref bool boss, ref int casters, ref int elites)
    {
        string n = t.gameObject.name;
        if (n == RootName) root = true;
        if (n == ZoneGate.FenGateName && t.gameObject.GetComponent<ZoneGate>() != null) fen = true;
        if (n == ZoneGate.OssuaryGateName && t.gameObject.GetComponent<ZoneGate>() != null) ret = true;
        if (n == BossController.BossName && t.gameObject.GetComponent<BossController>() != null) boss = true;
        if (n == "Bone Warden" && t.gameObject.GetComponent<RangedCaster>() != null) casters++;
        if (n == "Bone Warden Elite" && t.gameObject.GetComponent<RangedCaster>() != null) elites++;
        foreach (Transform c in t) CountOssuary(c, ref root, ref fen, ref ret, ref boss, ref casters, ref elites);
    }
}
