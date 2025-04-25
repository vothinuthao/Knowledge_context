namespace Components
{
    using Core.ECS;
    using UnityEngine;

    public class PositionComponent : IComponent
    {
        public Vector3 Position { get; set; }
        public Vector3 LastPosition { get; set; }
        
        public PositionComponent(Vector3 position)
        {
            Position = position;
            LastPosition = position;
        }
        
        public void UpdateLastPosition()
        {
            LastPosition = Position;
        }
        
        public bool HasMoved(float threshold = 0.001f)
        {
            return Vector3.Distance(Position, LastPosition) > threshold;
        }
    }
}