using Core.ECS;
using Movement;
using Steering;
using UnityEngine;

namespace Systems.Steering
{
    /// <summary>
    /// System that processes flee steering behavior (move away from threats)
    /// </summary>
    public class FleeSystem : ISystem
    {
        private World _world;
        
        public int Priority => 100; // Higher priority than most behaviors
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            foreach (var entity in _world.GetEntitiesWith<FleeComponent, SteeringDataComponent, PositionComponent>())
            {
                var fleeComponent = entity.GetComponent<FleeComponent>();
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                
                // Skip if behavior is disabled
                if (!fleeComponent.IsEnabled || !steeringData.IsEnabled)
                {
                    continue;
                }
                
                // Skip if no avoid position
                if (steeringData.AvoidPosition == Vector3.zero)
                {
                    continue;
                }
                
                // Calculate direction from threat
                Vector3 fromThreat = positionComponent.Position - steeringData.AvoidPosition;
                float distance = fromThreat.magnitude;
                
                // Only flee if within panic distance
                if (distance > fleeComponent.PanicDistance)
                {
                    continue;
                }
                
                // Calculate desired velocity (away from threat)
                float maxSpeed = 0f;
                if (entity.HasComponent<VelocityComponent>())
                {
                    maxSpeed = entity.GetComponent<VelocityComponent>().GetEffectiveMaxSpeed();
                }
                else
                {
                    maxSpeed = 3.0f; // Default speed
                }
                
                // Stronger flee force when closer to threat
                float intensityFactor = 1.0f - (distance / fleeComponent.PanicDistance);
                float adjustedSpeed = maxSpeed * (1.0f + intensityFactor);
                
                Vector3 desiredVelocity = fromThreat.normalized * adjustedSpeed;
                
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
                steeringForce *= fleeComponent.Weight;
                
                // Add force to steering data
                steeringData.AddForce(steeringForce);
            }
        }
    }
}