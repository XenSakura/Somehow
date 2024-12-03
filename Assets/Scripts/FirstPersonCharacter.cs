using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class FirstPersonCharacter : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    public float mouseSensitivity = 0.06f; // Sensitivity for mouse look

    public int DashCharges = 2;
    public float DashRecharge = 1.0f;

    private Vector3 moveDirection;
    private CharacterController characterController;
    private Vector3 velocity;

    private float xRotation = 0f; // Store the camera's rotation

    InputAction MoveAction;
    InputAction JumpAction;
    InputAction DashAction;
    InputAction LookAction;
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        MoveAction = InputSystem.actions.FindAction("Move");
        JumpAction = InputSystem.actions.FindAction("Jump");
        DashAction = InputSystem.actions.FindAction("Dash");
        LookAction = InputSystem.actions.FindAction("Look");

        // Lock the cursor to the center of the screen for a first-person view
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Get input from the player
        Vector2 moveVector = MoveAction.ReadValue<Vector2>();

        // Create movement direction based on camera direction
        moveDirection = transform.right * moveVector.x + transform.forward * moveVector.y;

        // Move the character
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        // Apply gravity to the character
        if (!characterController.isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else if (JumpAction.ReadValue<float>() > 0)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        if (DashAction.ReadValue<float>() > 0 && DashCharges > 0)
        {
            characterController.Move(moveDirection * 2.0f * Time.deltaTime);
        }
        
        // Apply velocity for gravity and jumping
        characterController.Move(velocity * Time.deltaTime);

        // Camera Look Rotation
        float mouseX = LookAction.ReadValue<Vector2>().x * mouseSensitivity;
        float mouseY = LookAction.ReadValue<Vector2>().y * mouseSensitivity;

        // Rotate the camera vertically
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Clamp the vertical rotation

        GetComponentInChildren<Camera>().transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        // Rotate the player horizontally
        transform.Rotate(Vector3.up * mouseX);
    }
}
