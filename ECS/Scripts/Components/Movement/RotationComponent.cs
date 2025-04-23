using Core.ECS;
using UnityEngine;

namespace Movement
{
    /// <summary>
    /// Component for entity rotation
    /// </summary>
    public class RotationComponent : IComponent
    {
        public Quaternion Rotation { get; set; } = Quaternion.identity;
        public float RotationSpeed { get; set; } = 10.0f;
        
        public RotationComponent()
        {
        }
        
        public RotationComponent(Quaternion rotation, float rotationSpeed)
        {
            Rotation = rotation;
            RotationSpeed = rotationSpeed;
        }
    }
}