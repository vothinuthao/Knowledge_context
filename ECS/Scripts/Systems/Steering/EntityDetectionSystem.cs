// File: ECS/Scripts/Systems/Steering/EntityDetectionSystem.cs
using Core.ECS;
using Movement;
using Steering;
using UnityEngine;
using System.Collections.Generic;
using Components;
using Squad;

namespace Systems.Steering
{
    /// <summary>
    /// System that detects nearby entities for steering behaviors
    /// </summary>
    public class EntityDetectionSystem : ISystem
    {
        private World _world;
        
        // Default detection ranges
        private const float DEFAULT_ALLY_DETECTION_RANGE = 10.0f;
        private const float DEFAULT_ENEMY_DETECTION_RANGE = 15.0f;
        private const float DEFAULT_OBSTACLE_DETECTION_RANGE = 8.0f;
        
        public int Priority => 120; // Very high priority - should run before steering systems
        
        public void Initialize(World world)
        {
            _world = world;
        }
        
        public void Update(float deltaTime)
        {
            // Step 1: Build spatial hash (for optimized proximity checks)
            var spatialHash = BuildSpatialHash();
            
            // Step 2: Process each entity with steering data
            foreach (var entity in _world.GetEntitiesWith<SteeringDataComponent, PositionComponent>())
            {
                var steeringData = entity.GetComponent<SteeringDataComponent>();
                var position = entity.GetComponent<PositionComponent>().Position;
                
                // Skip if steering is disabled
                if (!steeringData.IsEnabled)
                {
                    continue;
                }
                
                // Clear previous detection lists
                steeringData.NearbyAlliesIds.Clear();
                steeringData.NearbyEnemiesIds.Clear();
                
                // Get detection ranges (could be customized per entity in a real implementation)
                float allyDetectionRange = DEFAULT_ALLY_DETECTION_RANGE;
                float enemyDetectionRange = DEFAULT_ENEMY_DETECTION_RANGE;
                
                // Apply stealth modifier (for entities with ambush behavior)
                if (entity.HasComponent<AmbushMoveComponent>() && !steeringData.IsInDanger)
                {
                    var ambushComponent = entity.GetComponent<AmbushMoveComponent>();
                    enemyDetectionRange *= ambushComponent.DetectionRadiusMultiplier;
                }
                
                // Detect nearby entities using spatial hash
                DetectNearbyEntities(entity, spatialHash, position, allyDetectionRange, enemyDetectionRange);
                
                // Update danger state (if any enemies are too close)
                UpdateDangerState(entity, steeringData);
                
                // Set avoid position (from closest enemy)
                UpdateAvoidPosition(entity, steeringData);
            }
        }
        
        /// <summary>
        /// Build a simple spatial hash for optimized proximity checks
        /// </summary>
        private Dictionary<int, List<Entity>> BuildSpatialHash()
        {
            // For simplicity, we'll use a very basic spatial hash
            // In a real implementation, this would be more sophisticated and efficient
            var spatialHash = new Dictionary<int, List<Entity>>();
            float cellSize = 20.0f; // Size of each cell in the spatial hash
            
            foreach (var entity in _world.GetEntitiesWith<PositionComponent>())
            {
                var position = entity.GetComponent<PositionComponent>().Position;
                
                // Calculate cell key based on position
                int cellX = Mathf.FloorToInt(position.x / cellSize);
                int cellZ = Mathf.FloorToInt(position.z / cellSize);
                int cellKey = GetCellKey(cellX, cellZ);
                
                // Add entity to spatial hash
                if (!spatialHash.ContainsKey(cellKey))
                {
                    spatialHash[cellKey] = new List<Entity>();
                }
                
                spatialHash[cellKey].Add(entity);
                
                // Also add to neighboring cells to handle boundary cases
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz == 0) continue; // Skip center cell (already added)
                        
                        int neighborKey = GetCellKey(cellX + dx, cellZ + dz);
                        
                        if (!spatialHash.ContainsKey(neighborKey))
                        {
                            spatialHash[neighborKey] = new List<Entity>();
                        }
                        
                        spatialHash[neighborKey].Add(entity);
                    }
                }
            }
            
            return spatialHash;
        }
        
        /// <summary>
        /// Generate a unique key for a spatial hash cell
        /// </summary>
        private int GetCellKey(int x, int z)
        {
            return x * 1000 + z; // Simple hash function
        }
        
        /// <summary>
        /// Detect nearby entities using spatial hash
        /// </summary>
        private void DetectNearbyEntities(Entity entity, Dictionary<int, List<Entity>> spatialHash, 
            Vector3 position, float allyDetectionRange, float enemyDetectionRange)
        {
            var steeringData = entity.GetComponent<SteeringDataComponent>();
            float cellSize = 20.0f;
            
            // Calculate cell key for entity
            int cellX = Mathf.FloorToInt(position.x / cellSize);
            int cellZ = Mathf.FloorToInt(position.z / cellSize);
            int cellKey = GetCellKey(cellX, cellZ);
            
            // Get entities in same cell
            if (spatialHash.TryGetValue(cellKey, out var cellEntities))
            {
                foreach (var otherEntity in cellEntities)
                {
                    // Skip self
                    if (otherEntity.Id == entity.Id)
                    {
                        continue;
                    }
                    
                    if (!otherEntity.HasComponent<PositionComponent>())
                    {
                        continue;
                    }
                    
                    Vector3 otherPosition = otherEntity.GetComponent<PositionComponent>().Position;
                    float distance = Vector3.Distance(position, otherPosition);
                    
                    // Determine if entity is ally or enemy 
                    // For simplicity, we'll assume entities in same faction have same faction component
                    // In a real game, you'd have a more sophisticated faction/team system
                    bool isAlly = IsSameFaction(entity, otherEntity);
                    
                    // Add to appropriate list if within detection range
                    if (isAlly && distance <= allyDetectionRange)
                    {
                        steeringData.NearbyAlliesIds.Add(otherEntity.Id);
                    }
                    else if (!isAlly && distance <= enemyDetectionRange)
                    {
                        steeringData.NearbyEnemiesIds.Add(otherEntity.Id);
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if two entities are on the same faction/team
        /// </summary>
        private bool IsSameFaction(Entity entity1, Entity entity2)
        {
            // In a real implementation, this would check faction/team components
            // For now, we'll use a simple placeholder check
            
            // Example: check if both have the same squad ID
            if (entity1.HasComponent<SquadMemberComponent>() && entity2.HasComponent<SquadMemberComponent>())
            {
                int squad1 = entity1.GetComponent<SquadMemberComponent>().SquadEntityId;
                int squad2 = entity2.GetComponent<SquadMemberComponent>().SquadEntityId;
                
                return squad1 == squad2;
            }
            
            // Default behavior - assume different factions
            return false;
        }
        
        /// <summary>
        /// Update danger state based on nearby enemies
        /// </summary>
        private void UpdateDangerState(Entity entity, SteeringDataComponent steeringData)
        {
            // Entity is in danger if enemies are nearby
            steeringData.IsInDanger = steeringData.NearbyEnemiesIds.Count > 0;
            
            // In a more sophisticated implementation, you could check distances, 
            // enemy types, etc. to determine danger level
        }
        
        /// <summary>
        /// Update avoid position based on closest enemy
        /// </summary>
        private void UpdateAvoidPosition(Entity entity, SteeringDataComponent steeringData)
        {
            if (steeringData.NearbyEnemiesIds.Count == 0)
            {
                // No enemies, clear avoid position
                steeringData.AvoidPosition = Vector3.zero;
                return;
            }
            
            Vector3 entityPosition = entity.GetComponent<PositionComponent>().Position;
            Vector3 closestEnemyPosition = Vector3.zero;
            float closestDistance = float.MaxValue;
            
            // Find closest enemy
            foreach (var enemyId in steeringData.NearbyEnemiesIds)
            {
                foreach (var enemyEntity in _world.GetEntitiesWith<PositionComponent>())
                {
                    if (enemyEntity.Id != enemyId)
                    {
                        continue;
                    }
                    
                    Vector3 enemyPosition = enemyEntity.GetComponent<PositionComponent>().Position;
                    float distance = Vector3.Distance(entityPosition, enemyPosition);
                    
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemyPosition = enemyPosition;
                    }
                }
            }
            
            // Set avoid position to closest enemy position
            steeringData.AvoidPosition = closestEnemyPosition;
        }
    }
}