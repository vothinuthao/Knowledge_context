using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Interface for behaviors that support context configuration
    /// </summary>
    public interface IContextConfigurable
    {
        /// <summary>
        /// Configure context-specific settings
        /// </summary>
        void ConfigureContextSettings(
            float interestStrength,
            float dangerStrength,
            float directionalBias,
            AnimationCurve directionFalloff,
            bool useDynamicWeight,
            float minWeight,
            float maxWeight,
            AnimationCurve weightByDistance
        );
    }
}