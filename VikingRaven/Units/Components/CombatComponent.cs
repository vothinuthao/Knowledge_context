using System;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Factory;

namespace VikingRaven.Units.Components
{
    public class CombatComponent : BaseComponent
    {
        #region Combat Stats Configuration

        [TitleGroup("Basic Combat Stats")]
        [InfoBox("Basic combat statistics loaded from UnitDataSO. These are base values before modifiers.")]
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

        [Obsolete("Obsolete")]
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

        [Obsolete("Obsolete")]
        private void LoadStatsFromUnitData()
        {
            if (Entity == null) return;
            var unitFactory = FindObjectOfType<UnitFactory>();
            if (unitFactory)
            {
                var unitModel = unitFactory.GetUnitModel(Entity);
                if (unitModel != null)
                {
                    _attackDamage = unitModel.Damage;
                    _attackRange = unitModel.Range;
                    _attackCooldown = unitModel.HitSpeed;
                    _moveSpeed = unitModel.MoveSpeed;
                    InitializeStatsFromUnitType(unitModel.UnitType);
                }
            }
        }
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
        private void InitializeEnhancedCombatStats()
        {
            _armorCondition = 100f;
            _combatEfficiency = 1f;
            _isInCombat = false;
            _isStaggered = false;

        }

        #endregion

        #region Enhanced Combat Methods
        public bool PerformEnhancedAttack(IEntity target)
        {
            if (!CanAttack() || target == null || IsStaggered) return false;

            _lastAttackTime = Time.time;
            DamageInfo damageInfo = CalculateDamageInfo(target);
            var targetCombat = target.GetComponent<CombatComponent>();
            if (targetCombat != null)
            {
                bool hitSuccessful = targetCombat.ReceiveDamage(damageInfo, Entity);

                if (hitSuccessful)
                {
                    _consecutiveHits++;
                    _consecutiveMisses = 0;
                    _totalDamageDealt += damageInfo.FinalDamage;

                    // Check if target died
                    var targetHealth = target.GetComponent<HealthComponent>();
                    if (targetHealth && !targetHealth.IsAlive)
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

        public bool ReceiveDamage(DamageInfo damageInfo, IEntity attacker)
        {
            if (!IsActive) return false;

            _lastDamageTakenTime = Time.time;

            float damageReduction = CalculateDamageReduction(damageInfo.DamageType, damageInfo.ArmorPenetration);
            float finalDamage = damageInfo.BaseDamage * (1f - damageReduction);
            damageInfo.DamageReduction = damageReduction;
            damageInfo.FinalDamage = finalDamage;

            var healthComponent = Entity.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                healthComponent.TakeDamage(finalDamage, attacker);
            }
            DamageArmor(damageInfo.BaseDamage * 0.1f);
            CheckForStagger(damageInfo);

            _totalDamageReceived += finalDamage;
            OnDamageReceived?.Invoke(damageInfo);
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
            float weaponModifier = GetWeaponDamageModifier();
            float efficiencyModifier = _combatEfficiency;
            float staggerPenalty = IsStaggered ? 0.5f : 1f;
            return baseDamage * weaponModifier * efficiencyModifier * staggerPenalty;
        }

        private float CalculateEffectiveAttackRange()
        {
            return _attackRange * _weaponReachModifier;
        }

        private float CalculateEffectiveAttackSpeed()
        {
            float baseCooldown = _attackCooldown;
            float modifiedCooldown = baseCooldown / _weaponSpeedModifier;
            modifiedCooldown /= _combatEfficiency;

            return 1f / modifiedCooldown;
        }

        private float CalculateCombatEffectiveness()
        {
            float armorEffectiveness = _armorCondition;
            float weaponEffectiveness = 100f;
            float efficiencyScore = _combatEfficiency * 100f;

            return (armorEffectiveness + weaponEffectiveness + efficiencyScore) / 3f;
        }
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
        private void UpdateCombatEfficiency()
        {
            if (!_isInCombat) return;

            if (_consecutiveHits > 0)
            {
                _combatEfficiency = Mathf.Min(1.5f, 1f + (_consecutiveHits * 0.1f));
            }

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

            if (!targetTransform || !myTransform) return false;

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
        #region Enhanced Debug Methods

        [TitleGroup("Enhanced Debug Tools")]
        [InfoBox("Comprehensive debugging tools for combat system analysis and testing.", InfoMessageType.Info)]
        
        [Button("Full Combat Analysis"), ButtonGroup("Analysis")]
        private void PerformFullCombatAnalysis()
        {
            Debug.Log("=== FULL COMBAT ANALYSIS ===");
            
            AnalyzeBasicCombatStats();
            AnalyzeRangedAttackCapabilities();
            AnalyzeDefensiveCapabilities();
            AnalyzeCombatEfficiencyMetrics();
            AnalyzeWeaponPerformance();
            
            Debug.Log("=== ANALYSIS COMPLETE ===");
        }

        [Button("Range Attack Debug"), ButtonGroup("Specific")]
        private void DebugRangeAttackSystem()
        {
            Debug.Log("=== RANGED ATTACK SYSTEM DEBUG ===");
            Debug.Log($"Equipped Weapon: {_equippedWeaponType}");
            Debug.Log($"Primary Damage Type: {_primaryDamageType}");
            
            // Range calculations
            float baseRange = _attackRange;
            float effectiveRange = CalculateEffectiveAttackRange();
            float weaponReachModifier = _weaponReachModifier;
            
            Debug.Log($"Base Attack Range: {baseRange:F2} units");
            Debug.Log($"Weapon Reach Modifier: {weaponReachModifier:F2}x");
            Debug.Log($"Effective Attack Range: {effectiveRange:F2} units");
            Debug.Log($"Range Increase: {(effectiveRange - baseRange):F2} units ({((effectiveRange/baseRange - 1) * 100):F1}%)");
            
            // Ranged attack performance
            if (_equippedWeaponType == WeaponType.Bow)
            {
                Debug.Log("--- ARCHER SPECIFIC ANALYSIS ---");
                Debug.Log($"Armor Penetration: {_armorPenetration:F1}");
                Debug.Log($"Weapon Speed Modifier: {_weaponSpeedModifier:F2}x");
                Debug.Log($"Effective Attack Speed: {CalculateEffectiveAttackSpeed():F2} attacks/sec");
                Debug.Log($"Damage per Second: {(CalculateEffectiveAttackDamage() * CalculateEffectiveAttackSpeed()):F2}");
                
                // Calculate optimal engagement range
                float optimalRange = effectiveRange * 0.8f; // 80% of max range for safety
                Debug.Log($"Recommended Engagement Range: {optimalRange:F2} units");
            }
            
            // Line of sight and targeting analysis
            AnalyzeTargetingCapabilities();
            
            Debug.Log("=== RANGED ATTACK DEBUG COMPLETE ===");
        }

        [Button("Combat State Debug"), ButtonGroup("Specific")]
        private void DebugCombatState()
        {
            Debug.Log("=== COMBAT STATE DEBUG ===");
            
            Debug.Log($"Is In Combat: {_isInCombat}");
            Debug.Log($"Is Staggered: {IsStaggered}");
            Debug.Log($"Can Attack: {CanAttack()}");
            
            Debug.Log($"Time Since Last Attack: {TimeSinceLastAttack:F2}s");
            Debug.Log($"Time Since Last Damage: {TimeSinceLastDamage:F2}s");
            Debug.Log($"Attack Cooldown: {_attackCooldown:F2}s");
            
            Debug.Log($"Consecutive Hits: {_consecutiveHits}");
            Debug.Log($"Consecutive Misses: {_consecutiveMisses}");
            Debug.Log($"Combat Efficiency: {_combatEfficiency:F2}x");
            
            if (_isStaggered)
            {
                float staggerTimeRemaining = _staggerEndTime - Time.time;
                Debug.Log($"Stagger Time Remaining: {staggerTimeRemaining:F2}s");
            }
            
            Debug.Log("=== COMBAT STATE DEBUG COMPLETE ===");
        }

        [Button("Test Range vs Target"), ButtonGroup("Testing")]
        private void TestRangeVsNearestTarget()
        {
            Debug.Log("=== RANGE TESTING vs NEAREST TARGET ===");
            
            // Find nearest potential target (simplified)
            var allUnits = FindObjectsOfType<CombatComponent>();
            CombatComponent nearestEnemy = null;
            float nearestDistance = float.MaxValue;
            
            var myTransform = Entity?.GetComponent<TransformComponent>();
            if (myTransform == null)
            {
                Debug.LogWarning("No TransformComponent found on this entity!");
                return;
            }
            
            foreach (var unit in allUnits)
            {
                if (unit == this) continue;
                
                var targetTransform = unit.Entity?.GetComponent<TransformComponent>();
                if (targetTransform != null)
                {
                    float distance = Vector3.Distance(myTransform.Position, targetTransform.Position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = unit;
                    }
                }
            }
            
            if (nearestEnemy != null)
            {
                Debug.Log($"Nearest Target Distance: {nearestDistance:F2} units");
                Debug.Log($"Effective Attack Range: {CalculateEffectiveAttackRange():F2} units");
                Debug.Log($"Can Attack Target: {IsInAttackRange(nearestEnemy.Entity)}");
                
                if (nearestDistance > CalculateEffectiveAttackRange())
                {
                    float moveDistance = nearestDistance - CalculateEffectiveAttackRange();
                    Debug.Log($"Need to move closer by: {moveDistance:F2} units");
                }
                else
                {
                    Debug.Log("Target is within attack range!");
                }
            }
            else
            {
                Debug.Log("No potential targets found in scene.");
            }
            
            Debug.Log("=== RANGE TESTING COMPLETE ===");
        }

        [Button("Simulate Combat Scenario"), ButtonGroup("Simulation")]
        private void SimulateCombatScenario()
        {
            Debug.Log("=== COMBAT SCENARIO SIMULATION ===");
            
            // Simulate different armor targets
            float[] armorValues = { 0f, 10f, 25f, 50f };
            DamageType[] damageTypes = { DamageType.Physical, DamageType.Piercing, DamageType.Magical };
            
            foreach (var armor in armorValues)
            {
                Debug.Log($"--- vs {armor} Armor ---");
                
                foreach (var damageType in damageTypes)
                {
                    float reduction = SimulateDamageReduction(damageType, armor);
                    float effectiveDamage = CalculateEffectiveAttackDamage() * (1f - reduction);
                    
                    Debug.Log($"{damageType} Damage: {effectiveDamage:F1} (Reduction: {reduction*100:F1}%)");
                }
            }
            
            Debug.Log("=== SIMULATION COMPLETE ===");
        }

        #endregion

        #region Private Analysis Methods

        private void AnalyzeBasicCombatStats()
        {
            Debug.Log("--- BASIC COMBAT STATS ---");
            Debug.Log($"Attack Damage: {_attackDamage:F1}");
            Debug.Log($"Attack Range: {_attackRange:F1}");
            Debug.Log($"Attack Cooldown: {_attackCooldown:F2}s");
            Debug.Log($"Move Speed: {_moveSpeed:F1}");
            Debug.Log($"Primary Damage Type: {_primaryDamageType}");
        }

        private void AnalyzeRangedAttackCapabilities()
        {
            Debug.Log("--- RANGED ATTACK CAPABILITIES ---");
            
            float effectiveRange = CalculateEffectiveAttackRange();
            float attackSpeed = CalculateEffectiveAttackSpeed();
            float dps = CalculateEffectiveAttackDamage() * attackSpeed;
            
            Debug.Log($"Effective Range: {effectiveRange:F2} units");
            Debug.Log($"Attack Speed: {attackSpeed:F2} attacks/sec");
            Debug.Log($"Damage Per Second: {dps:F2}");
            Debug.Log($"Armor Penetration: {_armorPenetration:F1}");
            
            // Range classification
            string rangeClass = effectiveRange switch
            {
                < 2f => "Melee",
                < 4f => "Short Range",
                < 8f => "Medium Range",
                _ => "Long Range"
            };
            Debug.Log($"Range Classification: {rangeClass}");
        }

        private void AnalyzeDefensiveCapabilities()
        {
            Debug.Log("--- DEFENSIVE CAPABILITIES ---");
            Debug.Log($"Physical Armor: {_physicalArmor:F1}");
            Debug.Log($"Magical Resistance: {_magicalResistance:F1}");
            Debug.Log($"Armor Condition: {_armorCondition:F1}%");
            
            // Calculate effective armor values
            float effectivePhysicalArmor = _physicalArmor * (_armorCondition / 100f);
            float physicalReduction = effectivePhysicalArmor / (effectivePhysicalArmor + 100f);
            
            Debug.Log($"Effective Physical Armor: {effectivePhysicalArmor:F1}");
            Debug.Log($"Physical Damage Reduction: {physicalReduction*100:F1}%");
        }

        private void AnalyzeCombatEfficiencyMetrics()
        {
            Debug.Log("--- COMBAT EFFICIENCY METRICS ---");
            Debug.Log($"Combat Effectiveness: {CalculateCombatEffectiveness():F1}%");
            Debug.Log($"Combat Efficiency: {_combatEfficiency:F2}x");
            Debug.Log($"Total Damage Dealt: {_totalDamageDealt:F1}");
            Debug.Log($"Total Damage Received: {_totalDamageReceived:F1}");
            Debug.Log($"Kill Count: {_killCount}");
            
            if (_totalDamageReceived > 0)
            {
                float damageRatio = _totalDamageDealt / _totalDamageReceived;
                Debug.Log($"Damage Ratio (Dealt/Received): {damageRatio:F2}");
            }
        }

        private void AnalyzeWeaponPerformance()
        {
            Debug.Log("--- WEAPON PERFORMANCE ---");
            Debug.Log($"Equipped Weapon: {_equippedWeaponType}");
            Debug.Log($"Weapon Reach Modifier: {_weaponReachModifier:F2}x");
            Debug.Log($"Weapon Speed Modifier: {_weaponSpeedModifier:F2}x");
            Debug.Log($"Weapon Damage Modifier: {GetWeaponDamageModifier():F2}x");
            
            // Weapon effectiveness analysis
            float weaponScore = (_weaponReachModifier + _weaponSpeedModifier + GetWeaponDamageModifier()) / 3f;
            Debug.Log($"Overall Weapon Effectiveness: {weaponScore:F2}");
        }

        private void AnalyzeTargetingCapabilities()
        {
            Debug.Log("--- TARGETING CAPABILITIES ---");
            
            var myTransform = Entity?.GetComponent<TransformComponent>();
            if (myTransform != null)
            {
                Debug.Log($"Current Position: {myTransform.Position}");
                Debug.Log($"Current Rotation: {myTransform.Rotation.eulerAngles}");
            }
            
            // Check for line of sight capabilities
            if (_equippedWeaponType == WeaponType.Bow)
            {
                Debug.Log("Ranged weapon equipped - line of sight targeting available");
                Debug.Log($"Optimal firing arc: ±30° from forward direction");
            }
            else
            {
                Debug.Log("Melee weapon equipped - direct contact required");
            }
        }

        private float SimulateDamageReduction(DamageType damageType, float targetArmor)
        {
            float effectiveArmor = targetArmor;
            
            switch (damageType)
            {
                case DamageType.Physical:
                case DamageType.Piercing:
                case DamageType.Slashing:
                    // Use physical armor
                    break;
                case DamageType.Magical:
                case DamageType.Fire:
                case DamageType.Ice:
                    effectiveArmor = 0f; // Assume no magical resistance for simulation
                    break;
                case DamageType.True:
                    return 0f;
            }
            
            // Apply armor penetration
            effectiveArmor = Mathf.Max(0f, effectiveArmor - _armorPenetration);
            
            // Calculate reduction
            return effectiveArmor / (effectiveArmor + 100f);
        }

        #endregion

        #region Debug Visualization

        [Button("Draw Attack Range"), ButtonGroup("Visualization")]
        private void DrawAttackRangeGizmo()
        {
            var myTransform = Entity?.GetComponent<TransformComponent>();
            if (myTransform != null)
            {
                float range = CalculateEffectiveAttackRange();
                
                // This would be better implemented in OnDrawGizmosSelected
                Debug.Log($"Attack range visualization: {range:F2} units from position {myTransform.Position}");
                Debug.Log("Note: Implement OnDrawGizmosSelected for visual range display in Scene view");
            }
        }

        // Optional: Add this method to visualize range in Scene view
        private void OnDrawGizmosSelected()
        {
            var myTransform = Entity?.GetComponent<TransformComponent>();
            if (myTransform != null)
            {
                float range = CalculateEffectiveAttackRange();
                
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(myTransform.Position, range);
                
                // Draw weapon type indicator
                Gizmos.color = _equippedWeaponType == WeaponType.Bow ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(myTransform.Position, range * 0.8f);
            }
        }

        #endregion
    }
}