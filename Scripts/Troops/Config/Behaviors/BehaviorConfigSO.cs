// Assets/Scripts/Troops/Config/BehaviorConfigSO.cs

using System;
using System.Collections.Generic;
using Core.Behaviors;
using UnityEngine;

namespace Troops.Config
{
    /// <summary>
    /// Base class for behavior configuration scriptable objects
    /// </summary>
    [CreateAssetMenu(fileName = "BehaviorConfig", menuName = "WikingRaven/BehaviorConfig")]
    public class BehaviorConfigSO : ScriptableObject
    {
        [Header("General Settings")]
        public string behaviorName;
        public float weight = 1.0f;
        public float probability = 1.0f;
        public BehaviorType behaviorType;
        
        [Header("Behavior Vectors")]
        public List<SerializableVectorProbability> steeringVectors = new List<SerializableVectorProbability>();
        
        [Header("Specific Parameters")]
        public float slowingRadius = 3.0f;
        public float arrivalRadius = 0.5f;
        public float separationRadius = 2.0f;
        public float neighborRadius = 5.0f;
        public float fleeRadius = 5.0f;
        public float lookAheadDistance = 2.0f;
        
        /// <summary>
        /// Create an instance of the appropriate steering behavior
        /// </summary>
        public ISteeringComponent CreateBehavior()
        {
            ISteeringComponent behavior = null;
            
            switch (behaviorType)
            {
                case BehaviorType.Seek:
                    behavior = new SeekBehavior();
                    break;
                case BehaviorType.Flee:
                    behavior = new FleeBehavior();
                    break;
                case BehaviorType.Arrival:
                    behavior = new ArrivalBehavior();
                    break;
                case BehaviorType.Separation:
                    behavior = new SeparationBehavior();
                    break;
                case BehaviorType.Cohesion:
                    behavior = new CohesionBehavior();
                    break;
                case BehaviorType.Alignment:
                    behavior = new AlignmentBehavior();
                    break;
                case BehaviorType.ObstacleAvoidance:
                    behavior = new ObstacleAvoidanceBehavior();
                    break;
                case BehaviorType.PathFollowing:
                    behavior = new PathFollowingBehavior();
                    break;
                default:
                    Debug.LogError($"Unknown behavior type: {behaviorType}");
                    return null;
            }
            var behaviorBase = behavior as SteeringBehaviorBase;
            behaviorBase.ClearSteeringVectors();
            foreach (var vectorProb in steeringVectors)
            {
                behaviorBase.AddSteeringVector(vectorProb.direction, vectorProb.probability);
            }

            return behavior;
        }
        
        /// <summary>
        /// Apply this behavior configuration to an existing SteeringContext
        /// </summary>
        public void ApplyToContext(SteeringContext context)
        {
            context.SlowingRadius = slowingRadius;
            context.ArrivalRadius = arrivalRadius;
            context.SeparationRadius = separationRadius;
            context.NeighborRadius = neighborRadius;
            context.FleeRadius = fleeRadius;
        }
    }
}