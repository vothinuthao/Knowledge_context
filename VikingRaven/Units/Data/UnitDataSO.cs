using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Units.Components;
using System.Collections.Generic;

namespace VikingRaven.Units.Data
{
    /// <summary>
    /// ScriptableObject định nghĩa dữ liệu của một loại đơn vị
    /// Chỉ chứa dữ liệu, không liên quan đến logic component
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnitData", menuName = "VikingRaven/Unit Data SO")]
    public class UnitDataSO : SerializedScriptableObject
    {
        [FoldoutGroup("Basic Information")]
        [Tooltip("Định danh duy nhất cho loại đơn vị này")]
        [SerializeField] private string _unitId;
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("Tên hiển thị của đơn vị")]
        [SerializeField] private string _displayName;
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("Mô tả về đơn vị")]
        [TextArea(3, 5)]
        [SerializeField] private string _description;
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("Prefab cho loại đơn vị này")]
        [SerializeField, PreviewField(60), Required]
        private GameObject _prefab;
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("Loại đơn vị (Infantry, Archer, Pike)")]
        [SerializeField, EnumToggleButtons]
        private UnitType _unitType;
        
        [TitleGroup("Combat Stats", "Chỉ số và khả năng chiến đấu")]
        [FoldoutGroup("Combat Stats/Health")]
        [Tooltip("Điểm máu cơ bản")]
        [SerializeField, Range(50, 500), ProgressBar(50, 500, ColorGetter = "GetHealthColor")]
        private float _hitPoints = 100f;
        
        [FoldoutGroup("Combat Stats/Health")]
        [Tooltip("Điểm giáp hoặc khiên")]
        [SerializeField, Range(0, 100), ProgressBar(0, 100, ColorGetter = "GetShieldColor")]
        private float _shield = 0f;
        
        [FoldoutGroup("Combat Stats/Health")]
        [Tooltip("Khối lượng đơn vị (ảnh hưởng đến hiệu ứng đẩy lùi)")]
        [SerializeField, Range(1, 100)]
        private float _mass = 10f;
        
        [FoldoutGroup("Combat Stats/Damage")]
        [Tooltip("Sát thương cận chiến")]
        [SerializeField, Range(1, 100), ProgressBar(1, 100, ColorGetter = "GetDamageColor")]
        private float _damage = 10f;
        
        [FoldoutGroup("Combat Stats/Damage")]
        [Tooltip("Sát thương tầm xa")]
        [SerializeField, Range(0, 100), ProgressBar(0, 100, ColorGetter = "GetDamageColor")]
        private float _damageRanged = 0f;
        
        [FoldoutGroup("Combat Stats/Damage")]
        [Tooltip("Sát thương mỗi giây (tính toán)")]
        [ReadOnly, ShowInInspector, ProgressBar(0, 50, ColorGetter = "GetDPSColor")]
        private float _damagePerSecond;
        
        [FoldoutGroup("Combat Stats/Attack Parameters")]
        [Tooltip("Tầm tấn công (đơn vị)")]
        [SerializeField, Range(1, 20)]
        private float _range = 2f;
        
        [FoldoutGroup("Combat Stats/Attack Parameters")]
        [Tooltip("Tầm bắn đạn (cho đơn vị tầm xa)")]
        [SerializeField, Range(0, 50), ShowIf("HasRangedDamage")]
        private float _projectileRange = 0f;
        
        [FoldoutGroup("Combat Stats/Attack Parameters")]
        [Tooltip("Thời gian giữa các đợt tấn công (giây)")]
        [SerializeField, Range(0.1f, 5f)]
        private float _hitSpeed = 1.5f;
        
        [FoldoutGroup("Combat Stats/Attack Parameters")]
        [Tooltip("Thời gian chuẩn bị tấn công")]
        [SerializeField, Range(0, 3f), ShowIf("HasRangedDamage")]
        private float _loadTime = 0f;
        
        [FoldoutGroup("Movement Stats")]
        [Tooltip("Tốc độ di chuyển của đơn vị")]
        [SerializeField, Range(1, 10), ProgressBar(1, 10, ColorGetter = "GetSpeedColor")]
        private float _moveSpeed = 3.0f;
        
        [FoldoutGroup("Movement Stats")]
        [Tooltip("Thời gian cần thiết để triển khai đơn vị")]
        [SerializeField, Range(0, 5)]
        private float _deployTime = 1.0f;
        
        [FoldoutGroup("Detection Stats")]
        [Tooltip("Phạm vi mà đơn vị có thể phát hiện kẻ địch (ô)")]
        [SerializeField, Range(3, 20)]
        private float _detectionRange = 10f;
        
        [FoldoutGroup("Unit Count")]
        [Tooltip("Số lượng đơn vị trong một nhóm")]
        [SerializeField, Range(1, 20)]
        private int _count = 1;
        
        [FoldoutGroup("Ability Settings")]
        [Tooltip("Khả năng đặc biệt của đơn vị")]
        [SerializeField, ValueDropdown("GetAvailableAbilities")]
        private string _ability = "";
        
        [FoldoutGroup("Ability Settings")]
        [Tooltip("Chi phí sử dụng khả năng")]
        [SerializeField, Range(0, 100), EnableIf("HasAbility")]
        private float _abilityCost = 0f;
        
        [FoldoutGroup("Ability Settings")]
        [Tooltip("Thời gian hồi khả năng")]
        [SerializeField, Range(0, 60), EnableIf("HasAbility")]
        private float _abilityCooldown = 0f;
        
        [FoldoutGroup("Ability Settings")]
        [Tooltip("Tham số bổ sung cho khả năng")]
        [SerializeField, TextArea(2, 5), EnableIf("HasAbility")]
        private string _abilityParameters = "";
        
        [FoldoutGroup("Visual Settings")]
        [Tooltip("Biểu tượng đơn vị cho UI")]
        [SerializeField, PreviewField(80)]
        private Sprite _icon;
        
        [FoldoutGroup("Visual Settings")]
        [Tooltip("Màu đại diện cho loại đơn vị này")]
        [SerializeField, ColorUsage(true)]
        private Color _unitColor = Color.white;
        
        // Tùy chọn dropdown cho các khả năng
        private string[] GetAvailableAbilities()
        {
            return new string[] { "", "Charge", "Heal", "RangedAttack", "Shield", "Stealth", "Taunt", "Throw", "Snare", "Summon" };
        }
        
        // Các helper method cho Odin Inspector
        private bool HasAbility => !string.IsNullOrEmpty(_ability);
        private bool HasRangedDamage => _damageRanged > 0;
        
        // Các getter màu cho thanh tiến trình
        private Color GetHealthColor => new Color(0.2f, 0.6f, 0.2f);
        private Color GetShieldColor => new Color(0.2f, 0.2f, 0.8f);
        private Color GetDamageColor => new Color(0.8f, 0.2f, 0.2f);
        private Color GetDPSColor => new Color(0.8f, 0.4f, 0.0f);
        private Color GetSpeedColor => new Color(0.8f, 0.8f, 0.0f);
        
        // Public properties cho tất cả các trường
        public string UnitId => _unitId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public GameObject Prefab => _prefab;
        public UnitType UnitType => _unitType;
        public float HitPoints => _hitPoints;
        public float Shield => _shield;
        public float Mass => _mass;
        public float Damage => _damage;
        public float DamageRanged => _damageRanged;
        public float DamagePerSecond => _damagePerSecond;
        public float Range => _range;
        public float ProjectileRange => _projectileRange;
        public float HitSpeed => _hitSpeed;
        public float LoadTime => _loadTime;
        public float MoveSpeed => _moveSpeed;
        public float DeployTime => _deployTime;
        public float DetectionRange => _detectionRange;
        public int Count => _count;
        public string Ability => _ability;
        public float AbilityCost => _abilityCost;
        public float AbilityCooldown => _abilityCooldown;
        public string AbilityParameters => _abilityParameters;
        public Sprite Icon => _icon;
        public Color UnitColor => _unitColor;

        // Phương thức Odin Inspector để tính DPS khi các giá trị thay đổi
        [Button("Calculate DPS"), PropertyTooltip("Tính lại sát thương mỗi giây")]
        [OnInspectorGUI]
        private void CalculateDamagePerSecond()
        {
            float effectiveDamage = Mathf.Max(_damage, _damageRanged);
            _damagePerSecond = effectiveDamage / _hitSpeed;
        }
        
        // Phương thức kiểm tra dữ liệu khi giá trị thay đổi
        [OnValueChanged("ValidateData")]
        private void ValidateData()
        {
            // Đảm bảo đơn vị tầm xa có tầm bắn
            if (_damageRanged > 0 && _projectileRange <= 0)
            {
                _projectileRange = 10f; // Giá trị mặc định
            }
            
            // Đảm bảo cài đặt khả năng nhất quán
            if (string.IsNullOrEmpty(_ability))
            {
                _abilityCost = 0f;
                _abilityCooldown = 0f;
                _abilityParameters = "";
            }
            
            // Tính sát thương mỗi giây
            CalculateDamagePerSecond();
        }

        /// <summary>
        /// Phương thức để tạo bản sao của dữ liệu này
        /// </summary>
        public UnitDataSO Clone()
        {
            var clone = CreateInstance<UnitDataSO>();
            
            // Sao chép tất cả thông tin cơ bản
            clone._unitId = this._unitId;
            clone._displayName = this._displayName;
            clone._description = this._description;
            clone._prefab = this._prefab;
            clone._unitType = this._unitType;
            clone._icon = this._icon;
            clone._unitColor = this._unitColor;
            
            // Sao chép tất cả chỉ số chiến đấu
            clone._hitPoints = this._hitPoints;
            clone._shield = this._shield;
            clone._mass = this._mass;
            clone._damage = this._damage;
            clone._damageRanged = this._damageRanged;
            clone._damagePerSecond = this._damagePerSecond;
            clone._range = this._range;
            clone._projectileRange = this._projectileRange;
            clone._hitSpeed = this._hitSpeed;
            clone._loadTime = this._loadTime;
            
            // Sao chép tất cả chỉ số di chuyển
            clone._moveSpeed = this._moveSpeed;
            clone._deployTime = this._deployTime;
            
            // Sao chép tất cả chỉ số phát hiện
            clone._detectionRange = this._detectionRange;
            
            // Sao chép số lượng đơn vị
            clone._count = this._count;
            
            // Sao chép cài đặt khả năng
            clone._ability = this._ability;
            clone._abilityCost = this._abilityCost;
            clone._abilityCooldown = this._abilityCooldown;
            clone._abilityParameters = this._abilityParameters;
            
            return clone;
        }
    }
}