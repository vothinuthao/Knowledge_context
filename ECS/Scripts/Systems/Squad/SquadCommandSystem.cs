// ECS/Scripts/Systems/Squad/SquadCommandSystem.cs
using Core.ECS;
using Movement;
using Squad;
using Steering;
using UnityEngine;
using System.Collections.Generic;
using Components;
using Components.Squad;
using Components.Steering;

namespace Systems.Squad
{
    /// <summary>
    /// System that handles squad commands and state changes
    /// </summary>
    public class SquadCommandSystem : ISystem
    {
        private World _world;
        
        public int Priority => 120; // Very high priority
        
        // FIX: Dictionary to store squad target positions
        private Dictionary<int, Vector3> _squadTargets = new Dictionary<int, Vector3>();
        
        // FIX: Dictionary to store update timers to avoid constant updates
        private Dictionary<int, float> _squadUpdateTimers = new Dictionary<int, float>();
        private const float UPDATE_INTERVAL = 0.2f; // Update every 0.2 seconds
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            // Process all squads with state components
            foreach (var squadEntity in _world.GetEntitiesWith<SquadComponent, PositionComponent>())
            {
                var stateComponent = squadEntity.GetComponent<SquadComponent>();
                var positionComponent = squadEntity.GetComponent<PositionComponent>();
                
                // FIX: Update timer for squad
                int squadId = squadEntity.Id;
                if (!_squadUpdateTimers.ContainsKey(squadId))
                {
                    _squadUpdateTimers[squadId] = 0f;
                }
                _squadUpdateTimers[squadId] += deltaTime;
                
                // FIX: Update state time tracking
                stateComponent.UpdateTime(deltaTime);
                stateComponent.UpdateMovementInfo(positionComponent.Position);
                
                switch (stateComponent.CurrentState)
                {
                    case SquadState.MOVING:
                        UpdateMovingSquad(squadEntity, stateComponent, positionComponent, deltaTime);
                        break;
                        
                    case SquadState.ATTACKING:
                        UpdateAttackingSquad(squadEntity, stateComponent, deltaTime);
                        break;
                        
                    case SquadState.DEFENDING:
                        UpdateDefendingSquad(squadEntity, stateComponent, deltaTime);
                        break;
                        
                    case SquadState.IDLE:
                    default:
                        // Idle squads should have their members locked in formation
                        if (_squadUpdateTimers[squadId] >= UPDATE_INTERVAL)
                        {
                            _squadUpdateTimers[squadId] = 0f;
                            UpdateSquadMembersFormation(squadEntity);
                        }
                        break;
                }
            }
        }
        
        /// <summary>
        /// Update a squad in the moving state
        /// </summary>
        private void UpdateMovingSquad(Entity squadEntity, SquadComponent stateComponent, 
            PositionComponent positionComponent, float deltaTime)
        {
            int squadId = squadEntity.Id;
            
            // Check if squad has reached destination
            float distanceToTarget = Vector3.Distance(positionComponent.Position, stateComponent.TargetPosition);
            
            if (distanceToTarget < 1.0f)
            {
                stateComponent.ChangeState(SquadState.IDLE);
                
                // FIX: Remove target when arrived
                if (_squadTargets.ContainsKey(squadId))
                {
                    _squadTargets.Remove(squadId);
                }
                
                // Reset squad velocity
                if (squadEntity.HasComponent<VelocityComponent>())
                {
                    squadEntity.GetComponent<VelocityComponent>().Velocity = Vector3.zero;
                }
                
                // FIX: Force immediate update of troop positions
                _squadUpdateTimers[squadId] = UPDATE_INTERVAL;
                UpdateSquadMembersFormation(squadEntity);
                
                Debug.Log($"Squad {squadEntity.Id} reached destination and is now idle");
            }
            else
            {
                // Move squad towards target
                Vector3 direction = (stateComponent.TargetPosition - positionComponent.Position).normalized;
                
                // Move squad with appropriate velocity
                if (squadEntity.HasComponent<VelocityComponent>())
                {
                    var velocityComponent = squadEntity.GetComponent<VelocityComponent>();
                    float effectiveSpeed = velocityComponent.GetEffectiveMaxSpeed();
                    
                    // FIX: Apply arrival behavior for squad
                    if (distanceToTarget < 3.0f)
                    {
                        effectiveSpeed *= distanceToTarget / 3.0f;
                    }
                    
                    // Set squad velocity
                    velocityComponent.Velocity = direction * effectiveSpeed;
                }
                
                // FIX: Update troop positions at intervals
                if (_squadUpdateTimers[squadId] >= UPDATE_INTERVAL)
                {
                    _squadUpdateTimers[squadId] = 0f;
                    UpdateSquadMembersTargets(squadEntity);
                }
            }
        }
        
        /// <summary>
        /// Update target positions for all squad members
        /// </summary>
        private void UpdateSquadMembersTargets(Entity squadEntity)
        {
            int squadId = squadEntity.Id;
            
            // Ensure squad has formation component
            if (!squadEntity.HasComponent<SquadFormationComponent>())
            {
                return;
            }
            
            var formationComponent = squadEntity.GetComponent<SquadFormationComponent>();
            
            // Ensure squad has necessary movement components
            if (!squadEntity.HasComponent<PositionComponent>() || !squadEntity.HasComponent<RotationComponent>())
            {
                return;
            }
            
            var squadPosition = squadEntity.GetComponent<PositionComponent>().Position;
            var squadRotation = squadEntity.GetComponent<RotationComponent>().Rotation;
            
            // Update formation world positions
            formationComponent.UpdateWorldPositions(squadPosition, squadRotation);
            
            // Find all entities that are members of this squad
            foreach (var memberEntity in _world.GetEntitiesWith<SquadMemberComponent, SteeringDataComponent>())
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
                if (row < 0 || row >= formationComponent.Rows || col < 0 || col >= formationComponent.Columns)
                {
                    continue;
                }
                
                // Get world position from formation
                Vector3 desiredPosition = formationComponent.WorldPositions[row, col];
                
                // Update desired position in SquadMemberComponent
                memberComponent.DesiredPosition = desiredPosition;
                
                // Update steering target
                var steeringData = memberEntity.GetComponent<SteeringDataComponent>();
                
                // FIX: Only update if position has changed significantly
                if (Vector3.Distance(steeringData.TargetPosition, desiredPosition) > 0.1f)
                {
                    steeringData.TargetPosition = desiredPosition;
                }
            }
        }
        
        /// <summary>
        /// Update squad members to maintain formation
        /// </summary>
        private void UpdateSquadMembersFormation(Entity squadEntity)
        {
            UpdateSquadMembersTargets(squadEntity);
            
            // FIX: Set tighter formation parameters for idle state
            int squadId = squadEntity.Id;
            foreach (var memberEntity in _world.GetEntitiesWith<SquadMemberComponent, SteeringDataComponent>())
            {
                var memberComponent = memberEntity.GetComponent<SquadMemberComponent>();
                
                if (memberComponent.SquadEntityId != squadId)
                {
                    continue;
                }
                
                var steeringData = memberEntity.GetComponent<SteeringDataComponent>();
                
                // Tighter parameters for idle state
                steeringData.ArrivalDistance = 0.1f;
                steeringData.SlowingDistance = 1.0f;
                steeringData.SmoothingFactor = 0.5f; // More direct movement when idle
            }
        }
        
        /// <summary>
        /// Command a squad to move to a position
        /// </summary>
        public void CommandMove(Entity squadEntity, Vector3 targetPosition)
        {
            if (!squadEntity.HasComponent<SquadComponent>())
            {
                return;
            }
            
            var stateComponent = squadEntity.GetComponent<SquadComponent>();
            
            // Log the command being executed
            Debug.Log($"Commanding Squad {squadEntity.Id} to move to {targetPosition}");
            
            // Change state to Moving
            stateComponent.ChangeState(SquadState.MOVING);
            stateComponent.TargetPosition = targetPosition;
            stateComponent.TargetEntityId = -1;
            
            // Store target in dictionary
            _squadTargets[squadEntity.Id] = targetPosition;
            
            // FIX: Reset update timer to update immediately
            _squadUpdateTimers[squadEntity.Id] = UPDATE_INTERVAL;
            
            // Configure steering parameters for moving state
            foreach (var memberEntity in _world.GetEntitiesWith<SquadMemberComponent, SteeringDataComponent>())
            {
                var memberComponent = memberEntity.GetComponent<SquadMemberComponent>();
                
                if (memberComponent.SquadEntityId != squadEntity.Id)
                {
                    continue;
                }
                
                var steeringData = memberEntity.GetComponent<SteeringDataComponent>();
                
                // Looser parameters for moving state
                steeringData.ArrivalDistance = 0.5f;
                steeringData.SlowingDistance = 2.0f;
                steeringData.SmoothingFactor = 0.2f; // Smoother movement when moving
            }
            
            // Immediately update squad members' targets
            UpdateSquadMembersTargets(squadEntity);
        }
        
        // Other command methods remain the same...
        private void UpdateAttackingSquad(Entity squadEntity, SquadComponent stateComponent, float deltaTime)
        {
            // Similar to existing implementation
        }
        
        private void UpdateDefendingSquad(Entity squadEntity, SquadComponent stateComponent, float deltaTime)
        {
            // Similar to existing implementation
        }
    }
}