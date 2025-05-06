using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Combat.Components;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.SystemDebugger_Tool
{
    public class EntityVisualizer : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [SerializeField] private bool _showEntityGizmos = true;
        [SerializeField] private bool _showEntityLabels = true;
        [SerializeField] private bool _showSquadConnections = true;
        [SerializeField] private bool _showAttackRanges = false;
        [SerializeField] private bool _showAggroRanges = false;
        [SerializeField] private bool _showFormationOffsets = false;
        [SerializeField] private bool _showTacticalObjects = false;
        
        [Header("Gizmo Appearance")]
        [SerializeField] private float _unitGizmoSize = 0.5f;
        [SerializeField] private Color _infantryColor = Color.blue;
        [SerializeField] private Color _archerColor = Color.green;
        [SerializeField] private Color _pikeColor = Color.yellow;
        [SerializeField] private Color _squadConnectionColor = Color.white;
        [SerializeField] private Color _attackRangeColor = Color.red;
        [SerializeField] private Color _aggroRangeColor = Color.yellow;
        [SerializeField] private Color _formationOffsetColor = Color.cyan;
        [SerializeField] private Color _tacticalObjectiveColor = Color.magenta;
        
        [Header("References")]
        [SerializeField] private EntityRegistry _entityRegistry;
        
        private Dictionary<int, List<IEntity>> _squadEntities = new Dictionary<int, List<IEntity>>();
        
        private void OnDrawGizmos()
        {
            if (!_showEntityGizmos && !_showEntityLabels && !_showSquadConnections && 
                !_showAttackRanges && !_showAggroRanges && !_showFormationOffsets && !_showTacticalObjects)
                return;
                
            if (_entityRegistry == null)
                return;
                
            // Group entities by squad
            _squadEntities.Clear();
            
            var entities = _entityRegistry.GetAllEntities();
            
            foreach (var entity in entities)
            {
                // Skip inactive entities
                if (!entity.IsActive)
                    continue;
                    
                var formationComponent = entity.GetComponent<FormationComponent>();
                int squadId = formationComponent != null ? formationComponent.SquadId : -1;
                
                if (!_squadEntities.ContainsKey(squadId))
                {
                    _squadEntities[squadId] = new List<IEntity>();
                }
                
                _squadEntities[squadId].Add(entity);
                
                // Draw entity gizmo
                if (_showEntityGizmos)
                {
                    DrawEntityGizmo(entity);
                }
                
                // Draw entity label
                if (_showEntityLabels)
                {
                    DrawEntityLabel(entity);
                }
                
                // Draw attack range
                if (_showAttackRanges)
                {
                    DrawAttackRange(entity);
                }
                
                // Draw aggro range
                if (_showAggroRanges)
                {
                    DrawAggroRange(entity);
                }
                
                // Draw formation offset
                if (_showFormationOffsets)
                {
                    DrawFormationOffset(entity);
                }
                
                // Draw tactical objective
                if (_showTacticalObjects)
                {
                    DrawTacticalObjective(entity);
                }
            }
            
            // Draw squad connections
            if (_showSquadConnections)
            {
                DrawSquadConnections();
            }
        }
        
        private void DrawEntityGizmo(IEntity entity)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            if (transformComponent == null)
                return;
                
            Vector3 position = transformComponent.Position;
            
            // Get unit type to determine color
            var unitTypeComponent = entity.GetComponent<UnitTypeComponent>();
            Color gizmoColor = Color.gray;
            
            if (unitTypeComponent != null)
            {
                switch (unitTypeComponent.UnitType)
                {
                    case UnitType.Infantry:
                        gizmoColor = _infantryColor;
                        break;
                    case UnitType.Archer:
                        gizmoColor = _archerColor;
                        break;
                    case UnitType.Pike:
                        gizmoColor = _pikeColor;
                        break;
                }
            }
            
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(position, _unitGizmoSize);
            
            // Draw forward direction
            Vector3 forward = transformComponent.Forward;
            Gizmos.DrawLine(position, position + forward * _unitGizmoSize * 2f);
        }
        
        private void DrawEntityLabel(IEntity entity)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            if (transformComponent == null)
                return;
                
            Vector3 position = transformComponent.Position + Vector3.up * _unitGizmoSize * 2f;
            
            string label = $"ID: {entity.Id}";
            
            // Add unit type
            var unitTypeComponent = entity.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent != null)
            {
                label += $"\n{unitTypeComponent.UnitType}";
            }
            
            // Add squad ID
            var formationComponent = entity.GetComponent<FormationComponent>();
            if (formationComponent != null)
            {
                label += $"\nSquad: {formationComponent.SquadId}";
            }
            
            // Add health
            var healthComponent = entity.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                label += $"\nHP: {healthComponent.CurrentHealth:F0}/{healthComponent.MaxHealth:F0}";
            }
            
            // Add state
            var stateComponent = entity.GetComponent<StateComponent>();
            if (stateComponent != null && stateComponent.CurrentState != null)
            {
                string stateName = stateComponent.CurrentState.GetType().Name;
                label += $"\nState: {stateName}";
            }
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(position, label);
            #endif
        }
        
        private void DrawSquadConnections()
        {
            foreach (var squadPair in _squadEntities)
            {
                int squadId = squadPair.Key;
                List<IEntity> squadMembers = squadPair.Value;
                
                if (squadId == -1 || squadMembers.Count <= 1)
                    continue;
                    
                // Calculate squad center
                Vector3 squadCenter = Vector3.zero;
                int count = 0;
                
                foreach (var member in squadMembers)
                {
                    var transformComponent = member.GetComponent<TransformComponent>();
                    if (transformComponent != null)
                    {
                        squadCenter += transformComponent.Position;
                        count++;
                    }
                }
                
                if (count > 0)
                {
                    squadCenter /= count;
                }
                
                // Draw lines from center to members
                Gizmos.color = _squadConnectionColor;
                
                foreach (var member in squadMembers)
                {
                    var transformComponent = member.GetComponent<TransformComponent>();
                    if (transformComponent != null)
                    {
                        Gizmos.DrawLine(squadCenter, transformComponent.Position);
                    }
                }
                
                // Draw squad ID
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(squadCenter + Vector3.up * 2f, $"Squad {squadId}");
                #endif
            }
        }
        
        private void DrawAttackRange(IEntity entity)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var combatComponent = entity.GetComponent<CombatComponent>();
            
            if (transformComponent == null || combatComponent == null)
                return;
                
            Vector3 position = transformComponent.Position;
            float attackRange = combatComponent.AttackRange;
            
            Gizmos.color = _attackRangeColor;
            DrawWireCircle(position, attackRange);
        }
        
        private void DrawAggroRange(IEntity entity)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var aggroComponent = entity.GetComponent<AggroDetectionComponent>();
            
            if (transformComponent == null || aggroComponent == null)
                return;
                
            Vector3 position = transformComponent.Position;
            float aggroRange = aggroComponent.AggroRange;
            
            Gizmos.color = _aggroRangeColor;
            DrawWireCircle(position, aggroRange);
        }
        
        private void DrawFormationOffset(IEntity entity)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var formationComponent = entity.GetComponent<FormationComponent>();
            
            if (transformComponent == null || formationComponent == null)
                return;
                
            Vector3 position = transformComponent.Position;
            Vector3 formationOffset = formationComponent.FormationOffset;
            
            // Get squad center
            int squadId = formationComponent.SquadId;
            if (squadId == -1 || !_squadEntities.ContainsKey(squadId))
                return;
                
            Vector3 squadCenter = Vector3.zero;
            int count = 0;
            
            foreach (var member in _squadEntities[squadId])
            {
                var memberTransform = member.GetComponent<TransformComponent>();
                if (memberTransform != null)
                {
                    squadCenter += memberTransform.Position;
                    count++;
                }
            }
            
            if (count > 0)
            {
                squadCenter /= count;
            }
            
            // Draw line to ideal position
            Gizmos.color = _formationOffsetColor;
            
            // Get squad rotation
            Quaternion squadRotation = Quaternion.identity;
            Vector3 squadForward = Vector3.zero;
            
            foreach (var member in _squadEntities[squadId])
            {
                var memberTransform = member.GetComponent<TransformComponent>();
                if (memberTransform != null)
                {
                    squadForward += memberTransform.Forward;
                }
            }
            
            if (squadForward.magnitude > 0.01f)
            {
                squadForward.Normalize();
                squadRotation = Quaternion.LookRotation(squadForward);
            }
            
            Vector3 idealPosition = squadCenter + squadRotation * formationOffset;
            
            Gizmos.DrawLine(position, idealPosition);
            Gizmos.DrawWireSphere(idealPosition, 0.2f);
        }
        
        private void DrawTacticalObjective(IEntity entity)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var tacticalComponent = entity.GetComponent<TacticalComponent>();
            
            if (transformComponent == null || tacticalComponent == null)
                return;
                
            Vector3 position = transformComponent.Position;
            
            Gizmos.color = _tacticalObjectiveColor;
            
            // Draw based on objective type
            switch (tacticalComponent.CurrentObjective)
            {
                case TacticalObjective.Attack:
                    if (tacticalComponent.ObjectiveTarget != null)
                    {
                        var targetTransform = tacticalComponent.ObjectiveTarget.GetComponent<TransformComponent>();
                        if (targetTransform != null)
                        {
                            Gizmos.DrawLine(position, targetTransform.Position);
                            // Draw attack symbol
                            DrawCross(targetTransform.Position + Vector3.up * 2f, 0.5f);
                        }
                    }
                    break;
                    
                case TacticalObjective.Move:
                case TacticalObjective.Hold:
                case TacticalObjective.Defend:
                case TacticalObjective.Retreat:
                case TacticalObjective.Scout:
                    Gizmos.DrawLine(position, tacticalComponent.ObjectivePosition);
                    Gizmos.DrawWireSphere(tacticalComponent.ObjectivePosition, 0.5f);
                    break;
            }
        }
        
        private void DrawWireCircle(Vector3 center, float radius)
        {
            int segments = 32;
            float step = 2f * Mathf.PI / segments;
            
            Vector3 previousPoint = center + new Vector3(Mathf.Cos(0), 0, Mathf.Sin(0)) * radius;
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = step * i;
                Vector3 point = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(previousPoint, point);
                previousPoint = point;
            }
        }
        
        private void DrawCross(Vector3 center, float size)
        {
            Gizmos.DrawLine(center - Vector3.right * size, center + Vector3.right * size);
            Gizmos.DrawLine(center - Vector3.forward * size, center + Vector3.forward * size);
        }
    }
}