using UnityEditor;
using UnityEngine;

/// <summary>
/// Glass Barrens construction (cinderreach-act-two-opening S1): Act II's first
/// zone at <see cref="BarrensLayout.BarrensOffset"/> (+600 m X) in
/// DiabloZone.unity. A pale vitrified floor, low ash dunes, a broken ring of
/// teal glass spires with cold glow lights, KayKit ruins where present, and
/// the gate pair Kiln → Barrens (sealed until the Kilnmother falls, see
/// <see cref="ActProgress"/>) and Barrens → Kiln. S2: Shardbacks, Dune Augurs
/// and Glass Elites at variant-0 anchors (<see cref="LayoutSeeder"/> moves
/// them per seed). The zone boss follows in S3.
/// </summary>
public static class BarrensBuilder
{
    public const string SpirePrefix = "BarrensSpire_";
    public const string LightPrefix = "BarrensGlow_";
    public const string DunePrefix = "BarrensDune_";

    static readonly Color GlassTeal = new Color(0.35f, 0.85f, 0.8f);

    public static void BuildBarrens()
    {
        var off = BarrensLayout.BarrensOffset;
        var root = new GameObject(BarrensLayout.RootName);
        root.transform.position = off;

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "BarrensFloor";
        floor.transform.SetParent(root.transform, false);
        floor.transform.localScale = new Vector3(4f, 1f, 4f);
        Paint(floor, new Color(0.52f, 0.5f, 0.46f));

        // Glassed scar through the middle: a slightly raised, glossy strip.
        var scar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        scar.name = "BarrensGlassScar";
        Object.DestroyImmediate(scar.GetComponent<Collider>());
        scar.transform.SetParent(root.transform, false);
        scar.transform.localPosition = new Vector3(0f, 0.01f, -2f);
        scar.transform.localRotation = Quaternion.Euler(0f, 20f, 0f);
        scar.transform.localScale = new Vector3(5f, 0.02f, 26f);
        PaintGlass(scar, new Color(0.22f, 0.4f, 0.38f), 0.95f);

        // Ash dunes: flat, wide, walkable-around lumps.
        for (int i = 0; i < BarrensLayout.Dunes; i++)
        {
            var spot = BarrensLayout.DuneSpots[i];
            var dune = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dune.name = DunePrefix + i;
            dune.transform.SetParent(root.transform, false);
            dune.transform.localPosition = new Vector3(spot.x, 0.1f, spot.y);
            dune.transform.localScale = new Vector3(4.5f, 0.8f, 3.2f);
            dune.transform.localRotation = Quaternion.Euler(0f, i * 37f, 0f);
            Paint(dune, new Color(0.42f, 0.4f, 0.37f));
        }

        // Glass spires: tall thin shards leaning outward, each with a glowing tip.
        for (int i = 0; i < BarrensLayout.ShardSpires; i++)
        {
            var at = BarrensLayout.SpireAnchor(i) - off;
            var spire = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spire.name = SpirePrefix + i;
            spire.transform.SetParent(root.transform, false);
            float h = 2.6f + (i % 3) * 0.9f;
            spire.transform.localPosition = at + Vector3.up * (h * 0.45f);
            spire.transform.localRotation = Quaternion.Euler(8f + (i % 4) * 3f, i * 41f, (i % 2 == 0 ? 6f : -6f));
            spire.transform.localScale = new Vector3(0.7f, h, 0.5f);
            PaintGlass(spire, GlassTeal * 0.55f, 0.9f);
            var tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tip.name = "SpireTip";
            Object.DestroyImmediate(tip.GetComponent<Collider>());
            tip.transform.SetParent(spire.transform, false);
            tip.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            tip.transform.localScale = new Vector3(0.8f, 0.12f, 0.8f);
            PaintEmissive(tip, GlassTeal);
        }

        // Cold glow lights between the spires (the zone mood beside the volume grade).
        for (int i = 0; i < BarrensLayout.GlassLights; i++)
        {
            float a = (i * 60f + 15f) * Mathf.Deg2Rad;
            var go = new GameObject(LightPrefix + i);
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3(Mathf.Cos(a) * 10f, 1.6f, Mathf.Sin(a) * 10f);
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.55f, 0.95f, 0.9f);
            l.range = 11f;
            l.intensity = 1.8f;
        }

        TryKayKitDressing(root);
        SpawnEnemies(root.transform);
        SpawnGlasswright(root.transform);
        SpawnMoortor(root.transform);

        var gate = SpawnGate(ZoneGate.BarrensGateName, BarrensLayout.BarrensGatePos, BarrensLayout.BarrensArrival,
            ActProgress.Barrens, withHint: true);
        var kiln = GameObject.Find(KilnLayout.RootName);
        if (kiln != null) gate.transform.SetParent(kiln.transform, true);
        var ret = SpawnGate(ZoneGate.BarrensReturnGateName, BarrensLayout.BarrensReturnPos, BarrensLayout.KilnReturnArrival,
            ActProgress.Kiln, withHint: false);
        ret.transform.SetParent(root.transform, true);
    }

    // ---- Akt III Finale S3b: Moortor-Kette ----
    // The Moortor hangs off the Barrens south end (the south lane stays open:
    // the spire ring tops out at r 15.5, dunes sit north of z -11). Both gates
    // keep targetZone Barrens for the save (ZoneGate S3 contract); the pair
    // opens via the finale (RunProfile.Finale.MoortorOpen), not the act.
    static readonly Vector3 MoortorGatePos = BarrensLayout.BarrensOffset + new Vector3(4f, 1f, -15.5f);
    static readonly Vector3 MoortorArrival = BarrensLayout.BarrensOffset + new Vector3(0f, 1f, -18.5f);
    static readonly Vector3 MoortorReturnPos = BarrensLayout.BarrensOffset + new Vector3(-4f, 1f, -18f);
    static readonly Vector3 MoortorReturnArrival = BarrensLayout.BarrensOffset + new Vector3(4f, 1f, -13.5f);
    static readonly Vector3 ThewesPos = BarrensLayout.BarrensOffset + new Vector3(0f, 0.05f, -19f);

    /// <summary>
    /// S3b: Moortor gate pair after the BarrensGate pattern (cube trigger,
    /// <c>targetZone</c> + <c>destination</c>) plus Thewes, the Homecomer
    /// (H12) after the <c>SpawnGlasswright</c> pattern: boss tier for the
    /// target bar, melee through <see cref="EnemyAttack"/>, animated husk
    /// rig. The kill books generically (<c>EnemyHealth</c> →
    /// <c>BookKill(Thewes)</c> → <c>Finale.DefeatFinalBoss</c>): no new
    /// boss controller, no new system. No hint marker: the shared hint text
    /// names the act lock (Kilnmother), not the finale.
    /// </summary>
    static void SpawnMoortor(Transform parent)
    {
        var gate = SpawnGate(ZoneGate.MoortorGateName, MoortorGatePos, MoortorArrival,
            ActProgress.Barrens, withHint: false);
        gate.transform.SetParent(parent, true);
        var ret = SpawnGate(ZoneGate.MoortorReturnGateName, MoortorReturnPos, MoortorReturnArrival,
            ActProgress.Barrens, withHint: false);
        ret.transform.SetParent(parent, true);
        var thewes = SpawnBody("Skeletons/Characters/Skeleton_Warrior.fbx", StoryIds.Thewes,
            ThewesPos, GlasswrightController.MaxHP, LootTier.Boss, 2.2f, parent);
        var attack = thewes.AddComponent<EnemyAttack>();
        attack.damage = 3;
        attack.encounterGroup = EncounterLayout.FrontGroup;
        SandboxBuilder.WireHuskAnimator(thewes);
    }

    static void SpawnEnemies(Transform parent)
    {
        for (int i = 0; i < BarrensLayout.ShardbackCount; i++)
            SpawnShardback(BarrensLayout.ShardbackPrefix + i, BarrensLayout.ShardbackAnchor(0, i), 14, LootTier.Normal, 1f, 1,
                ShardGuard.DefaultArc, parent);
        for (int i = 0; i < BarrensLayout.EliteCount; i++)
            SpawnShardback(BarrensLayout.ElitePrefix + i, BarrensLayout.EliteAnchor(0, i), 30, LootTier.Elite, 1.3f, 2,
                ShardGuard.EliteArc, parent);
        for (int i = 0; i < BarrensLayout.AugurCount; i++)
        {
            var a = SpawnBody("Skeletons/Characters/Skeleton_Mage.fbx", BarrensLayout.AugurPrefix + i,
                BarrensLayout.AugurAnchor(0, i), 9, LootTier.Normal, 1f, parent);
            a.AddComponent<RangedCaster>();
            a.AddComponent<DuneAugur>();
            SandboxBuilder.WireHuskAnimator(a);
        }
    }

    /// <summary>S3: the zone boss (KayKit Warrior at 2.2×, glass crown), melee through EnemyAttack.</summary>
    static void SpawnGlasswright(Transform parent)
    {
        var boss = SpawnBody("Skeletons/Characters/Skeleton_Warrior.fbx", GlasswrightController.BossName,
            BarrensLayout.GlasswrightPos, GlasswrightController.MaxHP, LootTier.Boss, 2.2f, parent);
        var attack = boss.AddComponent<EnemyAttack>();
        attack.damage = 3;
        attack.encounterGroup = EncounterLayout.FrontGroup;
        boss.AddComponent<GlasswrightController>();
        boss.AddComponent<GlassWard>();
        var crown = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crown.name = "GlassCrown";
        Object.DestroyImmediate(crown.GetComponent<Collider>());
        crown.transform.SetParent(boss.transform, false);
        crown.transform.localPosition = new Vector3(0f, 2.05f, 0f);
        crown.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        crown.transform.localScale = new Vector3(0.5f, 0.35f, 0.5f);
        PaintEmissive(crown, GlassTeal);
        SandboxBuilder.WireHuskAnimator(boss);
    }

    static void SpawnShardback(string name, Vector3 pos, int hp, LootTier tier, float scale, int damage, float arc, Transform parent)
    {
        var e = SpawnBody("Skeletons/Characters/Skeleton_Warrior.fbx", name, pos, hp, tier, scale, parent);
        var attack = e.AddComponent<EnemyAttack>();
        attack.damage = damage;
        attack.turnRate = ShardGuard.TurnRate;
        attack.encounterGroup = EncounterLayout.FrontGroup;
        e.AddComponent<ShardGuard>().arcDegrees = arc;
        // Readable guard: a glowing glass slab in front of the body (no collider).
        var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slab.name = "GlassShield";
        Object.DestroyImmediate(slab.GetComponent<Collider>());
        slab.transform.SetParent(e.transform, false);
        slab.transform.localPosition = new Vector3(0f, 0.9f, 0.55f);
        slab.transform.localScale = new Vector3(tier == LootTier.Elite ? 1.3f : 0.95f, 1.1f, 0.08f);
        PaintEmissive(slab, GlassTeal * 0.8f);
        SandboxBuilder.WireHuskAnimator(e);
    }

    static GameObject SpawnBody(string model, string name, Vector3 pos, int hp, LootTier tier, float scale, Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ThirdParty/KayKit/" + model);
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
        // Face north, toward the arrival: the shields meet the party head-on.
        e.transform.rotation = Quaternion.LookRotation(Vector3.forward);
        e.transform.localScale = Vector3.one * scale;
        e.AddComponent<Unity.Netcode.NetworkObject>();
        var health = e.AddComponent<EnemyHealth>();
        health.SetTier(hp, tier);
        health.corpseModel = prefab;
        return e;
    }

    static GameObject SpawnGate(string name, Vector3 at, Vector3 dest, int zone, bool withHint)
    {
        var gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = name;
        gate.transform.position = at;
        gate.transform.localScale = new Vector3(2.4f, 2.4f, 0.6f);
        Paint(gate, new Color(0.3f, 0.7f, 0.68f));
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
            // Marker only: ZoneGate adds the TextMesh at runtime (URP-Lit-only scene contract).
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
        // A buried waystation the glass swallowed: broken walls, a toppled column, rubble.
        Place("wall_broken.fbx", "BarrensRuinWall_0", new Vector3(-12f, 0f, -3f), 75f);
        Place("wall_broken.fbx", "BarrensRuinWall_1", new Vector3(12f, 0f, 1f), 250f);
        Place("pillar.fbx", "BarrensPillar_0", new Vector3(-4f, 0f, 7f), 0f);
        Place("column.fbx", "BarrensColumn_0", new Vector3(5f, 0f, -4f), 30f);
        Place("rubble_large.fbx", "BarrensRubble_0", new Vector3(-8f, 0f, -14f), 110f);
        Place("rubble_half.fbx", "BarrensRubble_1", new Vector3(11f, 0f, -12f), 200f);
        Place("sword_shield_broken.fbx", "BarrensFallen_0", new Vector3(2f, 0f, 9f), 45f);
        Place("torch_lit.fbx", "BarrensTorch_0", new Vector3(-2.5f, 0f, 15f), 180f);
        Place("torch_lit.fbx", "BarrensTorch_1", new Vector3(2.5f, 0f, 15f), 180f);
    }

    static Material Lit(Color color)
    {
        return new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
    }

    static void Paint(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = Lit(color);
    }

    static void PaintGlass(GameObject go, Color color, float smoothness)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var m = Lit(color);
        m.SetFloat("_Smoothness", smoothness);
        m.SetFloat("_Metallic", 0.1f);
        r.sharedMaterial = m;
    }

    static void PaintEmissive(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var m = Lit(color);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", color * 2f);
        r.sharedMaterial = m;
    }

    /// <summary>Headless proof: Barrens root, spires, lights, dunes, sealed gate with hint, return gate.</summary>
    public static void VerifyBarrens(UnityEngine.SceneManagement.Scene scene)
    {
        bool root = false, gate = false, hint = false, ret = false, shrine = false;
        int spires = 0, lights = 0, dunes = 0, shardbacks = 0, augurs = 0, elites = 0;
        bool glasswright = false;
        foreach (var r in scene.GetRootGameObjects())
            foreach (var t in r.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                if (n == BarrensLayout.RootName && Vector3.Distance(t.position, BarrensLayout.BarrensOffset) < 0.01f) root = true;
                if (n.StartsWith(SpirePrefix)) spires++;
                if (n.StartsWith(DunePrefix)) dunes++;
                if (n.StartsWith(LightPrefix) && t.GetComponent<Light>() != null) lights++;
                var zg = t.GetComponent<ZoneGate>();
                if (n == ZoneGate.BarrensGateName && zg != null && zg.targetZone == ActProgress.Barrens)
                {
                    gate = true;
                    hint = t.Find(ZoneGate.LockedHintName) != null;
                }
                if (n == ZoneGate.BarrensReturnGateName && zg != null && zg.targetZone == ActProgress.Kiln) ret = true;
                var eh = t.GetComponent<EnemyHealth>();
                if (n.StartsWith(BarrensLayout.ShardbackPrefix) && t.GetComponent<ShardGuard>() != null
                    && t.GetComponent<EnemyAttack>() != null) shardbacks++;
                if (n.StartsWith(BarrensLayout.ElitePrefix) && t.GetComponent<ShardGuard>() != null
                    && eh != null && eh.tier == LootTier.Elite) elites++;
                if (n.StartsWith(BarrensLayout.AugurPrefix) && t.GetComponent<DuneAugur>() != null
                    && t.GetComponent<RangedCaster>() != null) augurs++;
                if (n == GlasswrightController.BossName && t.GetComponent<GlasswrightController>() != null
                    && t.GetComponent<GlassWard>() != null && eh != null && eh.tier == LootTier.Boss) glasswright = true;
                var ws = t.GetComponent<WaypointShrine>();
                if (ws != null && ws.shrineId == "Barrens" && ws.zone == ActProgress.Barrens) shrine = true;
            }
        Debug.Log($"[BarrensBuilder] Verify: root={root} spires={spires} lights={lights} dunes={dunes} " +
                  $"gate={gate} hint={hint} ret={ret} shrine={shrine} " +
                  $"shardbacks={shardbacks} augurs={augurs} elites={elites} glasswright={glasswright}");
        if (!root || spires != BarrensLayout.ShardSpires || lights != BarrensLayout.GlassLights
            || dunes != BarrensLayout.Dunes || !gate || !hint || !ret || !shrine
            || shardbacks != BarrensLayout.ShardbackCount || augurs != BarrensLayout.AugurCount
            || elites != BarrensLayout.EliteCount || !glasswright)
            throw new System.Exception("Barrens verify failed: zone 4 inventory incomplete.");
        Debug.Log("[BarrensBuilder] Verify: OK (glass barrens + gates present).");
    }
}
