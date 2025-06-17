using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;
using VikingRaven.Units.Team;

namespace VikingRaven.Units.Managers
{
    /// <summary>
    /// Event-driven Stats Manager - Pure C# class for unit statistics
    /// Initialized when unit is created, updates via Actions
    /// Enhanced with Odin Inspector for beautiful data visualization
    /// </summary>
    [System.Serializable]
    public class UnitStatsManager
    {
        #region Health & Vitality System
        
        [TitleGroup("Health & Vitality System")]
        [InfoBox("Core health and survival statistics with regeneration capabilities", InfoMessageType.Info)]
        
        [BoxGroup("Health & Vitality System/Current Health")]
        [LabelText("Current Health"), ProgressBar(0, "MaxHealth", ColorGetter = "GetHealthBarColor")]
        [ShowInInspector] public float CurrentHealth;
        
        [BoxGroup("Health & Vitality System/Current Health")]
        [LabelText("Max Health"), ReadOnly]
        [ShowInInspector] public float MaxHealth;
        
        [BoxGroup("Health & Vitality System/Current Health")]
        [LabelText("Health %"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetHealthBarColor")]
        [ShowInInspector] private float HealthPercentage => MaxHealth > 0 ? (CurrentHealth / MaxHealth) * 100f : 0f;
        
        [BoxGroup("Health & Vitality System/Shield System")]
        [LabelText("Current Shield"), ProgressBar(0, "MaxShield", ColorGetter = "GetShieldBarColor")]
        [ShowInInspector] public float CurrentShield;
        
        [BoxGroup("Health & Vitality System/Shield System")]
        [LabelText("Max Shield"), ReadOnly]
        [ShowInInspector] public float MaxShield;
        
        [BoxGroup("Health & Vitality System/Vital State")]
        [LabelText("Is Alive"), ReadOnly, ToggleLeft]
        [ShowInInspector] public bool IsAlive;
        
        [BoxGroup("Health & Vitality System/Vital State")]
        [LabelText("Is Wounded"), ReadOnly, ToggleLeft]
        [ShowInInspector] public bool IsWounded;
        
        [BoxGroup("Health & Vitality System/Vital State")]
        [LabelText("Is Critical"), ReadOnly, ToggleLeft]
        [ShowInInspector] public bool IsCritical;
        
        #endregion
        
        #region Combat Performance System
        
        [TitleGroup("Combat Performance System")]
        [InfoBox("Real-time combat statistics, damage tracking, and battle effectiveness", InfoMessageType.Warning)]
        
        [BoxGroup("Combat Performance System/Base Combat Stats")]
        [LabelText("Base Damage"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetDamageBarColor")]
        [ShowInInspector] public float BaseDamage;
        
        [BoxGroup("Combat Performance System/Base Combat Stats")]
        [LabelText("Attack Range"), ReadOnly]
        [ShowInInspector] public float AttackRange;
        
        [BoxGroup("Combat Performance System/Base Combat Stats")]
        [LabelText("Attack Speed"), ReadOnly]
        [ShowInInspector] public float AttackSpeed;
        
        [BoxGroup("Combat Performance System/Combat Statistics")]
        [LabelText("Total Damage Dealt"), ReadOnly, DisplayAsString]
        [ShowInInspector] public float TotalDamageDealt;
        
        [BoxGroup("Combat Performance System/Combat Statistics")]
        [LabelText("Total Damage Taken"), ReadOnly, DisplayAsString]
        [ShowInInspector] public float TotalDamageTaken;
        
        [BoxGroup("Combat Performance System/Combat Statistics")]
        [LabelText("Kill Count"), ReadOnly]
        [ShowInInspector] public int KillCount;
        
        [BoxGroup("Combat Performance System/Combat Statistics")]
        [LabelText("Accuracy Rate"), ReadOnly, ProgressBar(0, 100, ColorGetter = "GetAccuracyBarColor")]
        [ShowInInspector] public float AccuracyRate;
        
        #endregion
        
        #region Movement & Mobility System
        
        [TitleGroup("Movement & Mobility System")]
        [InfoBox("Movement capabilities, speed modifiers, and mobility restrictions", InfoMessageType.None)]
        
        [BoxGroup("Movement & Mobility System/Base Movement")]
        [LabelText("Base Move Speed"), ReadOnly]
        [ShowInInspector] public float BaseMoveSpeed;
        
        [BoxGroup("Movement & Mobility System/Base Movement")]
        [LabelText("Current Move Speed"), ReadOnly, ProgressBar(0, 20, ColorGetter = "GetSpeedBarColor")]
        [ShowInInspector] public float CurrentMoveSpeed;
        
        [BoxGroup("Movement & Mobility System/Movement State")]
        [LabelText("Can Move"), ReadOnly, ToggleLeft]
        [ShowInInspector] public bool CanMove;
        
        [BoxGroup("Movement & Mobility System/Movement State")]
        [LabelText("Is Moving"), ReadOnly, ToggleLeft]
        [ShowInInspector] public bool IsMoving;
        
        [BoxGroup("Movement & Mobility System/Movement Restrictions")]
        [LabelText("Is Stunned"), ReadOnly, ToggleLeft]
        [ShowInInspector] public bool IsStunned;
        
        [BoxGroup("Movement & Mobility System/Movement Restrictions")]
        [LabelText("Is Rooted"), ReadOnly, ToggleLeft]
        [ShowInInspector] public bool IsRooted;
        
        #endregion
        
        #region Morale & Psychology System
        
        [TitleGroup("Morale & Psychology System")]
        [InfoBox("Unit morale, fear levels, and psychological warfare effects")]
        
        [BoxGroup("Morale & Psychology System/Morale")]
        [LabelText("Current Morale"), ProgressBar(0, 100, ColorGetter = "GetMoraleBarColor")]
        [ShowInInspector] public float CurrentMorale;
        
        [BoxGroup("Morale & Psychology System/Morale")]
        [LabelText("Base Morale"), ReadOnly]
        [ShowInInspector] public float BaseMorale;
        
        [BoxGroup("Morale & Psychology System/Fear System")]
        [LabelText("Fear Level"), ProgressBar(0, 100, ColorGetter = "GetFearBarColor")]
        [ShowInInspector] public float FearLevel;
        
        [BoxGroup("Morale & Psychology System/Psychological State")]
        [LabelText("Is Panicked"), ReadOnly, ToggleLeft]
        [ShowInInspector] public bool IsPanicked;
        
        [BoxGroup("Morale & Psychology System/Psychological State")]
        [LabelText("Is Routing"), ReadOnly, ToggleLeft]
        [ShowInInspector] public bool IsRouting;
        
        [BoxGroup("Morale & Psychology System/Psychological State")]
        [LabelText("Is Inspired"), ReadOnly, ToggleLeft]
        [ShowInInspector] public bool IsInspired;
        
        #endregion
        
        #region Experience & Progression System
        
        [TitleGroup("Experience & Progression System")]
        [InfoBox("Level progression, experience tracking, and battle statistics", InfoMessageType.Info)]
        
        [BoxGroup("Experience & Progression System/Level Information")]
        [LabelText("Current Level"), Range(1, 20)]
        [ShowInInspector] public int CurrentLevel;
        
        [BoxGroup("Experience & Progression System/Level Information")]
        [LabelText("Current Experience"), ProgressBar(0, "ExperienceToNextLevel", ColorGetter = "GetExperienceBarColor")]
        [ShowInInspector] public float CurrentExperience;
        
        [BoxGroup("Experience & Progression System/Level Information")]
        [LabelText("Experience to Next Level"), ReadOnly]
        [ShowInInspector] public float ExperienceToNextLevel;
        
        [BoxGroup("Experience & Progression System/Experience Statistics")]
        [LabelText("Total Experience Gained"), ReadOnly, DisplayAsString]
        [ShowInInspector] public float TotalExperienceGained;
        
        [BoxGroup("Experience & Progression System/Experience Statistics")]
        [LabelText("Battles Participated"), ReadOnly]
        [ShowInInspector] public int BattlesParticipated;
        
        #endregion
        
        #region Status Effects System
        
        [TitleGroup("Status Effects System")]
        [InfoBox("Active status effects, buffs, debuffs, and magical influences", InfoMessageType.Error)]
        
        [BoxGroup("Status Effects System/Active Effects")]
        [LabelText("Active Status Effects")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = false, ShowPaging = true, NumberOfItemsPerPage = 5)]
        [ShowInInspector] public List<StatusEffect> ActiveStatusEffects;
        
        [BoxGroup("Status Effects System/Status Immunities")]
        [LabelText("Status Immunities"), EnumToggleButtons]
        [ShowInInspector] public StatusType StatusImmunities;
        
        #endregion
        
        #region Equipment & Gear System
        
        [TitleGroup("Equipment & Gear System")]
        [InfoBox("Equipment status, gear condition, and combat readiness", InfoMessageType.Warning)]
        
        [BoxGroup("Equipment & Gear System/Equipped Items")]
        [LabelText("Equipped Weapon"), EnumToggleButtons]
        [ShowInInspector] public WeaponType EquippedWeapon;
        
        [BoxGroup("Equipment & Gear System/Equipped Items")]
        [LabelText("Equipped Armor"), EnumToggleButtons]
        [ShowInInspector] public ArmorType EquippedArmor;
        
        [BoxGroup("Equipment & Gear System/Equipment Condition")]
        [LabelText("Weapon Condition"), ProgressBar(0, 100, ColorGetter = "GetConditionBarColor")]
        [ShowInInspector] public float WeaponCondition;
        
        [BoxGroup("Equipment & Gear System/Equipment Condition")]
        [LabelText("Armor Condition"), ProgressBar(0, 100, ColorGetter = "GetConditionBarColor")]
        [ShowInInspector] public float ArmorCondition;
        
        [BoxGroup("Equipment & Gear System/Shield System")]
        [LabelText("Has Shield"), ToggleLeft]
        [ShowInInspector] public bool HasShield;
        
        [BoxGroup("Equipment & Gear System/Shield System")]
        [LabelText("Shield Condition"), ShowIf("HasShield"), ProgressBar(0, 100, ColorGetter = "GetConditionBarColor")]
        [ShowInInspector] public float ShieldCondition;
        
        #endregion
        
        #region Environmental Factors System
        
        [TitleGroup("Environmental Factors System")]
        [InfoBox("Environmental conditions affecting unit performance and effectiveness", InfoMessageType.None)]
        
        [BoxGroup("Environmental Factors System/Current Environment")]
        [LabelText("Current Weather"), EnumToggleButtons]
        [ShowInInspector] public WeatherType CurrentWeather;
        
        [BoxGroup("Environmental Factors System/Current Environment")]
        [LabelText("Current Terrain"), EnumToggleButtons]
        [ShowInInspector] public TerrainType CurrentTerrain;
        
        [BoxGroup("Environmental Factors System/Current Environment")]
        [LabelText("Time of Day"), EnumToggleButtons]
        [ShowInInspector] public TimeOfDay TimeOfDay;
        
        [BoxGroup("Environmental Factors System/Environmental Modifiers")]
        [LabelText("Weather Modifier"), Range(0.5f, 1.5f)]
        [ShowInInspector] public float WeatherModifier;
        
        [BoxGroup("Environmental Factors System/Environmental Modifiers")]
        [LabelText("Terrain Modifier"), Range(0.5f, 1.5f)]
        [ShowInInspector] public float TerrainModifier;
        
        [BoxGroup("Environmental Factors System/Environmental Modifiers")]
        [LabelText("Time Modifier"), Range(0.8f, 1.2f)]
        [ShowInInspector] public float TimeModifier;
        
        #endregion
        
        #region Combat Modifiers System
        
        [TitleGroup("Combat Modifiers System")]
        [InfoBox("Dynamic combat modifiers from abilities, equipment, and temporary effects", InfoMessageType.Info)]
        
        [BoxGroup("Combat Modifiers System/Core Modifiers")]
        [LabelText("Damage Modifier"), Range(0.1f, 3f)]
        [ShowInInspector] public float DamageModifier;
        
        [BoxGroup("Combat Modifiers System/Core Modifiers")]
        [LabelText("Speed Modifier"), Range(0.1f, 3f)]
        [ShowInInspector] public float SpeedModifier;
        
        [BoxGroup("Combat Modifiers System/Core Modifiers")]
        [LabelText("Range Modifier"), Range(0.1f, 2f)]
        [ShowInInspector] public float RangeModifier;
        
        #endregion
        
        #region Events - Action System
        
        // Health Events
        public Action<float, float> OnHealthChanged;           // (currentHealth, maxHealth)
        public Action<float, float> OnDamageTaken;             // (damage, actualDamage)
        public Action<float, float> OnHealed;                  // (healAmount, actualHeal)
        public Action<float, float> OnShieldChanged;           // (currentShield, maxShield)
        public Action OnDied;
        public Action OnRevived;
        
        // Combat Events
        public Action<float> OnDamageDealt;                    // (damageAmount)
        public Action OnKillScored;
        public Action<bool> OnAttackResult;                    // (hit/miss)
        public Action<float, float, float> OnCombatStatsChanged; // (damage, range, speed)
        
        // Movement Events
        public Action<float> OnMoveSpeedChanged;               // (newSpeed)
        public Action<bool> OnMovementStateChanged;            // (canMove)
        public Action<bool> OnMovingStateChanged;              // (isMoving)
        
        // Status Effects Events
        public Action<StatusEffect> OnStatusEffectApplied;
        public Action<StatusEffect> OnStatusEffectRemoved;
        public Action<StatusType> OnStatusEffectExpired;
        public Action<List<StatusEffect>> OnStatusEffectsUpdated;
        
        // Morale Events
        public Action<float> OnMoraleChanged;                  // (newMorale)
        public Action<float> OnFearLevelChanged;               // (newFearLevel)
        public Action<bool> OnPanicStateChanged;               // (isPanicked)
        public Action<bool> OnRoutingStateChanged;             // (isRouting)
        
        // Experience Events
        public Action<float> OnExperienceGained;               // (expAmount)
        public Action<int> OnLevelUp;                          // (newLevel)
        public Action<int> OnBattleParticipated;               // (totalBattles)
        
        // Equipment Events
        public Action<WeaponType> OnWeaponChanged;
        public Action<ArmorType> OnArmorChanged;
        public Action<float> OnEquipmentConditionChanged;      // (averageCondition)
        
        // Environmental Events
        public Action<WeatherType> OnWeatherChanged;
        public Action<TerrainType> OnTerrainChanged;
        public Action<TimeOfDay> OnTimeChanged;
        public Action<float, float, float> OnEnvironmentalModifiersChanged; // (weather, terrain, time)
        
        // Modifier Events
        public Action<float, float, float> OnCombatModifiersChanged; // (damage, speed, range)
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize UnitStatsManager with data from UnitDataSO
        /// Called when creating a new unit
        /// </summary>
        public void Initialize(Unit unitData, uint unitId)
        {
            if (unitData == null)
            {
                Debug.LogError("UnitStatsManager: Cannot initialize with null UnitDataSO");
                return;
            }
            
            // Initialize Health System
            MaxHealth = unitData.HitPoints;
            CurrentHealth = MaxHealth;
            MaxShield = unitData.Shield;
            CurrentShield = MaxShield;
            IsAlive = true;
            IsWounded = false;
            IsCritical = false;
            
            // Initialize Combat Stats
            BaseDamage = unitData.Damage;
            AttackRange = unitData.Range;
            AttackSpeed = 1f / unitData.HitSpeed;
            TotalDamageDealt = 0f;
            TotalDamageTaken = 0f;
            KillCount = 0;
            AccuracyRate = 100f;
            
            // Initialize Movement Stats
            BaseMoveSpeed = unitData.MoveSpeed;
            CurrentMoveSpeed = BaseMoveSpeed;
            CanMove = true;
            IsMoving = false;
            IsStunned = false;
            IsRooted = false;
            
            // Initialize Morale & Psychology
            BaseMorale = 75f; // Default base morale
            CurrentMorale = BaseMorale;
            FearLevel = 0f;
            IsPanicked = false;
            IsRouting = false;
            IsInspired = false;
            
            // Initialize Experience System
            CurrentLevel = 1;
            CurrentExperience = 0f;
            ExperienceToNextLevel = CalculateExperienceForLevel(2);
            TotalExperienceGained = 0f;
            BattlesParticipated = 0;
            
            // Initialize Status Effects
            ActiveStatusEffects = new List<StatusEffect>();
            StatusImmunities = StatusType.None;
            
            // Initialize Equipment based on unit type
            InitializeEquipmentByUnitType(unitData.UnitType);
            
            // Initialize Environmental factors
            CurrentWeather = WeatherType.Clear;
            CurrentTerrain = TerrainType.Plains;
            TimeOfDay = TimeOfDay.Day;
            WeatherModifier = 1f;
            TerrainModifier = 1f;
            TimeModifier = 1f;
            
            // Initialize Combat Modifiers
            DamageModifier = 1f;
            SpeedModifier = 1f;
            RangeModifier = 1f;
            
            Debug.Log($"UnitStatsManager: Initialized for {unitData.DisplayName} (ID: {unitId})");
            
            // Trigger initial events
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            OnCombatStatsChanged?.Invoke(BaseDamage, AttackRange, AttackSpeed);
            OnMoveSpeedChanged?.Invoke(CurrentMoveSpeed);
            OnMoraleChanged?.Invoke(CurrentMorale);
        }
        
        /// <summary>
        /// Initialize equipment based on unit type
        /// </summary>
        private void InitializeEquipmentByUnitType(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Infantry:
                    EquippedWeapon = WeaponType.Sword;
                    EquippedArmor = ArmorType.Medium;
                    HasShield = true;
                    break;
                    
                case UnitType.Archer:
                    EquippedWeapon = WeaponType.Bow;
                    EquippedArmor = ArmorType.Light;
                    HasShield = false;
                    break;
                    
                case UnitType.Pike:
                    EquippedWeapon = WeaponType.Spear;
                    EquippedArmor = ArmorType.Heavy;
                    HasShield = false;
                    break;
                    
                default:
                    EquippedWeapon = WeaponType.Sword;
                    EquippedArmor = ArmorType.Light;
                    HasShield = false;
                    break;
            }
            
            WeaponCondition = 100f;
            ArmorCondition = 100f;
            ShieldCondition = HasShield ? 100f : 0f;
        }
        
        #endregion
        
        #region Health Management
        
        /// <summary>
        /// Apply damage to unit
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;
            
            float actualDamage = CalculateActualDamage(damage);
            
            // Apply to shield first
            if (CurrentShield > 0)
            {
                float shieldDamage = Mathf.Min(actualDamage, CurrentShield);
                CurrentShield -= shieldDamage;
                actualDamage -= shieldDamage;
                
                OnShieldChanged?.Invoke(CurrentShield, MaxShield);
            }
            
            // Apply remaining damage to health
            if (actualDamage > 0)
            {
                CurrentHealth -= actualDamage;
                CurrentHealth = Mathf.Max(0, CurrentHealth);
            }
            
            TotalDamageTaken += damage;
            UpdateHealthState();
            
            // Trigger events
            OnDamageTaken?.Invoke(damage, actualDamage);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            
            // Check for death
            if (CurrentHealth <= 0 && IsAlive)
            {
                Die();
            }
        }
        
        /// <summary>
        /// Heal unit
        /// </summary>
        public void Heal(float amount)
        {
            if (!IsAlive) return;
            
            float oldHealth = CurrentHealth;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            float actualHeal = CurrentHealth - oldHealth;
            
            UpdateHealthState();
            
            // Trigger events
            OnHealed?.Invoke(amount, actualHeal);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }
        
        /// <summary>
        /// Kill unit
        /// </summary>
        public void Die()
        {
            if (!IsAlive) return;
            
            IsAlive = false;
            CurrentHealth = 0;
            CanMove = false;
            
            // Clear movement states
            IsMoving = false;
            
            OnDied?.Invoke();
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            OnMovementStateChanged?.Invoke(CanMove);
            
            Debug.Log("UnitStatsManager: Unit has died");
        }
        
        #endregion
        
        #region Status Effects Management
        
        /// <summary>
        /// Apply status effect
        /// </summary>
        public void ApplyStatusEffect(StatusEffect effect)
        {
            if (HasStatusImmunity(effect.Type)) return;
            
            // Remove existing effect of same type
            RemoveStatusEffect(effect.Type);
            
            effect.StartTime = Time.time;
            ActiveStatusEffects.Add(effect);
            
            OnStatusEffectApplied?.Invoke(effect);
            OnStatusEffectsUpdated?.Invoke(ActiveStatusEffects);
        }
        
        /// <summary>
        /// Remove status effect by type
        /// </summary>
        public void RemoveStatusEffect(StatusType statusType)
        {
            for (int i = ActiveStatusEffects.Count - 1; i >= 0; i--)
            {
                if (ActiveStatusEffects[i].Type == statusType)
                {
                    var effect = ActiveStatusEffects[i];
                    ActiveStatusEffects.RemoveAt(i);
                    
                    OnStatusEffectRemoved?.Invoke(effect);
                    OnStatusEffectsUpdated?.Invoke(ActiveStatusEffects);
                }
            }
        }
        
        /// <summary>
        /// Process status effects (called periodically)
        /// </summary>
        public void ProcessStatusEffects()
        {
            bool statusChanged = false;
            
            for (int i = ActiveStatusEffects.Count - 1; i >= 0; i--)
            {
                var effect = ActiveStatusEffects[i];
                
                // Check if effect has expired
                if (Time.time - effect.StartTime >= effect.Duration)
                {
                    ActiveStatusEffects.RemoveAt(i);
                    statusChanged = true;
                    
                    OnStatusEffectExpired?.Invoke(effect.Type);
                    OnStatusEffectRemoved?.Invoke(effect);
                    continue;
                }
                
                // Apply periodic effects
                ProcessPeriodicStatusEffect(effect);
            }
            
            if (statusChanged)
            {
                OnStatusEffectsUpdated?.Invoke(ActiveStatusEffects);
            }
        }
        
        #endregion
        
        #region Combat Statistics
        
        /// <summary>
        /// Record damage dealt by this unit
        /// </summary>
        public void RecordDamageDealt(float damage)
        {
            TotalDamageDealt += damage;
            OnDamageDealt?.Invoke(damage);
        }
        
        /// <summary>
        /// Record a kill by this unit
        /// </summary>
        public void RecordKill()
        {
            KillCount++;
            GainExperience(10f); // Base XP for kill
            
            OnKillScored?.Invoke();
        }
        
        /// <summary>
        /// Record attack accuracy
        /// </summary>
        public void RecordAttackResult(bool hit)
        {
            // Simple running average
            float weight = 0.1f;
            AccuracyRate = AccuracyRate * (1 - weight) + (hit ? 100f : 0f) * weight;
            
            OnAttackResult?.Invoke(hit);
        }
        
        #endregion
        
        #region Experience System
        
        /// <summary>
        /// Gain experience points
        /// </summary>
        public void GainExperience(float amount)
        {
            CurrentExperience += amount;
            TotalExperienceGained += amount;
            
            OnExperienceGained?.Invoke(amount);
            
            // Check for level up
            while (CurrentExperience >= ExperienceToNextLevel && CurrentLevel < 20)
            {
                LevelUp();
            }
        }
        
        /// <summary>
        /// Level up the unit
        /// </summary>
        private void LevelUp()
        {
            CurrentExperience -= ExperienceToNextLevel;
            CurrentLevel++;
            
            // Apply level up bonuses
            ApplyLevelUpBonuses();
            
            ExperienceToNextLevel = CalculateExperienceForLevel(CurrentLevel + 1);
            
            OnLevelUp?.Invoke(CurrentLevel);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
            OnCombatStatsChanged?.Invoke(BaseDamage, AttackRange, AttackSpeed);
            
            Debug.Log($"UnitStatsManager: Unit leveled up to {CurrentLevel}");
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Check if unit has specific status effect
        /// </summary>
        public bool HasStatusEffect(StatusType statusType)
        {
            return ActiveStatusEffects.Exists(effect => effect.Type == statusType);
        }
        
        /// <summary>
        /// Check if unit has immunity to status type
        /// </summary>
        public bool HasStatusImmunity(StatusType statusType)
        {
            return (StatusImmunities & statusType) != 0;
        }
        
        /// <summary>
        /// Get effective damage after all modifiers
        /// </summary>
        public float GetEffectiveDamage()
        {
            return BaseDamage * DamageModifier * WeatherModifier * GetEquipmentDamageModifier();
        }
        
        /// <summary>
        /// Get effective attack range after modifiers
        /// </summary>
        public float GetEffectiveRange()
        {
            return AttackRange * RangeModifier;
        }
        
        /// <summary>
        /// Get current movement speed with all modifiers
        /// </summary>
        public float GetEffectiveMoveSpeed()
        {
            return CurrentMoveSpeed;
        }
        
        /// <summary>
        /// Get combat effectiveness percentage
        /// </summary>
        public float GetCombatEffectiveness()
        {
            float healthFactor = CurrentHealth / MaxHealth;
            float moraleFactor = CurrentMorale / 100f;
            float equipmentFactor = GetAverageEquipmentCondition() / 100f;
            float environmentFactor = WeatherModifier * TerrainModifier * TimeModifier;
            
            return (healthFactor + moraleFactor + equipmentFactor) * environmentFactor * 100f / 3f;
        }
        
        #endregion
        
        #region Private Helper Methods
        
        private void UpdateHealthState()
        {
            float healthPercentage = (CurrentHealth / MaxHealth) * 100f;
            
            IsWounded = healthPercentage < 75f;
            IsCritical = healthPercentage < 25f;
        }
        
        private float CalculateActualDamage(float baseDamage)
        {
            float damage = baseDamage;
            
            // Apply armor reduction
            float armorReduction = ArmorCondition / 100f;
            damage *= (1f - armorReduction * 0.5f); // 50% max reduction at 100% armor
            
            // Apply status effect modifiers
            foreach (var effect in ActiveStatusEffects)
            {
                switch (effect.Type)
                {
                    case StatusType.Shield:
                        damage *= 0.5f;
                        break;
                    case StatusType.Vulnerable:
                        damage *= 1.5f;
                        break;
                }
            }
            
            return damage;
        }
        
        private void ProcessPeriodicStatusEffect(StatusEffect effect)
        {
            switch (effect.Type)
            {
                case StatusType.Poison:
                    TakeDamage(effect.Strength * Time.deltaTime);
                    break;
                case StatusType.Burn:
                    TakeDamage(effect.Strength * Time.deltaTime);
                    break;
                case StatusType.Regeneration:
                    Heal(effect.Strength * Time.deltaTime);
                    break;
            }
        }
        
        private void ApplyLevelUpBonuses()
        {
            MaxHealth += 5f;
            CurrentHealth += 5f;
            BaseDamage += 1f;
            BaseMorale += 2f;
        }
        
        private float CalculateExperienceForLevel(int level)
        {
            return 100f * Mathf.Pow(1.2f, level - 1);
        }
        
        private float GetEquipmentDamageModifier()
        {
            return (WeaponCondition / 100f) * 0.3f + 0.7f; // 70-100% damage based on weapon condition
        }
        
        private float GetAverageEquipmentCondition()
        {
            float total = WeaponCondition + ArmorCondition;
            int count = 2;
            
            if (HasShield)
            {
                total += ShieldCondition;
                count++;
            }
            
            return total / count;
        }
        
        #endregion
        
        #region Odin Inspector Color Getters
        
        private Color GetHealthBarColor()
        {
            if (!IsAlive) return Color.gray;
            float percentage = CurrentHealth / MaxHealth;
            if (percentage > 0.75f) return Color.green;
            if (percentage > 0.5f) return Color.yellow;
            if (percentage > 0.25f) return new Color(1f, 0.65f, 0f); // Orange
            return Color.red;
        }
        
        private Color GetShieldBarColor()
        {
            return Color.cyan;
        }
        
        private Color GetDamageBarColor()
        {
            return new Color(1f, 0.3f, 0.3f); // Light red
        }
        
        private Color GetAccuracyBarColor()
        {
            if (AccuracyRate > 80f) return Color.green;
            if (AccuracyRate > 60f) return Color.yellow;
            if (AccuracyRate > 40f) return new Color(1f, 0.65f, 0f);
            return Color.red;
        }
        
        private Color GetSpeedBarColor()
        {
            return new Color(0.4f, 1f, 0.4f); // Light green
        }
        
        private Color GetMoraleBarColor()
        {
            if (CurrentMorale > 75f) return Color.green;
            if (CurrentMorale > 50f) return Color.yellow;
            if (CurrentMorale > 25f) return new Color(1f, 0.65f, 0f);
            return Color.red;
        }
        
        private Color GetFearBarColor()
        {
            return new Color(0.8f, 0.2f, 0.8f); // Purple
        }
        
        private Color GetExperienceBarColor()
        {
            return new Color(0.3f, 0.7f, 1f); // Sky blue
        }
        
        private Color GetConditionBarColor()
        {
            return new Color(0.2f, 0.6f, 1f); // Blue
        }
        
        #endregion
    }
    
    #region Supporting Data Structures
    
    [System.Flags]
    public enum StatusType
    {
        None = 0,
        Poison = 1 << 0,
        Burn = 1 << 1,
        Freeze = 1 << 2,
        Stun = 1 << 3,
        Root = 1 << 4,
        Blessing = 1 << 5,
        Curse = 1 << 6,
        Shield = 1 << 7,
        Regeneration = 1 << 8,
        Vulnerable = 1 << 9
    }
    
    public enum ArmorType
    {
        None, Light, Medium, Heavy, Magical
    }
    
    public enum WeaponType  
    {
        Sword, Spear, Bow, Mace, Hammer
    }
    
    public enum WeatherType
    {
        Clear, Rain, Snow, Fog, Storm
    }
    
    public enum TerrainType
    {
        Plains, Forest, Hills, Mountains, Swamp, Desert
    }
    
    public enum TimeOfDay
    {
        Dawn, Day, Dusk, Night
    }
    
    #endregion
}