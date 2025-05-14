using UnityEngine;
using VikingRaven.Game.Tile_InGame;

namespace VikingRaven.Game.TileSystem
{
    /// <summary>
    /// Enhanced camera controller with tile-based movement and rotation
    /// </summary>
    public class EnhancedCameraController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 20f;
        [SerializeField] private float _rotateSpeed = 100f;
        [SerializeField] private float _zoomSpeed = 15f;
        [SerializeField] private float _tiltSpeed = 30f;
        
        [Header("Zoom Limits")]
        [SerializeField] private float _minZoom = 5f;
        [SerializeField] private float _maxZoom = 30f;
        
        [Header("Tilt Limits")]
        [SerializeField] private float _minTiltAngle = 20f;
        [SerializeField] private float _maxTiltAngle = 80f;
        
        [Header("Position Limits")]
        [SerializeField] private Vector2 _horizontalLimits = new Vector2(-50f, 50f);
        [SerializeField] private Vector2 _verticalLimits = new Vector2(-50f, 50f);
        
        [Header("Tile Focus")]
        [SerializeField] private float _focusSpeed = 5f;
        [SerializeField] private float _focusHeight = 15f;
        [SerializeField] private float _focusTilt = 60f;
        [SerializeField] private bool _smoothFocusTransition = true;
        
        [Header("Orbit Mode")]
        [SerializeField] private float _orbitSpeed = 30f;
        [SerializeField] private bool _isOrbiting = false;
        [SerializeField] private Vector3 _orbitTarget = Vector3.zero;
        [SerializeField] private float _orbitDistance = 15f;
        
        [Header("Edge Scrolling")]
        [SerializeField] private bool _enableEdgeScrolling = true;
        [SerializeField] private float _edgeScrollThreshold = 0.02f;
        [SerializeField] private float _edgeScrollSpeed = 15f;
        
        // Internal state
        private Transform _cameraTransform;
        private float _currentZoom;
        private float _currentTiltAngle;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private float _targetZoom;
        private bool _isTransitioning = false;
        private float _transitionProgress = 0f;
        private float _transitionDuration = 1.0f;
        
        // Reference to TileManager
        private TileManager _tileManager;
        
        private void Start()
        {
            InitializeCamera();
            
            // Get TileManager reference
            _tileManager = TileManager.Instance;
            if (_tileManager == null)
            {
                Debug.LogWarning("EnhancedCameraController: TileManager not found");
            }
        }
        
        /// <summary>
        /// Initialize camera settings
        /// </summary>
        private void InitializeCamera()
        {
            // Get main camera transform
            _cameraTransform = Camera.main.transform;
            
            // Initialize camera state
            _currentZoom = Vector3.Distance(transform.position, _cameraTransform.position);
            _targetZoom = _currentZoom;
            
            // Calculate current tilt
            Vector3 direction = (transform.position - _cameraTransform.position).normalized;
            _currentTiltAngle = Vector3.Angle(Vector3.up, direction);
            
            // Initialize target values
            _targetPosition = transform.position;
            _targetRotation = transform.rotation;
            
            Debug.Log("EnhancedCameraController: Initialized");
        }
        
        private void Update()
        {
            // Handle camera transition if active
            if (_isTransitioning)
            {
                UpdateCameraTransition();
            }
            else
            {
                // Normal camera controls
                if (_isOrbiting)
                {
                    UpdateOrbitMode();
                }
                else
                {
                    // Standard movement and controls
                    HandleKeyboardMovement();
                    HandleMouseRotation();
                    HandleZoom();
                    HandleTilt();
                    
                    // Edge scrolling
                    if (_enableEdgeScrolling)
                    {
                        HandleEdgeScrolling();
                    }
                }
            }
            
            // Always update camera transform based on current state
            UpdateCameraTransform();
        }
        
        /// <summary>
        /// Handle keyboard movement input
        /// </summary>
        private void HandleKeyboardMovement()
        {
            // Get input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            // Create movement vector relative to camera orientation
            Vector3 forward = transform.forward;
            forward.y = 0; // Keep movement in horizontal plane
            forward.Normalize();
            
            Vector3 right = transform.right;
            right.y = 0; // Keep movement in horizontal plane
            right.Normalize();
            
            // Calculate movement
            Vector3 movement = (forward * vertical + right * horizontal) * (_moveSpeed * Time.deltaTime);
            
            // Apply movement with limits
            Vector3 newPosition = transform.position + movement;
            
            // Clamp position within bounds
            newPosition.x = Mathf.Clamp(newPosition.x, _horizontalLimits.x, _horizontalLimits.y);
            newPosition.z = Mathf.Clamp(newPosition.z, _verticalLimits.x, _verticalLimits.y);
            
            // Apply position
            transform.position = newPosition;
        }
        
        /// <summary>
        /// Handle mouse rotation when right mouse button is held
        /// </summary>
        private void HandleMouseRotation()
        {
            // Rotate camera with right mouse button
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * _rotateSpeed * Time.deltaTime;
                transform.Rotate(Vector3.up, mouseX);
            }
        }
        
        /// <summary>
        /// Handle camera zoom with mouse wheel
        /// </summary>
        private void HandleZoom()
        {
            // Get scroll input
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            
            // Apply zoom
            _targetZoom -= scrollInput * _zoomSpeed;
            _targetZoom = Mathf.Clamp(_targetZoom, _minZoom, _maxZoom);
            
            // Smooth zoom transition
            _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, Time.deltaTime * 5f);
        }
        
        /// <summary>
        /// Handle camera tilt with keyboard keys (Q/E)
        /// </summary>
        private void HandleTilt()
        {
            // Tilt up with Q, down with E
            if (Input.GetKey(KeyCode.Q))
            {
                _currentTiltAngle -= _tiltSpeed * Time.deltaTime;
            }
            else if (Input.GetKey(KeyCode.E))
            {
                _currentTiltAngle += _tiltSpeed * Time.deltaTime;
            }
            
            // Clamp tilt angle
            _currentTiltAngle = Mathf.Clamp(_currentTiltAngle, _minTiltAngle, _maxTiltAngle);
        }
        
        /// <summary>
        /// Handle edge scrolling when mouse is near screen edges
        /// </summary>
        private void HandleEdgeScrolling()
        {
            // Get mouse position in screen space (0-1)
            Vector3 mousePos = Input.mousePosition;
            Vector2 mouseViewportPos = new Vector2(
                mousePos.x / Screen.width,
                mousePos.y / Screen.height
            );
            
            // Define edge threshold
            float threshold = _edgeScrollThreshold;
            
            // Create movement vector
            Vector3 moveDirection = Vector3.zero;
            
            // Check if mouse is near edges
            if (mouseViewportPos.x < threshold)
            {
                // Left edge
                moveDirection -= transform.right;
            }
            else if (mouseViewportPos.x > 1 - threshold)
            {
                // Right edge
                moveDirection += transform.right;
            }
            
            if (mouseViewportPos.y < threshold)
            {
                // Bottom edge
                moveDirection -= transform.forward;
            }
            else if (mouseViewportPos.y > 1 - threshold)
            {
                // Top edge
                moveDirection += transform.forward;
            }
            
            // Normalize and apply movement
            if (moveDirection.magnitude > 0.01f)
            {
                moveDirection.Normalize();
                moveDirection.y = 0; // Keep in horizontal plane
                
                Vector3 movement = moveDirection * (_edgeScrollSpeed * Time.deltaTime);
                Vector3 newPosition = transform.position + movement;
                
                // Apply position limits
                newPosition.x = Mathf.Clamp(newPosition.x, _horizontalLimits.x, _horizontalLimits.y);
                newPosition.z = Mathf.Clamp(newPosition.z, _verticalLimits.x, _verticalLimits.y);
                
                transform.position = newPosition;
            }
        }
        
        /// <summary>
        /// Update camera position around orbit target
        /// </summary>
        private void UpdateOrbitMode()
        {
            // Handle orbit rotation
            float mouseX = 0;
            float mouseY = 0;
            
            // Use mouse for rotation when holding right button
            if (Input.GetMouseButton(1))
            {
                mouseX = Input.GetAxis("Mouse X") * _orbitSpeed * Time.deltaTime;
                mouseY = Input.GetAxis("Mouse Y") * _orbitSpeed * 0.5f * Time.deltaTime;
                
                // Adjust tilt
                _currentTiltAngle += mouseY;
                _currentTiltAngle = Mathf.Clamp(_currentTiltAngle, _minTiltAngle, _maxTiltAngle);
                
                // Rotate pivot
                transform.RotateAround(_orbitTarget, Vector3.up, mouseX);
            }
            
            // Handle zoom in orbit mode
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            _orbitDistance -= scrollInput * _zoomSpeed;
            _orbitDistance = Mathf.Clamp(_orbitDistance, _minZoom, _maxZoom);
            
            // Set position based on orbit distance
            Vector3 direction = (transform.position - _orbitTarget).normalized;
            transform.position = _orbitTarget + direction * _orbitDistance;
        }
        
        /// <summary>
        /// Update camera transform based on all settings
        /// </summary>
        private void UpdateCameraTransform()
        {
            // Calculate camera position based on pivot, angle, and zoom
            Vector3 offset = -transform.forward;
            
            // Adjust for tilt
            Quaternion tiltRotation = Quaternion.Euler(_currentTiltAngle, 0, 0);
            offset = Quaternion.Euler(0, transform.eulerAngles.y, 0) * tiltRotation * Vector3.forward;
            
            // Apply zoom distance
            offset = offset.normalized * _currentZoom;
            
            // Set camera position and rotation
            _cameraTransform.position = transform.position + offset;
            _cameraTransform.LookAt(transform.position);
        }
        
        /// <summary>
        /// Update camera transition when moving to a new focus point
        /// </summary>
        private void UpdateCameraTransition()
        {
            // Update transition progress
            _transitionProgress += Time.deltaTime / _transitionDuration;
            
            if (_transitionProgress >= 1.0f)
            {
                // Transition complete
                _isTransitioning = false;
                _transitionProgress = 0f;
                
                // Set final values
                transform.position = _targetPosition;
                transform.rotation = _targetRotation;
                _currentZoom = _targetZoom;
            }
            else
            {
                // Smooth transition
                float t = _smoothFocusTransition ? Mathf.SmoothStep(0, 1, _transitionProgress) : _transitionProgress;
                
                // Interpolate position and rotation
                transform.position = Vector3.Lerp(transform.position, _targetPosition, t);
                transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, t);
                _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, t);
            }
        }
        
        /// <summary>
        /// Focus camera on a specific world position
        /// </summary>
        public void FocusOnPosition(Vector3 position, float duration = 1.0f)
        {
            _targetPosition = position;
            _targetRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0); // Keep current Y rotation
            _targetZoom = _focusHeight;
            _currentTiltAngle = _focusTilt;
            
            _isTransitioning = true;
            _transitionProgress = 0f;
            _transitionDuration = duration;
            
            Debug.Log($"Camera focusing on position {position}");
        }
        
        /// <summary>
        /// Focus on a specific tile
        /// </summary>
        public void FocusOnTile(int tileId, float duration = 1.0f)
        {
            if (_tileManager == null)
            {
                Debug.LogWarning("Cannot focus on tile: TileManager not found");
                return;
            }
            
            TileComponent targetTileComponent = _tileManager.GetTileById(tileId);
            
            if (targetTileComponent != null)
            {
                FocusOnPosition(targetTileComponent.CenterPosition, duration);
            }
            else
            {
                Debug.LogWarning($"Cannot focus on tile: Tile {tileId} not found");
            }
        }
        
        /// <summary>
        /// Focus on the tile containing a specific squad
        /// </summary>
        public void FocusOnSquad(int squadId, float duration = 1.0f)
        {
            if (_tileManager == null)
            {
                Debug.LogWarning("Cannot focus on squad: TileManager not found");
                return;
            }
            
            TileComponent squadTileComponent = _tileManager.GetTileBySquadId(squadId);
            
            if (squadTileComponent != null)
            {
                FocusOnPosition(squadTileComponent.CenterPosition, duration);
            }
            else
            {
                Debug.LogWarning($"Cannot focus on squad: Squad {squadId} not on any tile");
            }
        }
        
        /// <summary>
        /// Toggle orbit mode around a target position
        /// </summary>
        public void ToggleOrbitMode(Vector3 target, bool enableOrbit = true)
        {
            _isOrbiting = enableOrbit;
            
            if (_isOrbiting)
            {
                _orbitTarget = target;
                
                // Calculate initial orbit distance
                _orbitDistance = Vector3.Distance(transform.position, _orbitTarget);
                _orbitDistance = Mathf.Clamp(_orbitDistance, _minZoom, _maxZoom);
                
                Debug.Log($"Camera entering orbit mode around {target}");
            }
            else
            {
                Debug.Log("Camera exiting orbit mode");
            }
        }
        
        /// <summary>
        /// Toggle orbit mode around a specific tile
        /// </summary>
        public void ToggleOrbitAroundTile(int tileId, bool enableOrbit = true)
        {
            if (_tileManager == null)
            {
                Debug.LogWarning("Cannot orbit around tile: TileManager not found");
                return;
            }
            
            TileComponent targetTileComponent = _tileManager.GetTileById(tileId);
            
            if (targetTileComponent != null)
            {
                ToggleOrbitMode(targetTileComponent.CenterPosition, enableOrbit);
            }
            else
            {
                Debug.LogWarning($"Cannot orbit around tile: Tile {tileId} not found");
            }
        }
        
        /// <summary>
        /// Reset camera to default position and orientation
        /// </summary>
        public void ResetCamera()
        {
            _isOrbiting = false;
            _isTransitioning = true;
            _transitionProgress = 0f;
            _transitionDuration = 1.0f;
            
            // Default position
            _targetPosition = new Vector3(0, 10, -10);
            _targetRotation = Quaternion.Euler(0, 0, 0);
            _targetZoom = 15f;
            _currentTiltAngle = 45f;
            
            Debug.Log("Camera reset to default position");
        }
    }
}