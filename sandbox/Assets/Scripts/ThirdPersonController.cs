using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

/// <summary>
/// Minimal hand-rolled third-person controller for the mechanics testbed.
/// Chosen over Starter Assets (Asset Store only, no headless fetch): full
/// control, zero black box. Bindings are code-defined, no .inputactions file.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public sealed class ThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 7f;
    public float jumpHeight = 1.4f;
    public float gravity = -22f;

    [Header("Camera")]
    public float lookSensitivity = 0.25f;
    public float cameraDistance = 4.5f;
    public float cameraHeight = 1.8f;
    public float minPitch = -60f;
    public float maxPitch = 60f;

    CharacterController controller;
    float verticalVelocity;
    float yaw;
    float pitch = 12f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Multiplayer: only the owning client drives its player.
        // Solo scene Player has no NetworkObject -> TryGetComponent is false -> normal control.
        if (TryGetComponent<NetworkObject>(out var net) && net.IsSpawned && !net.IsOwner)
            return;

        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null) return;

        // Look (mouse; gamepad right stick could extend this later).
        if (mouse != null)
        {
            var delta = mouse.delta.ReadValue();
            yaw += delta.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, minPitch, maxPitch);
        }

        // Move relative to camera yaw.
        var input = new Vector2(
            (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) -
            (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f),
            (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) -
            (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f));
        if (input.sqrMagnitude > 1f) input.Normalize();

        float speed = (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)
            ? sprintSpeed : walkSpeed;
        var forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        var right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        var planar = (forward * input.y + right * input.x) * speed;

        // Gravity + jump.
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -2f;
            if (kb.spaceKey.wasPressedThisFrame)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        controller.Move((planar + Vector3.up * verticalVelocity) * Time.deltaTime);

        if (planar.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(planar), 12f * Time.deltaTime);

        UpdateCamera();
    }

    void UpdateCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var pivot = transform.position + Vector3.up * cameraHeight;
        var rot = Quaternion.Euler(pitch, yaw, 0f);
        cam.transform.position = pivot - rot * Vector3.forward * cameraDistance;
        cam.transform.rotation = rot;
    }
}
