using Core.ECS;
using Movement;
using Steering;
using UnityEngine;

namespace Systems.Steering
{
    /// <summary>
    /// System that processes cohesion steering behavior (move toward center of nearby allies)
    /// </summary>
    public class CohesionSystem : ISystem
    {
        private World _world;
        
        public int Priority => 85;
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            foreach (var entity in _world.GetEntitiesWith<CohesionComponent, SteeringDataComponent, PositionComponent>())
            {
                var cohesionComponent = entity.GetComponent<CohesionComponent>();
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                
                // Skip if behavior is disabled
                if (!cohesionComponent.IsEnabled || !steeringData.IsEnabled)
                {
                    continue;
                }
                
                Vector3 centerOfMass = Vector3.zero;
                int neighborCount = 0;
                
                // Check nearby allies for cohesion
                foreach (var allyId in steeringData.NearbyAlliesIds)
                {
                    // Find entity by ID
                    foreach (var allyEntity in _world.GetEntitiesWith<PositionComponent>())
                    {
                        if (allyEntity.Id != allyId)
                        {
                            continue;
                        }
                        
                        var allyPosition = allyEntity.GetComponent<PositionComponent>().Position;
                        
                        // Calculate distance to ally
                        float distance = Vector3.Distance(positionComponent.Position, allyPosition);
                        
                        // Skip if too far
                        if (distance <= 0 || distance > cohesionComponent.CohesionRadius)
                        {
                            continue;
                        }
                        
                        // Add ally's position to center of mass
                        centerOfMass += allyPosition;
                        neighborCount++;
                    }
                }
                
                // Skip if no neighbors
                if (neighborCount == 0)
                {
                    continue;
                }
                
                // Calculate center of mass
                centerOfMass /= neighborCount;
                
                // Calculate direction to center of mass
                Vector3 toCenterOfMass = centerOfMass - positionComponent.Position;
                
                // Skip if already at center
                if (toCenterOfMass.magnitude < 0.1f)
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
                
                Vector3 desiredVelocity = toCenterOfMass.normalized * maxSpeed;
                
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
                steeringForce *= cohesionComponent.Weight;
                
                // Add force to steering data
                steeringData.AddForce(steeringForce);
            }
        }
    }
}
