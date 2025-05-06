

using System;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.StateMachine;

namespace VikingRaven.Units.Components
{
    public class CombatComponent : BaseComponent
    {
        [SerializeField] private float _attackDamage = 10f;
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _attackCooldown = 1.5f;
        [SerializeField] private float _moveSpeed = 3.0f;
        [SerializeField] private float _knockbackForce = 5f;
        [SerializeField] private float _knockbackDuration = 0.3f;
        
        private float _lastAttackTime = -100f;
        
        public float AttackDamage => _attackDamage;
        public float AttackRange => _attackRange;
        public float MoveSpeed => _moveSpeed;
        public float TimeSinceLastAttack => Time.time - _lastAttackTime;
        
        public event Action<IEntity> OnAttackPerformed;

        public bool CanAttack()
        {
            return TimeSinceLastAttack >= _attackCooldown;
        }

        public bool IsInAttackRange(IEntity target)
        {
            var targetTransform = target.GetComponent<TransformComponent>();
            var myTransform = Entity.GetComponent<TransformComponent>();
            
            if (targetTransform == null || myTransform == null)
                return false;
                
            return Vector3.Distance(myTransform.Position, targetTransform.Position) <= _attackRange;
        }

        public void Attack(IEntity target)
        {
            if (!CanAttack())
                return;
                
            _lastAttackTime = Time.time;
            
            // Play attack animation
            var animationComponent = Entity.GetComponent<AnimationComponent>();
            if (animationComponent != null)
            {
                animationComponent.PlayAnimation("Attack");
            }
            
            // Apply damage to target
            var targetHealth = target.GetComponent<HealthComponent>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(_attackDamage, Entity);
            }
            
            var targetTransform = target.GetComponent<TransformComponent>();
            var myTransform = Entity.GetComponent<TransformComponent>();
            
            if (targetTransform != null && myTransform != null)
            {
                Vector3 knockbackDirection = (targetTransform.Position - myTransform.Position).normalized;
                
                // var targetStateMachine = target.GetComponent<StateMachine>();
                // if (targetStateMachine != null)
                // {
                //     var knockbackState = targetStateMachine.GetState<KnockbackState>();
                //     if (knockbackState != null)
                //     {
                //         knockbackState.SetKnockbackParams(knockbackDirection, _knockbackForce, _knockbackDuration);
                //         targetStateMachine.ChangeState(knockbackState);
                //     }
                // }
            }
            
            OnAttackPerformed?.Invoke(target);
        }
    }
}