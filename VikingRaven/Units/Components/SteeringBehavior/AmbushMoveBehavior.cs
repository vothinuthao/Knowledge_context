using UnityEngine;
using VikingRaven.Core.Behavior;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class AmbushMoveBehavior : BaseBehavior
    {
        private Vector3 _targetPosition;
        private float _minDistanceToTarget = 0.1f;
        private float _baseWeight = 1.5f;
        private float _stealthDuration = 0f; // Increases over time in stealth

        public AmbushMoveBehavior(IEntity entity) : base("AmbushMove", entity)
        {
        }

        public void SetTargetPosition(Vector3 position)
        {
            _targetPosition = position;
        }

        public override float CalculateWeight()
        {
            // Make sure we have the required components
            var transformComponent = _entity.GetComponent<TransformComponent>();
            var stealthComponent = _entity.GetComponent<StealthComponent>();
            
            if (transformComponent == null || stealthComponent == null)
                return 0f;

            var distanceToTarget = Vector3.Distance(transformComponent.Position, _targetPosition);
            
            // If we're very close to target, reduce weight
            if (distanceToTarget < _minDistanceToTarget)
            {
                _weight = 0f;
                return _weight;
            }
            
            // Increase weight the longer we stay in stealth
            float stealthBonus = Mathf.Min(_stealthDuration / 5.0f, 1.0f);
            
            // If we've been detected or have taken damage, reduce weight significantly
            if (!stealthComponent.IsStealthed)
            {
                _weight = _baseWeight * 0.1f;
                _stealthDuration = 0f;
            }
            else
            {
                _weight = _baseWeight * (1.0f + stealthBonus);
                
                // Check if there are enemies nearby, if so reduce weight
                var aggroDetectionComponent = _entity.GetComponent<AggroDetectionComponent>();
                if (aggroDetectionComponent != null && aggroDetectionComponent.HasEnemyInRange())
                {
                    var closestEnemy = aggroDetectionComponent.GetClosestEnemy();
                    if (closestEnemy != null && stealthComponent.CanBeDetectedBy(closestEnemy))
                    {
                        _weight *= 0.5f;
                    }
                }
            }
            
            return _weight;
        }

        public override void Execute()
        {
            var transformComponent = _entity.GetComponent<TransformComponent>();
            var navigationComponent = _entity.GetComponent<NavigationComponent>();
            var stealthComponent = _entity.GetComponent<StealthComponent>();
            
            if (transformComponent == null || navigationComponent == null || stealthComponent == null)
                return;

            // Enter stealth mode if not already stealthed
            if (!stealthComponent.IsStealthed)
            {
                stealthComponent.EnterStealth();
                _stealthDuration = 0f;
            }
            else
            {
                _stealthDuration += Time.deltaTime;
            }
            
            // Move slower while in stealth
            var combatComponent = _entity.GetComponent<CombatComponent>();
            if (combatComponent != null)
            {
                float originalSpeed = combatComponent.MoveSpeed;
                float stealthSpeed = originalSpeed * stealthComponent.StealthMovementSpeedFactor;
                
                // This is a simplified approach - in a real implementation,
                // you'd modify the movement speed attribute directly
                navigationComponent.SetDestination(_targetPosition);
            }
            else
            {
                // Set the target in the navigation component
                navigationComponent.SetDestination(_targetPosition);
            }
            
            // Let the navigation component handle the actual movement
            navigationComponent.UpdatePathfinding();
            
            // Check if we've been detected
            var aggroDetectionComponent = _entity.GetComponent<AggroDetectionComponent>();
            if (aggroDetectionComponent != null && aggroDetectionComponent.HasEnemyInRange())
            {
                var closestEnemy = aggroDetectionComponent.GetClosestEnemy();
                if (closestEnemy != null && stealthComponent.CanBeDetectedBy(closestEnemy))
                {
                    // Break stealth if very close
                    var enemyTransform = closestEnemy.GetComponent<TransformComponent>();
                    if (enemyTransform != null)
                    {
                        float distance = Vector3.Distance(transformComponent.Position, enemyTransform.Position);
                        if (distance < stealthComponent.DetectionRadius * 0.5f)
                        {
                            stealthComponent.ExitStealth();
                        }
                    }
                }
            }
            
            // Check if we've reached the destination
            if (Vector3.Distance(transformComponent.Position, _targetPosition) < _minDistanceToTarget)
            {
                Debug.Log($"Entity {_entity.Id} reached ambush destination");
            }
        }
    }
}