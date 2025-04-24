using Core.ECS;
using Movement;
using Steering;
using UnityEngine;

namespace Systems.Steering
{
    /// <summary>
    /// System that processes ambush move behavior (stealthy movement)
    /// </summary>
    public class AmbushMoveSystem : ISystem
    {
        private World _world;
        
        public int Priority => 70;
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            foreach (var entity in _world.GetEntitiesWith<AmbushMoveComponent, SteeringDataComponent, PositionComponent>())
            {
                var ambushComponent = entity.GetComponent<AmbushMoveComponent>();
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                
                // Skip if behavior is disabled
                if (!ambushComponent.IsEnabled || !steeringData.IsEnabled)
                {
                    continue;
                }
                
                // Skip if in danger (ambush broken)
                if (steeringData.IsInDanger)
                {
                    continue;
                }
                
                // Skip if no target position
                if (steeringData.TargetPosition == Vector3.zero)
                {
                    continue;
                }
                
                // Modify movement speed
                if (entity.HasComponent<VelocityComponent>())
                {
                    var velocityComponent = entity.GetComponent<VelocityComponent>();
                    velocityComponent.SpeedMultiplier = ambushComponent.MoveSpeedMultiplier;
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
                    maxSpeed = 3.0f * ambushComponent.MoveSpeedMultiplier; // Default speed with multiplier
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
                steeringForce *= ambushComponent.Weight;
                
                // Add force to steering data
                steeringData.AddForce(steeringForce);
            }
        }
    }
}