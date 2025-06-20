using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.StateMachine;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.StateMachine
{
    #region Core States
    public class EnhancedIdleState : IState
    {
        private readonly IEntity _entity;
        private readonly IStateMachine _stateMachine;
        private CombatComponent _combatComponent;
        private HealthComponent _healthComponent;
        private WeaponComponent _weaponComponent;
        private AggroDetectionComponent _aggroComponent;
        
        private float _idleTime = 0f;
        private float _lastHealthRegenTime = 0f;
        private bool _isResting = false;

        public EnhancedIdleState(IEntity entity, IStateMachine stateMachine)
        {
            _entity = entity;
            _stateMachine = stateMachine;
        }

        public void Enter()
        {
            CacheComponents();
            _idleTime = 0f;
            _isResting = false;
            
            if (_healthComponent != null && _healthComponent.CurrentInjuryState != InjuryState.Healthy)
            {
                _isResting = true;
            }
        }

        public void Execute()
        {
            _idleTime += Time.deltaTime;
            
            if (CheckForThreats())
            {
                return;
            }
            UpdatePassiveRecovery();
            UpdateWeaponMaintenance();
        }

        public void Exit()
        {
            _isResting = false;
        }

        private void CacheComponents()
        {
            _combatComponent = _entity.GetComponent<CombatComponent>();
            _healthComponent = _entity.GetComponent<HealthComponent>();
            _weaponComponent = _entity.GetComponent<WeaponComponent>();
            _aggroComponent = _entity.GetComponent<AggroDetectionComponent>();
        }

        private bool CheckForThreats()
        {
            if (_aggroComponent != null && _aggroComponent.HasEnemyInRange())
            {
                return true;
            }
            return false;
        }

        private void UpdatePassiveRecovery()
        {
            if (!_isResting || _healthComponent == null) return;
            
            if (Time.time - _lastHealthRegenTime > 2f)
            {
                // _healthComponent.RestoreStamina(5f);
                _lastHealthRegenTime = Time.time;
            }
        }

        private void UpdateWeaponMaintenance()
        {
            if (_weaponComponent == null) return;
            
            if (_idleTime > 10f && _weaponComponent.PrimaryWeaponCondition < 100f)
            {
                _weaponComponent.RepairWeapon(_weaponComponent.PrimaryWeapon, 1f * Time.deltaTime);
            }
        }
    }

    public class EnhancedAggroState : IState
    {
        private readonly IEntity _entity;
        private readonly IStateMachine _stateMachine;
        private CombatComponent _combatComponent;
        private HealthComponent _healthComponent;
        private WeaponComponent _weaponComponent;
        private AggroDetectionComponent _aggroComponent;
        private NavigationComponent _navigationComponent;
        
        private IEntity _currentTarget;
        private float _aggroTime = 0f;
        private float _lastThreatAssessment = 0f;

        public EnhancedAggroState(IEntity entity, IStateMachine stateMachine)
        {
            _entity = entity;
            _stateMachine = stateMachine;
        }

        public void Enter()
        {
            CacheComponents();
            _aggroTime = 0f;
            _currentTarget = null;
            AssessThreats();
            
            Debug.Log($"EnhancedAggroState: Entity {_entity.Id} detected threats");
        }

        public void Execute()
        {
            _aggroTime += Time.deltaTime;
            
            // Regular threat reassessment
            if (Time.time - _lastThreatAssessment > 1f)
            {
                AssessThreats();
                _lastThreatAssessment = Time.time;
            }
            
            // Handle positioning and preparation
            UpdateCombatPositioning();
            
            // Check if we should engage
            if (ShouldEngageCombat())
            {
                return; // StateComponent will handle transition to CombatEngaged
            }
            
            // Check if threats are gone
            if (!HasValidThreats())
            {
                return; // StateComponent will handle transition back to Idle
            }
        }

        public void Exit()
        {
            Debug.Log($"EnhancedAggroState: Entity {_entity.Id} exited aggro state after {_aggroTime:F1}s");
        }

        private void CacheComponents()
        {
            _combatComponent = _entity.GetComponent<CombatComponent>();
            _healthComponent = _entity.GetComponent<HealthComponent>();
            _weaponComponent = _entity.GetComponent<WeaponComponent>();
            _aggroComponent = _entity.GetComponent<AggroDetectionComponent>();
            _navigationComponent = _entity.GetComponent<NavigationComponent>();
        }

        private void AssessThreats()
        {
            if (_aggroComponent == null) return;
            
            // Find highest priority target
            var enemy = _aggroComponent.GetClosestEnemy();
            if (enemy != null)
            {
                _currentTarget = enemy;
            }
        }

        private void UpdateCombatPositioning()
        {
            if (_currentTarget == null || _navigationComponent == null) return;
            
            var targetTransform = _currentTarget.GetComponent<TransformComponent>();
            var myTransform = _entity.GetComponent<TransformComponent>();
            
            if (targetTransform != null && myTransform != null)
            {
                // Calculate optimal positioning based on weapon type
                Vector3 optimalPosition = CalculateOptimalPosition(targetTransform.Position, myTransform.Position);
                
                if (Vector3.Distance(myTransform.Position, optimalPosition) > 1f)
                {
                    _navigationComponent.SetDestination(optimalPosition);
                }
            }
        }

        private Vector3 CalculateOptimalPosition(Vector3 targetPos, Vector3 currentPos)
        {
            if (_weaponComponent == null) return currentPos;
            
            float optimalRange = _weaponComponent.EffectiveRange * 0.8f; // Stay within 80% of max range
            
            Vector3 direction = (currentPos - targetPos).normalized;
            return targetPos + direction * optimalRange;
        }

        private bool ShouldEngageCombat()
        {
            if (_currentTarget == null || _combatComponent == null) return false;
            
            // Check if target is in attack range
            return _combatComponent.IsInAttackRange(_currentTarget);
        }

        private bool HasValidThreats()
        {
            return _aggroComponent != null && _aggroComponent.HasEnemyInRange();
        }
    }

    #endregion

    #region New Combat States

    /// <summary>
    /// Combat Engaged State - Active combat with intelligent tactics
    /// Phase 1 Enhancement: Smart combat behavior with weapon-specific tactics
    /// </summary>
    public class CombatEngagedState : IState
    {
        private readonly IEntity _entity;
        private readonly IStateMachine _stateMachine;
        private CombatComponent _combatComponent;
        private HealthComponent _healthComponent;
        private WeaponComponent _weaponComponent;
        private AggroDetectionComponent _aggroComponent;
        
        private IEntity _currentTarget;
        private float _combatTime = 0f;
        private float _lastAttackAttempt = 0f;
        private int _consecutiveMisses = 0;

        public CombatEngagedState(IEntity entity, IStateMachine stateMachine)
        {
            _entity = entity;
            _stateMachine = stateMachine;
        }

        public void Enter()
        {
            CacheComponents();
            _combatTime = 0f;
            _consecutiveMisses = 0;
            
            // Find initial target
            if (_aggroComponent != null)
            {
                _currentTarget = _aggroComponent.GetClosestEnemy();
            }
            
            Debug.Log($"CombatEngagedState: Entity {_entity.Id} engaged in combat");
        }

        public void Execute()
        {
            _combatTime += Time.deltaTime;
            
            // Update target
            UpdateTarget();
            
            // Check combat conditions
            if (!CanContinueCombat())
            {
                return; // StateComponent will handle appropriate transition
            }
            
            // Execute combat behavior
            ExecuteCombatBehavior();
            
            // Handle weapon abilities
            UpdateWeaponAbilities();
        }

        public void Exit()
        {
            Debug.Log($"CombatEngagedState: Entity {_entity.Id} disengaged from combat after {_combatTime:F1}s");
        }

        private void CacheComponents()
        {
            _combatComponent = _entity.GetComponent<CombatComponent>();
            _healthComponent = _entity.GetComponent<HealthComponent>();
            _weaponComponent = _entity.GetComponent<WeaponComponent>();
            _aggroComponent = _entity.GetComponent<AggroDetectionComponent>();
        }

        private void UpdateTarget()
        {
            if (_aggroComponent != null)
            {
                var newTarget = _aggroComponent.GetClosestEnemy();
                if (newTarget != null)
                {
                    _currentTarget = newTarget;
                }
            }
        }

        private bool CanContinueCombat()
        {
            // Check health condition
            if (_healthComponent != null && _healthComponent.HealthPercentage < 25f)
            {
                return false; // Should retreat
            }
            
            // Check stamina
            if (_healthComponent != null && _healthComponent.IsExhausted)
            {
                return false; // Too exhausted to continue
            }
            
            // Check weapon condition
            if (_weaponComponent != null && _weaponComponent.IsPrimaryWeaponBroken)
            {
                return false; // Weapon broken
            }
            
            // Check if target still exists
            if (_currentTarget == null)
            {
                return false; // No target
            }
            
            return true;
        }

        private void ExecuteCombatBehavior()
        {
            if (_currentTarget == null || _combatComponent == null) return;
            
            if (_combatComponent.CanAttack() && _combatComponent.IsInAttackRange(_currentTarget))
            {
                if (_healthComponent)
                {
                    return; // Not enough stamina
                }
                
                // Perform enhanced attack
                bool hitSuccessful = _combatComponent.PerformEnhancedAttack(_currentTarget);
                
                if (hitSuccessful)
                {
                    _consecutiveMisses = 0;
                    
                    // Gain weapon experience
                    if (_weaponComponent != null)
                    {
                        bool isCritical = Random.value < (_weaponComponent.CriticalChance / 100f);
                        _weaponComponent.ProcessSuccessfulHit(_weaponComponent.PrimaryWeapon, _combatComponent.EffectiveAttackDamage, isCritical);
                    }
                }
                else
                {
                    _consecutiveMisses++;
                }
                
                _lastAttackAttempt = Time.time;
            }
        }

        private void UpdateWeaponAbilities()
        {
            if (_weaponComponent == null || _currentTarget == null) return;
            
            // Use weapon abilities based on situation
            if (_consecutiveMisses >= 3) // Multiple misses, try special ability
            {
                TryUseWeaponAbility();
            }
            
            // Use abilities based on health situation
            if (_healthComponent != null && _healthComponent.HealthPercentage < 50f)
            {
                TryUseDefensiveAbility();
            }
        }

        private void TryUseWeaponAbility()
        {
            if (_weaponComponent.UnlockedAbilities.Count == 0) return;
            
            // Try to use an offensive ability
            foreach (var ability in _weaponComponent.UnlockedAbilities)
            {
                if (IsOffensiveAbility(ability.Type))
                {
                    if (_weaponComponent.UseWeaponAbility(ability.Type))
                    {
                        Debug.Log($"CombatEngagedState: Used weapon ability {ability.Type}");
                        break;
                    }
                }
            }
        }

        private void TryUseDefensiveAbility()
        {
            // Try to use defensive abilities when health is low
            foreach (var ability in _weaponComponent.UnlockedAbilities)
            {
                if (IsDefensiveAbility(ability.Type))
                {
                    if (_weaponComponent.UseWeaponAbility(ability.Type))
                    {
                        Debug.Log($"CombatEngagedState: Used defensive ability {ability.Type}");
                        break;
                    }
                }
            }
        }

        private bool IsOffensiveAbility(WeaponAbilityType abilityType)
        {
            return abilityType switch
            {
                WeaponAbilityType.PowerStrike or
                WeaponAbilityType.FlurryAttack or
                WeaponAbilityType.ThrustAttack or
                WeaponAbilityType.ChargeAttack or
                WeaponAbilityType.AimedShot or
                WeaponAbilityType.MultiShot or
                WeaponAbilityType.PiercingShot or
                WeaponAbilityType.CrushingBlow => true,
                _ => false
            };
        }

        private bool IsDefensiveAbility(WeaponAbilityType abilityType)
        {
            return abilityType switch
            {
                WeaponAbilityType.Parry => true,
                _ => false
            };
        }
    }

    /// <summary>
    /// Retreat State - Tactical withdrawal with intelligent escape behavior
    /// Phase 1 Enhancement: Smart retreat tactics based on situation
    /// </summary>
    public class RetreatState : IState
    {
        private readonly IEntity _entity;
        private readonly IStateMachine _stateMachine;
        private CombatComponent _combatComponent;
        private HealthComponent _healthComponent;
        private NavigationComponent _navigationComponent;
        private AggroDetectionComponent _aggroComponent;
        
        private Vector3 _retreatTarget;
        private float _retreatTime = 0f;
        private bool _hasReachedSafety = false;

        public RetreatState(IEntity entity, IStateMachine stateMachine)
        {
            _entity = entity;
            _stateMachine = stateMachine;
        }

        public void Enter()
        {
            CacheComponents();
            _retreatTime = 0f;
            _hasReachedSafety = false;
            
            // Calculate retreat destination
            CalculateRetreatDestination();
            
            Debug.Log($"RetreatState: Entity {_entity.Id} retreating to safety");
        }

        public void Execute()
        {
            _retreatTime += Time.deltaTime;
            
            // Move to retreat destination
            ExecuteRetreat();
            
            // Check if we've reached safety
            CheckSafetyStatus();
            
            // Check if we can stop retreating
            if (CanStopRetreating())
            {
                return; // StateComponent will handle transition
            }
        }

        public void Exit()
        {
            Debug.Log($"RetreatState: Entity {_entity.Id} finished retreating after {_retreatTime:F1}s");
        }

        private void CacheComponents()
        {
            _combatComponent = _entity.GetComponent<CombatComponent>();
            _healthComponent = _entity.GetComponent<HealthComponent>();
            _navigationComponent = _entity.GetComponent<NavigationComponent>();
            _aggroComponent = _entity.GetComponent<AggroDetectionComponent>();
        }

        private void CalculateRetreatDestination()
        {
            var myTransform = _entity.GetComponent<TransformComponent>();
            if (myTransform == null) return;
            
            Vector3 currentPos = myTransform.Position;
            Vector3 retreatDirection = Vector3.back;
            
            if (_aggroComponent != null && _aggroComponent.HasEnemyInRange())
            {
                var enemy = _aggroComponent.GetClosestEnemy();
                if (enemy != null)
                {
                    var enemyTransform = enemy.GetComponent<TransformComponent>();
                    if (enemyTransform != null)
                    {
                        retreatDirection = (currentPos - enemyTransform.Position).normalized;
                    }
                }
            }
            
            float retreatDistance = CalculateRetreatDistance();
            _retreatTarget = currentPos + retreatDirection * retreatDistance;
        }

        private float CalculateRetreatDistance()
        {
            float baseDistance = 15f;
            
            if (_healthComponent != null)
            {
                float healthFactor = (100f - _healthComponent.HealthPercentage) / 100f;
                baseDistance += healthFactor * 10f;
            }
            
            return baseDistance;
        }

        private void ExecuteRetreat()
        {
            if (_navigationComponent == null) return;
            
            _navigationComponent.SetDestination(_retreatTarget);
        }

        private void CheckSafetyStatus()
        {
            var myTransform = _entity.GetComponent<TransformComponent>();
            if (myTransform == null) return;
            
            // Check if we've reached the retreat destinatio
            if (Vector3.Distance(myTransform.Position, _retreatTarget) < 2f)
            {
                _hasReachedSafety = true;
            }
            
            // Check if enemies are still in range
            if (_aggroComponent != null && !_aggroComponent.HasEnemyInRange())
            {
                _hasReachedSafety = true;
            }
        }

        private bool CanStopRetreating()
        {
            if (!_hasReachedSafety) return false;
            if (_aggroComponent != null && _aggroComponent.HasEnemyInRange()) return false;
            if (_healthComponent != null && _healthComponent.HealthPercentage > 50f)
            {
                return true;
            }
            return _retreatTime > 20f;
        }
    }
    public class ExhaustedState : IState
    {
        private readonly IEntity _entity;
        private readonly IStateMachine _stateMachine;
        private HealthComponent _healthComponent;
        private CombatComponent _combatComponent;
        
        private float _restTime = 0f;
        private bool _isRecovering = false;

        public ExhaustedState(IEntity entity, IStateMachine stateMachine)
        {
            _entity = entity;
            _stateMachine = stateMachine;
        }

        public void Enter()
        {
            _healthComponent = _entity.GetComponent<HealthComponent>();
            _combatComponent = _entity.GetComponent<CombatComponent>();
            _restTime = 0f;
            _isRecovering = true;
            
            Debug.Log($"ExhaustedState: Entity {_entity.Id} is exhausted and resting");
        }

        public void Execute()
        {
            _restTime += Time.deltaTime;
            
            // Enhanced stamina recovery
            if (_healthComponent != null && _isRecovering)
            {
                float bonusRecovery = 10f * Time.deltaTime; // Bonus stamina recovery while resting
                // _healthComponent.RestoreStamina(bonusRecovery);
            }
        }

        public void Exit()
        {
            _isRecovering = false;
            Debug.Log($"ExhaustedState: Entity {_entity.Id} recovered after {_restTime:F1}s");
        }

        private bool HasRecovered()
        {
            if (_healthComponent == null) return true;
            
            // Recovery threshold - 50% stamina
            return _healthComponent.StaminaPercentage > 50f;
        }
    }

    /// <summary>
    /// Weapon Broken State - Equipment failure behavior
    /// Phase 1 Enhancement: Equipment management and emergency tactics
    /// </summary>
    public class WeaponBrokenState : IState
    {
        private readonly IEntity _entity;
        private readonly IStateMachine _stateMachine;
        private WeaponComponent _weaponComponent;
        private CombatComponent _combatComponent;
        
        private float _brokenTime = 0f;
        private bool _triedSecondaryWeapon = false;

        public WeaponBrokenState(IEntity entity, IStateMachine stateMachine)
        {
            _entity = entity;
            _stateMachine = stateMachine;
        }

        public void Enter()
        {
            _weaponComponent = _entity.GetComponent<WeaponComponent>();
            _combatComponent = _entity.GetComponent<CombatComponent>();
            _brokenTime = 0f;
            _triedSecondaryWeapon = false;
            
            // Try to switch to secondary weapon
            TrySwitchToSecondaryWeapon();
            
            Debug.Log($"WeaponBrokenState: Entity {_entity.Id} has a broken weapon");
        }

        public void Execute()
        {
            _brokenTime += Time.deltaTime;
            
            // Try emergency repairs
            if (_brokenTime > 5f && !_triedSecondaryWeapon)
            {
                TryEmergencyRepair();
            }
            
            // Check if weapon is repaired or we have alternative
            if (HasWorkingWeapon())
            {
                return; // StateComponent will handle transition
            }
        }

        public void Exit()
        {
            Debug.Log($"WeaponBrokenState: Entity {_entity.Id} resolved weapon issue after {_brokenTime:F1}s");
        }

        private void TrySwitchToSecondaryWeapon()
        {
            if (_weaponComponent == null) return;
            
            if (_weaponComponent.SecondaryWeapon != WeaponType.None && !_weaponComponent.IsSecondaryWeaponBroken)
            {
                _weaponComponent.SwitchPrimaryWeapon(_weaponComponent.SecondaryWeapon);
                _triedSecondaryWeapon = true;
                Debug.Log($"WeaponBrokenState: Switched to secondary weapon");
            }
        }

        private void TryEmergencyRepair()
        {
            if (_weaponComponent == null) return;
            
            // Emergency field repair (limited effectiveness)
            _weaponComponent.RepairWeapon(_weaponComponent.PrimaryWeapon, 20f);
            Debug.Log($"WeaponBrokenState: Attempted emergency repair");
        }

        private bool HasWorkingWeapon()
        {
            if (_weaponComponent == null) return false;
            
            return !_weaponComponent.IsPrimaryWeaponBroken;
        }
    }

    /// <summary>
    /// Guarding State - Defensive position behavior
    /// Phase 1 Enhancement: Intelligent defensive positioning
    /// </summary>
    public class GuardingState : IState
    {
        private readonly IEntity _entity;
        private readonly IStateMachine _stateMachine;
        private Vector3 _guardPosition;
        private float _guardTime = 0f;

        public GuardingState(IEntity entity, IStateMachine stateMachine)
        {
            _entity = entity;
            _stateMachine = stateMachine;
        }

        public void Enter()
        {
            var transform = _entity.GetComponent<TransformComponent>();
            if (transform != null)
            {
                _guardPosition = transform.Position;
            }
            
            _guardTime = 0f;
            Debug.Log($"GuardingState: Entity {_entity.Id} taking defensive position");
        }

        public void Execute()
        {
            _guardTime += Time.deltaTime;
            
            // Maintain position and watch for threats
            var aggroComponent = _entity.GetComponent<AggroDetectionComponent>();
            if (aggroComponent != null && aggroComponent.HasEnemyInRange())
            {
                return; // Threat detected, StateComponent will handle transition
            }
        }

        public void Exit()
        {
            Debug.Log($"GuardingState: Entity {_entity.Id} stopped guarding after {_guardTime:F1}s");
        }
    }

    /// <summary>
    /// Patrolling State - Movement patrol behavior
    /// Phase 1 Enhancement: Formation-aware patrol patterns
    /// </summary>
    public class PatrollingState : IState
    {
        private readonly IEntity _entity;
        private readonly IStateMachine _stateMachine;
        private float _patrolTime = 0f;

        public PatrollingState(IEntity entity, IStateMachine stateMachine)
        {
            _entity = entity;
            _stateMachine = stateMachine;
        }

        public void Enter()
        {
            _patrolTime = 0f;
            Debug.Log($"PatrollingState: Entity {_entity.Id} started patrolling");
        }

        public void Execute()
        {
            _patrolTime += Time.deltaTime;
            
            // Basic patrol logic - could be enhanced with waypoint system
            var aggroComponent = _entity.GetComponent<AggroDetectionComponent>();
            if (aggroComponent && aggroComponent.HasEnemyInRange())
            {
                return; // Threat detected, StateComponent will handle transition
            }
        }

        public void Exit()
        {
            Debug.Log($"PatrollingState: Entity {_entity.Id} stopped patrolling after {_patrolTime:F1}s");
        }
    }

    #endregion

    // #region Utility Extensions
    //
    // /// <summary>
    // /// Extension methods for state machine to support enhanced states
    // /// </summary>
    // public static class StateMachineExtensions
    // {
    //     public static T GetState<T>(this IStateMachine stateMachine) where T : class, IState
    //     {
    //         // This would need to be implemented in the actual StateMachine class
    //         // For now, returning null as placeholder
    //         return null;
    //     }
    // }
    //
    // #endregion
}