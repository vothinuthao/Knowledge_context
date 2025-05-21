using System;
using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;

namespace VikingRaven.Units.Models
{
    /// <summary>
    /// Model class for managing unit data and state
    /// Provides a high-level API for unit operations
    /// </summary>
    public class UnitModel
    {
        // Entity reference
        private IEntity _entity;
        
        // Cached components
        private Dictionary<Type, IComponent> _componentCache = new Dictionary<Type, IComponent>();
        
        private UnitType _unitType;
        private string _unitId;
        private string _displayName;
        private string _description;
        
        private float _hitPoints = 100f;          // Máu cơ bản
        private float _shieldHitPoints = 0f;      // Máu khiên (Shield Hitpoints)
        private float _mass = 10f;                // Khối lượng (ảnh hưởng đến knockback)
        private float _damage = 10f;              // Sát thương cận chiến
        private float _damageRanged = 0f;         // Sát thương tầm xa
        private float _damagePerSecond = 0f;      // DPS (tính toán)
        private float _moveSpeed = 3f;            // Tốc độ di chuyển
        private float _hitSpeed = 1.5f;           // Tốc độ đánh (giây)
        private float _loadTime = 0f;             // Thời gian nạp (đạn, kỹ năng)
        private float _range = 2f;                // Tầm đánh
        private float _projectileRange = 0f;      // Tầm bắn của đạn
        private float _deployTime = 1f;           // Thời gian triển khai
        private int _count = 1;                   // Số lượng
        private float _detectionRange = 10f;      // Tầm phát hiện kẻ địch
        
        // Ability properties
        private string _ability = "";             // Tên kỹ năng
        private float _abilityCost = 0f;          // Chi phí kỹ năng
        private float _abilityCooldown = 0f;      // Hồi chiêu kỹ năng
        private string _abilityParameters = "";   // Tham số kỹ năng
        
        // Visual properties
        private Color _unitColor = Color.white; // Màu đại diện

        // State tracking
        private float _currentHealth;             // Máu hiện tại
        private float _currentShield;             // Khiên hiện tại
        private int _squadId = -1;                // ID của đội hình
        private bool _isInCombat = false;         // Đang trong chiến đấu?
        private Vector3 _position;                // Vị trí
        private Quaternion _rotation;             // Hướng
        private bool _hasReachedDestination = true; // Đã đến đích?
        
        // Events
        public event Action OnDeath;
        public event Action<float, IEntity> OnDamageTaken;
        public event Action<IEntity, float> OnAttackPerformed;
        public event Action<float> OnHealthChanged;
        public event Action<float> OnShieldChanged;
        
        // Properties
        public IEntity Entity => _entity;
        public UnitType UnitType => _unitType;
        public string UnitId => _unitId;
        public string DisplayName => _displayName;
        public string Description => _description;
        
        // Health & Shield properties
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
        
        // Combat stats properties
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
        
        // Squad properties
        public int SquadId => _squadId;
        
        public Vector3 Position 
        {
            get
            {
                var transform = (_entity as MonoBehaviour)?.transform;
                if (transform != null)
                    return (Vector3)(_entity != null ? transform?.position : _position);
                return default;
            }
        }
        public Quaternion Rotation
        {
            get
            {
                var transform = (_entity as MonoBehaviour)?.transform;
                if (transform != null)
                    return (Quaternion)(_entity != null ? transform?.rotation : _rotation);
                return default;
            }
        }
        public bool HasReachedDestination => _hasReachedDestination;
        
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
                _count = unitData.Count;
                _detectionRange = unitData.DetectionRange;
                _ability = unitData.Ability;
                _abilityCost = unitData.AbilityCost;
                _abilityCooldown = unitData.AbilityCooldown;
                _abilityParameters = unitData.AbilityParameters;
                _unitColor = unitData.UnitColor;
                _currentHealth = _hitPoints;
                _currentShield = _shieldHitPoints;
            }
            else
            {
                _unitType = UnitType.Infantry;
                _unitId = "unknown";
                _displayName = "Unknown Unit";
                _description = "No description available";
                _currentHealth = _hitPoints;
                _currentShield = _shieldHitPoints;
            }
            
            if (_entity != null)
            {
                RegisterEvents();
                UpdateComponentReferences();
            }
        }
        
        private float CalculateDps()
        {
            float effectiveDamage = Mathf.Max(_damage, _damageRanged);
            return _hitSpeed > 0 ? effectiveDamage / _hitSpeed : 0;
        }
        
        private void RegisterEvents()
        {
            var healthComponent = GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                // healthComponent.OnDamage += HandleDamage;
                healthComponent.OnDeath += HandleDeath;
                _currentHealth = healthComponent.CurrentHealth;
                
                // Lấy thông tin khiên nếu có
                // _currentShield = healthComponent.CurrentArmor;
            }
            
            var combatComponent = GetComponent<CombatComponent>();
            if (combatComponent != null)
            {
                // combatComponent.OnAttackPerformed += HandleAttackPerformed;
            }
            
            var navigationComponent = GetComponent<NavigationComponent>();
            if (navigationComponent != null)
            {
                // navigationComponent.OnDestinationReached += HandleDestinationReached;
                _hasReachedDestination = navigationComponent.HasReachedDestination;
            }
            
            var formationComponent = GetComponent<FormationComponent>();
            if (formationComponent != null)
            {
                _squadId = formationComponent.SquadId;
            }
        }
        
        /// <summary>
        /// Cập nhật các tham chiếu component
        /// </summary>
        private void UpdateComponentReferences()
        {
            if (_entity == null) return;
            
            // Xóa cache hiện tại
            _componentCache.Clear();
            
            // Cập nhật giá trị thiết lập cho các component
            var healthComponent = GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                healthComponent.SetMaxHealth(_hitPoints);
                // healthComponent.SetCurrentHealth(_currentHealth);
                healthComponent.SetArmor(_shieldHitPoints);
            }
            
            var combatComponent = GetComponent<CombatComponent>();
            if (combatComponent != null)
            {
                combatComponent.SetAttackDamage(_damage);
                combatComponent.SetAttackCooldown(_hitSpeed);
                combatComponent.SetAttackRange(_range);
                combatComponent.SetMoveSpeed(_moveSpeed);
                
                // Cấu hình tấn công tầm xa nếu có
                if (_damageRanged > 0)
                {
                    combatComponent.ConfigureSecondaryAttack(
                        true, 
                        AttackType.Ranged, 
                        _damageRanged, 
                        _projectileRange, 
                        _loadTime > 0 ? _loadTime * 3 : 3f
                    );
                }
            }
            
            var unitTypeComponent = GetComponent<UnitTypeComponent>();
            if (unitTypeComponent != null)
            {
                unitTypeComponent.SetUnitType(_unitType);
            }
            
            var navigationComponent = GetComponent<NavigationComponent>();
            if (navigationComponent != null)
            {
                // navigationComponent.SetMoveSpeed(_moveSpeed);
            }
            
            var aggroComponent = GetComponent<AggroDetectionComponent>();
            if (aggroComponent != null)
            {
                aggroComponent.SetAggroRange(_detectionRange);
            }
            
            var abilityComponent = GetComponent<AbilityComponent>();
            if (abilityComponent != null && !string.IsNullOrEmpty(_ability))
            {
                abilityComponent.SetAbility(_ability, _abilityCost, _abilityCooldown, _abilityParameters);
            }
        }
        
        /// <summary>
        /// Xử lý sự kiện khi bị tấn công
        /// </summary>
        private void HandleDamage(float amount, IEntity source)
        {
            // Ưu tiên trừ khiên trước, sau đó mới trừ máu
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
            }
        }
        
        /// <summary>
        /// Xử lý sự kiện khi chết
        /// </summary>
        private void HandleDeath()
        {
            _currentHealth = 0;
            OnDeath?.Invoke();
        }
        
        /// <summary>
        /// Xử lý sự kiện khi tấn công
        /// </summary>
        private void HandleAttackPerformed(IEntity target, float damage)
        {
            OnAttackPerformed?.Invoke(target, damage);
            _isInCombat = true;
        }
        
        /// <summary>
        /// Xử lý sự kiện khi đến đích
        /// </summary>
        private void HandleDestinationReached()
        {
            _hasReachedDestination = true;
        }
        
        /// <summary>
        /// Lấy component theo kiểu
        /// </summary>
        public T GetComponent<T>() where T : class, IComponent
        {
            if (_entity == null) return null;
            
            Type type = typeof(T);
            
            // Kiểm tra cache
            if (_componentCache.TryGetValue(type, out IComponent cachedComponent))
            {
                return cachedComponent as T;
            }
            
            // Lấy component từ entity và lưu vào cache
            T component = _entity.GetComponent<T>();
            if (component != null)
            {
                _componentCache[type] = component;
            }
            
            return component;
        }
        
        /// <summary>
        /// Kiểm tra xem unit có đang trong trạng thái chiến đấu hay không
        /// </summary>
        public bool IsInCombat()
        {
            // Kiểm tra thông qua combat component nếu có
            var combatComponent = GetComponent<CombatComponent>();
            if (combatComponent != null)
            {
                return combatComponent.IsInCombat;
            }
            
            return _isInCombat;
        }
        
        /// <summary>
        /// Thiết lập ID đội
        /// </summary>
        public void SetSquadId(int squadId)
        {
            _squadId = squadId;
            
            // Cập nhật component nếu có
            var formationComponent = GetComponent<FormationComponent>();
            if (formationComponent != null)
            {
                formationComponent.SetSquadId(squadId);
            }
        }
        
        /// <summary>
        /// Kích hoạt kỹ năng nếu có thể
        /// </summary>
        public bool ActivateAbility(IEntity target = null)
        {
            if (string.IsNullOrEmpty(_ability)) return false;
            
            var abilityComponent = GetComponent<AbilityComponent>();
            if (abilityComponent != null)
            {
                return abilityComponent.ActivateAbility(new Vector3() ,target);
            }
            
            return false;
        }
        
        /// <summary>
        /// Hồi sinh đơn vị
        /// </summary>
        public void Revive()
        {
            _currentHealth = _hitPoints;
            _currentShield = _shieldHitPoints;
            
            var healthComponent = GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                healthComponent.Revive();
            }
            
            OnHealthChanged?.Invoke(_currentHealth);
            OnShieldChanged?.Invoke(_currentShield);
        }
        
        /// <summary>
        /// Áp dụng thay đổi thông số cho entity
        /// </summary>
        public void ApplyData()
        {
            if (_entity == null) return;
            
            // Cập nhật các component
            UpdateComponentReferences();
            
            Debug.Log($"UnitModel: Applied data for unit {_displayName} (ID: {_unitId})");
        }
        
        /// <summary>
        /// Dọn dẹp tài nguyên và hủy đăng ký sự kiện
        /// </summary>
        public void Cleanup()
        {
            // Hủy đăng ký tất cả các sự kiện
            if (_entity != null)
            {
                var healthComponent = GetComponent<HealthComponent>();
                if (healthComponent != null)
                {
                    // healthComponent.OnDamage -= HandleDamage;
                    healthComponent.OnDeath -= HandleDeath;
                }
                
                var combatComponent = GetComponent<CombatComponent>();
                if (combatComponent != null)
                {
                    // combatComponent.OnAttackPerformed -= HandleAttackPerformed;
                }
                
                var navigationComponent = GetComponent<NavigationComponent>();
                if (navigationComponent != null)
                {
                    // navigationComponent.OnDestinationReached -= HandleDestinationReached;
                }
            }
            
            // Xóa cache
            _componentCache.Clear();
            _entity = null;
            
            Debug.Log($"UnitModel: Cleaned up unit {_displayName} (ID: {_unitId})");
        }
        
        /// <summary>
        /// Tạo chuỗi biểu diễn của unit
        /// </summary>
        public override string ToString()
        {
            return $"UnitModel({_unitType}, '{_displayName}', HP: {_currentHealth}/{_hitPoints}, Shield: {_currentShield}/{_shieldHitPoints}, Squad: {_squadId})";
        }
    }
}