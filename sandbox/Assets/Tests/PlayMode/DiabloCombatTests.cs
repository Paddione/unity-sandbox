using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Q5 combat logic (API-level gate, deterministic). The full synthetic
/// click path (virtual mouse + ScreenPointToRay) is best-effort and lives
/// outside this gate by plan decision.
/// </summary>
public class DiabloCombatTests
{
    GameObject floor;
    GameObject camGo;
    GameObject player;
    DiabloClickController controller;
    DiabloMelee melee;

    [SetUp]
    public void Setup()
    {
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();

        floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "TestFloor";

        camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>();

        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.AddComponent<CharacterController>();
        controller = player.AddComponent<DiabloClickController>();
        melee = player.AddComponent<DiabloMelee>();
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var orb in GameObject.FindObjectsOfType<ToyTrigger>())
            if (orb.gameObject.name.StartsWith("DiabloOrb_"))
                Object.DestroyImmediate(orb.gameObject);
        foreach (var husk in GameObject.FindGameObjectsWithTag("Husk"))
            Object.DestroyImmediate(husk);
        if (player != null) Object.DestroyImmediate(player);
        if (camGo != null) Object.DestroyImmediate(camGo);
        if (floor != null) Object.DestroyImmediate(floor);
        ToyTrigger.ResetRun();
        EnemyHealth.ResetRun();
        DiabloMelee.ResetRun();
    }

    static GameObject SpawnHusk(Vector3 pos)
    {
        var husk = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        husk.name = "TestHusk";
        husk.tag = "Husk";
        husk.transform.position = pos;
        husk.AddComponent<EnemyHealth>();
        return husk;
    }

    // (Wait helper lives in DiabloTestFixture; no local copy.)

    [UnityTest]
    public IEnumerator SetDestination_MovesPlayerTowardPoint()
    {
        for (int i = 0; i < 15; i++) yield return null;

        Vector3 start = player.transform.position;
        controller.SetDestination(new Vector3(0f, 1.05f, 4f));
        Assert.IsTrue(controller.HasDestination);

        yield return DiabloTestFixture.WaitSeconds(1.0f);

        Vector3 after = player.transform.position;
        Assert.Greater(after.z, start.z + 1f,
            $"Expected click-move toward +Z. Start={start}, After={after}");
    }

    [UnityTest]
    public IEnumerator AttackTarget_ApproachesAndKillsHusk()
    {
        for (int i = 0; i < 15; i++) yield return null;

        var husk = SpawnHusk(new Vector3(0f, 1f, 6f));
        controller.AttackTarget(husk);
        Assert.AreSame(husk, controller.CurrentTarget);

        yield return DiabloTestFixture.WaitSeconds(6.0f);

        Assert.IsTrue(husk == null,
            "Expected attack-move to kill the husk within 6s.");
        Assert.AreEqual(1, EnemyHealth.HusksKilled);
        Assert.AreEqual(1, EnemyHealth.OrbsDropped);
        Assert.AreEqual(1, GameObject.FindObjectsOfType<ToyTrigger>().Length,
            "Expected exactly one dropped pickup orb.");
    }

    [UnityTest]
    public IEnumerator Melee_OutOfRangeAndCooldown_AreRejected()
    {
        for (int i = 0; i < 15; i++) yield return null;

        var far = SpawnHusk(new Vector3(0f, 1f, 10f));
        Assert.IsFalse(melee.TryAttack(far, player.transform),
            "Out-of-range swing must be rejected.");
        Assert.AreEqual(0, DiabloMelee.Swings);

        var near = SpawnHusk(new Vector3(0f, 1f, 1f));
        Assert.IsTrue(melee.TryAttack(near, player.transform));
        Assert.IsFalse(melee.TryAttack(near, player.transform),
            "Second swing inside cooldown must be rejected.");
        Assert.AreEqual(1, DiabloMelee.Swings);
        yield return null;
    }
}
