using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>cinderreach-town-npc-identity: each Hold figure has its own body + tint and turns toward heroes.</summary>
public sealed class TownNpcIdentityTests
{
    const string ZonePath = "Assets/Scenes/DiabloZone.unity";

    [Test]
    public void Catalog_EveryNamedNpc_HasOwnBodyAndTint()
    {
        var ids = new[] { StoryIds.Ilsabe, StoryIds.Jorin, StoryIds.Ottilie, StoryIds.Merle };
        var bodies = new HashSet<string>();
        var tints = new HashSet<Color>();
        foreach (var id in ids)
        {
            Assert.IsTrue(NpcAppearance.TryGet(id, out var look), id);
            Assert.IsTrue(bodies.Add(look.model), $"{id}: body {look.model} used twice");
            Assert.IsTrue(tints.Add(look.tint), $"{id}: tint used twice");
            Assert.AreNotEqual(Color.white, look.tint, $"{id}: an untinted body would read like a hero");
        }
    }

    [Test]
    public void Scene_HoldFigures_UniqueMeshAndTint_AndWatchHeroes()
    {
        EditorSceneManager.OpenScene(ZonePath, OpenSceneMode.Single);
        var figures = new Dictionary<string, string>
        {
            { StoryIds.Ilsabe, Vendor.VendorObjectName },
            { StoryIds.Ottilie, "StoryNpc_" + StoryIds.Ottilie },
            { StoryIds.Merle, "StoryNpc_" + StoryIds.Merle },
            { StoryIds.Jorin, "SmithFigure_jorin" }, // TownBuilder.SmithFigureName (Editor assembly)
        };
        var signatures = new HashSet<string>();
        foreach (var kv in figures)
        {
            var go = GameObject.Find(kv.Value);
            Assert.IsNotNull(go, kv.Value + " placed in the Hold");
            var look = go.GetComponent<NpcLook>();
            Assert.IsNotNull(look, kv.Value + " carries its tint");
            Assert.AreEqual(kv.Key, look.npcId);
            Assert.IsNotNull(go.GetComponent<NpcFacing>(), kv.Value + " turns toward heroes");
            Assert.IsNotNull(go.GetComponent<Animator>(), kv.Value + " is an animated figure");
            var meshes = go.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(r => r.sharedMesh != null).Select(r => r.sharedMesh.name).OrderBy(n => n);
            NpcAppearance.TryGet(kv.Key, out var tint);
            string signature = string.Join("+", meshes) + "|" + tint.tint;
            Assert.IsTrue(signatures.Add(signature), $"{kv.Value}: mesh+tint signature repeats ({signature})");
        }
    }
}
