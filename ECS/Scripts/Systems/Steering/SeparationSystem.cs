using Components;
using Core.ECS;
using Movement;
using Steering;
using UnityEngine;

namespace Systems.Steering
{
    /// <summary>
    /// System that processes separation steering behavior (avoid crowding)
    /// </summary>
    public class SeparationSystem : ISystem
    {
        private World _world;
        
        public int Priority => 95;
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
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
                        if (otherEntity.Id != allyId || otherEntity.Id == entity.Id)
                        {
                            continue;
                        }
                        
                        var otherPosition = otherEntity.GetComponent<PositionComponent>().Position;
                        
                        // Calculate vector from other to this entity
                        Vector3 awayFromNeighbor = positionComponent.Position - otherPosition;
                        float distance = awayFromNeighbor.magnitude;
                        
                        // Skip if too far or too close
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
    }
}