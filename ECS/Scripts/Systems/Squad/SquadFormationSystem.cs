// ECS/Scripts/Systems/Squad/SquadFormationSystem.cs

using Components;
using Components.Squad;
using Components.Steering;
using Core.ECS;
using Movement;
using Squad;
using Steering;
using UnityEngine;

namespace Systems.Squad
{
    /// <summary>
    /// System that handles squad formation calculations and updates
    /// </summary>
    public class SquadFormationSystem : ISystem
    {
        private World _world;
        
        public int Priority => 80;
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            // Update all squad formations
            foreach (var squadEntity in _world.GetEntitiesWith<SquadFormationComponent, PositionComponent, RotationComponent>())
            {
                var formationComponent = squadEntity.GetComponent<SquadFormationComponent>();
                var positionComponent = squadEntity.GetComponent<PositionComponent>();
                var rotationComponent = squadEntity.GetComponent<RotationComponent>();
                
                // FIX: Always update formation while squad is moving
                bool shouldUpdate = false;
                
                // Check if the squad has moved
                if (positionComponent.HasMoved(0.01f))
                {
                    shouldUpdate = true;
                }
                
                // FIX: Also update if squad is in moving state
                if (squadEntity.HasComponent<SquadStateComponent>() && 
                    squadEntity.GetComponent<SquadStateComponent>().CurrentState == SquadState.MOVING)
                {
                    shouldUpdate = true;
                }
                
                if (shouldUpdate)
                {
                    // Update formation world positions
                    formationComponent.UpdateWorldPositions(
                        positionComponent.Position,
                        rotationComponent.Rotation
                    );
                    
                    // Update all members of this squad
                    UpdateSquadMembers(squadEntity.Id, formationComponent);
                    
                    // FIX: Debug log
                    // Debug.Log($"Updated formation for Squad {squadEntity.Id} at position {positionComponent.Position}");
                }
            }
        }
        
        /// <summary>
        /// Update all members of a squad with their desired positions
        /// </summary>
        private void UpdateSquadMembers(int squadId, SquadFormationComponent formation)
        {
            // Find all entities that are members of this squad
            foreach (var memberEntity in _world.GetEntitiesWith<SquadMemberComponent>())
            {
                var memberComponent = memberEntity.GetComponent<SquadMemberComponent>();
                
                // Skip if not a member of this squad
                if (memberComponent.SquadEntityId != squadId)
                {
                    continue;
                }
                
                // Get grid position
                int row = memberComponent.GridPosition.x;
                int col = memberComponent.GridPosition.y;
                
                // Skip if invalid position
                if (row < 0 || row >= formation.Rows || col < 0 || col >= formation.Columns)
                {
                    Debug.LogWarning($"Squad member {memberEntity.Id} has invalid grid position: ({row}, {col})");
                    continue;
                }
                
                // Update desired position
                Vector3 desiredPosition = formation.WorldPositions[row, col];
                memberComponent.DesiredPosition = desiredPosition;
                
                // Update steering target if entity has SteeringDataComponent
                if (memberEntity.HasComponent<SteeringDataComponent>())
                {
                    var steeringData = memberEntity.GetComponent<SteeringDataComponent>();
                    steeringData.TargetPosition = desiredPosition;
                    
                    // FIX: Debug log for target position changes
                    // Debug.Log($"Updated target position for member {memberEntity.Id} to {desiredPosition}");
                }
                
                // FIX: Calculate distance to desired position
                if (memberEntity.HasComponent<PositionComponent>())
                {
                    var positionComponent = memberEntity.GetComponent<PositionComponent>();
                    float distance = Vector3.Distance(positionComponent.Position, desiredPosition);
                    
                    // Debug log if too far from desired position
                    if (distance > 3.0f)
                    {
                        // Debug.LogWarning($"Squad member {memberEntity.Id} is {distance:F2} units from desired position");
                    }
                }
            }
        }
    }
}