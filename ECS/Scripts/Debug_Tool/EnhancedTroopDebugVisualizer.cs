// ECS/Scripts/Debug_Tool/EnhancedTroopDebugVisualizer.cs

using Components;
using Components.Squad;
using Components.Steering;
using Core.ECS;
using Movement;
using Squad;
using Steering;
using UnityEngine;

namespace Debug_Tool
{
    /// <summary>
    /// Enhanced tool to visualize debug information for troops in the scene
    /// </summary>
    public class EnhancedTroopDebugVisualizer : MonoBehaviour
    {
        private EntityBehaviour _entityBehaviour;
        private Entity _entity;
        
        [Header("Visualization Settings")]
        [SerializeField] private bool showVelocity = true;
        [SerializeField] private bool showTargetPosition = true;
        [SerializeField] private bool showSmoothedTargetPosition = true; // FIX: Thêm hiển thị smoothed target
        [SerializeField] private bool showDesiredPosition = true;
        [SerializeField] private bool showSteeringForce = true;
        [SerializeField] private bool showEntityId = true;
        [SerializeField] private bool showStateDiagnostics = true; // FIX: Thêm hiển thị thông tin chẩn đoán
        
        [Header("Line Settings")]
        [SerializeField] private Color velocityColor = Color.blue;
        [SerializeField] private Color targetPositionColor = Color.green;
        [SerializeField] private Color smoothedTargetColor = Color.cyan; // FIX: Màu cho smoothed target
        [SerializeField] private Color desiredPositionColor = Color.yellow;
        [SerializeField] private Color steeringForceColor = Color.red;
        [SerializeField] private float lineWidth = 0.05f;
        [SerializeField] private float velocityLineScale = 1.0f; // FIX: Scale cho velocity line
        [SerializeField] private float steeringForceScale = 0.5f; // FIX: Scale cho steering force line
        
        // References to components
        private PositionComponent _positionComponent;
        private VelocityComponent _velocityComponent;
        private SteeringDataComponent _steeringDataComponent;
        private SquadMemberComponent _squadMemberComponent;
        
        // Line renderers
        private LineRenderer _velocityLine;
        private LineRenderer _targetLine;
        private LineRenderer _smoothedTargetLine; // FIX: Line cho smoothed target
        private LineRenderer _desiredLine;
        private LineRenderer _steeringLine;
        
        // Text mesh for entity ID and diagnostics
        private TextMesh _idText;
        private TextMesh _diagnosticText; // FIX: Thêm text cho diagnostics
        
        // FIX: Target indicators
        private GameObject _targetIndicator;
        private GameObject _smoothedTargetIndicator;
        
        private void Start()
        {
            _entityBehaviour = GetComponent<EntityBehaviour>();
            if (_entityBehaviour != null)
            {
                _entity = _entityBehaviour.GetEntity();
                InitializeComponents();
                CreateVisualizers();
            }
            else
            {
                Debug.LogWarning("TroopDebugVisualizer attached to object without EntityBehaviour");
                enabled = false;
            }
        }
        
        private void InitializeComponents()
        {
            if (_entity == null) return;
            
            if (_entity.HasComponent<PositionComponent>())
            {
                _positionComponent = _entity.GetComponent<PositionComponent>();
            }
            
            if (_entity.HasComponent<VelocityComponent>())
            {
                _velocityComponent = _entity.GetComponent<VelocityComponent>();
            }
            
            if (_entity.HasComponent<SteeringDataComponent>())
            {
                _steeringDataComponent = _entity.GetComponent<SteeringDataComponent>();
            }
            
            if (_entity.HasComponent<SquadMemberComponent>())
            {
                _squadMemberComponent = _entity.GetComponent<SquadMemberComponent>();
            }
        }
        
        private void CreateVisualizers()
        {
            // Create velocity line
            if (showVelocity)
            {
                _velocityLine = CreateLineRenderer("VelocityLine", velocityColor);
            }
            
            // Create target position line
            if (showTargetPosition)
            {
                _targetLine = CreateLineRenderer("TargetLine", targetPositionColor);
                
                // FIX: Create target indicator (sphere)
                _targetIndicator = CreateSphereIndicator("TargetIndicator", targetPositionColor, 0.2f);
            }
            
            // FIX: Create smoothed target position line
            if (showSmoothedTargetPosition && _steeringDataComponent != null)
            {
                _smoothedTargetLine = CreateLineRenderer("SmoothedTargetLine", smoothedTargetColor);
                
                // Create smoothed target indicator (sphere)
                _smoothedTargetIndicator = CreateSphereIndicator("SmoothedTargetIndicator", smoothedTargetColor, 0.15f);
            }
            
            // Create desired position line
            if (showDesiredPosition)
            {
                _desiredLine = CreateLineRenderer("DesiredLine", desiredPositionColor);
            }
            
            // Create steering force line
            if (showSteeringForce)
            {
                _steeringLine = CreateLineRenderer("SteeringLine", steeringForceColor);
            }
            
            // Create entity ID text
            if (showEntityId)
            {
                GameObject textObj = new GameObject("EntityIdText");
                textObj.transform.parent = transform;
                textObj.transform.localPosition = new Vector3(0, 2.0f, 0); // Above the entity
                _idText = textObj.AddComponent<TextMesh>();
                _idText.text = $"ID: {_entity.Id}";
                _idText.alignment = TextAlignment.Center;
                _idText.anchor = TextAnchor.MiddleCenter;
                _idText.fontSize = 25;
                _idText.characterSize = 0.1f;
                _idText.color = Color.white;
            }
            
            // FIX: Create diagnostic text
            if (showStateDiagnostics)
            {
                GameObject diagObj = new GameObject("DiagnosticText");
                diagObj.transform.parent = transform;
                diagObj.transform.localPosition = new Vector3(0, 1.7f, 0); // Below the ID text
                _diagnosticText = diagObj.AddComponent<TextMesh>();
                _diagnosticText.text = "";
                _diagnosticText.alignment = TextAlignment.Center;
                _diagnosticText.anchor = TextAnchor.MiddleCenter;
                _diagnosticText.fontSize = 20;
                _diagnosticText.characterSize = 0.08f;
                _diagnosticText.color = Color.yellow;
            }
        }
        
        private LineRenderer CreateLineRenderer(string name, Color color)
        {
            GameObject lineObj = new GameObject(name);
            lineObj.transform.parent = transform;
            lineObj.transform.localPosition = Vector3.zero;
            
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
            line.positionCount = 2;
            
            return line;
        }
        
        // FIX: Create sphere indicator
        private GameObject CreateSphereIndicator(string name, Color color, float radius)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.parent = transform;
            sphere.transform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);
            
            // Set material and color
            Renderer renderer = sphere.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = color;
            
            // Remove collider
            Destroy(sphere.GetComponent<Collider>());
            
            // Hide initially
            sphere.SetActive(false);
            
            return sphere;
        }
        
        private void Update()
        {
            if (_entity == null) return;
            
            UpdateVisualizers();
        }
        
        private void UpdateVisualizers()
        {
            if (_positionComponent == null) return;
            
            Vector3 position = _positionComponent.Position;
            position.y = 0.5f; // Slight offset to make lines visible above ground
            
            // Update velocity line
            if (_velocityLine != null && _velocityComponent != null)
            {
                Vector3 velocity = _velocityComponent.Velocity;
                _velocityLine.SetPosition(0, position);
                _velocityLine.SetPosition(1, position + velocity * velocityLineScale);
            }
            
            // Update target position line
            if (_targetLine != null && _steeringDataComponent != null)
            {
                Vector3 targetPosition = _steeringDataComponent.TargetPosition;
                if (targetPosition != Vector3.zero)
                {
                    targetPosition.y = position.y; // Keep same height
                    _targetLine.SetPosition(0, position);
                    _targetLine.SetPosition(1, targetPosition);
                    
                    // FIX: Update target indicator
                    if (_targetIndicator != null)
                    {
                        _targetIndicator.SetActive(true);
                        _targetIndicator.transform.position = targetPosition;
                    }
                }
                else
                {
                    // Hide line if no target
                    _targetLine.SetPosition(0, position);
                    _targetLine.SetPosition(1, position);
                    
                    if (_targetIndicator != null)
                    {
                        _targetIndicator.SetActive(false);
                    }
                }
            }
            
            // FIX: Update smoothed target position line
            if (_smoothedTargetLine != null && _steeringDataComponent != null)
            {
                Vector3 smoothedTarget = _steeringDataComponent.SmoothedTargetPosition;
                if (smoothedTarget != Vector3.zero)
                {
                    smoothedTarget.y = position.y; // Keep same height
                    _smoothedTargetLine.SetPosition(0, position);
                    _smoothedTargetLine.SetPosition(1, smoothedTarget);
                    
                    // Update smoothed target indicator
                    if (_smoothedTargetIndicator != null)
                    {
                        _smoothedTargetIndicator.SetActive(true);
                        _smoothedTargetIndicator.transform.position = smoothedTarget;
                    }
                }
                else
                {
                    // Hide line if no target
                    _smoothedTargetLine.SetPosition(0, position);
                    _smoothedTargetLine.SetPosition(1, position);
                    
                    if (_smoothedTargetIndicator != null)
                    {
                        _smoothedTargetIndicator.SetActive(false);
                    }
                }
            }
            
            // Update desired position line
            if (_desiredLine != null && _squadMemberComponent != null)
            {
                Vector3 desiredPosition = _squadMemberComponent.DesiredPosition;
                if (desiredPosition != Vector3.zero)
                {
                    desiredPosition.y = position.y; // Keep same height
                    _desiredLine.SetPosition(0, position);
                    _desiredLine.SetPosition(1, desiredPosition);
                }
                else
                {
                    // Hide line if no desired position
                    _desiredLine.SetPosition(0, position);
                    _desiredLine.SetPosition(1, position);
                }
            }
            
            // Update steering force line
            if (_steeringLine != null && _steeringDataComponent != null)
            {
                Vector3 steeringForce = _steeringDataComponent.SteeringForce;
                _steeringLine.SetPosition(0, position);
                _steeringLine.SetPosition(1, position + steeringForce * steeringForceScale);
            }
            
            // Update text position
            if (_idText != null)
            {
                // Make text face camera
                if (Camera.main != null)
                {
                    _idText.transform.rotation = Camera.main.transform.rotation;
                }
                
                // Add state info
                string stateInfo = "";
                
                if (_squadMemberComponent != null)
                {
                    var squadState = FindSquadState(_squadMemberComponent.SquadEntityId);
                    if (squadState != null)
                    {
                        stateInfo = $"\nSquad: {squadState}";
                    }
                }
                
                // Add velocity info
                if (_velocityComponent != null)
                {
                    stateInfo += $"\nSpeed: {_velocityComponent.Velocity.magnitude:F2}";
                }
                
                _idText.text = $"ID: {_entity.Id}{stateInfo}";
            }
            
            // FIX: Update diagnostic text
            if (_diagnosticText != null && _steeringDataComponent != null)
            {
                string diagnosticInfo = "";
                
                // Show target change status
                diagnosticInfo += $"Target Changed: {(_steeringDataComponent.TargetPositionChanged ? "Yes" : "No")}";
                
                // Add arrival info
                if (_positionComponent != null)
                {
                    bool inSlowingZone = _steeringDataComponent.IsInSlowingZone(position);
                    float slowingFactor = _steeringDataComponent.GetSlowingFactor(position);
                    
                    diagnosticInfo += $"\nSlowing: {(inSlowingZone ? "Yes" : "No")} ({slowingFactor:F2})";
                }
                
                // Add steering force info
                diagnosticInfo += $"\nForce: {_steeringDataComponent.SteeringForce.magnitude:F2}";
                
                _diagnosticText.text = diagnosticInfo;
                
                // Make text face camera
                if (Camera.main != null)
                {
                    _diagnosticText.transform.rotation = Camera.main.transform.rotation;
                }
            }
        }
        
        private SquadState? FindSquadState(int squadId)
        {
            if (squadId < 0) return null;
            
            var world = _entityBehaviour.GetWorld();
            if (world == null) return null;
            
            foreach (var entity in world.GetEntitiesWith<SquadStateComponent>())
            {
                if (entity.Id == squadId)
                {
                    return entity.GetComponent<SquadStateComponent>().CurrentState;
                }
            }
            
            return null;
        }
        
        private void OnEnable()
        {
            // Show visualizers
            SetVisualizersActive(true);
        }
        
        private void OnDisable()
        {
            // Hide visualizers
            SetVisualizersActive(false);
        }
        
        private void SetVisualizersActive(bool active)
        {
            if (_velocityLine != null) _velocityLine.gameObject.SetActive(active);
            if (_targetLine != null) _targetLine.gameObject.SetActive(active);
            if (_smoothedTargetLine != null) _smoothedTargetLine.gameObject.SetActive(active);
            if (_desiredLine != null) _desiredLine.gameObject.SetActive(active);
            if (_steeringLine != null) _steeringLine.gameObject.SetActive(active);
            if (_idText != null) _idText.gameObject.SetActive(active);
            if (_diagnosticText != null) _diagnosticText.gameObject.SetActive(active);
            if (_targetIndicator != null) _targetIndicator.SetActive(active && _steeringDataComponent?.TargetPosition != Vector3.zero);
            if (_smoothedTargetIndicator != null) _smoothedTargetIndicator.SetActive(active && _steeringDataComponent?.SmoothedTargetPosition != Vector3.zero);
        }
    }
}