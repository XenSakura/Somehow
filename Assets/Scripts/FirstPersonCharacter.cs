using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.InputSystem.XR.Haptics;

public class FirstPersonCharacter : MonoBehaviour
{
    public enum State
    {
        Normal,
        Slide
    }

    // ----- Movement Tuning -----
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float airControl = 0.5f;       // How much we can control in air
    public float gravity = -9.81f;
    public float jumpHeight = 2f;

    // ----- Sensitivity / Smoothness -----
    [Header("Rotation Settings")]
    public float mouseSensitivity = 0.12f; // Sensitivity for mouse look
    public float movementSmoothTime = 0.05f;

    // ----- Dashing (WIP) -----
    [Header("Dash Settings")]
    public float dashDistance = 20f;
    public int dashCharges = 2;
    public float dashRecharge = 1.0f;


    [Header("Slide Settings")]
    public float slideSpeed = 10.0f;
    public float slideDuration = 1.0f;
    public float slideHeight = 1.0f;
    public float slideCooldown = 1.0f;

    [Header("Attack Settings")]
    public List<GameObject> ProjectilePrefabs;
    public Transform spawnPoint;
    public float projectileSpeed = 10f;

    private State currentState = State.Normal;

    // Internal references
    private CharacterController characterController;
    private float xRotation = 0f;         // Camera's rotation around X (vertical)
    private float verticalVelocity = 0f;  // Stores only the vertical velocity
    private Vector3 smoothVelocity = Vector3.zero;

    private float originalHeight;
    private Vector3 originalCenter;

    private float slideTimer;
    private bool canSlide = true;

    private float dashTimer = 0;

    // Input Actions
    private InputAction MoveAction;
    private InputAction JumpAction;
    private InputAction DashAction;
    private InputAction LookAction;
    private InputAction SlideAction;
    private InputAction ShootAction;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        HandleInput();

        originalHeight = characterController.height;
        originalCenter = characterController.center;
    }

    void Update()
    {
        // 1. Handle Rotation
        HandleMouseLook();

        // 2. Process Jump (sets verticalVelocity if grounded & jump is pressed)
        HandleJump();

        // 3. Build and apply horizontal velocity
        PlayerMove();

        // 4. Apply gravity to vertical velocity
        HandleGravity();

        // 5. Move the CharacterController using horizontal + vertical
        Vector3 move = smoothVelocity;
        move.y = verticalVelocity;
        characterController.Move(move * Time.deltaTime);

        // 6. Handle Dash if desired (WIP)
        HandleDash();

        HandleSlide();

        HandleAttack();
    }

    // ----------------------------------------
    // Input Setup
    // ----------------------------------------
    private void HandleInput()
    {
        MoveAction = InputSystem.actions.FindAction("Move");
        JumpAction = InputSystem.actions.FindAction("Jump");
        DashAction = InputSystem.actions.FindAction("Dash");
        LookAction = InputSystem.actions.FindAction("Look");
        SlideAction = InputSystem.actions.FindAction("Slide");
        ShootAction = InputSystem.actions.FindAction("Attack");

        Cursor.lockState = CursorLockMode.Locked;
    }

    // ----------------------------------------
    // Movement (Horizontal) + Smoothing
    // ----------------------------------------
    private void PlayerMove()
    {
        // Get input from the player
        Vector2 moveVector = MoveAction.ReadValue<Vector2>();

        // Calculate target horizontal direction
        Vector3 targetHorizontalVelocity =
            (transform.right * moveVector.x + transform.forward * moveVector.y).normalized * moveSpeed;

        // If we're in the air, reduce our control
        if (!characterController.isGrounded)
        {
            targetHorizontalVelocity *= airControl;
        }

        if (currentState == State.Slide)
        {
            targetHorizontalVelocity = transform.forward * slideSpeed;
        }
        // Lerp from current horizontal velocity to the target velocity (smoother transitions)
        // We can get our current horizontal velocity by ignoring the vertical velocity from CC
        Vector3 currentHorizontalVelocity = characterController.velocity;
        currentHorizontalVelocity.y = 0f;

        smoothVelocity = Vector3.Lerp(currentHorizontalVelocity, targetHorizontalVelocity, movementSmoothTime);
    }

    // ----------------------------------------
    // Gravity
    // ----------------------------------------
    private void HandleGravity()
    {
        if (characterController.isGrounded)
        {
            // If grounded and falling, reset vertical velocity.
            // Small negative helps ensure isGrounded remains true (prevents "bouncing").
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
        }

        // Apply gravity acceleration
        verticalVelocity += gravity * Time.deltaTime;
    }

    // ----------------------------------------
    // Jump Logic
    // ----------------------------------------
    private void HandleJump()
    {
        if (JumpAction.triggered && characterController.isGrounded)
        {
            // v = sqrt(2gh)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    // ----------------------------------------
    // Mouse Look
    // ----------------------------------------
    private void HandleMouseLook()
    {
        float mouseX = LookAction.ReadValue<Vector2>().x * mouseSensitivity;
        float mouseY = LookAction.ReadValue<Vector2>().y * mouseSensitivity;

        // Vertical rotation
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply rotation
        GetComponentInChildren<Camera>().transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleSlide()
    {
        switch (currentState)
        {
            case State.Normal:
                //check if player can start sliding
                Debug.Log(SlideAction.ReadValue<float>());
                if (SlideAction.ReadValue<float>() > 0 && canSlide && characterController.isGrounded)
                {
                    float horizontalSpeed = new Vector2(characterController.velocity.x, characterController.velocity.z).magnitude;
                    if (horizontalSpeed > moveSpeed * 0.8f)
                    {
                        EnterSlide();
                    }
                }
                break;
            case State.Slide:
                slideTimer -= Time.deltaTime;
                if (slideTimer <= 0f || !characterController.isGrounded)
                {
                    ExitSlide();
                }
                break;
        }

    }

    private void EnterSlide()
    {
        currentState = State.Slide;
        slideTimer = slideDuration;
        canSlide = false;

        // Lower the collider
        characterController.height = slideHeight;
        characterController.center = new Vector3(originalCenter.x, slideHeight / 2f, originalCenter.z);

        

        // Start a cooldown timer to re-allow sliding
        Invoke(nameof(ResetSlide), slideCooldown);
    }

    private void ExitSlide()
    {
        currentState = State.Normal;

        // Restore the original collider
        characterController.height = originalHeight;
        characterController.center = originalCenter;

        // You could also restore velocity or let PlayerMove handle it
    }

    private void ResetSlide()
    {
        canSlide = true;
    }

    // ----------------------------------------
    // (Optional) Dash
    // ----------------------------------------
    private void HandleDash()
    {
        if (DashAction.ReadValue<float>() > 0 && dashCharges > 0)
        {
            // Example: Add a quick burst forward
            Vector3 dashVelocity = transform.forward * dashDistance;
            characterController.Move(dashVelocity * Time.deltaTime);

            dashCharges--;
        }

        if (dashCharges < 2)
        {
            dashTimer += Time.deltaTime;

        }
    }

    private void HandleAttack()
    {
        if (ShootAction.triggered)
        {
            GameObject projectile = Instantiate(ProjectilePrefabs[0], spawnPoint.position + transform.forward * 1.5f, spawnPoint.rotation);
            Rigidbody pjRb = projectile.GetComponent<Rigidbody>();
            pjRb.linearVelocity = spawnPoint.forward * projectileSpeed + characterController.velocity;
        }
    }
}
