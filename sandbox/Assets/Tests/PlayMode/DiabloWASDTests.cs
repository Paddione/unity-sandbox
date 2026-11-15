using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

/// <summary>WASD/arrows drive the hero camera-relative (synthetic keys).</summary>
public class DiabloWASDTests : InputTestFixture
{
    Keyboard keyboard;
    GameObject player;
    DiabloClickController controller;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        keyboard = InputSystem.AddDevice<Keyboard>();
        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.AddComponent<CharacterController>();
        controller = player.AddComponent<DiabloClickController>();
    }

    [TearDown]
    public override void TearDown()
    {
        if (player != null) Object.DestroyImmediate(player);
        base.TearDown();
    }

    [UnityTest]
    public IEnumerator WASD_MovesCameraRelative()
    {
        for (int i = 0; i < 5; i++) yield return null;
        Vector3 start = player.transform.position;

        Press(keyboard.wKey);
        float moveTime = 0f;
        while (moveTime < 0.3f)
        {
            yield return null;
            moveTime += Time.deltaTime;
        }
        Release(keyboard.wKey);

        // Default iso yaw 45°: "forward" is diagonal (sin45, 0, cos45).
        Assert.Greater(player.transform.position.z, start.z + 0.3f,
            $"W must move the hero forward. Start={start}, After={player.transform.position}");
        Assert.Greater(player.transform.position.x, start.x + 0.3f,
            $"W must strafe with the iso yaw. Start={start}, After={player.transform.position}");
        Assert.IsTrue(DiabloAnimator.IsMoving(controller),
            "Animator must see the hero as moving while WASD is held.");
    }
}
