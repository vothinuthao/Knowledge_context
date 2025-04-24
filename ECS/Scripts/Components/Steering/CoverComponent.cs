using Core.ECS;
using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Component for cover behavior (stay behind allies with protect behavior)
    /// </summary>
    public class CoverComponent : IComponent
    {
        // Weight of this behavior in the steering calculation
        public float Weight { get; set; } = 1.0f;
        
        // Whether this behavior is enabled
        public bool IsEnabled { get; set; } = true;
        
        // Distance to stay behind protector
        public float CoverDistance { get; set; } = 2.0f;
        
        // Speed when repositioning for cover
        public float PositioningSpeed { get; set; } = 4.0f;
        
        public CoverComponent(float weight = 1.0f, float coverDistance = 2.0f, float positioningSpeed = 4.0f)
        {
            Weight = weight;
            CoverDistance = coverDistance;
            PositioningSpeed = positioningSpeed;
        }
    }
}