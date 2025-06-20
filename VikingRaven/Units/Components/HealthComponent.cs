using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Factory;
using VikingRaven.Units.Models;

namespace VikingRaven.Units.Components
{
    public class HealthComponent : BaseComponent
    {
        [TitleGroup("Unit Model Integration")]
        [InfoBox("Health and Shield data comes directly from UnitModel - HealthComponent provides enhanced features only", InfoMessageType.Info)]
        [ShowInInspector, ReadOnly]
        private UnitModel _unitModel;
        [TitleGroup("Enhanced Health Features")]
        [InfoBox("Additional health mechanics built on top of UnitModel base data", InfoMessageType.Warning)]
        
        [Tooltip("Current injury severity level")]
        [SerializeField, EnumToggleButtons] private InjuryState _currentInjuryState = InjuryState.Healthy;
        
        [Tooltip("Current recovery phase")]
        [SerializeField, EnumToggleButtons] private RecoveryPhase _currentRecoveryPhase = RecoveryPhase.None;
        
        [Tooltip("Health threshold for light injuries (% of max health)")]
        [SerializeField, Range(60f, 90f)] private float _lightInjuryThreshold = 75f;
        
        [Tooltip("Health threshold for moderate injuries (% of max health)")]
        [SerializeField, Range(30f, 70f)] private float _moderateInjuryThreshold = 50f;
        
        [Tooltip("Health threshold for severe injuries (% of max health)")]
        [SerializeField, Range(10f, 40f)] private float _severeInjuryThreshold = 25f;
        
        [Tooltip("Time before natural regeneration begins (seconds)")]
        [SerializeField, Range(5f, 30f)] private float _regenerationDelay = 10f;
        
        [Tooltip("Time before shield starts regenerating after damage (seconds)")]
        [SerializeField, Range(1f, 10f)] private float _shieldRegenerationDelay = 3f;

        #region Stamina System (Independent Feature)
        
        [TitleGroup("Stamina System")]
        [InfoBox("Stamina system independent of UnitModel - affects combat performance", InfoMessageType.Warning)]
        
        [Tooltip("Maximum stamina points")]
        [SerializeField, ReadOnly] private float _maxStamina = 100f;
        
        [Tooltip("Current stamina level")]
        [SerializeField, ReadOnly] private float _currentStamina = 100f;
        
        [Tooltip("Stamina regeneration per second")]
        [SerializeField, Range(1f, 20f)] private float _staminaRegenerationRate = 10f;
        
        [Tooltip("Current fatigue level (0-100)")]
        [SerializeField, ReadOnly, Range(0f, 100f)] private float _fatigueLevel = 0f;

        #endregion

        #region Live Status Display (Read-Only from UnitModel)
        
        [TitleGroup("Live Status Display")]
        [InfoBox("Real-time data from UnitModel - Read-only display", InfoMessageType.None)]
        
        [HorizontalGroup("Live Status Display/Row1")]
        [BoxGroup("Live Status Display/Row1/Health")]
        [LabelText("Current Health"), ProgressBar(0, "MaxHealthFromModel", ColorGetter = "GetHealthColor")]
        [ShowInInspector, ReadOnly] 
        private float CurrentHealthDisplay => _unitModel?.CurrentHealth ?? 0f;
        
        [BoxGroup("Live Status Display/Row1/Health")]
        [LabelText("Max Health"), ReadOnly]
        [ShowInInspector] 
        private float MaxHealthFromModel => _unitModel?.MaxHealth ?? 0f;
        
        [BoxGroup("Live Status Display/Row1/Health")]
        [LabelText("Health %"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetHealthColor")]
        [ShowInInspector] 
        private float HealthPercentageDisplay => GetHealthPercentage() * 100f;
        
        [HorizontalGroup("Live Status Display/Row1")]
        [BoxGroup("Live Status Display/Row1/Shield")]
        [LabelText("Current Shield"), ProgressBar(0, "MaxShieldFromModel", ColorGetter = "GetShieldColor")]
        [ShowInInspector, ReadOnly] 
        private float CurrentShieldDisplay => _unitModel?.CurrentShield ?? 0f;
        
        [BoxGroup("Live Status Display/Row1/Shield")]
        [LabelText("Max Shield"), ReadOnly]
        [ShowInInspector] 
        private float MaxShieldFromModel => _unitModel?.MaxShield ?? 0f;
        
        [BoxGroup("Live Status Display/Row1/Shield")]
        [LabelText("Shield %"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetShieldColor")]
        [ShowInInspector] 
        private float ShieldPercentageDisplay => GetShieldPercentage() * 100f;
        
        [HorizontalGroup("Live Status Display/Row2")]
        [BoxGroup("Live Status Display/Row2/Stamina")]
        [LabelText("Stamina"), ProgressBar(0, "_maxStamina", ColorGetter = "GetStaminaColor")]
        [ShowInInspector, ReadOnly] 
        private float CurrentStaminaDisplay => _currentStamina;
        
        [BoxGroup("Live Status Display/Row2/Stamina")]
        [LabelText("Fatigue"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetFatigueColor")]
        [ShowInInspector] 
        private float FatigueLevelDisplay => _fatigueLevel;
        
        [HorizontalGroup("Live Status Display/Row2")]
        [BoxGroup("Live Status Display/Row2/Status")]
        [LabelText("Is Alive"), ReadOnly, ToggleLeft]
        [ShowInInspector] 
        private bool IsAliveDisplay => IsAlive;
        
        [BoxGroup("Live Status Display/Row2/Status")]
        [LabelText("Injury State"), ReadOnly, EnumToggleButtons]
        [ShowInInspector] 
        private InjuryState InjuryStateDisplay => _currentInjuryState;

        #endregion

        #region Private Fields
        
        private float _timeSinceLastDamage = 0f;
        private float _timeSinceLastShieldDamage = 0f;
        private List<StatusEffect> _activeStatusEffects = new List<StatusEffect>();
        private List<DamageType> _damageImmunities = new List<DamageType>();
        
        [SerializeField]
        private CombatComponent _combatComponent;
        [SerializeField]
        private StateComponent _stateComponent;
        
        // Regeneration rates (calculated from UnitModel)
        private float _healthRegenerationRate = 0f;
        private float _shieldRegenerationRate = 0f;

        #endregion

        #region Public Properties (Delegate to UnitModel)

        // Health Properties (Delegate to UnitModel)
        public float MaxHealth => _unitModel?.MaxHealth ?? 0f;
        public float CurrentHealth => _unitModel?.CurrentHealth ?? 0f;
        public float HealthPercentage => GetHealthPercentage();
        
        // Shield Properties (Delegate to UnitModel)
        public float MaxShield => _unitModel?.MaxShield ?? 0f;
        public float CurrentShield => _unitModel?.CurrentShield ?? 0f;
        public float ShieldPercentage => GetShieldPercentage();
        public bool HasShield => MaxShield > 0;
        public bool ShieldActive => CurrentShield > 0;
        
        // Enhanced Health Properties (HealthComponent managed)
        public InjuryState CurrentInjuryState => _currentInjuryState;
        public RecoveryPhase CurrentRecoveryPhase => _currentRecoveryPhase;
        public float TimeSinceLastDamage => _timeSinceLastDamage;
        public float TimeSinceLastShieldDamage => _timeSinceLastShieldDamage;
        
        // Stamina Properties (HealthComponent managed)
        public float MaxStamina => _maxStamina;
        public float CurrentStamina => _currentStamina;
        public float StaminaPercentage => _maxStamina > 0 ? _currentStamina / _maxStamina : 0f;
        public float FatigueLevel => _fatigueLevel;
        
        // Calculated Properties
        public float CombatReadiness => CalculateCombatReadiness();
        public float PerformanceModifier => CalculatePerformanceModifier();
        public bool IsExhausted => _currentStamina < (_maxStamina * 0.1f);
        public bool IsFatigued => _fatigueLevel > 50f;
        public bool IsAlive => _unitModel != null && _unitModel.CurrentHealth > 0f;
        
        // Status Effects (HealthComponent managed)
        public IReadOnlyList<StatusEffect> ActiveStatusEffects => _activeStatusEffects;
        public IReadOnlyList<DamageType> DamageImmunities => _damageImmunities;

        #endregion

        #region Events

        // Health Events (forwarded from UnitModel)
        public event Action<float, IEntity> OnDamageTaken;
        public event Action<float> OnHealthRegenerated;
        public event Action<float> OnHealthChanged;
        public event Action OnDeath;
        public event Action OnRevive;
        
        // Shield Events (managed by HealthComponent)
        public event Action<float, IEntity> OnShieldDamage;
        public event Action<float> OnShieldRegenerated;
        public event Action<float> OnShieldChanged;
        public event Action OnShieldBroken;
        public event Action OnShieldRestored;
        
        // Enhanced System Events (HealthComponent managed)
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
            LoadUnitModelReference();
            InitializeEnhancedFeatures();
            SubscribeToUnitModelEvents();
        }

        private void Update()
        {
            if (!IsActive || !IsAlive) return;

            UpdateRegenerationSystems();
            UpdateFeatures();
            UpdateTimers();
        }

        #endregion

        #region Initialization
        private void LoadUnitModelReference()
        {
            if (Entity == null) return;
            var unitFactory = FindObjectOfType<UnitFactory>();
            if (unitFactory != null)
            {
                _unitModel = unitFactory.GetUnitModel(Entity);
                if (_unitModel != null)
                {
                    Debug.Log($"HealthComponent: Connected to UnitModel for entity {Entity.Id} " +
                             $"(Health: {_unitModel.MaxHealth}, Shield: {_unitModel.MaxShield})");
                }
                else
                {
                    Debug.LogWarning($"HealthComponent: UnitModel not found for entity {Entity.Id}");
                }
            }
            else
            {
                Debug.LogError("HealthComponent: UnitFactory not found in scene!");
            }
        }

        /// <summary>
        /// Initialize enhanced features that are NOT in UnitModel
        /// </summary>
        private void InitializeEnhancedFeatures()
        {
            if (_unitModel == null) return;
            
            // Initialize injury state based on current health
            UpdateInjuryStateFromHealth();
            
            // Initialize stamina based on unit characteristics
            _maxStamina = CalculateMaxStaminaFromUnitModel();
            _currentStamina = _maxStamina;
            
            // Calculate regeneration rates based on UnitModel
            _healthRegenerationRate = CalculateHealthRegenRate();
            _shieldRegenerationRate = CalculateShieldRegenRate();
            
            // Initialize other features
            _currentRecoveryPhase = RecoveryPhase.None;
            _fatigueLevel = 0f;
            _timeSinceLastDamage = 0f;
            _timeSinceLastShieldDamage = 0f;
            _activeStatusEffects.Clear();
            
            Debug.Log($"HealthComponent: Enhanced features initialized - Stamina: {_maxStamina}");
        }

        /// <summary>
        /// Subscribe to UnitModel events to sync with authoritative data
        /// </summary>
        private void SubscribeToUnitModelEvents()
        {
            if (_unitModel == null) return;
            
            _unitModel.OnHealthChanged += OnUnitModelHealthChanged;
            _unitModel.OnShieldChanged += OnUnitModelShieldChanged;
            _unitModel.OnDamageTaken += OnUnitModelDamageTaken;
            _unitModel.OnDeath += OnUnitModelDeath;
        }

        #endregion

        #region UnitModel Event Handlers

        private void OnUnitModelHealthChanged(float newHealth)
        {
            UpdateInjuryStateFromHealth();
            OnHealthChanged?.Invoke(newHealth);
        }

        private void OnUnitModelShieldChanged(float newShield)
        {
            if (newShield <= 0 && _timeSinceLastShieldDamage < 1f)
            {
                OnShieldBroken?.Invoke();
            }
            OnShieldChanged?.Invoke(newShield);
        }

        private void OnUnitModelDamageTaken(float damage, IEntity source)
        {
            _timeSinceLastDamage = 0f;
            if (HasShield && CurrentShield < MaxShield)
            {
                _timeSinceLastShieldDamage = 0f;
            }
            
            // Add fatigue from taking damage
            AddFatigue(damage * 0.1f);
            
            OnDamageTaken?.Invoke(damage, source);
        }

        private void OnUnitModelDeath()
        {
            OnDeath?.Invoke();
        }

        #endregion

        #region Public API (Delegate to UnitModel)
        public void TakeDamage(float amount, IEntity source = null, bool ignoreArmor = false)
        {
            if (_unitModel == null || !IsAlive || amount <= 0) return;
            
            if (IsImmuneToDamage(source)) return;
            _unitModel.TakeDamage(amount, source);
        }

        /// <summary>
        /// Heal unit - delegates to UnitModel
        /// </summary>
        public void Heal(float amount)
        {
            if (_unitModel == null || !IsAlive || amount <= 0) return;
            
            _unitModel.Heal(amount);
            OnHealthRegenerated?.Invoke(amount);
        }
        public void RestoreShield(float amount)
        {
            if (_unitModel == null || amount <= 0) return;
            
            float oldShield = CurrentShield;
            _unitModel.CurrentShield = Mathf.Min(MaxShield, CurrentShield + amount);
            float actualRestore = CurrentShield - oldShield;
            
            if (actualRestore > 0)
            {
                OnShieldRegenerated?.Invoke(actualRestore);
                
                if (oldShield <= 0 && CurrentShield > 0)
                {
                    OnShieldRestored?.Invoke();
                }
            }
        }

        public void Revive(float healthPercent = 1.0f)
        {
            if (_unitModel == null) return;
            
            _unitModel.Revive(healthPercent);
            OnRevive?.Invoke();
        }

        #endregion

        #region Update Systems

        private void UpdateRegenerationSystems()
        {
            UpdateHealthRegeneration();
            UpdateShieldRegeneration();
            UpdateStaminaRegeneration();
        }

        private void UpdateFeatures()
        {
            UpdateInjuryState();
            UpdateRecoveryPhase();
            UpdateStatusEffects();
            UpdateFatigueLevel();
        }

        private void UpdateTimers()
        {
            _timeSinceLastDamage += Time.deltaTime;
            _timeSinceLastShieldDamage += Time.deltaTime;
        }

        private void UpdateHealthRegeneration()
        {
            if (!IsAlive || CurrentHealth >= MaxHealth || _timeSinceLastDamage < _regenerationDelay) return;
            
            float regenAmount = _healthRegenerationRate * Time.deltaTime;
            Heal(regenAmount);
        }

        private void UpdateShieldRegeneration()
        {
            if (!IsAlive || CurrentShield >= MaxShield || _timeSinceLastShieldDamage < _shieldRegenerationDelay) return;
            
            float regenMultiplier = IsInCombat() ? 1f : 2f; // Faster regen out of combat
            float regenAmount = _shieldRegenerationRate * regenMultiplier * Time.deltaTime;
            
            RestoreShield(regenAmount);
        }

        private void UpdateStaminaRegeneration()
        {
            if (_currentStamina >= _maxStamina) return;
            
            float regenAmount = _staminaRegenerationRate * Time.deltaTime;
            _currentStamina = Mathf.Min(_maxStamina, _currentStamina + regenAmount);
            OnStaminaChanged?.Invoke(_currentStamina);
        }

        #endregion

        #region Calculation Methods

        private float GetHealthPercentage()
        {
            return MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;
        }

        private float GetShieldPercentage()
        {
            return MaxShield > 0 ? CurrentShield / MaxShield : 0f;
        }

        private float CalculateCombatReadiness()
        {
            if (!IsAlive) return 0f;
            
            float healthFactor = HealthPercentage * 70f;
            float staminaFactor = StaminaPercentage * 20f;
            float shieldFactor = HasShield ? ShieldPercentage * 10f : 0f;
            
            return healthFactor + staminaFactor + shieldFactor;
        }

        private float CalculatePerformanceModifier()
        {
            if (!IsAlive) return 0f;
            
            float healthModifier = Mathf.Lerp(0.5f, 1f, HealthPercentage);
            float staminaModifier = Mathf.Lerp(0.7f, 1f, StaminaPercentage);
            float injuryModifier = GetInjuryModifier();
            
            return healthModifier * staminaModifier * injuryModifier;
        }

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

        private float CalculateMaxStaminaFromUnitModel()
        {
            if (_unitModel == null) return 100f;
            
            float baseStamina = _unitModel.UnitType switch
            {
                UnitType.Infantry => 120f,
                UnitType.Archer => 100f,
                UnitType.Pike => 110f,
                _ => 100f
            };
            
            // Scale with health
            float healthScale = _unitModel.MaxHealth / 100f;
            return baseStamina * healthScale;
        }

        private float CalculateHealthRegenRate()
        {
            if (_unitModel == null) return 0.5f;
            
            float baseRegen = 0.5f;
            float healthScale = _unitModel.MaxHealth / 100f;
            return baseRegen * healthScale;
        }

        private float CalculateShieldRegenRate()
        {
            if (_unitModel == null || MaxShield <= 0) return 0f;
            
            float baseShieldRegen = 2f;
            float shieldScale = MaxShield / 50f;
            
            float typeMultiplier = _unitModel.UnitType switch
            {
                UnitType.Infantry => 0.8f,
                UnitType.Archer => 1.2f,
                UnitType.Pike => 1.0f,
                _ => 1.0f
            };
            
            return baseShieldRegen * shieldScale * typeMultiplier;
        }

        private bool IsInCombat()
        {
            return _stateComponent?.IsInCombat == true || _timeSinceLastDamage < 5f;
        }

        private bool IsImmuneToDamage(IEntity source)
        {
            if (source == null) return false;
            
            var combatComponent = source.GetComponent<CombatComponent>();
            if (combatComponent == null) return false;
            
            // Check if immune to this damage type
            return _damageImmunities.Contains(combatComponent.PrimaryDamageType);
        }

        #endregion

        #region Odin Inspector Color Methods

        private Color GetHealthColor()
        {
            float healthPercent = HealthPercentage * 100f;
            if (healthPercent > 75f) return Color.green;
            if (healthPercent > 50f) return Color.yellow;
            if (healthPercent > 25f) return new Color(1f, 0.5f, 0f);
            return Color.red;
        }

        private Color GetShieldColor()
        {
            float shieldPercent = ShieldPercentage * 100f;
            if (shieldPercent > 75f) return Color.cyan;
            if (shieldPercent > 50f) return Color.blue;
            if (shieldPercent > 25f) return new Color(0.5f, 0f, 1f);
            return Color.gray;
        }

        private Color GetStaminaColor()
        {
            float staminaPercent = StaminaPercentage * 100f;
            if (staminaPercent > 75f) return Color.green;
            if (staminaPercent > 50f) return Color.yellow;
            if (staminaPercent > 25f) return new Color(1f, 0.5f, 0f);
            return Color.red;
        }

        private Color GetFatigueColor()
        {
            if (_fatigueLevel < 25f) return Color.green;
            if (_fatigueLevel < 50f) return Color.yellow;
            if (_fatigueLevel < 75f) return new Color(1f, 0.5f, 0f);
            return Color.red;
        }

        #endregion

        #region Enhanced Feature Stubs (Implementation Needed)
        private void UpdateInjuryState() { }
        private void UpdateInjuryStateFromHealth() { }
        private void UpdateRecoveryPhase() { }
        private void UpdateStatusEffects() { }
        private void UpdateFatigueLevel() { }
        private void AddFatigue(float amount) 
        {
            _fatigueLevel = Mathf.Clamp01(_fatigueLevel + amount);
        }
        #endregion
    }
}