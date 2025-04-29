using UnityEngine;
using System.Collections.Generic;
using Core.ECS;
using Core.Singleton;
using Core.DI;

namespace Core.Grid
{
    public class GridManager : ManualSingletonMono<GridManager>
    {
        [Header("Grid Settings")]
        [SerializeField] public int _gridWidth = 20;
        [SerializeField] public int _gridHeight = 20;
        [SerializeField] public float _cellSize = 3.0f;
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] private Transform gridParent;
        
        [Header("Materials")]
        [SerializeField] private Material defaultTileMaterial;
        [SerializeField] private Material highlightTileMaterial;
        [SerializeField] private Material selectedTileMaterial;
        [SerializeField] private Material blockedTileMaterial;
        [SerializeField] private float tileHeight = 0.1f;
        
        private GridCell[,] _grid;
        private Dictionary<Vector2Int, Entity> _occupiedCells = new Dictionary<Vector2Int, Entity>();
        private Dictionary<Vector2Int, GameObject> _tileVisuals = new Dictionary<Vector2Int, GameObject>();
        
        // private World _world;
        
        protected override void Awake()
        {
            base.Awake();
            // _world = ServiceLocator.Get<World>();
        }
        
        private void Start()
        {
            Invoke(nameof(DelayedStart), 0.1f);
        }
        
        private void DelayedStart()
        {
            InitializeGrid();
        }
        
        public void InitializeGrid()
        {
            _grid = new GridCell[_gridWidth, _gridHeight];
            _occupiedCells = new Dictionary<Vector2Int, Entity>();
    
            if (gridParent)
            {
                for (int i = gridParent.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(gridParent.GetChild(i).gameObject);
                }
            }
            else
            {
                gridParent = new GameObject("GridVisuals").transform;
                gridParent.SetParent(transform);
            }
    
            float halfCellSize = _cellSize * 0.5f;
    
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int z = 0; z < _gridHeight; z++)
                {
                    // Tạo grid cell
                    _grid[x, z] = new GridCell(x, z, this);
            
                    Vector3 position = new Vector3(
                        x * _cellSize + halfCellSize, 
                        0, 
                        z * _cellSize + halfCellSize
                    );
            
                    CreateTileVisual(x, z, position);
                }
            }
    
            Debug.Log($"Grid initialized successfully: {_gridWidth}x{_gridHeight} cells");
        }
        
        /// <summary>
        /// Create a visual representation for a grid cell
        /// </summary>
        public GameObject CreateTileVisual(int x, int z, Vector3 position)
        {
            Vector2Int gridPos = new Vector2Int(x, z);
    
            GameObject tileObj;
            if (tilePrefab != null)
            {
                // Sử dụng prefab
                tileObj = Instantiate(tilePrefab, position, Quaternion.identity, gridParent);
            }
            else
            {
                tileObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tileObj.transform.position = position;
                tileObj.transform.localScale = new Vector3(_cellSize * 0.9f, tileHeight, _cellSize * 0.9f);
                tileObj.transform.SetParent(gridParent);
        
                Renderer renderer = tileObj.GetComponent<Renderer>();
                renderer.material = defaultTileMaterial;
            }
    
            tileObj.name = $"Tile_{x}_{z}";
    
            // Thêm CellVisual component và khởi tạo
            CellVisual cellVisual = tileObj.GetComponent<CellVisual>();
            if (cellVisual == null)
            {
                cellVisual = tileObj.AddComponent<CellVisual>();
            }
            cellVisual.SetGridCoordinates(x, z);
    
            // Thiết lập Layer
            tileObj.layer = LayerMask.NameToLayer("Tile");
    
            return tileObj;
        }
        
        /// <summary>
        /// Highlight a tile with a specific color
        /// </summary>
        public void HighlightTile(int x, int z, Color color)
        {
            Vector2Int gridPos = new Vector2Int(x, z);
            
            if (_tileVisuals.TryGetValue(gridPos, out GameObject tileObj))
            {
                Renderer renderer = tileObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = highlightTileMaterial;
                }
            }
        }
        public void SelectTile(int x, int z)
        {
            Vector2Int gridPos = new Vector2Int(x, z);
            
            if (_tileVisuals.TryGetValue(gridPos, out GameObject tileObj))
            {
                Renderer renderer = tileObj.GetComponent<Renderer>();
                if (renderer != null && selectedTileMaterial != null)
                {
                    renderer.material = selectedTileMaterial;
                }
            }
        }
        /// <summary>
        /// Reset a tile's color to default
        /// </summary>
        public void ResetTileColor(int x, int z)
        {
            Vector2Int gridPos = new Vector2Int(x, z);
            
            if (_tileVisuals.TryGetValue(gridPos, out GameObject tileObj))
            {
                Renderer renderer = tileObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = defaultTileMaterial;
                }
            }
        }
        
        /// <summary>
        /// Reset all tiles to default color
        /// </summary>
        public void ResetAllTiles()
        {
            foreach (var tileObj in _tileVisuals.Values)
            {
                Renderer renderer = tileObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material = defaultTileMaterial;
                }
            }
        }
        
        public Vector3 GetCellCenter(Vector2Int cellPosition)
        {
            return new Vector3(
                cellPosition.x * _cellSize + _cellSize * 0.5f,
                0,
                cellPosition.y * _cellSize + _cellSize * 0.5f
            );
        }
        
        public Vector2Int GetGridCoordinates(Vector3 worldPosition)
        {
            int x = Mathf.FloorToInt(worldPosition.x / _cellSize);
            int y = Mathf.FloorToInt(worldPosition.z / _cellSize);
            return new Vector2Int(x, y);
        }
        
        public bool IsValidCell(Vector2Int cellPosition)
        {
            return cellPosition.x >= 0 && cellPosition.x < _gridWidth &&
                   cellPosition.y >= 0 && cellPosition.y < _gridHeight;
        }
        
        public bool IsCellOccupied(Vector2Int cellPosition)
        {
            return _occupiedCells.ContainsKey(cellPosition);
        }
        
        public void SetCellOccupied(Vector2Int cellPosition, bool occupied, Entity entity = null)
        {
            if (occupied)
            {
                _occupiedCells[cellPosition] = entity;
            }
            else
            {
                _occupiedCells.Remove(cellPosition);
            }
        }
        
        public GridCell GetCell(Vector2Int cellPosition)
        {
            if (IsValidCell(cellPosition))
            {
                return _grid[cellPosition.x, cellPosition.y];
            }
            return null;
        }
        
        /// <summary>
        /// Get neighbor cells of a specific cell
        /// </summary>
        public List<GridCell> GetNeighbors(int x, int y)
        {
            List<GridCell> neighbors = new List<GridCell>();
            
            // Check all 4 directions
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1)
            };
            
            foreach (var dir in directions)
            {
                Vector2Int checkPos = new Vector2Int(x + dir.x, y + dir.y);
                if (IsValidCell(checkPos))
                {
                    neighbors.Add(_grid[checkPos.x, checkPos.y]);
                }
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// Get world position from grid coordinates
        /// </summary>
        public Vector3 GridToWorld(int x, int z)
        {
            return new Vector3(x * _cellSize + _cellSize * 0.5f, 0, z * _cellSize + _cellSize * 0.5f);
        }
    }
}