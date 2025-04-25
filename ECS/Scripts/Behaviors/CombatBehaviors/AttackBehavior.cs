using Components;
using Components.Squad;
using Core.ECS;
using Movement;
using Systems.Behavior;
using UnityEngine;

namespace Behaviors
{
    /// <summary>
    /// Attack behavior for combat state
    /// </summary>
    public class AttackBehavior : IBehavior
    {
        private const float ATTACK_RANGE = 1.5f;
        private const float CHASE_SPEED_MULTIPLIER = 1.2f;
        
        public BehaviorPriority Priority => BehaviorPriority.COMBAT;
        
        public bool IsActive(BehaviorContext context)
        {
            // Active when in combat state and enemies nearby
            return context.SquadState == SquadState.COMBAT && 
                   context.NearbyEnemies.Count > 0;
        }
        
        public Vector3 CalculateForce(BehaviorContext context)
        {
            // Find closest enemy
            Entity closestEnemy = null;
            float closestDistance = float.MaxValue;
            
            foreach (var enemy in context.NearbyEnemies)
            {
                if (!enemy.HasComponent<PositionComponent>())
                    continue;
                
                var enemyPosition = enemy.GetComponent<PositionComponent>().Position;
                float distance = Vector3.Distance(context.CurrentPosition, enemyPosition);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = enemy;
                }
            }
            
            if (closestEnemy == null)
                return Vector3.zero;
            
            var targetPosition = closestEnemy.GetComponent<PositionComponent>().Position;
            Vector3 toTarget = targetPosition - context.CurrentPosition;
            
            // If within attack range, maintain distance
            if (closestDistance < ATTACK_RANGE)
            {
                // Circling behavior
                Vector3 perpendicular = new Vector3(-toTarget.z, 0, toTarget.x).normalized;
                return perpendicular * 2.0f;
            }
            else
            {
                // Chase enemy
                return toTarget.normalized * (3.0f * CHASE_SPEED_MULTIPLIER);
            }
        }
    }
}