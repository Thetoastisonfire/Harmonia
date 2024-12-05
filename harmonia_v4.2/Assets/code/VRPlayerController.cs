using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float movementSpeed = 3.0f;
    public OVRCameraRig cameraRig;
    public float rotationSmoothSpeed = 5.0f;
    public float rotationSpeed = 45.0f;
    public LayerMask collisionLayer; // Add this to specify which layers to check for collisions
    public float collisionCheckDistance = 0.5f; // Distance to check for collisions

    private Rigidbody rb;
    private Transform cameraTransform;
    private CapsuleCollider playerCollider; // Add this to reference the player's collider

    void Start()
    {
        // Cache the Rigidbody component and ensure it exists
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("Rigidbody missing on VRPlayerController object. Please add one.");
        }
        else
        {
            rb.isKinematic = false; // Change to false to enable collision detection
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY; // Freeze Y position and rotation
        }

        // Get the player's collider
        playerCollider = GetComponent<CapsuleCollider>();
        if (playerCollider == null)
        {
            Debug.LogWarning("CapsuleCollider missing on VRPlayerController object. Please add one.");
        }

        // Cache the camera transform
        if (cameraRig != null)
        {
            cameraTransform = cameraRig.centerEyeAnchor;
        }
        else
        {
            Debug.LogError("OVRCameraRig not assigned to VRPlayerController!");
        }
    }

    void Update()
    {
        HandleMovement();
        HandleCameraRotation();
    }

    private bool CanMoveInDirection(Vector3 moveDirection)
    {
        if (playerCollider == null) return true;

        // Calculate the position to check for collisions
        Vector3 origin = transform.position + Vector3.up * (playerCollider.height * 0.5f);

        // Cast a ray in the movement direction to check for obstacles
        RaycastHit hit;
        bool hasHit = Physics.CapsuleCast(
            origin,
            origin + Vector3.up * (playerCollider.height * 0.5f),
            playerCollider.radius,
            moveDirection,
            out hit,
            collisionCheckDistance,
            collisionLayer
        );

        return !hasHit;
    }

    private void HandleMovement()
    {
        // Get controller input from the left thumbstick
        Vector2 leftStickInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        // Get the camera's forward and right vectors, but ignore vertical component
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        // Project vectors onto the horizontal plane
        cameraForward.y = 0;
        cameraRight.y = 0;

        // Normalize the vectors
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Calculate movement direction relative to camera orientation
        Vector3 moveDirection = (cameraForward * leftStickInput.y + cameraRight * leftStickInput.x).normalized;

        // Apply movement only if there's no collision
        if (moveDirection.magnitude > 0.1f) // Small deadzone
        {
            if (CanMoveInDirection(moveDirection))
            {
                Vector3 targetPosition = rb.position + moveDirection * movementSpeed * Time.deltaTime;
                rb.MovePosition(targetPosition);
            }
        }
    }

    private void HandleCameraRotation()
    {
        // Get controller input from the right thumbstick for camera rotation
        Vector2 rightStickInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        // Apply rotation based on horizontal input from the right thumbstick
        if (Mathf.Abs(rightStickInput.x) > 0.1f) // Adding deadzone for stability
        {
            float rotationAmount = rightStickInput.x * rotationSpeed * Time.deltaTime;
            Quaternion rotation = Quaternion.Euler(0, rotationAmount, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, transform.rotation * rotation, Time.deltaTime * rotationSmoothSpeed);
        }
    }
}