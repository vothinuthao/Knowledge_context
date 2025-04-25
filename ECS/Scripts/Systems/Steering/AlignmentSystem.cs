using Core.ECS;
using Movement;
using Steering;
using UnityEngine;
using System.Collections.Generic;
using Components;

namespace Systems.Steering
{
    /// <summary>
    /// System that processes alignment steering behavior (move in same direction as neighbors)
    /// </summary>
    public class AlignmentSystem : ISystem
    {
        private World _world;
        
        public int Priority => 90;
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            foreach (var entity in _world.GetEntitiesWith<AlignmentComponent, SteeringDataComponent, PositionComponent>())
            {
                var alignmentComponent = entity.GetComponent<AlignmentComponent>();
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                
                // Skip if behavior is disabled
                if (!alignmentComponent.IsEnabled || !steeringData.IsEnabled)
                {
                    continue;
                }
                
                Vector3 averageHeading = Vector3.zero;
                int neighborCount = 0;
                
                // Check nearby allies for alignment
                foreach (var allyId in steeringData.NearbyAlliesIds)
                {
                    // Find entity by ID
                    foreach (var allyEntity in _world.GetEntitiesWith<PositionComponent, VelocityComponent>())
                    {
                        if (allyEntity.Id != allyId)
                        {
                            continue;
                        }
                        
                        var allyPosition = allyEntity.GetComponent<PositionComponent>().Position;
                        
                        // Calculate distance to ally
                        float distance = Vector3.Distance(positionComponent.Position, allyPosition);
                        
                        // Skip if too far
                        if (distance <= 0 || distance > alignmentComponent.AlignmentRadius)
                        {
                            continue;
                        }
                        
                        // Add ally's velocity direction
                        var allyVelocity = allyEntity.GetComponent<VelocityComponent>().Velocity;
                        if (allyVelocity.magnitude > 0.1f)
                        {
                            averageHeading += allyVelocity.normalized;
                            neighborCount++;
                        }
                        else
                        {
                            // If ally not moving, use its forward direction
                            // We would need a way to get the GameObject's transform - in real implementation
                            // For now we'll skip this ally if it's not moving
                        }
                    }
                }
                
                // Skip if no neighbors
                if (neighborCount == 0)
                {
                    continue;
                }
                
                // Average the headings
                averageHeading /= neighborCount;
                
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
                
                Vector3 desiredVelocity = averageHeading.normalized * maxSpeed;
                
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
                steeringForce *= alignmentComponent.Weight;
                
                // Add force to steering data
                steeringData.AddForce(steeringForce);
            }
        }
    }
}