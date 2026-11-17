using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>Feel polish end-to-end (headless): child-renderer flash, husk
/// anim hooks, corpses, steering around dressing, bounded hit-stop.</summary>
public class DiabloFeelPolishLoopTests
{
    GameObject player;
    DiabloClickController controller;
    DiabloMelee melee;
    GameObject wall;

    [SetUp]
    public void Setup()
    {
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        DiabloAudio.ResetRun();
        DiabloCorpse.ResetRun();
        HitStop.ResetRun();

        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.layer = LayerMask.NameToLayer("Ignore Raycast");
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.AddComponent<CharacterController>();
        controller = player.AddComponent<DiabloClickController>();
        melee = player.AddComponent<DiabloMelee>();
        melee.cooldown = 0f;
        player.AddComponent<PlayerHealth>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var c in GameObject.FindObjectsOfType<DiabloCorpse>())
            Object.DestroyImmediate(c.gameObject);
        if (wall != null) Object.DestroyImmediate(wall);
        DiabloTestFixture.CleanupTestScene(player);
        var bar = GameObject.Find("DTargetBar");
        if (bar != null) Object.DestroyImmediate(bar);
        HitStop.ResetRun();
        Time.timeScale = 1f;
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
        DiabloAudio.ResetRun();
        DiabloCorpse.ResetRun();
    }

    /// <summary>KayKit-shaped husk: empty root, mesh on a child (root has no renderer).</summary>
    static GameObject SpawnRiggedHusk(Vector3 pos, int hp)
    {
        var husk = new GameObject("TestRiggedHusk") { tag = "Husk" };
        husk.transform.position = pos;
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(husk.transform, false);
        body.transform.localPosition = Vector3.up;
        Object.DestroyImmediate(body.GetComponent<Collider>());
        var col = husk.AddComponent<CapsuleCollider>();
        col.height = 1.7f;
        col.radius = 0.35f;
        col.center = new Vector3(0f, 0.85f, 0f);
        husk.AddComponent<EnemyHealth>().SetTier(hp, LootTier.Normal);
        husk.AddComponent<HuskAnimator>();
        return husk;
    }

    [UnityTest]
    public IEnumerator Hit_FlashesChildRendererAndAnimates()
    {
        var husk = SpawnRiggedHusk(new Vector3(0f, 0f, 1.5f), 50);
        controller.SetAimPoint(new Vector3(0f, 0f, 10f));
        Assert.IsTrue(controller.Strike(), "Strike into the arc must hit.");
        yield return null;
        Assert.AreEqual(1, husk.GetComponent<EnemyHealth>().flashCount,
            "Flash must reach the child renderer (KayKit root has none).");
        Assert.AreEqual(1, husk.GetComponent<HuskAnimator>().hitCount, "Hit must play the Hit take.");
    }

    [UnityTest]
    public IEnumerator Kill_LeavesInertCorpseThatSinksAway()
    {
        var husk = SpawnRiggedHusk(new Vector3(0f, 0f, 1.5f), 1);
        controller.SetAimPoint(new Vector3(0f, 0f, 10f));
        Assert.IsTrue(controller.Strike());
        yield return null;
        Assert.IsTrue(husk == null, "Husk object must still be destroyed (kill contracts).");
        Assert.AreEqual(1, DiabloCorpse.Spawned);
        var corpse = Object.FindObjectOfType<DiabloCorpse>();
        Assert.IsNotNull(corpse, "Death must leave a corpse.");
        Assert.IsFalse(corpse.CompareTag("Husk"), "Corpse must not be targetable.");
        Assert.IsEmpty(corpse.GetComponentsInChildren<Collider>(), "Corpse must not block or trigger.");
        Assert.IsNull(corpse.GetComponent<EnemyHealth>());
        Assert.IsNull(corpse.GetComponent<HuskAnimator>());
        Assert.IsNull(DiabloMelee.FindArcTarget(player.transform, Vector3.forward, 3f),
            "Arc targeting must ignore corpses.");
        var go = corpse.gameObject;
        corpse.Advance(DiabloCorpse.LieSeconds + DiabloCorpse.SinkSeconds + 0.1f);
        yield return null;
        yield return null;
        Assert.IsTrue(go == null, "Corpse must clean itself up after sinking.");
    }

    [UnityTest]
    public IEnumerator Approach_WalksAnimatedAroundWall()
    {
        // Wall straight between husk (z=6) and hero (z=0).
        wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "TestWall";
        wall.transform.position = new Vector3(0f, 1f, 3.5f);
        wall.transform.localScale = new Vector3(2f, 2f, 0.4f);
        Physics.SyncTransforms();
        var husk = SpawnRiggedHusk(new Vector3(0f, 0f, 6f), 50);
        var attack = husk.AddComponent<EnemyAttack>();
        var anim = husk.GetComponent<HuskAnimator>();
        bool sawWalking = false;
        bool crossedWall = false;
        float end = Time.time + 6f;
        while (Time.time < end)
        {
            yield return null;
            sawWalking |= anim.moving;
            var p = husk.transform.position;
            if (p.z < 3.5f) { crossedWall = true; break; }
        }
        Assert.IsTrue(sawWalking, "Approach must play the Walk take.");
        Assert.IsTrue(crossedWall, "Husk must get past the wall.");
        Assert.Greater(attack.detours, 0, "Husk must have steered off the straight line.");
        Assert.Greater(Mathf.Abs(husk.transform.position.x), 0.7f,
            "Husk must pass beside the wall, not through it.");
    }

    [UnityTest]
    public IEnumerator Windup_PlaysAttackTake()
    {
        var husk = SpawnRiggedHusk(new Vector3(0f, 0f, 1.5f), 50);
        husk.AddComponent<EnemyAttack>();
        yield return DiabloTestFixture.WaitSeconds(0.2f);
        Assert.AreEqual(1, husk.GetComponent<HuskAnimator>().attackCount,
            "Wind-up must start the Attack take once.");
    }

    [UnityTest]
    public IEnumerator HitStop_FreezesBrieflyAndRestores()
    {
        SpawnRiggedHusk(new Vector3(0f, 0f, 1.5f), 50);
        controller.SetAimPoint(new Vector3(0f, 0f, 10f));
        Assert.IsTrue(controller.Strike());
        Assert.AreEqual(1, HitStop.Triggered, "Landed hit must hit-stop.");
        Assert.IsTrue(HitStop.IsActive);
        Assert.Less(Time.timeScale, 0.5f, "Time must be frozen during the stop.");
        float until = Time.realtimeSinceStartup + 0.5f;
        while (HitStop.IsActive && Time.realtimeSinceStartup < until) yield return null;
        Assert.IsFalse(HitStop.IsActive, "Stop must end within its realtime bound.");
        Assert.AreEqual(1f, Time.timeScale, "timeScale must be restored.");
    }
}
