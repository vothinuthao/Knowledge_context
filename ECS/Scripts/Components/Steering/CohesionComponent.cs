using Core.ECS;
using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Component for cohesion steering behavior (move toward center of nearby allies)
    /// </summary>
    public class CohesionComponent : IComponent
    {
        // Weight of this behavior in the steering calculation
        public float Weight { get; set; } = 1.0f;
        
        // Whether this behavior is enabled
        public bool IsEnabled { get; set; } = true;
        
        // Radius for considering nearby allies for cohesion
        public float CohesionRadius { get; set; } = 5.0f;
        
        public CohesionComponent(float weight = 1.0f, float cohesionRadius = 5.0f)
        {
            Weight = weight;
            CohesionRadius = cohesionRadius;
        }
    }
}