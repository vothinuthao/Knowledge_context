using System;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;
using VikingRaven.Core.StateMachine;
using VikingRaven.Units.Data;

namespace VikingRaven.Units.Components
{
    /// <summary>
    /// Enhanced Combat Component with damage types, armor system, and weapon specialization
    /// Phase 1 Enhancement: Comprehensive combat mechanics foundation
    /// </summary>
    public class CombatComponent : BaseComponent
    {
        #region Combat Stats Configuration

        [TitleGroup("Basic Combat Stats")]
        [InfoBox("Basic combat statistics loaded from UnitDataSO. These are base values before modifiers.",
            InfoMessageType.Info)]
        [SerializeField, ReadOnly]
        private float _attackDamage = 10f;

        [SerializeField, ReadOnly] private float _attackRange = 2f;
        [SerializeField, ReadOnly] private float _attackCooldown = 1.5f;
        [SerializeField, ReadOnly] private float _moveSpeed = 3.0f;

        [TitleGroup("Enhanced Combat Mechanics")]
        [InfoBox("Enhanced combat features for realistic medieval warfare simulation.", InfoMessageType.Warning)]
        [Tooltip("Primary damage type for this unit's attacks")]
        [SerializeField, EnumToggleButtons]
        private DamageType _primaryDamageType = DamageType.Physical;

        [Tooltip("Secondary damage type (if applicable)")] [SerializeField, EnumToggleButtons]
        private DamageType _secondaryDamageType = DamageType.None;

        [Tooltip("Percentage of secondary damage (0-100%)")]
        [SerializeField, Range(0f, 100f), ShowIf("HasSecondaryDamage")]
        private float _secondaryDamagePercentage = 0f;

        #endregion

        #region Armor and Defense System

        [TitleGroup("Armor and Defense")]
        [InfoBox("Armor system provides realistic protection against different damage types.", InfoMessageType.Info)]
        [Tooltip("Physical armor rating (reduces physical damage)")]
        [SerializeField, Range(0f, 100f), ProgressBar(0, 100, ColorGetter = "GetArmorColor")]
        private float _physicalArmor = 0f;

        [Tooltip("Magical resistance rating (reduces magical damage)")]
        [SerializeField, Range(0f, 100f), ProgressBar(0, 100, ColorGetter = "GetMagicResistanceColor")]
        private float _magicalResistance = 0f;

        [Tooltip("Armor condition (0-100%, affects effectiveness)")]
        [SerializeField, Range(0f, 100f), ProgressBar(0, 100, ColorGetter = "GetArmorConditionColor")]
        private float _armorCondition = 100f;

        [Tooltip("Armor penetration capability of attacks")] [SerializeField, Range(0f, 50f)]
        private float _armorPenetration = 0f;

        #endregion

        #region Weapon System Integration

        [TitleGroup("Weapon Integration")]
        [InfoBox("Weapon system determines combat behavior and effectiveness.", InfoMessageType.Info)]
        [Tooltip("Current weapon type equipped")]
        [SerializeField, EnumToggleButtons]
        private WeaponType _equippedWeaponType = WeaponType.Sword;

        [Tooltip("Weapon reach modifier (affects attack range)")] [SerializeField, Range(0.5f, 3f)]
        private float _weaponReachModifier = 1f;

        [Tooltip("Weapon speed modifier (affects attack cooldown)")] [SerializeField, Range(0.5f, 2f)]
        private float _weaponSpeedModifier = 1f;

        #endregion

        #region Combat State Tracking

        [TitleGroup("Combat State Tracking")]
        [InfoBox("Real-time combat state information for AI and gameplay systems.", InfoMessageType.None)]
        [ShowInInspector, ReadOnly]
        private bool _isInCombat = false;

        [ShowInInspector, ReadOnly] private float _lastAttackTime = -100f;
        [ShowInInspector, ReadOnly] private float _lastDamageTakenTime = -100f;
        [ShowInInspector, ReadOnly] private int _consecutiveHits = 0;
        [ShowInInspector, ReadOnly] private int _consecutiveMisses = 0;

        // Enhanced tracking variables
        private float _totalDamageDealt = 0f;
        private float _totalDamageReceived = 0f;
        private int _killCount = 0;
        private float _combatEfficiency = 1f;
        private bool _isStaggered = false;
        private float _staggerEndTime = 0f;

        #endregion

        #region Calculated Properties

        [TitleGroup("Combat Statistics")]
        [ShowInInspector, ReadOnly, ProgressBar(0, 100)]
        private float CombatEffectiveness => CalculateCombatEffectiveness();

        [ShowInInspector, ReadOnly] public float EffectiveAttackDamage => CalculateEffectiveAttackDamage();

        [ShowInInspector, ReadOnly] private float EffectiveAttackRange => CalculateEffectiveAttackRange();

        [ShowInInspector, ReadOnly] private float EffectiveAttackSpeed => CalculateEffectiveAttackSpeed();

        #endregion

        #region Public Properties

        // Basic Combat Stats (from UnitDataSO)
        public float AttackDamage => _attackDamage;
        public float AttackRange => _attackRange;
        public float AttackCooldown => _attackCooldown;
        public float MoveSpeed => _moveSpeed;

        // Enhanced Combat Properties
        public DamageType PrimaryDamageType => _primaryDamageType;
        public DamageType SecondaryDamageType => _secondaryDamageType;
        public float SecondaryDamagePercentage => _secondaryDamagePercentage;

        // Armor and Defense
        public float PhysicalArmor => _physicalArmor;
        public float MagicalResistance => _magicalResistance;
        public float ArmorCondition => _armorCondition;
        public float ArmorPenetration => _armorPenetration;

        // Weapon System
        public WeaponType EquippedWeaponType => _equippedWeaponType;
        public float WeaponReachModifier => _weaponReachModifier;
        public float WeaponSpeedModifier => _weaponSpeedModifier;

        // Combat State
        public bool IsInCombat => _isInCombat;
        public bool IsStaggered => _isStaggered && Time.time < _staggerEndTime;
        public float TimeSinceLastAttack => Time.time - _lastAttackTime;
        public float TimeSinceLastDamage => Time.time - _lastDamageTakenTime;
        public int ConsecutiveHits => _consecutiveHits;
        public int ConsecutiveMisses => _consecutiveMisses;
        public float TotalDamageDealt => _totalDamageDealt;
        public float TotalDamageReceived => _totalDamageReceived;
        public int KillCount => _killCount;
        public float CombatEfficiency => _combatEfficiency;

        #endregion

        #region Events

        public event Action<IEntity, DamageInfo> OnDamageDealt;
        public event Action<DamageInfo> OnDamageReceived;
        public event Action<IEntity> OnEnemyKilled;
        public event Action<bool> OnCombatStateChanged;
        public event Action<float> OnArmorDamaged;
        public event Action<WeaponType> OnWeaponChanged;

        #endregion

        #region Unity Lifecycle

        public override void Initialize()
        {
            base.Initialize();
            LoadStatsFromUnitData();
            InitializeEnhancedCombatStats();
        }

        private void Update()
        {
            if (!IsActive) return;

            UpdateCombatState();
            UpdateStaggerState();
            UpdateCombatEfficiency();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Load basic combat stats from UnitDataSO via UnitModel
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
                    _attackDamage = unitModel.Damage;
                    _attackRange = unitModel.Range;
                    _attackCooldown = unitModel.HitSpeed;
                    _moveSpeed = unitModel.MoveSpeed;

                    // Initialize enhanced stats based on unit type
                    InitializeStatsFromUnitType(unitModel.UnitType);

                    Debug.Log($"CombatComponent: Loaded stats from UnitModel for {unitModel.DisplayName}");
                }
            }
        }

        /// <summary>
        /// Initialize enhanced combat stats based on unit type
        /// </summary>
        private void InitializeStatsFromUnitType(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Infantry:
                    _primaryDamageType = DamageType.Physical;
                    _equippedWeaponType = WeaponType.Sword;
                    _physicalArmor = 15f;
                    _weaponReachModifier = 1f;
                    _weaponSpeedModifier = 1f;
                    break;

                case UnitType.Archer:
                    _primaryDamageType = DamageType.Piercing;
                    _equippedWeaponType = WeaponType.Bow;
                    _physicalArmor = 5f;
                    _armorPenetration = 10f;
                    _weaponReachModifier = 3f; // Longer range
                    _weaponSpeedModifier = 0.8f; // Slower attacks
                    break;

                case UnitType.Pike:
                    _primaryDamageType = DamageType.Piercing;
                    _equippedWeaponType = WeaponType.Spear;
                    _physicalArmor = 20f;
                    _armorPenetration = 15f;
                    _weaponReachModifier = 2f; // Extended reach
                    _weaponSpeedModifier = 0.7f; // Slower but powerful
                    break;

                default:
                    _primaryDamageType = DamageType.Physical;
                    _equippedWeaponType = WeaponType.Sword;
                    break;
            }
        }

        /// <summary>
        /// Initialize enhanced combat statistics
        /// </summary>
        private void InitializeEnhancedCombatStats()
        {
            _armorCondition = 100f;
            _combatEfficiency = 1f;
            _isInCombat = false;
            _isStaggered = false;

            Debug.Log($"CombatComponent: Enhanced combat stats initialized for {Entity.Id}");
        }

        #endregion

        #region Enhanced Combat Methods

        /// <summary>
        /// Perform enhanced attack with damage types and armor consideration
        /// </summary>
        public bool PerformEnhancedAttack(IEntity target)
        {
            if (!CanAttack() || target == null || IsStaggered) return false;

            _lastAttackTime = Time.time;

            // Calculate damage with all modifiers
            DamageInfo damageInfo = CalculateDamageInfo(target);

            // Apply damage to target
            var targetCombat = target.GetComponent<CombatComponent>();
            if (targetCombat != null)
            {
                bool hitSuccessful = targetCombat.ReceiveEnhancedDamage(damageInfo, Entity);

                if (hitSuccessful)
                {
                    _consecutiveHits++;
                    _consecutiveMisses = 0;
                    _totalDamageDealt += damageInfo.FinalDamage;

                    // Check if target died
                    var targetHealth = target.GetComponent<HealthComponent>();
                    if (targetHealth != null && targetHealth.IsDead)
                    {
                        _killCount++;
                        OnEnemyKilled?.Invoke(target);
                    }

                    OnDamageDealt?.Invoke(target, damageInfo);
                }
                else
                {
                    _consecutiveMisses++;
                    _consecutiveHits = 0;
                }

                return hitSuccessful;
            }

            return false;
        }

        /// <summary>
        /// Receive enhanced damage with armor calculation
        /// </summary>
        public bool ReceiveEnhancedDamage(DamageInfo damageInfo, IEntity attacker)
        {
            if (!IsActive) return false;

            _lastDamageTakenTime = Time.time;

            // Calculate damage reduction based on armor and damage type
            float damageReduction = CalculateDamageReduction(damageInfo.DamageType, damageInfo.ArmorPenetration);
            float finalDamage = damageInfo.BaseDamage * (1f - damageReduction);

            // Update damage info with final values
            damageInfo.DamageReduction = damageReduction;
            damageInfo.FinalDamage = finalDamage;

            // Apply damage to health component
            var healthComponent = Entity.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                healthComponent.TakeDamage(finalDamage, attacker);
            }

            // Update armor condition
            DamageArmor(damageInfo.BaseDamage * 0.1f);

            // Check for stagger effect
            CheckForStagger(damageInfo);

            _totalDamageReceived += finalDamage;
            OnDamageReceived?.Invoke(damageInfo);

            // Enter combat state
            SetCombatState(true);

            return true;
        }

        #endregion

        #region Damage Calculation System

        /// <summary>
        /// Calculate comprehensive damage information
        /// </summary>
        private DamageInfo CalculateDamageInfo(IEntity target)
        {
            var damageInfo = new DamageInfo
            {
                Attacker = Entity,
                Target = target,
                DamageType = _primaryDamageType,
                BaseDamage = CalculateEffectiveAttackDamage(),
                ArmorPenetration = _armorPenetration,
                WeaponType = _equippedWeaponType,
                IsSecondaryDamage = false
            };

            return damageInfo;
        }

        /// <summary>
        /// Calculate damage reduction based on armor and damage type
        /// </summary>
        private float CalculateDamageReduction(DamageType damageType, float armorPenetration)
        {
            float effectiveArmor = 0f;

            switch (damageType)
            {
                case DamageType.Physical:
                case DamageType.Piercing:
                case DamageType.Slashing:
                    effectiveArmor = _physicalArmor;
                    break;
                case DamageType.Magical:
                case DamageType.Fire:
                case DamageType.Ice:
                    effectiveArmor = _magicalResistance;
                    break;
                case DamageType.True:
                    return 0f; // True damage ignores all armor
            }

            // Apply armor condition
            effectiveArmor *= (_armorCondition / 100f);

            // Apply armor penetration
            effectiveArmor = Mathf.Max(0f, effectiveArmor - armorPenetration);

            // Calculate damage reduction (diminishing returns)
            float reduction = effectiveArmor / (effectiveArmor + 100f);
            return Mathf.Clamp01(reduction);
        }

        #endregion

        #region Combat State Management

        /// <summary>
        /// Update combat state based on recent activity
        /// </summary>
        private void UpdateCombatState()
        {
            bool shouldBeInCombat = (Time.time - _lastAttackTime < 5f) || (Time.time - _lastDamageTakenTime < 5f);

            if (_isInCombat != shouldBeInCombat)
            {
                SetCombatState(shouldBeInCombat);
            }
        }

        /// <summary>
        /// Set combat state and trigger events
        /// </summary>
        private void SetCombatState(bool inCombat)
        {
            if (_isInCombat == inCombat) return;

            _isInCombat = inCombat;
            OnCombatStateChanged?.Invoke(_isInCombat);

            if (!_isInCombat)
            {
                // Reset combat streaks when leaving combat
                _consecutiveHits = 0;
                _consecutiveMisses = 0;
            }
        }

        /// <summary>
        /// Update stagger state
        /// </summary>
        private void UpdateStaggerState()
        {
            if (_isStaggered && Time.time >= _staggerEndTime)
            {
                _isStaggered = false;
            }
        }

        /// <summary>
        /// Check if attack should cause stagger
        /// </summary>
        private void CheckForStagger(DamageInfo damageInfo)
        {
            // Heavy damage or certain weapon types can cause stagger
            float staggerThreshold = 20f; // Base threshold

            if (damageInfo.FinalDamage > staggerThreshold ||
                damageInfo.WeaponType == WeaponType.Mace ||
                damageInfo.WeaponType == WeaponType.Hammer)
            {
                ApplyStagger(0.5f); // 0.5 second stagger
            }
        }

        /// <summary>
        /// Apply stagger effect
        /// </summary>
        public void ApplyStagger(float duration)
        {
            _isStaggered = true;
            _staggerEndTime = Time.time + duration;
        }

        #endregion

        #region Armor and Weapon Management

        /// <summary>
        /// Damage armor condition
        /// </summary>
        private void DamageArmor(float damage)
        {
            float armorDamage = damage * 0.1f; // 10% of damage affects armor
            _armorCondition = Mathf.Max(0f, _armorCondition - armorDamage);

            OnArmorDamaged?.Invoke(armorDamage);
        }

        /// <summary>
        /// Repair armor condition
        /// </summary>
        public void RepairArmor(float repairAmount)
        {
            _armorCondition = Mathf.Min(100f, _armorCondition + repairAmount);
        }

        /// <summary>
        /// Change equipped weapon type
        /// </summary>
        public void ChangeWeapon(WeaponType newWeaponType)
        {
            if (_equippedWeaponType == newWeaponType) return;

            _equippedWeaponType = newWeaponType;
            UpdateWeaponModifiers();
            OnWeaponChanged?.Invoke(newWeaponType);
        }

        /// <summary>
        /// Update weapon modifiers based on weapon type
        /// </summary>
        private void UpdateWeaponModifiers()
        {
            switch (_equippedWeaponType)
            {
                case WeaponType.Sword:
                    _weaponReachModifier = 1f;
                    _weaponSpeedModifier = 1f;
                    _primaryDamageType = DamageType.Slashing;
                    break;
                case WeaponType.Spear:
                    _weaponReachModifier = 2f;
                    _weaponSpeedModifier = 0.7f;
                    _primaryDamageType = DamageType.Piercing;
                    break;
                case WeaponType.Bow:
                    _weaponReachModifier = 3f;
                    _weaponSpeedModifier = 0.8f;
                    _primaryDamageType = DamageType.Piercing;
                    break;
                case WeaponType.Mace:
                    _weaponReachModifier = 0.8f;
                    _weaponSpeedModifier = 0.9f;
                    _primaryDamageType = DamageType.Physical;
                    break;
            }
        }

        #endregion

        #region Calculation Methods

        /// <summary>
        /// Calculate effective attack damage with all modifiers
        /// </summary>
        private float CalculateEffectiveAttackDamage()
        {
            float baseDamage = _attackDamage;

            // Apply weapon modifiers
            float weaponModifier = GetWeaponDamageModifier();

            // Apply combat efficiency
            float efficiencyModifier = _combatEfficiency;

            // Apply stagger penalty
            float staggerPenalty = IsStaggered ? 0.5f : 1f;

            return baseDamage * weaponModifier * efficiencyModifier * staggerPenalty;
        }

        /// <summary>
        /// Calculate effective attack range
        /// </summary>
        private float CalculateEffectiveAttackRange()
        {
            return _attackRange * _weaponReachModifier;
        }

        /// <summary>
        /// Calculate effective attack speed (attacks per second)
        /// </summary>
        private float CalculateEffectiveAttackSpeed()
        {
            float baseCooldown = _attackCooldown;
            float modifiedCooldown = baseCooldown / _weaponSpeedModifier;

            // Apply combat efficiency
            modifiedCooldown /= _combatEfficiency;

            return 1f / modifiedCooldown; // Convert to attacks per second
        }

        /// <summary>
        /// Calculate overall combat effectiveness (0-100)
        /// </summary>
        private float CalculateCombatEffectiveness()
        {
            float armorEffectiveness = _armorCondition;
            float weaponEffectiveness = 100f; // Could be modified by weapon condition
            float efficiencyScore = _combatEfficiency * 100f;

            return (armorEffectiveness + weaponEffectiveness + efficiencyScore) / 3f;
        }

        /// <summary>
        /// Get weapon damage modifier based on weapon type
        /// </summary>
        private float GetWeaponDamageModifier()
        {
            return _equippedWeaponType switch
            {
                WeaponType.Sword => 1f,
                WeaponType.Spear => 1.2f,
                WeaponType.Bow => 0.8f,
                WeaponType.Mace => 1.3f,
                WeaponType.Hammer => 1.4f,
                _ => 1f
            };
        }

        /// <summary>
        /// Update combat efficiency based on performance
        /// </summary>
        private void UpdateCombatEfficiency()
        {
            if (!_isInCombat) return;

            // Increase efficiency with consecutive hits
            if (_consecutiveHits > 0)
            {
                _combatEfficiency = Mathf.Min(1.5f, 1f + (_consecutiveHits * 0.1f));
            }

            // Decrease efficiency with consecutive misses
            if (_consecutiveMisses > 0)
            {
                _combatEfficiency = Mathf.Max(0.5f, 1f - (_consecutiveMisses * 0.05f));
            }
        }

        #endregion

        #region Attack Validation

        /// <summary>
        /// Check if unit can perform attack
        /// </summary>
        public bool CanAttack()
        {
            return TimeSinceLastAttack >= _attackCooldown && !IsStaggered;
        }

        /// <summary>
        /// Check if target is within attack range
        /// </summary>
        public bool IsInAttackRange(IEntity target)
        {
            var targetTransform = target.GetComponent<TransformComponent>();
            var myTransform = Entity.GetComponent<TransformComponent>();

            if (targetTransform == null || myTransform == null) return false;

            float distance = Vector3.Distance(myTransform.Position, targetTransform.Position);
            return distance <= CalculateEffectiveAttackRange();
        }

        #endregion

        #region Helper Methods for Odin Inspector

        private bool HasSecondaryDamage => _secondaryDamageType != DamageType.None;
        private Color GetArmorColor => Color.Lerp(Color.red, Color.green, _physicalArmor / 100f);
        private Color GetMagicResistanceColor => Color.Lerp(Color.red, Color.blue, _magicalResistance / 100f);
        private Color GetArmorConditionColor => Color.Lerp(Color.red, Color.green, _armorCondition / 100f);

        #endregion

        #region Debug Methods

        [Button("Test Combat Effectiveness"), FoldoutGroup("Debug Tools")]
        private void TestCombatEffectiveness()
        {
            Debug.Log($"Combat Effectiveness: {CalculateCombatEffectiveness():F1}%");
            Debug.Log($"Effective Attack Damage: {CalculateEffectiveAttackDamage():F1}");
            Debug.Log($"Effective Attack Range: {CalculateEffectiveAttackRange():F1}");
            Debug.Log($"Attack Speed: {CalculateEffectiveAttackSpeed():F2} attacks/sec");
        }

        [Button("Simulate Armor Damage"), FoldoutGroup("Debug Tools")]
        private void SimulateArmorDamage()
        {
            DamageArmor(10f);
            Debug.Log($"Armor damaged. Current condition: {_armorCondition:F1}%");
        }

        #endregion
    }
    /// <summary>
    /// Type of attack
    /// </summary>
    public enum AttackType
    {
        None,
        Melee,
        Ranged,
        Magic,
        Special
    }
    #region Supporting Data Structures

    public class DamageInfo
    {
        public IEntity Attacker;
        public IEntity Target;
        public DamageType DamageType;
        public WeaponType WeaponType;
        public float BaseDamage;
        public float FinalDamage;
        public float ArmorPenetration;
        public float DamageReduction;
        public bool IsSecondaryDamage;
        public bool IsCritical;
        public Vector3 HitPosition;
        public Vector3 HitDirection;
    }

    public enum DamageType
    {
        None,
        Physical,    // Basic melee damage
        Piercing,    // Arrows, spear thrusts
        Slashing,    // Sword cuts
        Blunt,       // Mace, hammer impacts
        Magical,     // Magical damage
        Fire,        // Fire damage
        Ice,         // Cold damage
        True         // Ignores all armor
    }

    public enum WeaponType
    {
        None,
        Sword,       // Balanced weapon
        Spear,       // Long reach, piercing
        Bow,         // Ranged, piercing
        Mace,        // High damage, stagger
        Hammer,      // Highest damage, slow
        Dagger,      // Fast, low damage
        Staff        // Magical focus
    }

    #endregion
}