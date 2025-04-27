using Core.ECS;
using Movement;
using Steering;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Components;
using Components.Squad;
using Components.Steering;
using Squad;

namespace Systems.Steering
{
    /// <summary>
    /// System that processes phalanx formation behavior
    /// </summary>
    public class PhalanxSystem : ISystem
    {
        private World _world;
        
        public int Priority => 95;
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            // Get all squads with phalanx behavior
            var phalanxSquads = new Dictionary<int, List<Entity>>();
            
            // First, identify all troops with phalanx behavior and group by squad
            foreach (var entity in _world.GetEntitiesWith<PhalanxComponent, SteeringDataComponent, PositionComponent, SquadMemberComponent>())
            {
                var phalanxComponent = entity.GetComponent<PhalanxComponent>();
                var squadMember = entity.GetComponent<SquadMemberComponent>();
                
                // Skip if behavior is disabled
                if (!phalanxComponent.IsEnabled)
                {
                    continue;
                }
                
                int squadId = squadMember.SquadEntityId;
                
                if (!phalanxSquads.ContainsKey(squadId))
                {
                    phalanxSquads[squadId] = new List<Entity>();
                }
                
                phalanxSquads[squadId].Add(entity);
            }
            
            // Process each squad with phalanx behavior
            foreach (var squadGroup in phalanxSquads)
            {
                int squadId = squadGroup.Key;
                List<Entity> squadMembers = squadGroup.Value;
                
                // Need at least 3 members to form a phalanx
                if (squadMembers.Count < 3)
                {
                    continue;
                }
                
                // Get squad entity
                Entity squadEntity = null;
                foreach (var entity in _world.GetEntitiesWith<SquadComponent>())
                {
                    if (entity.Id == squadId)
                    {
                        squadEntity = entity;
                        break;
                    }
                }
                
                if (squadEntity == null)
                {
                    continue;
                }
                
                // Get squad state and determine movement direction
                var squadState = squadEntity.GetComponent<SquadComponent>();
                Vector3 squadCenter = CalculateSquadCenter(squadMembers);
                Vector3 moveDirection = DetermineMovementDirection(squadMembers, squadState);
                
                // Process each member
                for (int i = 0; i < squadMembers.Count; i++)
                {
                    Entity entity = squadMembers[i];
                    var phalanxComponent = entity.GetComponent<PhalanxComponent>();
                    var steeringData = entity.GetComponent<SteeringDataComponent>();
                    var positionComponent = entity.GetComponent<PositionComponent>();
                    
                    // Calculate desired position in formation
                    Vector3 formationPosition = CalculateFormationPosition(i, squadMembers.Count, 
                        squadCenter, moveDirection, phalanxComponent.FormationSpacing, 
                        phalanxComponent.MaxRowsInFormation);
                    
                    // Calculate desired velocity to reach formation position
                    float maxSpeed = 0f;
                    if (entity.HasComponent<VelocityComponent>())
                    {
                        maxSpeed = entity.GetComponent<VelocityComponent>().GetEffectiveMaxSpeed();
                        
                        // Apply speed multiplier for phalanx movement
                        entity.GetComponent<VelocityComponent>().SpeedMultiplier = phalanxComponent.MovementSpeedMultiplier;
                    }
                    else
                    {
                        maxSpeed = 3.0f * phalanxComponent.MovementSpeedMultiplier; // Default speed with multiplier
                    }
                    
                    Vector3 toFormationPos = formationPosition - positionComponent.Position;
                    Vector3 desiredVelocity = toFormationPos.normalized * maxSpeed;
                    
                    // Calculate steering force
                    Vector3 steeringForce = Vector3.zero;
                    if (entity.HasComponent<VelocityComponent>())
                    {
                        steeringForce = desiredVelocity - entity.GetComponent<VelocityComponent>().Velocity;
                    }
                    else
                    {
                        steeringForce = desiredVelocity;
                    }
                    
                    // Apply weight
                    steeringForce *= phalanxComponent.Weight;
                    
                    // Add force to steering data
                    steeringData.AddForce(steeringForce);
                }
            }
        }
        
        private Vector3 CalculateSquadCenter(List<Entity> squadMembers)
        {
            Vector3 center = Vector3.zero;
            int count = 0;
            
            foreach (var entity in squadMembers)
            {
                if (entity.HasComponent<PositionComponent>())
                {
                    center += entity.GetComponent<PositionComponent>().Position;
                    count++;
                }
            }
            
            if (count > 0)
            {
                center /= count;
            }
            
            return center;
        }
        
        private Vector3 DetermineMovementDirection(List<Entity> squadMembers, SquadComponent squadState)
        {
            // If squad is moving, use direction to target
            if (squadState.IsMoving && squadState.TargetPosition != Vector3.zero)
            {
                Vector3 squadCenter = CalculateSquadCenter(squadMembers);
                return (squadState.TargetPosition - squadCenter).normalized;
            }
            
            // Otherwise use average facing direction
            Vector3 averageDirection = Vector3.zero;
            int count = 0;
            
            foreach (var entity in squadMembers)
            {
                if (entity.HasComponent<VelocityComponent>())
                {
                    var velocity = entity.GetComponent<VelocityComponent>().Velocity;
                    if (velocity.magnitude > 0.1f)
                    {
                        averageDirection += velocity.normalized;
                        count++;
                    }
                }
            }
            
            if (count > 0)
            {
                averageDirection /= count;
                return averageDirection.normalized;
            }
            
            // Default to forward if no movement detected
            return Vector3.forward;
        }
        
        private Vector3 CalculateFormationPosition(int index, int totalCount, Vector3 center, 
            Vector3 direction, float spacing, int maxRows)
        {
            // Calculate right vector perpendicular to direction
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            
            // Calculate number of rows and columns
            int columns = Mathf.CeilToInt(Mathf.Sqrt(totalCount));
            int rows = Mathf.CeilToInt((float)totalCount / columns);
            rows = Mathf.Min(rows, maxRows);
            
            // Calculate row and column for this index
            int row = index / columns;
            int col = index % columns;
            
            // Adjust column count for last row (may have fewer columns)
            int columnsInThisRow = (row == rows - 1) ? totalCount - (row * columns) : columns;
            
            // Calculate offset from center
            float rowOffset = row * spacing;
            float colOffset = (col - (columnsInThisRow - 1) / 2.0f) * spacing;
            
            // Calculate position
            Vector3 position = center + direction * rowOffset + right * colOffset;
            
            return position;
        }
    }
}