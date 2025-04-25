using UnityEngine;

namespace Core.Grid
{
    public class GridCell
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public Vector2Int Position { get; private set; }
        public bool IsWalkable { get; set; } = true;
        public bool IsOccupied { get; set; } = false;
        
        private GridManager _gridManager;
        
        public GridCell(int x, int y, GridManager gridManager)
        {
            X = x;
            Y = y;
            Position = new Vector2Int(x, y);
            _gridManager = gridManager;
        }
        
        public Vector3 GetWorldPosition()
        {
            return _gridManager.GetCellCenter(Position);
        }
    }
}