using Core.ECS;
using UnityEngine;

namespace Movement
{
    /// <summary>
    /// Component for entity position
    /// </summary>
    public class PositionComponent : IComponent
    {
        public Vector3 Position { get; set; }
        public Vector3 LastPosition { get; set; }
        
        public PositionComponent(Vector3 position)
        {
            Position = position;
            LastPosition = position;
        }
        
        /// <summary>
        /// Updates the last position to the current position
        /// </summary>
        public void UpdateLastPosition()
        {
            LastPosition = Position;
        }
        
        /// <summary>
        /// Check if the entity has moved since last update
        /// </summary>
        public bool HasMoved(float threshold = 0.001f)
        {
            return Vector3.Distance(Position, LastPosition) > threshold;
        }
    }
}