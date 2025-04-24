using UnityEngine;

namespace CameraControl
{
    public class CameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float movementSpeed = 20f;
        [SerializeField] private float fastMovementMultiplier = 2f;
        [SerializeField] private float boundaryDistance = 100f; // Maximum distance from origin
    
        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 120f; // Degrees per second
        [SerializeField] private float minPitch = 10f; // Minimum angle on X axis (looking down)
        [SerializeField] private float maxPitch = 80f; // Maximum angle on X axis (looking down)
    
        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 15f;
        [SerializeField] private float minZoomDistance = 5f;
        [SerializeField] private float maxZoomDistance = 50f;
        [SerializeField] private float zoomDampening = 5f;
    
        [Header("Focus Settings")]
        [SerializeField] private LayerMask groundMask; // Layer mask for ground/grid
        [SerializeField] private float focusHeight = 2f; // Height to place camera focus point above ground
    
        // Internal variables
        private Transform cameraTransform;
        private float targetDistance; // Current target zoom distance
        private float currentDistance; // Current actual zoom distance
        private Vector3 focusPoint; // Point that camera rotates around
        private float currentPitch = 45f;
        private float currentYaw = 0f;
    
        private void Awake()
        {
            cameraTransform = transform;
        
            // Initialize with default values
            targetDistance = 20f;
            currentDistance = 20f;
            focusPoint = new Vector3(0, 0, 0);
        
            // Set initial rotation
            UpdateCameraTransform();
        }
    
        private void LateUpdate()
        {
            HandleMovement();
            HandleRotation();
            HandleZoom();
        
            UpdateCameraTransform();
        }
    
        private void HandleMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
        
            // Apply speed multiplier if shift key is pressed
            float speedMultiplier = Input.GetKey(KeyCode.LeftShift) ? fastMovementMultiplier : 1f;
        
            // Get movement direction relative to camera orientation
            Vector3 forward = new Vector3(cameraTransform.forward.x, 0, cameraTransform.forward.z).normalized;
            Vector3 right = new Vector3(cameraTransform.right.x, 0, cameraTransform.right.z).normalized;
        
            Vector3 moveDirection = (forward * vertical + right * horizontal).normalized;
        
            if (moveDirection != Vector3.zero)
            {
                // Move focus point
                Vector3 moveAmount = moveDirection * movementSpeed * speedMultiplier * Time.deltaTime;
                focusPoint += moveAmount;
            
                // Limit distance from origin
                if (focusPoint.magnitude > boundaryDistance)
                {
                    focusPoint = focusPoint.normalized * boundaryDistance;
                }
            }
        }
    
        private void HandleRotation()
        {
            // Middle mouse button to orbit
            if (Input.GetMouseButton(2) || (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftAlt)))
            {
                float yawDelta = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
                float pitchDelta = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
            
                currentYaw += yawDelta;
                currentPitch += pitchDelta;
            
                // Clamp pitch to avoid flipping
                currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            }
        
            // Q and E for yaw rotation
            if (Input.GetKey(KeyCode.Q))
            {
                currentYaw -= rotationSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.E))
            {
                currentYaw += rotationSpeed * Time.deltaTime;
            }
        }
    
        private void HandleZoom()
        {
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        
            if (scrollDelta != 0)
            {
                targetDistance -= scrollDelta * zoomSpeed;
                targetDistance = Mathf.Clamp(targetDistance, minZoomDistance, maxZoomDistance);
            }
        
            // Smoothly approach target distance
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * zoomDampening);
        }
    
        private void UpdateCameraTransform()
        {
            // Calculate rotation
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        
            // Calculate position based on focus point, rotation and distance
            Vector3 negativeDistance = new Vector3(0, 0, -currentDistance);
            Vector3 position = rotation * negativeDistance + focusPoint;
        
            // Update camera transform
            cameraTransform.rotation = rotation;
            cameraTransform.position = position;
        }
    
        /// <summary>
        /// Focus camera on specific world position
        /// </summary>
        public void FocusOn(Vector3 targetPosition)
        {
            focusPoint = targetPosition + new Vector3(0, focusHeight, 0);
        }
    
        /// <summary>
        /// Handle screen edge scrolling (useful for RTS games)
        /// </summary>
        private void HandleEdgeScrolling()
        {
            if (!Input.GetKey(KeyCode.LeftAlt))
            {
                float edgeSize = 20f;
                Vector3 moveDirection = Vector3.zero;
            
                if (Input.mousePosition.x < edgeSize)
                {
                    moveDirection += -cameraTransform.right;
                }
                else if (Input.mousePosition.x > Screen.width - edgeSize)
                {
                    moveDirection += cameraTransform.right;
                }
            
                if (Input.mousePosition.y < edgeSize)
                {
                    moveDirection += -Vector3.forward;
                }
                else if (Input.mousePosition.y > Screen.height - edgeSize)
                {
                    moveDirection += Vector3.forward;
                }
            
                if (moveDirection != Vector3.zero)
                {
                    moveDirection.Normalize();
                    focusPoint += moveDirection * movementSpeed * Time.deltaTime;
                }
            }
        }
    
        /// <summary>
        /// Handle click to focus on ground position
        /// </summary>
        private bool TryFocusOnClick()
        {
            if (Input.GetMouseButtonDown(1) && !Input.GetKey(KeyCode.LeftAlt))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
            
                if (Physics.Raycast(ray, out hit, 1000f, groundMask))
                {
                    FocusOn(hit.point);
                    return true;
                }
            }
            return false;
        }
    }
}