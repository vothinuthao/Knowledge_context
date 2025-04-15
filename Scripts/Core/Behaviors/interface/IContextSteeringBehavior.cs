using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Interface for behaviors that support context-based steering
    /// </summary>
    public interface IContextSteeringBehavior
    {
        /// <summary>
        /// Fill the interest and danger maps based on this behavior
        /// </summary>
        /// <param name="context">The steering context with maps</param>
        /// <param name="weight">The weight factor for this behavior</param>
        void FillContextMaps(SteeringContext context, float weight);
    }
}