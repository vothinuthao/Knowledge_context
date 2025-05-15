using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;

namespace VikingRaven.Units.Models
{
    /// <summary>
    /// Model class for managing unit data and state
    /// Serves as the bridge between UnitData (ScriptableObject) and the Entity components
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
        
        // Dynamic properties that can change during gameplay
        private int _experiencePoints = 0;
        private int _killCount = 0;
        private float _damageDealt = 0;
        private float _damageReceived = 0;
        
        // Constructor
        public UnitModel(IEntity entity, UnitDataSO unitData)
        {
            _entity = entity;
            _unitData = unitData;
            
            // Cache components
            CacheComponents();
            
            // Subscribe to events
            SubscribeToEvents();
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
        
        // Game statistics
        public int ExperiencePoints => _experiencePoints;
        public int KillCount => _killCount;
        public float DamageDealt => _damageDealt;
        public float DamageReceived => _damageReceived;
        
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
            }
        }
        
        /// <summary>
        /// Handle damage taken event
        /// </summary>
        private void HandleDamageTaken(float amount, IEntity source)
        {
            _damageReceived += amount;
        }
        
        /// <summary>
        /// Handle death event
        /// </summary>
        private void HandleDeath()
        {
            // Could trigger other game systems here
            Debug.Log($"Unit {_entity.Id} of type {UnitType} has died");
        }
        
        /// <summary>
        /// Handle attack performed event
        /// </summary>
        private void HandleAttackPerformed(IEntity target)
        {
            if (_combatComponent != null)
            {
                _damageDealt += _combatComponent.AttackDamage;
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
            
            // Refresh cached components
            CacheComponents();
        }
        
        /// <summary>
        /// Update the unit model's data
        /// </summary>
        /// <param name="newData">New unit data to apply</param>
        public void UpdateData(UnitDataSO newData)
        {
            if (newData == null) return;
            
            _unitData = newData;
            ApplyData();
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
            
            _formationComponent.SetSquadId(squadId);
            _formationComponent.SetFormationSlot(slotIndex);
            _formationComponent.SetFormationType(formationType);
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
        /// Create a string representation of the unit
        /// </summary>
        public override string ToString()
        {
            return $"UnitModel(ID: {_entity?.Id}, Type: {UnitType}, HP: {CurrentHealth}/{MaxHealth}, Squad: {SquadId}, Slot: {FormationSlot})";
        }
    }
}