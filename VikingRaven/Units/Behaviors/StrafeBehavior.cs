using UnityEngine;
using VikingRaven.Core.Behavior;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Behaviors
{
    public class StrafeBehavior : BaseBehavior
    {
        private float _strafeDistance = 3.0f;
        private float _strafeDuration = 2.0f;
        private float _strafeTimer = 0f;
        private Vector3 _strafeDirection;
        private float _baseWeight = 1.5f;

        public StrafeBehavior(IEntity entity) : base("Strafe", entity)
        {
        }

        public override float CalculateWeight()
        {
            var aggroDetectionComponent = _entity.GetComponent<AggroDetectionComponent>();
            if (aggroDetectionComponent == null || !aggroDetectionComponent.HasEnemyInRange())
                return 0f;

            var combatComponent = _entity.GetComponent<CombatComponent>();
            if (combatComponent == null)
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
            
            // If we're in attack range, reduce strafe weight
            if (combatComponent.IsInAttackRange(targetEntity))
            {
                _weight = _baseWeight * 0.5f;
            }
            else
            {
                // The closer to the enemy (but not in attack range), the higher the strafe weight
                _weight = _baseWeight * Mathf.Clamp(1.0f / distanceToEnemy, 0.1f, 2.0f);
            }
            
            return _weight;
        }

        public override void Execute()
        {
            var transformComponent = _entity.GetComponent<TransformComponent>();
            var aggroDetectionComponent = _entity.GetComponent<AggroDetectionComponent>();
            
            if (transformComponent == null || aggroDetectionComponent == null)
                return;

            var targetEntity = aggroDetectionComponent.GetClosestEnemy();
            if (targetEntity == null)
                return;

            var targetTransform = targetEntity.GetComponent<TransformComponent>();
            if (targetTransform == null)
                return;

            // Update strafe timer
            _strafeTimer += Time.deltaTime;
            
            // If timer exceeds duration, pick a new strafe direction
            if (_strafeTimer >= _strafeDuration)
            {
                _strafeTimer = 0f;
                
                // Calculate direction to enemy
                var directionToEnemy = (targetTransform.Position - transformComponent.Position).normalized;
                
                // Calculate perpendicular direction (either left or right randomly)
                _strafeDirection = Vector3.Cross(directionToEnemy, Vector3.up);
                if (UnityEngine.Random.value > 0.5f)
                {
                    _strafeDirection = -_strafeDirection;
                }
            }
            
            // Apply strafe movement
            transformComponent.Move(_strafeDirection * _strafeDistance * Time.deltaTime);
            
            // Keep facing the enemy
            transformComponent.LookAt(targetTransform.Position);
        }
    }
}