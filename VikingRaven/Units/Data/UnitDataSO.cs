using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Units.Components;
using System.Collections.Generic;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Data
{
    /// <summary>
    /// Enhanced ScriptableObject that defines data for a unit type with comprehensive properties
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnitData", menuName = "VikingRaven/Unit Data SO")]
    public class UnitDataSO : SerializedScriptableObject
    {
        [FoldoutGroup("Basic Information")]
        [Tooltip("Unique identifier for this unit type")]
        [SerializeField] private string _unitId;
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("Display name of the unit")]
        [SerializeField] private string _displayName;
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("Description of the unit")]
        [TextArea(3, 5)]
        [SerializeField] private string _description;
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("The prefab for this unit type")]
        [SerializeField, PreviewField(60), Required]
        private GameObject _prefab;
        
        [FoldoutGroup("Basic Information")]
        [Tooltip("Type of unit (Infantry, Archer, Pike)")]
        [SerializeField, EnumToggleButtons]
        private UnitType _unitType;
        
        [TitleGroup("Combat Stats", "Combat Statistics and Abilities")]
        [FoldoutGroup("Combat Stats/Health")]
        [Tooltip("Base health points")]
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
        [Tooltip("Attack range in units")]
        [SerializeField, Range(1, 20)]
        private float _range = 2f;
        
        [FoldoutGroup("Combat Stats/Attack Parameters")]
        [Tooltip("Projectile range (for ranged units)")]
        [SerializeField, Range(0, 50), ShowIf("HasRangedDamage")]
        private float _projectileRange = 0f;
        
        [FoldoutGroup("Combat Stats/Attack Parameters")]
        [Tooltip("Time between attacks in seconds")]
        [SerializeField, Range(0.1f, 5f)]
        private float _hitSpeed = 1.5f;
        
        [FoldoutGroup("Combat Stats/Attack Parameters")]
        [Tooltip("Time to load/prepare attack")]
        [SerializeField, Range(0, 3f), ShowIf("HasRangedDamage")]
        private float _loadTime = 0f;
        
        [FoldoutGroup("Movement Stats")]
        [Tooltip("Speed at which the unit moves")]
        [SerializeField, Range(1, 10), ProgressBar(1, 10, ColorGetter = "GetSpeedColor")]
        private float _moveSpeed = 3.0f;
        
        [FoldoutGroup("Movement Stats")]
        [Tooltip("Time needed to deploy unit")]
        [SerializeField, Range(0, 5)]
        private float _deployTime = 1.0f;
        
        [FoldoutGroup("Detection Stats")]
        [Tooltip("Range at which the unit can detect enemies (in tiles)")]
        [SerializeField, Range(3, 20)]
        private float _detectionRange = 10f;
        
        [FoldoutGroup("Unit Count")]
        [Tooltip("Number of units in a batch")]
        [SerializeField, Range(1, 20)]
        private int _count = 1;
        
        [FoldoutGroup("Ability Settings")]
        [Tooltip("Special ability of the unit")]
        [SerializeField, ValueDropdown("GetAvailableAbilities")]
        private string _ability = "";
        
        [FoldoutGroup("Ability Settings")]
        [Tooltip("Cost to use ability")]
        [SerializeField, Range(0, 100), EnableIf("HasAbility")]
        private float _abilityCost = 0f;
        
        [FoldoutGroup("Ability Settings")]
        [Tooltip("Cooldown time for ability")]
        [SerializeField, Range(0, 60), EnableIf("HasAbility")]
        private float _abilityCooldown = 0f;
        
        [FoldoutGroup("Ability Settings")]
        [Tooltip("Additional parameters for ability")]
        [SerializeField, TextArea(2, 5), EnableIf("HasAbility")]
        private string _abilityParameters = "";
        
        [FoldoutGroup("Visual Settings")]
        [Tooltip("Unit's icon for UI")]
        [SerializeField, PreviewField(80)]
        private Sprite _icon;
        
        [FoldoutGroup("Visual Settings")]
        [Tooltip("Color to represent this unit type")]
        [SerializeField, ColorUsage(true)]
        private Color _unitColor = Color.white;
        
        [TabGroup("Components", "Required Components")]
        [SerializeField]
        private List<string> _requiredComponents = new List<string>
        {
            "TransformComponent",
            "HealthComponent",
            "CombatComponent",
            "AggroDetectionComponent",
            "StateComponent", 
            "FormationComponent",
            "NavigationComponent"
        };
        
        // Dropdown options for abilities
        private string[] GetAvailableAbilities()
        {
            return new string[] { "", "Charge", "Heal", "RangedAttack", "Shield", "Stealth", "Taunt" };
        }
        
        // Helper methods for Odin Inspector
        private bool HasAbility => !string.IsNullOrEmpty(_ability);
        private bool HasRangedDamage => _damageRanged > 0;
        
        // Color getters for progress bars
        private Color GetHealthColor => new Color(0.2f, 0.6f, 0.2f);
        private Color GetShieldColor => new Color(0.2f, 0.2f, 0.8f);
        private Color GetDamageColor => new Color(0.8f, 0.2f, 0.2f);
        private Color GetDPSColor => new Color(0.8f, 0.4f, 0.0f);
        private Color GetSpeedColor => new Color(0.8f, 0.8f, 0.0f);
        
        // Public properties for all fields
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
        public IReadOnlyList<string> RequiredComponents => _requiredComponents;

        // Odin Inspector method to calculate DPS whenever relevant values change
        [Button("Calculate DPS"), PropertyTooltip("Recalculate damage per second")]
        [OnInspectorGUI]
        private void CalculateDamagePerSecond()
        {
            float effectiveDamage = Mathf.Max(_damage, _damageRanged);
            _damagePerSecond = effectiveDamage / _hitSpeed;
        }
        
        // Method to validate data on value change
        [OnValueChanged("ValidateData")]
        private void ValidateData()
        {
            // Ensure ranged units have projectile range
            if (_damageRanged > 0 && _projectileRange <= 0)
            {
                _projectileRange = 10f; // Default value
            }
            
            // Ensure ability settings are consistent
            if (string.IsNullOrEmpty(_ability))
            {
                _abilityCost = 0f;
                _abilityCooldown = 0f;
                _abilityParameters = "";
            }
            
            // Calculate damage per second
            CalculateDamagePerSecond();
            
            // Add AbilityComponent to required components if there's an ability
            if (!string.IsNullOrEmpty(_ability) && !_requiredComponents.Contains("AbilityComponent"))
            {
                _requiredComponents.Add("AbilityComponent");
            }
        }

        /// <summary>
        /// Method to apply unit data to an entity's components
        /// </summary>
        public void ApplyToUnit(GameObject unitObject)
        {
            if (unitObject == null)
                return;
                
            // Apply to UnitTypeComponent
            var unitTypeComponent = unitObject.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent != null)
            {
                unitTypeComponent.SetUnitType(_unitType);
            }
            
            // Apply to HealthComponent
            var healthComponent = unitObject.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                healthComponent.SetMaxHealth(_hitPoints);
                healthComponent.SetArmor(_shield);
            }
            
            // Apply to CombatComponent
            var combatComponent = unitObject.GetComponent<CombatComponent>();
            if (combatComponent != null)
            {
                combatComponent.SetAttackDamage(_damage);
                combatComponent.SetAttackRange(_range);
                combatComponent.SetAttackCooldown(_hitSpeed);
                combatComponent.SetMoveSpeed(_moveSpeed);
                
                // Configure ranged attack if available
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
            
            // Apply to AggroDetectionComponent
            var aggroComponent = unitObject.GetComponent<AggroDetectionComponent>();
            if (aggroComponent != null)
            {
                aggroComponent.SetAggroRange(_detectionRange);
            }
            
            // Apply to AbilityComponent if it exists
            var abilityComponent = unitObject.GetComponent<AbilityComponent>();
            if (abilityComponent != null && !string.IsNullOrEmpty(_ability))
            {
                abilityComponent.SetAbility(_ability, _abilityCost, _abilityCooldown, _abilityParameters);
            }
        }
        
        /// <summary>
        /// Method to create a clone of this data
        /// </summary>
        public UnitDataSO Clone()
        {
            var clone = CreateInstance<UnitDataSO>();
            
            // Copy all basic information
            clone._unitId = this._unitId;
            clone._displayName = this._displayName;
            clone._description = this._description;
            clone._prefab = this._prefab;
            clone._unitType = this._unitType;
            clone._icon = this._icon;
            clone._unitColor = this._unitColor;
            
            // Copy all combat stats
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
            
            // Copy all movement stats
            clone._moveSpeed = this._moveSpeed;
            clone._deployTime = this._deployTime;
            
            // Copy all detection stats
            clone._detectionRange = this._detectionRange;
            
            // Copy unit count
            clone._count = this._count;
            
            // Copy ability settings
            clone._ability = this._ability;
            clone._abilityCost = this._abilityCost;
            clone._abilityCooldown = this._abilityCooldown;
            clone._abilityParameters = this._abilityParameters;
            
            // Copy required components
            clone._requiredComponents = new List<string>(this._requiredComponents);
            
            return clone;
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Setup prefab with all required components
        /// </summary>
        [Button("Setup Prefab Components"), PropertyTooltip("Add required components to the prefab")]
        [FoldoutGroup("Basic Information")]
        public void SetupPrefabComponents()
        {
            if (_prefab == null)
            {
                Debug.LogError("UnitDataSO: Cannot setup components - prefab is null");
                return;
            }
            
            // Open the prefab for editing
            UnityEditor.AssetDatabase.OpenAsset(_prefab);
            GameObject prefabInstance = UnityEditor.PrefabUtility.LoadPrefabContents(UnityEditor.AssetDatabase.GetAssetPath(_prefab));
            
            // Add base entity if not exists
            EnsureComponent<BaseEntity>(prefabInstance);
            
            // Add required components from the list
            foreach (var componentName in _requiredComponents)
            {
                System.Type componentType = System.Type.GetType("VikingRaven.Units.Components." + componentName);
                if (componentType != null)
                {
                    EnsureComponent(prefabInstance, componentType);
                }
                else
                {
                    Debug.LogWarning($"UnitDataSO: Component type {componentName} not found");
                }
            }
            
            // Add ability component if an ability is specified
            if (!string.IsNullOrEmpty(_ability) && !_requiredComponents.Contains("AbilityComponent"))
            {
                EnsureComponent<AbilityComponent>(prefabInstance);
            }
            
            // Save changes to the prefab
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefabInstance, UnityEditor.AssetDatabase.GetAssetPath(_prefab));
            UnityEditor.PrefabUtility.UnloadPrefabContents(prefabInstance);
            
            Debug.Log($"UnitDataSO: Setup components for prefab {_prefab.name}");
        }
        
        /// <summary>
        /// Add missing components to the prefab
        /// </summary>
        [Button("Check Missing Components"), PropertyTooltip("Check which required components are missing")]
        [TabGroup("Components")]
        public void CheckMissingComponents()
        {
            if (_prefab == null)
            {
                Debug.LogError("UnitDataSO: Cannot check components - prefab is null");
                return;
            }
            
            List<string> missingComponents = new List<string>();
            
            foreach (var componentName in _requiredComponents)
            {
                System.Type componentType = System.Type.GetType("VikingRaven.Units.Components." + componentName);
                if (componentType != null)
                {
                    if (_prefab.GetComponent(componentType) == null)
                    {
                        missingComponents.Add(componentName);
                    }
                }
                else
                {
                    missingComponents.Add(componentName + " (Type not found)");
                }
            }
            
            if (missingComponents.Count > 0)
            {
                Debug.LogWarning($"UnitDataSO: Prefab {_prefab.name} is missing these components: " + string.Join(", ", missingComponents));
            }
            else
            {
                Debug.Log($"UnitDataSO: Prefab {_prefab.name} has all required components");
            }
        }
        
        /// <summary>
        /// Helper method to ensure a component exists
        /// </summary>
        private T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
                Debug.Log($"UnitDataSO: Added {typeof(T).Name} to prefab {gameObject.name}");
            }
            return component;
        }
        
        /// <summary>
        /// Helper method to ensure a component exists using Type
        /// </summary>
        private Component EnsureComponent(GameObject gameObject, System.Type componentType)
        {
            Component component = gameObject.GetComponent(componentType);
            if (component == null)
            {
                component = gameObject.AddComponent(componentType);
                Debug.Log($"UnitDataSO: Added {componentType.Name} to prefab {gameObject.name}");
            }
            return component;
        }
        
        /// <summary>
        /// Estimate unit balancing score
        /// </summary>
        [Button("Calculate Unit Balance Score"), PropertyTooltip("Calculate a rough balance score")]
        [TabGroup("Components", "Balance")]
        public void CalculateBalanceScore()
        {
            float healthScore = _hitPoints * 0.02f + _shield * 0.05f;
            float damageScore = _damage * 0.1f + _damageRanged * 0.15f;
            float speedScore = _moveSpeed * 0.8f;
            float specialScore = !string.IsNullOrEmpty(_ability) ? 10f : 0f;
            
            float totalScore = healthScore + damageScore + speedScore + specialScore;
            
            Debug.Log($"UnitDataSO: {_displayName} balance score: {totalScore:F2}");
            Debug.Log($"- Health: {healthScore:F2}, Damage: {damageScore:F2}, Speed: {speedScore:F2}, Special: {specialScore:F2}");
        }
#endif
    }
}