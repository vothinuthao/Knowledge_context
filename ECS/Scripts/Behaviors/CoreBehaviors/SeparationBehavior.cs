// File: Behaviors/CoreBehaviors/SeparationBehavior.cs

using Movement;
using Systems.Behavior;
using UnityEngine;

namespace Behaviors.CoreBehaviors
{
    /// <summary>
    /// Avoids collision with nearby allies
    /// </summary>
    public class SeparationBehavior : IBehavior
    {
        private const float SEPARATION_RADIUS = 1.0f;
        private const float SEPARATION_STRENGTH = 2.0f;
        
        public BehaviorPriority Priority => BehaviorPriority.SEPARATION;
        
        public bool IsActive(BehaviorContext context)
        {
            // Always active when there are nearby allies
            return context.NearbyAllies.Count > 0;
        }
        
        public Vector3 CalculateForce(BehaviorContext context)
        {
            Vector3 separationForce = Vector3.zero;
            int count = 0;
            
            foreach (var ally in context.NearbyAllies)
            {
                if (!ally.HasComponent<PositionComponent>())
                    continue;
                
                var allyPosition = ally.GetComponent<PositionComponent>().Position;
                float distance = Vector3.Distance(context.CurrentPosition, allyPosition);
                
                if (distance < SEPARATION_RADIUS && distance > 0.01f)
                {
                    // Calculate repulsion force
                    Vector3 awayFromAlly = (context.CurrentPosition - allyPosition) / distance;
                    
                    // Stronger force when closer
                    float strength = (SEPARATION_RADIUS - distance) / SEPARATION_RADIUS;
                    separationForce += awayFromAlly * strength;
                    count++;
                }
            }
            
            if (count > 0)
            {
                // Average the force
                separationForce /= count;
                
                // Scale to desired strength
                return separationForce.normalized * SEPARATION_STRENGTH;
            }
            
            return Vector3.zero;
        }
    }
}