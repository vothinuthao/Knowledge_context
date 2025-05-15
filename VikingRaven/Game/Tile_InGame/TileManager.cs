using System.Collections.Generic;
using System.Linq;
using Core.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using VikingRaven.Units.Components;

namespace VikingRaven.Game.Tile_InGame
{
    /// <summary>
    /// Manages the tile system in the game world
    /// </summary>
    public class TileManager : Singleton<TileManager>
    {
        [TitleGroup("Tile References")]
        [Tooltip("Parent transform containing all tile objects")]
        [SerializeField] private Transform _tileParent;
        
        [TitleGroup("Selection Settings")]
        [Tooltip("Material to use for tile highlights")]
        [SerializeField] private Material _highlightMaterial;
        
        [TitleGroup("Debug")]
        [SerializeField, ReadOnly] private int _tileCount = 0;
        [SerializeField, ReadOnly] private int _occupiedTileCount = 0;
        [SerializeField, ReadOnly] private int _spawnTileCount = 0;
        
        // Tile collections
        private Dictionary<int, TileComponent> _tilesById = new Dictionary<int, TileComponent>();
        private Dictionary<int, TileComponent> _tilesBySquadId = new Dictionary<int, TileComponent>();
        private List<TileComponent> _spawnTiles = new List<TileComponent>();
        private List<TileComponent> _enemySpawnTiles = new List<TileComponent>();
        
        // Selected tiles
        private TileComponent _selectedTileComponent = null;
        private List<TileComponent> _highlightedTiles = new List<TileComponent>();
        
        // Properties
        public TileComponent SelectedTileComponent => _selectedTileComponent;
        public IReadOnlyDictionary<int, TileComponent> TilesById => _tilesById;
        public IReadOnlyDictionary<int, TileComponent> TilesBySquadId => _tilesBySquadId;
        public IReadOnlyList<TileComponent> SpawnTiles => _spawnTiles;
        public IReadOnlyList<TileComponent> EnemySpawnTiles => _enemySpawnTiles;
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            Debug.Log("TileManager initialized");
        }
        
        private void Start()
        {
            // Find and register all tiles in the scene
            RegisterExistingTiles();
        }
        
        /// <summary>
        /// Register all existing tiles in the scene
        /// </summary>
        [Button("Register All Tiles")]
        private void RegisterExistingTiles()
        {
            // Clear existing collections
            _tilesById.Clear();
            _tilesBySquadId.Clear();
            _spawnTiles.Clear();
            _enemySpawnTiles.Clear();
            
            // Find all tiles
            var tiles = FindObjectsOfType<TileComponent>();
            
            Debug.Log($"TileManager: Found {tiles.Length} existing tiles in the scene");
            
            foreach (var tile in tiles)
            {
                RegisterTile(tile);
            }
            
            // Update debug counters
            UpdateDebugCounters();
        }
        
        /// <summary>
        /// Update debug counter values
        /// </summary>
        private void UpdateDebugCounters()
        {
            _tileCount = _tilesById.Count;
            _occupiedTileCount = _tilesBySquadId.Count;
            _spawnTileCount = _spawnTiles.Count + _enemySpawnTiles.Count;
        }
        
        /// <summary>
        /// Register a tile with the manager
        /// </summary>
        public void RegisterTile(TileComponent tileComponent)
        {
            if (tileComponent == null) return;
            
            int tileId = tileComponent.TileId;
            
            // Ensure unique ID
            if (_tilesById.ContainsKey(tileId))
            {
                Debug.LogWarning($"TileManager: Tile with ID {tileId} already exists. Using existing tile.");
                return;
            }
            
            // Register the tile
            _tilesById[tileId] = tileComponent;
            
            // Check if tile is a spawn point
            if (tileComponent.IsSpawnPoint)
            {
                if (tileComponent.IsEnemySpawn)
                {
                    _enemySpawnTiles.Add(tileComponent);
                }
                else
                {
                    _spawnTiles.Add(tileComponent);
                }
            }
            
            // Check if occupied by squad
            if (tileComponent.IsOccupied && tileComponent.OccupyingSquadId >= 0)
            {
                _tilesBySquadId[tileComponent.OccupyingSquadId] = tileComponent;
            }
            
            // Update debug counters
            UpdateDebugCounters();
        }
        
        /// <summary>
        /// Get a free spawn tile for player
        /// </summary>
        public TileComponent GetFreePlayerSpawnTile()
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
                Debug.LogWarning("TileManager: No free spawn tiles available, returning first spawn tile");
                return _spawnTiles[0];
            }
            
            Debug.LogError("TileManager: No spawn tiles available");
            return null;
        }
        
        /// <summary>
        /// Get a free spawn tile for enemy
        /// </summary>
        public TileComponent GetFreeEnemySpawnTile()
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
                Debug.LogWarning("TileManager: No free enemy spawn tiles available, returning first enemy spawn tile");
                return _enemySpawnTiles[0];
            }
            
            Debug.LogError("TileManager: No enemy spawn tiles available");
            return null;
        }
        
        /// <summary>
        /// Get spawn tile by unit type
        /// </summary>
        public TileComponent GetSpawnTileByUnitType(UnitType unitType, bool isEnemy = false)
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
            
            Debug.LogError($"TileManager: No spawn tiles available for {unitType}");
            return null;
        }
        
        /// <summary>
        /// Get a tile by its ID
        /// </summary>
        public TileComponent GetTileById(int tileId)
        {
            return _tilesById.TryGetValue(tileId, out TileComponent tile) ? tile : null;
        }
        
        /// <summary>
        /// Get the tile currently occupied by a squad
        /// </summary>
        public TileComponent GetTileBySquadId(int squadId)
        {
            return _tilesBySquadId.TryGetValue(squadId, out TileComponent tile) ? tile : null;
        }
        
        /// <summary>
        /// Find the closest tile to a world position
        /// </summary>
        public TileComponent FindClosestTile(Vector3 worldPosition)
        {
            TileComponent closestTileComponent = null;
            float closestDistance = float.MaxValue;
            
            foreach (var tile in _tilesById.Values)
            {
                float distance = Vector3.Distance(tile.transform.position, worldPosition);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTileComponent = tile;
                }
            }
            
            return closestTileComponent;
        }
        
        /// <summary>
        /// Register a squad as occupying a tile
        /// </summary>
        public void RegisterSquadOnTile(int squadId, int tileId)
        {
            // Clear any previous registration for this squad
            if (_tilesBySquadId.TryGetValue(squadId, out TileComponent oldTile))
            {
                oldTile.ReleaseTile();
            }
            
            // Register new tile
            if (_tilesById.TryGetValue(tileId, out TileComponent newTile))
            {
                newTile.OccupyTile(squadId);
                _tilesBySquadId[squadId] = newTile;
                
                Debug.Log($"TileManager: Squad {squadId} now occupies Tile {tileId}");
            }
            
            // Update debug counters
            UpdateDebugCounters();
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
            
            Debug.Log($"TileManager: Highlighted {_highlightedTiles.Count} valid tiles for movement");
        }
        
        /// <summary>
        /// Select a tile and highlight it
        /// </summary>
        public void SelectTile(TileComponent tileComponent)
        {
            // Clear previous selection
            ClearSelection();
            
            if (tileComponent == null) return;
            
            // Set as selected
            _selectedTileComponent = tileComponent;
            _selectedTileComponent.SelectTile();
            
            Debug.Log($"TileManager: Selected Tile {tileComponent.TileId}");
        }
        
        /// <summary>
        /// Clear current tile selection
        /// </summary>
        public void ClearSelection()
        {
            // Reset selected tile
            if (_selectedTileComponent != null)
            {
                _selectedTileComponent.ResetTileAppearance();
                _selectedTileComponent = null;
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
        /// Check if a tile is valid for movement
        /// </summary>
        public bool IsTileValidForMovement(TileComponent tileComponent)
        {
            return _highlightedTiles.Contains(tileComponent);
        }
        
        /// <summary>
        /// Get the optimal formation for a squad on a specific tile
        /// </summary>
        public FormationType GetOptimalFormation(int tileId, int squadSize)
        {
            if (_tilesById.TryGetValue(tileId, out TileComponent tile))
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
            if (_tilesById.TryGetValue(tileId, out TileComponent tile))
            {
                return tile.GetFormationScale(squadSize);
            }
            
            return 1.0f; // Default scale
        }
        
        /// <summary>
        /// Get all spawn tiles of a specific type
        /// </summary>
        public List<TileComponent> GetSpawnTilesByType(UnitType unitType, bool isEnemy = false)
        {
            var tileCollection = isEnemy ? _enemySpawnTiles : _spawnTiles;
            return tileCollection.Where(t => t.SpawnUnitType == unitType).ToList();
        }
        
        /// <summary>
        /// Display debug information about all tiles
        /// </summary>
        [Button("Debug Tile Info")]
        public void DebugTileInfo()
        {
            Debug.Log($"--- Tile System Debug ---");
            Debug.Log($"Total Tiles: {_tilesById.Count}");
            Debug.Log($"Occupied Tiles: {_tilesBySquadId.Count}");
            Debug.Log($"Player Spawn Tiles: {_spawnTiles.Count}");
            Debug.Log($"Enemy Spawn Tiles: {_enemySpawnTiles.Count}");
            
            // Debug spawn tiles
            Debug.Log("Spawn Tiles:");
            foreach (var tile in _spawnTiles)
            {
                Debug.Log($"- Player Spawn Tile {tile.TileId}: {tile.SpawnUnitType}, Occupied: {tile.IsOccupied}");
            }
            foreach (var tile in _enemySpawnTiles)
            {
                Debug.Log($"- Enemy Spawn Tile {tile.TileId}: {tile.SpawnUnitType}, Occupied: {tile.IsOccupied}");
            }
        }
    }
}