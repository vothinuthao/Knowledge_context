// ECS/Scripts/Systems/Steering/SteeringSystem.cs
using Components;
using Components.Steering;
using Core.ECS;
using Movement;
using Steering;
using UnityEngine;

namespace Systems.Steering
{
    /// <summary>
    /// System that calculates and applies steering behaviors
    /// </summary>
    public class SteeringSystem : ISystem
    {
        private World _world;
        
        public int Priority => 110;
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            // FIX: First, update all SteeringDataComponents
            UpdateSteeringData(deltaTime);
            
            // Reset all steering forces
            foreach (var entity in _world.GetEntitiesWith<SteeringDataComponent>())
            {
                entity.GetComponent<SteeringDataComponent>().Reset();
            }
            
            // Apply calculated steering forces to acceleration
            ApplySteeringForces();
        }
        
        /// <summary>
        /// FIX: Update steering data for each entity
        /// </summary>
        private void UpdateSteeringData(float deltaTime)
        {
            foreach (var entity in _world.GetEntitiesWith<SteeringDataComponent>())
            {
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                
                // Update smoothing and tracking in SteeringDataComponent
                steeringData.Update(deltaTime);
            }
        }
        
        /// <summary>
        /// Apply calculated steering forces to acceleration
        /// </summary>
        private void ApplySteeringForces()
        {
            foreach (var entity in _world.GetEntitiesWith<SteeringDataComponent, AccelerationComponent>())
            {
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var accelerationComponent = entity.GetComponent<AccelerationComponent>();
                
                // Skip if steering is disabled
                if (!steeringData.IsEnabled)
                {
                    continue;
                }
                
                // FIX: Check if entity has reached target
                if (entity.HasComponent<PositionComponent>())
                {
                    var positionComponent = entity.GetComponent<PositionComponent>();
                    
                    if (steeringData.HasReachedTarget(positionComponent.Position))
                    {
                        // Entity has arrived, apply braking force
                        if (entity.HasComponent<VelocityComponent>())
                        {
                            var velocityComponent = entity.GetComponent<VelocityComponent>();
                            
                            // Gradually reduce velocity to zero
                            if (velocityComponent.Velocity.magnitude > 0.01f)
                            {
                                // Stronger braking force when very close to target
                                float brakingFactor = 5.0f;
                                if (Vector3.Distance(positionComponent.Position, steeringData.TargetPosition) < 0.1f)
                                {
                                    brakingFactor = 10.0f;
                                }
                                
                                accelerationComponent.Acceleration = -velocityComponent.Velocity * brakingFactor;
                            }
                            else
                            {
                                // Stop completely
                                velocityComponent.Velocity = Vector3.zero;
                                accelerationComponent.Acceleration = Vector3.zero;
                            }
                        }
                        
                        continue;
                    }
                    
                    // FIX: Apply slowing factor if in slowing zone
                    if (steeringData.IsInSlowingZone(positionComponent.Position))
                    {
                        float slowingFactor = steeringData.GetSlowingFactor(positionComponent.Position);
                        
                        // Apply force with slowing factor
                        accelerationComponent.Acceleration += steeringData.SteeringForce * slowingFactor;
                    }
                    else
                    {
                        // Apply normal force
                        accelerationComponent.Acceleration += steeringData.SteeringForce;
                    }
                }
                else
                {
                    // No PositionComponent, apply force normally
                    accelerationComponent.Acceleration += steeringData.SteeringForce;
                }
                
                // Limit acceleration
                accelerationComponent.LimitAcceleration();
            }
        }
    }
}