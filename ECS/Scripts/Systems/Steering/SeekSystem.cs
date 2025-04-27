// ECS/Scripts/Systems/Steering/SeekSystem.cs
using Components;
using Core.ECS;
using Steering;
using UnityEngine;
using System.Collections.Generic;
using Components.Steering;

namespace Systems.Steering
{
    /// <summary>
    /// System that processes seek steering behavior (move toward target)
    /// </summary>
    public class SeekSystem : ISystem
    {
        private World _world;
        
        public int Priority => 98; // High priority
        
        // FIX: Add threshold to avoid oscillation
        private const float ARRIVAL_THRESHOLD = 0.2f; // Distance considered as arrived
        private const float MIN_VELOCITY_THRESHOLD = 0.05f; // Minimum velocity to avoid jittering
        
        // FIX: Store previous targets to avoid constant updates
        private Dictionary<int, Vector3> _previousTargets = new Dictionary<int, Vector3>();
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
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
                
                // Update steering data
                steeringData.Update(deltaTime);
                
                // Skip if no target position
                if (steeringData.TargetPosition == Vector3.zero)
                {
                    // FIX: Remove old target if no longer exists
                    if (_previousTargets.ContainsKey(entity.Id))
                    {
                        _previousTargets.Remove(entity.Id);
                    }
                    continue;
                }
                
                // FIX: Use smoothed target for smoother movement
                Vector3 targetPosition = steeringData.SmoothedTargetPosition;
                
                // Calculate direction to target
                Vector3 toTarget = targetPosition - positionComponent.Position;
                toTarget.y = 0; // Keep movement on the horizontal plane
                
                float distance = toTarget.magnitude;
                
                // FIX: Check if already arrived
                if (steeringData.HasReachedTarget(positionComponent.Position))
                {
                    // Stop completely when arrived
                    if (entity.HasComponent<VelocityComponent>())
                    {
                        var velocityComponent = entity.GetComponent<VelocityComponent>();
                        
                        // Gradually reduce velocity to zero
                        if (velocityComponent.Velocity.magnitude > MIN_VELOCITY_THRESHOLD)
                        {
                            Vector3 brakingForce = -velocityComponent.Velocity * 5.0f;
                            steeringData.AddForce(brakingForce);
                        }
                        else
                        {
                            velocityComponent.Velocity = Vector3.zero;
                        }
                    }
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
                
                // FIX: Apply arrival behavior
                float targetSpeed = maxSpeed;
                
                if (steeringData.IsInSlowingZone(positionComponent.Position))
                {
                    // Slow down based on distance
                    float slowingFactor = steeringData.GetSlowingFactor(positionComponent.Position);
                    targetSpeed *= slowingFactor;
                    
                    // Ensure minimum speed when very close to avoid stopping too early
                    if (distance < 0.5f && targetSpeed < MIN_VELOCITY_THRESHOLD)
                    {
                        targetSpeed = MIN_VELOCITY_THRESHOLD;
                    }
                }
                
                Vector3 desiredVelocity = toTarget.normalized * targetSpeed;
                
                // Calculate steering force
                Vector3 steeringForce;
                if (entity.HasComponent<VelocityComponent>())
                {
                    var velocityComponent = entity.GetComponent<VelocityComponent>();
                    steeringForce = desiredVelocity - velocityComponent.Velocity;
                    
                    // FIX: Scale force based on distance for smoother approach
                    if (distance < 1.0f)
                    {
                        steeringForce *= distance;
                    }
                }
                else
                {
                    steeringForce = desiredVelocity;
                }
                
                // Apply weight
                steeringForce *= seekComponent.Weight;
                
                // FIX: Limit force to prevent overshooting
                float maxForce = 10.0f;
                if (steeringForce.magnitude > maxForce)
                {
                    steeringForce = steeringForce.normalized * maxForce;
                }
                
                // Add force to steering data
                steeringData.AddForce(steeringForce);
                
                // Debug if needed
                if (steeringForce.magnitude > 5.0f)
                {
                    // Debug.Log($"Entity {entity.Id} seek force: {steeringForce.magnitude}, " +
                    //          $"distance to target: {distance}, " +
                    //          $"target pos: {targetPosition}, " +
                    //          $"current pos: {positionComponent.Position}");
                }
            }
        }
    }
}