using System.Collections.Generic;
using Core.Patterns.StrategyPattern;
using UnityEngine;

namespace SteeringBehavior
{
    /// <summary>
    /// Enhanced version of the CompositeSteeringBehavior class with better debugging
    /// and more reliable behavior combination
    /// </summary>
    public class CompositeSteeringBehavior : CompositeStrategy<SteeringContext, Vector3>
    {
        private float _maxForce = 10f;
        private int _priorityThreshold = 1;
        
        // Debug information
        public List<(string behaviorName, Vector3 force, float weight, int priority, bool enabled)> LastForces { get; private set; } 
            = new List<(string, Vector3, float, int, bool)>();
        
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
    
        public Vector3 Execute(SteeringContext context)
        {
            return Execute(context, CombineForces);
        }
        
        // Get all strategies as steering behaviors
        public List<ISteeringBehavior> GetSteeringBehaviors()
        {
            return strategies.ConvertAll(s => (ISteeringBehavior)s);
        }
    
        // Enhanced force combination method with better prioritization and debugging
        private Vector3 CombineForces(List<(Vector3 force, float weight)> weightedForces)
        {
            LastForces.Clear();
            
            // First, check if we have any high-priority behaviors
            int highestPriority = int.MinValue;
            bool hasHighPriorityBehaviors = false;
            
            for (int i = 0; i < strategies.Count; i++)
            {
                if (strategies[i] is ISteeringBehavior behavior)
                {
                    int priority = behavior.GetPriority();
                    
                    // Add to debug info
                    if (i < weightedForces.Count)
                    {
                        LastForces.Add((
                            behavior.GetName(),
                            weightedForces[i].force,
                            behavior.GetWeight(),
                            priority,
                            behavior.IsEnabled()
                        ));
                    }
                    
                    // Skip disabled behaviors
                    if (!behavior.IsEnabled()) continue;
                    
                    highestPriority = Mathf.Max(highestPriority, priority);
                    
                    if (priority > _priorityThreshold)
                    {
                        hasHighPriorityBehaviors = true;
                    }
                }
            }
            
            // Track whether we used priority-based selection
            bool usedPrioritySelection = false;
            
            Vector3 totalForce = Vector3.zero;
            int forcesUsed = 0;
            
            // If we have high-priority behaviors, only consider those with the highest priority
            if (hasHighPriorityBehaviors)
            {
                usedPrioritySelection = true;
                
                for (int i = 0; i < strategies.Count; i++)
                {
                    if (i >= weightedForces.Count) break;
                    
                    if (strategies[i] is ISteeringBehavior behavior && 
                        behavior.IsEnabled() && 
                        behavior.GetPriority() >= highestPriority)
                    {
                        var (force, weight) = weightedForces[i];
                        if (force.magnitude > 0.001f)  // Only include non-zero forces
                        {
                            totalForce += force * weight;
                            forcesUsed++;
                        }
                    }
                }
            }
            else // Otherwise combine all forces normally
            {
                for (int i = 0; i < weightedForces.Count; i++)
                {
                    if (i >= strategies.Count) break;
                    
                    if (strategies[i] is ISteeringBehavior behavior && behavior.IsEnabled())
                    {
                        var (force, weight) = weightedForces[i];
                        if (force.magnitude > 0.001f)  // Only include non-zero forces
                        {
                            totalForce += force * weight;
                            forcesUsed++;
                        }
                    }
                }
            }
            
            // Log if using priority selection produced no forces
            if (usedPrioritySelection && forcesUsed == 0 && hasHighPriorityBehaviors)
            {
                Debug.LogWarning($"High priority behaviors ({highestPriority}) produced no forces. Falling back to all behaviors.");
                
                // Fall back to using all behaviors
                totalForce = Vector3.zero;
                for (int i = 0; i < weightedForces.Count; i++)
                {
                    if (i >= strategies.Count) break;
                    
                    if (strategies[i] is ISteeringBehavior behavior && behavior.IsEnabled())
                    {
                        var (force, weight) = weightedForces[i];
                        if (force.magnitude > 0.001f)
                        {
                            totalForce += force * weight;
                            forcesUsed++;
                        }
                    }
                }
            }
            
            // Limit maximum force magnitude
            if (totalForce.magnitude > _maxForce)
            {
                totalForce = totalForce.normalized * _maxForce;
            }
            
            // If no forces, ensure we return exactly zero
            if (forcesUsed == 0)
            {
                return Vector3.zero;
            }
            
            return totalForce;
        }
        
        // Get debug information about the last forces calculated
        public string GetDebugInfo()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("Behavior Forces:");
            
            foreach (var (name, force, weight, priority, enabled) in LastForces)
            {
                if (enabled)
                {
                    sb.AppendLine($"- {name}: Mag={force.magnitude:F2}, Pri={priority}, Wt={weight:F1}, En=Yes");
                }
                else
                {
                    sb.AppendLine($"- {name}: DISABLED");
                }
            }
            
            return sb.ToString();
        }
    }
    
}