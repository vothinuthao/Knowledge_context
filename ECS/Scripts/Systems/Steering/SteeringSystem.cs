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
            // Reset all steering forces
            foreach (var entity in _world.GetEntitiesWith<SteeringDataComponent>())
            {
                entity.GetComponent<SteeringDataComponent>().Reset();
            }
            
            // Process seek behavior
            ProcessSeekBehavior();
            
            // Process separation behavior
            ProcessSeparationBehavior();
            
            // Process other behaviors...
            
            // Apply calculated steering forces to acceleration
            ApplySteeringForces();
        }
        
        /// <summary>
        /// Process seek behavior for all entities with SeekComponent
        /// </summary>
        private void ProcessSeekBehavior()
        {
            foreach (var entity in _world.GetEntitiesWith<SeekComponent, SteeringDataComponent, PositionComponent>())
            {
                var seekComponent = entity.GetComponent<SeekComponent>();
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                
                // Skip if behavior is disabled
                if (!seekComponent.IsEnabled || !steeringData.IsEnabled)
                {
                    continue;
                }
                
                // Skip if no target position
                if (steeringData.TargetPosition == Vector3.zero)
                {
                    continue;
                }
                
                // Calculate direction to target
                Vector3 toTarget = steeringData.TargetPosition - positionComponent.Position;
                
                // Skip if already at target
                if (toTarget.magnitude < 0.1f)
                {
                    continue;
                }
                
                // Calculate desired velocity
                float maxSpeed = 0f;
                if (entity.HasComponent<VelocityComponent>())
                {
                    maxSpeed = entity.GetComponent<VelocityComponent>().GetEffectiveMaxSpeed();
                }
                else
                {
                    maxSpeed = 3.0f; // Default speed
                }
                
                Vector3 desiredVelocity = toTarget.normalized * maxSpeed;
                
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
                steeringForce *= seekComponent.Weight;
                
                // Add force to steering data
                steeringData.AddForce(steeringForce);
            }
        }
        
        /// <summary>
        /// Process separation behavior for all entities with SeparationComponent
        /// </summary>
        private void ProcessSeparationBehavior()
        {
            foreach (var entity in _world.GetEntitiesWith<SeparationComponent, SteeringDataComponent, PositionComponent>())
            {
                var separationComponent = entity.GetComponent<SeparationComponent>();
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                
                // Skip if behavior is disabled
                if (!separationComponent.IsEnabled || !steeringData.IsEnabled)
                {
                    continue;
                }
                
                Vector3 separationForce = Vector3.zero;
                int neighborCount = 0;
                
                // Check nearby allies for separation
                foreach (var allyId in steeringData.NearbyAlliesIds)
                {
                    // Find entity by ID
                    foreach (var otherEntity in _world.GetEntitiesWith<PositionComponent>())
                    {
                        if (otherEntity.Id != allyId)
                        {
                            continue;
                        }
                        
                        var otherPosition = otherEntity.GetComponent<PositionComponent>().Position;
                        
                        // Calculate vector from other to this entity
                        Vector3 awayFromNeighbor = positionComponent.Position - otherPosition;
                        float distance = awayFromNeighbor.magnitude;
                        
                        // Skip if too far
                        if (distance <= 0 || distance > separationComponent.SeparationRadius)
                        {
                            continue;
                        }
                        
                        // Calculate repulsion force (stronger when closer)
                        Vector3 repulsionForce = awayFromNeighbor.normalized / distance;
                        
                        // Add to total force
                        separationForce += repulsionForce;
                        neighborCount++;
                    }
                }
                
                // Skip if no neighbors
                if (neighborCount == 0)
                {
                    continue;
                }
                
                // Average the force
                separationForce /= neighborCount;
                
                // Scale to max speed
                float maxSpeed = 0f;
                if (entity.HasComponent<VelocityComponent>())
                {
                    maxSpeed = entity.GetComponent<VelocityComponent>().GetEffectiveMaxSpeed();
                }
                else
                {
                    maxSpeed = 3.0f; // Default speed
                }
                
                separationForce = separationForce.normalized * maxSpeed;
                
                // Apply weight
                separationForce *= separationComponent.Weight;
                
                // Add force to steering data
                steeringData.AddForce(separationForce);
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
                
                // Apply steering force to acceleration
                accelerationComponent.Acceleration += steeringData.SteeringForce;
                
                // Limit acceleration
                accelerationComponent.LimitAcceleration();
            }
        }
    }
}