using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Data
{
    /// <summary>
    /// ScriptableObject defining unit data
    /// Updated to use uint for ID instead of string
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnitData", menuName = "VikingRaven/Unit Data SO")]
    public class UnitDataSO : SerializedScriptableObject
    {
        [FoldoutGroup("Basic Information")]
        [Tooltip("Unique identifier for this unit type (must be greater than 0)")]
        [SerializeField, MinValue(1)]
        private uint _unitId;
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("Display name of the unit")]
        [SerializeField] private string _displayName;
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("Description of the unit")]
        [TextArea(3, 5)]
        [SerializeField] private string _description;
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("Prefab for this unit type")]
        [SerializeField, PreviewField(60), Required]
        private GameObject _prefab;
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("Unit type category")]
        [SerializeField, EnumToggleButtons]
        private UnitType _unitType;
        
        [FoldoutGroup("Combat Stats/Health")]
        [Tooltip("Base hit points")]
        [SerializeField, Range(50, 500), ProgressBar(50, 500, ColorGetter = "GetHealthColor")]
        private float _hitPoints = 100f;
        
        [FoldoutGroup("Combat Stats/Health")]
        [Tooltip("Shield or armor points")]
        [SerializeField, Range(0, 100), ProgressBar(0, 100, ColorGetter = "GetShieldColor")]
        private float _shield = 0f;
        
        [FoldoutGroup("Combat Stats/Health")]
        [Tooltip("Unit mass (affects knockback)")]
        [SerializeField, Range(1, 100)]
        private float _mass = 10f;
        
        [FoldoutGroup("Combat Stats/Damage")]
        [Tooltip("Melee damage")]
        [SerializeField, Range(1, 100), ProgressBar(1, 100, ColorGetter = "GetDamageColor")]
        private float _damage = 10f;
        
        [FoldoutGroup("Combat Stats/Damage")]
        [Tooltip("Ranged damage")]
        [SerializeField, Range(0, 100), ProgressBar(0, 100, ColorGetter = "GetDamageColor")]
        private float _damageRanged = 0f;
        
        [FoldoutGroup("Combat Stats/Damage")]
        [Tooltip("Damage per second (calculated)")]
        [ReadOnly, ShowInInspector, ProgressBar(0, 50, ColorGetter = "GetDPSColor")]
        private float _damagePerSecond;
        
        [FoldoutGroup("Combat Stats/Attack Parameters")]
        [Tooltip("Attack range (units)")]
        [SerializeField, Range(1, 20)]
        private float _range = 2f;
        
        [FoldoutGroup("Combat Stats/Attack Parameters")]
        [Tooltip("Projectile range (for ranged units)")]
        [SerializeField, Range(0, 50), ShowIf("HasRangedDamage")]
        private float _projectileRange = 0f;
        
        [FoldoutGroup("Combat Stats/Attack Parameters")]
        [Tooltip("Time between attacks (seconds)")]
        [SerializeField, Range(0.1f, 5f)]
        private float _hitSpeed = 1.5f;
        
        [FoldoutGroup("Combat Stats/Attack Parameters")]
        [Tooltip("Attack preparation time")]
        [SerializeField, Range(0, 3f), ShowIf("HasRangedDamage")]
        private float _loadTime = 0f;
        
        [FoldoutGroup("Movement Stats")]
        [Tooltip("Movement speed")]
        [SerializeField, Range(1, 10), ProgressBar(1, 10, ColorGetter = "GetSpeedColor")]
        private float _moveSpeed = 3.0f;
        
        [FoldoutGroup("Movement Stats")]
        [Tooltip("Time needed to deploy unit")]
        [SerializeField, Range(0, 5)]
        private float _deployTime = 1.0f;
        
        [FoldoutGroup("Detection Stats")]
        [Tooltip("Enemy detection range")]
        [SerializeField, Range(3, 20)]
        private float _detectionRange = 10f;
        
        [FoldoutGroup("Visual Settings")]
        [Tooltip("Unit icon for UI")]
        [SerializeField, PreviewField(80)]
        private Sprite _icon;
        
        [FoldoutGroup("Visual Settings")]
        [Tooltip("Unit team color")]
        [SerializeField, ColorUsage(true)]
        private Color _unitColor = Color.white;

        // Properties
        public uint UnitId => _unitId;
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
        public Sprite Icon => _icon;
        public Color UnitColor => _unitColor;

        // Helper methods for Odin
        private bool HasRangedDamage => _damageRanged > 0;
        
        private Color GetHealthColor => new Color(0.2f, 0.6f, 0.2f);
        private Color GetShieldColor => new Color(0.2f, 0.2f, 0.8f);
        private Color GetDamageColor => new Color(0.8f, 0.2f, 0.2f);
        private Color GetDPSColor => new Color(0.8f, 0.4f, 0.0f);
        private Color GetSpeedColor => new Color(0.8f, 0.8f, 0.0f);

        [Button("Calculate DPS"), PropertyTooltip("Calculate damage per second")]
        [OnInspectorGUI]
        private void CalculateDamagePerSecond()
        {
            float effectiveDamage = Mathf.Max(_damage, _damageRanged);
            _damagePerSecond = effectiveDamage / _hitSpeed;
        }

        [OnValueChanged("ValidateData")]
        private void ValidateData()
        {
            // Ensure ranged units have projectile range
            if (_damageRanged > 0 && _projectileRange <= 0)
            {
                _projectileRange = 10f;
            }
            
            // Calculate DPS
            CalculateDamagePerSecond();
        }
    }
}