using UnityEngine;
using UnityEngine.InputSystem; // Add this!

[RequireComponent(typeof(CharacterController))]
public class DebugFPSController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;
    
    [Header("Look Settings")]
    public float mouseSensitivity = 0.000001f; // New system uses pixels/delta, lower this value!
    public Transform cameraTransform;

    private CharacterController _controller;
    private float _xRotation = 0f;
    private Vector3 _velocity;
    public GameObject xrOrigin; // Assign your XR Rig here in the Inspector
    public GameObject debugPlayer; // Assign this Debug Player prefab in the Inspector
    // Quick pseudo-code for a toggle
    void Awake() {
    bool isVR = UnityEngine.XR.XRSettings.enabled; 
    xrOrigin.SetActive(isVR);
    debugPlayer.SetActive(!isVR);
    }
    void Start()
    {
        _controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>().transform;
    }

    void Update()
    {
        // 1. Mouse Look (New Input System style)
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        
        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime; // Scale by deltaTime for frame rate independence
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        // 2. Movement (New Input System style)
        Vector2 moveInput = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) moveInput.y = 1;
        if (Keyboard.current.sKey.isPressed) moveInput.y = -1;
        if (Keyboard.current.aKey.isPressed) moveInput.x = -1;
        if (Keyboard.current.dKey.isPressed) moveInput.x = 1;

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        _controller.Move(move * moveSpeed * Time.deltaTime);

        // 3. Simple Gravity
        if (_controller.isGrounded && _velocity.y < 0)
            _velocity.y = -2f;

        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);

        // 4. Emergency Exit
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            Cursor.lockState = CursorLockMode.None;
    }
}