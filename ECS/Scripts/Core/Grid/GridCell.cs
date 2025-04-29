using UnityEngine;
using System;

namespace Core.Grid
{
    public class GridCell
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public Vector2Int Position { get { return new Vector2Int(X, Y); } }
        public bool IsWalkable { get; set; } = true;
        public bool IsOccupied { get; set; } = false;
        public int OccupyingEntityId { get; set; } = -1;
        
        // Visual state
        public bool IsHighlighted { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        
        private GridManager _gridManager;
        
        // Event for when cell is clicked
        public event Action<GridCell> OnCellClicked;
        
        public GridCell(int x, int y, GridManager gridManager)
        {
            X = x;
            Y = y;
            _gridManager = gridManager;
            IsWalkable = true;
            IsOccupied = false;
            OccupyingEntityId = -1;
        }
        
        public Vector3 GetWorldPosition()
        {
            return _gridManager.GetCellCenter(Position);
        }
        
        /// <summary>
        /// Set this cell as occupied by an entity
        /// </summary>
        public void SetOccupied(int entityId)
        {
            IsOccupied = true;
            OccupyingEntityId = entityId;
            
            // Update GridManager
            _gridManager.SetCellOccupied(Position, true);
        }
        
        /// <summary>
        /// Clear occupation state
        /// </summary>
        public void ClearOccupied()
        {
            IsOccupied = false;
            OccupyingEntityId = -1;
            
            // Update GridManager
            _gridManager.SetCellOccupied(Position, false);
        }
        
        /// <summary>
        /// Trigger click event
        /// </summary>
        public void Click()
        {
            OnCellClicked?.Invoke(this);
        }
        
        /// <summary>
        /// Highlight this cell with a color
        /// </summary>
        public void Highlight(Color color)
        {
            IsHighlighted = true;
            _gridManager.HighlightTile(X, Y, color);
        }
        
        /// <summary>
        /// Reset this cell's visual state
        /// </summary>
        public void ResetVisual()
        {
            IsHighlighted = false;
            IsSelected = false;
            _gridManager.ResetTileColor(X, Y);
        }
    }
}