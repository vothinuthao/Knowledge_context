using Core.ECS;
using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Component for arrival steering behavior (slow down when approaching target)
    /// </summary>
    public class ArrivalComponent : IComponent
    {
        // Weight of this behavior in the steering calculation
        public float Weight { get; set; } = 1.0f;
        
        // Whether this behavior is enabled
        public bool IsEnabled { get; set; } = true;
        
        // Distance at which to start slowing down
        public float SlowingDistance { get; set; } = 3.0f;
        
        public ArrivalComponent(float weight = 1.0f, float slowingDistance = 3.0f)
        {
            Weight = weight;
            SlowingDistance = slowingDistance;
        }
    }
}