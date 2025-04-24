using Core.ECS;
using UnityEngine;

namespace Steering
{
    /// <summary>
    /// Component for alignment steering behavior (movement in the average direction of neighbors)
    /// </summary>
    public class AlignmentComponent : IComponent
    {
        // Weight of this behavior in the steering calculation
        public float Weight { get; set; } = 1.0f;
        
        // Whether this behavior is enabled
        public bool IsEnabled { get; set; } = true;
        
        // Radius for considering nearby allies for alignment
        public float AlignmentRadius { get; set; } = 5.0f;
        
        public AlignmentComponent(float weight = 1.0f, float alignmentRadius = 5.0f)
        {
            Weight = weight;
            AlignmentRadius = alignmentRadius;
        }
    }
}