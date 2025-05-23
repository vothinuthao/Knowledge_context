using System;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;

namespace VikingRaven.Units.Models
{
    /// <summary>
    /// Model class quản lý dữ liệu và trạng thái đơn vị
    /// Đã được tách biệt khỏi logic component để tránh lỗi tham chiếu null
    /// </summary>
    public class UnitModel
    {
        // Entity reference - chỉ lưu trữ tham chiếu, không xử lý logic
        private IEntity _entity;
        
        // Core unit properties (lưu trữ trực tiếp thay vì tham chiếu đến UnitDataSO)
        private UnitType _unitType;
        private string _unitId;
        private string _displayName;
        private string _description;
        
        // Combat stats
        private float _hitPoints = 100f;           // Máu cơ bản
        private float _shieldHitPoints = 0f;       // Máu khiên (Shield Hitpoints)
        private float _mass = 10f;                 // Khối lượng (ảnh hưởng đến knockback)
        private float _damage = 10f;               // Sát thương cận chiến
        private float _damageRanged = 0f;          // Sát thương tầm xa
        private float _damagePerSecond = 0f;       // DPS (tính toán)
        private float _moveSpeed = 3f;             // Tốc độ di chuyển
        private float _hitSpeed = 1.5f;            // Tốc độ đánh (giây)
        private float _loadTime = 0f;              // Thời gian nạp (đạn, kỹ năng)
        private float _range = 2f;                 // Tầm đánh
        private float _projectileRange = 0f;        // Tầm bắn của đạn
        private float _deployTime = 1f;            // Thời gian triển khai
        private int _count = 1;                    // Số lượng
        private float _detectionRange = 10f;       // Tầm phát hiện kẻ địch
        
        // Ability properties
        private string _ability = "";              // Tên kỹ năng
        private float _abilityCost = 0f;           // Chi phí kỹ năng
        private float _abilityCooldown = 0f;       // Hồi chiêu kỹ năng
        private string _abilityParameters = "";    // Tham số kỹ năng
        
        // Visual properties
        private Color _unitColor = Color.white;    // Màu đại diện

        // State tracking - chỉ lưu trữ dữ liệu, không xử lý logic
        private float _currentHealth;              // Máu hiện tại
        private float _currentShield;              // Khiên hiện tại
        private int _squadId = -1;                 // ID của đội hình
        private Vector3 _position;                 // Vị trí (sao lưu)
        private Quaternion _rotation;              // Hướng (sao lưu)
        
        public event Action OnDeath;
        public event Action<float, IEntity> OnDamageTaken;
        public event Action<IEntity, float> OnAttackPerformed;
        public event Action<float> OnHealthChanged;
        public event Action<float> OnShieldChanged;
        
        // Properties - chỉ trả về dữ liệu, không xử lý logic
        public IEntity Entity => _entity;
        public UnitType UnitType => _unitType;
        public string UnitId => _unitId;
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
        
        // Position properties (lưu ý: không truy xuất transform nữa)
        public Vector3 Position => _position;
        public Quaternion Rotation => _rotation;
        
        /// <summary>
        /// Khởi tạo từ entity và unitData
        /// </summary>
        public UnitModel(IEntity entity, UnitDataSO unitData)
        {
            _entity = entity;
            
            if (unitData != null)
            {
                // Khởi tạo thông tin cơ bản
                _unitType = unitData.UnitType;
                _unitId = unitData.UnitId;
                _displayName = unitData.DisplayName;
                _description = unitData.Description;
                
                // Khởi tạo các chỉ số chiến đấu
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
                
                // Khởi tạo thông tin kỹ năng
                _ability = unitData.Ability;
                _abilityCost = unitData.AbilityCost;
                _abilityCooldown = unitData.AbilityCooldown;
                _abilityParameters = unitData.AbilityParameters;
                
                // Khởi tạo thuộc tính hình ảnh
                _unitColor = unitData.UnitColor;
                
                // Khởi tạo giá trị máu hiện tại
                _currentHealth = _hitPoints;
                _currentShield = _shieldHitPoints;
            }
            else
            {
                // Giá trị mặc định nếu không có unitData
                _unitType = UnitType.Infantry;
                _unitId = "unknown";
                _displayName = "Unknown Unit";
                _description = "No description available";
                
                // Giá trị chiến đấu mặc định
                _currentHealth = _hitPoints;
                _currentShield = _shieldHitPoints;
            }
            
            // Lưu vị trí ban đầu nếu có transform
            if (entity != null)
            {
                var entityObj = entity as MonoBehaviour;
                if (entityObj != null)
                {
                    _position = entityObj.transform.position;
                    _rotation = entityObj.transform.rotation;
                }
            }
        }
        
        /// <summary>
        /// Khởi tạo trực tiếp từ các tham số (constructor đầy đủ)
        /// </summary>
        public UnitModel(UnitType unitType, string unitId, string displayName, string description,
                        float hitPoints, float shieldHitPoints, float mass, float damage, float damageRanged,
                        float moveSpeed, float hitSpeed, float loadTime, float range, float projectileRange,
                        float deployTime, int count, float detectionRange,
                        string ability = "", float abilityCost = 0, float abilityCooldown = 0, string abilityParameters = "")
        {
            // Khởi tạo thông tin cơ bản
            _unitType = unitType;
            _unitId = unitId;
            _displayName = displayName;
            _description = description;
            
            // Khởi tạo các chỉ số chiến đấu
            _hitPoints = hitPoints;
            _shieldHitPoints = shieldHitPoints;
            _mass = mass;
            _damage = damage;
            _damageRanged = damageRanged;
            _moveSpeed = moveSpeed;
            _hitSpeed = hitSpeed;
            _loadTime = loadTime;
            _range = range;
            _projectileRange = projectileRange;
            _deployTime = deployTime;
            _count = count;
            _detectionRange = detectionRange;
            
            // Tính toán DPS
            _damagePerSecond = CalculateDPS();
            
            // Khởi tạo thông tin kỹ năng
            _ability = ability;
            _abilityCost = abilityCost;
            _abilityCooldown = abilityCooldown;
            _abilityParameters = abilityParameters;
            
            // Khởi tạo giá trị máu hiện tại
            _currentHealth = _hitPoints;
            _currentShield = _shieldHitPoints;
        }
        
        /// <summary>
        /// Tính toán giá trị DPS
        /// </summary>
        private float CalculateDPS()
        {
            float effectiveDamage = Mathf.Max(_damage, _damageRanged);
            return _hitSpeed > 0 ? effectiveDamage / _hitSpeed : 0;
        }
        
        /// <summary>
        /// Cập nhật vị trí và góc quay từ entity (nếu có transform)
        /// </summary>
        public void UpdateTransformData()
        {
            if (_entity != null)
            {
                var entityObj = _entity as MonoBehaviour;
                if (entityObj != null)
                {
                    _position = entityObj.transform.position;
                    _rotation = entityObj.transform.rotation;
                }
            }
        }
        
        /// <summary>
        /// Thiết lập ID đội
        /// </summary>
        public void SetSquadId(int squadId)
        {
            _squadId = squadId;
        }
        
        /// <summary>
        /// Gây sát thương cho đơn vị
        /// </summary>
        public void TakeDamage(float amount, IEntity source)
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
                
                // Kiểm tra xem đã chết chưa
                if (_currentHealth <= 0)
                {
                    OnDeath?.Invoke();
                }
            }
        }
        
        /// <summary>
        /// Hồi máu cho đơn vị
        /// </summary>
        public void Heal(float amount)
        {
            float oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(_currentHealth + amount, _hitPoints);
            
            if (_currentHealth > oldHealth)
            {
                OnHealthChanged?.Invoke(_currentHealth);
            }
        }
        
        /// <summary>
        /// Hồi sinh đơn vị
        /// </summary>
        public void Revive(float healthPercent = 1.0f)
        {
            _currentHealth = _hitPoints * Mathf.Clamp01(healthPercent);
            _currentShield = _shieldHitPoints;
            
            OnHealthChanged?.Invoke(_currentHealth);
            OnShieldChanged?.Invoke(_currentShield);
        }
        
        /// <summary>
        /// Tạo chuỗi biểu diễn của đơn vị
        /// </summary>
        public override string ToString()
        {
            return $"UnitModel({_unitType}, '{_displayName}', HP: {_currentHealth}/{_hitPoints}, Shield: {_currentShield}/{_shieldHitPoints}, Squad: {_squadId})";
        }
        
        /// <summary>
        /// Tạo bản sao của model này
        /// </summary>
        public UnitModel Clone()
        {
            return new UnitModel(
                _unitType, _unitId, _displayName, _description,
                _hitPoints, _shieldHitPoints, _mass, _damage, _damageRanged,
                _moveSpeed, _hitSpeed, _loadTime, _range, _projectileRange,
                _deployTime, _count, _detectionRange,
                _ability, _abilityCost, _abilityCooldown, _abilityParameters
            );
        }
    }
}