using Components;
using Core.ECS;
using Movement;
using Steering;
using UnityEngine;

namespace Systems.Steering
{
    /// <summary>
    /// System that processes arrival steering behavior (slow down when approaching target)
    /// </summary>
    public class ArrivalSystem : ISystem
    {
        private World _world;
        
        public int Priority => 95;
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            foreach (var entity in _world.GetEntitiesWith<ArrivalComponent, SteeringDataComponent, PositionComponent>())
            {
                var arrivalComponent = entity.GetComponent<ArrivalComponent>();
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                
                // Skip if behavior is disabled
                if (!arrivalComponent.IsEnabled || !steeringData.IsEnabled)
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
                float distance = toTarget.magnitude;
                
                // Skip if already at target
                if (distance < 0.1f)
                {
                    continue;
                }
                
                // Calculate desired velocity based on distance
                float maxSpeed = 0f;
                if (entity.HasComponent<VelocityComponent>())
                {
                    maxSpeed = entity.GetComponent<VelocityComponent>().GetEffectiveMaxSpeed();
                }
                else
                {
                    maxSpeed = 3.0f; // Default speed
                }
                
                Vector3 desiredVelocity;
                
                // If within slowing distance, reduce speed based on how close we are
                if (distance < arrivalComponent.SlowingDistance)
                {
                    desiredVelocity = toTarget.normalized * maxSpeed * (distance / arrivalComponent.SlowingDistance);
                }
                else
                {
                    desiredVelocity = toTarget.normalized * maxSpeed;
                }
                
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
                steeringForce *= arrivalComponent.Weight;
                
                // Add force to steering data
                steeringData.AddForce(steeringForce);
            }
        }
    }
}