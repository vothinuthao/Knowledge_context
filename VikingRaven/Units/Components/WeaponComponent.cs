using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;

namespace VikingRaven.Units.Components
{
    /// <summary>
    /// Weapon Component with specialization system, mastery progression, and unique weapon mechanics
    /// Phase 1 Enhancement: Comprehensive weapon management for medieval combat simulation
    /// </summary>
    public class WeaponComponent : BaseComponent
    {
        #region Weapon Configuration
        
        [TitleGroup("Weapon Configuration")]
        [InfoBox("Weapon system provides unique mechanics and progression for each weapon type.", InfoMessageType.Info)]
        
        [Tooltip("Primary weapon equipped by this unit")]
        [SerializeField, EnumPaging] private WeaponType _primaryWeapon = WeaponType.Sword;
        
        [Tooltip("Secondary weapon (if dual-wielding or backup)")]
        [SerializeField, EnumPaging] private WeaponType _secondaryWeapon = WeaponType.None;
        
        [Tooltip("Enable dual-wielding mechanics")]
        [SerializeField, ToggleLeft] private bool _isDualWielding = false;
        
        [Tooltip("Allow weapon switching during combat")]
        [SerializeField, ToggleLeft] private bool _allowWeaponSwitching = true;

        #endregion

        #region Weapon Mastery System
        
        [TitleGroup("Weapon Mastery System")]
        [InfoBox("Units gain experience and unlock abilities through weapon usage.", InfoMessageType.Warning)]
        
        [Tooltip("Weapon mastery levels for different weapon types")]
        [SerializeField, DictionaryDrawerSettings(KeyLabel = "Weapon Type", ValueLabel = "Mastery Level")]
        private Dictionary<WeaponType, WeaponMastery> _weaponMasteries = new Dictionary<WeaponType, WeaponMastery>();
        
        [Tooltip("Experience gain rate multiplier")]
        [SerializeField, Range(0.1f, 5f)] private float _experienceGainMultiplier = 1f;
        
        [ShowInInspector, ReadOnly]
        private WeaponMastery CurrentWeaponMastery => GetWeaponMastery(_primaryWeapon);

        #endregion

        #region Weapon Condition and Durability
        
        [TitleGroup("Weapon Condition")]
        [InfoBox("Weapon condition affects damage output and special abilities.", InfoMessageType.Info)]
        
        [Tooltip("Primary weapon condition (0-100%)")]
        [SerializeField, Range(0f, 100f), ProgressBar(0, 100, ColorGetter = "GetWeaponConditionColor")]
        private float _primaryWeaponCondition = 100f;
        
        [Tooltip("Secondary weapon condition (0-100%)")]
        [SerializeField, Range(0f, 100f), ProgressBar(0, 100, ColorGetter = "GetSecondaryConditionColor"), ShowIf("HasSecondaryWeapon")]
        private float _secondaryWeaponCondition = 100f;
        
        [Tooltip("Weapon degradation rate per use")]
        [SerializeField, Range(0.1f, 5f)] private float _durabilityLossRate = 1f;
        
        [Tooltip("Minimum condition for effective combat")]
        [SerializeField, Range(10f, 50f)] private float _minimumEffectiveCondition = 25f;

        #endregion

        #region Weapon Statistics
        
        [TitleGroup("Weapon Statistics")]
        [InfoBox("Enhanced weapon stats based on type, mastery, and condition.", InfoMessageType.None)]
        
        [ShowInInspector, ReadOnly] private float _effectiveDamage;
        [ShowInInspector, ReadOnly] private float _effectiveRange;
        [ShowInInspector, ReadOnly] private float _effectiveSpeed;
        [ShowInInspector, ReadOnly] private float _criticalChance;
        [ShowInInspector, ReadOnly] private float _armorPenetration;
        
        // Combat tracking
        private int _totalHits = 0;
        private int _totalCriticalHits = 0;
        private float _totalDamageDealt = 0f;
        private int _enemiesKilled = 0;

        #endregion

        #region Weapon Abilities
        
        [TitleGroup("Weapon Abilities")]
        [InfoBox("Special abilities unlocked through weapon mastery and combat experience.", InfoMessageType.Warning)]
        
        [Tooltip("Available weapon-specific abilities")]
        [SerializeField, ReadOnly] private List<WeaponAbility> _unlockedAbilities = new List<WeaponAbility>();
        
        [Tooltip("Cooldown for weapon abilities")]
        [SerializeField, ReadOnly] private Dictionary<WeaponAbilityType, float> _abilityCooldowns = new Dictionary<WeaponAbilityType, float>();

        #endregion

        #region Public Properties

        // Weapon Configuration
        public WeaponType PrimaryWeapon => _primaryWeapon;
        public WeaponType SecondaryWeapon => _secondaryWeapon;
        public bool IsDualWielding => _isDualWielding && _secondaryWeapon != WeaponType.None;
        public bool AllowWeaponSwitching => _allowWeaponSwitching;
        
        // Weapon Condition
        public float PrimaryWeaponCondition => _primaryWeaponCondition;
        public float SecondaryWeaponCondition => _secondaryWeaponCondition;
        public bool IsPrimaryWeaponBroken => _primaryWeaponCondition < _minimumEffectiveCondition;
        public bool IsSecondaryWeaponBroken => _secondaryWeaponCondition < _minimumEffectiveCondition;
        
        // Weapon Mastery
        public WeaponMastery GetWeaponMastery(WeaponType weaponType) => 
            _weaponMasteries.TryGetValue(weaponType, out var mastery) ? mastery : new WeaponMastery();
        
        // Combat Statistics
        public int TotalHits => _totalHits;
        public int TotalCriticalHits => _totalCriticalHits;
        public float TotalDamageDealt => _totalDamageDealt;
        public int EnemiesKilled => _enemiesKilled;
        public float CriticalHitRate => _totalHits > 0 ? (float)_totalCriticalHits / _totalHits : 0f;
        
        // Effective Stats
        public float EffectiveDamage => _effectiveDamage;
        public float EffectiveRange => _effectiveRange;
        public float EffectiveSpeed => _effectiveSpeed;
        public float CriticalChance => _criticalChance;
        public float ArmorPenetration => _armorPenetration;
        
        // Abilities
        public IReadOnlyList<WeaponAbility> UnlockedAbilities => _unlockedAbilities;

        #endregion

        #region Events

        public event Action<WeaponType> OnWeaponChanged;
        public event Action<WeaponType, int> OnMasteryLevelUp;
        public event Action<WeaponAbility> OnAbilityUnlocked;
        public event Action<WeaponAbilityType> OnAbilityUsed;
        public event Action<WeaponType, float> OnWeaponDamaged;
        public event Action<WeaponType> OnWeaponBroken;
        public event Action<WeaponType> OnWeaponRepaired;

        #endregion

        #region Unity Lifecycle

        public override void Initialize()
        {
            base.Initialize();
            LoadWeaponDataFromUnit();
            InitializeWeaponMasteries();
            InitializeWeaponAbilities();
            UpdateWeaponStatistics();
        }

        private void Update()
        {
            if (!IsActive) return;
            
            UpdateAbilityCooldowns();
            UpdateWeaponStatistics();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Load weapon configuration from unit data
        /// </summary>
        private void LoadWeaponDataFromUnit()
        {
            if (Entity == null) return;
            
            // Get UnitModel from UnitFactory to access UnitDataSO
            var unitFactory = FindObjectOfType<VikingRaven.Core.Factory.UnitFactory>();
            if (unitFactory != null)
            {
                var unitModel = unitFactory.GetUnitModel(Entity);
                if (unitModel != null)
                {
                    InitializeWeaponFromUnitType(unitModel.UnitType);
                    Debug.Log($"WeaponComponent: Loaded weapon data for {unitModel.DisplayName}");
                }
            }
        }

        /// <summary>
        /// Initialize weapon configuration based on unit type
        /// </summary>
        private void InitializeWeaponFromUnitType(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Infantry:
                    _primaryWeapon = WeaponType.Sword;
                    _secondaryWeapon = WeaponType.None;
                    _isDualWielding = false;
                    break;
                    
                case UnitType.Archer:
                    _primaryWeapon = WeaponType.Bow;
                    _secondaryWeapon = WeaponType.Dagger;
                    _isDualWielding = false;
                    break;
                    
                case UnitType.Pike:
                    _primaryWeapon = WeaponType.Spear;
                    _secondaryWeapon = WeaponType.None;
                    _isDualWielding = false;
                    break;
                    
                default:
                    _primaryWeapon = WeaponType.Sword;
                    _secondaryWeapon = WeaponType.None;
                    break;
            }
            
            // Initialize weapon conditions
            _primaryWeaponCondition = 100f;
            _secondaryWeaponCondition = 100f;
        }

        /// <summary>
        /// Initialize weapon mastery system
        /// </summary>
        private void InitializeWeaponMasteries()
        {
            // Initialize mastery for primary weapon
            if (!_weaponMasteries.ContainsKey(_primaryWeapon))
            {
                _weaponMasteries[_primaryWeapon] = new WeaponMastery();
            }
            
            // Initialize mastery for secondary weapon
            if (_secondaryWeapon != WeaponType.None && !_weaponMasteries.ContainsKey(_secondaryWeapon))
            {
                _weaponMasteries[_secondaryWeapon] = new WeaponMastery();
            }
            
            Debug.Log($"WeaponComponent: Initialized weapon masteries for {Entity.Id}");
        }

        /// <summary>
        /// Initialize weapon abilities based on current mastery
        /// </summary>
        private void InitializeWeaponAbilities()
        {
            _unlockedAbilities.Clear();
            _abilityCooldowns.Clear();
            
            // Unlock abilities based on current mastery level
            var currentMastery = GetWeaponMastery(_primaryWeapon);
            UnlockAbilitiesForMasteryLevel(_primaryWeapon, currentMastery.Level);
        }

        #endregion

        #region Weapon Management

        /// <summary>
        /// Switch primary weapon
        /// </summary>
        public bool SwitchPrimaryWeapon(WeaponType newWeapon)
        {
            if (!_allowWeaponSwitching || newWeapon == _primaryWeapon) return false;
            
            WeaponType oldWeapon = _primaryWeapon;
            _primaryWeapon = newWeapon;
            
            // Initialize mastery if new weapon
            if (!_weaponMasteries.ContainsKey(newWeapon))
            {
                _weaponMasteries[newWeapon] = new WeaponMastery();
            }
            
            // Update abilities for new weapon
            UpdateWeaponAbilities();
            UpdateWeaponStatistics();
            
            // Update CombatComponent if present
            var combatComponent = Entity.GetComponent<CombatComponent>();
            if (combatComponent != null)
            {
                combatComponent.ChangeWeapon(newWeapon);
            }
            
            OnWeaponChanged?.Invoke(newWeapon);
            
            Debug.Log($"WeaponComponent: Switched from {oldWeapon} to {newWeapon}");
            return true;
        }

        /// <summary>
        /// Set secondary weapon
        /// </summary>
        public void SetSecondaryWeapon(WeaponType weapon)
        {
            _secondaryWeapon = weapon;
            
            if (weapon != WeaponType.None && !_weaponMasteries.ContainsKey(weapon))
            {
                _weaponMasteries[weapon] = new WeaponMastery();
            }
            
            UpdateWeaponStatistics();
        }

        /// <summary>
        /// Toggle dual wielding
        /// </summary>
        public bool ToggleDualWielding()
        {
            if (_secondaryWeapon == WeaponType.None) return false;
            
            _isDualWielding = !_isDualWielding;
            UpdateWeaponStatistics();
            
            Debug.Log($"WeaponComponent: Dual wielding {(_isDualWielding ? "enabled" : "disabled")}");
            return true;
        }

        #endregion

        #region Weapon Condition System

        /// <summary>
        /// Damage weapon condition from use
        /// </summary>
        public void DamageWeaponFromUse(WeaponType weaponType, float usageIntensity = 1f)
        {
            float durabilityLoss = _durabilityLossRate * usageIntensity;
            
            if (weaponType == _primaryWeapon)
            {
                float oldCondition = _primaryWeaponCondition;
                _primaryWeaponCondition = Mathf.Max(0f, _primaryWeaponCondition - durabilityLoss);
                
                if (oldCondition >= _minimumEffectiveCondition && _primaryWeaponCondition < _minimumEffectiveCondition)
                {
                    OnWeaponBroken?.Invoke(_primaryWeapon);
                    Debug.Log($"WeaponComponent: Primary weapon {_primaryWeapon} is broken!");
                }
                
                OnWeaponDamaged?.Invoke(_primaryWeapon, durabilityLoss);
            }
            else if (weaponType == _secondaryWeapon)
            {
                float oldCondition = _secondaryWeaponCondition;
                _secondaryWeaponCondition = Mathf.Max(0f, _secondaryWeaponCondition - durabilityLoss);
                
                if (oldCondition >= _minimumEffectiveCondition && _secondaryWeaponCondition < _minimumEffectiveCondition)
                {
                    OnWeaponBroken?.Invoke(_secondaryWeapon);
                    Debug.Log($"WeaponComponent: Secondary weapon {_secondaryWeapon} is broken!");
                }
                
                OnWeaponDamaged?.Invoke(_secondaryWeapon, durabilityLoss);
            }
            
            UpdateWeaponStatistics();
        }

        /// <summary>
        /// Repair weapon condition
        /// </summary>
        public void RepairWeapon(WeaponType weaponType, float repairAmount)
        {
            if (weaponType == _primaryWeapon)
            {
                _primaryWeaponCondition = Mathf.Min(100f, _primaryWeaponCondition + repairAmount);
                OnWeaponRepaired?.Invoke(_primaryWeapon);
            }
            else if (weaponType == _secondaryWeapon)
            {
                _secondaryWeaponCondition = Mathf.Min(100f, _secondaryWeaponCondition + repairAmount);
                OnWeaponRepaired?.Invoke(_secondaryWeapon);
            }
            
            UpdateWeaponStatistics();
        }

        /// <summary>
        /// Get weapon condition modifier for damage calculations
        /// </summary>
        public float GetWeaponConditionModifier(WeaponType weaponType)
        {
            float condition = weaponType == _primaryWeapon ? _primaryWeaponCondition : _secondaryWeaponCondition;
            
            if (condition < _minimumEffectiveCondition)
            {
                return 0.1f; // Broken weapon severely reduces effectiveness
            }
            
            return Mathf.Lerp(0.5f, 1f, condition / 100f);
        }

        #endregion

        #region Mastery and Experience System

        /// <summary>
        /// Gain weapon experience from combat
        /// </summary>
        public void GainWeaponExperience(WeaponType weaponType, float experience)
        {
            if (!_weaponMasteries.TryGetValue(weaponType, out var mastery)) return;
            
            float modifiedExperience = experience * _experienceGainMultiplier;
            int oldLevel = mastery.Level;
            
            mastery.AddExperience(modifiedExperience);
            
            // Check for level up
            if (mastery.Level > oldLevel)
            {
                OnMasteryLevelUp?.Invoke(weaponType, mastery.Level);
                UnlockAbilitiesForMasteryLevel(weaponType, mastery.Level);
                
                Debug.Log($"WeaponComponent: {weaponType} mastery increased to level {mastery.Level}!");
            }
            
            UpdateWeaponStatistics();
        }

        /// <summary>
        /// Process successful hit for experience and mastery
        /// </summary>
        public void ProcessSuccessfulHit(WeaponType weaponType, float damage, bool isCritical)
        {
            _totalHits++;
            _totalDamageDealt += damage;
            
            if (isCritical)
            {
                _totalCriticalHits++;
            }
            
            // Gain experience based on damage dealt
            float experience = damage * 0.1f + (isCritical ? 5f : 1f);
            GainWeaponExperience(weaponType, experience);
            
            // Damage weapon from use
            DamageWeaponFromUse(weaponType, isCritical ? 1.5f : 1f);
        }

        /// <summary>
        /// Process enemy kill for bonus experience
        /// </summary>
        public void ProcessEnemyKill(WeaponType weaponType)
        {
            _enemiesKilled++;
            
            // Bonus experience for kills
            float killExperience = 10f;
            GainWeaponExperience(weaponType, killExperience);
        }

        #endregion

        #region Weapon Abilities System

        /// <summary>
        /// Unlock abilities for specific mastery level
        /// </summary>
        private void UnlockAbilitiesForMasteryLevel(WeaponType weaponType, int masteryLevel)
        {
            var newAbilities = GetAbilitiesForWeaponAndLevel(weaponType, masteryLevel);
            
            foreach (var ability in newAbilities)
            {
                if (!_unlockedAbilities.Contains(ability))
                {
                    _unlockedAbilities.Add(ability);
                    OnAbilityUnlocked?.Invoke(ability);
                    
                    Debug.Log($"WeaponComponent: Unlocked ability {ability.Type} for {weaponType}!");
                }
            }
        }

        /// <summary>
        /// Get abilities available for weapon type and mastery level
        /// </summary>
        private List<WeaponAbility> GetAbilitiesForWeaponAndLevel(WeaponType weaponType, int masteryLevel)
        {
            var abilities = new List<WeaponAbility>();
            
            switch (weaponType)
            {
                case WeaponType.Sword:
                    if (masteryLevel >= 2) abilities.Add(new WeaponAbility(WeaponAbilityType.PowerStrike, 10f, "Increased damage strike"));
                    if (masteryLevel >= 4) abilities.Add(new WeaponAbility(WeaponAbilityType.Parry, 8f, "Defensive counter ability"));
                    if (masteryLevel >= 6) abilities.Add(new WeaponAbility(WeaponAbilityType.FlurryAttack, 15f, "Multiple quick strikes"));
                    break;
                    
                case WeaponType.Spear:
                    if (masteryLevel >= 2) abilities.Add(new WeaponAbility(WeaponAbilityType.ThrustAttack, 12f, "Extended reach thrust"));
                    if (masteryLevel >= 4) abilities.Add(new WeaponAbility(WeaponAbilityType.SweepAttack, 10f, "Area sweep attack"));
                    if (masteryLevel >= 6) abilities.Add(new WeaponAbility(WeaponAbilityType.ChargeAttack, 18f, "Devastating charge attack"));
                    break;
                    
                case WeaponType.Bow:
                    if (masteryLevel >= 2) abilities.Add(new WeaponAbility(WeaponAbilityType.AimedShot, 8f, "High accuracy shot"));
                    if (masteryLevel >= 4) abilities.Add(new WeaponAbility(WeaponAbilityType.MultiShot, 12f, "Fire multiple arrows"));
                    if (masteryLevel >= 6) abilities.Add(new WeaponAbility(WeaponAbilityType.PiercingShot, 15f, "Armor-piercing shot"));
                    break;
                    
                case WeaponType.Mace:
                    if (masteryLevel >= 2) abilities.Add(new WeaponAbility(WeaponAbilityType.CrushingBlow, 12f, "Armor-crushing attack"));
                    if (masteryLevel >= 4) abilities.Add(new WeaponAbility(WeaponAbilityType.StunStrike, 10f, "Stunning attack"));
                    if (masteryLevel >= 6) abilities.Add(new WeaponAbility(WeaponAbilityType.ShieldBreaker, 16f, "Shield-destroying attack"));
                    break;
            }
            
            return abilities;
        }

        /// <summary>
        /// Use weapon ability
        /// </summary>
        public bool UseWeaponAbility(WeaponAbilityType abilityType)
        {
            var ability = _unlockedAbilities.Find(a => a.Type == abilityType);
            if (ability == null) return false;
            
            // Check cooldown
            if (_abilityCooldowns.TryGetValue(abilityType, out float remainingCooldown) && remainingCooldown > 0f)
            {
                return false;
            }
            
            // Use ability
            _abilityCooldowns[abilityType] = ability.Cooldown;
            OnAbilityUsed?.Invoke(abilityType);
            
            Debug.Log($"WeaponComponent: Used ability {abilityType}");
            return true;
        }

        /// <summary>
        /// Update weapon abilities based on current weapon
        /// </summary>
        private void UpdateWeaponAbilities()
        {
            var currentMastery = GetWeaponMastery(_primaryWeapon);
            
            // Clear current abilities
            _unlockedAbilities.Clear();
            
            // Unlock abilities for current weapon
            UnlockAbilitiesForMasteryLevel(_primaryWeapon, currentMastery.Level);
        }

        /// <summary>
        /// Update ability cooldowns
        /// </summary>
        private void UpdateAbilityCooldowns()
        {
            var keys = new List<WeaponAbilityType>(_abilityCooldowns.Keys);
            
            foreach (var key in keys)
            {
                if (_abilityCooldowns[key] > 0f)
                {
                    _abilityCooldowns[key] -= Time.deltaTime;
                    if (_abilityCooldowns[key] <= 0f)
                    {
                        _abilityCooldowns[key] = 0f;
                    }
                }
            }
        }

        #endregion

        #region Statistics Calculation

        /// <summary>
        /// Update weapon statistics based on mastery, condition, and type
        /// </summary>
        private void UpdateWeaponStatistics()
        {
            var baseCombat = Entity.GetComponent<CombatComponent>();
            if (baseCombat == null) return;
            
            float baseDamage = baseCombat.AttackDamage;
            float baseRange = baseCombat.AttackRange;
            float baseSpeed = 1f / baseCombat.AttackCooldown;
            
            var weaponMods = GetWeaponTypeModifiers(_primaryWeapon);
            var masteryMods = GetMasteryModifiers(_primaryWeapon);
            var conditionMod = GetWeaponConditionModifier(_primaryWeapon);
            
            // Calculate effective stats
            _effectiveDamage = baseDamage * weaponMods.DamageMultiplier * masteryMods.DamageMultiplier * conditionMod;
            _effectiveRange = baseRange * weaponMods.RangeMultiplier;
            _effectiveSpeed = baseSpeed * weaponMods.SpeedMultiplier * masteryMods.SpeedMultiplier;
            _criticalChance = weaponMods.CriticalChance + masteryMods.CriticalChance;
            _armorPenetration = weaponMods.ArmorPenetration + masteryMods.ArmorPenetration;
            
            // Dual wielding bonuse
            if (IsDualWielding)
            {
                _effectiveSpeed *= 1.3f;
                _effectiveDamage *= 0.8f;
            }
        }

        /// <summary>
        /// Get weapon type modifiers
        /// </summary>
        private WeaponModifiers GetWeaponTypeModifiers(WeaponType weaponType)
        {
            return weaponType switch
            {
                WeaponType.Sword => new WeaponModifiers(1f, 1f, 1f, 5f, 5f),
                WeaponType.Spear => new WeaponModifiers(1.2f, 1.5f, 0.8f, 3f, 10f),
                WeaponType.Bow => new WeaponModifiers(0.8f, 3f, 0.7f, 8f, 15f),
                WeaponType.Mace => new WeaponModifiers(1.3f, 0.8f, 0.7f, 4f, 20f),
                WeaponType.Hammer => new WeaponModifiers(1.5f, 0.7f, 0.6f, 6f, 25f),
                WeaponType.Dagger => new WeaponModifiers(0.6f, 0.8f, 1.5f, 12f, 2f),
                _ => new WeaponModifiers(1f, 1f, 1f, 5f, 5f)
            };
        }

        /// <summary>
        /// Get mastery modifiers for weapon
        /// </summary>
        private WeaponModifiers GetMasteryModifiers(WeaponType weaponType)
        {
            var mastery = GetWeaponMastery(weaponType);
            float levelBonus = mastery.Level * 0.05f; // 5% per level
            
            return new WeaponModifiers(
                1f + levelBonus,           // Damage multiplier
                1f,                        // Range stays same
                1f + levelBonus * 0.5f,    // Speed bonus
                mastery.Level * 1f,        // Critical chance bonus
                mastery.Level * 2f         // Armor penetration bonus
            );
        }

        #endregion

        #region Helper Methods

        private bool HasSecondaryWeapon => _secondaryWeapon != WeaponType.None;
        private Color GetWeaponConditionColor => Color.Lerp(Color.red, Color.green, _primaryWeaponCondition / 100f);
        private Color GetSecondaryConditionColor => Color.Lerp(Color.red, Color.green, _secondaryWeaponCondition / 100f);

        #endregion

        #region Debug Methods

        [Button("Test Weapon Switch"), FoldoutGroup("Debug Tools")]
        private void TestWeaponSwitch()
        {
            var nextWeapon = (WeaponType)(((int)_primaryWeapon + 1) % System.Enum.GetValues(typeof(WeaponType)).Length);
            SwitchPrimaryWeapon(nextWeapon);
        }

        [Button("Add Weapon Experience"), FoldoutGroup("Debug Tools")]
        private void AddWeaponExperience()
        {
            GainWeaponExperience(_primaryWeapon, 50f);
        }

        [Button("Damage Weapon"), FoldoutGroup("Debug Tools")]
        private void DamageWeapon()
        {
            DamageWeaponFromUse(_primaryWeapon, 5f);
        }

        [Button("Show Weapon Stats"), FoldoutGroup("Debug Tools")]
        private void ShowWeaponStats()
        {
            Debug.Log($"=== Weapon Statistics ===");
            Debug.Log($"Primary Weapon: {_primaryWeapon}");
            Debug.Log($"Effective Damage: {_effectiveDamage:F1}");
            Debug.Log($"Effective Range: {_effectiveRange:F1}");
            Debug.Log($"Attack Speed: {_effectiveSpeed:F2}/sec");
            Debug.Log($"Critical Chance: {_criticalChance:F1}%");
            Debug.Log($"Armor Penetration: {_armorPenetration:F1}");
            Debug.Log($"Weapon Condition: {_primaryWeaponCondition:F1}%");
            
            var mastery = GetWeaponMastery(_primaryWeapon);
            Debug.Log($"Mastery Level: {mastery.Level} (XP: {mastery.Experience:F0}/{mastery.ExperienceToNextLevel:F0})");
        }

        #endregion
    }

    #region Supporting Data Structures

    /// <summary>
    /// Weapon mastery progression system
    /// </summary>
    [Serializable]
    public class WeaponMastery
    {
        [SerializeField] private int _level = 1;
        [SerializeField] private float _experience = 0f;
        
        public int Level => _level;
        public float Experience => _experience;
        public float ExperienceToNextLevel => CalculateExperienceRequired(_level + 1) - _experience;
        public float ProgressToNextLevel => _experience / CalculateExperienceRequired(_level + 1);
        
        public void AddExperience(float amount)
        {
            _experience += amount;
            
            // Check for level ups
            while (_experience >= CalculateExperienceRequired(_level + 1) && _level < 10)
            {
                _level++;
            }
        }
        
        private float CalculateExperienceRequired(int level)
        {
            return level * level * 100f; // Exponential experience requirements
        }
    }

    /// <summary>
    /// Weapon ability definition
    /// </summary>
    [Serializable]
    public class WeaponAbility
    {
        public WeaponAbilityType Type;
        public float Cooldown;
        public string Description;
        
        public WeaponAbility(WeaponAbilityType type, float cooldown, string description)
        {
            Type = type;
            Cooldown = cooldown;
            Description = description;
        }
    }

    /// <summary>
    /// Weapon modifiers for calculations
    /// </summary>
    public struct WeaponModifiers
    {
        public float DamageMultiplier;
        public float RangeMultiplier;
        public float SpeedMultiplier;
        public float CriticalChance;
        public float ArmorPenetration;
        
        public WeaponModifiers(float damage, float range, float speed, float crit, float penetration)
        {
            DamageMultiplier = damage;
            RangeMultiplier = range;
            SpeedMultiplier = speed;
            CriticalChance = crit;
            ArmorPenetration = penetration;
        }
    }

    /// <summary>
    /// Weapon ability types
    /// </summary>
    public enum WeaponAbilityType
    {
        // Sword abilities
        PowerStrike,
        Parry,
        FlurryAttack,
        
        // Spear abilities
        ThrustAttack,
        SweepAttack,
        ChargeAttack,
        
        // Bow abilities
        AimedShot,
        MultiShot,
        PiercingShot,
        
        // Mace abilities
        CrushingBlow,
        StunStrike,
        ShieldBreaker
    }

    #endregion
}