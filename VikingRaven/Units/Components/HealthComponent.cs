using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    /// <summary>
    /// Enhanced Health Component with stamina system, injury states, and detailed health management
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
        [InfoBox("Stamina affects combat performance, movement speed, and ability usage.", InfoMessageType.Info)]
        
        [Tooltip("Maximum stamina points")]
        [SerializeField, Range(50f, 200f), ProgressBar(50, 200, ColorGetter = "GetStaminaMaxColor")]
        private float _maxStamina = 100f;
        
        [Tooltip("Current stamina level")]
        [SerializeField, ReadOnly, ProgressBar(0, 100, ColorGetter = "GetStaminaColor")]
        private float _currentStamina;
        
        [Tooltip("Stamina regeneration rate per second")]
        [SerializeField, Range(1f, 20f)] private float _staminaRegenRate = 5f;
        
        [Tooltip("Stamina depletion from combat actions")]
        [SerializeField, Range(5f, 25f)] private float _attackStaminaCost = 10f;
        
        [Tooltip("Stamina depletion from movement")]
        [SerializeField, Range(1f, 10f)] private float _movementStaminaCost = 2f;
        
        [Tooltip("Fatigue level affects performance (0-100%)")]
        [SerializeField, ReadOnly, Range(0f, 100f), ProgressBar(0, 100, ColorGetter = "GetFatigueColor")]
        private float _fatigueLevel = 0f;

        #endregion

        #region Recovery System
        
        [TitleGroup("Recovery System")]
        [InfoBox("Advanced recovery mechanics for realistic healing and rehabilitation.", InfoMessageType.Info)]
        
        [Tooltip("Base recovery rate multiplier")]
        [SerializeField, Range(0.1f, 5f)] private float _recoveryRateMultiplier = 1f;
        
        [Tooltip("Time since last damage taken")]
        [SerializeField, ReadOnly] private float _timeSinceLastDamage = 0f;
        
        [Tooltip("Recovery phase affects regeneration speed")]
        [SerializeField, ReadOnly, EnumToggleButtons] private RecoveryPhase _currentRecoveryPhase = RecoveryPhase.None;
        
        [Tooltip("Enable natural regeneration out of combat")]
        [SerializeField, ToggleLeft] private bool _enableNaturalRegeneration = true;

        #endregion

        #region Status Effects System
        
        [TitleGroup("Status Effects")]
        [InfoBox("Track various status effects that influence health and combat performance.", InfoMessageType.None)]
        
        [Tooltip("Active status effects")]
        [SerializeField, ReadOnly] private List<StatusEffect> _activeStatusEffects = new List<StatusEffect>();
        
        [Tooltip("Immunity to certain damage types")]
        [SerializeField] private List<DamageType> _damageImmunities = new List<DamageType>();
        
        [Tooltip("Resistance to status effects")]
        [SerializeField, Range(0f, 100f)] private float _statusEffectResistance = 0f;

        #endregion

        #region Calculated Properties
        
        [TitleGroup("Health Statistics")]
        [ShowInInspector, ReadOnly, ProgressBar(0, 100)]
        private float HealthPercentageDisplay => _maxHealth > 0 ? (_currentHealth / _maxHealth) * 100f : 0f;
        
        [ShowInInspector, ReadOnly, ProgressBar(0, 100)]
        private float StaminaPercentageDisplay => _maxStamina > 0 ? (_currentStamina / _maxStamina) * 100f : 0f;
        
        [ShowInInspector, ReadOnly]
        private float OverallCondition => CalculateOverallCondition();

        #endregion

        #region Public Properties

        // Basic Health Properties
        public float MaxHealth => _maxHealth;
        public float CurrentHealth => _currentHealth;
        public bool IsDead => _isDead;
        public float HealthPercentage => _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;
        public float RegenerationRate => _regenerationRate;
        
        // Enhanced Health Properties
        public InjuryState CurrentInjuryState => _currentInjuryState;
        public RecoveryPhase CurrentRecoveryPhase => _currentRecoveryPhase;
        public float TimeSinceLastDamage => _timeSinceLastDamage;
        
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
        
        // Status Effects
        public IReadOnlyList<StatusEffect> ActiveStatusEffects => _activeStatusEffects;
        public IReadOnlyList<DamageType> DamageImmunities => _damageImmunities;

        #endregion

        #region Events

        public event Action<float, IEntity> OnDamageTaken;
        public event Action<float> OnHealthRegenerated;
        public event Action OnDeath;
        public event Action OnRevive;
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
            UpdateStaminaRegeneration();
            UpdateInjuryState();
            UpdateRecoveryPhase();
            UpdateStatusEffects();
            UpdateFatigueLevel();
            
            _timeSinceLastDamage += Time.deltaTime;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Load health stats from UnitDataSO via UnitModel
        /// </summary>
        private void LoadStatsFromUnitData()
        {
            if (Entity == null) return;
            
            // Get UnitModel from UnitFactory to access UnitDataSO
            var unitFactory = FindObjectOfType<VikingRaven.Core.Factory.UnitFactory>();
            if (unitFactory != null)
            {
                var unitModel = unitFactory.GetUnitModel(Entity);
                if (unitModel != null)
                {
                    _maxHealth = unitModel.MaxHealth;
                    _currentHealth = _maxHealth;
                    
                    // Calculate stamina based on unit type and stats
                    _maxStamina = CalculateStaminaFromUnitData(unitModel);
                    _currentStamina = _maxStamina;
                    
                    // Set regeneration based on unit characteristics
                    _regenerationRate = CalculateRegenerationFromUnitData(unitModel);
                    
                    Debug.Log($"HealthComponent: Loaded stats from UnitModel for {unitModel.DisplayName}");
                }
            }
        }

        /// <summary>
        /// Calculate stamina based on unit data
        /// </summary>
        private float CalculateStaminaFromUnitData(VikingRaven.Units.Models.UnitModel unitModel)
        {
            float baseStamina = 100f;
            
            // Modify based on unit type
            switch (unitModel.UnitType)
            {
                case VikingRaven.Units.Components.UnitType.Infantry:
                    baseStamina = 120f; // Higher stamina for melee fighters
                    break;
                case VikingRaven.Units.Components.UnitType.Archer:
                    baseStamina = 100f; // Standard stamina
                    break;
                case VikingRaven.Units.Components.UnitType.Pike:
                    baseStamina = 110f; // Moderate stamina for heavy units
                    break;
            }
            
            // Scale with unit stats
            float healthScale = unitModel.MaxHealth / 100f;
            return baseStamina * healthScale;
        }

        /// <summary>
        /// Calculate regeneration rate from unit data
        /// </summary>
        private float CalculateRegenerationFromUnitData(VikingRaven.Units.Models.UnitModel unitModel)
        {
            float baseRegen = 0.5f; // Base regeneration per second
            
            // Scale with unit health
            float healthScale = unitModel.MaxHealth / 100f;
            return baseRegen * healthScale;
        }
        
        /// <summary>
        /// Calculate stamina regen rate from unit data
        /// </summary>
        private float CalculateStaminaRegenFromUnitData()
        {
            float baseStaminaRegen = 5f; // Base stamina regen per second
            
            // Get current unit model to scale
            var unitFactory = FindObjectOfType<VikingRaven.Core.Factory.UnitFactory>();
            if (unitFactory != null)
            {
                var unitModel = unitFactory.GetUnitModel(Entity);
                if (unitModel != null)
                {
                    // Scale based on unit type
                    switch (unitModel.UnitType)
                    {
                        case VikingRaven.Units.Components.UnitType.Infantry:
                            baseStaminaRegen *= 1.1f; // 10% bonus for infantry
                            break;
                        case VikingRaven.Units.Components.UnitType.Archer:
                            baseStaminaRegen *= 0.9f; // 10% penalty for archer
                            break;
                        case VikingRaven.Units.Components.UnitType.Pike:
                            baseStaminaRegen *= 1.0f; // Standard for pike
                            break;
                    }
                }
            }
            
            return baseStaminaRegen;
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
            _activeStatusEffects.Clear();
            
            Debug.Log($"HealthComponent: Enhanced health system initialized for {Entity.Id}");
        }

        #endregion

        #region Enhanced Damage System

        /// <summary>
        /// Take enhanced damage with injury state calculation
        /// </summary>
        public void TakeDamage(float amount, IEntity source, bool ignoreArmor = false)
        {
            if (!IsActive || _isDead) return;
            
            // Check damage immunities
            var combatComponent = source?.GetComponent<CombatComponent>();
            if (combatComponent != null && _damageImmunities.Contains(combatComponent.PrimaryDamageType))
            {
                Debug.Log($"HealthComponent: Damage immune to {combatComponent.PrimaryDamageType}");
                return;
            }
            
            // Apply damage
            float actualDamage = Mathf.Max(1f, amount);
            _currentHealth = Mathf.Max(0, _currentHealth - actualDamage);
            _timeSinceLastDamage = 0f;
            
            // Update injury state
            UpdateInjuryStateFromDamage();
            
            // Add fatigue from taking damage
            AddFatigue(actualDamage * 0.1f);
            
            // Check for status effects from damage
            CheckForDamageStatusEffects(actualDamage, source);
            
            // Trigger events
            OnDamageTaken?.Invoke(actualDamage, source);
            
            // Check for death
            if (_currentHealth <= 0 && !_isDead)
            {
                Die();
            }
            
            Debug.Log($"HealthComponent: Took {actualDamage:F1} damage. Health: {_currentHealth:F1}/{_maxHealth:F1}");
        }

        /// <summary>
        /// Take true damage that ignores armor and immunities
        /// </summary>
        public void TakeTrueDamage(float amount, IEntity source)
        {
            if (!IsActive || _isDead) return;
            
            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            _timeSinceLastDamage = 0f;
            
            UpdateInjuryStateFromDamage();
            OnDamageTaken?.Invoke(amount, source);
            
            if (_currentHealth <= 0 && !_isDead)
            {
                Die();
            }
        }

        /// <summary>
        /// Enhanced heal method with recovery phases
        /// </summary>
        public void Heal(float amount)
        {
            if (!IsActive || _isDead) return;

            float previousHealth = _currentHealth;
            float healAmount = amount * GetHealingModifier();
            
            _currentHealth = Mathf.Min(_currentHealth + healAmount, _maxHealth);
            
            float actualHealed = _currentHealth - previousHealth;
            if (actualHealed > 0)
            {
                OnHealthRegenerated?.Invoke(actualHealed);
                UpdateInjuryStateFromHealing();
                
                // Reduce fatigue slightly when healing
                ReduceFatigue(actualHealed * 0.05f);
            }
        }

        #endregion

        #region Stamina System

        /// <summary>
        /// Consume stamina for actions
        /// </summary>
        public bool ConsumeStamina(float amount)
        {
            if (_currentStamina < amount)
            {
                return false; // Not enough stamina
            }
            
            _currentStamina = Mathf.Max(0f, _currentStamina - amount);
            UpdateFatigueFromStamina();
            OnStaminaChanged?.Invoke(_currentStamina);
            
            return true;
        }

        /// <summary>
        /// Consume stamina for attack actions
        /// </summary>
        public bool ConsumeAttackStamina()
        {
            return ConsumeStamina(_attackStaminaCost);
        }

        /// <summary>
        /// Consume stamina for movement
        /// </summary>
        public void ConsumeMovementStamina(float distance)
        {
            float staminaCost = _movementStaminaCost * distance * Time.deltaTime;
            ConsumeStamina(staminaCost);
        }

        /// <summary>
        /// Restore stamina
        /// </summary>
        public void RestoreStamina(float amount)
        {
            _currentStamina = Mathf.Min(_maxStamina, _currentStamina + amount);
            UpdateFatigueFromStamina();
            OnStaminaChanged?.Invoke(_currentStamina);
        }

        /// <summary>
        /// Update stamina regeneration
        /// </summary>
        private void UpdateStaminaRegeneration()
        {
            if (_currentStamina < _maxStamina)
            {
                float regenAmount = _staminaRegenRate * Time.deltaTime;
                
                // Reduce regen rate based on injury state
                regenAmount *= GetStaminaRegenModifier();
                
                RestoreStamina(regenAmount);
            }
        }

        #endregion

        #region Injury and Recovery System

        /// <summary>
        /// Update injury state based on current health
        /// </summary>
        private void UpdateInjuryState()
        {
            InjuryState newState = CalculateInjuryState();
            
            if (newState != _currentInjuryState)
            {
                _currentInjuryState = newState;
                OnInjuryStateChanged?.Invoke(_currentInjuryState);
                
                Debug.Log($"HealthComponent: Injury state changed to {_currentInjuryState}");
            }
        }

        /// <summary>
        /// Calculate injury state based on health percentage
        /// </summary>
        private InjuryState CalculateInjuryState()
        {
            float healthPercent = HealthPercentage;
            
            if (healthPercent >= _lightInjuryThreshold)
                return InjuryState.Healthy;
            else if (healthPercent >= _moderateInjuryThreshold)
                return InjuryState.LightInjury;
            else if (healthPercent >= _severeInjuryThreshold)
                return InjuryState.ModerateInjury;
            else
                return InjuryState.SevereInjury;
        }

        /// <summary>
        /// Update injury state when taking damage
        /// </summary>
        private void UpdateInjuryStateFromDamage()
        {
            UpdateInjuryState();
            
            // Reset recovery phase when taking new damage
            if (_currentRecoveryPhase != RecoveryPhase.None)
            {
                SetRecoveryPhase(RecoveryPhase.None);
            }
        }

        /// <summary>
        /// Update injury state when healing
        /// </summary>
        private void UpdateInjuryStateFromHealing()
        {
            UpdateInjuryState();
        }

        /// <summary>
        /// Update recovery phase based on time since last damage
        /// </summary>
        private void UpdateRecoveryPhase()
        {
            if (_isDead || _currentInjuryState == InjuryState.Healthy) return;
            
            RecoveryPhase newPhase = CalculateRecoveryPhase();
            
            if (newPhase != _currentRecoveryPhase)
            {
                SetRecoveryPhase(newPhase);
            }
        }

        /// <summary>
        /// Calculate recovery phase based on time since damage
        /// </summary>
        private RecoveryPhase CalculateRecoveryPhase()
        {
            if (_timeSinceLastDamage < _regenerationDelay)
                return RecoveryPhase.None;
            else if (_timeSinceLastDamage < _regenerationDelay * 2f)
                return RecoveryPhase.Initial;
            else if (_timeSinceLastDamage < _regenerationDelay * 4f)
                return RecoveryPhase.Active;
            else
                return RecoveryPhase.Advanced;
        }

        /// <summary>
        /// Set recovery phase and trigger events
        /// </summary>
        private void SetRecoveryPhase(RecoveryPhase phase)
        {
            _currentRecoveryPhase = phase;
            OnRecoveryPhaseChanged?.Invoke(_currentRecoveryPhase);
        }

        #endregion

        #region Health Regeneration

        /// <summary>
        /// Update health regeneration based on recovery phase
        /// </summary>
        private void UpdateHealthRegeneration()
        {
            if (!_enableNaturalRegeneration || _isDead || _currentRecoveryPhase == RecoveryPhase.None) return;
            
            float regenAmount = _regenerationRate * Time.deltaTime * GetRegenerationModifier();
            
            if (regenAmount > 0)
            {
                Heal(regenAmount);
            }
        }

        /// <summary>
        /// Get regeneration modifier based on current state
        /// </summary>
        private float GetRegenerationModifier()
        {
            float modifier = _recoveryRateMultiplier;
            
            // Recovery phase modifier
            modifier *= _currentRecoveryPhase switch
            {
                RecoveryPhase.None => 0f,
                RecoveryPhase.Initial => 0.5f,
                RecoveryPhase.Active => 1f,
                RecoveryPhase.Advanced => 1.5f,
                _ => 1f
            };
            
            // Injury state modifier
            modifier *= _currentInjuryState switch
            {
                InjuryState.Healthy => 1f,
                InjuryState.LightInjury => 0.8f,
                InjuryState.ModerateInjury => 0.6f,
                InjuryState.SevereInjury => 0.4f,
                _ => 1f
            };
            
            // Fatigue modifier
            modifier *= Mathf.Lerp(1f, 0.5f, _fatigueLevel / 100f);
            
            return modifier;
        }

        /// <summary>
        /// Get healing modifier for external healing
        /// </summary>
        private float GetHealingModifier()
        {
            float modifier = 1f;
            
            // Injury state affects healing efficiency
            modifier *= _currentInjuryState switch
            {
                InjuryState.Healthy => 1f,
                InjuryState.LightInjury => 0.9f,
                InjuryState.ModerateInjury => 0.7f,
                InjuryState.SevereInjury => 0.5f,
                _ => 1f
            };
            
            return modifier;
        }

        #endregion

        #region Fatigue System

        /// <summary>
        /// Update fatigue level based on various factors
        /// </summary>
        private void UpdateFatigueLevel()
        {
            // Natural fatigue recovery over time
            if (_fatigueLevel > 0f)
            {
                float fatigueRecovery = 5f * Time.deltaTime; // 5% per second base recovery
                
                // Modify recovery based on injury state
                fatigueRecovery *= _currentInjuryState switch
                {
                    InjuryState.Healthy => 1f,
                    InjuryState.LightInjury => 0.8f,
                    InjuryState.ModerateInjury => 0.6f,
                    InjuryState.SevereInjury => 0.4f,
                    _ => 1f
                };
                
                ReduceFatigue(fatigueRecovery);
            }
        }

        /// <summary>
        /// Update fatigue based on stamina levels
        /// </summary>
        private void UpdateFatigueFromStamina()
        {
            float staminaPercent = StaminaPercentage;
            
            if (staminaPercent < 20f) // Very low stamina increases fatigue
            {
                AddFatigue(2f * Time.deltaTime);
            }
            else if (staminaPercent < 50f) // Low stamina increases fatigue slowly
            {
                AddFatigue(0.5f * Time.deltaTime);
            }
        }

        /// <summary>
        /// Add fatigue
        /// </summary>
        private void AddFatigue(float amount)
        {
            _fatigueLevel = Mathf.Min(100f, _fatigueLevel + amount);
        }

        /// <summary>
        /// Reduce fatigue
        /// </summary>
        private void ReduceFatigue(float amount)
        {
            _fatigueLevel = Mathf.Max(0f, _fatigueLevel - amount);
        }

        /// <summary>
        /// Get stamina regeneration modifier based on injury state
        /// </summary>
        private float GetStaminaRegenModifier()
        {
            return _currentInjuryState switch
            {
                InjuryState.Healthy => 1f,
                InjuryState.LightInjury => 0.9f,
                InjuryState.ModerateInjury => 0.7f,
                InjuryState.SevereInjury => 0.5f,
                _ => 1f
            };
        }

        #endregion

        #region Status Effects System

        /// <summary>
        /// Add status effect
        /// </summary>
        public bool AddStatusEffect(StatusEffect statusEffect)
        {
            if (statusEffect == null) return false;
            
            // Check resistance
            if (UnityEngine.Random.Range(0f, 100f) < _statusEffectResistance)
            {
                Debug.Log($"HealthComponent: Resisted status effect {statusEffect.Type}");
                return false;
            }
            
            // Check if effect already exists
            var existingEffect = _activeStatusEffects.Find(e => e.Type == statusEffect.Type);
            if (existingEffect != null)
            {
                // Refresh duration or stack effect
                existingEffect.RefreshDuration(statusEffect.Duration);
                return true;
            }
            
            _activeStatusEffects.Add(statusEffect);
            statusEffect.ApplyEffect(this);
            OnStatusEffectAdded?.Invoke(statusEffect);
            
            Debug.Log($"HealthComponent: Added status effect {statusEffect.Type}");
            return true;
        }

        /// <summary>
        /// Remove status effect
        /// </summary>
        public bool RemoveStatusEffect(StatusEffectType type)
        {
            var effect = _activeStatusEffects.Find(e => e.Type == type);
            if (effect != null)
            {
                _activeStatusEffects.Remove(effect);
                effect.RemoveEffect(this);
                OnStatusEffectRemoved?.Invoke(effect);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Update all active status effects
        /// </summary>
        private void UpdateStatusEffects()
        {
            for (int i = _activeStatusEffects.Count - 1; i >= 0; i--)
            {
                var effect = _activeStatusEffects[i];
                effect.Update(Time.deltaTime);
                
                if (effect.IsExpired)
                {
                    _activeStatusEffects.RemoveAt(i);
                    effect.RemoveEffect(this);
                    OnStatusEffectRemoved?.Invoke(effect);
                }
            }
        }

        /// <summary>
        /// Check for status effects from damage
        /// </summary>
        private void CheckForDamageStatusEffects(float damage, IEntity source)
        {
            // Heavy damage can cause bleeding
            if (damage > _maxHealth * 0.2f)
            {
                var bleedingEffect = new StatusEffect(StatusEffectType.Bleeding, 10f, damage * 0.1f);
                AddStatusEffect(bleedingEffect);
            }
            
            // Check for other damage-based status effects based on source
            var sourceCombat = source?.GetComponent<CombatComponent>();
            if (sourceCombat != null)
            {
                // Fire damage can cause burning
                if (sourceCombat.PrimaryDamageType == DamageType.Fire)
                {
                    var burnEffect = new StatusEffect(StatusEffectType.Burning, 8f, damage * 0.15f);
                    AddStatusEffect(burnEffect);
                }
                
                // Blunt weapons can cause stun
                if (sourceCombat.EquippedWeaponType == WeaponType.Mace && damage > _maxHealth * 0.15f)
                {
                    var stunEffect = new StatusEffect(StatusEffectType.Stunned, 2f, 0f);
                    AddStatusEffect(stunEffect);
                }
            }
        }

        #endregion

        #region Calculation Methods

        /// <summary>
        /// Calculate overall condition (0-100)
        /// </summary>
        private float CalculateOverallCondition()
        {
            float healthScore = HealthPercentage;
            float staminaScore = StaminaPercentage;
            float fatigueScore = 100f - _fatigueLevel;
            
            return (healthScore + staminaScore + fatigueScore) / 3f;
        }

        /// <summary>
        /// Calculate combat readiness (0-100)
        /// </summary>
        private float CalculateCombatReadiness()
        {
            float healthFactor = HealthPercentage / 100f;
            float staminaFactor = StaminaPercentage / 100f;
            float fatigueFactor = (100f - _fatigueLevel) / 100f;
            
            // Injury state penalty
            float injuryPenalty = _currentInjuryState switch
            {
                InjuryState.Healthy => 1f,
                InjuryState.LightInjury => 0.9f,
                InjuryState.ModerateInjury => 0.7f,
                InjuryState.SevereInjury => 0.5f,
                _ => 1f
            };
            
            return (healthFactor * 0.4f + staminaFactor * 0.4f + fatigueFactor * 0.2f) * injuryPenalty * 100f;
        }

        /// <summary>
        /// Calculate performance modifier for other systems
        /// </summary>
        private float CalculatePerformanceModifier()
        {
            float combatReadiness = CalculateCombatReadiness() / 100f;
            
            // Apply status effect modifiers
            foreach (var effect in _activeStatusEffects)
            {
                combatReadiness *= effect.GetPerformanceModifier();
            }
            
            return Mathf.Clamp(combatReadiness, 0.1f, 1.5f);
        }

        #endregion

        #region Death and Revival

        /// <summary>
        /// Handle death
        /// </summary>
        private void Die()
        {
            _isDead = true;
            _currentHealth = 0f;
            _currentStamina = 0f;
            _currentInjuryState = InjuryState.SevereInjury;
            _currentRecoveryPhase = RecoveryPhase.None;
            
            // Clear status effects
            _activeStatusEffects.Clear();
            
            OnDeath?.Invoke();
            
            Debug.Log($"HealthComponent: Entity {Entity.Id} has died");
        }

        /// <summary>
        /// Revive the unit with enhanced parameters
        /// </summary>
        public void Revive(float healthPercentage = 1.0f, float staminaPercentage = 1.0f)
        {
            if (!_isDead) return;

            _isDead = false;
            _currentHealth = _maxHealth * Mathf.Clamp01(healthPercentage);
            _currentStamina = _maxStamina * Mathf.Clamp01(staminaPercentage);
            
            // Reset states
            UpdateInjuryState();
            _currentRecoveryPhase = RecoveryPhase.None;
            _fatigueLevel = 50f; // Start with some fatigue after revival
            _timeSinceLastDamage = 0f;
            
            OnRevive?.Invoke();
            
            Debug.Log($"HealthComponent: Entity {Entity.Id} has been revived");
        }

        #endregion

        #region Helper Methods for Odin Inspector

        private Color GetStaminaMaxColor => Color.Lerp(Color.yellow, Color.green, _maxStamina / 200f);
        private Color GetStaminaColor => Color.Lerp(Color.red, Color.green, _currentStamina / _maxStamina);
        private Color GetFatigueColor => Color.Lerp(Color.green, Color.red, _fatigueLevel / 100f);

        #endregion

        #region Debug Methods

        [Button("Test Injury State"), FoldoutGroup("Debug Tools")]
        private void TestInjuryState()
        {
            Debug.Log($"Current Injury State: {_currentInjuryState}");
            Debug.Log($"Health: {_currentHealth:F1}/{_maxHealth:F1} ({HealthPercentage:F1}%)");
            Debug.Log($"Combat Readiness: {CalculateCombatReadiness():F1}%");
        }

        [Button("Simulate Damage"), FoldoutGroup("Debug Tools")]
        private void SimulateDamage()
        {
            TakeDamage(_maxHealth * 0.2f, null);
            Debug.Log($"Simulated damage. Health: {_currentHealth:F1}/{_maxHealth:F1}");
        }

        [Button("Add Test Status Effect"), FoldoutGroup("Debug Tools")]
        private void AddTestStatusEffect()
        {
            var testEffect = new StatusEffect(StatusEffectType.Bleeding, 10f, 2f);
            AddStatusEffect(testEffect);
        }

        [Button("Exhaust Stamina"), FoldoutGroup("Debug Tools")]
        private void ExhaustStamina()
        {
            _currentStamina = 0f;
            _fatigueLevel = 80f;
            OnStaminaChanged?.Invoke(_currentStamina);
        }

        #endregion
    }

    #region Supporting Enums and Classes

    /// <summary>
    /// Injury states that affect unit performance
    /// </summary>
    public enum InjuryState
    {
        Healthy,        // 75-100% health
        LightInjury,    // 50-75% health
        ModerateInjury, // 25-50% health
        SevereInjury    // 0-25% health
    }

    /// <summary>
    /// Recovery phases for health regeneration
    /// </summary>
    public enum RecoveryPhase
    {
        None,      // No recovery (recently damaged)
        Initial,   // Early recovery phase
        Active,    // Active recovery phase
        Advanced   // Advanced recovery phase
    }

    /// <summary>
    /// Status effect types
    /// </summary>
    public enum StatusEffectType
    {
        Bleeding,
        Burning,
        Poisoned,
        Stunned,
        Slowed,
        Weakened,
        Strengthened,
        Regenerating
    }

    /// <summary>
    /// Status effect implementation
    /// </summary>
    [Serializable]
    public class StatusEffect
    {
        public StatusEffectType Type;
        public float Duration;
        public float RemainingTime;
        public float Intensity;
        public bool IsExpired => RemainingTime <= 0f;

        public StatusEffect(StatusEffectType type, float duration, float intensity)
        {
            Type = type;
            Duration = duration;
            RemainingTime = duration;
            Intensity = intensity;
        }

        public void Update(float deltaTime)
        {
            RemainingTime -= deltaTime;
        }

        public void RefreshDuration(float newDuration)
        {
            RemainingTime = Mathf.Max(RemainingTime, newDuration);
        }

        public void ApplyEffect(HealthComponent healthComponent)
        {
            // Apply initial effect
        }

        public void RemoveEffect(HealthComponent healthComponent)
        {
            // Remove effect
        }

        public float GetPerformanceModifier()
        {
            return Type switch
            {
                StatusEffectType.Stunned => 0.1f,
                StatusEffectType.Slowed => 0.7f,
                StatusEffectType.Weakened => 0.8f,
                StatusEffectType.Strengthened => 1.2f,
                _ => 1f
            };
        }
    }

    #endregion
}