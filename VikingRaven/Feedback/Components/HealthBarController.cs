using UnityEngine;
using UnityEngine.UI;
using VikingRaven.Units.Components;

namespace VikingRaven.Feedback.Components
{
    public class HealthBarController : MonoBehaviour
    {
        [SerializeField] private Image _healthFill;
        [SerializeField] private Image _damageIndicator;
        [SerializeField] private float _damageIndicatorDelay = 0.5f;
        [SerializeField] private float _damageIndicatorSpeed = 2f;
        
        private HealthComponent _healthComponent;
        private float _displayedHealth = 1f;
        private float _damageHealthTarget = 1f;
        private float _damageTimer = 0f;
        
        public void SetHealthComponent(HealthComponent healthComponent)
        {
            _healthComponent = healthComponent;
            
            // Register for health events
            if (_healthComponent != null)
            {
                _healthComponent.OnDamageTaken += OnDamageTaken;
                _healthComponent.OnHealthRegenerated += OnHealthRegenerated;
                _healthComponent.OnDeath += OnDeath;
                
                // Initialize with current health
                UpdateHealthBar(_healthComponent.HealthPercentage);
                _displayedHealth = _healthComponent.HealthPercentage;
                _damageHealthTarget = _displayedHealth;
            }
        }
        
        private void OnDamageTaken(float amount, Core.ECS.IEntity source)
        {
            if (_healthComponent != null)
            {
                // Update damage indicator
                _damageHealthTarget = _healthComponent.HealthPercentage;
                _damageTimer = _damageIndicatorDelay;
                
                // Update main health bar immediately
                UpdateHealthBar(_healthComponent.HealthPercentage);
            }
        }
        
        private void OnHealthRegenerated(float amount)
        {
            if (_healthComponent != null)
            {
                // Update both health and damage indicator
                UpdateHealthBar(_healthComponent.HealthPercentage);
                _displayedHealth = _healthComponent.HealthPercentage;
                _damageHealthTarget = _displayedHealth;
            }
        }
        
        private void OnDeath()
        {
            UpdateHealthBar(0f);
            _displayedHealth = 0f;
            _damageHealthTarget = 0f;
            
            // Make health bar red
            if (_healthFill != null)
            {
                _healthFill.color = Color.red;
            }
        }
        
        private void Update()
        {
            // Update damage indicator with delay
            if (_damageTimer > 0)
            {
                _damageTimer -= Time.deltaTime;
            }
            else if (_displayedHealth > _damageHealthTarget)
            {
                // Smooth transition for damage indicator
                _displayedHealth = Mathf.MoveTowards(_displayedHealth, _damageHealthTarget, 
                                                   _damageIndicatorSpeed * Time.deltaTime);
                
                if (_damageIndicator != null)
                {
                    _damageIndicator.fillAmount = _displayedHealth;
                }
            }
        }
        
        private void UpdateHealthBar(float healthPercentage)
        {
            if (_healthFill != null)
            {
                _healthFill.fillAmount = healthPercentage;
                
                // Change color based on health
                if (healthPercentage > 0.6f)
                {
                    _healthFill.color = Color.green;
                }
                else if (healthPercentage > 0.3f)
                {
                    _healthFill.color = Color.yellow;
                }
                else
                {
                    _healthFill.color = Color.red;
                }
            }
        }
        
        private void OnDestroy()
        {
            if (_healthComponent != null)
            {
                _healthComponent.OnDamageTaken -= OnDamageTaken;
                _healthComponent.OnHealthRegenerated -= OnHealthRegenerated;
                _healthComponent.OnDeath -= OnDeath;
            }
        }
    }
}