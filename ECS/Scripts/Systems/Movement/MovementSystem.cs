// ECS/Scripts/Systems/Movement/MovementSystem.cs
using System.Linq;
using Components;
using Components.Squad;
using Core.ECS;
using Movement;
using Squad;
using UnityEngine;

namespace Systems.Movement
{
    /// <summary>
    /// System that handles entity movement based on position, velocity and acceleration
    /// </summary>
    public class MovementSystem : ISystem
    {
        private World _world;
        
        public int Priority => 100; // High priority
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            // Process entities with position, velocity and acceleration
            foreach (var entity in _world.GetEntitiesWith<PositionComponent, VelocityComponent, AccelerationComponent>())
            {
                var positionComponent = entity.GetComponent<PositionComponent>();
                var velocityComponent = entity.GetComponent<VelocityComponent>();
                var accelerationComponent = entity.GetComponent<AccelerationComponent>();
                
                // Skip if entity is position-locked
                bool isLocked = false;
                
                // Check if entity is a squad member that should be locked
                if (entity.HasComponent<SquadMemberComponent>())
                {
                    var squadMember = entity.GetComponent<SquadMemberComponent>();
                    
                    // Find the squad entity
                    var squadEntities = _world.GetEntitiesWith<SquadComponent>()
                        .Where(e => e.Id == squadMember.SquadEntityId);
                    
                    if (squadEntities.Any())
                    {
                        var squadEntity = squadEntities.First();
                        var squadState = squadEntity.GetComponent<SquadComponent>();
                        
                        // Lock position if squad is idle and member should lock
                        // FIX: Only lock if squad is ACTUALLY idle, not just when ShouldLockTroops is true
                        isLocked = squadState.CurrentState == SquadState.IDLE && 
                                  squadState.ShouldLockTroops && 
                                  squadMember.LockPositionWhenIdle;
                    }
                }
                
                // If locked, reset velocity and acceleration and continue
                if (isLocked)
                {
                    velocityComponent.Velocity = Vector3.zero;
                    accelerationComponent.Acceleration = Vector3.zero;
                    continue;
                }
                
                // Save last position
                positionComponent.UpdateLastPosition();
                
                // Update velocity based on acceleration
                velocityComponent.Velocity += accelerationComponent.Acceleration * deltaTime;
                
                // Limit velocity to max speed
                velocityComponent.LimitVelocity();
                
                // FIX: Check if velocity is too small, to prevent jittering
                if (velocityComponent.Velocity.magnitude < 0.01f)
                {
                    velocityComponent.Velocity = Vector3.zero;
                }
                
                // Update position based on velocity
                positionComponent.Position += velocityComponent.Velocity * deltaTime;
                
                // Apply ground alignment if necessary
                ApplyGroundAlignment(entity, positionComponent);
                
                // Reset acceleration
                accelerationComponent.Acceleration = Vector3.zero;
                
                // Debug velocity - helps identify if unit is moving
                if (velocityComponent.Velocity.magnitude > 0.01f)
                {
                    // Debug.Log($"Entity {entity.Id} velocity: {velocityComponent.Velocity.magnitude} in direction {velocityComponent.Velocity.normalized}");
                }
            }
        }
        
        /// <summary>
        /// Align entity to ground if necessary
        /// </summary>
        private void ApplyGroundAlignment(Entity entity, PositionComponent positionComponent)
        {
            // In a real implementation, this would raycast to ground and adjust y position
            // Here we'll just simulate a flat ground at y=0
            
            // Keep y at 0 (or use other ground detection in real implementation)
            positionComponent.Position = new Vector3(
                positionComponent.Position.x,
                0f, // Fixed ground level for simplicity
                positionComponent.Position.z
            );
        }
    }
}