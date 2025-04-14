// Assets/Scripts/Core/Behaviors/ISteeringComponent.cs

using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Context object that provides environment information for steering behaviors
    /// </summary>
    public class SteeringContext
    {
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 Forward { get; set; }
        public float MaxSpeed { get; set; }
        public float MaxForce { get; set; }
        public Transform Target { get; set; }
        public LayerMask ObstacleLayer { get; set; }

        // Additional properties for specific behaviors
        public float SlowingRadius { get; set; }
        public float ArrivalRadius { get; set; }
        public float SeparationRadius { get; set; }
        public float NeighborRadius { get; set; }
        public float FleeRadius { get; set; }
    }

    /// <summary>
    /// Interface for all steering behavior components
    /// </summary>
    public interface ISteeringComponent
    {
        /// <summary>
        /// Calculate the steering force for this behavior
        /// </summary>
        /// <param name="context">Context providing environment information</param>
        /// <returns>Vector3 steering force</returns>
        Vector3 CalculateForce(SteeringContext context);
        
        /// <summary>
        /// Get the weight of this behavior for blending calculations
        /// </summary>
        /// <returns>Weight value between 0-1</returns>
        float GetWeight();
        
        /// <summary>
        /// Get the probability that this behavior will be active
        /// </summary>
        /// <returns>Probability value between 0-1</returns>
        float GetProbability();
        
        /// <summary>
        /// Name of this steering behavior
        /// </summary>
        string Name { get; }
    }
}