using UnityEngine;
using UnityEngine.EventSystems;

public class FirstPersonCharacter : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    public float mouseSensitivity = 2f; // Sensitivity for mouse look

    private Vector3 moveDirection;
    private CharacterController characterController;
    private Vector3 velocity;

    private float xRotation = 0f; // Store the camera's rotation

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Lock the cursor to the center of the screen for a first-person view
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Get input from the player
        float moveX = Input.GetAxis("Horizontal"); // Left/right movement
        float moveZ = Input.GetAxis("Vertical");   // Forward/backward movement

        // Create movement direction based on camera direction
        moveDirection = transform.right * moveX + transform.forward * moveZ;

        // Move the character
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        // Apply gravity to the character
        if (!characterController.isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else if (Input.GetButtonDown("Jump"))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply velocity for gravity and jumping
        characterController.Move(velocity * Time.deltaTime);

        // Camera Look Rotation
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate the camera vertically
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Clamp the vertical rotation

        GetComponentInChildren<Camera>().transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        // Rotate the player horizontally
        transform.Rotate(Vector3.up * mouseX);
    }
}
