using Core.ECS;
using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Component for obstacle avoidance steering behavior
    /// </summary>
    public class ObstacleAvoidanceComponent : IComponent
    {
        // Weight of this behavior in the steering calculation
        public float Weight { get; set; } = 1.5f;
        
        // Whether this behavior is enabled
        public bool IsEnabled { get; set; } = true;
        
        // Distance to keep from obstacles
        public float AvoidDistance { get; set; } = 2.0f;
        
        // How far ahead to look for obstacles
        public float LookAheadDistance { get; set; } = 3.0f;
        
        public ObstacleAvoidanceComponent(float weight = 1.5f, float avoidDistance = 2.0f, float lookAheadDistance = 3.0f)
        {
            Weight = weight;
            AvoidDistance = avoidDistance;
            LookAheadDistance = lookAheadDistance;
        }
    }
}