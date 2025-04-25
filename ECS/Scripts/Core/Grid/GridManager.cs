using UnityEngine;
using System.Collections.Generic;
using Core.ECS;
using Core.Singleton;

namespace Core.Grid
{
    public class GridManager : ManualSingletonMono<GridManager>
    {
        
        [SerializeField] private int _gridWidth = 20;
        [SerializeField] private int _gridHeight = 20;
        [SerializeField] private float _cellSize = 3.0f;
        
        private GridCell[,] _grid;
        private Dictionary<Vector2Int, Entity> _occupiedCells;

        protected override void Awake()
        {
            base.Awake();
        }
        
        public void InitializeGrid()
        {
            _grid = new GridCell[_gridWidth, _gridHeight];
            _occupiedCells = new Dictionary<Vector2Int, Entity>();
            
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    _grid[x, y] = new GridCell(x, y, this);
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
    }
}