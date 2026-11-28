using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class StoryFindArtTests
{
    [UnityTest]
    public IEnumerator FindsUseItemMeshesAndKeepCollectionTriggers()
    {
        var ids = new[] {
            StoryIds.Fuhrbrief, StoryIds.Lieferbuch, StoryIds.Fuhrfetzen, StoryIds.Siegelband,
            StoryIds.Grabtafel, StoryIds.Tafelbruch, StoryIds.Grablicht, StoryIds.Fuhrliste,
            StoryIds.Russseite, StoryIds.Sehglas, StoryIds.Glasscherbe, StoryIds.Speichenrad,
            StoryIds.Spendenseite, StoryIds.Glutspur, StoryIds.Lanzenspitze, StoryIds.Sehglas
        };
        var finds = new List<GameObject>();
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        try
        {
            foreach (string id in ids)
            {
                var find = GameObject.CreatePrimitive(PrimitiveType.Cube);
                finds.Add(find);
                find.GetComponent<MeshRenderer>().sharedMaterial = material;
                find.GetComponent<BoxCollider>().isTrigger = true;
                find.AddComponent<StoryPickup>().itemId = id;
            }
            yield return null;
            for (int i = 0; i < finds.Count; i++)
            {
                var mesh = finds[i].GetComponent<MeshFilter>().sharedMesh;
                Assert.AreEqual(ids[i], mesh.name);
                Assert.Greater(mesh.vertexCount, 0);
                Assert.AreEqual(mesh.subMeshCount, finds[i].GetComponent<MeshRenderer>().sharedMaterials.Length);
                Assert.IsTrue(finds[i].GetComponent<BoxCollider>().isTrigger);
                Assert.AreEqual(Vector3.one, finds[i].GetComponent<BoxCollider>().size);
            }
            Assert.AreSame(finds[9].GetComponent<MeshFilter>().sharedMesh, finds[15].GetComponent<MeshFilter>().sharedMesh);
        }
        finally
        {
            foreach (var find in finds) Object.Destroy(find);
            Object.Destroy(material);
        }
    }
}
