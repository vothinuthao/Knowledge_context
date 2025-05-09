using UnityEngine;
using VikingRaven.Core.Behavior;
using VikingRaven.Core.ECS;
using VikingRaven.Core.StateMachine;

namespace VikingRaven.Units.Components
{
    public class ChargeBehavior : BaseBehavior
    {
        private Vector3 _chargeTarget;
        private Vector3 _chargeDirection;
        private float _baseWeight = 2.2f;
        private float _chargeSpeed = 2.0f;
        private float _chargeDuration = 3.0f;
        private float _cooldownDuration = 8.0f;
        private float _chargeTimer = 0f;
        private float _cooldownTimer = 0f;
        private bool _isCharging = false;
        private float _chargeDistance = 15.0f;
        private float _damageBonusMultiplier = 1.5f;

        public ChargeBehavior(IEntity entity) : base("Charge", entity)
        {
        }

        public void SetChargeTarget(Vector3 target)
        {
            _chargeTarget = target;
        }

        public override float CalculateWeight()
        {
            // Check cooldown
            if (_cooldownTimer > 0)
            {
                _cooldownTimer -= Time.deltaTime;
                _weight = 0f;
                return _weight;
            }
            
            // Check if already charging
            if (_isCharging)
            {
                _weight = _baseWeight * 2.0f; // High priority to continue charge
                return _weight;
            }
            
            // Check unit type - better for melee units
            var unitTypeComponent = _entity.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent == null)
                return 0f;
                
            if (unitTypeComponent.UnitType == UnitType.Infantry)
            {
                _weight = _baseWeight * 1.2f;
            }
            else if (unitTypeComponent.UnitType == UnitType.Pike)
            {
                _weight = _baseWeight * 1.5f; // Best for pike units
            }
            else
            {
                _weight = _baseWeight * 0.5f; // Not great for archers
            }
            
            // Check distance to target
            var transformComponent = _entity.GetComponent<TransformComponent>();
            if (transformComponent != null)
            {
                float distanceToTarget = Vector3.Distance(transformComponent.Position, _chargeTarget);
                
                // Ideal charge distance - not too close, not too far
                if (distanceToTarget > _chargeDistance * 0.5f && distanceToTarget < _chargeDistance)
                {
                    _weight *= 1.5f;
                }
                else if (distanceToTarget <= _chargeDistance * 0.5f)
                {
                    // Too close for effective charge
                    _weight *= 0.5f;
                }
                else if (distanceToTarget >= _chargeDistance * 2.0f)
                {
                    // Too far for charge
                    _weight *= 0.2f;
                }
            }
            
            return _weight;
        }

        public override void Execute()
        {
            var transformComponent = _entity.GetComponent<TransformComponent>();
            var navigationComponent = _entity.GetComponent<NavigationComponent>();
            
            if (transformComponent == null || navigationComponent == null)
                return;
                
            // Start charging if not already
            if (!_isCharging)
            {
                StartCharge(transformComponent);
            }
            
            // Update charge timer
            _chargeTimer += Time.deltaTime;
            
            // Check if charge should end
            if (_chargeTimer >= _chargeDuration)
            {
                EndCharge();
                return;
            }
            
            // Execute the charge - direct movement instead of pathfinding
            navigationComponent.DisablePathfinding();
            
            // Move forward in charge direction
            transformComponent.Move(_chargeDirection * _chargeSpeed * Time.deltaTime);
            
            // Check for collision with enemies during charge
            CheckChargeCollisions(transformComponent);
        }

        private void StartCharge(TransformComponent transformComponent)
        {
            _isCharging = true;
            _chargeTimer = 0f;
            
            // Set direction towards target
            _chargeDirection = (_chargeTarget - transformComponent.Position).normalized;
            _chargeDirection.y = 0; // Keep charge horizontal
            
            // Face the charge direction
            transformComponent.LookAt(transformComponent.Position + _chargeDirection);
            
            // Play charge animation
            var animationComponent = _entity.GetComponent<AnimationComponent>();
            if (animationComponent != null)
            {
                animationComponent.PlayAnimation("Charge");
            }
            
            Debug.Log($"Entity {_entity.Id} started charging!");
        }

        private void EndCharge()
        {
            _isCharging = false;
            _cooldownTimer = _cooldownDuration;
            
            // Re-enable pathfinding
            var navigationComponent = _entity.GetComponent<NavigationComponent>();
            if (navigationComponent != null)
            {
                navigationComponent.EnablePathfinding();
            }
            
            // Play end charge animation
            var animationComponent = _entity.GetComponent<AnimationComponent>();
            if (animationComponent != null)
            {
                animationComponent.PlayAnimation("Idle");
            }
            
            Debug.Log($"Entity {_entity.Id} finished charging!");
        }

        private void CheckChargeCollisions(TransformComponent transformComponent)
        {
            // This is a simplified collision check
            // In a real game, you'd use physics for this
            
            float collisionRadius = 1.0f;
            
            // Get nearby entities
            var entityRegistry = GameObject.FindObjectOfType<EntityRegistry>();
            if (!entityRegistry)
                return;
                
            var allEntities = entityRegistry.GetAllEntities();
            
            foreach (var entity in allEntities)
            {
                if (entity == _entity)
                    continue;
                    
                var entityTransform = entity.GetComponent<TransformComponent>();
                if (entityTransform == null)
                    continue;
                    
                float distance = Vector3.Distance(transformComponent.Position, entityTransform.Position);
                
                if (distance < collisionRadius)
                {
                    // Check if this is an enemy
                    // In a real game, you'd use a faction system
                    bool isEnemy = true; // Simplified for example
                    
                    if (isEnemy)
                    {
                        // Apply damage with bonus
                        var combatComponent = _entity.GetComponent<CombatComponent>();
                        var entityHealth = entity.GetComponent<HealthComponent>();
                        
                        if (combatComponent != null && entityHealth != null)
                        {
                            float chargeDamage = combatComponent.AttackDamage * _damageBonusMultiplier;
                            entityHealth.TakeDamage(chargeDamage, _entity);
                        }
                        
                        Vector3 knockbackDirection = (entityTransform.Position - transformComponent.Position).normalized;
                        
                        var stateComponent = entity.GetComponent<StateComponent>();
                        if (stateComponent != null && stateComponent.StateMachineInGame != null)
                        {
                            if (stateComponent.StateMachineInGame.CurrentState is KnockbackState knockbackState)
                            {
                                knockbackState.SetKnockbackParams(knockbackDirection, 8.0f, 0.5f);
                                stateComponent.StateMachineInGame.ChangeState(knockbackState);
                            }
                        }
                        
                        Debug.Log($"Entity {_entity.Id} hit enemy {entity.Id} during charge!");
                    }
                }
            }
        }
    }
}