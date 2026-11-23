using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// coop-trio: heroes do not body-block each other (an idle ally stalled the
/// host in the Fen for 120 s), while a husk still blocks a hero.
/// </summary>
public class HeroAllyCollisionTests
{
    readonly System.Collections.Generic.List<GameObject> made = new System.Collections.Generic.List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        foreach (var g in made) if (g != null) Object.DestroyImmediate(g);
        made.Clear();
    }

    CharacterController Hero(string name, Vector3 at)
    {
        var go = new GameObject(name);
        go.transform.position = at;
        made.Add(go);
        return go.AddComponent<CharacterController>();
    }

    [UnityTest]
    public IEnumerator Hero_WalksThroughIdleAlly_ButNotThroughAnEnemy()
    {
        var mover = Hero("Mover", new Vector3(0f, 1f, 0f));
        var ally = Hero("Ally", new Vector3(1.2f, 1f, 0f));
        var enemyGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemyGo.transform.position = new Vector3(-1.2f, 1f, 0f);
        made.Add(enemyGo);
        yield return new WaitForFixedUpdate();

        Assert.AreEqual(1, DiabloNetworkPlayer.IgnorePairs(mover, new[] { ally, mover, null }),
            "self and null are skipped");
        Assert.IsTrue(Physics.GetIgnoreCollision(mover, ally));

        mover.Move(new Vector3(2.4f, 0f, 0f));
        Assert.Greater(mover.transform.position.x, 2f, "passes through the ally");

        mover.transform.position = new Vector3(0f, 1f, 0f);
        Physics.SyncTransforms();
        mover.Move(new Vector3(-2.4f, 0f, 0f));
        Assert.Greater(mover.transform.position.x, -1f, "an enemy still blocks");
    }
}
