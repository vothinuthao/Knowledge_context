using System;
using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    /// <summary>
    /// Component for managing unit health and damage
    /// </summary>
    public class HealthComponent : BaseComponent
    {
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth;
        [SerializeField] private float _regenerationRate = 0f;
        [SerializeField] private bool _isDead = false;
        [SerializeField] private float _armor = 0f;
        [SerializeField] private float _damageReduction = 0f;
        
        // Public properties for accessing stats
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public bool IsDead => _isDead;
        public float HealthPercentage => _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;
        public float Armor => _armor;
        public float DamageReduction => _damageReduction;
        
        // Events
        public event Action<float, IEntity> OnDamageTaken;
        public event Action<float> OnHealthRegenerated;
        public event Action OnDeath;
        public event Action OnRevive;

        /// <summary>
        /// Initialize component
        /// </summary>
        public override void Initialize()
        {
            _currentHealth = _maxHealth;
            _isDead = false;
        }
        
        /// <summary>
        /// Update method called by Unity
        /// </summary>
        public void Update()
        {
            if (!IsActive || _isDead)
                return;

            // Regenerate health if applicable
            if (_regenerationRate > 0 && _currentHealth < _maxHealth)
            {
                float regeneratedAmount = _regenerationRate * Time.deltaTime;
                _currentHealth = Mathf.Min(_currentHealth + regeneratedAmount, _maxHealth);
                OnHealthRegenerated?.Invoke(regeneratedAmount);
            }
        }
        
        /// <summary>
        /// Set the maximum health
        /// </summary>
        /// <param name="maxHealth">New maximum health value</param>
        public void SetMaxHealth(float maxHealth)
        {
            if (maxHealth <= 0)
            {
                Debug.LogWarning($"HealthComponent: Invalid max health value: {maxHealth}");
                return;
            }
            
            float oldMax = _maxHealth;
            _maxHealth = maxHealth;
            
            // Scale current health proportionally
            if (oldMax > 0 && !_isDead)
            {
                float healthPercentage = _currentHealth / oldMax;
                _currentHealth = _maxHealth * healthPercentage;
            }
            else
            {
                _currentHealth = _maxHealth;
            }
        }
        
        /// <summary>
        /// Set armor value for damage reduction
        /// </summary>
        /// <param name="armor">New armor value</param>
        public void SetArmor(float armor)
        {
            _armor = Mathf.Max(0f, armor);
            
            // Update damage reduction based on armor (example formula)
            _damageReduction = _armor / (_armor + 100f); // Damage reduction is between 0 and 1
        }
        
        /// <summary>
        /// Take damage from a source
        /// </summary>
        /// <param name="amount">Raw damage amount</param>
        /// <param name="source">Entity causing the damage</param>
        /// <param name="ignoreArmor">Whether to ignore armor</param>
        public void TakeDamage(float amount, IEntity source, bool ignoreArmor = false)
        {
            if (!IsActive || _isDead)
                return;
                
            // Apply damage reduction from armor
            float actualDamage = amount;
            if (!ignoreArmor && _damageReduction > 0)
            {
                actualDamage = amount * (1f - _damageReduction);
            }
            
            // Ensure minimum damage
            actualDamage = Mathf.Max(1f, actualDamage);
            
            // Apply damage
            _currentHealth = Mathf.Max(0, _currentHealth - actualDamage);
            
            // Trigger event
            OnDamageTaken?.Invoke(actualDamage, source);
            
            // Check for death
            if (_currentHealth <= 0 && !_isDead)
            {
                Die();
            }
        }
        
        /// <summary>
        /// Take true damage that ignores armor
        /// </summary>
        /// <param name="amount">Damage amount</param>
        /// <param name="source">Entity causing the damage</param>
        public void TakeTrueDamage(float amount, IEntity source)
        {
            TakeDamage(amount, source, true);
        }
        
        /// <summary>
        /// Heal the unit
        /// </summary>
        /// <param name="amount">Amount to heal</param>
        public void Heal(float amount)
        {
            if (!IsActive || _isDead)
                return;

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            
            float healedAmount = _currentHealth - previousHealth;
            if (healedAmount > 0)
            {
                OnHealthRegenerated?.Invoke(healedAmount);
            }
        }
        
        /// <summary>
        /// Set health to a specific value
        /// </summary>
        /// <param name="value">New health value</param>
        public void SetHealth(float value)
        {
            if (_isDead)
                return;
                
            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(value, 0f, _maxHealth);
            
            if (_currentHealth <= 0 && !_isDead)
            {
                Die();
            }
            else if (_currentHealth > previousHealth)
            {
                OnHealthRegenerated?.Invoke(_currentHealth - previousHealth);
            }
            else if (_currentHealth < previousHealth)
            {
                OnDamageTaken?.Invoke(previousHealth - _currentHealth, null);
            }
        }
        
        /// <summary>
        /// Handle death
        /// </summary>
        private void Die()
        {
            _isDead = true;
            _currentHealth = 0f;
            
            // Trigger event
            OnDeath?.Invoke();
            
            Debug.Log($"HealthComponent: Entity {Entity.Id} has died");
        }
        
        /// <summary>
        /// Revive the unit with a percentage of max health
        /// </summary>
        /// <param name="healthPercentage">Percentage of max health to restore (0-1)</param>
        public void Revive(float healthPercentage = 1.0f)
        {
            if (!_isDead)
                return;

            _isDead = false;
            _currentHealth = _maxHealth * Mathf.Clamp01(healthPercentage);
            
            // Trigger event
            OnRevive?.Invoke();
            
            Debug.Log($"HealthComponent: Entity {Entity.Id} has been revived with {_currentHealth} health");
        }
        
        /// <summary>
        /// Set regeneration rate
        /// </summary>
        /// <param name="regen">New regeneration rate (health per second)</param>
        public void SetRegenerationRate(float regen)
        {
            _regenerationRate = Mathf.Max(0f, regen);
        }
    }
}