using UnityEngine;
using UnityEngine.InputSystem; // REQUIRED: This namespace contains the new system

[RequireComponent(typeof(CharacterController))]
public class nonvrcontroller : MonoBehaviour
{
    // ========================================================================
    // GODOT vs UNITY: INPUT REFERENCES
    // ========================================================================
    // In Godot, you define Input Map strings in Project Settings.
    // In Unity, you create an .inputactions Asset, and we reference specific "Actions" here.
    // InputActionReference acts like a "link" to that specific setting.
    
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction; // Link to "Move"
    [SerializeField] private InputActionReference lookAction; // Link to "Look"
    [SerializeField] private InputActionReference jumpAction; // Link to "Jump"

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6.0f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Camera Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float lookSensitivity = 0.5f; // Note: New Input values are usually much higher in raw pixels, or normalized. We'll stick to 0.5.
    [SerializeField] private float lookXLimit = 85.0f;

    private CharacterController characterController;
    private Vector3 velocity;
    private float rotationX = 0;

    // ========================================================================
    // LIFECYCLE (Enabling Inputs)
    // ========================================================================
    // CRITICAL DIFFERENCE:
    // Godot inputs are always "on". 
    // Unity Input Actions must be ENABLED to be polled, and DISABLED when the object is off.
    
    private void OnEnable()
    {
        moveAction.action.Enable();
        lookAction.action.Enable();
        jumpAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        lookAction.action.Disable();
        jumpAction.action.Disable();
    }

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ========================================================================
    // UPDATE LOOP
    // ========================================================================
    void Update()
    {
        HandleRotation();
        HandleMovement();
    }

    void HandleMovement()
    {
        // 1. GROUND CHECK
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 2. READ INPUT (The New Way)
        // Godot: Input.get_vector(...)
        // Unity: action.ReadValue<Vector2>() returns a normalized Vector2 (x, y)
        Vector2 inputVector = moveAction.action.ReadValue<Vector2>();

        // 3. TRANSFORM INPUT TO WORLD DIRECTION
        // Use the Camera's/Player's facing direction
        Vector3 moveDirection = transform.right * inputVector.x + transform.forward * inputVector.y;

        // 4. JUMP
        // Godot: if Input.is_action_just_pressed("jump")
        // Unity: action.WasPressedThisFrame() OR action.triggered
        if (jumpAction.action.WasPressedThisFrame() && characterController.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 5. GRAVITY & APPLY
        velocity.y += gravity * Time.deltaTime;
        
        // Combine Move Speed + Vertical Gravity
        Vector3 finalMove = (moveDirection * moveSpeed) + new Vector3(0, velocity.y, 0);

        characterController.Move(finalMove * Time.deltaTime);
    }

    void HandleRotation()
    {
        // 1. READ MOUSE DELTA
        // The Input System returns the Delta (change) in pixels since last frame.
        Vector2 mouseDelta = lookAction.action.ReadValue<Vector2>();

        rotationX += -mouseDelta.y * lookSensitivity;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, mouseDelta.x * lookSensitivity, 0);
    }
}