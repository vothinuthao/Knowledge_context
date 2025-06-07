using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    /// <summary>
    /// Enhanced Unit Type Component with improved integration to enhanced combat system
    /// Phase 1 Enhancement: Deep integration with CombatComponent, HealthComponent, and WeaponComponent
    /// </summary>
    public class UnitTypeComponent : BaseComponent
    {
        #region Unit Type Configuration
        
        [TitleGroup("Unit Type Configuration")]
        [InfoBox("Unit type determines combat characteristics, weapon preferences, and tactical roles.", InfoMessageType.Info)]
        
        [SerializeField, EnumToggleButtons] private UnitType _unitType = UnitType.Infantry;
        [SerializeField, ReadOnly] private UnitRole _primaryRole = UnitRole.Frontline;
        [SerializeField, ReadOnly] private UnitRole _secondaryRole = UnitRole.None;

        #endregion

        #region Unit Characteristics
        
        [TitleGroup("Unit Characteristics")]
        [InfoBox("Characteristics automatically configured based on unit type.", InfoMessageType.None)]
        
        [ShowInInspector, ReadOnly] private TacticalPreference _tacticalPreference;
        [ShowInInspector, ReadOnly] private FormationPreference _formationPreference;
        [ShowInInspector, ReadOnly] private CombatRange _preferredCombatRange;
        [ShowInInspector, ReadOnly] private float _formationEfficiencyBonus;

        #endregion

        #region Enhanced Integration
        
        [TitleGroup("Enhanced Integration")]
        [InfoBox("Integration status with enhanced combat components.", InfoMessageType.Warning)]
        
        [ShowInInspector, ReadOnly] private bool _combatComponentConfigured = false;
        [ShowInInspector, ReadOnly] private bool _healthComponentConfigured = false;
        [ShowInInspector, ReadOnly] private bool _weaponComponentConfigured = false;
        [ShowInInspector, ReadOnly] private bool _allComponentsReady = false;

        #endregion

        #region Component References
        
        private CombatComponent _combatComponent;
        private HealthComponent _healthComponent;
        private WeaponComponent _weaponComponent;
        private FormationComponent _formationComponent;

        #endregion

        #region Public Properties

        public UnitType UnitType => _unitType;
        public UnitRole PrimaryRole => _primaryRole;
        public UnitRole SecondaryRole => _secondaryRole;
        public TacticalPreference TacticalPreference => _tacticalPreference;
        public FormationPreference FormationPreference => _formationPreference;
        public CombatRange PreferredCombatRange => _preferredCombatRange;
        public float FormationEfficiencyBonus => _formationEfficiencyBonus;
        public bool AllComponentsReady => _allComponentsReady;

        #endregion

        #region Events

        public event System.Action<UnitType> OnUnitTypeChanged;
        public event System.Action OnComponentsConfigured;

        #endregion

        #region Unity Lifecycle

        public override void Initialize()
        {
            base.Initialize();
            
            CacheComponentReferences();
            ConfigureUnitCharacteristics();
            ConfigureEnhancedComponents();
            
            Debug.Log($"UnitTypeComponent: Enhanced unit type system initialized for {Entity.Id}");
        }

        private void Start()
        {
            // Delay configuration to ensure all components are ready
            Invoke(nameof(DelayedConfiguration), 0.1f);
        }

        #endregion

        #region Initialization and Configuration

        /// <summary>
        /// Cache references to other components
        /// </summary>
        private void CacheComponentReferences()
        {
            _combatComponent = Entity.GetComponent<CombatComponent>();
            _healthComponent = Entity.GetComponent<HealthComponent>();
            _weaponComponent = Entity.GetComponent<WeaponComponent>();
            _formationComponent = Entity.GetComponent<FormationComponent>();
        }

        /// <summary>
        /// Configure unit characteristics based on type
        /// </summary>
        private void ConfigureUnitCharacteristics()
        {
            switch (_unitType)
            {
                case UnitType.Infantry:
                    ConfigureInfantryCharacteristics();
                    break;
                case UnitType.Archer:
                    ConfigureArcherCharacteristics();
                    break;
                case UnitType.Pike:
                    ConfigurePikeCharacteristics();
                    break;
                default:
                    ConfigureDefaultCharacteristics();
                    break;
            }
        }

        /// <summary>
        /// Configure enhanced components integration
        /// </summary>
        private void ConfigureEnhancedComponents()
        {
            ConfigureCombatComponent();
            ConfigureHealthComponent();
            ConfigureWeaponComponent();
            ConfigureFormationComponent();
            
            CheckAllComponentsReady();
        }

        /// <summary>
        /// Delayed configuration to ensure component readiness
        /// </summary>
        private void DelayedConfiguration()
        {
            // Re-cache components in case they were added after initialization
            CacheComponentReferences();
            
            // Re-configure if components are now available
            ConfigureEnhancedComponents();
            
            if (_allComponentsReady)
            {
                OnComponentsConfigured?.Invoke();
                Debug.Log($"UnitTypeComponent: All enhanced components configured for {_unitType}");
            }
        }

        #endregion

        #region Unit Type Characteristics Configuration

        /// <summary>
        /// Configure Infantry characteristics
        /// </summary>
        private void ConfigureInfantryCharacteristics()
        {
            _primaryRole = UnitRole.Frontline;
            _secondaryRole = UnitRole.Support;
            _tacticalPreference = TacticalPreference.Aggressive;
            _formationPreference = FormationPreference.Normal;
            _preferredCombatRange = CombatRange.Melee;
            _formationEfficiencyBonus = 1.0f;
        }

        /// <summary>
        /// Configure Archer characteristics
        /// </summary>
        private void ConfigureArcherCharacteristics()
        {
            _primaryRole = UnitRole.Ranged;
            _secondaryRole = UnitRole.Support;
            _tacticalPreference = TacticalPreference.Defensive;
            _formationPreference = FormationPreference.Loose;
            _preferredCombatRange = CombatRange.Ranged;
            _formationEfficiencyBonus = 0.8f; // Less effective in tight formations
        }

        /// <summary>
        /// Configure Pike characteristics
        /// </summary>
        private void ConfigurePikeCharacteristics()
        {
            _primaryRole = UnitRole.HeavyFrontline;
            _secondaryRole = UnitRole.AntiCavalry;
            _tacticalPreference = TacticalPreference.Defensive;
            _formationPreference = FormationPreference.Phalanx;
            _preferredCombatRange = CombatRange.Extended;
            _formationEfficiencyBonus = 1.3f; // Bonus in formations
        }

        /// <summary>
        /// Configure default characteristics
        /// </summary>
        private void ConfigureDefaultCharacteristics()
        {
            _primaryRole = UnitRole.Frontline;
            _secondaryRole = UnitRole.None;
            _tacticalPreference = TacticalPreference.Balanced;
            _formationPreference = FormationPreference.Normal;
            _preferredCombatRange = CombatRange.Melee;
            _formationEfficiencyBonus = 1.0f;
        }

        #endregion

        #region Enhanced Component Configuration

        /// <summary>
        /// Configure CombatComponent integration
        /// </summary>
        private void ConfigureCombatComponent()
        {
            if (_combatComponent == null)
            {
                Debug.LogWarning($"UnitTypeComponent: CombatComponent not found for {Entity.Id}");
                return;
            }

            // Combat component configuration is handled in CombatComponent.InitializeStatsFromUnitType()
            // This method ensures the integration is working
            _combatComponentConfigured = true;
            
            Debug.Log($"UnitTypeComponent: CombatComponent configured for {_unitType}");
        }

        /// <summary>
        /// Configure HealthComponent integration
        /// </summary>
        private void ConfigureHealthComponent()
        {
            if (_healthComponent == null)
            {
                Debug.LogWarning($"UnitTypeComponent: HealthComponent not found for {Entity.Id}");
                return;
            }

            // Health component configuration is handled in HealthComponent.LoadStatsFromUnitData()
            // This method ensures the integration is working
            _healthComponentConfigured = true;
            
            Debug.Log($"UnitTypeComponent: HealthComponent configured for {_unitType}");
        }

        /// <summary>
        /// Configure WeaponComponent integration
        /// </summary>
        private void ConfigureWeaponComponent()
        {
            if (_weaponComponent == null)
            {
                Debug.LogWarning($"UnitTypeComponent: WeaponComponent not found for {Entity.Id}");
                return;
            }

            _weaponComponentConfigured = true;
            
            Debug.Log($"UnitTypeComponent: WeaponComponent configured for {_unitType}");
        }

        /// <summary>
        /// Configure FormationComponent integration
        /// </summary>
        private void ConfigureFormationComponent()
        {
            if (_formationComponent == null)
            {
                Debug.LogWarning($"UnitTypeComponent: FormationComponent not found for {Entity.Id}");
                return;
            }

            ApplyFormationBonuses();
        }

        /// <summary>
        /// Apply formation bonuses based on unit type
        /// </summary>
        private void ApplyFormationBonuses()
        {
            if (!_formationComponent) return;

            // Apply formation efficiency bonus
            // This could be used by FormationSystem to calculate effectiveness
            
            // Set formation role based on unit type
            FormationRole recommendedRole = GetRecommendedFormationRole();
            _formationComponent.SetFormationRole(recommendedRole);
            
            Debug.Log($"UnitTypeComponent: Formation bonuses applied for {_unitType}");
        }

        /// <summary>
        /// Get recommended formation role based on unit type
        /// </summary>
        private FormationRole GetRecommendedFormationRole()
        {
            return _unitType switch
            {
                UnitType.Infantry => FormationRole.FrontLine,
                UnitType.Pike => FormationRole.FrontLine,
                UnitType.Archer => FormationRole.Support,
                _ => FormationRole.Follower
            };
        }

        /// <summary>
        /// Check if all components are ready and configured
        /// </summary>
        private void CheckAllComponentsReady()
        {
            _allComponentsReady = _combatComponentConfigured && 
                                 _healthComponentConfigured && 
                                 _weaponComponentConfigured;
            
            if (_allComponentsReady)
            {
                Debug.Log($"UnitTypeComponent: All enhanced components ready for {_unitType}");
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Change unit type and reconfigure all components
        /// </summary>
        public void SetUnitType(UnitType unitType)
        {
            if (!System.Enum.IsDefined(typeof(UnitType), unitType))
            {
                Debug.LogError($"UnitTypeComponent: Invalid UnitType value: {unitType}");
                return;
            }

            if (_unitType == unitType) return;

            UnitType oldType = _unitType;
            _unitType = unitType;

            // Reconfigure characteristics
            ConfigureUnitCharacteristics();
            
            // Reconfigure enhanced components
            ConfigureEnhancedComponents();

            // Trigger event
            OnUnitTypeChanged?.Invoke(unitType);

            Debug.Log($"UnitTypeComponent: Changed unit type from {oldType} to {unitType}");
        }

        /// <summary>
        /// Get combat effectiveness multiplier for current formation
        /// </summary>
        public float GetFormationCombatEffectiveness(FormationType currentFormation)
        {
            float baseEffectiveness = _formationEfficiencyBonus;
            
            // Apply formation preference bonus
            if (IsPreferredFormation(currentFormation))
            {
                baseEffectiveness *= 1.2f; // 20% bonus for preferred formation
            }
            
            return baseEffectiveness;
        }

        /// <summary>
        /// Check if the given formation is preferred for this unit type
        /// </summary>
        public bool IsPreferredFormation(FormationType formation)
        {
            return (_formationPreference, formation) switch
            {
                (FormationPreference.Normal, FormationType.Normal) => true,
                (FormationPreference.Phalanx, FormationType.Phalanx) => true,
                (FormationPreference.Loose, FormationType.Normal) => true, // Archers prefer loose formations
                _ => false
            };
        }

        /// <summary>
        /// Get tactical AI weight modifier for behavior selection
        /// </summary>
        public float GetTacticalWeightModifier(string behaviorName)
        {
            return behaviorName.ToLower() switch
            {
                "aggressive" when _tacticalPreference == TacticalPreference.Aggressive => 1.5f,
                "defensive" when _tacticalPreference == TacticalPreference.Defensive => 1.5f,
                "phalanx" when _formationPreference == FormationPreference.Phalanx => 2.0f,
                "ranged" when _preferredCombatRange == CombatRange.Ranged => 1.8f,
                _ => 1.0f
            };
        }

        #endregion

        #region Debug Methods

        [Button("Show Unit Configuration"), FoldoutGroup("Debug Tools")]
        private void ShowUnitConfiguration()
        {
            Debug.Log($"=== Unit Type Configuration ===");
            Debug.Log($"Unit Type: {_unitType}");
            Debug.Log($"Primary Role: {_primaryRole}");
            Debug.Log($"Secondary Role: {_secondaryRole}");
            Debug.Log($"Tactical Preference: {_tacticalPreference}");
            Debug.Log($"Formation Preference: {_formationPreference}");
            Debug.Log($"Combat Range: {_preferredCombatRange}");
            Debug.Log($"Formation Efficiency Bonus: {_formationEfficiencyBonus:F2}");
            Debug.Log($"All Components Ready: {_allComponentsReady}");
        }

        [Button("Test Formation Effectiveness"), FoldoutGroup("Debug Tools")]
        private void TestFormationEffectiveness()
        {
            Debug.Log($"=== Formation Effectiveness Test ===");
            foreach (FormationType formation in System.Enum.GetValues(typeof(FormationType)))
            {
                float effectiveness = GetFormationCombatEffectiveness(formation);
                bool isPreferred = IsPreferredFormation(formation);
                Debug.Log($"{formation}: {effectiveness:F2}x effectiveness {(isPreferred ? "(PREFERRED)" : "")}");
            }
        }

        [Button("Force Reconfigure Components"), FoldoutGroup("Debug Tools")]
        private void ForceReconfigureComponents()
        {
            _combatComponentConfigured = false;
            _healthComponentConfigured = false;
            _weaponComponentConfigured = false;
            _allComponentsReady = false;
            
            DelayedConfiguration();
        }

        #endregion
    }

    #region Supporting Enums

    public enum UnitType
    {
        Infantry,   // Balanced melee fighters
        Archer,     // Ranged units
        Pike        // Anti-cavalry spear units
    }

    /// <summary>
    /// Tactical roles for unit specialization
    /// </summary>
    public enum UnitRole
    {
        None,
        Frontline,        // Front line fighters
        HeavyFrontline,   // Heavy armored front line
        Ranged,          // Ranged attackers
        Support,         // Support units
        AntiCavalry,     // Anti-cavalry specialists
        Skirmisher       // Light mobile units
    }

    /// <summary>
    /// Tactical behavior preferences
    /// </summary>
    public enum TacticalPreference
    {
        Defensive,   // Prefer defensive tactics
        Balanced,    // Balanced approach
        Aggressive   // Prefer aggressive tactics
    }

    /// <summary>
    /// Formation preferences for unit types
    /// </summary>
    public enum FormationPreference
    {
        Normal,     // Standard formations
        Phalanx,    // Tight combat formations
        Loose       // Spread out formations
    }

    /// <summary>
    /// Preferred combat engagement ranges
    /// </summary>
    public enum CombatRange
    {
        Melee,      // Close combat
        Extended,   // Spear/pike range
        Ranged      // Bow/crossbow range
    }

    #endregion
}