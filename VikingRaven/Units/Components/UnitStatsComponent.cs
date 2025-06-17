using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;
using VikingRaven.Units.AI;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;
using VikingRaven.Units.Managers;
using VikingRaven.Units.Models;

namespace VikingRaven.Units.Components
{
    /// <summary>
    /// MonoBehaviour wrapper for UnitStatsManager
    /// Hosts the stats data and handles Unity integration
    /// Enhanced with beautiful Odin Inspector visualization
    /// </summary>
    public class UnitStatsComponent : BaseComponent
    {
        #region Stats Manager Instance
        
        [TitleGroup("Unit Statistics Manager")]
        [InfoBox("Event-driven stats system - All changes automatically trigger Actions for seamless integration", InfoMessageType.Info)]
        [SerializeField, HideLabel, InlineProperty]
        private UnitStatsManager _statsManager = new UnitStatsManager();
        
        #endregion
        
        #region Live Stats Overview
        
        [TitleGroup("Live Stats Overview")]
        [InfoBox("Real-time statistics overview - Updates automatically during gameplay", InfoMessageType.None)]
        
        [HorizontalGroup("Live Stats Overview/Row1")]
        [BoxGroup("Live Stats Overview/Row1/Health Status")]
        [LabelText("Health"), ProgressBar(0, "MaxHealth", ColorGetter = "GetHealthColor")]
        [ShowInInspector, ReadOnly] private float CurrentHealth => _statsManager.CurrentHealth;
        
        [BoxGroup("Live Stats Overview/Row1/Health Status")]
        [LabelText("Shield"), ProgressBar(0, "MaxShield", ColorGetter = "GetShieldColor")]
        [ShowInInspector, ReadOnly] private float CurrentShield => _statsManager.CurrentShield;
        
        [BoxGroup("Live Stats Overview/Row1/Health Status")]
        [LabelText("Alive"), ReadOnly, ToggleLeft]
        [ShowInInspector] private bool IsAlive => _statsManager.IsAlive;
        
        [HorizontalGroup("Live Stats Overview/Row1")]
        [BoxGroup("Live Stats Overview/Row1/Combat Status")]
        [LabelText("Effective Damage"), ReadOnly, ProgressBar(0, 200, ColorGetter = "GetDamageColor")]
        [ShowInInspector] private float EffectiveDamage => _statsManager.GetEffectiveDamage();
        
        [BoxGroup("Live Stats Overview/Row1/Combat Status")]
        [LabelText("Kills"), ReadOnly]
        [ShowInInspector] private int KillCount => _statsManager.KillCount;
        
        [BoxGroup("Live Stats Overview/Row1/Combat Status")]
        [LabelText("Accuracy"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetAccuracyColor")]
        [ShowInInspector] private float Accuracy => _statsManager.AccuracyRate;
        
        [HorizontalGroup("Live Stats Overview/Row2")]
        [BoxGroup("Live Stats Overview/Row2/Movement Status")]
        [LabelText("Move Speed"), ReadOnly, ProgressBar(0, 20, ColorGetter = "GetSpeedColor")]
        [ShowInInspector] private float MoveSpeed => _statsManager.GetEffectiveMoveSpeed();
        
        [BoxGroup("Live Stats Overview/Row2/Movement Status")]
        [LabelText("Can Move"), ReadOnly, ToggleLeft]
        [ShowInInspector] private bool CanMove => _statsManager.CanMove;
        
        [BoxGroup("Live Stats Overview/Row2/Movement Status")]
        [LabelText("Moving"), ReadOnly, ToggleLeft]
        [ShowInInspector] private bool IsMoving => _statsManager.IsMoving;
        
        [HorizontalGroup("Live Stats Overview/Row2")]
        [BoxGroup("Live Stats Overview/Row2/Psychology Status")]
        [LabelText("Morale"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetMoraleColor")]
        [ShowInInspector] private float Morale => _statsManager.CurrentMorale;
        
        [BoxGroup("Live Stats Overview/Row2/Psychology Status")]
        [LabelText("Fear"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetFearColor")]
        [ShowInInspector] private float FearLevel => _statsManager.FearLevel;
        
        [BoxGroup("Live Stats Overview/Row2/Psychology Status")]
        [LabelText("Panicked"), ReadOnly, ToggleLeft]
        [ShowInInspector] private bool IsPanicked => _statsManager.IsPanicked;
        
        [HorizontalGroup("Live Stats Overview/Row3")]
        [BoxGroup("Live Stats Overview/Row3/Progression")]
        [LabelText("Level"), ReadOnly]
        [ShowInInspector] private int Level => _statsManager.CurrentLevel;
        
        [BoxGroup("Live Stats Overview/Row3/Progression")]
        [LabelText("Experience"), ReadOnly, ProgressBar(0, "ExperienceToNextLevel", ColorGetter = "GetExperienceColor")]
        [ShowInInspector] private float Experience => _statsManager.CurrentExperience;
        
        [BoxGroup("Live Stats Overview/Row3/Progression")]
        [LabelText("XP %"), ReadOnly]
        [ShowInInspector] private float ExperiencePercentage => _statsManager.ExperienceToNextLevel > 0 ? (_statsManager.CurrentExperience / _statsManager.ExperienceToNextLevel) * 100f : 0f;
        
        [HorizontalGroup("Live Stats Overview/Row3")]
        [BoxGroup("Live Stats Overview/Row3/Equipment")]
        [LabelText("Weapon Condition"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetConditionColor")]
        [ShowInInspector] private float WeaponCondition => _statsManager.WeaponCondition;
        
        [BoxGroup("Live Stats Overview/Row3/Equipment")]
        [LabelText("Armor Condition"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetConditionColor")]
        [ShowInInspector] private float ArmorCondition => _statsManager.ArmorCondition;
        
        [BoxGroup("Live Stats Overview/Row3/Equipment")]
        [LabelText("Equipment Avg"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetConditionColor")]
        [ShowInInspector] private float EquipmentAverage => (_statsManager.WeaponCondition + _statsManager.ArmorCondition) / 2f;
        
        #endregion
        
        #region Status Effects & Environment
        
        [TitleGroup("Status Effects & Environment")]
        [InfoBox("Active status effects and environmental conditions affecting unit performance", InfoMessageType.Warning)]
        
        [HorizontalGroup("Status Effects & Environment/StatusRow")]
        [BoxGroup("Status Effects & Environment/StatusRow/Active Effects")]
        [LabelText("Active Status Effects")]
        [ListDrawerSettings(ShowIndexLabels = false, DraggableItems = false, ShowPaging = true, NumberOfItemsPerPage = 3)]
        [ShowInInspector, ReadOnly] private System.Collections.Generic.List<StatusEffect> ActiveEffects => _statsManager.ActiveStatusEffects;
        
        [BoxGroup("Status Effects & Environment/StatusRow/Active Effects")]
        [LabelText("Effect Count"), ReadOnly]
        [ShowInInspector] private int StatusEffectCount => _statsManager.ActiveStatusEffects?.Count ?? 0;
        
        [HorizontalGroup("Status Effects & Environment/StatusRow")]
        [BoxGroup("Status Effects & Environment/StatusRow/Environment")]
        [LabelText("Weather"), ReadOnly, EnumToggleButtons]
        [ShowInInspector] private WeatherType Weather => _statsManager.CurrentWeather;
        
        [BoxGroup("Status Effects & Environment/StatusRow/Environment")]
        [LabelText("Terrain"), ReadOnly, EnumToggleButtons]
        [ShowInInspector] private TerrainType Terrain => _statsManager.CurrentTerrain;
        
        [BoxGroup("Status Effects & Environment/StatusRow/Environment")]
        [LabelText("Time of Day"), ReadOnly, EnumToggleButtons]
        [ShowInInspector] private TimeOfDay TimeOfDay => _statsManager.TimeOfDay;
        
        [BoxGroup("Status Effects & Environment/StatusRow/Environment")]
        [LabelText("Environmental Impact"), ReadOnly, ProgressBar(0.3f, 2f, ColorGetter = "GetEnvironmentalColor")]
        [ShowInInspector] private float EnvironmentalImpact => _statsManager.WeatherModifier * _statsManager.TerrainModifier * _statsManager.TimeModifier;
        
        #endregion
        
        #region Combat Effectiveness Analysis
        
        [TitleGroup("Combat Effectiveness Analysis")]
        [InfoBox("Comprehensive combat effectiveness breakdown and performance metrics", InfoMessageType.Info)]
        
        [HorizontalGroup("Combat Effectiveness Analysis/EffectivenessRow")]
        [BoxGroup("Combat Effectiveness Analysis/EffectivenessRow/Overall")]
        [LabelText("Combat Effectiveness"), ReadOnly, ProgressBar(0, 150, ColorGetter = "GetEffectivenessColor")]
        [ShowInInspector] private float CombatEffectiveness => _statsManager.GetCombatEffectiveness();
        
        [BoxGroup("Combat Effectiveness Analysis/EffectivenessRow/Overall")]
        [LabelText("Health Factor"), ReadOnly, ProgressBar(0, 100)]
        [ShowInInspector] private float HealthFactor => _statsManager.MaxHealth > 0 ? (_statsManager.CurrentHealth / _statsManager.MaxHealth) * 100f : 0f;
        
        [BoxGroup("Combat Effectiveness Analysis/EffectivenessRow/Overall")]
        [LabelText("Morale Factor"), ReadOnly, ProgressBar(0, 100)]
        [ShowInInspector] private float MoraleFactor => _statsManager.CurrentMorale;
        
        [HorizontalGroup("Combat Effectiveness Analysis/EffectivenessRow")]
        [BoxGroup("Combat Effectiveness Analysis/EffectivenessRow/Modifiers")]
        [LabelText("Damage Modifier"), ReadOnly, Range(0.1f, 3f)]
        [ShowInInspector] private float DamageModifier => _statsManager.DamageModifier;
        
        [BoxGroup("Combat Effectiveness Analysis/EffectivenessRow/Modifiers")]
        [LabelText("Speed Modifier"), ReadOnly, Range(0.1f, 3f)]
        [ShowInInspector] private float SpeedModifier => _statsManager.SpeedModifier;
        
        [BoxGroup("Combat Effectiveness Analysis/EffectivenessRow/Modifiers")]
        [LabelText("Range Modifier"), ReadOnly, Range(0.1f, 2f)]
        [ShowInInspector] private float RangeModifier => _statsManager.RangeModifier;
        
        #endregion
        
        #region Unit Information
        
        [TitleGroup("Unit Information")]
        [InfoBox("Basic unit identification and configuration details", InfoMessageType.None)]
        
        [HorizontalGroup("Unit Information/InfoRow")]
        [BoxGroup("Unit Information/InfoRow/Identity")]
        [LabelText("Unit ID"), ReadOnly]
        [ShowInInspector] private uint UnitID => _unitModel.UnitId;
        
        [BoxGroup("Unit Information/InfoRow/Identity")]
        [LabelText("Unit Name"), ReadOnly]
        [ShowInInspector] private string UnitName => _unitModel?.DisplayName ?? "Unknown";
        
        [BoxGroup("Unit Information/InfoRow/Identity")]
        [LabelText("Unit Type"), ReadOnly, EnumToggleButtons]
        [ShowInInspector] private UnitType UnitType => _unitModel?.UnitType ?? UnitType.Infantry;
        
        [HorizontalGroup("Unit Information/InfoRow")]
        [BoxGroup("Unit Information/InfoRow/Runtime")]
        [LabelText("Time Alive"), ReadOnly]
        [ShowInInspector] private float TimeAlive => Time.time - _initializationTime;
        
        [BoxGroup("Unit Information/InfoRow/Runtime")]
        [LabelText("Battles Fought"), ReadOnly]
        [ShowInInspector] private int BattlesFought => _statsManager.BattlesParticipated;
        
        [BoxGroup("Unit Information/InfoRow/Runtime")]
        [LabelText("Is Initialized"), ReadOnly, ToggleLeft]
        [ShowInInspector] private bool IsInitialized => _isInitialized;
        
        #endregion
        
        #region Dependencies and State
        
        private UnitModel _unitModel;
        private CombatComponent _combatComponent;
        private float _statusEffectUpdateTimer = 0f;
        private float _initializationTime = 0f;
        private bool _isInitialized = false;
        private const float STATUS_EFFECT_UPDATE_INTERVAL = 0.5f;
        
        #endregion
        
        #region Unity Lifecycle
        
        public override void Initialize()
        {
            base.Initialize();
            
            _unitModel = Entity.GetComponent<UnitModel>();
            _combatComponent = Entity.GetComponent<CombatComponent>();
            
            if (_unitModel != null)
            {
                _statsManager.Initialize(_unitModel, Entity.Id);
                SubscribeToEvents();
                
                _initializationTime = Time.time;
                _isInitialized = true;
                
            }
            else
            {
                _isInitialized = false;
            }
        }
        
        private void Update()
        {
            // Process status effects periodically
            _statusEffectUpdateTimer += Time.deltaTime;
            if (_statusEffectUpdateTimer >= STATUS_EFFECT_UPDATE_INTERVAL)
            {
                _statsManager.ProcessStatusEffects();
                _statusEffectUpdateTimer = 0f;
            }
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        #endregion
        
        #region Event Subscription
        
        private void SubscribeToEvents()
        {
            // Health events
            _statsManager.OnHealthChanged += OnHealthChanged;
            _statsManager.OnDamageTaken += OnDamageTaken;
            _statsManager.OnHealed += OnHealed;
            _statsManager.OnDied += OnUnitDied;
            
            // Combat events  
            _statsManager.OnDamageDealt += OnDamageDealt;
            _statsManager.OnKillScored += OnKillScored;
            _statsManager.OnAttackResult += OnAttackResult;
            
            // Status events
            _statsManager.OnStatusEffectApplied += OnStatusEffectApplied;
            _statsManager.OnStatusEffectRemoved += OnStatusEffectRemoved;
            
            // Experience events
            _statsManager.OnExperienceGained += OnExperienceGained;
            _statsManager.OnLevelUp += OnLevelUp;
            
            // Morale events
            _statsManager.OnMoraleChanged += OnMoraleChanged;
            _statsManager.OnPanicStateChanged += OnPanicStateChanged;
            
            // Movement events
            _statsManager.OnMovementStateChanged += OnMovementStateChanged;
        }
        
        /// <summary>
        /// Unsubscribe from events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (_statsManager == null) return;
            
            // Health events
            _statsManager.OnHealthChanged -= OnHealthChanged;
            _statsManager.OnDamageTaken -= OnDamageTaken;
            _statsManager.OnHealed -= OnHealed;
            _statsManager.OnDied -= OnUnitDied;
            
            // Combat events
            _statsManager.OnDamageDealt -= OnDamageDealt;
            _statsManager.OnKillScored -= OnKillScored;
            _statsManager.OnAttackResult -= OnAttackResult;
            
            // Status events
            _statsManager.OnStatusEffectApplied -= OnStatusEffectApplied;
            _statsManager.OnStatusEffectRemoved -= OnStatusEffectRemoved;
            
            // Experience events
            _statsManager.OnExperienceGained -= OnExperienceGained;
            _statsManager.OnLevelUp -= OnLevelUp;
            
            // Morale events
            _statsManager.OnMoraleChanged -= OnMoraleChanged;
            _statsManager.OnPanicStateChanged -= OnPanicStateChanged;
            
            // Movement events
            _statsManager.OnMovementStateChanged -= OnMovementStateChanged;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnHealthChanged(float currentHealth, float maxHealth)
        {
            // Update health UI, trigger VFX if needed
            float healthPercentage = currentHealth / maxHealth;
            
            if (healthPercentage < 0.25f)
            {
                // Show critical health effects
                Debug.Log($"{Entity.Id}: Critical health - {healthPercentage:P0}");
            }
        }
        
        private void OnDamageTaken(float damage, float actualDamage)
        {
            // Trigger damage VFX, screen shake, etc.
            Debug.Log($"{Entity.Id}: Took {actualDamage} damage (from {damage})");
            
            // Could trigger damage number UI, blood effects, etc.
        }
        
        private void OnHealed(float healAmount, float actualHeal)
        {
            // Trigger healing VFX
            Debug.Log($"{Entity.Id}: Healed {actualHeal} health");
        }
        
        private void OnUnitDied()
        {
            // Trigger death animation, disable AI, etc.
            Debug.Log($"{Entity.Id}: Unit has died");
            
            // Disable combat AI if present
            var combatAI = Entity.GetComponent<CombatAIComponent>();
            if (combatAI != null)
            {
                combatAI.SetCombatAIEnabled(false);
            }
        }
        
        private void OnDamageDealt(float damage)
        {
            // Trigger attack VFX, screen effects
            Debug.Log($"{Entity.Id}: Dealt {damage} damage");
        }
        
        private void OnKillScored()
        {
            // Show kill notification, effects
            Debug.Log($"{Entity.Id}: Scored a kill! Total: {_statsManager.KillCount}");
        }
        
        private void OnAttackResult(bool hit)
        {
            // Update accuracy tracking
            if (!hit)
            {
                Debug.Log($"{Entity.Id}: Attack missed");
            }
        }
        
        private void OnStatusEffectApplied(StatusEffect effect)
        {
            // Show status effect UI, VFX
            Debug.Log($"{Entity.Id}: Status effect applied - {effect.Type} for {effect.Duration}s");
        }
        
        private void OnStatusEffectRemoved(StatusEffect effect)
        {
            // Remove status effect UI
            Debug.Log($"{Entity.Id}: Status effect removed - {effect.Type}");
        }
        
        private void OnExperienceGained(float amount)
        {
            // Show XP gain UI
            Debug.Log($"{Entity.Id}: Gained {amount} XP");
        }
        
        private void OnLevelUp(int newLevel)
        {
            // Show level up VFX, UI notification
            Debug.Log($"{Entity.Id}: Level UP! Now level {newLevel}");
        }
        
        private void OnMoraleChanged(float newMorale)
        {
            // Update morale UI, change unit behavior
            if (newMorale < 25f)
            {
                Debug.Log($"{Entity.Id}: Low morale - {newMorale:F0}");
            }
        }
        
        private void OnPanicStateChanged(bool isPanicked)
        {
            // Change unit behavior, visual effects
            if (isPanicked)
            {
                Debug.Log($"{Entity.Id}: Unit is panicking!");
            }
        }
        
        private void OnMovementStateChanged(bool canMove)
        {
            // Update movement AI, visual indicators
            Debug.Log($"{Entity.Id}: Movement state changed - Can move: {canMove}");
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Get the stats manager instance
        /// </summary>
        public UnitStatsManager StatsManager => _statsManager;
        
        /// <summary>
        /// Quick access methods for common operations
        /// </summary>
        public void TakeDamage(float damage) => _statsManager.TakeDamage(damage);
        public void Heal(float amount) => _statsManager.Heal(amount);
        public void RecordDamageDealt(float damage) => _statsManager.RecordDamageDealt(damage);
        public void RecordKill() => _statsManager.RecordKill();
        public void ApplyStatusEffect(StatusEffect effect) => _statsManager.ApplyStatusEffect(effect);
        public void GainExperience(float amount) => _statsManager.GainExperience(amount);
        
        /// <summary>
        /// Quick access properties
        /// </summary>
        public bool IsUnitAlive => _statsManager.IsAlive;
        public bool CanUnitMove => _statsManager.CanMove;
        public bool CanUnitAttack => _statsManager.IsAlive && !_statsManager.IsStunned;
        public float GetEffectiveDamage() => _statsManager.GetEffectiveDamage();
        public float GetEffectiveSpeed() => _statsManager.GetEffectiveMoveSpeed();
        public float GetCombatEffectiveness() => _statsManager.GetCombatEffectiveness();
        
        #endregion
        
        #region Integration Methods
        
        /// <summary>
        /// Integration with CombatComponent
        /// </summary>
        public void IntegrateWithCombat()
        {
            if (_combatComponent == null) return;
            
            // Update combat component when stats change
            _statsManager.OnCombatStatsChanged += (damage, range, speed) =>
            {
                // Could update CombatComponent here if needed
            };
        }
        
        /// <summary>
        /// Update environmental conditions
        /// </summary>
        public void UpdateEnvironment(WeatherType weather, TerrainType terrain, TimeOfDay timeOfDay)
        {
            _statsManager.CurrentWeather = weather;
            _statsManager.CurrentTerrain = terrain;
            _statsManager.TimeOfDay = timeOfDay;
        }
        
        /// <summary>
        /// Battle start/end tracking
        /// </summary>
        public void StartBattle()
        {
            _statsManager.BattlesParticipated++;
        }
        
        #endregion
        
        #region Odin Inspector Color Getters
        
        private Color GetHealthColor()
        {
            if (!_statsManager.IsAlive) return Color.gray;
            float percentage = _statsManager.CurrentHealth / _statsManager.MaxHealth;
            if (percentage > 0.75f) return Color.green;
            if (percentage > 0.5f) return Color.yellow;
            if (percentage > 0.25f) return new Color(1f, 0.65f, 0f); // Orange
            return Color.red;
        }
        
        private Color GetShieldColor() => Color.cyan;
        
        private Color GetDamageColor() => new Color(1f, 0.3f, 0.3f); // Light red
        
        private Color GetAccuracyColor()
        {
            float accuracy = _statsManager.AccuracyRate;
            if (accuracy > 80f) return Color.green;
            if (accuracy > 60f) return Color.yellow;
            if (accuracy > 40f) return new Color(1f, 0.65f, 0f);
            return Color.red;
        }
        
        private Color GetSpeedColor() => new Color(0.4f, 1f, 0.4f); // Light green
        
        private Color GetMoraleColor()
        {
            float morale = _statsManager.CurrentMorale;
            if (morale > 75f) return Color.green;
            if (morale > 50f) return Color.yellow;
            if (morale > 25f) return new Color(1f, 0.65f, 0f);
            return Color.red;
        }
        
        private Color GetFearColor() => new Color(0.8f, 0.2f, 0.8f); // Purple
        
        private Color GetExperienceColor() => new Color(0.3f, 0.7f, 1f); // Sky blue
        
        private Color GetConditionColor() => new Color(0.2f, 0.6f, 1f); // Blue
        
        private Color GetEnvironmentalColor()
        {
            float impact = _statsManager.WeatherModifier * _statsManager.TerrainModifier * _statsManager.TimeModifier;
            if (impact > 1.1f) return Color.green;
            if (impact > 0.9f) return Color.yellow;
            return Color.red;
        }
        
        private Color GetEffectivenessColor()
        {
            float effectiveness = _statsManager.GetCombatEffectiveness();
            if (effectiveness > 100f) return Color.green;
            if (effectiveness > 75f) return Color.yellow;
            if (effectiveness > 50f) return new Color(1f, 0.65f, 0f);
            return Color.red;
        }
        
        #endregion
    }
}