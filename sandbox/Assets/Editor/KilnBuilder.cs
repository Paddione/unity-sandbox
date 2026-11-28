using UnityEditor;
using UnityEngine;

/// <summary>
/// Cinder Kiln construction (cinderreach-act-one S2): Act I's third zone, a
/// burnt-out kiln abbey at <see cref="KilnOffset"/> (+400 m X) in
/// DiabloZone.unity like the Ossuary. Ash floor, a ring of six kiln domes,
/// slag heaps, KayKit rubble/barrels where present, ember lights, and the
/// gate pair Ossuary → Kiln (locked until the Hollow Abbot falls, see
/// <see cref="ActProgress"/>) and Kiln → Ossuary. Enemies follow in S3/S4.
/// </summary>
public static class KilnBuilder
{
    public static readonly Vector3 KilnOffset = KilnLayout.KilnOffset;
    public const string RootName = KilnLayout.RootName;
    public const int KilnDomes = KilnLayout.KilnDomes;
    public const int EmberLights = KilnLayout.EmberLights;
    static Vector3 KilnGatePos => KilnLayout.KilnGatePos;
    static Vector3 KilnArrival => KilnLayout.KilnArrival;
    static Vector3 KilnReturnPos => KilnLayout.KilnReturnPos;
    static Vector3 OssuaryReturnArrival => KilnLayout.OssuaryReturnArrival;
    public static string LockedHint => KilnLayout.LockedHint;

    public static void BuildKiln()
    {
        var root = new GameObject(RootName);
        root.transform.position = KilnOffset;

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "KilnFloor";
        floor.transform.SetParent(root.transform, false);
        floor.transform.localScale = new Vector3(6f, 1f, 6f);
        Paint(floor, new Color(0.17f, 0.13f, 0.11f));

        // Kiln domes: squat cylinder + half-buried sphere, glowing mouth.
        for (int i = 0; i < KilnDomes; i++)
        {
            float a = i * Mathf.PI * 2f / KilnDomes + Mathf.PI / KilnDomes;
            var basePos = new Vector3(Mathf.Cos(a) * 11.5f, 0f, Mathf.Sin(a) * 11.5f);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = $"KilnDome_{i}";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = basePos + Vector3.up * 1f;
            body.transform.localScale = new Vector3(3f, 1f, 3f);
            Paint(body, new Color(0.30f, 0.22f, 0.18f));
            var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = $"KilnDomeCap_{i}";
            cap.transform.SetParent(body.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            cap.transform.localScale = new Vector3(1f, 1.6f, 1f);
            Paint(cap, new Color(0.26f, 0.19f, 0.16f));
            var mouth = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mouth.name = $"KilnMouth_{i}";
            Object.DestroyImmediate(mouth.GetComponent<Collider>());
            mouth.transform.SetParent(root.transform, false);
            mouth.transform.localPosition = basePos * 0.86f + Vector3.up * 0.7f;
            mouth.transform.localRotation = Quaternion.LookRotation(-basePos.normalized);
            mouth.transform.localScale = new Vector3(1.1f, 0.8f, 0.2f);
            PaintEmissive(mouth, new Color(1f, 0.45f, 0.12f));
        }

        // Slag heaps (dark, low) scattered inside the ring.
        var slagSpots = new[] { new Vector3(-6f, 0f, -4f), new Vector3(7f, 0f, 3f), new Vector3(-3f, 0f, 7f), new Vector3(4f, 0f, -7f) };
        for (int i = 0; i < slagSpots.Length; i++)
        {
            var slag = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            slag.name = $"KilnSlag_{i}";
            slag.transform.SetParent(root.transform, false);
            slag.transform.localPosition = slagSpots[i] + Vector3.up * 0.25f;
            slag.transform.localScale = new Vector3(2.2f, 0.6f, 1.8f);
            Paint(slag, new Color(0.09f, 0.08f, 0.08f));
        }

        // Ember lights (warm, low) — the Kiln's mood without a volume profile.
        for (int i = 0; i < EmberLights; i++)
        {
            float a = i * Mathf.PI * 2f / EmberLights;
            var go = new GameObject($"KilnEmber_{i}");
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3(Mathf.Cos(a) * 9f, 1.2f, Mathf.Sin(a) * 9f);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.5f, 0.18f);
            l.range = 9f;
            l.intensity = 2.2f;
        }

        TryKayKitDressing(root);

        // S3: Cinder Thralls (packs), Ash Lancers, two Kiln Elites at variant 0 anchors.
        // Anchors and groups come from the encounter layout (variant 0 = previous positions).
        for (int i = 0; i < KilnLayout.ThrallCount; i++)
            SpawnEnemy("Skeletons/Characters/Skeleton_Minion.fbx", KilnLayout.ThrallPrefix + i,
                EncounterLayout.KilnThrallAnchor(0, i), 6, LootTier.Normal, 0.9f, root.transform, thrall: true,
                group: EncounterLayout.IsKilnFront(i) ? EncounterLayout.FrontGroup : EncounterLayout.BackGroup);
        for (int i = 0; i < KilnLayout.LancerCount; i++)
            SpawnEnemy("Skeletons/Characters/Skeleton_Rogue.fbx", KilnLayout.LancerPrefix + i,
                EncounterLayout.KilnLancerAnchor(0, i), 10, LootTier.Normal, 1f, root.transform, thrall: false,
                group: EncounterLayout.IsKilnHunterLancer(i) ? EncounterLayout.BackGroup : EncounterLayout.FrontGroup);
        for (int i = 0; i < KilnLayout.EliteCount; i++)
            SpawnEnemy("Skeletons/Characters/Skeleton_Warrior.fbx", KilnLayout.ElitePrefix + i,
                EncounterLayout.KilnEliteAnchor(0, i), 24, LootTier.Elite, 1.25f, root.transform, thrall: true,
                group: EncounterLayout.BackGroup);
        EncounterBuilder.PlacePairs(KilnLayout.KilnOffset, EncounterBuilder.KilnPacks, (i, pos) =>
            SpawnEnemy("Skeletons/Characters/Skeleton_Minion.fbx", KilnLayout.ThrallPrefix + (KilnLayout.ThrallCount + i),
                pos, 6, LootTier.Normal, 0.9f, root.transform, thrall: true));
        SpawnKilnWardens(root.transform); // S1 hunter group: lancers + zone-bound wardens

        // S4: The Kilnmother at the north end of the ring.
        SpawnKilnmother(root.transform);

        var gate = SpawnGate(ZoneGate.KilnGateName, KilnGatePos, KilnArrival, ActProgress.Kiln, withHint: true);
        var ossuary = GameObject.Find(OssuaryBuilder.RootName);
        if (ossuary != null) gate.transform.SetParent(ossuary.transform, true);
        var ret = SpawnGate(ZoneGate.KilnReturnGateName, KilnReturnPos, OssuaryReturnArrival, ActProgress.Ossuary, withHint: false);
        ret.transform.SetParent(root.transform, true);
    }

    /// <summary>act-one S5: one waypoint shrine per zone (pedestal + glowing stone).</summary>
    public static void BuildWaypoints()
    {
        foreach (var (id, name, zone, pos) in KilnLayout.Shrines)
        {
            var shrine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shrine.name = "WaypointShrine_" + id;
            shrine.transform.position = pos + Vector3.up * 0.5f;
            shrine.transform.localScale = new Vector3(1f, 0.5f, 1f);
            Paint(shrine, new Color(0.32f, 0.3f, 0.34f));
            var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stone.name = "ShrineStone";
            Object.DestroyImmediate(stone.GetComponent<Collider>());
            stone.transform.SetParent(shrine.transform, false);
            stone.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            stone.transform.localScale = new Vector3(0.35f, 1.2f, 0.35f);
            stone.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            PaintEmissive(stone, new Color(0.35f, 0.8f, 1f));
            var ws = shrine.AddComponent<WaypointShrine>();
            ws.shrineId = id;
            ws.displayName = name;
            ws.zone = zone;
        }
    }

    static GameObject SpawnEnemy(string model, string name, Vector3 pos, int hp, LootTier tier, float scale,
        Transform parent, bool thrall, int group = EncounterLayout.FrontGroup)
    {
        // Skeleton_Brute is not in the pack: fall back to the Warrior like the Abbot does.
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/KayKit/" + model)
            ?? AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/KayKit/Skeletons/Characters/Skeleton_Warrior.fbx");
        GameObject e = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
            : GameObject.CreatePrimitive(PrimitiveType.Capsule);
        if (e.GetComponent<Collider>() == null)
        {
            var col = e.AddComponent<CapsuleCollider>();
            col.height = 1.7f;
            col.radius = 0.35f;
            col.center = new Vector3(0f, 0.85f, 0f);
        }
        e.name = name;
        e.tag = "Husk";
        e.transform.SetParent(parent, true);
        e.transform.position = pos;
        e.transform.localScale = Vector3.one * scale;
        e.AddComponent<Unity.Netcode.NetworkObject>();
        var health = e.AddComponent<EnemyHealth>();
        health.SetTier(hp, tier);
        health.corpseModel = prefab;
        if (thrall)
        {
            e.AddComponent<EnemyAttack>().damage = tier == LootTier.Normal ? 1 : 2;
            e.GetComponent<EnemyAttack>().encounterGroup = group;
            e.AddComponent<CinderThrall>();
        }
        else e.AddComponent<AshLancer>();
        SandboxBuilder.WireHuskAnimator(e);
        return e;
    }

    /// <summary>Hunter group wardens (zone-bound casters beside the elites).</summary>
    static void SpawnKilnWardens(Transform parent)
    {
        for (int k = 0; k < 2; k++)
        {
            var w = SpawnEnemy("Skeletons/Characters/Skeleton_Mage.fbx", EncounterBuilder.KilnWardenName(k),
                EncounterLayout.KilnWardenAnchor(0, k), 12, LootTier.Normal, 1f, parent, thrall: true,
                group: EncounterLayout.BackGroup);
            w.AddComponent<RangedCaster>();
        }
    }

    static void SpawnKilnmother(Transform parent)
    {
        var boss = SpawnEnemy("Skeletons/Characters/Skeleton_Brute.fbx", KilnLayout.KilnmotherName,
            KilnLayout.KilnmotherPos, KilnmotherController.MaxHP, LootTier.ActBoss, 2f, parent, thrall: false);
        Object.DestroyImmediate(boss.GetComponent<AshLancer>());
        boss.AddComponent<EnemyAttack>().damage = 3;
        boss.AddComponent<KilnmotherController>();
    }

    static GameObject SpawnGate(string name, Vector3 at, Vector3 dest, int zone, bool withHint)
    {
        var gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = name;
        gate.transform.position = at;
        gate.transform.localScale = new Vector3(2.4f, 2.4f, 0.6f);
        Paint(gate, new Color(0.75f, 0.35f, 0.12f));
        gate.GetComponent<BoxCollider>().isTrigger = true;
        var zg = gate.AddComponent<ZoneGate>();
        zg.destination = dest;
        zg.targetZone = zone;
        if (withHint)
        {
            var hint = new GameObject(ZoneGate.LockedHintName);
            hint.transform.SetParent(gate.transform, false);
            hint.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            hint.transform.localScale = new Vector3(1f / 2.4f, 1f / 2.4f, 1f / 0.6f) * 0.25f;
            // Marker only: ZoneGate adds the TextMesh at runtime, so the scene keeps
            // its URP-Lit-only material contract (TextMesh uses the GUI text shader).
        }
        return gate;
    }

    static void TryKayKitDressing(GameObject root)
    {
        void Place(string model, string name, Vector3 local, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/KayKit/Dungeon/Models/" + model);
            if (prefab == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
        Place("rubble_large.fbx", "KilnRubble_0", new Vector3(-12f, 0f, 4f), 20f);
        Place("rubble_half.fbx", "KilnRubble_1", new Vector3(12.5f, 0f, -3f), 140f);
        Place("barrel_small_stack.fbx", "KilnBarrels", new Vector3(-9f, 0f, 11f), 60f);
        Place("torch_lit.fbx", "KilnTorch_0", new Vector3(-2.5f, 0f, 12f), 180f);
        Place("torch_lit.fbx", "KilnTorch_1", new Vector3(2.5f, 0f, 12f), 180f);
    }

    static void Paint(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        r.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
    }

    static void PaintEmissive(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", color * 2f);
        r.sharedMaterial = m;
    }

    /// <summary>Headless proof: Kiln root, domes, ember lights, locked gate with hint, return gate.</summary>
    public static void VerifyKiln(UnityEngine.SceneManagement.Scene scene)
    {
        bool root = false, gate = false, hint = false, ret = false, mother = false;
        int shrines = 0;
        int domes = 0, embers = 0, thralls = 0, lancers = 0, elites = 0;
        foreach (var r in scene.GetRootGameObjects())
            foreach (var t in r.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                if (n == RootName) root = true;
                if (n.StartsWith("KilnDome_")) domes++;
                if (n.StartsWith("KilnEmber_") && t.GetComponent<Light>() != null) embers++;
                var zg = t.GetComponent<ZoneGate>();
                if (n == ZoneGate.KilnGateName && zg != null && zg.targetZone == ActProgress.Kiln)
                {
                    gate = true;
                    hint = t.Find(ZoneGate.LockedHintName) != null;
                }
                if (n == ZoneGate.KilnReturnGateName && zg != null) ret = true;
                if (n.StartsWith(KilnLayout.ThrallPrefix) && t.GetComponent<CinderThrall>() != null) thralls++;
                if (n.StartsWith(KilnLayout.LancerPrefix) && t.GetComponent<AshLancer>() != null) lancers++;
                if (n.StartsWith(KilnLayout.ElitePrefix) && t.GetComponent<CinderThrall>() != null) elites++;
                if (n == KilnLayout.KilnmotherName && t.GetComponent<KilnmotherController>() != null) mother = true;
                if (n.StartsWith("WaypointShrine_") && t.GetComponent<WaypointShrine>() != null) shrines++;
            }
        Debug.Log($"[KilnBuilder] Verify: root={root} domes={domes} embers={embers} gate={gate} hint={hint} ret={ret} " +
                  $"thralls={thralls} lancers={lancers} elites={elites} kilnmother={mother} shrines={shrines}");
        if (!root || domes < KilnDomes || embers < EmberLights || !gate || !hint || !ret
            || thralls != KilnLayout.ThrallCount + 2 * EncounterBuilder.KilnPacks.Length || lancers != KilnLayout.LancerCount
            || elites != KilnLayout.EliteCount || !mother || shrines != KilnLayout.Shrines.Length)
            throw new System.Exception("Kiln verify failed: zone 3 inventory incomplete.");
        Debug.Log("[KilnBuilder] Verify: OK (cinder kiln + gates present).");
    }
}
