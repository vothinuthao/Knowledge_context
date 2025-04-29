using Core.ECS;
using Components;
using System.Collections.Generic;
using UnityEngine;
using Core.Grid;
using System;

namespace Systems
{
    /// <summary>
    /// System for managing the grid and tile-based operations
    /// </summary>
    public class GridSystem : ISystem
    {
        private World _world;
        
        // Grid parameters
        private int _gridWidth = 20;
        private int _gridHeight = 20;
        private float _cellSize = 3.0f;
        
        // Reference to GridManager for Unity interactions
        // private GridManager _gridManager;
        
        // Entity IDs for all grid tiles
        private Dictionary<Vector2Int, int> _tileEntities = new Dictionary<Vector2Int, int>();
        
        // Currently highlighted and selected tiles
        private Entity _highlightedTile;
        private Entity _selectedTile;
        
        public int Priority => 150; // High priority to run before movement systems
        
        public void Initialize(World world)
        {
            _world = world;

            OnInitSingleton();
            CreateGridEntities();
        }
        
        public void Update(float deltaTime)
        {
            UpdateTileVisuals();
            HandleMouseHover();
        }
        
        /// <summary>
        /// Create all the grid tile entities
        /// </summary>
        private void CreateGridEntities()
        {
            if (_tileEntities.Count > 0)
                return;
            
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int z = 0; z < _gridHeight; z++)
                {
                    Vector2Int gridPos = new Vector2Int(x, z);
                    Vector3 worldPos = GridToWorld(gridPos);
                    Entity tileEntity = _world.CreateEntity();
                    tileEntity.AddComponent(new GridTileComponent(x, z));
                    tileEntity.AddComponent(new PositionComponent(worldPos));
                    _tileEntities[gridPos] = tileEntity.Id;
                    if (GridManager.IsInstanceValid())
                    {
                         GridManager.Instance.CreateTileVisual(x, z, worldPos);
                    }
                }
            }
            
            Debug.Log($"Created {_tileEntities.Count} grid tile entities");
        }
        
        /// <summary>
        /// Get a tile entity at grid coordinates
        /// </summary>
        public Entity GetTileAt(int x, int z)
        {
            Vector2Int gridPos = new Vector2Int(x, z);
            if (_tileEntities.TryGetValue(gridPos, out int entityId))
            {
                return _world.GetEntityById(entityId);
            }
            return null;
        }
        
        /// <summary>
        /// Get a tile entity at a world position
        /// </summary>
        public Entity GetTileAtPosition(Vector3 worldPosition)
        {
            Vector2Int gridPos = WorldToGrid(worldPosition);
            return GetTileAt(gridPos.x, gridPos.y);
        }
        
        private bool IsValidGridPosition(int x, int z)
        {
            return x >= 0 && x < _gridWidth && z >= 0 && z < _gridHeight;
        }
        
        /// <summary>
        /// Convert grid coordinates to world position
        /// </summary>
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * _cellSize, 0, gridPos.y * _cellSize);
        }
        
        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            if (float.IsInfinity(worldPos.x) || float.IsInfinity(worldPos.y) || float.IsInfinity(worldPos.z) ||
                float.IsNaN(worldPos.x) || float.IsNaN(worldPos.y) || float.IsNaN(worldPos.z))
            {
                Debug.LogWarning($"Invalid world position in WorldToGrid: {worldPos}");
                return new Vector2Int(0, 0);
            }
    
            int x = Mathf.FloorToInt(worldPos.x / _cellSize);
            int z = Mathf.FloorToInt(worldPos.z / _cellSize);
            return new Vector2Int(x, z);
        }
        
        /// <summary>
        /// Highlight tiles where a squad can move
        /// </summary>
        public void HighlightMovementOptions(Entity squadEntity, int moveRange = 1)
        {
            // Reset all highlights
            ClearAllHighlights();
            
            if (squadEntity == null || !squadEntity.HasComponent<PositionComponent>())
                return;
                
            var positionComponent = squadEntity.GetComponent<PositionComponent>();
            Vector2Int currentPos = WorldToGrid(positionComponent.Position);
            
            // Get all tiles within movement range
            List<Entity> movableTiles = GetTilesInRange(currentPos.x, currentPos.y, moveRange);
            
            // Highlight each tile
            foreach (var tileEntity in movableTiles)
            {
                if (tileEntity.HasComponent<GridTileComponent>())
                {
                    var tileComponent = tileEntity.GetComponent<GridTileComponent>();
                    if (!tileComponent.IsOccupied && tileComponent.IsWalkable)
                    {
                        tileComponent.IsHighlighted = true;
                        
                        // Update visual through GridManager if available
                        if ( GridManager.Instance != null)
                        {
                             GridManager.Instance.HighlightTile(tileComponent.X, tileComponent.Z, tileComponent.HighlightColor);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Get all tiles within a certain range of a position
        /// </summary>
        public List<Entity> GetTilesInRange(int centerX, int centerZ, int range)
        {
            List<Entity> tiles = new List<Entity>();
            
            for (int x = centerX - range; x <= centerX + range; x++)
            {
                for (int z = centerZ - range; z <= centerZ + range; z++)
                {
                    // Use Manhattan distance for range
                    int distance = Mathf.Abs(x - centerX) + Mathf.Abs(z - centerZ);
                    
                    if (distance <= range && IsValidGridPosition(x, z))
                    {
                        Entity tileEntity = GetTileAt(x, z);
                        if (tileEntity != null)
                        {
                            tiles.Add(tileEntity);
                        }
                    }
                }
            }
            
            return tiles;
        }
        
        /// <summary>
        /// Clear all tile highlights
        /// </summary>
        public void ClearAllHighlights()
        {
            foreach (var tileEntityId in _tileEntities.Values)
            {
                Entity tileEntity = _world.GetEntityById(tileEntityId);
                if (tileEntity != null && tileEntity.HasComponent<GridTileComponent>())
                {
                    var tileComponent = tileEntity.GetComponent<GridTileComponent>();
                    tileComponent.IsHighlighted = false;
                    
                    // Update visual through GridManager if available
                    if ( GridManager.Instance != null && !tileComponent.IsSelected)
                    {
                         GridManager.Instance.ResetTileColor(tileComponent.X, tileComponent.Z);
                    }
                }
            }
            
            _highlightedTile = null;
        }
        
        /// <summary>
        /// Select a tile
        /// </summary>
        public void SelectTile(int x, int z)
        {
            // Deselect current tile if any
            if (_selectedTile != null && _selectedTile.HasComponent<GridTileComponent>())
            {
                var currentTileComponent = _selectedTile.GetComponent<GridTileComponent>();
                currentTileComponent.IsSelected = false;
                
                if ( GridManager.Instance != null)
                {
                     GridManager.Instance.ResetTileColor(currentTileComponent.X, currentTileComponent.Z);
                }
            }
            
            // Select new tile
            Entity tileEntity = GetTileAt(x, z);
            if (tileEntity != null && tileEntity.HasComponent<GridTileComponent>())
            {
                _selectedTile = tileEntity;
                var tileComponent = tileEntity.GetComponent<GridTileComponent>();
                tileComponent.IsSelected = true;
                
                if ( GridManager.Instance != null)
                {
                     GridManager.Instance.HighlightTile(x, z, tileComponent.SelectedColor);
                }
            }
            else
            {
                _selectedTile = null;
            }
        }
        
        /// <summary>
        /// Handle mouse hover for tile highlighting
        /// </summary>
        // private void HandleMouseHover()
        // {
        //     if (!GridManager.IsInstanceValid()) return;
        //     
        //     Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //     if (Physics.Raycast(ray, out RaycastHit hit))
        //     {
        //         Vector2Int gridPos = WorldToGrid(hit.point);
        //         if (IsValidGridPosition(gridPos.x, gridPos.y))
        //         {
        //             Entity tileEntity = GetTileAt(gridPos.x, gridPos.y);
        //             if (tileEntity != null && tileEntity != _highlightedTile)
        //             {
        //                 // Unhighlight previous tile
        //                 if (_highlightedTile != null && _highlightedTile.HasComponent<GridTileComponent>())
        //                 {
        //                     var prevTileComponent = _highlightedTile.GetComponent<GridTileComponent>();
        //                     if (!prevTileComponent.IsSelected && !prevTileComponent.IsHighlighted)
        //                     {
        //                          GridManager.Instance.ResetTileColor(prevTileComponent.X, prevTileComponent.Z);
        //                     }
        //                 }
        //                 
        //                 // Highlight new tile
        //                 _highlightedTile = tileEntity;
        //                 var tileComponent = tileEntity.GetComponent<GridTileComponent>();
        //                 
        //                 if (!tileComponent.IsSelected && !tileComponent.IsHighlighted)
        //                 {
        //                      GridManager.Instance.HighlightTile(gridPos.x, gridPos.y, Color.white);
        //                 }
        //             }
        //         }
        //     }
        // }
        private void HandleMouseHover()
        {
            if (!GridManager.IsInstanceValid()) return;
            
            if (!Camera.main)
            {
                Debug.LogWarning("Main camera not found in HandleMouseHover");
                return;
            }
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (float.IsInfinity(ray.origin.x) || float.IsInfinity(ray.origin.y) || float.IsInfinity(ray.origin.z) ||
                float.IsInfinity(ray.direction.x) || float.IsInfinity(ray.direction.y) || float.IsInfinity(ray.direction.z))
            {
                return; // Bỏ qua nếu ray không hợp lệ
            }
            
            // Phần còn lại giữ nguyên
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                Vector2Int gridPos = WorldToGrid(hit.point);
                
                if (IsValidGridPosition(gridPos.x, gridPos.y))
                {
                    Entity tileEntity = GetTileAt(gridPos.x, gridPos.y);
                    if (tileEntity != null && tileEntity != _highlightedTile)
                    {
                        
                    }
                }
            }
        }
        
        /// <summary>
        /// Update visual representation of tiles
        /// </summary>
        private void UpdateTileVisuals()
        {
            if ( GridManager.Instance == null) return;
            
            // Update any tile visuals that need updating
            foreach (var pair in _tileEntities)
            {
                Entity tileEntity = _world.GetEntityById(pair.Value);
                if (tileEntity != null && tileEntity.HasComponent<GridTileComponent>())
                {
                    var tileComponent = tileEntity.GetComponent<GridTileComponent>();
                    
                    // Update occupancy
                    if (tileComponent.IsOccupied)
                    {
                         GridManager.Instance.SetCellOccupied(pair.Key, true);
                    }
                    else
                    {
                         GridManager.Instance.SetCellOccupied(pair.Key, false);
                    }
                }
            }
        }

        private void OnInitSingleton()
        {
            if (GridManager.IsInstanceValid())
            {
                _gridWidth =  GridManager.Instance._gridWidth;
                _gridHeight =  GridManager.Instance._gridHeight;
                _cellSize =  GridManager.Instance._cellSize;
            }
            
        }
    }
}