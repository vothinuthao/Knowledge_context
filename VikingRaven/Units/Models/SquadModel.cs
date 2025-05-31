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
    /// FIXED: SquadModel with proper formation positioning logic
    /// Ensures units are properly positioned in formation instead of stacking on one spot
    /// </summary>
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
        
        // FIXED: Formation offsets cache with proper calculation
        private Dictionary<int, Vector3> _formationOffsets = new Dictionary<int, Vector3>();
        private bool _formationDirty = true; // Track when formation needs recalculation
        
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
        
        public SquadModel(int squadId, SquadDataSO squadData, Vector3 position, Quaternion rotation)
        {
            _squadId = squadId;
            _squadData = squadData;
            _currentPosition = position;
            _currentRotation = rotation;
            _unitModelsByType[UnitType.Infantry] = new List<UnitModel>();
            _unitModelsByType[UnitType.Archer] = new List<UnitModel>();
            _unitModelsByType[UnitType.Pike] = new List<UnitModel>();
            InitializeFormationSpacing(squadData);
            _currentFormation = squadData != null 
                ? squadData.DefaultFormationType 
                : FormationType.Normal;
                
            _isInitialized = true;
            
            Debug.Log($"SquadModel: Created squad {squadId} with formation {_currentFormation}, " +
                     $"spacing: {GetEffectiveFormationSpacing(_currentFormation):F2}");
        }
        private void InitializeFormationSpacing(SquadDataSO squadData)
        {
            if (squadData != null)
            {
                _formationSpacing = squadData.SpacingMultiplier;
                _formationSpacingConfig = squadData.FormationSpacingConfig;
            }
            else
            {
                _formationSpacing = 1.0f;
                _formationSpacingConfig = null;
            }
        }

        private float GetEffectiveFormationSpacing(FormationType formationType)
        {
            if (_squadData != null)
            {
                return _squadData.GetFormationSpacing(formationType);
            }
            
            float baseSpacing = GetLegacyFormationSpacing(formationType);
            return baseSpacing * _formationSpacing;
        }
        
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
        public float GetPositionTolerance()
        {
            if (_squadData != null)
            {
                return _squadData.GetPositionTolerance();
            }
            
            return 0.3f; 
        }
        public void AddUnit(UnitModel unitModel)
        {
            if (unitModel == null || unitModel.Entity == null) return;
            if (_unitModelsById.ContainsKey(unitModel.Entity.Id)) return;
            
            _unitModels.Add(unitModel);
            _unitModelsById[unitModel.Entity.Id] = unitModel;
            
            UnitType unitType = unitModel.UnitType;
            if (_unitModelsByType.ContainsKey(unitType))
            {
                _unitModelsByType[unitType].Add(unitModel);
            }
            _formationDirty = true;
            SubscribeToUnitEvents(unitModel);
            OnUnitAdded?.Invoke(unitModel);
        }
        public void AddUnits(List<UnitModel> unitModels)
        {
            if (unitModels == null) return;
            
            foreach (var unitModel in unitModels)
            {
                AddUnit(unitModel);
            }
            
            ForceRecalculateFormation();
        }
        
        public void RemoveUnit(UnitModel unitModel)
        {
            if (unitModel == null || unitModel.Entity == null) return;
            
            int entityId = unitModel.Entity.Id;
            
            if (_unitModelsById.ContainsKey(entityId))
            {
                _unitModels.Remove(unitModel);
                _unitModelsById.Remove(entityId);
                
                UnitType unitType = unitModel.UnitType;
                if (_unitModelsByType.ContainsKey(unitType))
                {
                    _unitModelsByType[unitType].Remove(unitModel);
                }
                _formationOffsets.Remove(entityId);
                
                _formationDirty = true;
                UnsubscribeFromUnitEvents(unitModel);
                OnUnitRemoved?.Invoke(unitModel);
                ForceRecalculateFormation();
            }
        }
        private void SubscribeToUnitEvents(UnitModel unitModel)
        {
            if (unitModel == null) return;
            unitModel.OnDamageTaken += HandleUnitDamageTaken;
            unitModel.OnDeath += () => HandleUnitDeath(unitModel);
            unitModel.OnAttackPerformed += HandleUnitAttackPerformed;
        }
        private void UnsubscribeFromUnitEvents(UnitModel unitModel)
        {
            if (unitModel == null) return;
            unitModel.OnDamageTaken -= HandleUnitDamageTaken;
            // ReSharper disable once EventUnsubscriptionViaAnonymousDelegate
            unitModel.OnDeath -= () => HandleUnitDeath(unitModel);
            unitModel.OnAttackPerformed -= HandleUnitAttackPerformed;
        }
        private void HandleUnitDeath(UnitModel unitModel)
        {
            if (unitModel == null) return;
            
            OnUnitDied?.Invoke(unitModel);
            
            RemoveUnit(unitModel);
            if (_unitModels.Count == 0)
            {
                Debug.Log($"SquadModel: Squad {_squadId} has no more units");
            }
        }
        
        private void HandleUnitDamageTaken(float amount, IEntity source)
        {
            _totalDamageReceived += amount;
            OnDamageTaken?.Invoke(amount);
            if (!_isEngaged)
            {
                _isEngaged = true;
                _lastCombatTime = Time.time;
                OnCombatStateChanged?.Invoke(true);
            }
        }
        private void HandleUnitAttackPerformed(IEntity target, float damage)
        {
            _totalDamageDealt += damage;
            OnDamageDealt?.Invoke(damage);
            
            _lastCombatTime = Time.time;
            
            // Check if target died
            var targetHealth = target.GetComponent<HealthComponent>();
            if (targetHealth != null && targetHealth.IsDead)
            {
                _totalKills++;
                _combatStreak++;
            }
        }
        public void SetFormation(FormationType formationType)
        {
            if (_currentFormation == formationType) return;
            
            FormationType oldFormation = _currentFormation;
            _currentFormation = formationType;
            _isReforming = true;
            _formationDirty = true;
            ForceRecalculateFormation();
            
            OnFormationChanged?.Invoke(formationType);
            
            Debug.Log($"SquadModel: Changed formation of squad {_squadId} from {oldFormation} to {formationType}, " +
                     $"new spacing: {GetEffectiveFormationSpacing(formationType):F2}");
        }
        public void SetTargetPosition(Vector3 position)
        {
            if (Vector3.Distance(_currentPosition, position) < 0.1f) return;
            
            _currentPosition = position;
            _isMoving = true;
            _formationDirty = true;
            
            ForceRecalculateFormation();
            
            OnPositionChanged?.Invoke(position);
            
            Debug.Log($"SquadModel: Set target position of squad {_squadId} to {position}");
        }
        
        public void SetTargetRotation(Quaternion rotation)
        {
            if (Quaternion.Angle(_currentRotation, rotation) < 1f) return;
            
            _currentRotation = rotation;
            _formationDirty = true;
            
            ForceRecalculateFormation();
        }
        public void ForceRecalculateFormation()
        {
            if (_unitModels.Count == 0) return;
            
            // Step 1: Clear existing offsets
            _formationOffsets.Clear();
            CalculateFormationOffsets();
            
            ApplyFormationOffsetsToUnits();
            
            // Step 4: Mark formation as clean
            _formationDirty = false;
            
            Debug.Log($"SquadModel: Force recalculated formation for squad {_squadId} with {_unitModels.Count} units " +
                     $"in {_currentFormation} formation");
        }
        
        private void CalculateFormationOffsets()
        {
            if (_unitModels.Count == 0) return;
            
            float spacing = GetEffectiveFormationSpacing(_currentFormation);
            
            switch (_currentFormation)
            {
                case FormationType.Normal:
                    CalculateNormalFormationOffsets(spacing);
                    break;
                case FormationType.Phalanx:
                    CalculatePhalanxFormationOffsets(spacing);
                    break;
                case FormationType.Testudo:
                    CalculateTestudoFormationOffsets(spacing);
                    break;
                default:
                    CalculateNormalFormationOffsets(spacing);
                    break;
            }
        }
        private void CalculateNormalFormationOffsets(float spacing)
        {
            const int gridWidth = 3;
            
            for (int i = 0; i < _unitModels.Count; i++)
            {
                UnitModel unit = _unitModels[i];
                int entityId = unit.Entity.Id;
                
                // FIXED: Assign leader to center position (slot 4 in 3x3 grid)
                int slotIndex;
                if (i == 0) // First unit is leader, goes to center
                {
                    slotIndex = 4; // Center of 3x3 grid
                }
                else if (i <= 8) // Other units fill remaining slots
                {
                    slotIndex = i < 4 ? i : i + 1; // Skip slot 4 (reserved for leader)
                }
                else // Extra units beyond 9 wrap around
                {
                    slotIndex = (i - 1) % 8; // Wrap around non-center positions
                    if (slotIndex >= 4) slotIndex++; // Skip center slot
                }
                
                int row = slotIndex / gridWidth;
                int col = slotIndex % gridWidth;
                
                // Center the grid around origin (squad center)
                float x = (col - 1) * spacing; 
                float z = (row - 1) * spacing;
                
                Vector3 offset = new Vector3(x, 0, z);
                _formationOffsets[entityId] = offset;
                
                Debug.Log($"SquadModel: Unit {entityId} (slot {i}) assigned to grid position [{row},{col}] " +
                         $"with offset {offset}");
            }
        }
        
        private void CalculatePhalanxFormationOffsets(float spacing)
        {
            int unitCount = _unitModels.Count;
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
        
        private void CalculateTestudoFormationOffsets(float spacing)
        {
            // Similar to phalanx but with the tighter spacing already applied
            CalculatePhalanxFormationOffsets(spacing);
        }
        
        private void ApplyFormationOffsetsToUnits()
        {
            foreach (var unitModel in _unitModels)
            {
                int entityId = unitModel.Entity.Id;
                
                if (_formationOffsets.TryGetValue(entityId, out Vector3 offset))
                {
                    // FIXED: Apply to FormationComponent first
                    var formationComponent = unitModel.Entity.GetComponent<FormationComponent>();
                    if (formationComponent != null)
                    {
                        formationComponent.SetFormationOffset(offset, true); // Use smooth transition
                        formationComponent.SetSquadId(_squadId);
                        formationComponent.SetFormationType(_currentFormation);
                    }
                    
                    // FIXED: Apply to NavigationComponent to actually move the unit
                    var navigationComponent = unitModel.Entity.GetComponent<NavigationComponent>();
                    if (navigationComponent != null)
                    {
                        navigationComponent.SetFormationInfo(
                            _currentPosition,  // Squad center
                            offset,           // Formation offset
                            NavigationCommandPriority.High
                        );
                        
                        Debug.Log($"SquadModel: Applied formation offset {offset} to unit {entityId} " +
                                 $"at squad position {_currentPosition}");
                    }
                }
            }
        }
        
        public void Update()
        {
            if (_formationDirty)
            {
                ForceRecalculateFormation();
            }
            UpdateCurrentPosition();
            UpdateMovingState();
            UpdateEngagedState();
            if (_isReforming && Time.time - _lastCombatTime > 1.0f)
            {
                _isReforming = false;
            }
        }
        
        private void UpdateCurrentPosition()
        {
            if (_unitModels.Count == 0) return;
            
            Vector3 sum = Vector3.zero;
            int validCount = 0;
            
            foreach (var unitModel in _unitModels)
            {
                if (unitModel?.Entity != null)
                {
                    var transformComponent = unitModel.Entity.GetComponent<TransformComponent>();
                    if (transformComponent != null)
                    {
                        sum += transformComponent.Position;
                        validCount++;
                    }
                }
            }
            
            if (validCount > 0)
            {
                Vector3 averagePosition = sum / validCount;
                if (Vector3.Distance(_currentPosition, averagePosition) > 0.5f && !_isMoving)
                {
                    _currentPosition = averagePosition;
                }
            }
        }
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
            _formationDirty = true;
            
            // FIXED: Force immediate recalculation with new spacing
            ForceRecalculateFormation();
            
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