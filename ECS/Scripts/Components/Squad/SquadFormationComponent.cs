using Core.ECS;
using UnityEngine;

namespace Squad
{
    /// <summary>
    /// Component for squad formation data
    /// </summary>
    public class SquadFormationComponent : IComponent
    {
        // Number of rows and columns in the formation
        public int Rows { get; set; }
        public int Columns { get; set; }
        
        // Spacing between troops in the formation
        public float Spacing { get; set; }
        
        // Occupied positions in the grid
        public bool[,] OccupiedPositions { get; private set; }
        
        // World positions corresponding to grid positions
        public Vector3[,] WorldPositions { get; private set; }
        
        public SquadFormationComponent(int rows, int columns, float spacing)
        {
            Rows = rows;
            Columns = columns;
            Spacing = spacing;
            
            OccupiedPositions = new bool[rows, columns];
            WorldPositions = new Vector3[rows, columns];
        }
        
        /// <summary>
        /// Find an empty position in the formation
        /// </summary>
        public Vector2Int FindEmptyPosition()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    if (!OccupiedPositions[row, col])
                    {
                        return new Vector2Int(row, col);
                    }
                }
            }
            
            return new Vector2Int(-1, -1); // No empty position found
        }
        
        /// <summary>
        /// Calculate the local position for a grid position
        /// </summary>
        public Vector3 CalculateLocalPosition(int row, int col)
        {
            float xOffset = (col - (Columns - 1) / 2.0f) * Spacing;
            float zOffset = (row - (Rows - 1) / 2.0f) * Spacing;
            
            return new Vector3(xOffset, 0, zOffset);
        }
        
        /// <summary>
        /// Set a position as occupied
        /// </summary>
        public void SetPositionOccupied(int row, int col, bool occupied)
        {
            if (row >= 0 && row < Rows && col >= 0 && col < Columns)
            {
                OccupiedPositions[row, col] = occupied;
            }
        }
        
        /// <summary>
        /// Update world positions based on squad position and rotation
        /// </summary>
        public void UpdateWorldPositions(Vector3 squadPosition, Quaternion squadRotation)
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    Vector3 localPos = CalculateLocalPosition(row, col);
                    Vector3 worldPos = squadPosition + squadRotation * localPos;
                    
                    // Store the world position
                    WorldPositions[row, col] = worldPos;
                }
            }
        }
    }
}