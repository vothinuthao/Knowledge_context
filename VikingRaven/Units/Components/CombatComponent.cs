using System;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.StateMachine;

namespace VikingRaven.Units.Components
{
    /// <summary>
    /// Component responsible for handling unit combat capabilities
    /// </summary>
    public class CombatComponent : BaseComponent
    {
        [Header("Base Combat Stats")]
        [SerializeField] private float _attackDamage = 10f;
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _attackCooldown = 1.5f;
        [SerializeField] private float _moveSpeed = 3.0f;
        
        [Header("Knockback Settings")]
        [SerializeField] private float _knockbackForce = 5f;
        [SerializeField] private float _knockbackDuration = 0.3f;
        
        [Header("Critical Hit Settings")]
        [SerializeField] private float _criticalHitChance = 0.05f;
        [SerializeField] private float _criticalHitMultiplier = 1.5f;
        
        [Header("Attack Types")]
        [SerializeField] private AttackType _primaryAttackType = AttackType.Melee;
        [SerializeField] private bool _hasSecondaryAttack = false;
        [SerializeField] private AttackType _secondaryAttackType = AttackType.None;
        [SerializeField] private float _secondaryAttackDamage = 0f;
        [SerializeField] private float _secondaryAttackRange = 0f;
        [SerializeField] private float _secondaryAttackCooldown = 3f;
        
        // Tracking variables
        private float _lastAttackTime = -100f;
        private float _lastSecondaryAttackTime = -100f;
        private int _attackCount = 0;
        private float _totalDamageDealt = 0f;
        private bool _isInCombat = false;
        
        // Public properties
        public float AttackDamage => _attackDamage;
        public float AttackRange => _attackRange;
        public float MoveSpeed => _moveSpeed;
        public float TimeSinceLastAttack => Time.time - _lastAttackTime;
        public float AttackCooldown => _attackCooldown;
        public AttackType PrimaryAttackType => _primaryAttackType;
        public bool HasSecondaryAttack => _hasSecondaryAttack;
        public AttackType SecondaryAttackType => _secondaryAttackType;
        public int AttackCount => _attackCount;
        public float TotalDamageDealt => _totalDamageDealt;
        public bool IsInCombat => _isInCombat;
        
        // Events
        public event Action<IEntity> OnAttackPerformed;
        public event Action<IEntity, float> OnDamageDealt;
        public event Action<IEntity> OnSecondaryAttackPerformed;
        public event Action<bool> OnCriticalHit;
        
        /// <summary>
        /// Check if the unit can perform primary attack
        /// </summary>
        public bool CanAttack()
        {
            return TimeSinceLastAttack >= _attackCooldown;
        }
        
        /// <summary>
        /// Check if the unit can perform secondary attack
        /// </summary>
        public bool CanSecondaryAttack()
        {
            if (!_hasSecondaryAttack) return false;
            return (Time.time - _lastSecondaryAttackTime) >= _secondaryAttackCooldown;
        }
        
        /// <summary>
        /// Check if target is within primary attack range
        /// </summary>
        public bool IsInAttackRange(IEntity target)
        {
            var targetTransform = target.GetComponent<TransformComponent>();
            var myTransform = Entity.GetComponent<TransformComponent>();
            
            if (targetTransform == null || myTransform == null)
                return false;
                
            return Vector3.Distance(myTransform.Position, targetTransform.Position) <= _attackRange;
        }
        
        /// <summary>
        /// Check if target is within secondary attack range
        /// </summary>
        public bool IsInSecondaryAttackRange(IEntity target)
        {
            if (!_hasSecondaryAttack) return false;
            
            var targetTransform = target.GetComponent<TransformComponent>();
            var myTransform = Entity.GetComponent<TransformComponent>();
            
            if (targetTransform == null || myTransform == null)
                return false;
                
            return Vector3.Distance(myTransform.Position, targetTransform.Position) <= _secondaryAttackRange;
        }
        
        /// <summary>
        /// Perform primary attack against a target
        /// </summary>
        public void Attack(IEntity target)
        {
            if (!CanAttack() || target == null)
                return;
                
            _lastAttackTime = Time.time;
            _attackCount++;
            
            // Play attack animation
            var animationComponent = Entity.GetComponent<AnimationComponent>();
            if (animationComponent != null)
            {
                animationComponent.PlayAnimation("Attack");
            }
            
            // Calculate damage with chance for critical hit
            float damage = _attackDamage;
            bool isCritical = false;
            
            if (UnityEngine.Random.value < _criticalHitChance)
            {
                damage *= _criticalHitMultiplier;
                isCritical = true;
                OnCriticalHit?.Invoke(true);
            }
            
            // Apply damage to target
            var targetHealth = target.GetComponent<HealthComponent>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage, Entity);
                _totalDamageDealt += damage;
                OnDamageDealt?.Invoke(target, damage);
            }
            
            // Apply knockback if melee attack
            if (_primaryAttackType == AttackType.Melee)
            {
                ApplyKnockback(target);
            }
            
            // Trigger event
            OnAttackPerformed?.Invoke(target);
            
            Debug.Log($"CombatComponent: Entity {Entity.Id} attacked {target.Id} for {damage} damage" + (isCritical ? " (CRITICAL)" : ""));
        }
        
        /// <summary>
        /// Perform secondary attack against a target
        /// </summary>
        public void SecondaryAttack(IEntity target)
        {
            if (!_hasSecondaryAttack || !CanSecondaryAttack() || target == null)
                return;
                
            _lastSecondaryAttackTime = Time.time;
            
            // Play secondary attack animation
            var animationComponent = Entity.GetComponent<AnimationComponent>();
            if (animationComponent != null)
            {
                animationComponent.PlayAnimation("SecondaryAttack");
            }
            
            // Calculate damage
            float damage = _secondaryAttackDamage;
            
            // Apply damage to target
            var targetHealth = target.GetComponent<HealthComponent>();
            if (targetHealth != null)
            {
                // Secondary attacks often ignore armor
                targetHealth.TakeTrueDamage(damage, Entity);
                _totalDamageDealt += damage;
                OnDamageDealt?.Invoke(target, damage);
            }
            
            // Trigger event
            OnSecondaryAttackPerformed?.Invoke(target);
            
            Debug.Log($"CombatComponent: Entity {Entity.Id} performed secondary attack on {target.Id} for {damage} damage");
        }
        
        /// <summary>
        /// Apply knockback effect to target
        /// </summary>
        private void ApplyKnockback(IEntity target)
        {
            var targetTransform = target.GetComponent<TransformComponent>();
            var myTransform = Entity.GetComponent<TransformComponent>();
            
            if (targetTransform == null || myTransform == null)
                return;
                
            Vector3 knockbackDirection = (targetTransform.Position - myTransform.Position).normalized;
            
            // Try to get state machine for knockback state
            var targetStateComponent = target.GetComponent<StateComponent>();
            if (targetStateComponent != null && targetStateComponent.StateMachineInGame != null)
            {
                var stateMachine = targetStateComponent.StateMachineInGame;
                
                // Use reflection to find and set KnockbackState parameters
                var knockbackState = stateMachine.GetType().GetMethod("GetState")?.Invoke(stateMachine, new[] { typeof(KnockbackState) });
                
                if (knockbackState != null)
                {
                    // Set knockback parameters using reflection
                    knockbackState.GetType().GetMethod("SetKnockbackParams")?.Invoke(
                        knockbackState, 
                        new object[] { knockbackDirection, _knockbackForce, _knockbackDuration }
                    );
                    
                    // Change to knockback state
                    stateMachine.GetType().GetMethod("ChangeState")?.Invoke(stateMachine, new[] { knockbackState });
                }
            }
        }
        
        /// <summary>
        /// Set primary attack damage
        /// </summary>
        public void SetAttackDamage(float damage)
        {
            _attackDamage = Mathf.Max(0, damage);
        }
        
        /// <summary>
        /// Set attack range
        /// </summary>
        public void SetAttackRange(float range)
        {
            _attackRange = Mathf.Max(0, range);
        }
        
        /// <summary>
        /// Set attack cooldown
        /// </summary>
        public void SetAttackCooldown(float cooldown)
        {
            _attackCooldown = Mathf.Max(0.1f, cooldown);
        }
        
        /// <summary>
        /// Set movement speed
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            _moveSpeed = Mathf.Max(0.1f, speed);
        }
        
        /// <summary>
        /// Set knockback force
        /// </summary>
        public void SetKnockbackForce(float force)
        {
            _knockbackForce = Mathf.Max(0, force);
        }
        
        /// <summary>
        /// Set knockback duration
        /// </summary>
        public void SetKnockbackDuration(float duration)
        {
            _knockbackDuration = Mathf.Max(0, duration);
        }
        
        /// <summary>
        /// Configure secondary attack
        /// </summary>
        public void ConfigureSecondaryAttack(bool hasSecondary, AttackType type, float damage, float range, float cooldown)
        {
            _hasSecondaryAttack = hasSecondary;
            _secondaryAttackType = type;
            _secondaryAttackDamage = damage;
            _secondaryAttackRange = range;
            _secondaryAttackCooldown = cooldown;
        }
        
        /// <summary>
        /// Set critical hit parameters
        /// </summary>
        public void SetCriticalHitParams(float chance, float multiplier)
        {
            _criticalHitChance = Mathf.Clamp01(chance);
            _criticalHitMultiplier = Mathf.Max(1f, multiplier);
        }
        
        /// <summary>
        /// Configure unit for melee combat
        /// </summary>
        public void ConfigureForMeleeCombat(float damage, float range, float cooldown)
        {
            _primaryAttackType = AttackType.Melee;
            SetAttackDamage(damage);
            SetAttackRange(range);
            SetAttackCooldown(cooldown);
            SetKnockbackForce(5f);
            SetKnockbackDuration(0.3f);
        }
        
        /// <summary>
        /// Configure unit for ranged combat
        /// </summary>
        public void ConfigureForRangedCombat(float damage, float range, float cooldown)
        {
            _primaryAttackType = AttackType.Ranged;
            SetAttackDamage(damage);
            SetAttackRange(range);
            SetAttackCooldown(cooldown);
            SetKnockbackForce(0f);
            SetKnockbackDuration(0f);
        }
    }
    
    /// <summary>
    /// Type of attack
    /// </summary>
    public enum AttackType
    {
        None,
        Melee,
        Ranged,
        Magic,
        Special
    }
}