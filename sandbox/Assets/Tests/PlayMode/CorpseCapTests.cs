using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>act-one perf: corpse cap and animator stop after the death take.</summary>
public class CorpseCapTests
{
    readonly List<GameObject> spawned = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (var c in Object.FindObjectsOfType<DiabloCorpse>()) Object.DestroyImmediate(c.gameObject);
        foreach (var go in spawned) if (go != null) Object.DestroyImmediate(go);
        spawned.Clear();
        DiabloCorpse.ResetRun();
    }

    GameObject Husk(int i)
    {
        var go = new GameObject($"CapHusk_{i}");
        go.AddComponent<EnemyHealth>();
        go.AddComponent<Animator>();
        spawned.Add(go);
        return go;
    }

    [UnityTest]
    public IEnumerator OverTheCap_TheOldestCorpsesStartSinking()
    {
        var corpses = new List<DiabloCorpse>();
        for (int i = 0; i < DiabloCorpse.MaxCorpses + 2; i++)
            corpses.Add(DiabloCorpse.SpawnFrom(Husk(i)).GetComponent<DiabloCorpse>());
        yield return null;
        Assert.GreaterOrEqual(corpses[0].Age, DiabloCorpse.LieSeconds, "oldest sinks");
        Assert.GreaterOrEqual(corpses[1].Age, DiabloCorpse.LieSeconds, "second oldest sinks");
        Assert.Less(corpses[2].Age, DiabloCorpse.LieSeconds, "the rest keeps lying");
        Assert.Less(corpses[corpses.Count - 1].Age, DiabloCorpse.LieSeconds);
    }

    [UnityTest]
    public IEnumerator Animator_StopsAfterTheDeathTake()
    {
        var corpse = DiabloCorpse.SpawnFrom(Husk(0)).GetComponent<DiabloCorpse>();
        var anim = corpse.GetComponent<Animator>();
        Assert.IsNotNull(anim);
        // The clone path strips behaviours; in play the corpse model gets an active
        // Animator through CopyLook. Switch it on to pin the stop.
        yield return null; // Start: without a controller the corpse switches its Animator off itself
        anim.enabled = true;
        yield return null;
        Assert.IsTrue(anim.enabled, "still playing the death take");
        corpse.Advance(DiabloCorpse.AnimatorSeconds);
        yield return null;
        Assert.IsFalse(anim.enabled, "pose kept, animator off");
        Assert.IsNotNull(corpse, "still lying");
    }
}
