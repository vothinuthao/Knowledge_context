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
        [Header("Specific Parameters")]
        public float slowingRadius = 3.0f;
        public float arrivalRadius = 0.5f;
        public float separationRadius = 2.0f;
        public float neighborRadius = 5.0f;
        public float fleeRadius = 5.0f;
        
        [Header("Context Steering Settings")]
        public float interestStrength = 1.0f;
        public float dangerStrength = 1.0f;
        public float directionalBias = 0.5f;
        public AnimationCurve directionFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        [Header("Dynamic Weight Adjustments")]
        public bool useDynamicWeight = true;
        public float minWeight = 0.5f;
        public float maxWeight = 2.0f;
        public AnimationCurve weightByDistance = AnimationCurve.Linear(0, 2, 1, 0.5f);
        
        /// <summary>
        /// Create an instance of the appropriate steering behavior
        /// </summary>
        public ISteeringComponent CreateBehavior()
        {
            ISteeringComponent behavior = null;
            switch (behaviorType)
            {
                case BehaviorType.Seek:
                    behavior = new ContextSeekBehavior();
                    break;
                case BehaviorType.Flee:
                    behavior = new ContextFleeBehavior();
                    break;
                case BehaviorType.Arrival:
                    behavior = new ContextArrivalBehavior();
                    break;
                case BehaviorType.Separation:
                    behavior = new ContextSeparationBehavior();
                    break;
                case BehaviorType.Cohesion:
                    behavior = new ContextCohesionBehavior();
                    break;
                case BehaviorType.Alignment:
                    behavior = new ContextAlignmentBehavior();
                    break;
                case BehaviorType.ObstacleAvoidance:
                    behavior = new ContextObstacleAvoidanceBehavior();
                    break;
                case BehaviorType.PathFollowing:
                    return CreateBehavior();
                default:
                    return CreateBehavior();
            }
            if (behavior is IContextConfigurable contextBehavior)
            {
                contextBehavior.ConfigureContextSettings(
                    interestStrength, 
                    dangerStrength, 
                    directionalBias,
                    directionFalloff,
                    useDynamicWeight,
                    minWeight,
                    maxWeight,
                    weightByDistance
                );
            }
            
            var behaviorBase = behavior as SteeringBehaviorBase;
            typeof(SteeringBehaviorBase)
                .GetField("weight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(behaviorBase, weight);
                
            typeof(SteeringBehaviorBase)
                .GetField("probability", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(behaviorBase, probability);

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
            
            if (context.InterestMap == null || context.DangerMap == null)
            {
                context.InitializeMaps();
            }
        }
    }
}