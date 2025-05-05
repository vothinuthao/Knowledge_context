using Core.ECS;
using UnityEngine;

namespace Components
{
    /// <summary>
    /// Component for entities representing grid tiles
    /// </summary>
    public class GridTileComponent : IComponent
    {
        // Grid coordinates
        public int X { get; set; }
        public int Z { get; set; }
        
        // Tile state
        public bool IsWalkable { get; set; } = true;
        public bool IsOccupied { get; set; } = false;
        public int OccupyingEntityId { get; set; } = -1;
        
        // Visual properties
        public Color DefaultColor { get; set; } = new Color(0.3f, 0.3f, 0.3f, 1f);
        public Color HighlightColor { get; set; } = new Color(0.3f, 0.7f, 0.3f, 1f);
        public Color SelectedColor { get; set; } = new Color(0.7f, 0.3f, 0.3f, 1f);
        public bool IsHighlighted { get; set; } = false;
        public bool IsSelected { get; set; } = false;
        
        public GridTileComponent(int x, int z)
        {
            X = x;
            Z = z;
        }
        
        /// <summary>
        /// Set the tile as occupied by an entity
        /// </summary>
        public void SetOccupied(int entityId)
        {
            IsOccupied = true;
            OccupyingEntityId = entityId;
        }
        
        /// <summary>
        /// Clear the occupied status
        /// </summary>
        public void ClearOccupied()
        {
            IsOccupied = false;
            OccupyingEntityId = -1;
        }
    }
}