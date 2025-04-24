using Core.ECS;
using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Component for surround behavior (encircle enemies)
    /// </summary>
    public class SurroundComponent : IComponent
    {
        // Weight of this behavior in the steering calculation
        public float Weight { get; set; } = 1.0f;
        
        // Whether this behavior is enabled
        public bool IsEnabled { get; set; } = true;
        
        // Radius to maintain when surrounding
        public float SurroundRadius { get; set; } = 5.0f;
        
        // Speed when repositioning for surrounding
        public float SurroundSpeed { get; set; } = 3.0f;
        
        public SurroundComponent(float weight = 1.0f, float surroundRadius = 5.0f, float surroundSpeed = 3.0f)
        {
            Weight = weight;
            SurroundRadius = surroundRadius;
            SurroundSpeed = surroundSpeed;
        }
    }
}