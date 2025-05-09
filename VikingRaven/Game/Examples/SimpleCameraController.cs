using UnityEngine;

namespace VikingRaven.Game.Examples
{
     public class SimpleCameraController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 20f;
        [SerializeField] private float _rotateSpeed = 100f;
        [SerializeField] private float _zoomSpeed = 15f;
        
        [SerializeField] private float _minZoom = 5f;
        [SerializeField] private float _maxZoom = 30f;
        
        [SerializeField] private Vector2 _horizontalLimits = new Vector2(-50f, 50f);
        [SerializeField] private Vector2 _verticalLimits = new Vector2(-50f, 50f);
        
        private Transform _cameraTransform;
        private float _currentZoom;

        private void Start()
        {
            _cameraTransform = Camera.main.transform;
            _currentZoom = Vector3.Distance(transform.position, _cameraTransform.position);
        }

        private void Update()
        {
            // Handle keyboard movement input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            
            // Create movement vector relative to camera orientation
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();
            
            Vector3 right = transform.right;
            right.y = 0;
            right.Normalize();
            
            Vector3 movement = (forward * vertical + right * horizontal) * (_moveSpeed * Time.deltaTime);
            
            // Apply movement
            Vector3 newPosition = transform.position + movement;
            
            // Clamp position within limits
            newPosition.x = Mathf.Clamp(newPosition.x, _horizontalLimits.x, _horizontalLimits.y);
            newPosition.z = Mathf.Clamp(newPosition.z, _verticalLimits.x, _verticalLimits.y);
            
            transform.position = newPosition;
            
            // Handle rotation with mouse (if right mouse button is held)
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * _rotateSpeed * Time.deltaTime;
                transform.Rotate(Vector3.up, mouseX);
            }
            
            // Handle zoom with scroll wheel
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            _currentZoom -= scrollInput * _zoomSpeed;
            _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);
            
            // Apply zoom by adjusting camera position
            Vector3 cameraDirection = (_cameraTransform.position - transform.position).normalized;
            _cameraTransform.position = transform.position + cameraDirection * _currentZoom;
            
            // Always make camera look at the pivot point
            _cameraTransform.LookAt(transform);
        }
    }
}