using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Systems.Behavior
{
    /// <summary>
    /// Manages behavior registration and execution
    /// </summary>
    public class BehaviorManager
    {
        private Dictionary<BehaviorPriority, IBehavior> _behaviors;
        
        public BehaviorManager()
        {
            _behaviors = new Dictionary<BehaviorPriority, IBehavior>();
        }
        
        public void RegisterBehavior(IBehavior behavior)
        {
            _behaviors[behavior.Priority] = behavior;
        }
        
        public Vector3 CalculateSteeringForce(BehaviorContext context)
        {
            Vector3 totalForce = Vector3.zero;
            
            // Process behaviors in priority order
            foreach (var kvp in _behaviors.OrderByDescending(b => b.Key))
            {
                var behavior = kvp.Value;
                
                if (behavior.IsActive(context))
                {
                    Vector3 force = behavior.CalculateForce(context);
                    
                    // Apply priority-based weight
                    float weight = (float)behavior.Priority / 100f;
                    totalForce += force * weight;
                    
                    // Early exit if force is significant
                    if (totalForce.magnitude > 10f)
                        break;
                }
            }
            
            return totalForce;
        }
    }
}