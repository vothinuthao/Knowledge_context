using Core.ECS;
using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Component for flee steering behavior (move away from target)
    /// </summary>
    public class FleeComponent : IComponent
    {
        // Weight of this behavior in the steering calculation
        public float Weight { get; set; } = 1.0f;
        
        // Whether this behavior is enabled
        public bool IsEnabled { get; set; } = true;
        
        // Distance at which to start fleeing
        public float PanicDistance { get; set; } = 5.0f;
        
        public FleeComponent(float weight = 1.0f, float panicDistance = 5.0f)
        {
            Weight = weight;
            PanicDistance = panicDistance;
        }
    }
}