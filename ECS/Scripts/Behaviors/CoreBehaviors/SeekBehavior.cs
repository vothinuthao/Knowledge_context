using Components;
using Components.Squad;
using Movement;
using Systems.Behavior;
using UnityEngine;

namespace Behaviors.CoreBehaviors
{
    /// <summary>
    /// Seeks toward target position
    /// </summary>
    public class SeekBehavior : IBehavior
    {
        public BehaviorPriority Priority => BehaviorPriority.SEEK;
        
        public bool IsActive(BehaviorContext context)
        {
            // Active when squad is moving or in combat
            return context.SquadState == SquadState.MOVING || 
                   context.SquadState == SquadState.ATTACKING;
        }
        
        public Vector3 CalculateForce(BehaviorContext context)
        {
            if (context.TargetPosition == Vector3.zero)
                return Vector3.zero;
            
            Vector3 desiredVelocity = (context.TargetPosition - context.CurrentPosition).normalized * 3.0f;
            
            // Arrival behavior - slow down when close to target
            if (context.DistanceToTarget < 2.0f)
            {
                desiredVelocity *= context.DistanceToTarget / 2.0f;
            }
            
            // Get current velocity
            var velocity = context.Entity.GetComponent<VelocityComponent>();
            Vector3 currentVelocity = velocity != null ? velocity.Velocity : Vector3.zero;
            
            return desiredVelocity - currentVelocity;
        }
    }
}