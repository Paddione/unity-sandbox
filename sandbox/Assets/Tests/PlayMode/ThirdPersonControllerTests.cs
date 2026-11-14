using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

public class ThirdPersonControllerTests : InputTestFixture
{
    Keyboard keyboard;
    Mouse mouse;
    GameObject floor;
    GameObject camGo;
    GameObject player;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        keyboard = InputSystem.AddDevice<Keyboard>();
        mouse = InputSystem.AddDevice<Mouse>();

        floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "TestFloor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(5f, 1f, 5f);

        camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>();

        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1.05f, 0f);
        player.AddComponent<CharacterController>();
        player.AddComponent<ThirdPersonController>();
    }

    [TearDown]
    public override void TearDown()
    {
        if (player != null) Object.DestroyImmediate(player);
        if (camGo != null) Object.DestroyImmediate(camGo);
        if (floor != null) Object.DestroyImmediate(floor);
        base.TearDown();
    }

    [UnityTest]
    public IEnumerator MoveForwardAndJump_ChangesPlayerPosition()
    {
        // Settle onto the floor so CharacterController.isGrounded becomes true.
        for (int i = 0; i < 15; i++)
            yield return null;

        Vector3 startPos = player.transform.position;

        Press(keyboard.wKey);
        float moveTime = 0f;
        while (moveTime < 0.25f)
        {
            yield return null;
            moveTime += Time.deltaTime;
        }
        Release(keyboard.wKey);

        Vector3 afterMovePos = player.transform.position;
        Assert.Greater(afterMovePos.z, startPos.z + 0.1f,
            $"Expected W key input to move player forward (+Z). Start={startPos}, After={afterMovePos}");

        // Trigger jump on grounded state.
        Press(keyboard.spaceKey);
        yield return null;
        Release(keyboard.spaceKey);

        float maxY = afterMovePos.y;
        float jumpTime = 0f;
        while (jumpTime < 0.2f)
        {
            yield return null;
            jumpTime += Time.deltaTime;
            if (player.transform.position.y > maxY)
                maxY = player.transform.position.y;
        }

        Assert.Greater(maxY, afterMovePos.y + 0.05f,
            $"Expected Space key input to jump (+Y). BeforeJumpY={afterMovePos.y}, MaxY={maxY}");
    }
}
