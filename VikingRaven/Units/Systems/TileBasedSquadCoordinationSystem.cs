using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Game.Tile_InGame;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    /// <summary>
    /// Enhanced version of SquadCoordinationSystem to work with tile-based movement
    /// </summary>
    public class TileBasedSquadCoordinationSystem : SquadCoordinationSystem 
    {
        [Header("Tile System Integration")]
        [SerializeField] private bool _useTileSystem = true;
        [SerializeField] private float _tileSnapThreshold = 0.3f; // Distance threshold to snap to tile center
        [SerializeField] private float _formationTileScaleFactor = 0.8f; // Scale formations to fit in tiles
        
        [Header("Movement Settings")]
        [SerializeField] private float _pathCorrectionFactor = 0.5f; // How strongly to correct path between tiles
        [SerializeField] private bool _useSmoothedMovement = true; // Use smoothed movement between tiles
        [SerializeField] private float _smoothFactor = 0.7f; // Smoothing factor for movement (0-1)
        [SerializeField] private float _obstacleAvoidanceWeight = 0.8f; // Weight of obstacle avoidance during movement
        
        // References
        private TileManager _tileManager;
        private Dictionary<int, Vector3[]> _pathsBySquadId = new Dictionary<int, Vector3[]>();
        private Dictionary<int, int> _currentPathPointIndex = new Dictionary<int, int>();
        
        // Formation scale tracking
        private Dictionary<int, float> _formationScaleBySquadId = new Dictionary<int, float>();
        
        public override void Initialize()
        {
            base.Initialize();
            
            // Get TileManager reference
            _tileManager = TileManager.Instance;
            
            if (_tileManager == null)
            {
                Debug.LogWarning("TileBasedSquadCoordinationSystem: TileManager not found! Falling back to default movement.");
                _useTileSystem = false;
            }
            else
            {
                Debug.Log("TileBasedSquadCoordinationSystem: Successfully integrated with TileManager.");
            }
        }
        
        /// <summary>
        /// Move a squad to a tile by ID
        /// </summary>
        public void MoveSquadToTile(int squadId, int targetTileId)
        {
            if (!_useTileSystem || _tileManager == null)
            {
                Debug.LogWarning("Cannot use tile-based movement: TileManager not available");
                return;
            }
            
            // Get current and target tile
            TileComponent currentTileComponent = _tileManager.GetTileBySquadId(squadId);
            TileComponent targetTileComponent = _tileManager.GetTileById(targetTileId);
            
            if (targetTileComponent == null)
            {
                Debug.LogError($"TileBasedSquadCoordinationSystem: Target tile {targetTileId} not found");
                return;
            }
            
            // Check if tiles are neighbors (for direct movement)
            bool areNeighbors = false;
            if (currentTileComponent != null)
            {
                areNeighbors = currentTileComponent.Neighbors.Contains(targetTileComponent);
            }
            
            // Determine optimal formation for the target tile
            int squadSize = GetSquadSize(squadId);
            FormationType optimalFormation = targetTileComponent.GetOptimalFormation(squadSize);
            float formationScale = targetTileComponent.GetFormationScale(squadSize) * _formationTileScaleFactor;
            
            // Store the formation scale for this squad
            _formationScaleBySquadId[squadId] = formationScale;
            
            // Register squad on new tile
            _tileManager.RegisterSquadOnTile(squadId, targetTileId);
            
            if (areNeighbors || currentTileComponent == null)
            {
                // Direct movement to neighboring tile
                base.MoveSquadToPosition(squadId, targetTileComponent.CenterPosition);
                
                // Apply optimal formation with scale
                SetSquadFormationWithScale(squadId, optimalFormation, formationScale);
                
                Debug.Log($"TileBasedSquadCoordinationSystem: Moving squad {squadId} directly to neighboring tile {targetTileId}");
            }
            else
            {
                // Calculate path between tiles
                List<Vector3> path = CalculatePathBetweenTiles(currentTileComponent, targetTileComponent);
                
                if (path.Count > 0)
                {
                    // Store path for this squad
                    _pathsBySquadId[squadId] = path.ToArray();
                    _currentPathPointIndex[squadId] = 0;
                    
                    // Start moving to first waypoint
                    base.MoveSquadToPosition(squadId, path[0]);
                    
                    Debug.Log($"TileBasedSquadCoordinationSystem: Moving squad {squadId} to tile {targetTileId} along a path with {path.Count} waypoints");
                }
                else
                {
                    // Fallback to direct movement if no path found
                    base.MoveSquadToPosition(squadId, targetTileComponent.CenterPosition);
                    Debug.LogWarning($"TileBasedSquadCoordinationSystem: No path found between tiles, using direct movement");
                }
                
                // Apply optimal formation for the target tile
                SetSquadFormationWithScale(squadId, optimalFormation, formationScale);
            }
        }
        
        /// <summary>
        /// Override to integrate with tile system
        /// </summary>
        public override void MoveSquadToPosition(int squadId, Vector3 targetPosition)
        {
            if (!_useTileSystem || _tileManager == null)
            {
                // Fall back to base implementation if tile system not available
                base.MoveSquadToPosition(squadId, targetPosition);
                return;
            }
            
            // Find closest tile to target position
            TileComponent targetTileComponent = _tileManager.FindClosestTile(targetPosition);
            
            if (targetTileComponent != null)
            {
                // Use tile-based movement
                MoveSquadToTile(squadId, targetTileComponent.TileId);
            }
            else
            {
                // Fall back to base implementation
                base.MoveSquadToPosition(squadId, targetPosition);
            }
        }
        
        /// <summary>
        /// Override to check squad movement status and update path following
        /// </summary>
        public override void Execute()
        {
            base.Execute();
            
            // Handle path following for squads
            if (_useTileSystem && _tileManager != null)
            {
                UpdatePathFollowing();
            }
        }
        
        /// <summary>
        /// Set squad formation with scaling
        /// </summary>
        private void SetSquadFormationWithScale(int squadId, FormationType formationType, float scale)
        {
            // Apply formation type
            base.SetSquadFormation(squadId, formationType);
            
            // Apply scaling to formations by modifying unit offsets
            // Note: This would require extending FormationSystem to support scaled offsets
            // For now, store scale factor for future use
            _formationScaleBySquadId[squadId] = scale;
            
            // Notification in case formation system doesn't support scaling
            Debug.Log($"TileBasedSquadCoordinationSystem: Set squad {squadId} formation to {formationType} with scale {scale}");
        }
        
        /// <summary>
        /// Calculate path between two tiles using neighbors
        /// </summary>
        private List<Vector3> CalculatePathBetweenTiles(TileComponent startTileComponent, TileComponent endTileComponent)
        {
            List<Vector3> path = new List<Vector3>();
            
            if (startTileComponent == null || endTileComponent == null)
            {
                return path;
            }
            
            // Simple BFS to find path
            Queue<TileComponent> frontier = new Queue<TileComponent>();
            Dictionary<TileComponent, TileComponent> cameFrom = new Dictionary<TileComponent, TileComponent>();
            
            frontier.Enqueue(startTileComponent);
            cameFrom[startTileComponent] = null;
            
            bool pathFound = false;
            
            // Find path
            while (frontier.Count > 0 && !pathFound)
            {
                TileComponent current = frontier.Dequeue();
                
                if (current == endTileComponent)
                {
                    pathFound = true;
                    break;
                }
                
                foreach (var neighbor in current.GetValidNeighbors(-1)) // Use -1 to check all possible neighbors
                {
                    if (!cameFrom.ContainsKey(neighbor))
                    {
                        frontier.Enqueue(neighbor);
                        cameFrom[neighbor] = current;
                    }
                }
            }
            
            // Reconstruct path if found
            if (pathFound)
            {
                TileComponent current = endTileComponent;
                List<TileComponent> tilePath = new List<TileComponent>();
                
                while (current != startTileComponent)
                {
                    tilePath.Add(current);
                    current = cameFrom[current];
                }
                
                tilePath.Reverse();
                
                // Create position path with waypoints at tile centers
                foreach (var tile in tilePath)
                {
                    path.Add(tile.CenterPosition);
                }
                
                // Add end tile position as final waypoint
                if (!path.Contains(endTileComponent.CenterPosition))
                {
                    path.Add(endTileComponent.CenterPosition);
                }
            }
            
            return path;
        }
        
        /// <summary>
        /// Update path following for all squads
        /// </summary>
        private void UpdatePathFollowing()
        {
            // List of squads to remove from tracking (completed movement)
            List<int> completedSquads = new List<int>();
            
            // Check each squad with an active path
            foreach (var entry in _pathsBySquadId)
            {
                int squadId = entry.Key;
                Vector3[] path = entry.Value;
                
                if (!_currentPathPointIndex.TryGetValue(squadId, out int currentIndex))
                {
                    currentIndex = 0;
                    _currentPathPointIndex[squadId] = currentIndex;
                }
                
                // Skip if no path points left
                if (currentIndex >= path.Length)
                {
                    completedSquads.Add(squadId);
                    continue;
                }
                
                // Check if squad has reached current waypoint
                bool waypointReached = HasSquadReachedPosition(squadId, path[currentIndex], _tileSnapThreshold);
                
                if (waypointReached)
                {
                    // Move to next waypoint
                    currentIndex++;
                    _currentPathPointIndex[squadId] = currentIndex;
                    
                    // Check if path complete
                    if (currentIndex >= path.Length)
                    {
                        completedSquads.Add(squadId);
                        
                        // Final position adjustment
                        if (path.Length > 0)
                        {
                            Vector3 finalPosition = path[path.Length - 1];
                            base.MoveSquadToPosition(squadId, finalPosition);
                        }
                        
                        Debug.Log($"TileBasedSquadCoordinationSystem: Squad {squadId} completed path movement");
                        continue;
                    }
                    
                    // Move to next waypoint
                    base.MoveSquadToPosition(squadId, path[currentIndex]);
                    
                    Debug.Log($"TileBasedSquadCoordinationSystem: Squad {squadId} reached waypoint {currentIndex-1}, moving to next waypoint");
                }
            }
            
            // Clean up completed squads
            foreach (var squadId in completedSquads)
            {
                _pathsBySquadId.Remove(squadId);
                _currentPathPointIndex.Remove(squadId);
            }
        }
        
        /// <summary>
        /// Check if a squad has reached a position within threshold
        /// </summary>
        private bool HasSquadReachedPosition(int squadId, Vector3 targetPosition, float threshold)
        {
            // Get all entities in squad
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            bool allUnitsArrived = true;
            bool anyUnitFound = false;
            
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                
                if (formationComponent != null && formationComponent.SquadId == squadId)
                {
                    anyUnitFound = true;
                    
                    var transformComponent = entity.GetComponent<TransformComponent>();
                    if (transformComponent != null)
                    {
                        float distance = Vector3.Distance(transformComponent.Position, targetPosition);
                        
                        // Check if within threshold
                        if (distance > threshold)
                        {
                            allUnitsArrived = false;
                            break;
                        }
                    }
                }
            }
            
            return anyUnitFound && allUnitsArrived;
        }
        
        /// <summary>
        /// Get the number of units in a squad
        /// </summary>
        private int GetSquadSize(int squadId)
        {
            // Count all units with this squad ID
            var entities = EntityRegistry.GetEntitiesWithComponent<FormationComponent>();
            int count = 0;
            
            foreach (var entity in entities)
            {
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent != null && formationComponent.SquadId == squadId)
                {
                    count++;
                }
            }
            
            return count;
        }
    }
}