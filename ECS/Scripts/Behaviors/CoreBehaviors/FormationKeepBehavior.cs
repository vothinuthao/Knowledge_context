// File: Behaviors/CoreBehaviors/FormationKeepBehavior.cs

using Core.ECS;
using Movement;
using UnityEngine;
using Systems.Behavior;

namespace Behaviors.CoreBehaviors
{
    /// <summary>
    /// Maintains formation position relative to squad
    /// </summary>
    public class FormationKeepBehavior : IBehavior
    {
        public BehaviorPriority Priority => BehaviorPriority.FORMATION_KEEP;
        
        public bool IsActive(BehaviorContext context)
        {
            // Always active when in a squad
            var troop = context.Entity.GetComponent<TroopComponent>();
            return troop != null && troop.SquadId != -1;
        }
        
        public Vector3 CalculateForce(BehaviorContext context)
        {
            if (context.TargetPosition == Vector3.zero)
                return Vector3.zero;
            
            Vector3 toTarget = context.TargetPosition - context.CurrentPosition;
            float distance = toTarget.magnitude;
            
            // Strong force to maintain formation
            float forceMagnitude = distance > 0.5f ? 5.0f : 3.0f;
            
            // Scale force by distance
            if (distance > 2.0f)
            {
                forceMagnitude *= distance / 2.0f;
            }
            
            return toTarget.normalized * forceMagnitude;
        }
    }
}

