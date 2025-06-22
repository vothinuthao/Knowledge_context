// Enhanced State Machine Classes with Animation Integration

using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.StateMachine;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.StateMachine
{
    /// <summary>
    /// Enhanced Idle State with improved animation handling
    /// </summary>
    public class EnhancedIdleState : BaseUnitState
    {
        private AnimationComponent _animationComponent;
        private AggroDetectionComponent _aggroDetectionComponent;
        
        public EnhancedIdleState(IEntity entity, IStateMachine stateMachine) : base(entity, stateMachine)
        {
        }

        public override void Enter()
        {
            Debug.Log($"Entity {Entity.Id} entered Enhanced Idle state");
            
            // Get animation component
            _animationComponent = Entity.GetComponent<AnimationComponent>();
            _aggroDetectionComponent = Entity.GetComponent<AggroDetectionComponent>();
            
            // Play idle animation with safety check
            if (_animationComponent != null)
            {
                bool success = _animationComponent.PlayAnimation(AnimationComponent.AnimationState.Idle);
                if (success)
                {
                    Debug.Log($"Successfully started Idle animation for entity {Entity.Id}");
                }
                else
                {
                    Debug.LogWarning($"Failed to start Idle animation for entity {Entity.Id}");
                }
            }
            else
            {
                Debug.LogWarning($"No AnimationComponent found on entity {Entity.Id}");
            }
        }

        public override void Execute()
        {
            // Check for enemies in range
            if (_aggroDetectionComponent != null && _aggroDetectionComponent.HasEnemyInRange())
            {
                Debug.Log($"Entity {Entity.Id} detected enemy, transitioning to Aggro state");
                StateMachine.ChangeState<EnhancedAggroState>();
                return;
            }
            
            // Check if we should transition to moving state (handled by AnimationSystem)
            // The AnimationSystem will automatically handle Idle <-> Moving transitions
        }

        public override void Exit()
        {
            Debug.Log($"Entity {Entity.Id} exited Enhanced Idle state");
        }
    }

    /// <summary>
    /// Enhanced Aggro State with combat animation handling
    /// </summary>
    public class EnhancedAggroState : BaseUnitState
    {
        private AnimationComponent _animationComponent;
        private AggroDetectionComponent _aggroDetectionComponent;
        private CombatComponent _combatComponent;
        private TransformComponent _transformComponent;
        
        private float _aggroAnimationDuration = 1f;
        private float _aggroStartTime;
        private bool _hasPlayedAggroAnimation = false;

        public EnhancedAggroState(IEntity entity, IStateMachine stateMachine) : base(entity, stateMachine)
        {
        }

        public override void Enter()
        {
            Debug.Log($"Entity {Entity.Id} entered Enhanced Aggro state");
            
            // Cache components
            _animationComponent = Entity.GetComponent<AnimationComponent>();
            _aggroDetectionComponent = Entity.GetComponent<AggroDetectionComponent>();
            _combatComponent = Entity.GetComponent<CombatComponent>();
            _transformComponent = Entity.GetComponent<TransformComponent>();
            
            _aggroStartTime = Time.time;
            _hasPlayedAggroAnimation = false;
            
            // Play aggro animation
            if (_animationComponent != null)
            {
                bool success = _animationComponent.PlayAnimation(AnimationComponent.AnimationState.Aggro, forcePlay: true);
                if (success)
                {
                    _hasPlayedAggroAnimation = true;
                    Debug.Log($"Successfully started Aggro animation for entity {Entity.Id}");
                }
            }
        }

        public override void Execute()
        {
            // Check if enemy is still in range
            if (_aggroDetectionComponent != null && !_aggroDetectionComponent.HasEnemyInRange())
            {
                Debug.Log($"Entity {Entity.Id} lost enemy, returning to Idle state");
                StateMachine.ChangeState<EnhancedIdleState>();
                return;
            }
            
            // Get target and face it
            var targetEntity = _aggroDetectionComponent?.GetClosestEnemy();
            if (targetEntity != null && _transformComponent != null)
            {
                var targetTransform = targetEntity.GetComponent<TransformComponent>();
                if (targetTransform != null)
                {
                    _transformComponent.LookAt(targetTransform.Position);
                }
                
                // Check if aggro animation has finished and we can transition to combat
                if (_hasPlayedAggroAnimation && Time.time - _aggroStartTime >= _aggroAnimationDuration)
                {
                    // Check if in attack range
                    if (_combatComponent != null && _combatComponent.IsInAttackRange(targetEntity))
                    {
                        Debug.Log($"Entity {Entity.Id} in attack range, transitioning to Combat state");
                        StateMachine.ChangeState<EnhancedCombatState>();
                        return;
                    }
                    else
                    {
                        // Need to move closer - transition to moving/chasing state
                        Debug.Log($"Entity {Entity.Id} needs to move closer to target");
                        StateMachine.ChangeState<EnhancedMovingState>();
                        return;
                    }
                }
            }
        }

        public override void Exit()
        {
            Debug.Log($"Entity {Entity.Id} exited Enhanced Aggro state");
        }
    }

    /// <summary>
    /// Enhanced Moving State with navigation and animation
    /// </summary>
    public class EnhancedMovingState : BaseUnitState
    {
        private AnimationComponent _animationComponent;
        private NavigationComponent _navigationComponent;
        private AggroDetectionComponent _aggroDetectionComponent;
        private CombatComponent _combatComponent;
        
        private IEntity _targetEntity;

        public EnhancedMovingState(IEntity entity, IStateMachine stateMachine) : base(entity, stateMachine)
        {
        }

        public override void Enter()
        {
            Debug.Log($"Entity {Entity.Id} entered Enhanced Moving state");
            
            // Cache components
            _animationComponent = Entity.GetComponent<AnimationComponent>();
            _navigationComponent = Entity.GetComponent<NavigationComponent>();
            _aggroDetectionComponent = Entity.GetComponent<AggroDetectionComponent>();
            _combatComponent = Entity.GetComponent<CombatComponent>();
            
            // Get current target
            _targetEntity = _aggroDetectionComponent?.GetClosestEnemy();
            
            // Animation will be handled automatically by AnimationSystem
            // But we can force it here for immediate response
            if (_animationComponent != null)
            {
                _animationComponent.PlayAnimation(AnimationComponent.AnimationState.Moving);
            }
        }

        public override void Execute()
        {
            // Check if target is still valid
            if (_aggroDetectionComponent != null && !_aggroDetectionComponent.HasEnemyInRange())
            {
                Debug.Log($"Entity {Entity.Id} lost target while moving, returning to Idle");
                StateMachine.ChangeState<EnhancedIdleState>();
                return;
            }
            
            // Update target
            _targetEntity = _aggroDetectionComponent?.GetClosestEnemy();
            
            if (_targetEntity != null)
            {
                // Move towards target
                if (_navigationComponent != null)
                {
                    var targetTransform = _targetEntity.GetComponent<TransformComponent>();
                    if (targetTransform != null)
                    {
                        _navigationComponent.SetDestination(targetTransform.Position);
                    }
                }
                
                // Check if in attack range
                if (_combatComponent != null && _combatComponent.IsInAttackRange(_targetEntity))
                {
                    Debug.Log($"Entity {Entity.Id} reached attack range, transitioning to Combat state");
                    StateMachine.ChangeState<EnhancedCombatState>();
                    return;
                }
            }
            
            // Check if reached destination and no target
            if (_navigationComponent != null && _navigationComponent.HasReachedDestination && _targetEntity == null)
            {
                Debug.Log($"Entity {Entity.Id} reached destination with no target, returning to Idle");
                StateMachine.ChangeState<EnhancedIdleState>();
                return;
            }
        }

        public override void Exit()
        {
            Debug.Log($"Entity {Entity.Id} exited Enhanced Moving state");
            
            // Stop navigation
            if (_navigationComponent != null)
            {
                // _navigationComponent.Stop();
            }
        }
    }

    public class EnhancedCombatState : BaseUnitState
    {
        private AnimationComponent _animationComponent;
        private CombatComponent _combatComponent;
        private AggroDetectionComponent _aggroDetectionComponent;
        
        private IEntity _targetEntity;
        private bool _isAttacking = false;
        private float _attackStartTime;

        public EnhancedCombatState(IEntity entity, IStateMachine stateMachine) : base(entity, stateMachine)
        {
        }

        public override void Enter()
        {
            Debug.Log($"Entity {Entity.Id} entered Enhanced Combat state");
            
            // Cache components
            _animationComponent = Entity.GetComponent<AnimationComponent>();
            _combatComponent = Entity.GetComponent<CombatComponent>();
            _aggroDetectionComponent = Entity.GetComponent<AggroDetectionComponent>();
            
            // Subscribe to animation events
            if (_animationComponent != null)
            {
                _animationComponent.OnAttackAnimationEvent += OnAttackAnimationEvent;
                _animationComponent.OnAnimationCompleted += OnAnimationCompleted;
            }
            
            _targetEntity = _aggroDetectionComponent?.GetClosestEnemy();
            _isAttacking = false;
        }

        public override void Execute()
        {
            // Check if target is still valid
            if (_aggroDetectionComponent != null && !_aggroDetectionComponent.HasEnemyInRange())
            {
                Debug.Log($"Entity {Entity.Id} lost target in combat, returning to Idle");
                StateMachine.ChangeState<EnhancedIdleState>();
                return;
            }
            
            _targetEntity = _aggroDetectionComponent?.GetClosestEnemy();
            
            if (_targetEntity != null && _combatComponent != null)
            {
                // Check if still in attack range
                if (!_combatComponent.IsInAttackRange(_targetEntity))
                {
                    Debug.Log($"Entity {Entity.Id} target moved out of range, chasing");
                    StateMachine.ChangeState<EnhancedMovingState>();
                    return;
                }
                
                // Attack if ready and not currently attacking
                if (!_isAttacking && _combatComponent.CanAttack())
                {
                    StartAttack();
                }
            }
        }

        private void StartAttack()
        {
            Debug.Log($"Entity {Entity.Id} starting attack animation");
            
            _isAttacking = true;
            _attackStartTime = Time.time;
            
            // Play attack animation
            if (_animationComponent != null)
            {
                _animationComponent.PlayAnimation(AnimationComponent.AnimationState.Attack, forcePlay: true);
            }
        }

        private void OnAttackAnimationEvent()
        {
            Debug.Log($"Entity {Entity.Id} attack animation event triggered - dealing damage");
            
            // This is called by Animation Event at the moment of impact
            if (_targetEntity != null && _combatComponent != null)
            {
                // _combatComponent.Attack(_targetEntity);
            }
        }

        private void OnAnimationCompleted(string animationName)
        {
            if (animationName.Contains("Attack"))
            {
                Debug.Log($"Entity {Entity.Id} attack animation completed");
                _isAttacking = false;
            }
        }

        public override void Exit()
        {
            Debug.Log($"Entity {Entity.Id} exited Enhanced Combat state");
            
            // Unsubscribe from animation events
            if (_animationComponent != null)
            {
                _animationComponent.OnAttackAnimationEvent -= OnAttackAnimationEvent;
                _animationComponent.OnAnimationCompleted -= OnAnimationCompleted;
            }
            
            _isAttacking = false;
        }
    }

    /// <summary>
    /// Death State with death animation handling
    /// </summary>
    public class EnhancedDeathState : BaseUnitState
    {
        private AnimationComponent _animationComponent;
        private bool _deathAnimationCompleted = false;

        public EnhancedDeathState(IEntity entity, IStateMachine stateMachine) : base(entity, stateMachine)
        {
        }

        public override void Enter()
        {
            Debug.Log($"Entity {Entity.Id} entered Death state");
            
            _animationComponent = Entity.GetComponent<AnimationComponent>();
            
            // Subscribe to death animation completion
            if (_animationComponent != null)
            {
                _animationComponent.OnDeathAnimationComplete += OnDeathAnimationComplete;
                _animationComponent.OnAnimationCompleted += OnAnimationCompleted;
                
                // Play death animation
                bool success = _animationComponent.PlayAnimation(AnimationComponent.AnimationState.Death, forcePlay: true);
                if (!success)
                {
                    Debug.LogWarning($"Failed to play death animation for entity {Entity.Id}");
                    // If animation fails, proceed with death immediately
                    OnDeathAnimationComplete();
                }
            }
            else
            {
                // No animation component, proceed with death
                OnDeathAnimationComplete();
            }
        }

        public override void Execute()
        {
            // Death state doesn't need to do anything during execution
            // All logic is handled by animation events
        }

        private void OnDeathAnimationComplete()
        {
            if (_deathAnimationCompleted)
                return;
                
            _deathAnimationCompleted = true;
            Debug.Log($"Entity {Entity.Id} death animation completed - destroying entity");
            
            // Handle entity destruction or deactivation
            HandleEntityDeath();
        }

        private void OnAnimationCompleted(string animationName)
        {
            if (animationName.Contains("Death"))
            {
                OnDeathAnimationComplete();
            }
        }

        private void HandleEntityDeath()
        {
            // Disable entity or destroy it
            // Entity.SetActive(false);
        }

        public override void Exit()
        {
            Debug.Log($"Entity {Entity.Id} exited Death state");
            
            // Unsubscribe from events
            if (_animationComponent != null)
            {
                _animationComponent.OnDeathAnimationComplete -= OnDeathAnimationComplete;
                _animationComponent.OnAnimationCompleted -= OnAnimationCompleted;
            }
        }
    }
}