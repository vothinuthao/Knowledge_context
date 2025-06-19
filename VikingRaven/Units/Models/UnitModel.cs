using System;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;

namespace VikingRaven.Units.Models
{
    public class UnitModel
    {
        private IEntity _entity;
        private UnitType _unitType;
        private uint _unitId;
        private string _displayName;
        private string _description;
        private float _hitPoints = 100f;
        private float _shieldHitPoints = 0f;
        private float _mass = 10f; 
        private float _damage = 10f;
        private float _damageRanged = 0f;
        private float _damagePerSecond = 0f;
        private float _moveSpeed = 3f;
        private float _hitSpeed = 1.5f; 
        private float _loadTime = 0f;
        private float _range = 2f;
        private float _projectileRange = 0f;
        private float _deployTime = 1f;
        private int _count = 1;
        private float _detectionRange = 10f;
        
        private string _ability = "";
        private float _abilityCost = 0f;
        private float _abilityCooldown = 0f;
        private string _abilityParameters = "";
        
        private Color _unitColor = Color.white;

        private float _currentHealth;
        private float _currentShield;
        private int _squadId = -1;
        private Vector3 _position;
        private Quaternion _rotation;
        
        public event Action OnDeath;
        public event Action<float, IEntity> OnDamageTaken;
        public event Action<IEntity, float> OnAttackPerformed;
        public event Action<float> OnHealthChanged;
        public event Action<float> OnShieldChanged;
        
        public IEntity Entity => _entity;
        public UnitType UnitType => _unitType;
        public uint UnitId => _unitId;
        public string DisplayName => _displayName;
        public string Description => _description; 
        
        public float CurrentHealth 
        { 
            get => _currentHealth; 
            set 
            { 
                float oldHealth = _currentHealth;
                _currentHealth = Mathf.Clamp(value, 0, _hitPoints);
                
                if (_currentHealth != oldHealth)
                {
                    OnHealthChanged?.Invoke(_currentHealth);
                    
                    // Check for death
                    if (_currentHealth <= 0 && oldHealth > 0)
                    {
                        OnDeath?.Invoke();
                    }
                }
            }
        }
        
        public float CurrentShield
        {
            get => _currentShield;
            set
            {
                float oldShield = _currentShield;
                _currentShield = Mathf.Clamp(value, 0, _shieldHitPoints);
                
                if (_currentShield != oldShield)
                {
                    OnShieldChanged?.Invoke(_currentShield);
                }
            }
        }
        public float MaxHealth => _hitPoints;
        public float MaxShield => _shieldHitPoints;
        public float Mass => _mass;
        public float Damage => _damage;
        public float DamageRanged => _damageRanged;
        public float DamagePerSecond => _damagePerSecond;
        public float MoveSpeed => _moveSpeed;
        public float HitSpeed => _hitSpeed;
        public float LoadTime => _loadTime;
        public float Range => _range;
        public float ProjectileRange => _projectileRange;
        public float DeployTime => _deployTime;
        public int Count => _count;
        public float DetectionRange => _detectionRange;
        
        // Ability properties
        public string Ability => _ability;
        public float AbilityCost => _abilityCost;
        public float AbilityCooldown => _abilityCooldown;
        public string AbilityParameters => _abilityParameters;
        
        public int SquadId => _squadId;
        public Vector3 Position => _position;
        public Quaternion Rotation => _rotation;
        public UnitModel(IEntity entity, UnitDataSO unitData)
        {
            _entity = entity;
            
            if (unitData != null)
            {
                _unitType = unitData.UnitType;
                _unitId = unitData.UnitId;
                _displayName = unitData.DisplayName;
                _description = unitData.Description;
                _hitPoints = unitData.HitPoints;
                _shieldHitPoints = unitData.Shield;
                _mass = unitData.Mass;
                _damage = unitData.Damage;
                _damageRanged = unitData.DamageRanged;
                _damagePerSecond = unitData.DamagePerSecond;
                _moveSpeed = unitData.MoveSpeed;
                _hitSpeed = unitData.HitSpeed;
                _loadTime = unitData.LoadTime;
                _range = unitData.Range;
                _projectileRange = unitData.ProjectileRange;
                _deployTime = unitData.DeployTime;
                _detectionRange = unitData.DetectionRange;
                _unitColor = unitData.UnitColor;
                _currentHealth = _hitPoints;
                _currentShield = _shieldHitPoints;
            }
            else
            {
                _unitType = UnitType.Infantry;
                _unitId = 9999;
                _displayName = "Unknown Unit";
                _description = "No description available";
                _currentHealth = _hitPoints;
                _currentShield = _shieldHitPoints;
            }
            if (entity != null)
            {
                var entityObj = entity as MonoBehaviour;
                if (entityObj)
                {
                    _position = entityObj.transform.position;
                    _rotation = entityObj.transform.rotation;
                }
            }
        }
        public void SetSquadId(int squadId)
        {
            _squadId = squadId;
        }
        
        public void TakeDamage(float amount, IEntity source)
        {
            if (_currentShield > 0)
            {
                float shieldDamage = Mathf.Min(_currentShield, amount);
                _currentShield -= shieldDamage;
                amount -= shieldDamage;
                
                OnShieldChanged?.Invoke(_currentShield);
            }
            
            if (amount > 0)
            {
                _currentHealth = Mathf.Max(0, _currentHealth - amount);
                OnDamageTaken?.Invoke(amount, source);
                OnHealthChanged?.Invoke(_currentHealth);
                
                if (_currentHealth <= 0)
                {
                    OnDeath?.Invoke();
                }
            }
        }
        public void Heal(float amount)
        {
            float oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(_currentHealth + amount, _hitPoints);
            
            if (_currentHealth > oldHealth)
            {
                OnHealthChanged?.Invoke(_currentHealth);
            }
        }
        public void Revive(float healthPercent = 1.0f)
        {
            _currentHealth = _hitPoints * Mathf.Clamp01(healthPercent);
            _currentShield = _shieldHitPoints;
            
            OnHealthChanged?.Invoke(_currentHealth);
            OnShieldChanged?.Invoke(_currentShield);
        }
        
    }
}