using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement : MonoBehaviour
{
    public enum MovementMode { WASD, Click, Mobile }
    public MovementMode currentMode = MovementMode.WASD;

    [Header("Movement Settings")]
    public float speed = 5f;  // Movement speed of the player
    public float rotationSpeed = 720f;  // Speed at which the player rotates
    public LayerMask groundMask;  // Mask to identify the ground layer for raycasting
    public VariableJoystick moveJoystick; // Joystick for movement (mobile)
    public VariableJoystick lookJoystick; // Joystick for rotation (mobile)

    private PlayerInput playerInput;  // Player input system for handling input actions
    private InputAction moveAction;  // Input action for movement
    private Vector3 targetPosition;  // The target position the player moves towards
    private Camera mainCamera;  // Main camera for raycasting and movement direction

    private TrailRenderer trailRenderer;  // TrailRenderer for visual effects while moving
    private Vector3 previousPosition;  // The previous position to determine if the player has moved

    private void Awake()
    {
        // Get the necessary components at the start
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];  // Assign movement input action
        mainCamera = Camera.main;  // Get the main camera
        targetPosition = transform.position;  // Set initial target position to player's position

        trailRenderer = GetComponent<TrailRenderer>();  // Get the TrailRenderer component
        SetupTrailRenderer();  // Set up the TrailRenderer properties

        previousPosition = transform.position;  // Initialize the previous position
    }

    private void Update()
    {
        // Switch between movement modes using number keys
        HandleModeSwitching();

        // Handle player movement depending on the selected mode
        switch (currentMode)
        {
            case MovementMode.WASD:
                HandleWASDMovement();  // Handle movement using WASD keys
                break;

            case MovementMode.Click:
                HandlePointClickMovement();  // Handle movement by clicking on the ground
                break;

            case MovementMode.Mobile:
                HandleMobileMovement();  // Handle movement using mobile joysticks
                break;
        }

        MoveToTarget();  // Move the player to the target position (only for point-click movement)
    }

    // Switch between movement modes using digit keys (1, 2, 3)
    private void HandleModeSwitching()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            currentMode = MovementMode.WASD;

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            currentMode = MovementMode.Click;

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            currentMode = MovementMode.Mobile;
    }

    // Handle movement using WASD keys
    private void HandleWASDMovement()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();  // Get input from the move action
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);  // Convert 2D input to 3D movement

        if (move.sqrMagnitude > 0.01f)  // Check if the player is moving
        {
            transform.position += move.normalized * speed * Time.deltaTime;  // Move the player
            Quaternion targetRotation = Quaternion.LookRotation(move);  // Rotate the player in the direction of movement
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);  // Rotate the player smoothly
        }
    }

    // Handle movement by clicking on the ground
    private void HandlePointClickMovement()
    {
        // If the pointer is over a UI element, do not move the player
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))  // Check if the left mouse button is pressed
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);  // Create a ray from the mouse position

            // If the ray hits the ground, set the target position to the hit point
            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, groundMask))
            {
                targetPosition = hitInfo.point;
            }
        }
    }

    // Handle movement using mobile joysticks
    private void HandleMobileMovement()
    {
        Vector2 moveInput = moveJoystick.Direction;  // Get direction from the movement joystick
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);  // Convert to 3D movement

        if (move.sqrMagnitude > 0.01f)  // Check if there is movement
        {
            transform.position += move.normalized * speed * Time.deltaTime;  // Move the player
        }

        Vector2 lookInput = lookJoystick.Direction;  // Get direction from the look joystick
        if (lookInput.sqrMagnitude > 0.1f)  // Check if joystick input is significant
        {
            Vector3 lookDirection = new Vector3(lookInput.x, 0, lookInput.y);  // Convert to 3D direction
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);  // Create a rotation towards the look direction
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);  // Rotate the player smoothly
        }
    }

    // Move the player to the target position (used for point-click movement mode)
    private void MoveToTarget()
    {
        if (currentMode != MovementMode.Click)
            return;

        Vector3 direction = targetPosition - transform.position;  // Calculate direction to the target
        direction.y = 0;  // Ignore vertical movement

        if (direction.sqrMagnitude > 0.1f)  // If the player is not at the target position
        {
            transform.position += direction.normalized * speed * Time.deltaTime;  // Move the player
            Quaternion targetRotation = Quaternion.LookRotation(direction);  // Rotate the player towards the target
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);  // Smoothly rotate the player
        }
    }

    // Set up the TrailRenderer properties
    private void SetupTrailRenderer()
    {
        trailRenderer.time = 0.5f;  // Set how long the trail lasts
        trailRenderer.startWidth = 0.3f;  // Set the starting width of the trail
        trailRenderer.endWidth = 0f;  // Set the ending width of the trail
        trailRenderer.material = new Material(Shader.Find("Unlit/Color"));  // Set the trail material
        trailRenderer.material.color = Color.cyan;  // Set the trail color to cyan

        // Create a gradient for the trail color
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.cyan, 0.0f), new GradientColorKey(Color.blue, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        trailRenderer.colorGradient = gradient;  // Assign the gradient to the trail renderer
    }

    // Update the status of the TrailRenderer (enable it if moving, disable if not)
    private void UpdateTrailStatus()
    {
        float movementThreshold = 0.01f;  // Set a threshold for movement detection
        bool isMoving = Vector3.Distance(transform.position, previousPosition) > movementThreshold;  // Check if the player has moved significantly
        trailRenderer.emitting = isMoving;  // Enable or disable the trail based on movement

        previousPosition = transform.position;  // Update the previous position for the next frame
    }
}
