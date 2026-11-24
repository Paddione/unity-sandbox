using NUnit.Framework;
using UnityEngine;

/// <summary>
/// cinderreach-companion-rendering: runtime husk bodies prefer an explicit
/// model, then the scene template, and never ship a bare primitive.
/// </summary>
public class HuskModelLibraryTests
{
    GameObject template;
    GameObject templateModel;
    GameObject spawned;

    [TearDown]
    public void TearDown()
    {
        if (spawned != null) Object.DestroyImmediate(spawned);
        if (template != null) Object.DestroyImmediate(template);
        if (templateModel != null) Object.DestroyImmediate(templateModel);
        spawned = template = templateModel = null;
    }

    /// <summary>Model with a marker child (Instantiate renames the root, children keep their names).</summary>
    static GameObject ModelWithMarker(string marker)
    {
        var model = new GameObject("Model");
        var visual = new GameObject(marker);
        visual.transform.SetParent(model.transform, false);
        return model;
    }

    /// <summary>Template: a husk carrying a model (like the builder husks carry their FBX).</summary>
    void BuildTemplate()
    {
        templateModel = ModelWithMarker("TemplateVisual");
        template = new GameObject("TemplateHusk");
        var health = template.AddComponent<EnemyHealth>();
        health.corpseModel = templateModel;
        template.tag = "Husk";
    }

    [Test]
    public void SpawnHusk_UsesExplicitModelFirst()
    {
        BuildTemplate();
        var explicitModel = ModelWithMarker("ExplicitVisual");
        try
        {
            spawned = HuskModelLibrary.SpawnHusk("Elite", Vector3.zero, 1.3f, 15, LootTier.Elite, 2, explicitModel, netServer: false);
            Assert.AreEqual("Elite", spawned.name);
            Assert.AreEqual("Husk", spawned.tag);
            Assert.IsNotNull(spawned.transform.Find("ExplicitVisual"), "explicit model wins over the scene template");
            Assert.IsNull(spawned.transform.Find("TemplateVisual"));
        }
        finally { Object.DestroyImmediate(explicitModel); }
    }

    [Test]
    public void SpawnHusk_FallsBackToSceneTemplate()
    {
        BuildTemplate();
        spawned = HuskModelLibrary.SpawnHusk("Summon", new Vector3(1f, 0f, 2f), 1f, 5, LootTier.Normal, 1, null, netServer: false);
        Assert.IsNotNull(spawned.transform.Find("TemplateVisual"), "scene husk template renders the summon");
        var health = spawned.GetComponent<EnemyHealth>();
        Assert.IsNotNull(health);
        var attack = spawned.GetComponent<EnemyAttack>();
        Assert.IsNotNull(attack);
        Assert.AreEqual(1, attack.damage);
        Assert.IsNotNull(spawned.GetComponent<HuskAnimator>(), "summons animate");
        Assert.IsNotNull(spawned.GetComponent<Collider>(), "summons collide");
        Assert.AreEqual(new Vector3(1f, 0f, 2f), spawned.transform.position);
    }

    [Test]
    public void SpawnHusk_LastResortCapsuleIsNeverBare()
    {
        spawned = HuskModelLibrary.SpawnHusk("LastResort", Vector3.zero, 1f, 1, LootTier.Normal, 1, null, netServer: false);
        Assert.AreEqual("Husk", spawned.tag);
        Assert.IsNotNull(spawned.GetComponent<EnemyHealth>());
        var rend = spawned.GetComponent<Renderer>();
        Assert.IsNotNull(rend);
        if (rend.sharedMaterial != null && rend.sharedMaterial.shader != null)
            Assert.IsTrue(rend.sharedMaterial.shader.name.Contains("Universal Render Pipeline"),
                "tinted URP, never the magenta default");
    }
}
