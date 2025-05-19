using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;
using System;

namespace VikingRaven.Units.Models
{
    /// <summary>
    /// Enhanced model class for managing unit data and state
    /// Serves as the bridge between UnitDataSO (ScriptableObject) and the Entity components
    /// </summary>
    public class UnitModel
    {
        // Reference to the entity
        private IEntity _entity;
        
        // Reference to the data
        private UnitDataSO _unitData;
        
        // Cached components for quick access
        private TransformComponent _transformComponent;
        private HealthComponent _healthComponent;
        private CombatComponent _combatComponent;
        private StateComponent _stateComponent;
        private FormationComponent _formationComponent;
        private UnitTypeComponent _unitTypeComponent;
        private AggroDetectionComponent _aggroComponent;
        private NavigationComponent _navigationComponent;
        private AbilityComponent _abilityComponent;
        
        // Dynamic properties that can change during gameplay
        private int _experiencePoints = 0;
        private int _killCount = 0;
        private float _damageDealt = 0;
        private float _damageReceived = 0;
        private int _attacksPerformed = 0;
        private bool _isInitialized = false;
        
        // Events
        public event Action<float, IEntity> OnDamageTaken;
        public event Action OnDeath;
        public event Action<IEntity, float> OnAttackPerformed;
        public event Action<UnitType> OnUnitTypeChanged;
        public event Action<int> OnSquadAssigned;
        
        // Constructor
        public UnitModel(IEntity entity, UnitDataSO unitData)
        {
            _entity = entity;
            _unitData = unitData;
            
            // Cache components
            CacheComponents();
            
            // Apply initial data
            if (_unitData != null)
            {
                ApplyData();
            }
            
            // Subscribe to events
            SubscribeToEvents();
            
            _isInitialized = true;
        }
        
        // Properties to access data
        public IEntity Entity => _entity;
        public UnitDataSO Data => _unitData;
        public UnitType UnitType => _unitTypeComponent?.UnitType ?? UnitType.Infantry;
        public int SquadId => _formationComponent?.SquadId ?? -1;
        public int FormationSlot => _formationComponent?.FormationSlotIndex ?? -1;
        public float CurrentHealth => _healthComponent?.CurrentHealth ?? 0f;
        public float MaxHealth => _healthComponent?.MaxHealth ?? 0f;
        public bool IsDead => _healthComponent?.IsDead ?? false;
        public Vector3 Position => _transformComponent?.Position ?? Vector3.zero;
        public float AttackDamage => _combatComponent?.AttackDamage ?? 0f;
        public float AttackRange => _combatComponent?.AttackRange ?? 0f;
        public float MoveSpeed => _combatComponent?.MoveSpeed ?? 0f;
        public bool IsInitialized => _isInitialized;
        public bool HasReachedDestination => _navigationComponent?.HasReachedDestination ?? true;
        
        // Game statistics
        public int ExperiencePoints => _experiencePoints;
        public int KillCount => _killCount;
        public float DamageDealt => _damageDealt;
        public float DamageReceived => _damageReceived;
        public int AttacksPerformed => _attacksPerformed;
        public float DamagePerAttack => _attacksPerformed > 0 ? _damageDealt / _attacksPerformed : 0f;
        
        /// <summary>
        /// Cache components for quick access
        /// </summary>
        private void CacheComponents()
        {
            if (_entity == null) return;
            
            _transformComponent = _entity.GetComponent<TransformComponent>();
            _healthComponent = _entity.GetComponent<HealthComponent>();
            _combatComponent = _entity.GetComponent<CombatComponent>();
            _stateComponent = _entity.GetComponent<StateComponent>();
            _formationComponent = _entity.GetComponent<FormationComponent>();
            _unitTypeComponent = _entity.GetComponent<UnitTypeComponent>();
            _aggroComponent = _entity.GetComponent<AggroDetectionComponent>();
            _navigationComponent = _entity.GetComponent<NavigationComponent>();
            _abilityComponent = _entity.GetComponent<AbilityComponent>();
        }
        
        /// <summary>
        /// Subscribe to component events
        /// </summary>
        private void SubscribeToEvents()
        {
            if (_healthComponent != null)
            {
                _healthComponent.OnDamageTaken += HandleDamageTaken;
                _healthComponent.OnDeath += HandleDeath;
            }
            
            if (_combatComponent != null)
            {
                _combatComponent.OnAttackPerformed += HandleAttackPerformed;
                _combatComponent.OnDamageDealt += HandleDamageDealt;
            }
            
            if (_formationComponent != null)
            {
                // TODO: Add formation changed event
            }
        }
        
        /// <summary>
        /// Unsubscribe from all events
        /// </summary>
        public void Cleanup()
        {
            if (_healthComponent != null)
            {
                _healthComponent.OnDamageTaken -= HandleDamageTaken;
                _healthComponent.OnDeath -= HandleDeath;
            }
            
            if (_combatComponent != null)
            {
                _combatComponent.OnAttackPerformed -= HandleAttackPerformed;
                _combatComponent.OnDamageDealt -= HandleDamageDealt;
            }
            
            _isInitialized = false;
        }
        
        /// <summary>
        /// Handle damage taken event
        /// </summary>
        private void HandleDamageTaken(float amount, IEntity source)
        {
            _damageReceived += amount;
            OnDamageTaken?.Invoke(amount, source);
        }
        
        /// <summary>
        /// Handle death event
        /// </summary>
        private void HandleDeath()
        {
            OnDeath?.Invoke();
            Debug.Log($"Unit {_entity.Id} of type {UnitType} has died");
        }
        
        /// <summary>
        /// Handle attack performed event
        /// </summary>
        private void HandleAttackPerformed(IEntity target)
        {
            _attacksPerformed++;
            
            if (_combatComponent != null)
            {
                OnAttackPerformed?.Invoke(target, _combatComponent.AttackDamage);
            }
            
            // Check if target died from this attack
            var targetHealth = target.GetComponent<HealthComponent>();
            if (targetHealth != null && targetHealth.IsDead)
            {
                _killCount++;
                _experiencePoints += 10; // Simple XP reward
            }
        }
        
        /// <summary>
        /// Handle damage dealt event
        /// </summary>
        private void HandleDamageDealt(IEntity target, float amount)
        {
            _damageDealt += amount;
        }
        
        /// <summary>
        /// Apply data to the entity's components
        /// </summary>
        public void ApplyData()
        {
            if (_entity == null || _unitData == null) return;
            
            // Get the GameObject
            var entityObject = (_entity as MonoBehaviour)?.gameObject;
            if (entityObject == null) return;
            
            // Apply data to components
            _unitData.ApplyToUnit(entityObject);
            
            // Refresh cached components in case new ones were added
            CacheComponents();
            
            // Resubscribe to events
            SubscribeToEvents();
            
            Debug.Log($"UnitModel: Applied data from {_unitData.DisplayName} to entity {_entity.Id}");
        }
        
        /// <summary>
        /// Update the unit model's data
        /// </summary>
        /// <param name="newData">New unit data to apply</param>
        public void UpdateData(UnitDataSO newData)
        {
            if (newData == null) return;
            
            UnitDataSO oldData = _unitData;
            _unitData = newData;
            
            // Check if unit type has changed
            UnitType oldType = oldData?.UnitType ?? UnitType.Infantry;
            UnitType newType = newData.UnitType;
            
            ApplyData();
            
            // Trigger unit type changed event if needed
            if (oldType != newType)
            {
                OnUnitTypeChanged?.Invoke(newType);
            }
            
            Debug.Log($"UnitModel: Updated data for entity {_entity.Id} from {oldData?.DisplayName ?? "none"} to {newData.DisplayName}");
        }
        
        /// <summary>
        /// Set the unit's formation information
        /// </summary>
        /// <param name="squadId">ID of the squad</param>
        /// <param name="slotIndex">Slot index in formation</param>
        /// <param name="formationType">Type of formation</param>
        public void SetFormationInfo(int squadId, int slotIndex, FormationType formationType)
        {
            if (_formationComponent == null) return;
            
            int oldSquadId = _formationComponent.SquadId;
            
            _formationComponent.SetSquadId(squadId);
            _formationComponent.SetFormationSlot(slotIndex);
            _formationComponent.SetFormationType(formationType);
            
            // Trigger squad assigned event if squad changed
            if (oldSquadId != squadId)
            {
                OnSquadAssigned?.Invoke(squadId);
            }
            
            Debug.Log($"UnitModel: Set formation info for entity {_entity.Id} - Squad: {squadId}, Slot: {slotIndex}, Formation: {formationType}");
        }
        
        /// <summary>
        /// Move the unit to a position
        /// </summary>
        /// <param name="position">Target position</param>
        /// <param name="priority">Navigation priority</param>
        public void MoveTo(Vector3 position, NavigationCommandPriority priority = NavigationCommandPriority.Normal)
        {
            if (_navigationComponent == null) return;
            
            _navigationComponent.SetDestination(position, priority);
            
            Debug.Log($"UnitModel: Moving entity {_entity.Id} to position {position} with priority {priority}");
        }
        
        /// <summary>
        /// Attack a target entity
        /// </summary>
        /// <param name="target">Target entity</param>
        /// <returns>True if attack was initiated</returns>
        public bool Attack(IEntity target)
        {
            if (_combatComponent == null || target == null) return false;
            
            if (_combatComponent.CanAttack() && _combatComponent.IsInAttackRange(target))
            {
                _combatComponent.Attack(target);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Use the unit's ability
        /// </summary>
        /// <param name="targetPosition">Target position for the ability</param>
        /// <param name="targetEntity">Optional target entity</param>
        /// <returns>True if ability was activated</returns>
        public bool UseAbility(Vector3 targetPosition, IEntity targetEntity = null)
        {
            if (_abilityComponent == null) return false;
            
            return _abilityComponent.ActivateAbility(targetPosition, targetEntity);
        }
        
        /// <summary>
        /// Check if the unit has a specific component
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <returns>True if the unit has the component</returns>
        public bool HasComponent<T>() where T : class, IComponent
        {
            return _entity != null && _entity.HasComponent<T>();
        }
        
        /// <summary>
        /// Get a component from the unit
        /// </summary>
        /// <typeparam name="T">Component type</typeparam>
        /// <returns>Component or null if not found</returns>
        public T GetComponent<T>() where T : class, IComponent
        {
            return _entity?.GetComponent<T>();
        }
        
        /// <summary>
        /// Get the unit's current state
        /// </summary>
        /// <returns>Name of the current state</returns>
        public string GetCurrentState()
        {
            if (_stateComponent?.CurrentState != null)
            {
                return _stateComponent.CurrentState.GetType().Name;
            }
            return "Unknown";
        }
        
        /// <summary>
        /// Check if the unit is in combat
        /// </summary>
        /// <returns>True if the unit is in combat</returns>
        public bool IsInCombat()
        {
            if (_aggroComponent == null) return false;
            
            return _aggroComponent.HasEnemyInRange();
        }
        
        /// <summary>
        /// Reset the unit's stats
        /// </summary>
        public void ResetStats()
        {
            _experiencePoints = 0;
            _killCount = 0;
            _damageDealt = 0;
            _damageReceived = 0;
            _attacksPerformed = 0;
            
            // Also reset health if available
            if (_healthComponent != null)
            {
                _healthComponent.Revive();
            }
            
            Debug.Log($"UnitModel: Reset stats for entity {_entity.Id}");
        }
        
        /// <summary>
        /// Create a string representation of the unit
        /// </summary>
        public override string ToString()
        {
            return $"UnitModel(ID: {_entity?.Id}, Type: {UnitType}, HP: {CurrentHealth}/{MaxHealth}, Squad: {SquadId}, Slot: {FormationSlot})";
        }
        
        /// <summary>
        /// Create a deep copy of this unit model with a new entity
        /// </summary>
        /// <param name="newEntity">New entity to use</param>
        /// <returns>Clone of this unit model</returns>
        public UnitModel Clone(IEntity newEntity)
        {
            if (newEntity == null) return null;
            
            // Create a new model with the same data
            UnitModel clone = new UnitModel(newEntity, _unitData);
            
            // Copy statistics if needed
            // clone._experiencePoints = this._experiencePoints;
            // clone._killCount = this._killCount;
            
            return clone;
        }
    }
}