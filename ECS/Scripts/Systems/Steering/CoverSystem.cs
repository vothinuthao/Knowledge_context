using Core.ECS;
using Movement;
using Steering;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Components;

namespace Systems.Steering
{
    /// <summary>
    /// System that processes cover behavior (stay behind protectors)
    /// </summary>
    public class CoverSystem : ISystem
    {
        private World _world;
        
        public int Priority => 85;
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            foreach (var entity in _world.GetEntitiesWith<CoverComponent, SteeringDataComponent, PositionComponent>())
            {
                var coverComponent = entity.GetComponent<CoverComponent>();
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                
                // Skip if behavior is disabled
                if (!coverComponent.IsEnabled || !steeringData.IsEnabled)
                {
                    continue;
                }
                
                // Skip if no allies or enemies
                if (steeringData.NearbyAlliesIds.Count == 0 || steeringData.NearbyEnemiesIds.Count == 0)
                {
                    continue;
                }
                
                // Find a protector
                Entity protector = FindProtector(entity, steeringData);
                if (protector == null)
                {
                    continue;
                }
                
                // Find nearest enemy
                Entity nearestEnemy = FindNearestEnemy(steeringData);
                if (nearestEnemy == null)
                {
                    continue;
                }
                
                // Calculate ideal position to take cover (behind protector)
                Vector3 protectorPosition = protector.GetComponent<PositionComponent>().Position;
                Vector3 enemyPosition = nearestEnemy.GetComponent<PositionComponent>().Position;
                
                // Get direction from enemy to protector
                Vector3 direction = (protectorPosition - enemyPosition).normalized;
                
                // Position behind protector, away from enemy
                Vector3 idealPosition = protectorPosition + direction * coverComponent.CoverDistance;
                
                // Calculate desired velocity to reach ideal position
                float maxSpeed = 0f;
                if (entity.HasComponent<VelocityComponent>())
                {
                    maxSpeed = entity.GetComponent<VelocityComponent>().GetEffectiveMaxSpeed();
                }
                else
                {
                    maxSpeed = coverComponent.PositioningSpeed;
                }
                
                Vector3 desiredVelocity = (idealPosition - positionComponent.Position).normalized * maxSpeed;
                
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
                steeringForce *= coverComponent.Weight;
                
                // Add force to steering data
                steeringData.AddForce(steeringForce);
            }
        }
        
        private Entity FindProtector(Entity coverSeeker, SteeringDataComponent steeringData)
        {
            // Look for allies with protect behavior
            foreach (var allyId in steeringData.NearbyAlliesIds)
            {
                foreach (var allyEntity in _world.GetEntitiesWith<ProtectComponent, PositionComponent>())
                {
                    if (allyEntity.Id != allyId || allyEntity.Id == coverSeeker.Id)
                    {
                        continue;
                    }
                    
                    // Found a protector
                    return allyEntity;
                }
            }
            
            // If no protector found, find nearest ally
            Entity nearestAlly = null;
            float nearestDistance = float.MaxValue;
            
            Vector3 seekerPosition = coverSeeker.GetComponent<PositionComponent>().Position;
            
            foreach (var allyId in steeringData.NearbyAlliesIds)
            {
                foreach (var allyEntity in _world.GetEntitiesWith<PositionComponent>())
                {
                    if (allyEntity.Id != allyId || allyEntity.Id == coverSeeker.Id)
                    {
                        continue;
                    }
                    
                    Vector3 allyPosition = allyEntity.GetComponent<PositionComponent>().Position;
                    float distance = Vector3.Distance(seekerPosition, allyPosition);
                    
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestAlly = allyEntity;
                    }
                }
            }
            
            return nearestAlly;
        }
        
        private Entity FindNearestEnemy(SteeringDataComponent steeringData)
        {
            Entity nearestEnemy = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var enemyId in steeringData.NearbyEnemiesIds)
            {
                foreach (var enemyEntity in _world.GetEntitiesWith<PositionComponent>())
                {
                    if (enemyEntity.Id != enemyId)
                    {
                        continue;
                    }
                    
                    // For simplicity, just pick the first enemy
                    return enemyEntity;
                }
            }
            
            return null;
        }
    }
}