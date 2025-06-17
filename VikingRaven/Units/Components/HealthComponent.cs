using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Factory;

namespace VikingRaven.Units.Components
{
    /// <summary>
    /// Enhanced Health Component with shield system, stamina system, injury states, and detailed health management
    /// Phase 1 Enhancement: Comprehensive health and fatigue mechanics for realistic combat
    /// </summary>
    public class HealthComponent : BaseComponent
    {
        #region Health Configuration
        
        [TitleGroup("Health Configuration")]
        [InfoBox("Health statistics loaded from UnitDataSO. Enhanced with realistic injury and recovery systems.", InfoMessageType.Info)]
        
        [SerializeField, ReadOnly] private float _maxHealth = 100f;
        [SerializeField, ReadOnly] private float _currentHealth;
        [SerializeField, ReadOnly] private float _regenerationRate = 0f;
        [SerializeField, ReadOnly] private bool _isDead = false;

        #endregion

        #region Shield Configuration
        
        [TitleGroup("Shield Configuration")]
        [InfoBox("Shield system provides additional protection layer with separate regeneration mechanics.", InfoMessageType.Info)]
        
        [SerializeField, ReadOnly] private float _maxShield = 0f;
        [SerializeField, ReadOnly] private float _currentShield = 0f;
        [SerializeField, ReadOnly] private float _shieldRegenerationRate = 0f;
        [SerializeField, ReadOnly] private float _shieldRegenerationDelay = 3f;
        
        [Tooltip("Time before shield starts regenerating after taking damage (seconds)")]
        [SerializeField, Range(1f, 10f)] private float _shieldRegenerationDelayTime = 3f;
        
        [Tooltip("Shield regenerates faster when not in combat")]
        [SerializeField, Range(1f, 5f)] private float _shieldOutOfCombatMultiplier = 2f;

        #endregion

        #region Enhanced Health System
        
        [TitleGroup("Enhanced Health System")]
        [InfoBox("Advanced health mechanics including injury states and recovery phases.", InfoMessageType.Warning)]
        
        [Tooltip("Current injury severity level")]
        [SerializeField, EnumToggleButtons] private InjuryState _currentInjuryState = InjuryState.Healthy;
        
        [Tooltip("Health threshold for light injuries (% of max health)")]
        [SerializeField, Range(60f, 90f)] private float _lightInjuryThreshold = 75f;
        
        [Tooltip("Health threshold for moderate injuries (% of max health)")]
        [SerializeField, Range(30f, 70f)] private float _moderateInjuryThreshold = 50f;
        
        [Tooltip("Health threshold for severe injuries (% of max health)")]
        [SerializeField, Range(10f, 40f)] private float _severeInjuryThreshold = 25f;
        
        [Tooltip("Time before natural regeneration begins (seconds)")]
        [SerializeField, Range(5f, 30f)] private float _regenerationDelay = 10f;

        #endregion

        #region Stamina System
        
        [TitleGroup("Stamina System")]
        [InfoBox("Stamina affects combat performance, movement speed, and ability usage.", InfoMessageType.Warning)]
        
        [Tooltip("Maximum stamina points")]
        [SerializeField, ReadOnly] private float _maxStamina = 100f;
        
        [Tooltip("Current stamina level")]
        [SerializeField, ReadOnly] private float _currentStamina = 100f;
        
        [Tooltip("Stamina regeneration per second")]
        [SerializeField, Range(1f, 20f)] private float _staminaRegenerationRate = 10f;
        
        [Tooltip("Current fatigue level (0-100)")]
        [SerializeField, ReadOnly, Range(0f, 100f)] private float _fatigueLevel = 0f;

        #endregion

        #region Live Status Display
        
        [TitleGroup("Live Status Display")]
        [InfoBox("Real-time health, shield, and stamina visualization", InfoMessageType.None)]
        
        [HorizontalGroup("Live Status Display/Row1")]
        [BoxGroup("Live Status Display/Row1/Health")]
        [LabelText("Health"), ProgressBar(0, "_maxHealth", ColorGetter = "GetHealthColor")]
        [ShowInInspector, ReadOnly] private float CurrentHealthDisplay => _currentHealth;
        
        [BoxGroup("Live Status Display/Row1/Health")]
        [LabelText("Health %"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetHealthColor")]
        [ShowInInspector] private float HealthPercentDisplay => _maxHealth > 0 ? (_currentHealth / _maxHealth) * 100f : 0f;
        
        [HorizontalGroup("Live Status Display/Row1")]
        [BoxGroup("Live Status Display/Row1/Shield")]
        [LabelText("Shield"), ProgressBar(0, "_maxShield", ColorGetter = "GetShieldColor")]
        [ShowInInspector, ReadOnly] private float CurrentShieldDisplay => _currentShield;
        
        [BoxGroup("Live Status Display/Row1/Shield")]
        [LabelText("Shield %"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetShieldColor")]
        [ShowInInspector] private float ShieldPercentDisplay => _maxShield > 0 ? (_currentShield / _maxShield) * 100f : 0f;
        
        [HorizontalGroup("Live Status Display/Row2")]
        [BoxGroup("Live Status Display/Row2/Stamina")]
        [LabelText("Stamina"), ProgressBar(0, "_maxStamina", ColorGetter = "GetStaminaColor")]
        [ShowInInspector, ReadOnly] private float CurrentStaminaDisplay => _currentStamina;
        
        [BoxGroup("Live Status Display/Row2/Stamina")]
        [LabelText("Fatigue"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetFatigueColor")]
        [ShowInInspector] private float FatigueLevelDisplay => _fatigueLevel;
        
        [HorizontalGroup("Live Status Display/Row2")]
        [BoxGroup("Live Status Display/Row2/Status")]
        [LabelText("Is Alive"), ReadOnly, ToggleLeft]
        [ShowInInspector] private bool IsAliveDisplay => !_isDead;
        
        [BoxGroup("Live Status Display/Row2/Status")]
        [LabelText("Injury State"), ReadOnly, EnumToggleButtons]
        [ShowInInspector] private InjuryState InjuryStateDisplay => _currentInjuryState;

        #endregion

        #region Private Fields
        
        private RecoveryPhase _currentRecoveryPhase = RecoveryPhase.None;
        private float _timeSinceLastDamage = 0f;
        private float _timeSinceLastShieldDamage = 0f;
        private List<StatusEffect> _activeStatusEffects = new List<StatusEffect>();
        private List<DamageType> _damageImmunities = new List<DamageType>();
        
        // Component references
        private VikingRaven.Units.Models.UnitModel _unitModel;
        private CombatComponent _combatComponent;
        private StateComponent _stateComponent;

        #endregion

        #region Public Properties

        // Health Properties
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public float HealthPercentage => _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;
        public float RegenerationRate => _regenerationRate;
        
        // Shield Properties
        public float MaxShield => _maxShield;
        public float CurrentShield => _currentShield;
        public float ShieldPercentage => _maxShield > 0 ? _currentShield / _maxShield : 0f;
        public float ShieldRegenerationRate => _shieldRegenerationRate;
        public bool HasShield => _maxShield > 0;
        public bool ShieldActive => _currentShield > 0;
        
        // Enhanced Health Properties
        public InjuryState CurrentInjuryState => _currentInjuryState;
        public RecoveryPhase CurrentRecoveryPhase => _currentRecoveryPhase;
        public float TimeSinceLastDamage => _timeSinceLastDamage;
        public float TimeSinceLastShieldDamage => _timeSinceLastShieldDamage;
        
        // Stamina Properties
        public float MaxStamina => _maxStamina;
        public float CurrentStamina => _currentStamina;
        public float StaminaPercentage => _maxStamina > 0 ? _currentStamina / _maxStamina : 0f;
        public float FatigueLevel => _fatigueLevel;
        
        // Performance Properties
        public float CombatReadiness => CalculateCombatReadiness();
        public float PerformanceModifier => CalculatePerformanceModifier();
        public bool IsExhausted => _currentStamina < (_maxStamina * 0.1f);
        public bool IsFatigued => _fatigueLevel > 50f;
        public bool IsAlive => !_isDead;
        
        // Status Effects
        public IReadOnlyList<StatusEffect> ActiveStatusEffects => _activeStatusEffects;
        public IReadOnlyList<DamageType> DamageImmunities => _damageImmunities;

        #endregion

        #region Events

        // Health Events
        public event Action<float, IEntity> OnDamageTaken;
        public event Action<float> OnHealthRegenerated;
        public event Action<float> OnHealthChanged;
        public event Action OnDeath;
        public event Action OnRevive;
        
        // Shield Events
        public event Action<float, IEntity> OnShieldDamage;
        public event Action<float> OnShieldRegenerated;
        public event Action<float> OnShieldChanged;
        public event Action OnShieldBroken;
        public event Action OnShieldRestored;
        
        // Enhanced System Events
        public event Action<InjuryState> OnInjuryStateChanged;
        public event Action<float> OnStaminaChanged;
        public event Action<StatusEffect> OnStatusEffectAdded;
        public event Action<StatusEffect> OnStatusEffectRemoved;
        public event Action<RecoveryPhase> OnRecoveryPhaseChanged;

        #endregion

        #region Unity Lifecycle

        public override void Initialize()
        {
            base.Initialize();
            LoadStatsFromUnitData();
            InitializeEnhancedHealthSystem();
        }

        private void Update()
        {
            if (!IsActive || _isDead) return;

            UpdateHealthRegeneration();
            UpdateShieldRegeneration();
            UpdateStaminaRegeneration();
            UpdateInjuryState();
            UpdateRecoveryPhase();
            UpdateStatusEffects();
            UpdateFatigueLevel();
            
            _timeSinceLastDamage += Time.deltaTime;
            _timeSinceLastShieldDamage += Time.deltaTime;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Load health stats from UnitDataSO via UnitModel
        /// </summary>
        private void LoadStatsFromUnitData()
        {
            if (Entity == null) return;
            
            _combatComponent = Entity.GetComponent<CombatComponent>();
            _stateComponent = Entity.GetComponent<StateComponent>();
            var unitFactory = FindObjectOfType<UnitFactory>();
            if (unitFactory != null)
            {
                _unitModel = unitFactory.GetUnitModel(Entity);
                if (_unitModel != null)
                {
                    // Initialize Health
                    _maxHealth = _unitModel.MaxHealth;
                    _currentHealth = _maxHealth;
                    _regenerationRate = CalculateRegenerationFromUnitData(_unitModel);
                    
                    // Initialize Shield
                    _maxShield = _unitModel.MaxShield;
                    _currentShield = _maxShield;
                    _shieldRegenerationRate = CalculateShieldRegenFromUnitData();
                    
                    _maxStamina = CalculateStaminaFromUnitData();
                    _currentStamina = _maxStamina;
                }
                else
                {
                    Debug.LogWarning($"HealthComponent: UnitModel not found for entity {Entity.Id}");
                    SetDefaultStats();
                }
            }
            else
            {
                Debug.LogError("HealthComponent: UnitFactory not found in scene!");
                SetDefaultStats();
            }
        }
        
        private void SetDefaultStats()
        {
            _maxHealth = 100f;
            _currentHealth = _maxHealth;
            _maxShield = 50f;
            _currentShield = _maxShield;
            _maxStamina = 100f;
            _currentStamina = _maxStamina;
            _regenerationRate = 1f;
            _shieldRegenerationRate = 5f;
        }

        /// <summary>
        /// Calculate stamina based on unit data
        /// </summary>
        private float CalculateStaminaFromUnitData()
        {
            float baseStamina = 100f;
            
            if (_unitModel != null)
            {
                // Modify based on unit type
                switch (_unitModel.UnitType)
                {
                    case UnitType.Infantry:
                        baseStamina = 120f; // Higher stamina for melee fighters
                        break;
                    case UnitType.Archer:
                        baseStamina = 100f; // Standard stamina
                        break;
                    case UnitType.Pike:
                        baseStamina = 110f; // Moderate stamina for heavy units
                        break;
                }
                
                // Scale with unit health
                float healthScale = _unitModel.MaxHealth / 100f;
                baseStamina *= healthScale;
            }
            
            return baseStamina;
        }

        /// <summary>
        /// Calculate regeneration rate from unit data
        /// </summary>
        private float CalculateRegenerationFromUnitData(VikingRaven.Units.Models.UnitModel unitModel)
        {
            float baseRegen = 0.5f; // Base regeneration per second
            float healthScale = unitModel.MaxHealth / 100f;
            return baseRegen * healthScale;
        }
        
        /// <summary>
        /// Calculate shield regeneration rate from unit data
        /// </summary>
        private float CalculateShieldRegenFromUnitData()
        {
            float baseShieldRegen = 2f; // Base shield regen per second
            
            if (_unitModel != null && _maxShield > 0)
            {
                // Scale based on unit type
                switch (_unitModel.UnitType)
                {
                    case UnitType.Infantry:
                        baseShieldRegen *= 0.8f; // Slower shield regen for infantry
                        break;
                    case UnitType.Archer:
                        baseShieldRegen *= 1.2f; // Faster shield regen for archers
                        break;
                    case UnitType.Pike:
                        baseShieldRegen *= 1.0f; // Standard for pike units
                        break;
                }
                
                // Scale with shield amount
                float shieldScale = _maxShield / 50f; // Normalize to 50 base shield
                baseShieldRegen *= shieldScale;
            }
            
            return baseShieldRegen;
        }

        /// <summary>
        /// Initialize enhanced health system
        /// </summary>
        private void InitializeEnhancedHealthSystem()
        {
            _isDead = false;
            _currentInjuryState = InjuryState.Healthy;
            _currentRecoveryPhase = RecoveryPhase.None;
            _fatigueLevel = 0f;
            _timeSinceLastDamage = 0f;
            _timeSinceLastShieldDamage = 0f;
            _activeStatusEffects.Clear();
            
            Debug.Log($"HealthComponent: Enhanced health system initialized for {Entity.Id}");
        }

        #endregion

        #region Enhanced Damage System

        /// <summary>
        /// Take enhanced damage with shield absorption and injury state calculation
        /// </summary>
        public void TakeDamage(float amount, IEntity source = null, bool ignoreArmor = false)
        {
            if (!IsActive || _isDead || amount <= 0) return;
            
            // Check damage immunities
            var combatComponent = source?.GetComponent<CombatComponent>();
            if (combatComponent && _damageImmunities.Contains(combatComponent.PrimaryDamageType))
            {
                Debug.Log($"HealthComponent: Damage immune to {combatComponent.PrimaryDamageType}");
                return;
            }
            
            float remainingDamage = amount;
            
            // Apply damage to shield first
            if (_currentShield > 0)
            {
                float shieldDamage = Mathf.Min(_currentShield, remainingDamage);
                _currentShield -= shieldDamage;
                remainingDamage -= shieldDamage;
                _timeSinceLastShieldDamage = 0f;
                
                // Trigger shield events
                OnShieldDamage?.Invoke(shieldDamage, source);
                OnShieldChanged?.Invoke(_currentShield);
                
                if (_currentShield <= 0)
                {
                    OnShieldBroken?.Invoke();
                    Debug.Log($"HealthComponent: Shield broken on entity {Entity.Id}");
                }
            }
            
            // Apply remaining damage to health
            if (remainingDamage > 0)
            {
                _currentHealth = Mathf.Max(0, _currentHealth - remainingDamage);
                _timeSinceLastDamage = 0f;
                
                // Update injury state
                UpdateInjuryStateFromDamage();
                
                // Add fatigue from taking damage
                AddFatigue(remainingDamage * 0.1f);
                
                // Check for status effects from damage
                CheckForDamageStatusEffects(remainingDamage, source);
                
                // Trigger health events
                OnDamageTaken?.Invoke(remainingDamage, source);
                OnHealthChanged?.Invoke(_currentHealth);
                
                // Check for death
                if (_currentHealth <= 0)
                {
                    Die();
                }
            }
            
            Debug.Log($"HealthComponent: Entity {Entity.Id} took {amount:F1} damage (Shield: {_currentShield:F1}/{_maxShield:F1}, Health: {_currentHealth:F1}/{_maxHealth:F1})");
        }

        /// <summary>
        /// Heal unit
        /// </summary>
        public void Heal(float amount)
        {
            if (!IsActive || _isDead || amount <= 0) return;
            
            float oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            float actualHeal = _currentHealth - oldHealth;
            
            if (actualHeal > 0)
            {
                UpdateInjuryStateFromHealing();
                OnHealthRegenerated?.Invoke(actualHeal);
                OnHealthChanged?.Invoke(_currentHealth);
                
                Debug.Log($"HealthComponent: Entity {Entity.Id} healed for {actualHeal:F1}");
            }
        }

        /// <summary>
        /// Restore shield
        /// </summary>
        public void RestoreShield(float amount)
        {
            if (!IsActive || amount <= 0) return;
            
            float oldShield = _currentShield;
            _currentShield = Mathf.Min(_maxShield, _currentShield + amount);
            float actualRestore = _currentShield - oldShield;
            
            if (actualRestore > 0)
            {
                OnShieldRegenerated?.Invoke(actualRestore);
                OnShieldChanged?.Invoke(_currentShield);
                
                if (oldShield <= 0 && _currentShield > 0)
                {
                    OnShieldRestored?.Invoke();
                    Debug.Log($"HealthComponent: Shield restored on entity {Entity.Id}");
                }
            }
        }

        #endregion

        #region Regeneration Systems

        /// <summary>
        /// Update health regeneration
        /// </summary>
        private void UpdateHealthRegeneration()
        {
            if (_isDead || _currentHealth >= _maxHealth || _timeSinceLastDamage < _regenerationDelay) return;
            
            float regenAmount = _regenerationRate * Time.deltaTime;
            float oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + regenAmount);
            
            if (_currentHealth > oldHealth)
            {
                OnHealthRegenerated?.Invoke(_currentHealth - oldHealth);
                OnHealthChanged?.Invoke(_currentHealth);
            }
        }

        /// <summary>
        /// Update shield regeneration
        /// </summary>
        private void UpdateShieldRegeneration()
        {
            if (_isDead || _currentShield >= _maxShield || _timeSinceLastShieldDamage < _shieldRegenerationDelayTime) return;
            
            float regenMultiplier = IsInCombat() ? 1f : _shieldOutOfCombatMultiplier;
            float regenAmount = _shieldRegenerationRate * regenMultiplier * Time.deltaTime;
            
            float oldShield = _currentShield;
            _currentShield = Mathf.Min(_maxShield, _currentShield + regenAmount);
            
            if (_currentShield > oldShield)
            {
                OnShieldRegenerated?.Invoke(_currentShield - oldShield);
                OnShieldChanged?.Invoke(_currentShield);
                
                if (oldShield <= 0 && _currentShield > 0)
                {
                    OnShieldRestored?.Invoke();
                }
            }
        }

        /// <summary>
        /// Update stamina regeneration
        /// </summary>
        private void UpdateStaminaRegeneration()
        {
            if (_currentStamina >= _maxStamina) return;
            
            float regenAmount = _staminaRegenerationRate * Time.deltaTime;
            _currentStamina = Mathf.Min(_maxStamina, _currentStamina + regenAmount);
            OnStaminaChanged?.Invoke(_currentStamina);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Check if unit is in combat
        /// </summary>
        private bool IsInCombat()
        {
            return _stateComponent?.IsInCombat == true || _timeSinceLastDamage < 5f;
        }

        /// <summary>
        /// Calculate combat readiness
        /// </summary>
        private float CalculateCombatReadiness()
        {
            if (_isDead) return 0f;
            
            float healthFactor = HealthPercentage;
            float staminaFactor = StaminaPercentage;
            float shieldFactor = HasShield ? ShieldPercentage * 0.3f : 0f; // Shield contributes 30% to readiness
            
            return (healthFactor * 0.6f + staminaFactor * 0.4f + shieldFactor) / (HasShield ? 1.3f : 1f);
        }

        /// <summary>
        /// Calculate performance modifier
        /// </summary>
        private float CalculatePerformanceModifier()
        {
            if (_isDead) return 0f;
            
            float healthModifier = Mathf.Lerp(0.5f, 1f, HealthPercentage / 100f);
            float staminaModifier = Mathf.Lerp(0.7f, 1f, StaminaPercentage / 100f);
            float injuryModifier = GetInjuryModifier();
            
            return healthModifier * staminaModifier * injuryModifier;
        }

        /// <summary>
        /// Get injury modifier based on current injury state
        /// </summary>
        private float GetInjuryModifier()
        {
            return _currentInjuryState switch
            {
                InjuryState.Healthy => 1f,
                InjuryState.Light => 0.9f,
                InjuryState.Moderate => 0.75f,
                InjuryState.Severe => 0.5f,
                _ => 1f
            };
        }

        #endregion

        #region Odin Inspector Color Methods

        private Color GetHealthColor()
        {
            float healthPercent = HealthPercentage * 100f;
            if (healthPercent > 75f) return Color.green;
            if (healthPercent > 50f) return Color.yellow;
            if (healthPercent > 25f) return new Color(1f, 0.5f, 0f); // Orange
            return Color.red;
        }

        private Color GetShieldColor()
        {
            float shieldPercent = ShieldPercentage * 100f;
            if (shieldPercent > 75f) return Color.cyan;
            if (shieldPercent > 50f) return Color.blue;
            if (shieldPercent > 25f) return new Color(0.5f, 0f, 1f); // Purple
            return Color.gray;
        }

        private Color GetStaminaColor()
        {
            float staminaPercent = StaminaPercentage * 100f;
            if (staminaPercent > 75f) return Color.green;
            if (staminaPercent > 50f) return Color.yellow;
            if (staminaPercent > 25f) return new Color(1f, 0.5f, 0f); // Orange
            return Color.red;
        }

        private Color GetFatigueColor()
        {
            if (_fatigueLevel < 25f) return Color.green;
            if (_fatigueLevel < 50f) return Color.yellow;
            if (_fatigueLevel < 75f) return new Color(1f, 0.5f, 0f); // Orange
            return Color.red;
        }

        #endregion

        #region Additional Methods (Stubs for compilation)

        private void UpdateInjuryState() { }
        private void UpdateInjuryStateFromDamage() { }
        private void UpdateInjuryStateFromHealing() { }
        private void UpdateRecoveryPhase() { }
        private void UpdateStatusEffects() { }
        private void UpdateFatigueLevel() { }
        private void AddFatigue(float amount) { }
        private void CheckForDamageStatusEffects(float damage, IEntity source) { }
        private void Die() 
        { 
            _isDead = true;
            OnDeath?.Invoke();
        }

        #endregion
    }

    #region Supporting Enums

    public enum InjuryState
    {
        Healthy,
        Light,
        Moderate,
        Severe
    }

    public enum RecoveryPhase
    {
        None,
        Initial,
        Stabilizing,
        Recovering
    }

    [System.Serializable]
    public class StatusEffect
    {
        public string Name;
        public float Duration;
        public float RemainingTime;
    }

    #endregion
}