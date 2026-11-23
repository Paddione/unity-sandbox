using System;
using System.Collections.Generic;
using UnityEngine;

public static class HeroGearModels
{
    public static readonly Color Brass = new Color32(242, 193, 62, 255);
    static readonly Color Iron = new Color32(124, 151, 159, 255);
    static readonly Color Dark = new Color32(51, 58, 74, 255);
    static readonly Color Ember = new Color32(246, 95, 47, 255);
    static readonly Color Pearl = new Color32(221, 213, 189, 255);
    static readonly Color Jade = new Color32(78, 195, 177, 255);
    static readonly Color Lilac = new Color32(173, 126, 223, 255);

    readonly struct Model
    {
        public readonly ItemSlot Slot;
        public readonly Action<Transform> Build;
        public Model(ItemSlot slot, Action<Transform> build) { Slot = slot; Build = build; }
    }

    static readonly Dictionary<string, Model> Models = new Dictionary<string, Model>(StringComparer.Ordinal)
    {
        ["Emberbrand Blade"] = new Model(ItemSlot.Weapon, root =>
        {
            Grip(root);
            Plate(root, "Flame edge", Ember, .045f, 0,
                -.10f,.17f, -.04f,.31f, -.09f,.43f, 0,.66f, .09f,.46f, .045f,.31f, .09f,.17f);
        }),
        [ItemDatabase.LancerWardId] = new Model(ItemSlot.Weapon, root =>
        {
            Grip(root, .46f);
            Plate(root, "Bearded axe head", Iron, .075f, 0,
                -.07f,.38f, -.07f,.56f, .12f,.59f, .28f,.66f, .32f,.47f, .28f,.28f, .15f,.32f, .12f,.41f);
        }),
        [ItemDatabase.RogueKeepsakeId] = new Model(ItemSlot.Weapon, root =>
        {
            Grip(root);
            Plate(root, "Hooked blade", Lilac, .045f, 0, -.06f,.14f, -.04f,.36f, .05f,.46f,
                .16f,.42f, .09f,.39f, .055f,.29f, .06f,.14f);
        }),
        [ItemDatabase.NightfangId] = new Model(ItemSlot.Weapon, root =>
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Plate(root, "Recurved limb", Lilac, .05f, 0, -.025f,0, .035f,0, .12f,.20f*side,
                    .08f,.36f*side, -.06f,.53f*side, -.03f,.35f*side, .04f,.18f*side);
                Beam(root, "String", Pearl, new Vector3(-.06f,.52f*side,0), new Vector3(-.06f,0,0), .009f);
            }
        }),
        ["Lantern of the Hold"] = new Model(ItemSlot.Weapon, root =>
        {
            Beam(root, "Handle", Dark, Vector3.zero, new Vector3(0,-.16f,0), .045f);
            Box(root, "Square roof", Brass, new Vector3(0,-.15f,0), new Vector3(.28f,.07f,.28f));
            Box(root, "Square foot", Brass, new Vector3(0,-.44f,0), new Vector3(.24f,.055f,.24f));
            Box(root, "Light chamber", Ember, new Vector3(0,-.30f,0), new Vector3(.15f,.20f,.15f));
            foreach (float x in new[] { -.105f, .105f })
                foreach (float z in new[] { -.105f, .105f })
                    Box(root, "Corner post", Dark, new Vector3(x,-.30f,z), new Vector3(.029f,.27f,.029f));
        }),
        [ItemDatabase.WickOfHaloId] = new Model(ItemSlot.Weapon, root =>
        {
            Beam(root, "Censer handle", Pearl, Vector3.zero, new Vector3(0,-.17f,0), .05f);
            Crystal(root, "Censer bowl", Brass, new Vector3(0,-.44f,0), new Vector3(.30f,.17f,.30f));
            Crystal(root, "Censer core", Jade, new Vector3(0,-.33f,0), new Vector3(.17f,.22f,.17f));
            for (int i = 0; i < 3; i++)
            {
                float angle = i * 2f * Mathf.PI / 3f;
                Beam(root, "Chain", Iron, new Vector3(0,-.14f,0),
                    new Vector3(.127f * Mathf.Cos(angle),-.42f,.127f * Mathf.Sin(angle)), .018f);
            }
        }),
        ["Cryptwarden Plate"] = new Model(ItemSlot.Armor, root =>
        {
            Plate(root, "Tomb plate", Iron, .10f, .36f, -.30f,.10f, .30f,.10f, .23f,-.27f,
                0,-.37f, -.23f,-.27f);
            Box(root, "Raised spine", Brass, new Vector3(0,-.10f,.425f), new Vector3(.09f,.34f,.045f));
        }),
        [ItemDatabase.BulwarkCarrierId] = new Model(ItemSlot.Armor, root =>
        {
            Plate(root, "Tall shield breastplate", Brass, .11f, .36f, -.37f,.13f, 0,.20f, .37f,.13f,
                .31f,-.24f, 0,-.38f, -.31f,-.24f);
            Box(root, "Crossbar", Dark, new Vector3(0,-.02f,.429f), new Vector3(.52f,.075f,.05f));
        }),
        [ItemDatabase.MercyShroudId] = new Model(ItemSlot.Armor, root =>
        {
            Plate(root, "Split mantle", Pearl, .06f, .36f, -.39f,.10f, -.14f,.18f, 0,.05f, .14f,.18f,
                .39f,.10f, .31f,-.15f, .08f,-.30f, 0,-.18f, -.08f,-.30f, -.31f,-.15f);
            Crystal(root, "Brooch", Jade, new Vector3(0,.015f,.405f), new Vector3(.14f,.16f,.08f));
        }),
        [ItemDatabase.ShadowstrideVestId] = new Model(ItemSlot.Armor, root =>
        {
            Plate(root, "Asymmetric leather panels", Dark, .09f, .36f, -.30f,.10f, -.06f,.12f, .30f,-.29f,
                .22f,-.39f, -.29f,-.29f);
            Beam(root, "Diagonal clasp", Lilac, new Vector3(-.23f,.08f,.419f),
                new Vector3(.20f,-.31f,.419f), .063f);
        }),
        [ItemDatabase.ShardbackAegisId] = new Model(ItemSlot.Armor, root =>
        {
            Plate(root, "Jagged cuirass", Dark, .10f, .36f, -.32f,.09f, 0,.14f, .32f,.09f, .20f,-.37f, -.20f,-.37f);
            for (int i = -2; i <= 2; i++)
                Crystal(root, "Glass splinter", Jade, new Vector3(i*.13f,.12f-Mathf.Abs(i)*.042f,-.39f),
                    new Vector3(.14f,.49f-Mathf.Abs(i)*.095f,.20f));
        }),
        ["Seer's Circlet"] = new Model(ItemSlot.Helmet, root =>
        {
            Band(root, Brass, .26f, .20f, .51f);
            Crystal(root, "Oracle stone", Lilac, new Vector3(0,.32f,.27f), new Vector3(.16f,.24f,.12f));
        }),
        [ItemDatabase.SeismicCrownId] = new Model(ItemSlot.Helmet, root =>
        {
            Band(root, Iron, .30f, .21f, .55f);
            for (int i = -2; i <= 2; i++)
                Crystal(root, "Fault spire", Brass, new Vector3(i*.116f,.38f-Mathf.Abs(i)*.033f,.23f),
                    new Vector3(.10f,.31f,.13f));
        }),
        [ItemDatabase.DawnCircleId] = new Model(ItemSlot.Helmet, root =>
        {
            Band(root, Brass, .25f, .18f, .51f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                var center = new Vector3(0,.32f,-.25f);
                var ray = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle),0);
                Beam(root, "Sun ray", Pearl, center + ray*.16f, center + ray*.34f, .047f);
            }
        }),
        [ItemDatabase.KeenCadenceId] = new Model(ItemSlot.Helmet, root =>
        {
            Plate(root, "Forward cowl", Dark, .10f, .24f, -.26f,.17f, -.27f,.36f, -.14f,.50f, .14f,.50f,
                .27f,.36f, .26f,.17f, .13f,.31f, -.13f,.31f);
            Box(root, "Hood crest", Lilac, new Vector3(0,.44f,.29f), new Vector3(.075f,.08f,.20f));
        }),
        [ItemDatabase.GlasswrightPrismId] = new Model(ItemSlot.Helmet, root =>
        {
            Band(root, Dark, .26f, .21f, .54f);
            Crystal(root, "Central prism", Jade, new Vector3(0,.50f,.15f), new Vector3(.30f,.42f,.28f));
            Crystal(root, "Left prism", Jade, new Vector3(-.24f,.42f,.13f), new Vector3(.13f,.25f,.16f));
            Crystal(root, "Right prism", Jade, new Vector3(.24f,.42f,.13f), new Vector3(.13f,.25f,.16f));
        }),
    };

    static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();
    static Mesh cube;
    static Mesh Cube => cube != null ? cube : cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
    static readonly int ColorProperty = Shader.PropertyToID("_BaseColor");

    public static bool IsKnown(string id, ItemSlot slot) =>
        id != null && Models.TryGetValue(id, out var model) && model.Slot == slot;

    public static IEnumerable<string> Ids => Models.Keys;

    public static GameObject Create(string id, ItemSlot slot, Transform parent)
    {
        if (!IsKnown(id, slot)) return null;
        var root = new GameObject(id);
        root.transform.SetParent(parent, false);
        if (slot == ItemSlot.Helmet)
        {
            root.transform.localPosition = new Vector3(0f, .47f, 0f);
            root.transform.localScale = new Vector3(1.85f, 1f, 1.85f);
        }
        Models[id].Build(root.transform);
        return root;
    }

    public static void StarterArmor(Transform root, string classId)
    {
        Plate(root, "Yellow chest guard", Brass, .065f, .36f, -.33f,.13f, .33f,.13f, .27f,-.18f, 0,-.28f, -.27f,-.18f);
        var trim = classId == ClassCatalog.RogueId ? Lilac : classId == ClassCatalog.LanternbearerId ? Pearl : Iron;
        Box(root, "Guard clasp", trim, new Vector3(0,-.06f,.405f), new Vector3(.12f,.09f,.04f));
    }

    static void Grip(Transform root, float length = .22f)
    {
        Beam(root, "Grip", Dark, new Vector3(0,-.13f,0), new Vector3(0,length,0), .065f);
        Box(root, "Guard", Brass, new Vector3(0,.13f,0), new Vector3(.27f,.05f,.09f));
    }

    static void Band(Transform root, Color color, float radius, float y, float width)
    {
        for (int i = 0; i < 8; i++)
        {
            float a = Mathf.PI * i / 4f;
            float b = Mathf.PI * (i + 1) / 4f;
            Beam(root, "Band", color, new Vector3(radius*Mathf.Cos(a),y,width*.5f*Mathf.Sin(a)),
                new Vector3(radius*Mathf.Cos(b),y,width*.5f*Mathf.Sin(b)), .065f);
        }
    }

    static void Box(Transform root, string name, Color color, Vector3 center, Vector3 size)
    {
        var part = Part(root, name, color, Cube);
        part.localPosition = center;
        part.localScale = size;
    }

    static void Beam(Transform root, string name, Color color, Vector3 a, Vector3 b, float width)
    {
        var part = Part(root, name, color, Cube);
        part.localPosition = (a + b) * .5f;
        part.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
        part.localScale = new Vector3(width, Vector3.Distance(a, b), width);
    }

    static void Crystal(Transform root, string name, Color color, Vector3 center, Vector3 size)
    {
        Plate(root, name, color, size.z, center.z, center.x-size.x/2,center.y, center.x,center.y+size.y/2,
            center.x+size.x/2,center.y, center.x,center.y-size.y/2);
    }

    // A faceted outline with solid side walls and no imported mesh dependencies.
    static void Plate(Transform root, string name, Color color, float depth, float z, params float[] points)
    {
        int count = points.Length / 2;
        var vertices = new Vector3[count * 2];
        for (int i = 0; i < count; i++)
        {
            vertices[i] = new Vector3(points[i * 2], points[i * 2 + 1], z + depth / 2);
            vertices[i + count] = vertices[i] - Vector3.forward * depth;
        }
        var perimeter = new List<int>(count);
        float area = 0f;
        for (int i = 0; i < count; i++)
        {
            perimeter.Add(i);
            var a = vertices[i];
            var b = vertices[(i + 1) % count];
            area += a.x * b.y - b.x * a.y;
        }
        if (area < 0f) perimeter.Reverse();

        var faces = new List<Vector3>();
        for (int i = 0; i < count; i++)
        {
            int a = perimeter[i];
            int b = perimeter[(i + 1) % count];
            faces.AddRange(new[] { vertices[a], vertices[a + count], vertices[b],
                vertices[b], vertices[a + count], vertices[b + count] });
        }

        // Clip convex ears so the notches remain open on concave silhouettes.
        while (perimeter.Count >= 3)
        {
            bool clipped = false;
            for (int i = 0; i < perimeter.Count; i++)
            {
                int a = perimeter[(i + perimeter.Count - 1) % perimeter.Count];
                int b = perimeter[i];
                int c = perimeter[(i + 1) % perimeter.Count];
                if (Turn(vertices[a], vertices[b], vertices[c]) <= .000001f) continue;
                bool contains = perimeter.Exists(j => j != a && j != b && j != c &&
                    Turn(vertices[a], vertices[b], vertices[j]) >= 0f &&
                    Turn(vertices[b], vertices[c], vertices[j]) >= 0f &&
                    Turn(vertices[c], vertices[a], vertices[j]) >= 0f);
                if (contains) continue;
                faces.AddRange(new[] { vertices[a], vertices[b], vertices[c],
                    vertices[a + count], vertices[c + count], vertices[b + count] });
                perimeter.RemoveAt(i);
                clipped = true;
                break;
            }
            if (!clipped) throw new InvalidOperationException($"Invalid gear outline: {name}.");
        }
        var indices = new int[faces.Count];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        var mesh = new Mesh { name = name, vertices = faces.ToArray(), triangles = indices };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        Part(root, name, color, mesh).gameObject.AddComponent<GeneratedGearMesh>().Mesh = mesh;
    }

    static float Turn(Vector3 a, Vector3 b, Vector3 c) =>
        (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

    static Transform Part(Transform root, string name, Color color, Mesh mesh)
    {
        var part = new GameObject(name);
        part.transform.SetParent(root, false);
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        if (!Materials.TryGetValue(color, out var material) || material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor(ColorProperty, color);
            Materials[color] = material;
        }
        part.AddComponent<MeshRenderer>().sharedMaterial = material;
        return part.transform;
    }
}
