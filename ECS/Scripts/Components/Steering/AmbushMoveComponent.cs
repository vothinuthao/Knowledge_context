using Core.ECS;
using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Component for ambush move behavior (stealthy movement)
    /// </summary>
    public class AmbushMoveComponent : IComponent
    {
        // Weight of this behavior in the steering calculation
        public float Weight { get; set; } = 1.0f;
        
        // Whether this behavior is enabled
        public bool IsEnabled { get; set; } = true;
        
        // Speed multiplier when in ambush mode (slower)
        public float MoveSpeedMultiplier { get; set; } = 0.5f;
        
        // Detection radius multiplier when in ambush mode (harder to detect)
        public float DetectionRadiusMultiplier { get; set; } = 0.5f;
        
        public AmbushMoveComponent(float weight = 1.0f, float moveSpeedMultiplier = 0.5f, float detectionRadiusMultiplier = 0.5f)
        {
            Weight = weight;
            MoveSpeedMultiplier = moveSpeedMultiplier;
            DetectionRadiusMultiplier = detectionRadiusMultiplier;
        }
    }
}