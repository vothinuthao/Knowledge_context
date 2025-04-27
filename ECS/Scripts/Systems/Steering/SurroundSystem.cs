using Components;
using Components.Steering;
using Core.ECS;
using Movement;
using Steering;
using UnityEngine;

namespace Systems.Steering
{
    /// <summary>
    /// System that processes surround behavior (encircle enemies)
    /// </summary>
    public class SurroundSystem : ISystem
    {
        private World _world;
        
        public int Priority => 88;
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            foreach (var entity in _world.GetEntitiesWith<SurroundComponent, SteeringDataComponent, PositionComponent>())
            {
                var surroundComponent = entity.GetComponent<SurroundComponent>();
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var positionComponent = entity.GetComponent<PositionComponent>();
                
                // Skip if behavior is disabled
                if (!surroundComponent.IsEnabled || !steeringData.IsEnabled)
                {
                    continue;
                }
                
                // Skip if no enemies
                if (steeringData.NearbyEnemiesIds.Count == 0)
                {
                    continue;
                }
                
                // Find nearest enemy
                Entity nearestEnemy = FindNearestEnemy(entity, steeringData);
                if (nearestEnemy == null)
                {
                    continue;
                }
                
                Vector3 enemyPosition = nearestEnemy.GetComponent<PositionComponent>().Position;
                
                // Calculate vector to enemy
                Vector3 toEnemy = positionComponent.Position - enemyPosition;
                toEnemy.y = 0; // Stay on the same plane
                
                float distanceToEnemy = toEnemy.magnitude;
                Vector3 surroundPosition;
                
                // Determine desired position based on distance
                if (distanceToEnemy < surroundComponent.SurroundRadius * 0.8f)
                {
                    // Too close - move away slightly
                    surroundPosition = enemyPosition + toEnemy.normalized * surroundComponent.SurroundRadius;
                }
                else if (distanceToEnemy > surroundComponent.SurroundRadius * 1.2f)
                {
                    // Too far - move closer
                    surroundPosition = enemyPosition + toEnemy.normalized * surroundComponent.SurroundRadius;
                }
                else
                {
                    // At good distance - move around the enemy
                    Vector3 circlePos = toEnemy.normalized * surroundComponent.SurroundRadius;
                    Vector3 tangent = new Vector3(-circlePos.z, 0, circlePos.x).normalized;
                    surroundPosition = enemyPosition + circlePos + tangent * 2f;
                }
                
                // Calculate desired velocity
                float maxSpeed = 0f;
                if (entity.HasComponent<VelocityComponent>())
                {
                    maxSpeed = entity.GetComponent<VelocityComponent>().GetEffectiveMaxSpeed();
                }
                else
                {
                    maxSpeed = surroundComponent.SurroundSpeed;
                }
                
                Vector3 desiredVelocity = (surroundPosition - positionComponent.Position).normalized * maxSpeed;
                
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
                steeringForce *= surroundComponent.Weight;
                
                // Add force to steering data
                steeringData.AddForce(steeringForce);
            }
        }
        
        private Entity FindNearestEnemy(Entity entity, SteeringDataComponent steeringData)
        {
            Vector3 position = entity.GetComponent<PositionComponent>().Position;
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
                    float distance = Vector3.Distance(position, enemyPosition);
                    
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

