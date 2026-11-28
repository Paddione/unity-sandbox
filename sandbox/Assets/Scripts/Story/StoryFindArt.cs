using System.Collections.Generic;
using UnityEngine;

// Pickups share one assembled mesh per item ID. Their scene colliders stay on the original objects.
internal static class StoryFindArt
{
    enum Surface { Paper, Ink, Leather, Wax, Stone, Iron, Brass, Glass, Ember, Wood }
    static readonly Color[] Colors = {
        new(.83f, .72f, .51f), new(.17f, .12f, .09f), new(.32f, .16f, .09f),
        new(.62f, .13f, .09f), new(.43f, .42f, .39f), new(.45f, .47f, .49f),
        new(.74f, .54f, .24f), new(.31f, .68f, .66f), new(.98f, .53f, .12f), new(.39f, .24f, .12f)
    };
    static readonly Dictionary<Surface, Material> Materials = new();
    static readonly Dictionary<string, Mesh> Meshes = new();

    static readonly Dictionary<string, (Mesh mesh, Material[] materials)> Finds = new();

    static Mesh Fragment()
    {
        var spine = new Vector3(0, .30f, -.07f);
        var back = new Vector3(0, -.25f, -.10f);
        var rim = new Vector3[] { new(0, 0, .55f), new(.26f, 0, -.16f), new(.08f, 0, -.45f), new(-.19f, 0, -.25f) };
        var vertices = new List<Vector3>();
        for (int i = 0; i < rim.Length; i++)
        {
            var a = rim[i];
            var b = rim[(i + 1) % rim.Length];
            vertices.AddRange(new[] { spine, a, b, back, b, a });
        }
        var mesh = new Mesh { name = "Ridged fragment" };
        mesh.SetVertices(vertices);
        var indices = new int[vertices.Count];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        mesh.SetTriangles(indices, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    static Mesh SharedMesh(string name)
    {
        if (!Meshes.TryGetValue(name, out var mesh) || !mesh)
            Meshes[name] = mesh = name == "Fragment" ? Fragment() : Resources.GetBuiltinResource<Mesh>(name + ".fbx");
        return mesh;
    }

    public static void Dress(StoryPickup find)
    {
        var original = find.GetComponent<MeshRenderer>();
        var filter = find.GetComponent<MeshFilter>();
        if (!original || !filter || string.IsNullOrEmpty(find.itemId)) return;
        if (Finds.TryGetValue(find.itemId, out var ready) && ready.mesh)
        {
            filter.sharedMesh = ready.mesh;
            original.sharedMaterials = ready.materials;
            return;
        }
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) return;
        var surfaces = new Dictionary<Surface, List<CombineInstance>>();
        void Part(string shape, Surface surface, Vector3 position, Vector3 scale, float angle = 0f)
        {
            if (!surfaces.TryGetValue(surface, out var parts))
                surfaces[surface] = parts = new List<CombineInstance>();
            parts.Add(new CombineInstance { mesh = SharedMesh(shape),
                transform = Matrix4x4.TRS(position, Quaternion.Euler(0, angle, 0), scale) });
            if (!Materials.TryGetValue(surface, out var material) || !material)
            {
                material = new Material(shader) { name = "Story " + surface };
                material.color = Colors[(int)surface];
                bool metal = surface == Surface.Iron || surface == Surface.Brass;
                material.SetFloat("_Metallic", metal ? .72f : 0f);
                material.SetFloat("_Smoothness", metal || surface == Surface.Glass ? .68f : .22f);
                Materials[surface] = material;
            }
        }
        void Page(Surface stock, float width, float length)
        {
            Part("Cube", stock, Vector3.zero, new(width, .13f, length));
            for (int i = 0; i < 4; i++)
                Part("Cube", Surface.Ink, new(-.03f, .075f, .19f - i * .105f), new(width * (i == 3 ? .46f : .68f), .02f, .014f));
        }
        switch (find.itemId)
        {
            case StoryIds.Lieferbuch:
                Part("Cube", Surface.Leather, new(0, -.27f, 0), new(.89f, .55f, 1.05f));
                Page(Surface.Paper, .81f, .92f);
                Part("Cube", Surface.Leather, new(-.4f, .03f, 0), new(.12f, .36f, 1.04f));
                break;
            case StoryIds.Fuhrbrief:
            case StoryIds.Fuhrliste:
            case StoryIds.Russseite:
            case StoryIds.Spendenseite:
            case StoryIds.Fuhrfetzen:
                bool scrap = find.itemId == StoryIds.Fuhrfetzen;
                Page(find.itemId == StoryIds.Russseite ? Surface.Leather : Surface.Paper, scrap ? .48f : .82f, scrap ? .66f : 1f);
                if (find.itemId == StoryIds.Fuhrbrief || find.itemId == StoryIds.Spendenseite)
                    Part("Cylinder", Surface.Wax, new(.21f, .1f, -.32f), new(.16f, .05f, .13f));
                break;
            case StoryIds.Siegelband:
                Part("Cube", Surface.Wax, Vector3.zero, new(.21f, .10f, 1.05f), -18f);
                Part("Cylinder", Surface.Brass, new(0, .09f, 0), new(.39f, .09f, .29f));
                break;
            case StoryIds.Grabtafel:
            case StoryIds.Tafelbruch:
                Page(Surface.Stone, find.itemId == StoryIds.Grabtafel ? .94f : .58f, .85f);
                Part("Cube", Surface.Stone, new(0, -.22f, 0), new(find.itemId == StoryIds.Grabtafel ? .90f : .53f, .4f, .81f));
                break;
            case StoryIds.Grablicht:
                Part("Cylinder", Surface.Brass, new(0, -.25f, 0), new(.68f, .14f, .52f));
                Part("Cylinder", Surface.Paper, Vector3.zero, new(.36f, .34f, .28f));
                Part("Sphere", Surface.Ember, new(0, .94f, 0), new(.14f, .54f, .10f));
                break;
            case StoryIds.Sehglas:
                Part("Cube", Surface.Wood, new(0, 0, -.29f), new(.13f, .22f, .51f));
                Part("Cylinder", Surface.Brass, new(0, 0, .18f), new(.72f, .14f, .54f));
                Part("Sphere", Surface.Glass, new(0, .15f, .18f), new(.61f, .22f, .46f));
                break;
            case StoryIds.Speichenrad:
                for (int i = 0; i < 10; i++)
                {
                    float a = i * Mathf.PI / 5;
                    Part("Cube", Surface.Wood, new(.42f * Mathf.Sin(a), 0, .42f * Mathf.Cos(a)), new(.30f, .28f, .09f), i * 36f);
                    if (i < 5) Part("Cube", Surface.Wood, Vector3.zero, new(.06f, .18f, .78f), i * 36f);
                }
                Part("Cylinder", Surface.Iron, Vector3.zero, new(.18f, .24f, .18f));
                break;
            case StoryIds.Glasscherbe:
            case StoryIds.Lanzenspitze:
            case StoryIds.Glutspur:
                Surface finish = find.itemId == StoryIds.Lanzenspitze ? Surface.Iron :
                    find.itemId == StoryIds.Glasscherbe ? Surface.Glass : Surface.Ember;
                Part("Fragment", finish, Vector3.zero, new(.95f, .95f, .93f), 12f);
                if (find.itemId == StoryIds.Lanzenspitze)
                    Part("Cylinder", Surface.Iron, new(0, -.16f, -.33f), new(.15f, .20f, .21f));
                else
                    Part("Fragment", finish, new(.28f, 0, -.23f), new(.45f, .52f, .42f), -20f);
                break;
            default:
                return;
        }
        var sections = new List<CombineInstance>();
        var finishes = new List<Material>();
        foreach (var pair in surfaces)
        {
            var section = new Mesh();
            section.CombineMeshes(pair.Value.ToArray());
            sections.Add(new CombineInstance { mesh = section });
            finishes.Add(Materials[pair.Key]);
        }
        var result = new Mesh { name = find.itemId };
        result.CombineMeshes(sections.ToArray(), false, false);
        foreach (var section in sections)
        {
            if (Application.isPlaying) Object.Destroy(section.mesh);
            else Object.DestroyImmediate(section.mesh);
        }
        var materials = finishes.ToArray();
        Finds[find.itemId] = (result, materials);
        filter.sharedMesh = result;
        original.sharedMaterials = materials;
    }
}
