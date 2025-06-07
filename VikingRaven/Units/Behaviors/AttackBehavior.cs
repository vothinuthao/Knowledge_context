using UnityEngine;
using VikingRaven.Core.Behavior;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Behaviors
{
    public class AttackBehavior : BaseBehavior
    {
        private float _baseWeight = 3.0f;

        public AttackBehavior(IEntity entity) : base("Attack", entity)
        {
        }

        public override float CalculateWeight()
        {
            var aggroDetectionComponent = _entity.GetComponent<AggroDetectionComponent>();
            if (aggroDetectionComponent == null || !aggroDetectionComponent.HasEnemyInRange())
                return 0f;

            var combatComponent = _entity.GetComponent<CombatComponent>();
            if (combatComponent == null || !combatComponent.CanAttack())
                return 0f;

            var targetEntity = aggroDetectionComponent.GetClosestEnemy();
            if (targetEntity == null)
                return 0f;

            // Calculate weight based on distance to enemy
            var transformComponent = _entity.GetComponent<TransformComponent>();
            var targetTransform = targetEntity.GetComponent<TransformComponent>();
            
            if (transformComponent == null || targetTransform == null)
                return 0f;

            var distanceToEnemy = Vector3.Distance(transformComponent.Position, targetTransform.Position);
            
            // If we're in attack range, set a high weight
            if (combatComponent.IsInAttackRange(targetEntity))
            {
                _weight = _baseWeight * 2.0f;
            }
            else
            {
                // The closer to attack range, the higher the weight
                var attackRange = combatComponent.AttackRange;
                var distanceToAttackRange = Mathf.Max(0, distanceToEnemy - attackRange);
                _weight = _baseWeight * Mathf.Clamp(1.0f / (1.0f + distanceToAttackRange), 0.1f, 1.0f);
            }
            
            return _weight;
        }

        public override void Execute()
        {
            var aggroDetectionComponent = _entity.GetComponent<AggroDetectionComponent>();
            var combatComponent = _entity.GetComponent<CombatComponent>();
            
            if (aggroDetectionComponent == null || combatComponent == null)
                return;

            var targetEntity = aggroDetectionComponent.GetClosestEnemy();
            if (targetEntity == null)
                return;

            // If we're in attack range, attack
            if (combatComponent.IsInAttackRange(targetEntity) && combatComponent.CanAttack())
            {
                // combatComponent.Attack(targetEntity);
            }
            else
            {
                // Otherwise, move towards the target
                var transformComponent = _entity.GetComponent<TransformComponent>();
                var targetTransform = targetEntity.GetComponent<TransformComponent>();
                
                if (transformComponent == null || targetTransform == null)
                    return;

                // Calculate direction to enemy
                var directionToEnemy = (targetTransform.Position - transformComponent.Position).normalized;
                
                // Move towards enemy
                transformComponent.Move(directionToEnemy * (combatComponent.MoveSpeed * Time.deltaTime));
                
                // Look at enemy
                transformComponent.LookAt(targetTransform.Position);
            }
        }
    }
}