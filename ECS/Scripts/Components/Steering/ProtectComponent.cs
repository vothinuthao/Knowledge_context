using Core.ECS;
using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Component for protect behavior (defend allies from enemies)
    /// </summary>
    public class ProtectComponent : IComponent
    {
        // Weight of this behavior in the steering calculation
        public float Weight { get; set; } = 2.0f;
        
        // Whether this behavior is enabled
        public bool IsEnabled { get; set; } = true;
        
        // Radius to stay near protected allies
        public float ProtectRadius { get; set; } = 3.0f;
        
        // Speed when repositioning to protect
        public float PositioningSpeed { get; set; } = 5.0f;
        
        // Tags of entities to prioritize protecting
        public string[] ProtectedTags { get; set; } = new string[] { "Player" };
        
        public ProtectComponent(float weight = 2.0f, float protectRadius = 3.0f, float positioningSpeed = 5.0f)
        {
            Weight = weight;
            ProtectRadius = protectRadius;
            PositioningSpeed = positioningSpeed;
        }
    }
}