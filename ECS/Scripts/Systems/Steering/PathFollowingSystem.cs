using Components;
using Core.ECS;
using Movement;
using Steering;
using UnityEngine;

namespace Systems.Steering
{
    /// <summary>
    /// System that processes path following steering behavior
    /// </summary>
    public class PathFollowingSystem : ISystem
    {
        private World _world;
        
        public int Priority => 80;
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            foreach (var entity in _world.GetEntitiesWith<PathFollowingComponent, SteeringDataComponent, PositionComponent>())
            {
                var pathComponent = entity.GetComponent<PathFollowingComponent>();
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                
                // Skip if behavior is disabled
                if (!pathComponent.IsEnabled || !steeringData.IsEnabled)
                {
                    continue;
                }
                
                // Skip if no waypoints
                if (pathComponent.Waypoints.Count == 0)
                {
                    continue;
                }
                
                // Get current waypoint
                Vector3 currentWaypoint = pathComponent.GetCurrentWaypoint();
                
                // Calculate direction to waypoint
                Vector3 toWaypoint = currentWaypoint - positionComponent.Position;
                float distance = toWaypoint.magnitude;
                
                // If close enough to current waypoint, advance to next
                if (distance < pathComponent.ArrivalDistance)
                {
                    if (pathComponent.AdvanceToNext())
                    {
                        // Update with new waypoint
                        currentWaypoint = pathComponent.GetCurrentWaypoint();
                        toWaypoint = currentWaypoint - positionComponent.Position;
                        distance = toWaypoint.magnitude;
                    }
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
                
                Vector3 desiredVelocity;
                
                // If within path radius, scale speed based on distance
                if (distance < pathComponent.PathRadius)
                {
                    desiredVelocity = toWaypoint.normalized * maxSpeed * (distance / pathComponent.PathRadius);
                }
                else
                {
                    desiredVelocity = toWaypoint.normalized * maxSpeed;
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
                steeringForce *= pathComponent.Weight;
                
                // Add force to steering data
                steeringData.AddForce(steeringForce);
            }
        }
    }
}