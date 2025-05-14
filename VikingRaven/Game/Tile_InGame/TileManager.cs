using System.Collections.Generic;
using System.Linq;
using Core.Utils;
using UnityEngine;
using VikingRaven.Units.Components;

namespace VikingRaven.Game.Tile_InGame
{
    /// <summary>
    /// Manages the tile system in the game world
    /// </summary>
    public class TileManager : Singleton<TileManager>
    {
        [Header("Tile Generation")]
        [SerializeField] private GameObject _tilePrefab;
        [SerializeField] private Transform _tileParent;
        [SerializeField] private float _tileSize = 4f; // Size of each tile
        [SerializeField] private float _tileHeight = 0.5f; // Height of tiles
        [SerializeField] private float _neighborDetectionDistance = 5f; // Distance to detect neighboring tiles
        
        [Header("Tile Grid")]
        [SerializeField] private bool _generateAutomatically = false;
        [SerializeField] private Vector2Int _gridSize = new Vector2Int(5, 5);
        [SerializeField] private float _gridSpacing = 4f;
        
        // Tile collections
        private Dictionary<int, Tile_InGame.TileComponent> _tilesById = new Dictionary<int, Tile_InGame.TileComponent>();
        private Dictionary<int, Tile_InGame.TileComponent> _tilesBySquadId = new Dictionary<int, Tile_InGame.TileComponent>();
        
        // Selected tiles
        private Tile_InGame.TileComponent _selectedTileComponent = null;
        private List<Tile_InGame.TileComponent> _highlightedTiles = new List<Tile_InGame.TileComponent>();
        
        // Properties
        public Tile_InGame.TileComponent SelectedTileComponent => _selectedTileComponent;
        public IReadOnlyDictionary<int, Tile_InGame.TileComponent> TilesById => _tilesById;
        public IReadOnlyDictionary<int, Tile_InGame.TileComponent> TilesBySquadId => _tilesBySquadId;
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            Debug.Log("TileManager initialized");
            
            // Generate tiles if set to automatic
            if (_generateAutomatically)
            {
                GenerateTileGrid();
            }
        }
        
        private void Start()
        {
            // Find and register all tiles in the scene if not generated automatically
            if (!_generateAutomatically)
            {
                RegisterExistingTiles();
            }
        }
        
        /// <summary>
        /// Register all existing tiles in the scene
        /// </summary>
        private void RegisterExistingTiles()
        {
            var tiles = FindObjectsOfType<Tile_InGame.TileComponent>();
            
            Debug.Log($"TileManager: Found {tiles.Length} existing tiles in the scene");
            
            foreach (var tile in tiles)
            {
                RegisterTile(tile);
            }
            
            // Connect neighbors
            DetectAndAssignNeighbors();
        }
        
        /// <summary>
        /// Generate a grid of tiles
        /// </summary>
        public void GenerateTileGrid()
        {
            if (_tilePrefab == null)
            {
                Debug.LogError("TileManager: No tile prefab assigned!");
                return;
            }
            
            // Clear existing tiles if any
            ClearAllTiles();
            
            int tileId = 0;
            
            // Create tile parent if not exists
            if (_tileParent == null)
            {
                GameObject parent = new GameObject("Tiles");
                _tileParent = parent.transform;
            }
            
            // Generate grid of tiles
            for (int x = 0; x < _gridSize.x; x++)
            {
                for (int z = 0; z < _gridSize.y; z++)
                {
                    // Calculate position
                    Vector3 position = new Vector3(
                        x * _gridSpacing,
                        0,
                        z * _gridSpacing
                    );
                    
                    // Create tile
                    GameObject tileObject = Instantiate(_tilePrefab, position, Quaternion.identity, _tileParent);
                    Tile_InGame.TileComponent tileComponent = tileObject.GetComponent<Tile_InGame.TileComponent>();
                    
                    if (tileComponent == null)
                    {
                        tileComponent = tileObject.AddComponent<Tile_InGame.TileComponent>();
                    }
                    
                    // Set tile properties
                    tileComponent.SetTileId(tileId++);
                    
                    // Register tile
                    RegisterTile(tileComponent);
                }
            }
            
            // Connect neighbors
            DetectAndAssignNeighbors();
            
            Debug.Log($"TileManager: Generated {_tilesById.Count} tiles in a {_gridSize.x}x{_gridSize.y} grid");
        }
        
        /// <summary>
        /// Clear all tiles
        /// </summary>
        public void ClearAllTiles()
        {
            foreach (var tile in _tilesById.Values)
            {
                if (tile != null)
                {
                    Destroy(tile.gameObject);
                }
            }
            
            _tilesById.Clear();
            _tilesBySquadId.Clear();
            _selectedTileComponent = null;
            _highlightedTiles.Clear();
        }
        
        /// <summary>
        /// Register a tile in the manager
        /// </summary>
        public void RegisterTile(Tile_InGame.TileComponent tileComponent)
        {
            if (tileComponent == null) return;
            
            int tileId = tileComponent.TileId;
            
            // Ensure unique ID
            if (_tilesById.ContainsKey(tileId))
            {
                Debug.LogWarning($"TileManager: Tile with ID {tileId} already exists. Reassigning ID.");
                
                // Find next available ID
                int newId = _tilesById.Keys.Max() + 1;
                tileComponent.SetTileId(newId);
                tileId = newId;
            }
            
            // Register the tile
            _tilesById[tileId] = tileComponent;
            
            // Check if occupied by squad
            if (tileComponent.IsOccupied && tileComponent.OccupyingSquadId >= 0)
            {
                _tilesBySquadId[tileComponent.OccupyingSquadId] = tileComponent;
            }
        }
        
        /// <summary>
        /// Detect and assign neighboring tiles
        /// </summary>
        private void DetectAndAssignNeighbors()
        {
            foreach (var tile in _tilesById.Values)
            {
                // Find neighbors based on distance
                foreach (var otherTile in _tilesById.Values)
                {
                    if (tile == otherTile) continue;
                    
                    float distance = Vector3.Distance(tile.transform.position, otherTile.transform.position);
                    
                    if (distance <= _neighborDetectionDistance)
                    {
                        tile.AddNeighbor(otherTile);
                    }
                }
            }
            
            Debug.Log("TileManager: Assigned tile neighbors based on proximity");
        }
        
        /// <summary>
        /// Get a tile by its ID
        /// </summary>
        public Tile_InGame.TileComponent GetTileById(int tileId)
        {
            return _tilesById.TryGetValue(tileId, out Tile_InGame.TileComponent tile) ? tile : null;
        }
        
        /// <summary>
        /// Get the tile currently occupied by a squad
        /// </summary>
        public Tile_InGame.TileComponent GetTileBySquadId(int squadId)
        {
            return _tilesBySquadId.TryGetValue(squadId, out Tile_InGame.TileComponent tile) ? tile : null;
        }
        
        /// <summary>
        /// Find the closest tile to a world position
        /// </summary>
        public Tile_InGame.TileComponent FindClosestTile(Vector3 worldPosition)
        {
            Tile_InGame.TileComponent closestTileComponent = null;
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
            if (_tilesBySquadId.TryGetValue(squadId, out Tile_InGame.TileComponent oldTile))
            {
                oldTile.ReleaseTile();
            }
            
            // Register new tile
            if (_tilesById.TryGetValue(tileId, out Tile_InGame.TileComponent newTile))
            {
                newTile.OccupyTile(squadId);
                _tilesBySquadId[squadId] = newTile;
                
                Debug.Log($"TileManager: Squad {squadId} now occupies Tile {tileId}");
            }
        }
        
        /// <summary>
        /// Select a tile and highlight valid movement destinations
        /// </summary>
        public void SelectTile(Tile_InGame.TileComponent tileComponent, int currentSquadId)
        {
            // Clear previous selection
            ClearSelection();
            
            if (tileComponent == null) return;
            
            // Set as selected
            _selectedTileComponent = tileComponent;
            _selectedTileComponent.SelectTile();
            
            // Highlight valid neighbors for movement
            foreach (var neighbor in tileComponent.GetValidNeighbors(currentSquadId))
            {
                neighbor.HighlightTile();
                _highlightedTiles.Add(neighbor);
            }
            
            Debug.Log($"TileManager: Selected Tile {tileComponent.TileId} with {_highlightedTiles.Count} valid neighbors");
        }
        
        /// <summary>
        /// Clear current tile selection and highlights
        /// </summary>
        public void ClearSelection()
        {
            // Reset selected tile
            if (_selectedTileComponent != null)
            {
                _selectedTileComponent.ResetTileAppearance();
                _selectedTileComponent = null;
            }
            
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
        /// Check if a tile is valid for movement from the current selection
        /// </summary>
        public bool IsTileValidForMovement(Tile_InGame.TileComponent tileComponent)
        {
            return _highlightedTiles.Contains(tileComponent);
        }
        
        /// <summary>
        /// Get all neighboring tiles of a specific tile
        /// </summary>
        public List<Tile_InGame.TileComponent> GetNeighboringTiles(int tileId)
        {
            if (_tilesById.TryGetValue(tileId, out Tile_InGame.TileComponent tile))
            {
                return tile.Neighbors;
            }
            
            return new List<Tile_InGame.TileComponent>();
        }
        
        /// <summary>
        /// Get the optimal formation for a squad on a specific tile
        /// </summary>
        public FormationType GetOptimalFormation(int tileId, int squadSize)
        {
            if (_tilesById.TryGetValue(tileId, out Tile_InGame.TileComponent tile))
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
            if (_tilesById.TryGetValue(tileId, out Tile_InGame.TileComponent tile))
            {
                return tile.GetFormationScale(squadSize);
            }
            
            return 1.0f; // Default scale
        }
    }
}