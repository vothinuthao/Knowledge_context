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
    /// System that processes testudo formation behavior (defensive turtle formation)
    /// </summary>
    public class TestudoSystem : ISystem
    {
        private World _world;
        
        public int Priority => 95;
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            // Get all squads with testudo behavior
            var testudoSquads = new Dictionary<int, List<Entity>>();
            
            // First, identify all troops with testudo behavior and group by squad
            foreach (var entity in _world.GetEntitiesWith<TestudoComponent, SteeringDataComponent, PositionComponent, SquadMemberComponent>())
            {
                var testudoComponent = entity.GetComponent<TestudoComponent>();
                var squadMember = entity.GetComponent<SquadMemberComponent>();
                
                // Skip if behavior is disabled
                if (!testudoComponent.IsEnabled)
                {
                    continue;
                }
                
                int squadId = squadMember.SquadEntityId;
                
                if (!testudoSquads.ContainsKey(squadId))
                {
                    testudoSquads[squadId] = new List<Entity>();
                }
                
                testudoSquads[squadId].Add(entity);
            }
            
            // Process each squad with testudo behavior
            foreach (var squadGroup in testudoSquads)
            {
                int squadId = squadGroup.Key;
                List<Entity> squadMembers = squadGroup.Value;
                
                // Need at least 5 members to form a testudo
                if (squadMembers.Count < 5)
                {
                    continue;
                }
                
                // Get squad entity
                Entity squadEntity = null;
                foreach (var entity in _world.GetEntitiesWith<SquadStateComponent>())
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
                var squadState = squadEntity.GetComponent<SquadStateComponent>();
                Vector3 squadCenter = CalculateSquadCenter(squadMembers);
                Vector3 moveDirection = DetermineMovementDirection(squadMembers, squadState);
                
                // Process each member
                for (int i = 0; i < squadMembers.Count; i++)
                {
                    Entity entity = squadMembers[i];
                    var testudoComponent = entity.GetComponent<TestudoComponent>();
                    var steeringData = entity.GetComponent<SteeringDataComponent>();
                    var positionComponent = entity.GetComponent<PositionComponent>();
                    
                    // Calculate desired position in formation
                    Vector3 formationPosition = CalculateTestudoPosition(i, squadMembers.Count, 
                        squadCenter, moveDirection, testudoComponent.FormationSpacing);
                    
                    // Calculate desired velocity to reach formation position
                    float maxSpeed = 0f;
                    if (entity.HasComponent<VelocityComponent>())
                    {
                        maxSpeed = entity.GetComponent<VelocityComponent>().GetEffectiveMaxSpeed();
                        
                        // Apply speed multiplier for testudo movement (very slow)
                        entity.GetComponent<VelocityComponent>().SpeedMultiplier = testudoComponent.MovementSpeedMultiplier;
                    }
                    else
                    {
                        maxSpeed = 3.0f * testudoComponent.MovementSpeedMultiplier; // Default speed with multiplier
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
                    steeringForce *= testudoComponent.Weight;
                    
                    // Add force to steering data
                    steeringData.AddForce(steeringForce);
                    
                    // Apply defense bonuses
                    ApplyTestudoBonuses(entity, testudoComponent);
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
        
        private Vector3 DetermineMovementDirection(List<Entity> squadMembers, SquadStateComponent squadState)
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
        
        private Vector3 CalculateTestudoPosition(int index, int totalCount, Vector3 center, 
            Vector3 direction, float spacing)
        {
            // Calculate right vector perpendicular to direction
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            
            // Calculate grid dimensions (try to make it square-ish)
            int sideLength = Mathf.CeilToInt(Mathf.Sqrt(totalCount));
            
            // Calculate row and column for this index
            int row = index / sideLength;
            int col = index % sideLength;
            
            // Calculate position in rectangular grid
            float rowOffset = (row - (sideLength / 2f)) * spacing;
            float colOffset = (col - (sideLength / 2f)) * spacing;
            
            // Calculate position
            Vector3 position = center + direction * rowOffset + right * colOffset;
            
            return position;
        }
        
        private void ApplyTestudoBonuses(Entity entity, TestudoComponent testudoComponent)
        {
            // In a real implementation, this would apply defense bonuses
            // For now, this is a placeholder
            
            // Example of how it might look:
            /*
            if (entity.HasComponent<DefenseComponent>())
            {
                var defenseComponent = entity.GetComponent<DefenseComponent>();
                
                // Apply knockback resistance
                defenseComponent.KnockbackResistance += testudoComponent.KnockbackResistanceBonus;
                
                // Apply ranged defense bonus
                defenseComponent.RangedDefense += testudoComponent.RangedDefenseBonus;
            }
            */
        }
    }
}