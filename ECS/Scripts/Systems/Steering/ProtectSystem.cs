using Core.ECS;
using Movement;
using Steering;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Systems.Steering
{
    /// <summary>
    /// System that processes protect behavior (defend allies from enemies)
    /// </summary>
    public class ProtectSystem : ISystem
    {
        private World _world;
        
        public int Priority => 92;
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            foreach (var entity in _world.GetEntitiesWith<ProtectComponent, SteeringDataComponent, PositionComponent>())
            {
                var protectComponent = entity.GetComponent<ProtectComponent>();
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                
                // Skip if behavior is disabled
                if (!protectComponent.IsEnabled || !steeringData.IsEnabled)
                {
                    continue;
                }
                
                // Skip if no allies or enemies
                if (steeringData.NearbyAlliesIds.Count == 0 || steeringData.NearbyEnemiesIds.Count == 0)
                {
                    continue;
                }
                
                // Find ally to protect
                Entity allyToProtect = FindPriorityAlly(entity, protectComponent, steeringData);
                if (allyToProtect == null)
                {
                    continue;
                }
                
                // Find nearest enemy to that ally
                Entity nearestEnemy = FindNearestEnemyToAlly(allyToProtect, steeringData);
                if (nearestEnemy == null)
                {
                    continue;
                }
                
                // Calculate ideal position to protect (between ally and enemy)
                Vector3 allyPosition = allyToProtect.GetComponent<PositionComponent>().Position;
                Vector3 enemyPosition = nearestEnemy.GetComponent<PositionComponent>().Position;
                
                Vector3 direction = (enemyPosition - allyPosition).normalized;
                Vector3 idealPosition = allyPosition + direction * protectComponent.ProtectRadius * 0.5f;
                
                // Calculate desired velocity to reach ideal position
                float maxSpeed = 0f;
                if (entity.HasComponent<VelocityComponent>())
                {
                    maxSpeed = entity.GetComponent<VelocityComponent>().GetEffectiveMaxSpeed();
                }
                else
                {
                    maxSpeed = protectComponent.PositioningSpeed;
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
                steeringForce *= protectComponent.Weight;
                
                // Add force to steering data
                steeringData.AddForce(steeringForce);
            }
        }
        
        private Entity FindPriorityAlly(Entity protector, ProtectComponent protectComponent, SteeringDataComponent steeringData)
        {
            // Priority allies by tags, health, etc.
            List<(Entity entity, float priority)> prioritizedAllies = new List<(Entity, float)>();
            
            foreach (var allyId in steeringData.NearbyAlliesIds)
            {
                foreach (var allyEntity in _world.GetEntitiesWith<PositionComponent>())
                {
                    if (allyEntity.Id != allyId || allyEntity.Id == protector.Id)
                    {
                        continue;
                    }
                    
                    float priority = 0f;
                    
                    // Priority 1: Protected tags
                    /*
                    foreach (var tag in protectComponent.ProtectedTags)
                    {
                        if (allyEntity.gameObject.CompareTag(tag))
                        {
                            priority += 100f;
                            break;
                        }
                    }
                    */
                    
                    // Priority 2: Low health
                    /*
                    if (allyEntity.HasComponent<HealthComponent>())
                    {
                        var healthComponent = allyEntity.GetComponent<HealthComponent>();
                        float healthPercentage = healthComponent.CurrentHealth / healthComponent.MaxHealth;
                        priority += (1f - healthPercentage) * 50f;
                    }
                    */
                    
                    // Add to list
                    prioritizedAllies.Add((allyEntity, priority));
                }
            }
            
            // Sort by priority (descending)
            prioritizedAllies.Sort((a, b) => b.priority.CompareTo(a.priority));
            
            // Return highest priority ally, or null if none
            return prioritizedAllies.Count > 0 ? prioritizedAllies[0].entity : null;
        }
        
        private Entity FindNearestEnemyToAlly(Entity ally, SteeringDataComponent steeringData)
        {
            Vector3 allyPosition = ally.GetComponent<PositionComponent>().Position;
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
                    
                    Vector3 enemyPosition = enemyEntity.GetComponent<PositionComponent>().Position;
                    float distance = Vector3.Distance(allyPosition, enemyPosition);
                    
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = enemyEntity;
                    }
                }
            }
            
            return nearestEnemy;
        }
    }
}