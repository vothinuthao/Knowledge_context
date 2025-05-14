using System.Collections.Generic;
using System.Linq;
using Core.Utils;
using UnityEngine;
using VikingRaven.Units.Components;

namespace VikingRaven.Game.Tile_InGame
{
    /// <summary>
    /// Manages the tile system in the game world with simplified functionality
    /// </summary>
    public class SimpleTileManager : Singleton<SimpleTileManager>
    {
        [Header("Tile References")]
        [SerializeField] private Transform _tileParent;
        
        // Tile collections
        private Dictionary<int, SimpleTile> _tilesById = new Dictionary<int, SimpleTile>();
        private Dictionary<int, SimpleTile> _tilesBySquadId = new Dictionary<int, SimpleTile>();
        [SerializeField]
        private List<SimpleTile> _spawnTiles = new List<SimpleTile>();
        [SerializeField]
        private List<SimpleTile> _enemySpawnTiles = new List<SimpleTile>();
        
        // Selected tiles
        private SimpleTile _selectedTile = null;
        private List<SimpleTile> _highlightedTiles = new List<SimpleTile>();
        
        // Properties
        public SimpleTile SelectedTile => _selectedTile;
        public IReadOnlyDictionary<int, SimpleTile> TilesById => _tilesById;
        public IReadOnlyDictionary<int, SimpleTile> TilesBySquadId => _tilesBySquadId;
        public IReadOnlyList<SimpleTile> SpawnTiles => _spawnTiles;
        public IReadOnlyList<SimpleTile> EnemySpawnTiles => _enemySpawnTiles;
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            Debug.Log("SimpleTileManager initialized");
        }
        
        private void Start()
        {
            // Find and register all tiles in the scene
            RegisterExistingTiles();
        }
        
        /// <summary>
        /// Register all existing tiles in the scene
        /// </summary>
        private void RegisterExistingTiles()
        {
            var tiles = FindObjectsOfType<SimpleTile>();
            
            Debug.Log($"SimpleTileManager: Found {tiles.Length} existing tiles in the scene");
            
            foreach (var tile in tiles)
            {
                RegisterTile(tile);
            }
        }
        
        /// <summary>
        /// Register a tile with the manager
        /// </summary>
        public void RegisterTile(SimpleTile tile)
        {
            if (tile == null) return;
            
            int tileId = tile.TileId;
            
            // Ensure unique ID
            if (_tilesById.ContainsKey(tileId))
            {
                Debug.LogWarning($"SimpleTileManager: Tile with ID {tileId} already exists. Using existing tile.");
                return;
            }
            
            // Register the tile
            _tilesById[tileId] = tile;
            
            // Check if tile is a spawn point
            if (tile.IsSpawnPoint)
            {
                if (tile.IsEnemySpawn)
                {
                    _enemySpawnTiles.Add(tile);
                }
                else
                {
                    _spawnTiles.Add(tile);
                }
            }
            
            // Check if occupied by squad
            if (tile.IsOccupied && tile.OccupyingSquadId >= 0)
            {
                _tilesBySquadId[tile.OccupyingSquadId] = tile;
            }
        }
        
        /// <summary>
        /// Get a free spawn tile for player
        /// </summary>
        public SimpleTile GetFreePlayerSpawnTile()
        {
            foreach (var tile in _spawnTiles)
            {
                if (!tile.IsOccupied)
                {
                    return tile;
                }
            }
            
            // If no unoccupied tile is found, return the first spawn tile
            if (_spawnTiles.Count > 0)
            {
                Debug.LogWarning("SimpleTileManager: No free spawn tiles available, returning first spawn tile");
                return _spawnTiles[0];
            }
            
            Debug.LogError("SimpleTileManager: No spawn tiles available");
            return null;
        }
        
        /// <summary>
        /// Get a free spawn tile for enemy
        /// </summary>
        public SimpleTile GetFreeEnemySpawnTile()
        {
            foreach (var tile in _enemySpawnTiles)
            {
                if (!tile.IsOccupied)
                {
                    return tile;
                }
            }
            
            // If no unoccupied tile is found, return the first enemy spawn tile
            if (_enemySpawnTiles.Count > 0)
            {
                Debug.LogWarning("SimpleTileManager: No free enemy spawn tiles available, returning first enemy spawn tile");
                return _enemySpawnTiles[0];
            }
            
            Debug.LogError("SimpleTileManager: No enemy spawn tiles available");
            return null;
        }
        
        /// <summary>
        /// Get spawn tile by unit type
        /// </summary>
        public SimpleTile GetSpawnTileByUnitType(UnitType unitType, bool isEnemy = false)
        {
            var tileCollection = isEnemy ? _enemySpawnTiles : _spawnTiles;
            
            // Try to find an unoccupied tile matching the unit type
            foreach (var tile in tileCollection)
            {
                if (!tile.IsOccupied && tile.SpawnUnitType == unitType)
                {
                    return tile;
                }
            }
            
            // If no specific unoccupied tile found, try any unoccupied tile
            foreach (var tile in tileCollection)
            {
                if (!tile.IsOccupied)
                {
                    return tile;
                }
            }
            
            // If all tiles are occupied, find one with matching unit type
            foreach (var tile in tileCollection)
            {
                if (tile.SpawnUnitType == unitType)
                {
                    return tile;
                }
            }
            
            // Last resort: return any spawn tile
            if (tileCollection.Count > 0)
            {
                return tileCollection[0];
            }
            
            Debug.LogError($"SimpleTileManager: No spawn tiles available for {unitType}");
            return null;
        }
        
        /// <summary>
        /// Get a tile by its ID
        /// </summary>
        public SimpleTile GetTileById(int tileId)
        {
            return _tilesById.TryGetValue(tileId, out SimpleTile tile) ? tile : null;
        }
        
        /// <summary>
        /// Get the tile currently occupied by a squad
        /// </summary>
        public SimpleTile GetTileBySquadId(int squadId)
        {
            return _tilesBySquadId.TryGetValue(squadId, out SimpleTile tile) ? tile : null;
        }
        
        /// <summary>
        /// Find the closest tile to a world position
        /// </summary>
        public SimpleTile FindClosestTile(Vector3 worldPosition)
        {
            SimpleTile closestTile = null;
            float closestDistance = float.MaxValue;
            
            foreach (var tile in _tilesById.Values)
            {
                float distance = Vector3.Distance(tile.transform.position, worldPosition);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTile = tile;
                }
            }
            
            return closestTile;
        }
        
        /// <summary>
        /// Register a squad as occupying a tile
        /// </summary>
        public void RegisterSquadOnTile(int squadId, int tileId)
        {
            // Clear any previous registration for this squad
            if (_tilesBySquadId.TryGetValue(squadId, out SimpleTile oldTile))
            {
                oldTile.ReleaseTile();
            }
            
            // Register new tile
            if (_tilesById.TryGetValue(tileId, out SimpleTile newTile))
            {
                newTile.OccupyTile(squadId);
                _tilesBySquadId[squadId] = newTile;
                
                Debug.Log($"SimpleTileManager: Squad {squadId} now occupies Tile {tileId}");
            }
        }
        
        /// <summary>
        /// Highlight all valid tiles for movement
        /// </summary>
        public void HighlightAllValidTiles(int currentSquadId)
        {
            // Clear previous highlights
            ClearHighlights();
            
            // Highlight all valid tiles
            foreach (var tile in _tilesById.Values)
            {
                if (tile.IsValidDestination(currentSquadId))
                {
                    tile.HighlightTile();
                    _highlightedTiles.Add(tile);
                }
            }
            
            Debug.Log($"SimpleTileManager: Highlighted {_highlightedTiles.Count} valid tiles for movement");
        }
        
        /// <summary>
        /// Select a tile and highlight it
        /// </summary>
        public void SelectTile(SimpleTile tile)
        {
            // Clear previous selection
            ClearSelection();
            
            if (tile == null) return;
            
            // Set as selected
            _selectedTile = tile;
            _selectedTile.SelectTile();
            
            Debug.Log($"SimpleTileManager: Selected Tile {tile.TileId}");
        }
        
        /// <summary>
        /// Clear current tile selection
        /// </summary>
        public void ClearSelection()
        {
            // Reset selected tile
            if (_selectedTile != null)
            {
                _selectedTile.ResetTileAppearance();
                _selectedTile = null;
            }
        }
        
        /// <summary>
        /// Clear highlighted tiles
        /// </summary>
        public void ClearHighlights()
        {
            // Reset highlighted tiles
            foreach (var tile in _highlightedTiles)
            {
                if (tile != null)
                {
                    tile.ResetTileAppearance();
                }
            }
            
            _highlightedTiles.Clear();
        }
        
        /// <summary>
        /// Get the optimal formation for a squad on a specific tile
        /// </summary>
        public FormationType GetOptimalFormation(int tileId, int squadSize)
        {
            if (_tilesById.TryGetValue(tileId, out SimpleTile tile))
            {
                return tile.GetOptimalFormation(squadSize);
            }
            
            return FormationType.Line; // Default
        }
        
        /// <summary>
        /// Get a scaling factor for formations on a specific tile
        /// </summary>
        public float GetFormationScale(int tileId, int squadSize)
        {
            if (_tilesById.TryGetValue(tileId, out SimpleTile tile))
            {
                return tile.GetFormationScale(squadSize);
            }
            
            return 1.0f; // Default scale
        }
        
        /// <summary>
        /// Get all spawn tiles of a specific type
        /// </summary>
        public List<SimpleTile> GetSpawnTilesByType(UnitType unitType, bool isEnemy = false)
        {
            var tileCollection = isEnemy ? _enemySpawnTiles : _spawnTiles;
            return tileCollection.Where(t => t.SpawnUnitType == unitType).ToList();
        }
    }
}