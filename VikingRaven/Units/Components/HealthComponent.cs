using System;
using UnityEngine;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    public class HealthComponent : BaseComponent
    {
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _currentHealth;
        [SerializeField] private float _regenerationRate = 0f;
        [SerializeField] private bool _isDead = false;
        
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public bool IsDead => _isDead;
        public float HealthPercentage => _currentHealth / _maxHealth;

        public event Action<float, IEntity> OnDamageTaken;
        public event Action<float> OnHealthRegenerated;
        public event Action OnDeath;

        public override void Initialize()
        {
            _currentHealth = _maxHealth;
            _isDead = false;
        }

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

        public void TakeDamage(float amount, IEntity source)
        {
            if (!IsActive || _isDead)
                return;

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            
            OnDamageTaken?.Invoke(amount, source);
            
            if (_currentHealth <= 0 && !_isDead)
            {
                _isDead = true;
                OnDeath?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (!IsActive || _isDead)
                return;

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
            
            OnHealthRegenerated?.Invoke(_currentHealth - previousHealth);
        }

        public void Revive(float healthPercentage = 1.0f)
        {
            if (!_isDead)
                return;

            _isDead = false;
            _currentHealth = _maxHealth * Mathf.Clamp01(healthPercentage);
        }
    }
}