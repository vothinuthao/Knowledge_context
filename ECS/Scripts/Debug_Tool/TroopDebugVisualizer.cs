using System.Collections.Generic;
using Core.ECS;
using Movement;
using Steering;
using UnityEngine;

namespace Debug_Tool
{
    /// <summary>
    /// Component for visualizing troop debug information using gizmos
    /// </summary>
    [ExecuteInEditMode]
    public class TroopDebugVisualizer : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showVelocity = true;
        [SerializeField] private bool showTargetPosition = true;
        [SerializeField] private bool showAvoidPosition = true;
        [SerializeField] private bool showSteeringForces = true;
        [SerializeField] private bool showNearbyEntities = true;
        [SerializeField] private bool showSquadInfo = true;
    
        [Header("Gizmo Settings")]
        [SerializeField] private float velocityScale = 1.0f;
        [SerializeField] private float forceScale = 2.0f;
        [SerializeField] private Color velocityColor = Color.blue;
        [SerializeField] private Color targetPositionColor = Color.green;
        [SerializeField] private Color avoidPositionColor = Color.red;
        [SerializeField] private Color steeringForceColor = Color.magenta;
        [SerializeField] private Color allyColor = Color.green;
        [SerializeField] private Color enemyColor = Color.red;
    
        // Reference to the entity
        private EntityBehaviour _entityBehaviour;
    
        // Components for debug
        private SteeringDataComponent _steeringData;
        private PositionComponent _position;
        private VelocityComponent _velocity;
    
        // Cache for steering forces
        private Dictionary<string, Vector3> _debugSteeringForces = new Dictionary<string, Vector3>();
    
        private void Awake()
        {
            _entityBehaviour = GetComponent<EntityBehaviour>();
            if (_entityBehaviour == null)
            {
                Debug.LogWarning($"TroopDebugVisualizer on {gameObject.name} cannot find EntityBehaviour component!");
            }
        }
    
        private void Start()
        {
            // Enable debug info by default in development builds
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            showDebugInfo = true;
#endif
        
            UpdateReferences();
        }
    
        private void OnEnable()
        {
            UpdateReferences();
        }
    
        private void Update()
        {
            if (_entityBehaviour == null || !showDebugInfo || Application.isPlaying == false)
                return;
            
            UpdateReferences();
            CaptureSteeringForces();
        }
    
        /// <summary>
        /// Updates references to entity components
        /// </summary>
        private void UpdateReferences()
        {
            if (_entityBehaviour == null)
                return;
            
            Entity entity = _entityBehaviour.GetEntity();
            if (entity == null)
                return;
            
            if (entity.HasComponent<SteeringDataComponent>())
                _steeringData = entity.GetComponent<SteeringDataComponent>();
            
            if (entity.HasComponent<PositionComponent>())
                _position = entity.GetComponent<PositionComponent>();
            
            if (entity.HasComponent<VelocityComponent>())
                _velocity = entity.GetComponent<VelocityComponent>();
        }
    
        /// <summary>
        /// Captures steering forces for visualization.
        /// </summary>
        private void CaptureSteeringForces()
        {
            if (_entityBehaviour == null)
                return;
            
            Entity entity = _entityBehaviour.GetEntity();
            if (entity == null || _steeringData == null)
                return;
            
            // Capture total steering force
            _debugSteeringForces["TotalForce"] = _steeringData.SteeringForce;
        
            // Individual components (in a real system, you might inject these from the steering systems)
            if (entity.HasComponent<SeekComponent>() && entity.GetComponent<SeekComponent>().IsEnabled)
            {
                // This is a simplified approximation - in reality, we would need to hook into the actual steering systems
                Vector3 seekForce = Vector3.zero;
                if (_steeringData.TargetPosition != Vector3.zero && _position != null)
                {
                    seekForce = (_steeringData.TargetPosition - _position.Position).normalized * 
                                entity.GetComponent<SeekComponent>().Weight;
                }
                _debugSteeringForces["Seek"] = seekForce;
            }
        
            if (entity.HasComponent<SeparationComponent>() && entity.GetComponent<SeparationComponent>().IsEnabled)
            {
                // Simplified for visualization - not the actual calculated force
                Vector3 separationForce = Vector3.zero;
                if (_steeringData.NearbyAlliesIds.Count > 0)
                {
                    // Approximate force based on nearby entities
                    separationForce = new Vector3(
                        Random.Range(-0.5f, 0.5f), 
                        0, 
                        Random.Range(-0.5f, 0.5f)
                    ).normalized * entity.GetComponent<SeparationComponent>().Weight;
                }
                _debugSteeringForces["Separation"] = separationForce;
            }
        }
    
        /// <summary>
        /// Draws debug information in scene view
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showDebugInfo || _entityBehaviour == null)
                return;
            
            Vector3 position = transform.position;
        
            // Draw velocity vector
            if (showVelocity && _velocity != null)
            {
                Gizmos.color = velocityColor;
                Vector3 velocityVector = _velocity.Velocity * velocityScale;
                Gizmos.DrawLine(position, position + velocityVector);
                DrawArrowhead(position + velocityVector, velocityVector.normalized, 0.2f, velocityColor);
            }
        
            // Draw target position
            if (showTargetPosition && _steeringData != null && _steeringData.TargetPosition != Vector3.zero)
            {
                Gizmos.color = targetPositionColor;
                Gizmos.DrawWireSphere(_steeringData.TargetPosition, 0.3f);
                Gizmos.DrawLine(position, _steeringData.TargetPosition);
            }
        
            // Draw avoid position
            if (showAvoidPosition && _steeringData != null && _steeringData.AvoidPosition != Vector3.zero)
            {
                Gizmos.color = avoidPositionColor;
                Gizmos.DrawWireSphere(_steeringData.AvoidPosition, 0.3f);
                Gizmos.DrawLine(position, _steeringData.AvoidPosition);
            }
        
            // Draw steering forces
            if (showSteeringForces && _debugSteeringForces.Count > 0)
            {
                float offset = 0.1f;
            
                foreach (var force in _debugSteeringForces)
                {
                    if (force.Value.magnitude < 0.01f)
                        continue;
                    
                    Gizmos.color = steeringForceColor;
                    Vector3 scaledForce = force.Value * forceScale;
                    Vector3 offsetPosition = position + new Vector3(0, offset, 0);
                    Gizmos.DrawLine(offsetPosition, offsetPosition + scaledForce);
                    DrawArrowhead(offsetPosition + scaledForce, scaledForce.normalized, 0.1f, steeringForceColor);
                    offset += 0.1f;
                }
            }
        
            // Draw nearby entities connections
            if (showNearbyEntities && _steeringData != null)
            {
                // Nearby allies
                Gizmos.color = allyColor;
                foreach (var allyId in _steeringData.NearbyAlliesIds)
                {
                    // Find entity in scene
                    EntityBehaviour[] entities = FindObjectsOfType<EntityBehaviour>();
                    foreach (var entity in entities)
                    {
                        if (entity.GetEntity()?.Id == allyId)
                        {
                            Gizmos.DrawLine(position, entity.transform.position);
                            break;
                        }
                    }
                }
            
                // Nearby enemies
                Gizmos.color = enemyColor;
                foreach (var enemyId in _steeringData.NearbyEnemiesIds)
                {
                    // Find entity in scene
                    EntityBehaviour[] entities = FindObjectsOfType<EntityBehaviour>();
                    foreach (var entity in entities)
                    {
                        if (entity.GetEntity()?.Id == enemyId)
                        {
                            Gizmos.DrawLine(position, entity.transform.position);
                            break;
                        }
                    }
                }
            }
        
            // Draw squad info
            if (showSquadInfo && _entityBehaviour.GetEntity() != null && _entityBehaviour.GetEntity().HasComponent<Squad.SquadMemberComponent>())
            {
                var squadMember = _entityBehaviour.GetEntity().GetComponent<Squad.SquadMemberComponent>();
                if (squadMember.DesiredPosition != Vector3.zero)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(squadMember.DesiredPosition, 0.2f);
                    Gizmos.DrawLine(position, squadMember.DesiredPosition);
                }
            }
        }
    
        /// <summary>
        /// Draws debug information as GUI overlay
        /// </summary>
        private void OnGUI()
        {
            if (!showDebugInfo || _entityBehaviour == null || Camera.main == null)
                return;
            
            Entity entity = _entityBehaviour.GetEntity();
            if (entity == null)
                return;
            
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            screenPos.y = Screen.height - screenPos.y;
        
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 10;
            style.alignment = TextAnchor.UpperLeft;
            style.normal.background = MakeTexture(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.7f));
        
            string info = $"ID: {entity.Id}\n";
        
            if (_velocity != null)
                info += $"Speed: {_velocity.Velocity.magnitude:F2}\n";
            
            if (_steeringData != null)
            {
                info += $"Force: {_steeringData.SteeringForce.magnitude:F2}\n";
                info += $"Allies: {_steeringData.NearbyAlliesIds.Count}\n";
                info += $"Enemies: {_steeringData.NearbyEnemiesIds.Count}\n";
            
                // List active behaviors
                info += "Behaviors:\n";
            
                // Seek
                if (entity.HasComponent<SeekComponent>())
                {
                    var seek = entity.GetComponent<SeekComponent>();
                    if (seek.IsEnabled)
                        info += $"- Seek (w: {seek.Weight:F1})\n";
                }
            
                // Separation
                if (entity.HasComponent<SeparationComponent>())
                {
                    var separation = entity.GetComponent<SeparationComponent>();
                    if (separation.IsEnabled)
                        info += $"- Separat. (w: {separation.Weight:F1})\n";
                }
            
                // Squad Member info
                if (entity.HasComponent<Squad.SquadMemberComponent>())
                {
                    var member = entity.GetComponent<Squad.SquadMemberComponent>();
                    info += $"Squad: {member.SquadEntityId}\n";
                    info += $"Grid: {member.GridPosition}\n";
                }
            }
        
            GUI.Box(new Rect(screenPos.x - 50, screenPos.y, 100, 200), "");
            GUI.Label(new Rect(screenPos.x - 45, screenPos.y + 5, 90, 190), info, style);
        }
    
        /// <summary>
        /// Draws an arrowhead for gizmo lines
        /// </summary>
        private void DrawArrowhead(Vector3 position, Vector3 direction, float size, Color color)
        {
            Vector3 right = Quaternion.Euler(0, 30, 0) * -direction * size;
            Vector3 left = Quaternion.Euler(0, -30, 0) * -direction * size;
        
            Gizmos.color = color;
            Gizmos.DrawRay(position, right);
            Gizmos.DrawRay(position, left);
        }
    
        /// <summary>
        /// Creates a texture for GUI backgrounds
        /// </summary>
        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
        
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    
        /// <summary>
        /// Force enable debug visualization
        /// </summary>
        public void EnableDebugVisualization()
        {
            showDebugInfo = true;
            showVelocity = true;
            showTargetPosition = true;
            showSteeringForces = true;
            showNearbyEntities = true;
            showSquadInfo = true;
        }
    }
}