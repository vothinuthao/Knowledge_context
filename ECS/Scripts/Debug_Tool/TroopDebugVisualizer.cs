using UnityEngine;
using Core.ECS;
using Movement;
using Steering;
using System.Collections.Generic;

/// <summary>
/// Component để hiển thị debug gizmos cho troop (hướng di chuyển, các lực tác động, v.v.)
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
    /// Capture steering forces from different components for visualization.
    /// In a real implementation, this would need to be modified to capture forces
    /// from the steering systems as they are calculated.
    /// </summary>
    private void CaptureSteeringForces()
    {
        // Note: In this simplified version, we don't have direct access to individual forces.
        // This method would need to be expanded with a more direct connection to the steering systems.
        
        // For this demo, we'll just use the total steering force
        if (_steeringData != null)
        {
            _debugSteeringForces["TotalForce"] = _steeringData.SteeringForce;
        }
    }
    
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
        if (showSteeringForces)
        {
            float offset = 0.1f;
            
            foreach (var force in _debugSteeringForces)
            {
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
    
    private void OnGUI()
    {
        if (!showDebugInfo || _entityBehaviour == null || Camera.main == null)
            return;
            
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        screenPos.y = Screen.height - screenPos.y;
        
        Entity entity = _entityBehaviour.GetEntity();
        if (entity == null)
            return;
            
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 10;
        style.alignment = TextAnchor.UpperLeft;
        
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
            
            // Add more behaviors to display...
        }
        
        GUI.Box(new Rect(screenPos.x, screenPos.y, 100, 120), "");
        GUI.Label(new Rect(screenPos.x + 5, screenPos.y + 5, 90, 110), info, style);
    }
    
    private void DrawArrowhead(Vector3 position, Vector3 direction, float size, Color color)
    {
        Vector3 right = Quaternion.Euler(0, 30, 0) * -direction * size;
        Vector3 left = Quaternion.Euler(0, -30, 0) * -direction * size;
        
        Gizmos.color = color;
        Gizmos.DrawRay(position, right);
        Gizmos.DrawRay(position, left);
    }
}