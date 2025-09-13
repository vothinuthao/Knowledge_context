using UnityEngine;
using UnityEngine.InputSystem;
using RavenDeckbuilding.Core;
using RavenDeckbuilding.Systems.Events;

namespace RavenDeckbuilding.Systems.Input
{
    /// <summary>
    /// High-frequency input processor that captures and processes input at 120Hz
    /// Provides input prediction for immediate visual feedback
    /// </summary>
    public class RealTimeInputProcessor : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float processFrequency = 120f; // Hz
        [SerializeField] private float inputHistoryDuration = 1f; // seconds
        [SerializeField] private float predictionThreshold = 0.016f; // 16ms prediction window
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogging = false;
        [SerializeField] private bool showPerformanceStats = false;

        // Core systems
        private InputRingBuffer _inputBuffer;
        private Camera _mainCamera;
        private float _processTimer;
        private float _nextProcessTime;
        
        // Input state tracking
        private Vector2 _lastMousePosition;
        private bool _isDragging;
        private int _draggedCardIndex = -1;
        private Vector2 _dragStartPosition;
        private float _dragStartTime;
        
        // Performance tracking
        private int _processedEventsThisFrame;
        private float _averageProcessTime;
        private int _frameCount;
        
        // Input prediction
        private Vector2 _predictedPosition;
        private Vector2 _lastVelocity;
        
        // Pre-allocated arrays for zero allocation processing
        private readonly InputEvent[] _tempEventArray = new InputEvent[16];

        public InputRingBuffer InputBuffer => _inputBuffer;
        public bool IsDragging => _isDragging;
        public Vector2 PredictedPosition => _predictedPosition;

        private void Awake()
        {
            _inputBuffer = new InputRingBuffer();
            _mainCamera = Camera.main;
            
            if (_mainCamera == null)
                _mainCamera = FindObjectOfType<Camera>();

            _processTimer = 1f / processFrequency;
            _nextProcessTime = Time.realtimeSinceStartup + _processTimer;
        }

        private void Start()
        {
            // Subscribe to input events if using Unity Input System
            if (Mouse.current != null)
            {
                _lastMousePosition = Mouse.current.position.ReadValue();
            }
        }

        private void Update()
        {
            // High-frequency processing independent of frame rate
            float currentTime = Time.realtimeSinceStartup;
            
            while (currentTime >= _nextProcessTime)
            {
                ProcessInputAtFrequency();
                _nextProcessTime += _processTimer;
            }
            
            // Cleanup old input events
            _inputBuffer.CleanupExpired(inputHistoryDuration);
            
            // Update performance statistics
            if (showPerformanceStats)
                UpdatePerformanceStats();
        }

        private void ProcessInputAtFrequency()
        {
            float startTime = Time.realtimeSinceStartup;
            _processedEventsThisFrame = 0;

            // Process mouse input
            if (Mouse.current != null)
            {
                ProcessMouseInput();
            }

            // Process keyboard input for card selection
            ProcessKeyboardInput();

            // Update input prediction
            UpdateInputPrediction();

            // Track processing performance
            float processTime = Time.realtimeSinceStartup - startTime;
            UpdateAverageProcessTime(processTime);
        }

        private void ProcessMouseInput()
        {
            Vector2 currentMousePos = Mouse.current.position.ReadValue();
            Vector2 mouseDelta = currentMousePos - _lastMousePosition;
            
            // Left click handling
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                ProcessMouseDown(currentMousePos);
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                ProcessMouseUp(currentMousePos);
            }
            else if (Mouse.current.leftButton.isPressed && mouseDelta.magnitude > 0.1f)
            {
                ProcessMouseDrag(currentMousePos, mouseDelta);
            }

            _lastMousePosition = currentMousePos;
        }

        private void ProcessMouseDown(Vector2 screenPos)
        {
            Vector3 worldPos = ScreenToWorldPoint(screenPos);
            int cardIndex = GetCardIndexAtPosition(worldPos);
            
            InputEvent inputEvent = InputEvent.Create(InputType.CardSelect, screenPos, cardIndex);
            _inputBuffer.TryAdd(inputEvent);
            
            // Fire prediction event for immediate feedback
            GameEventBus.FireCardPreviewEvent(cardIndex, worldPos, true);
            
            if (cardIndex >= 0)
            {
                _isDragging = true;
                _draggedCardIndex = cardIndex;
                _dragStartPosition = screenPos;
                _dragStartTime = Time.realtimeSinceStartup;
            }

            _processedEventsThisFrame++;
            
            if (enableDebugLogging)
                Debug.Log($"Mouse Down: Card {cardIndex} at {worldPos}");
        }

        private void ProcessMouseUp(Vector2 screenPos)
        {
            Vector3 worldPos = ScreenToWorldPoint(screenPos);
            
            if (_isDragging)
            {
                InputEvent dropEvent = InputEvent.Create(InputType.CardDrop, screenPos, _draggedCardIndex);
                _inputBuffer.TryAdd(dropEvent);
                
                // Fire prediction event
                GameEventBus.FireCardDropPrediction(_draggedCardIndex, worldPos);
                
                _isDragging = false;
                _draggedCardIndex = -1;
            }
            else
            {
                InputEvent selectEvent = InputEvent.Create(InputType.CardSelect, screenPos);
                _inputBuffer.TryAdd(selectEvent);
                
                GameEventBus.FireCardPreviewEvent(-1, worldPos, false);
            }

            _processedEventsThisFrame++;
        }

        private void ProcessMouseDrag(Vector2 screenPos, Vector2 delta)
        {
            if (!_isDragging) return;

            Vector3 worldPos = ScreenToWorldPoint(screenPos);
            
            InputEvent dragEvent = InputEvent.Create(InputType.CardDrag, screenPos, _draggedCardIndex);
            dragEvent.Direction = delta.normalized;
            _inputBuffer.TryAdd(dragEvent);
            
            // Fire immediate prediction for smooth dragging
            GameEventBus.FireCardDragPrediction(_draggedCardIndex, worldPos, delta);
            
            _processedEventsThisFrame++;
        }

        private void ProcessKeyboardInput()
        {
            if (Keyboard.current == null) return;

            // Card selection hotkeys (1-9)
            for (int i = 0; i < 9; i++)
            {
                Key key = (Key)((int)Key.Digit1 + i);
                if (Keyboard.current[key].wasPressedThisFrame)
                {
                    InputEvent keyEvent = InputEvent.Create(InputType.CardSelect, _lastMousePosition, i);
                    _inputBuffer.TryAdd(keyEvent);
                    
                    GameEventBus.FireCardHotkeyPressed(i);
                    _processedEventsThisFrame++;
                }
            }

            // Cancel action (Escape)
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                InputEvent cancelEvent = InputEvent.Create(InputType.Cancel, _lastMousePosition);
                _inputBuffer.TryAdd(cancelEvent);
                
                GameEventBus.FireCancelAction();
                _processedEventsThisFrame++;
            }
        }

        private void UpdateInputPrediction()
        {
            if (_isDragging && _draggedCardIndex >= 0)
            {
                Vector2 currentVelocity = (_lastMousePosition - _dragStartPosition) / 
                                        (Time.realtimeSinceStartup - _dragStartTime);
                
                // Simple linear prediction
                _predictedPosition = _lastMousePosition + currentVelocity * predictionThreshold;
                _lastVelocity = currentVelocity;
            }
            else
            {
                _predictedPosition = _lastMousePosition;
                _lastVelocity = Vector2.zero;
            }
        }

        private Vector3 ScreenToWorldPoint(Vector2 screenPos)
        {
            if (_mainCamera == null) return Vector3.zero;
            
            Vector3 screenPoint = new Vector3(screenPos.x, screenPos.y, _mainCamera.nearClipPlane);
            return _mainCamera.ScreenToWorldPoint(screenPoint);
        }

        private int GetCardIndexAtPosition(Vector3 worldPos)
        {
            // TODO: Implement raycast or collision detection to find card at position
            // This is a placeholder that would integrate with the card positioning system
            return -1;
        }

        private void UpdateAverageProcessTime(float processTime)
        {
            _frameCount++;
            float alpha = 1f / _frameCount;
            _averageProcessTime = Mathf.Lerp(_averageProcessTime, processTime, alpha);
        }

        private void UpdatePerformanceStats()
        {
            if (_frameCount % 60 == 0) // Log every 60 frames
            {
                float bufferUtilization = _inputBuffer.GetUtilization();
                Debug.Log($"Input Performance - Avg Process Time: {_averageProcessTime * 1000:F2}ms, " +
                         $"Buffer Utilization: {bufferUtilization * 100:F1}%, " +
                         $"Events/Frame: {_processedEventsThisFrame}");
            }
        }

        // Public API for external systems
        public bool TryGetRecentInput(InputEvent[] output, int maxCount, float timeWindow = 0.1f)
        {
            int count = _inputBuffer.GetRecentByTime(output, maxCount, timeWindow);
            return count > 0;
        }

        public bool HasPendingInput(InputType inputType, float timeWindow = 0.1f)
        {
            int count = _inputBuffer.GetRecentByTime(_tempEventArray, _tempEventArray.Length, timeWindow);
            
            for (int i = 0; i < count; i++)
            {
                if (_tempEventArray[i].InputType == inputType)
                    return true;
            }
            
            return false;
        }

        private void OnDestroy()
        {
            // Cleanup if needed
        }

        // Debug visualization
        private void OnGUI()
        {
            if (!showPerformanceStats) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Label($"Input Buffer: {_inputBuffer.Count}/{_inputBuffer.Capacity}");
            GUILayout.Label($"Utilization: {_inputBuffer.GetUtilization() * 100:F1}%");
            GUILayout.Label($"Avg Process Time: {_averageProcessTime * 1000:F2}ms");
            GUILayout.Label($"Predicted Pos: {_predictedPosition}");
            GUILayout.Label($"Is Dragging: {_isDragging} (Card {_draggedCardIndex})");
            GUILayout.EndArea();
        }
    }
}