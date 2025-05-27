using System;
using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;
using VikingRaven.Units.Models;

namespace VikingRaven.Units.Models
{
    public class SquadModel
    {
        private int _squadId;
        private SquadDataSO _squadData;
        
        // Unit management
        private List<UnitModel> _unitModels = new List<UnitModel>();
        private Dictionary<int, UnitModel> _unitModelsById = new Dictionary<int, UnitModel>();
        private Dictionary<UnitType, List<UnitModel>> _unitModelsByType = new Dictionary<UnitType, List<UnitModel>>();
        private FormationType _currentFormation;
        private Vector3 _currentPosition;
        private Quaternion _currentRotation;
        private float _formationSpacing;
        private FormationSpacingConfig _formationSpacingConfig;
        
        // Formation offsets cache
        private Dictionary<int, Vector3> _formationOffsets = new Dictionary<int, Vector3>();
        
        // State tracking
        private bool _isMoving = false;
        private bool _isEngaged = false;
        private bool _isReforming = false;
        private bool _isInitialized = false;
        
        // Squad statistics
        private int _totalKills = 0;
        private float _totalDamageDealt = 0f;
        private float _totalDamageReceived = 0f;
        private int _combatStreak = 0;
        private float _lastCombatTime = 0f;
        
        // Events
        public event Action<UnitModel> OnUnitAdded;
        public event Action<UnitModel> OnUnitRemoved;
        public event Action<UnitModel> OnUnitDied;
        public event Action<FormationType> OnFormationChanged;
        public event Action<Vector3> OnPositionChanged;
        public event Action<bool> OnCombatStateChanged;
        public event Action<float> OnDamageTaken;
        public event Action<float> OnDamageDealt;
        
        // Properties
        public int SquadId => _squadId;
        public SquadDataSO Data => _squadData;
        public FormationType CurrentFormation => _currentFormation;
        public Vector3 CurrentPosition => _currentPosition;
        public Quaternion CurrentRotation => _currentRotation;
        
        /// <summary>
        /// ENHANCED: FormationSpacing now uses both legacy and new system
        /// </summary>
        public float FormationSpacing => GetEffectiveFormationSpacing(_currentFormation);
        
        public bool IsMoving => _isMoving;
        public bool IsEngaged => _isEngaged;
        public bool IsReforming => _isReforming;
        public bool IsInitialized => _isInitialized;
        public int UnitCount => _unitModels.Count;
        public int TotalKills => _totalKills;
        public float TotalDamageDealt => _totalDamageDealt;
        public float TotalDamageReceived => _totalDamageReceived;
        public int CombatStreak => _combatStreak;
        public IReadOnlyList<UnitModel> Units => _unitModels;
        public IReadOnlyDictionary<int, Vector3> FormationOffsets => _formationOffsets;
        
        /// <summary>
        /// ENHANCED: Constructor with Formation Config Integration
        /// Maintains backward compatibility with legacy SpacingMultiplier
        /// </summary>
        public SquadModel(int squadId, SquadDataSO squadData, Vector3 position, Quaternion rotation)
        {
            _squadId = squadId;
            _squadData = squadData;
            _currentPosition = position;
            _currentRotation = rotation;
            
            // Initialize dictionaries for unit types
            _unitModelsByType[UnitType.Infantry] = new List<UnitModel>();
            _unitModelsByType[UnitType.Archer] = new List<UnitModel>();
            _unitModelsByType[UnitType.Pike] = new List<UnitModel>();
            
            // FIXED: Initialize formation spacing - supports both systems
            InitializeFormationSpacing(squadData);
            
            // Set default formation from data if available
            _currentFormation = squadData != null 
                ? squadData.DefaultFormationType 
                : FormationType.Normal; // Changed from FormationType.Line to FormationType.Normal
                
            _isInitialized = true;
            
            Debug.Log($"SquadModel: Created squad {squadId} with formation {_currentFormation}, " +
                     $"spacing: {GetEffectiveFormationSpacing(_currentFormation):F2}");
        }
        
        /// <summary>
        /// FIXED: Initialize formation spacing from squadData
        /// Supports both legacy SpacingMultiplier and new FormationSpacingConfig
        /// </summary>
        private void InitializeFormationSpacing(SquadDataSO squadData)
        {
            if (squadData != null)
            {
                // FIXED: Get SpacingMultiplier from squadData (legacy compatibility)
                _formationSpacing = squadData.SpacingMultiplier;
                
                // NEW: Get FormationSpacingConfig for enhanced formation control
                _formationSpacingConfig = squadData.FormationSpacingConfig;
                
                if (_formationSpacingConfig != null)
                {
                    Debug.Log($"SquadModel: Using FormationSpacingConfig '{_formationSpacingConfig.name}' " +
                             $"with base multiplier {_formationSpacing:F2}");
                }
                else
                {
                    Debug.Log($"SquadModel: Using legacy spacing multiplier {_formationSpacing:F2}");
                }
            }
            else
            {
                // Fallback values
                _formationSpacing = 1.0f;
                _formationSpacingConfig = null;
                Debug.LogWarning("SquadModel: No squad data provided, using default spacing");
            }
        }
        
        /// <summary>
        /// ENHANCED: Get effective formation spacing for specific formation type
        /// Combines legacy SpacingMultiplier with new FormationSpacingConfig
        /// </summary>
        public float GetEffectiveFormationSpacing(FormationType formationType)
        {
            if (_squadData != null)
            {
                // Use SquadDataSO method which handles both legacy and new system
                return _squadData.GetFormationSpacing(formationType);
            }
            
            // Fallback calculation if no squad data
            float baseSpacing = GetLegacyFormationSpacing(formationType);
            return baseSpacing * _formationSpacing;
        }
        
        /// <summary>
        /// Legacy formation spacing calculation for fallback
        /// </summary>
        private float GetLegacyFormationSpacing(FormationType formationType)
        {
            return formationType switch
            {
                FormationType.Normal => 2.5f,
                FormationType.Phalanx => 1.8f,
                FormationType.Testudo => 1.2f,
                _ => 2.0f
            };
        }
        
        /// <summary>
        /// ENHANCED: Get position tolerance for formation positioning
        /// Uses FormationSpacingConfig if available, otherwise uses default
        /// </summary>
        public float GetPositionTolerance()
        {
            if (_squadData != null)
            {
                return _squadData.GetPositionTolerance();
            }
            
            return 0.3f; // Default tolerance
        }
        
        /// <summary>
        /// Check if squad has enhanced formation configuration
        /// </summary>
        public bool HasFormationConfig()
        {
            return _formationSpacingConfig != null;
        }
        
        /// <summary>
        /// Add a unit to the squad
        /// </summary>
        public void AddUnit(UnitModel unitModel)
        {
            if (unitModel == null || unitModel.Entity == null) return;
            
            // Skip if already in the squad
            if (_unitModelsById.ContainsKey(unitModel.Entity.Id)) return;
            
            // Add to collections
            _unitModels.Add(unitModel);
            _unitModelsById[unitModel.Entity.Id] = unitModel;
            
            // Add to type-specific collection
            UnitType unitType = unitModel.UnitType;
            if (_unitModelsByType.ContainsKey(unitType))
            {
                _unitModelsByType[unitType].Add(unitModel);
            }
            
            // Generate formation offset for the unit
            UpdateUnitFormationOffset(unitModel);
            
            // Subscribe to unit events
            SubscribeToUnitEvents(unitModel);
            
            // Trigger event
            OnUnitAdded?.Invoke(unitModel);
            
            Debug.Log($"SquadModel: Added unit {unitModel.Entity.Id} to squad {_squadId}, slot {_unitModels.Count - 1}");
        }
        
        /// <summary>
        /// Add multiple units to the squad
        /// </summary>
        public void AddUnits(List<UnitModel> unitModels)
        {
            if (unitModels == null) return;
            
            foreach (var unitModel in unitModels)
            {
                AddUnit(unitModel);
            }
            
            // After adding all units, calculate formation offsets
            RecalculateAllFormationOffsets();
        }
        
        /// <summary>
        /// Remove a unit from the squad
        /// </summary>
        public void RemoveUnit(UnitModel unitModel)
        {
            if (unitModel == null || unitModel.Entity == null) return;
            
            int entityId = unitModel.Entity.Id;
            
            if (_unitModelsById.ContainsKey(entityId))
            {
                // Remove from collections
                _unitModels.Remove(unitModel);
                _unitModelsById.Remove(entityId);
                
                // Remove from type-specific collection
                UnitType unitType = unitModel.UnitType;
                if (_unitModelsByType.ContainsKey(unitType))
                {
                    _unitModelsByType[unitType].Remove(unitModel);
                }
                
                // Remove from formation offsets
                _formationOffsets.Remove(entityId);
                
                // Unsubscribe from events
                UnsubscribeFromUnitEvents(unitModel);
                
                // Trigger event
                OnUnitRemoved?.Invoke(unitModel);
                
                Debug.Log($"SquadModel: Removed unit {entityId} from squad {_squadId}");
                
                // Update formation slots for remaining units
                UpdateFormationSlots();
            }
        }
        
        /// <summary>
        /// Subscribe to unit events
        /// </summary>
        private void SubscribeToUnitEvents(UnitModel unitModel)
        {
            if (unitModel == null) return;
            
            // Subscribe to damage and death events
            unitModel.OnDamageTaken += HandleUnitDamageTaken;
            unitModel.OnDeath += () => HandleUnitDeath(unitModel);
            unitModel.OnAttackPerformed += HandleUnitAttackPerformed;
        }
        
        /// <summary>
        /// Unsubscribe from unit events
        /// </summary>
        private void UnsubscribeFromUnitEvents(UnitModel unitModel)
        {
            if (unitModel == null) return;
            
            // Unsubscribe from events
            unitModel.OnDamageTaken -= HandleUnitDamageTaken;
            unitModel.OnDeath -= () => HandleUnitDeath(unitModel);
            unitModel.OnAttackPerformed -= HandleUnitAttackPerformed;
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
        private void HandleUnitDamageTaken(float amount, IEntity source)
        {
            _totalDamageReceived += amount;
            OnDamageTaken?.Invoke(amount);
            
            // Check if we need to change combat state
            if (!_isEngaged)
            {
                _isEngaged = true;
                _lastCombatTime = Time.time;
                OnCombatStateChanged?.Invoke(true);
            }
        }
        
        /// <summary>
        /// Handle unit attack event
        /// </summary>
        private void HandleUnitAttackPerformed(IEntity target, float damage)
        {
            _totalDamageDealt += damage;
            OnDamageDealt?.Invoke(damage);
            
            // Update last combat time
            _lastCombatTime = Time.time;
            
            // Check if target died
            var targetHealth = target.GetComponent<HealthComponent>();
            if (targetHealth != null && targetHealth.IsDead)
            {
                _totalKills++;
                _combatStreak++;
            }
        }
        
        /// <summary>
        /// Set the formation type for the squad
        /// ENHANCED: Uses new spacing calculation system
        /// </summary>
        public void SetFormation(FormationType formationType)
        {
            if (_currentFormation == formationType) return;
            
            FormationType oldFormation = _currentFormation;
            _currentFormation = formationType;
            _isReforming = true;
            
            // Recalculate formation offsets for all units with new spacing
            RecalculateAllFormationOffsets();
            
            // Trigger event
            OnFormationChanged?.Invoke(formationType);
            
            Debug.Log($"SquadModel: Changed formation of squad {_squadId} from {oldFormation} to {formationType}, " +
                     $"new spacing: {GetEffectiveFormationSpacing(formationType):F2}");
        }
        
        /// <summary>
        /// Set the target position for the squad
        /// </summary>
        public void SetTargetPosition(Vector3 position)
        {
            if (_currentPosition == position) return;
            
            _currentPosition = position;
            _isMoving = true;
            
            // Update formation positions for all units
            UpdateAllUnitPositions();
            
            // Trigger event
            OnPositionChanged?.Invoke(position);
            
            Debug.Log($"SquadModel: Set target position of squad {_squadId} to {position}");
        }
        
        /// <summary>
        /// Set the target rotation for the squad
        /// </summary>
        public void SetTargetRotation(Quaternion rotation)
        {
            if (_currentRotation == rotation) return;
            
            _currentRotation = rotation;
            
            // Update formation positions with new rotation
            UpdateAllUnitPositions();
        }
        
        /// <summary>
        /// Update all unit positions based on formation
        /// </summary>
        private void UpdateAllUnitPositions()
        {
            foreach (var unitModel in _unitModels)
            {
                int entityId = unitModel.Entity.Id;
                
                if (_formationOffsets.TryGetValue(entityId, out Vector3 offset))
                {
                    var navigationComponent = unitModel.Entity.GetComponent<NavigationComponent>();
                    if (navigationComponent != null)
                    {
                        // Set formation info with squad center, formation offset
                        navigationComponent.SetFormationInfo(
                            _currentPosition, 
                            offset, 
                            NavigationCommandPriority.High
                        );
                    }
                }
            }
        }
        
        /// <summary>
        /// Recalculate formation offsets for all units
        /// ENHANCED: Uses new spacing calculation system
        /// </summary>
        private void RecalculateAllFormationOffsets()
        {
            // Clear existing offsets
            _formationOffsets.Clear();
            
            // Calculate new offsets based on formation type
            switch (_currentFormation)
            {
                case FormationType.Normal:
                    CalculateNormalFormation();
                    break;
                case FormationType.Phalanx:
                    CalculatePhalanxFormation();
                    break;
                case FormationType.Testudo:
                    CalculateTestudoFormation();
                    break;
                default:
                    CalculateNormalFormation(); // Default to normal formation
                    break;
            }
            
            // Apply offsets to formation components
            ApplyFormationOffsets();
        }
        
        /// <summary>
        /// Calculate normal formation (3x3 grid)
        /// ENHANCED: Uses effective spacing calculation
        /// </summary>
        private void CalculateNormalFormation()
        {
            int unitCount = _unitModels.Count;
            if (unitCount == 0) return;
            
            float spacing = GetEffectiveFormationSpacing(FormationType.Normal);
            const int gridWidth = 3;
            
            for (int i = 0; i < _unitModels.Count; i++)
            {
                UnitModel unit = _unitModels[i];
                int entityId = unit.Entity.Id;
                
                int row = i / gridWidth;
                int col = i % gridWidth;
                
                // Center the grid around origin
                float x = (col - 1) * spacing;  // -1, 0, 1 for columns 0, 1, 2
                float z = (row - 1) * spacing;  // -1, 0, 1 for rows 0, 1, 2
                
                Vector3 offset = new Vector3(x, 0, z);
                _formationOffsets[entityId] = offset;
            }
        }
        
        /// <summary>
        /// Calculate phalanx formation (tight combat grid)
        /// ENHANCED: Uses effective spacing calculation
        /// </summary>
        private void CalculatePhalanxFormation()
        {
            int unitCount = _unitModels.Count;
            if (unitCount == 0) return;
            
            float spacing = GetEffectiveFormationSpacing(FormationType.Phalanx);
            int width = Mathf.CeilToInt(Mathf.Sqrt(unitCount));
            int height = Mathf.CeilToInt((float)unitCount / width);
            
            for (int i = 0; i < _unitModels.Count; i++)
            {
                UnitModel unit = _unitModels[i];
                int entityId = unit.Entity.Id;
                
                int row = i / width;
                int col = i % width;
                
                // Center the grid around origin
                float x = (col - (width - 1) * 0.5f) * spacing;
                float z = (row - (height - 1) * 0.5f) * spacing;
                
                Vector3 offset = new Vector3(x, 0, z);
                _formationOffsets[entityId] = offset;
            }
        }
        
        /// <summary>
        /// Calculate testudo formation (very tight defensive grid)
        /// ENHANCED: Uses effective spacing calculation
        /// </summary>
        private void CalculateTestudoFormation()
        {
            // Similar to phalanx but with tighter spacing
            int unitCount = _unitModels.Count;
            if (unitCount == 0) return;
            
            float spacing = GetEffectiveFormationSpacing(FormationType.Testudo);
            int width = Mathf.CeilToInt(Mathf.Sqrt(unitCount));
            int height = Mathf.CeilToInt((float)unitCount / width);
            
            for (int i = 0; i < _unitModels.Count; i++)
            {
                UnitModel unit = _unitModels[i];
                int entityId = unit.Entity.Id;
                
                int row = i / width;
                int col = i % width;
                
                // Center the grid around origin
                float x = (col - (width - 1) * 0.5f) * spacing;
                float z = (row - (height - 1) * 0.5f) * spacing;
                
                Vector3 offset = new Vector3(x, 0, z);
                _formationOffsets[entityId] = offset;
            }
        }
        
        /// <summary>
        /// Apply formation offsets to all units
        /// </summary>
        private void ApplyFormationOffsets()
        {
            foreach (var unitModel in _unitModels)
            {
                UpdateUnitFormationOffset(unitModel);
            }
        }
        
        /// <summary>
        /// Update formation offset for a specific unit
        /// </summary>
        private void UpdateUnitFormationOffset(UnitModel unitModel)
        {
            if (unitModel == null || unitModel.Entity == null) return;
            
            int entityId = unitModel.Entity.Id;
            
            // Get formation offset
            if (_formationOffsets.TryGetValue(entityId, out Vector3 offset))
            {
                var formationComponent = unitModel.Entity.GetComponent<FormationComponent>();
                if (formationComponent != null)
                {
                    formationComponent.SetFormationOffset(offset, true); // Use smooth transition
                }
            }
            else
            {
                // Calculate new offset for this unit
                int slotIndex = _unitModels.IndexOf(unitModel);
                if (slotIndex >= 0)
                {
                    Vector3 newOffset = CalculateFormationOffsetForSlot(slotIndex, unitModel.UnitType);
                    _formationOffsets[entityId] = newOffset;
                    
                    var formationComponent = unitModel.Entity.GetComponent<FormationComponent>();
                    if (formationComponent != null)
                    {
                        formationComponent.SetFormationOffset(newOffset, false); // No transition for initial setup
                    }
                }
            }
        }
        
        /// <summary>
        /// Calculate offset for a specific slot and unit type
        /// </summary>
        private Vector3 CalculateFormationOffsetForSlot(int slotIndex, UnitType unitType)
        {
            float spacing = GetEffectiveFormationSpacing(_currentFormation);
            
            switch (_currentFormation)
            {
                case FormationType.Normal:
                    {
                        const int gridWidth = 3;
                        int row = slotIndex / gridWidth;
                        int col = slotIndex % gridWidth;
                        float x = (col - 1) * spacing;
                        float z = (row - 1) * spacing;
                        return new Vector3(x, 0, z);
                    }
                    
                case FormationType.Phalanx:
                case FormationType.Testudo:
                    {
                        int unitCount = _unitModels.Count;
                        int width = Mathf.CeilToInt(Mathf.Sqrt(unitCount));
                        int row = slotIndex / width;
                        int col = slotIndex % width;
                        float x = (col - (width - 1) * 0.5f) * spacing;
                        float z = (row - ((unitCount - 1) / width) * 0.5f) * spacing;
                        return new Vector3(x, 0, z);
                    }
                    
                default:
                    return Vector3.zero;
            }
        }
        
        /// <summary>
        /// Update formation slots for all units
        /// </summary>
        private void UpdateFormationSlots()
        {
            // Recalculate formation offsets after changing slots
            RecalculateAllFormationOffsets();
        }
        
        /// <summary>
        /// Update the squad's state (should be called each frame)
        /// </summary>
        public void Update()
        {
            // Update position based on average of units
            UpdateCurrentPosition();
            
            // Update moving state
            UpdateMovingState();
            
            // Update engaged state
            UpdateEngagedState();
            
            // Complete reformation if necessary
            if (_isReforming && Time.time - _lastCombatTime > 1.0f)
            {
                _isReforming = false;
            }
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
            if (Vector3.Distance(_currentPosition, averagePosition) > 0.5f && !_isMoving)
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
            int movingCount = 0;
            foreach (var unitModel in _unitModels)
            {
                var navigationComponent = unitModel.Entity.GetComponent<NavigationComponent>();
                if (navigationComponent != null && !navigationComponent.HasReachedDestination)
                {
                    movingCount++;
                }
            }
            
            // If less than 20% of units are still moving, consider the squad stopped
            bool isMoving = movingCount > _unitModels.Count * 0.2f;
            
            // Update moving state
            if (_isMoving != isMoving)
            {
                _isMoving = isMoving;
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
                var combatComponent = unitModel.Entity.GetComponent<CombatComponent>();
                if (combatComponent != null && combatComponent.IsInCombat)
                {
                    anyEngaged = true;
                    _lastCombatTime = Time.time;
                    break;
                }
            }
            
            // If no units are engaged and it's been a while since combat, exit combat state
            bool shouldBeEngaged = anyEngaged || (Time.time - _lastCombatTime < 5.0f);
            
            // Update engaged state
            if (_isEngaged != shouldBeEngaged)
            {
                _isEngaged = shouldBeEngaged;
                OnCombatStateChanged?.Invoke(_isEngaged);
                
                // Reset combat streak if exiting combat
                if (!_isEngaged)
                {
                    _combatStreak = 0;
                }
            }
        }
        
        /// <summary>
        /// Get a unit model by entity ID
        /// </summary>
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
        public List<UnitModel> GetUnitsByType(UnitType unitType)
        {
            if (_unitModelsByType.TryGetValue(unitType, out var units))
            {
                return new List<UnitModel>(units);
            }
            return new List<UnitModel>();
        }
        
        /// <summary>
        /// Get all unit entities
        /// </summary>
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
        public bool HasUnitType(UnitType unitType)
        {
            return _unitModelsByType.TryGetValue(unitType, out var units) && units.Count > 0;
        }
        
        /// <summary>
        /// Calculate the average health percentage of the squad
        /// </summary>
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
        public bool IsViable()
        {
            return _unitModels.Count > 0;
        }
        
        /// <summary>
        /// Update the squad's data
        /// ENHANCED: Reinitialize formation spacing when data changes
        /// </summary>
        public void UpdateData(SquadDataSO newData)
        {
            if (newData == null) return;
            
            _squadData = newData;
            
            // Reinitialize formation spacing with new data
            InitializeFormationSpacing(newData);
            
            // Update formation settings
            _currentFormation = newData.DefaultFormationType;
            
            // Recalculate formation offsets with new spacing
            RecalculateAllFormationOffsets();
            
            Debug.Log($"SquadModel: Updated data for squad {_squadId} to {newData.DisplayName}, " +
                     $"new spacing: {GetEffectiveFormationSpacing(_currentFormation):F2}");
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
            }
            
            // Clear collections
            _unitModels.Clear();
            _unitModelsById.Clear();
            foreach (var type in _unitModelsByType.Keys)
            {
                _unitModelsByType[type].Clear();
            }
            _formationOffsets.Clear();
            
            _isInitialized = false;
            
            Debug.Log($"SquadModel: Cleaned up squad {_squadId}");
        }
        
        /// <summary>
        /// Create a string representation of the squad
        /// ENHANCED: Shows formation spacing info
        /// </summary>
        public override string ToString()
        {
            return $"SquadModel(ID: {_squadId}, Units: {_unitModels.Count}, Formation: {_currentFormation}, " +
                   $"Spacing: {GetEffectiveFormationSpacing(_currentFormation):F2}, Position: {_currentPosition}, " +
                   $"Moving: {_isMoving}, Engaged: {_isEngaged})";
        }
    }
}