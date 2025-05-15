using System;
using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;
using VikingRaven.Units.Models;

namespace VikingRaven.Units.Models
{
    /// <summary>
    /// Model class for managing squad data and state
    /// Acts as a coordinator for all units in a squad
    /// </summary>
    public class SquadModel
    {
        // Squad identification
        private int _squadId;
        private SquadDataSO _squadData;
        
        // Unit management
        private List<UnitModel> _unitModels = new List<UnitModel>();
        private Dictionary<int, UnitModel> _unitModelsById = new Dictionary<int, UnitModel>();
        
        // Formation and position tracking
        private FormationType _currentFormation;
        private Vector3 _currentPosition;
        private Quaternion _currentRotation;
        
        // State tracking
        private bool _isMoving = false;
        private bool _isEngaged = false; // In combat
        private bool _isReforming = false; // Changing formation
        
        // Squad statistics
        private int _totalKills = 0;
        private float _totalDamageDealt = 0f;
        private float _totalDamageReceived = 0f;
        
        // Events
        public event Action<UnitModel> OnUnitAdded;
        public event Action<UnitModel> OnUnitRemoved;
        public event Action<UnitModel> OnUnitDied;
        public event Action<FormationType> OnFormationChanged;
        public event Action<Vector3> OnPositionChanged;
        public event Action<bool> OnCombatStateChanged;
        
        // Properties
        public int SquadId => _squadId;
        public SquadDataSO Data => _squadData;
        public FormationType CurrentFormation => _currentFormation;
        public Vector3 CurrentPosition => _currentPosition;
        public Quaternion CurrentRotation => _currentRotation;
        public bool IsMoving => _isMoving;
        public bool IsEngaged => _isEngaged;
        public bool IsReforming => _isReforming;
        public int UnitCount => _unitModels.Count;
        public int TotalKills => _totalKills;
        public float TotalDamageDealt => _totalDamageDealt;
        public float TotalDamageReceived => _totalDamageReceived;
        public IReadOnlyList<UnitModel> Units => _unitModels;
        
        public SquadModel(int squadId, SquadDataSO squadData, Vector3 position, Quaternion rotation)
        {
            _squadId = squadId;
            _squadData = squadData;
            _currentPosition = position;
            _currentRotation = rotation;
            
            // Set default formation from data if available
            _currentFormation = squadData != null 
                ? squadData.DefaultFormationType 
                : FormationType.Line;
        }
        
        /// <summary>
        /// Add a unit to the squad
        /// </summary>
        /// <param name="unitModel">Unit model to add</param>
        public void AddUnit(UnitModel unitModel)
        {
            if (unitModel == null || unitModel.Entity == null) return;
            
            // Skip if already in the squad
            if (_unitModelsById.ContainsKey(unitModel.Entity.Id)) return;
            
            // Add to collections
            _unitModels.Add(unitModel);
            _unitModelsById[unitModel.Entity.Id] = unitModel;
            
            // Set formation component if available
            var formationComponent = unitModel.GetComponent<FormationComponent>();
            if (formationComponent != null)
            {
                formationComponent.SetSquadId(_squadId);
                formationComponent.SetFormationSlot(_unitModels.Count - 1);
                formationComponent.SetFormationType(_currentFormation);
            }
            
            // Subscribe to unit events
            SubscribeToUnitEvents(unitModel);
            
            // Trigger event
            OnUnitAdded?.Invoke(unitModel);
            
            Debug.Log($"SquadModel: Added unit {unitModel.Entity.Id} to squad {_squadId}, slot {_unitModels.Count - 1}");
        }
        
        /// <summary>
        /// Add multiple units to the squad
        /// </summary>
        /// <param name="unitModels">List of unit models to add</param>
        public void AddUnits(List<UnitModel> unitModels)
        {
            if (unitModels == null) return;
            
            foreach (var unitModel in unitModels)
            {
                AddUnit(unitModel);
            }
        }
        
        /// <summary>
        /// Remove a unit from the squad
        /// </summary>
        /// <param name="unitModel">Unit model to remove</param>
        public void RemoveUnit(UnitModel unitModel)
        {
            if (unitModel == null || unitModel.Entity == null) return;
            
            int entityId = unitModel.Entity.Id;
            
            if (_unitModelsById.ContainsKey(entityId))
            {
                // Remove from collections
                _unitModels.Remove(unitModel);
                _unitModelsById.Remove(entityId);
                
                // Unsubscribe from events
                UnsubscribeFromUnitEvents(unitModel);
                
                // Reset formation component
                var formationComponent = unitModel.GetComponent<FormationComponent>();
                if (formationComponent != null)
                {
                    formationComponent.SetSquadId(-1);
                }
                
                // Trigger event
                OnUnitRemoved?.Invoke(unitModel);
                
                Debug.Log($"SquadModel: Removed unit {entityId} from squad {_squadId}");
                
                // Update formation slots for remaining units
                UpdateFormationSlots();
            }
        }
        
        /// <summary>
        /// Remove a unit by entity ID
        /// </summary>
        /// <param name="entityId">ID of the entity to remove</param>
        public void RemoveUnitById(int entityId)
        {
            if (_unitModelsById.TryGetValue(entityId, out UnitModel unitModel))
            {
                RemoveUnit(unitModel);
            }
        }
        
        /// <summary>
        /// Subscribe to unit events
        /// </summary>
        private void SubscribeToUnitEvents(UnitModel unitModel)
        {
            if (unitModel == null) return;
            
            // Get health component to track death
            var healthComponent = unitModel.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                healthComponent.OnDeath += () => HandleUnitDeath(unitModel);
                healthComponent.OnDamageTaken += (amount, source) => HandleUnitDamageTaken(unitModel, amount, source);
            }
            
            // Get combat component to track attacks
            var combatComponent = unitModel.GetComponent<CombatComponent>();
            if (combatComponent != null)
            {
                combatComponent.OnAttackPerformed += (target) => HandleUnitAttack(unitModel, target);
            }
        }
        
        /// <summary>
        /// Unsubscribe from unit events
        /// </summary>
        private void UnsubscribeFromUnitEvents(UnitModel unitModel)
        {
            if (unitModel == null) return;
            
            var healthComponent = unitModel.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                // We can't easily unsubscribe from the lambda expressions
                // A better approach would be to store the handlers
                // This is a simplification for this example
            }
        }
        
        /// <summary>
        /// Handle unit death event
        /// </summary>
        private void HandleUnitDeath(UnitModel unitModel)
        {
            if (unitModel == null) return;
            
            // Trigger event
            OnUnitDied?.Invoke(unitModel);
            
            // Remove from squad
            RemoveUnit(unitModel);
            
            // Check if squad is empty
            if (_unitModels.Count == 0)
            {
                Debug.Log($"SquadModel: Squad {_squadId} has no more units");
            }
        }
        
        /// <summary>
        /// Handle unit damage taken event
        /// </summary>
        private void HandleUnitDamageTaken(UnitModel unitModel, float amount, IEntity source)
        {
            _totalDamageReceived += amount;
            
            // Check if we need to change combat state
            if (!_isEngaged)
            {
                _isEngaged = true;
                OnCombatStateChanged?.Invoke(true);
            }
        }
        
        /// <summary>
        /// Handle unit attack event
        /// </summary>
        private void HandleUnitAttack(UnitModel unitModel, IEntity target)
        {
            if (unitModel == null) return;
            
            // Update statistics
            var combatComponent = unitModel.GetComponent<CombatComponent>();
            if (combatComponent != null)
            {
                _totalDamageDealt += combatComponent.AttackDamage;
            }
            
            // Check if target died
            var targetHealth = target.GetComponent<HealthComponent>();
            if (targetHealth != null && targetHealth.IsDead)
            {
                _totalKills++;
            }
        }
        
        /// <summary>
        /// Update formation slots for all units
        /// </summary>
        private void UpdateFormationSlots()
        {
            for (int i = 0; i < _unitModels.Count; i++)
            {
                var formationComponent = _unitModels[i].GetComponent<FormationComponent>();
                if (formationComponent != null)
                {
                    formationComponent.SetFormationSlot(i);
                }
            }
        }
        
        /// <summary>
        /// Set the formation type for the squad
        /// </summary>
        /// <param name="formationType">New formation type</param>
        public void SetFormation(FormationType formationType)
        {
            if (_currentFormation == formationType) return;
            
            _currentFormation = formationType;
            _isReforming = true;
            
            // Update all unit formation components
            foreach (var unitModel in _unitModels)
            {
                var formationComponent = unitModel.GetComponent<FormationComponent>();
                if (formationComponent != null)
                {
                    formationComponent.SetFormationType(formationType);
                }
            }
            
            // Trigger event
            OnFormationChanged?.Invoke(formationType);
            
            Debug.Log($"SquadModel: Changed formation of squad {_squadId} to {formationType}");
            
            // Reset reforming flag after a delay (could be handled by a coroutine in a MonoBehaviour)
            // For now, we'll just use a simple delay mechanism
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => _isReforming = false;
            #endif
        }
        
        /// <summary>
        /// Set the target position for the squad
        /// </summary>
        /// <param name="position">New target position</param>
        public void SetTargetPosition(Vector3 position)
        {
            if (_currentPosition == position) return;
            
            _currentPosition = position;
            _isMoving = true;
            
            // Trigger event
            OnPositionChanged?.Invoke(position);
            
            Debug.Log($"SquadModel: Set target position of squad {_squadId} to {position}");
        }
        
        /// <summary>
        /// Set the target rotation for the squad
        /// </summary>
        /// <param name="rotation">New target rotation</param>
        public void SetTargetRotation(Quaternion rotation)
        {
            _currentRotation = rotation;
        }
        
        /// <summary>
        /// Update the squad's state
        /// </summary>
        public void Update()
        {
            // Update position based on average of units
            UpdateCurrentPosition();
            
            // Update moving state
            UpdateMovingState();
            
            // Update engaged state
            UpdateEngagedState();
        }
        
        /// <summary>
        /// Update the current position based on unit positions
        /// </summary>
        private void UpdateCurrentPosition()
        {
            if (_unitModels.Count == 0) return;
            
            Vector3 sum = Vector3.zero;
            foreach (var unitModel in _unitModels)
            {
                sum += unitModel.Position;
            }
            
            Vector3 averagePosition = sum / _unitModels.Count;
            
            // Only update if position has changed significantly
            if (Vector3.Distance(_currentPosition, averagePosition) > 0.5f)
            {
                _currentPosition = averagePosition;
            }
        }
        
        /// <summary>
        /// Update the moving state based on unit navigation
        /// </summary>
        private void UpdateMovingState()
        {
            if (_unitModels.Count == 0)
            {
                _isMoving = false;
                return;
            }
            
            // Check if any unit is still moving
            bool anyMoving = false;
            foreach (var unitModel in _unitModels)
            {
                var navigationComponent = unitModel.GetComponent<NavigationComponent>();
                if (navigationComponent != null && !navigationComponent.HasReachedDestination)
                {
                    anyMoving = true;
                    break;
                }
            }
            
            // Update moving state
            if (_isMoving != anyMoving)
            {
                _isMoving = anyMoving;
            }
        }
        
        /// <summary>
        /// Update the engaged state based on unit aggro
        /// </summary>
        private void UpdateEngagedState()
        {
            if (_unitModels.Count == 0)
            {
                _isEngaged = false;
                return;
            }
            
            // Check if any unit is engaged
            bool anyEngaged = false;
            foreach (var unitModel in _unitModels)
            {
                var aggroComponent = unitModel.GetComponent<AggroDetectionComponent>();
                if (aggroComponent != null && aggroComponent.HasEnemyInRange())
                {
                    anyEngaged = true;
                    break;
                }
            }
            
            // Update engaged state
            if (_isEngaged != anyEngaged)
            {
                _isEngaged = anyEngaged;
                OnCombatStateChanged?.Invoke(_isEngaged);
            }
        }
        
        /// <summary>
        /// Get a unit model by entity ID
        /// </summary>
        /// <param name="entityId">Entity ID</param>
        /// <returns>Unit model or null if not found</returns>
        public UnitModel GetUnitById(int entityId)
        {
            if (_unitModelsById.TryGetValue(entityId, out UnitModel unitModel))
            {
                return unitModel;
            }
            return null;
        }
        
        /// <summary>
        /// Get all unit models of a specific type
        /// </summary>
        /// <param name="unitType">Unit type to filter by</param>
        /// <returns>List of unit models</returns>
        public List<UnitModel> GetUnitsByType(UnitType unitType)
        {
            List<UnitModel> result = new List<UnitModel>();
            foreach (var unitModel in _unitModels)
            {
                if (unitModel.UnitType == unitType)
                {
                    result.Add(unitModel);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Get all unit entities
        /// </summary>
        /// <returns>List of entity references</returns>
        public List<IEntity> GetAllUnitEntities()
        {
            List<IEntity> result = new List<IEntity>();
            foreach (var unitModel in _unitModels)
            {
                result.Add(unitModel.Entity);
            }
            return result;
        }
        
        /// <summary>
        /// Check if this squad has any units of a specific type
        /// </summary>
        /// <param name="unitType">Unit type to check</param>
        /// <returns>True if the squad has at least one unit of the specified type</returns>
        public bool HasUnitType(UnitType unitType)
        {
            foreach (var unitModel in _unitModels)
            {
                if (unitModel.UnitType == unitType)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Calculate the average health percentage of the squad
        /// </summary>
        /// <returns>Average health percentage (0-1)</returns>
        public float GetAverageHealthPercentage()
        {
            if (_unitModels.Count == 0) return 0f;
            
            float sum = 0f;
            foreach (var unitModel in _unitModels)
            {
                if (unitModel.MaxHealth > 0)
                {
                    sum += unitModel.CurrentHealth / unitModel.MaxHealth;
                }
            }
            
            return sum / _unitModels.Count;
        }
        
        /// <summary>
        /// Check if this squad is still viable (has enough units)
        /// </summary>
        /// <returns>True if the squad has enough units to be viable</returns>
        public bool IsViable()
        {
            return _unitModels.Count > 0;
        }
        
        /// <summary>
        /// Clean up resources and unsubscribe from events
        /// </summary>
        public void Cleanup()
        {
            // Unsubscribe from all unit events
            foreach (var unitModel in _unitModels)
            {
                UnsubscribeFromUnitEvents(unitModel);
                unitModel.Cleanup();
            }
            
            // Clear collections
            _unitModels.Clear();
            _unitModelsById.Clear();
        }
        
        /// <summary>
        /// Create a string representation of the squad
        /// </summary>
        public override string ToString()
        {
            return $"SquadModel(ID: {_squadId}, Units: {_unitModels.Count}, Formation: {_currentFormation}, " +
                   $"Position: {_currentPosition}, Moving: {_isMoving}, Engaged: {_isEngaged})";
        }
    }
}