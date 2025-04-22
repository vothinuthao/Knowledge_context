using System.Collections.Generic;
using Core.Patterns.StrategyPattern;
using UnityEngine;

namespace SteeringBehavior
{
    public class CompositeSteeringBehavior : CompositeStrategy<SteeringContext, Vector3>
    {
        private float _maxForce = 10f;
        private int _priorityThreshold = 0;
        
        public CompositeSteeringBehavior() : base()
        {
        }
        
        // Set the maximum force magnitude
        public void SetMaxForce(float force)
        {
            _maxForce = force;
        }
        
        // Set the priority threshold
        public void SetPriorityThreshold(int threshold)
        {
            _priorityThreshold = threshold;
        }
    
        // ReSharper disable Unity.PerformanceAnalysis
        public Vector3 Execute(SteeringContext context)
        {
            return Execute(context, CombineForces);
        }
        
        // Get all strategies
        public List<ISteeringBehavior> GetStrategies()
        {
            return strategies.ConvertAll(s => (ISteeringBehavior)s);
        }
    
        // Combine forces method
        private Vector3 CombineForces(List<(Vector3, float)> weightedForces)
        {
            // First, check if we have any high-priority behaviors that should dominate
            bool hasHighPriorityBehaviors = false;
            int highestPriority = int.MinValue;
            
            foreach (var strategy in strategies)
            {
                if (strategy is ISteeringBehavior behavior && behavior.IsEnabled())
                {
                    int behaviorPriority = behavior.GetPriority();
                    highestPriority = Mathf.Max(highestPriority, behaviorPriority);
                    
                    if (behaviorPriority > _priorityThreshold)
                    {
                        hasHighPriorityBehaviors = true;
                    }
                }
            }
            
            Vector3 totalForce = Vector3.zero;
            
            // If we have high-priority behaviors, only include them
            if (hasHighPriorityBehaviors)
            {
                foreach (var (force, weight) in weightedForces)
                {
                    // Only include forces from high-priority behaviors
                    int index = weightedForces.IndexOf((force, weight));
                    
                    if (index < strategies.Count && strategies[index] is ISteeringBehavior behavior)
                    {
                        if (behavior.GetPriority() == highestPriority && behavior.IsEnabled())
                        {
                            totalForce += force * weight;
                        }
                    }
                }
            }
            else // Otherwise, include all behaviors with normal weights
            {
                foreach (var (force, weight) in weightedForces)
                {
                    totalForce += force * weight;
                }
            }
        
            // Limit maximum force
            if (totalForce.magnitude > _maxForce)
            {
                totalForce = totalForce.normalized * _maxForce;
            }
        
            return totalForce;
        }
    }
}