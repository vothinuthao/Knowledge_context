using Core.ECS;
using Movement;
using Squad;
using UnityEngine;

namespace Systems.Squad
{
    /// <summary>
    /// System that handles squad commands and state changes
    /// </summary>
    public class SquadCommandSystem : ISystem
    {
        private World _world;
        
        public int Priority => 120; // Very high priority
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            // Process all squads with state components
            foreach (var squadEntity in _world.GetEntitiesWith<SquadStateComponent, PositionComponent>())
            {
                var stateComponent = squadEntity.GetComponent<SquadStateComponent>();
                var positionComponent = squadEntity.GetComponent<PositionComponent>();
                
                switch (stateComponent.CurrentState)
                {
                    case SquadState.Moving:
                        UpdateMovingSquad(squadEntity, stateComponent, positionComponent, deltaTime);
                        break;
                        
                    case SquadState.Attacking:
                        UpdateAttackingSquad(squadEntity, stateComponent, deltaTime);
                        break;
                        
                    case SquadState.Defending:
                        UpdateDefendingSquad(squadEntity, stateComponent, deltaTime);
                        break;
                        
                    case SquadState.Idle:
                    default:
                        // Nothing to do for idle squads
                        break;
                }
            }
        }
        
        /// <summary>
        /// Update a squad in the moving state
        /// </summary>
        private void UpdateMovingSquad(Entity squadEntity, SquadStateComponent stateComponent, 
            PositionComponent positionComponent, float deltaTime)
        {
            // Check if squad has reached destination
            float distanceToTarget = Vector3.Distance(positionComponent.Position, stateComponent.TargetPosition);
            
            if (distanceToTarget < 1.0f)
            {
                // Reached destination, transition to idle
                stateComponent.CurrentState = SquadState.Idle;
            }
            else
            {
                // Move squad towards target
                // In a real implementation, this would use a path finding system
                // For simplicity, we'll just move directly towards the target
                
                // Calculate direction to target
                Vector3 direction = (stateComponent.TargetPosition - positionComponent.Position).normalized;
                
                // Move squad
                if (squadEntity.HasComponent<VelocityComponent>())
                {
                    var velocityComponent = squadEntity.GetComponent<VelocityComponent>();
                    velocityComponent.Velocity = direction * velocityComponent.GetEffectiveMaxSpeed();
                }
            }
        }
        
        /// <summary>
        /// Update a squad in the attacking state
        /// </summary>
        private void UpdateAttackingSquad(Entity squadEntity, SquadStateComponent stateComponent, float deltaTime)
        {
            // In a real implementation, this would manage attack behavior
            // For simplicity, we'll just keep the squad in attacking state
            
            // Check if target entity still exists
            bool targetExists = false;
            
            foreach (var entity in _world.GetEntitiesWith<PositionComponent>())
            {
                if (entity.Id == stateComponent.TargetEntityId)
                {
                    targetExists = true;
                    break;
                }
            }
            
            // If target no longer exists, transition to idle
            if (!targetExists)
            {
                stateComponent.CurrentState = SquadState.Idle;
                stateComponent.TargetEntityId = -1;
            }
        }
        
        /// <summary>
        /// Update a squad in the defending state
        /// </summary>
        private void UpdateDefendingSquad(Entity squadEntity, SquadStateComponent stateComponent, float deltaTime)
        {
            // In a real implementation, this would manage defense behavior
            // For simplicity, we'll just keep the squad in defending state
        }
        
        /// <summary>
        /// Command a squad to move to a position
        /// </summary>
        public void CommandMove(Entity squadEntity, Vector3 targetPosition)
        {
            if (!squadEntity.HasComponent<SquadStateComponent>())
            {
                return;
            }
            
            var stateComponent = squadEntity.GetComponent<SquadStateComponent>();
            
            stateComponent.CurrentState = SquadState.Moving;
            stateComponent.TargetPosition = targetPosition;
            stateComponent.TargetEntityId = -1;
        }
        
        /// <summary>
        /// Command a squad to attack a target
        /// </summary>
        public void CommandAttack(Entity squadEntity, Entity targetEntity)
        {
            if (!squadEntity.HasComponent<SquadStateComponent>() || 
                !targetEntity.HasComponent<PositionComponent>())
            {
                return;
            }
            
            var stateComponent = squadEntity.GetComponent<SquadStateComponent>();
            var targetPosition = targetEntity.GetComponent<PositionComponent>().Position;
            
            stateComponent.CurrentState = SquadState.Attacking;
            stateComponent.TargetPosition = targetPosition;
            stateComponent.TargetEntityId = targetEntity.Id;
        }
        
        /// <summary>
        /// Command a squad to defend its current position
        /// </summary>
        public void CommandDefend(Entity squadEntity)
        {
            if (!squadEntity.HasComponent<SquadStateComponent>())
            {
                return;
            }
            
            var stateComponent = squadEntity.GetComponent<SquadStateComponent>();
            
            stateComponent.CurrentState = SquadState.Defending;
            stateComponent.TargetEntityId = -1;
        }
        
        /// <summary>
        /// Command a squad to stop and become idle
        /// </summary>
        public void CommandStop(Entity squadEntity)
        {
            if (!squadEntity.HasComponent<SquadStateComponent>())
            {
                return;
            }
            
            var stateComponent = squadEntity.GetComponent<SquadStateComponent>();
            
            stateComponent.CurrentState = SquadState.Idle;
            stateComponent.TargetEntityId = -1;
        }
    }
}