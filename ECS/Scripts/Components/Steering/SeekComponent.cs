using Core.ECS;

namespace Steering
{
    /// <summary>
    /// Component for seek steering behavior
    /// </summary>
    public class SeekComponent : IComponent
    {
        public float Weight { get; set; } = 1.0f;
        public bool IsEnabled { get; set; } = true;
        
        public SeekComponent(float weight = 1.0f)
        {
            Weight = weight;
        }
    }
}