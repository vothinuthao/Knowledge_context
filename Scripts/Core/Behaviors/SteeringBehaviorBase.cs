// Assets/Scripts/Core/Behaviors/SteeringBehaviorBase.cs

using System.Collections.Generic;
using UnityEngine;

namespace Core.Behaviors
{
    /// <summary>
    /// Base class for all steering behaviors
    /// </summary>
    [System.Serializable]
    public abstract class SteeringBehaviorBase : ISteeringComponent
    {
        [SerializeField] protected float weight = 1.0f;
        [SerializeField] protected float probability = 1.0f;
        [SerializeField] protected string behaviorName;
        [SerializeField] protected List<VectorProbability> steeringVectors = new List<VectorProbability>();

        public float GetWeight() => weight;
        public float GetProbability() => probability;
        public string Name => behaviorName;

        /// <summary>
        /// Calculate the steering force based on the current context
        /// </summary>
        public virtual Vector3 CalculateForce(SteeringContext context)
        {
            if (steeringVectors.Count > 0)
            {
                return SelectVectorByProbability();
            }
            return CalculateSteeringForce(context);
        }

        /// <summary>
        /// Behavior-specific implementation to calculate the steering force
        /// </summary>
        protected abstract Vector3 CalculateSteeringForce(SteeringContext context);

        /// <summary>
        /// Selects a vector from the list of possible steering vectors based on probability
        /// </summary>
        protected Vector3 SelectVectorByProbability()
        {
            NormalizeProbabilities();
            float random = Random.value;
            float cumulativeProbability = 0f;
            
            foreach (var vectorProb in steeringVectors)
            {
                cumulativeProbability += vectorProb.probability;
                if (random <= cumulativeProbability)
                {
                    return vectorProb.direction;
                }
            }
            return steeringVectors[^1].direction;
        }

        /// <summary>
        /// Normalizes the probabilities of all vectors to ensure they sum to 1
        /// </summary>
        protected void NormalizeProbabilities()
        {
            float sum = 0f;
            foreach (var vectorProb in steeringVectors)
            {
                sum += vectorProb.probability;
            }
            
            if (sum <= 0f) return;
            
            for (int i = 0; i < steeringVectors.Count; i++)
            {
                var vp = steeringVectors[i];
                vp.probability /= sum;
                steeringVectors[i] = vp;
            }
        }
    }

    /// <summary>
    /// Structure to store a direction vector and its probability
    /// </summary>
    [System.Serializable]
    public struct VectorProbability
    {
        public Vector3 direction;
        public float probability;

        public VectorProbability(Vector3 direction, float probability)
        {
            this.direction = direction;
            this.probability = probability;
        }
    }
}