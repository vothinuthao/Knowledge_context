using Core.ECS;
using Movement;
using Steering;
using UnityEngine;

namespace Systems.Steering
{
    /// <summary>
    /// System that processes obstacle avoidance steering behavior
    /// </summary>
    public class ObstacleAvoidanceSystem : ISystem
    {
        private World _world;
        
        public int Priority => 105; // Very high priority
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            foreach (var entity in _world.GetEntitiesWith<ObstacleAvoidanceComponent, SteeringDataComponent, PositionComponent>())
            {
                var obstacleComponent = entity.GetComponent<ObstacleAvoidanceComponent>();
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                
                // Skip if behavior is disabled
                if (!obstacleComponent.IsEnabled || !steeringData.IsEnabled)
                {
                    continue;
                }
                
                // Skip if no velocity
                if (!entity.HasComponent<VelocityComponent>())
                {
                    continue;
                }
                
                var velocityComponent = entity.GetComponent<VelocityComponent>();
                if (velocityComponent.Velocity.magnitude < 0.1f)
                {
                    continue;
                }
                
                // Calculate ahead point in velocity direction
                Vector3 ahead = positionComponent.Position + velocityComponent.Velocity.normalized * obstacleComponent.LookAheadDistance;
                
                // Find closest obstacle (in a real implementation, this would use obstacle ids from steering data)
                Transform closestObstacle = null;
                float closestDistance = float.MaxValue;
                
                // Simplified obstacle detection for the example
                // In a real implementation, you would use raycasting or collision checks with obstacle ids from steeringData
                
                // If no obstacle found, return
                if (closestObstacle == null)
                {
                    continue;
                }
                
                // Calculate avoidance force
                Vector3 obstaclePos = closestObstacle.position; // This is simplified
                Vector3 avoidanceForce = ahead - obstaclePos;
                avoidanceForce.y = 0; // Keep on same plane
                
                if (avoidanceForce.magnitude < 0.1f)
                {
                    continue;
                }
                
                // Scale avoidance force
                float maxSpeed = velocityComponent.GetEffectiveMaxSpeed();
                avoidanceForce = avoidanceForce.normalized * maxSpeed;
                
                // Apply weight
                avoidanceForce *= obstacleComponent.Weight;
                
                // Add force to steering data
                steeringData.AddForce(avoidanceForce);
            }
        }
    }
}