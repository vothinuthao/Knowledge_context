using Core.ECS;

namespace Steering
{
    /// <summary>
    /// Component for separation steering behavior
    /// </summary>
    public class SeparationComponent : IComponent
    {
        public float Weight { get; set; } = 2.0f; 
        public bool IsEnabled { get; set; } = true;
        public float SeparationRadius { get; set; } = 2.0f;
        
        public SeparationComponent(float weight = 2.0f, float separationRadius = 2.0f)
        {
            Weight = weight;
            SeparationRadius = separationRadius;
        }
    }
}