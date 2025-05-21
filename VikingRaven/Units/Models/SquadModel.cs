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
    /// Enhanced model class for managing squad data and state
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
        private Dictionary<UnitType, List<UnitModel>> _unitModelsByType = new Dictionary<UnitType, List<UnitModel>>();
        
        // Formation and position tracking
        private FormationType _currentFormation;
        private Vector3 _currentPosition;
        private Quaternion _currentRotation;
        private float _formationSpacing;
        
        // Formation offsets cache
        private Dictionary<int, Vector3> _formationOffsets = new Dictionary<int, Vector3>();
        
        // State tracking
        private bool _isMoving = false;
        private bool _isEngaged = false; // In combat
        private bool _isReforming = false; // Changing formation
        private bool _isInitialized = false;
        
        // Squad statistics
        private int _totalKills = 0;
        private float _totalDamageDealt = 0f;
        private float _totalDamageReceived = 0f;
        private int _combatStreak = 0; // Number of consecutive victories
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
        public float FormationSpacing => _formationSpacing;
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
        /// Constructor for a new squad
        /// </summary>
        /// <param name="squadId">Unique Squad ID</param>
        /// <param name="squadData">Squad data configuration</param>
        /// <param name="position">Initial position</param>
        /// <param name="rotation">Initial rotation</param>
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
            
            // Set default formation from data if available
            _currentFormation = squadData != null 
                ? squadData.DefaultFormationType 
                : FormationType.Line;
                
            // Set formation spacing
            _formationSpacing = squadData != null 
                ? squadData.SpacingMultiplier 
                : 1.0f;
            
            _isInitialized = true;
            
            Debug.Log($"SquadModel: Created new squad with ID {squadId}, formation: {_currentFormation}");
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
            
            // Add to type-specific collection
            UnitType unitType = unitModel.UnitType;
            if (_unitModelsByType.ContainsKey(unitType))
            {
                _unitModelsByType[unitType].Add(unitModel);
            }
            
            // Set formation component if available
            // var formationComponent = unitModel.GetComponent<FormationComponent>();
            // if (formationComponent != null)
            // {
            //     formationComponent.SetSquadId(_squadId);
            //     formationComponent.SetFormationSlot(_unitModels.Count - 1);
            //     formationComponent.SetFormationType(_currentFormation);
            // }
            
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
        /// <param name="unitModels">List of unit models to add</param>
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
                
                // Reset formation component
                // var formationComponent = unitModel.GetComponent<FormationComponent>();
                // if (formationComponent != null)
                // {
                //     formationComponent.SetSquadId(-1);
                // }
                
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
        /// Update formation slots for all units
        /// </summary>
        private void UpdateFormationSlots()
        {
            // for (int i = 0; i < _unitModels.Count; i++)
            // {
            //     var formationComponent = _unitModels[i].GetComponent<FormationComponent>();
            //     if (formationComponent != null)
            //     {
            //         formationComponent.SetFormationSlot(i);
            //     }
            // }
            
            // Recalculate formation offsets after changing slots
            RecalculateAllFormationOffsets();
        }
        
        /// <summary>
        /// Set the formation type for the squad
        /// </summary>
        /// <param name="formationType">New formation type</param>
        public void SetFormation(FormationType formationType)
        {
            if (_currentFormation == formationType) return;
            
            FormationType oldFormation = _currentFormation;
            _currentFormation = formationType;
            _isReforming = true;
            
            // Recalculate formation offsets for all units
            RecalculateAllFormationOffsets();
            
            // Update all unit formation components
            foreach (var unitModel in _unitModels)
            {
                // var formationComponent = unitModel.GetComponent<FormationComponent>();
                // if (formationComponent != null)
                // {
                //     formationComponent.SetFormationType(formationType, true); // Use smooth transition
                // }
            }
            
            // Trigger event
            OnFormationChanged?.Invoke(formationType);
            
            Debug.Log($"SquadModel: Changed formation of squad {_squadId} from {oldFormation} to {formationType}");
            
            // Reset reforming flag after a delay
            // In a MonoBehaviour, we would use a coroutine
            // For now, we'll use a simple approach that assumes this is checked externally
        }
        
        /// <summary>
        /// Set the formation spacing for the squad
        /// </summary>
        /// <param name="spacing">New spacing multiplier</param>
        public void SetFormationSpacing(float spacing)
        {
            if (_formationSpacing == spacing) return;
            
            _formationSpacing = spacing;
            
            // Recalculate formation offsets with new spacing
            RecalculateAllFormationOffsets();
            
            Debug.Log($"SquadModel: Changed formation spacing of squad {_squadId} to {spacing}");
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
            
            // Update formation positions for all units
            UpdateAllUnitPositions();
            
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
                    // var navigationComponent = unitModel.GetComponent<NavigationComponent>();
                    // if (navigationComponent != null)
                    // {
                    //     // Set formation info with squad center, formation offset
                    //     navigationComponent.SetFormationInfo(
                    //         _currentPosition, 
                    //         offset, 
                    //         NavigationCommandPriority.High
                    //     );
                    // }
                }
            }
        }
        
        /// <summary>
        /// Recalculate formation offsets for all units
        /// </summary>
        private void RecalculateAllFormationOffsets()
        {
            // Clear existing offsets
            _formationOffsets.Clear();
            
            // Calculate new offsets based on formation type
            switch (_currentFormation)
            {
                case FormationType.Line:
                    CalculateLineFormation();
                    break;
                case FormationType.Column:
                    CalculateColumnFormation();
                    break;
                case FormationType.Circle:
                    CalculateCircleFormation();
                    break;
                case FormationType.Phalanx:
                    CalculatePhalanxFormation();
                    break;
                case FormationType.Testudo:
                    CalculateTestudoFormation();
                    break;
                case FormationType.Normal:
                    CalculateNormalFormation();
                    break;
                default:
                    CalculateLineFormation(); // Default to line formation
                    break;
            }
            
            // Apply offsets to formation components
            ApplyFormationOffsets();
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
                // Update formation component
                // var formationComponent = unitModel.GetComponent<FormationComponent>();
                // if (formationComponent != null)
                // {
                //     formationComponent.SetFormationOffset(offset, true); // Use smooth transition
                // }
            }
            else
            {
                // Calculate new offset for this unit
                int slotIndex = _unitModels.IndexOf(unitModel);
                if (slotIndex >= 0)
                {
                    Vector3 newOffset = CalculateFormationOffsetForSlot(slotIndex, unitModel.UnitType);
                    _formationOffsets[entityId] = newOffset;
                    
                    // Update formation component
                    // var formationComponent = unitModel.GetComponent<FormationComponent>();
                    // if (formationComponent != null)
                    // {
                    //     formationComponent.SetFormationOffset(newOffset, false); // No transition for initial setup
                    // }
                }
            }
        }
        
        /// <summary>
        /// Calculate offset for a specific slot and unit type
        /// </summary>
        private Vector3 CalculateFormationOffsetForSlot(int slotIndex, UnitType unitType)
        {
            // Get unit counts by type
            int infantryCount = _unitModelsByType[UnitType.Infantry].Count;
            int archerCount = _unitModelsByType[UnitType.Archer].Count;
            int pikeCount = _unitModelsByType[UnitType.Pike].Count;
            
            // Calculate type-specific index
            int typeIndex = 0;
            if (unitType == UnitType.Infantry)
            {
                typeIndex = _unitModelsByType[UnitType.Infantry].IndexOf(_unitModels[slotIndex]);
            }
            else if (unitType == UnitType.Archer)
            {
                typeIndex = _unitModelsByType[UnitType.Archer].IndexOf(_unitModels[slotIndex]);
            }
            else if (unitType == UnitType.Pike)
            {
                typeIndex = _unitModelsByType[UnitType.Pike].IndexOf(_unitModels[slotIndex]);
            }
            
            // Fallback to simple offset calculation
            float spacing = 1.0f * _formationSpacing;
            float x = (slotIndex % 3) * spacing - spacing;
            float z = (slotIndex / 3) * spacing - spacing;
            
            return new Vector3(x, 0, z);
        }
        
        /// <summary>
        /// Calculate line formation (units side by side)
        /// </summary>
        private void CalculateLineFormation()
        {
            int unitCount = _unitModels.Count;
            if (unitCount == 0) return;
            
            float spacing = 1.0f * _formationSpacing;
            float totalWidth = unitCount * spacing;
            float startX = -totalWidth / 2 + spacing / 2;
            
            // Order by unit type priority (Pike > Infantry > Archer)
            List<UnitModel> orderedUnits = OrderUnitsByTypePriority();
            
            for (int i = 0; i < orderedUnits.Count; i++)
            {
                UnitModel unit = orderedUnits[i];
                int entityId = unit.Entity.Id;
                
                // Calculate position in line
                float x = startX + i * spacing;
                Vector3 offset = new Vector3(x, 0, 0);
                
                // Store offset
                _formationOffsets[entityId] = offset;
            }
        }
        
        /// <summary>
        /// Calculate column formation (units one behind the other)
        /// </summary>
        private void CalculateColumnFormation()
        {
            int unitCount = _unitModels.Count;
            if (unitCount == 0) return;
            
            float spacing = 1.0f * _formationSpacing;
            float totalDepth = unitCount * spacing;
            float startZ = totalDepth / 2 - spacing / 2;
            
            // Order by unit type priority (Pike > Infantry > Archer)
            List<UnitModel> orderedUnits = OrderUnitsByTypePriority();
            
            for (int i = 0; i < orderedUnits.Count; i++)
            {
                UnitModel unit = orderedUnits[i];
                int entityId = unit.Entity.Id;
                
                // Calculate position in column
                float z = startZ - i * spacing;
                Vector3 offset = new Vector3(0, 0, z);
                
                // Store offset
                _formationOffsets[entityId] = offset;
            }
        }
        
        /// <summary>
        /// Calculate circle formation (units in a circle around center)
        /// </summary>
        private void CalculateCircleFormation()
        {
            int unitCount = _unitModels.Count;
            if (unitCount == 0) return;
            
            float spacing = 1.0f * _formationSpacing;
            float radius = unitCount * spacing / (2 * Mathf.PI);
            radius = Mathf.Max(radius, 2.0f * spacing); // Ensure minimum radius
            
            // First place units that should be in the center (like archers)
            List<UnitModel> centerUnits = new List<UnitModel>();
            List<UnitModel> circleUnits = new List<UnitModel>();
            
            // Archers in the center, others on the circle
            foreach (var unit in _unitModels)
            {
                if (unit.UnitType == UnitType.Archer)
                {
                    centerUnits.Add(unit);
                }
                else
                {
                    circleUnits.Add(unit);
                }
            }
            
            // Place center units
            for (int i = 0; i < centerUnits.Count; i++)
            {
                UnitModel unit = centerUnits[i];
                int entityId = unit.Entity.Id;
                
                // Calculate position in center (small circle or grid)
                float innerRadius = radius * 0.5f;
                float angle = i * (2 * Mathf.PI / Mathf.Max(centerUnits.Count, 1));
                
                float x = Mathf.Sin(angle) * innerRadius;
                float z = Mathf.Cos(angle) * innerRadius;
                
                Vector3 offset = new Vector3(x, 0, z);
                _formationOffsets[entityId] = offset;
            }
            
            // Place circle units
            for (int i = 0; i < circleUnits.Count; i++)
            {
                UnitModel unit = circleUnits[i];
                int entityId = unit.Entity.Id;
                
                // Calculate position on circle
                float angle = i * (2 * Mathf.PI / Mathf.Max(circleUnits.Count, 1));
                
                float x = Mathf.Sin(angle) * radius;
                float z = Mathf.Cos(angle) * radius;
                
                Vector3 offset = new Vector3(x, 0, z);
                _formationOffsets[entityId] = offset;
            }
        }
        
        /// <summary>
        /// Calculate phalanx formation (tight grid optimized for pike units)
        /// </summary>
        private void CalculatePhalanxFormation()
        {
            int unitCount = _unitModels.Count;
            if (unitCount == 0) return;
            
            float spacing = 0.8f * _formationSpacing; // Tighter spacing for phalanx
            
            // Calculate grid dimensions
            int rows = Mathf.CeilToInt(Mathf.Sqrt(unitCount));
            int cols = Mathf.CeilToInt((float)unitCount / rows);
            
            // Sort units by type (Pike in front, then Infantry, then Archers)
            List<UnitModel> orderedUnits = OrderUnitsByFormationPosition();
            
            // Assign positions in grid
            for (int i = 0; i < orderedUnits.Count; i++)
            {
                UnitModel unit = orderedUnits[i];
                int entityId = unit.Entity.Id;
                
                // Calculate row and column
                int row = i / cols;
                int col = i % cols;
                
                // Calculate position in grid
                float x = (col - (cols - 1) / 2.0f) * spacing;
                float z = (rows - 1) / 2.0f - row * spacing; // Front row has highest z
                
                Vector3 offset = new Vector3(x, 0, z);
                _formationOffsets[entityId] = offset;
            }
        }
        
        /// <summary>
        /// Calculate testudo formation (very tight defensive formation)
        /// </summary>
        private void CalculateTestudoFormation()
        {
            int unitCount = _unitModels.Count;
            if (unitCount == 0) return;
            
            float spacing = 0.6f * _formationSpacing; // Very tight spacing for testudo
            
            // Calculate grid dimensions (more square-like)
            int rows = Mathf.CeilToInt(Mathf.Sqrt(unitCount));
            int cols = Mathf.CeilToInt((float)unitCount / rows);
            
            // Sort units by type (Infantry outside, Archers inside)
            List<UnitModel> borderUnits = new List<UnitModel>();
            List<UnitModel> innerUnits = new List<UnitModel>();
            
            foreach (var unit in _unitModels)
            {
                if (unit.UnitType == UnitType.Infantry || unit.UnitType == UnitType.Pike)
                {
                    borderUnits.Add(unit);
                }
                else
                {
                    innerUnits.Add(unit);
                }
            }
            
            // Assign border positions first
            int borderIndex = 0;
            
            for (int r = 0; r < rows && borderIndex < borderUnits.Count; r++)
            {
                for (int c = 0; c < cols && borderIndex < borderUnits.Count; c++)
                {
                    // Only process border positions (first/last row, first/last column)
                    if (r == 0 || r == rows - 1 || c == 0 || c == cols - 1)
                    {
                        UnitModel unit = borderUnits[borderIndex];
                        int entityId = unit.Entity.Id;
                        
                        // Calculate position
                        float x = (c - (cols - 1) / 2.0f) * spacing;
                        float z = (rows - 1) / 2.0f - r * spacing;
                        
                        Vector3 offset = new Vector3(x, 0, z);
                        _formationOffsets[entityId] = offset;
                        
                        borderIndex++;
                    }
                }
            }
            
            // Assign inner positions
            int innerIndex = 0;
            
            for (int r = 1; r < rows - 1 && innerIndex < innerUnits.Count; r++)
            {
                for (int c = 1; c < cols - 1 && innerIndex < innerUnits.Count; c++)
                {
                    UnitModel unit = innerUnits[innerIndex];
                    int entityId = unit.Entity.Id;
                    
                    // Calculate position
                    float x = (c - (cols - 1) / 2.0f) * spacing;
                    float z = (rows - 1) / 2.0f - r * spacing;
                    
                    Vector3 offset = new Vector3(x, 0, z);
                    _formationOffsets[entityId] = offset;
                    
                    innerIndex++;
                }
            }
            
            // If we have leftover units, place them in a simple grid
            List<UnitModel> leftoverUnits = new List<UnitModel>();
            leftoverUnits.AddRange(borderUnits.GetRange(borderIndex, Mathf.Max(0, borderUnits.Count - borderIndex)));
            leftoverUnits.AddRange(innerUnits.GetRange(innerIndex, Mathf.Max(0, innerUnits.Count - innerIndex)));
            
            for (int i = 0; i < leftoverUnits.Count; i++)
            {
                UnitModel unit = leftoverUnits[i];
                int entityId = unit.Entity.Id;
                
                // Calculate simple grid position
                int row = i / cols;
                int col = i % cols;
                
                float x = (col - (cols - 1) / 2.0f) * spacing;
                float z = -rows * spacing - row * spacing; // Place behind main formation
                
                Vector3 offset = new Vector3(x, 0, z);
                _formationOffsets[entityId] = offset;
            }
        }
        
        /// <summary>
        /// Calculate normal formation (simple grid)
        /// </summary>
        private void CalculateNormalFormation()
        {
            int unitCount = _unitModels.Count;
            if (unitCount == 0) return;
            
            float spacing = 1.0f * _formationSpacing;
            
            // Calculate grid dimensions
            int cols = 3; // Fixed width of 3 units
            int rows = Mathf.CeilToInt((float)unitCount / cols);
            
            // Sort units by type
            List<UnitModel> orderedUnits = OrderUnitsByTypePriority();
            
            // Assign positions in grid
            for (int i = 0; i < orderedUnits.Count; i++)
            {
                UnitModel unit = orderedUnits[i];
                int entityId = unit.Entity.Id;
                
                // Calculate row and column
                int row = i / cols;
                int col = i % cols;
                
                // Calculate position in grid
                float x = (col - (cols - 1) / 2.0f) * spacing;
                float z = -row * spacing;
                
                Vector3 offset = new Vector3(x, 0, z);
                _formationOffsets[entityId] = offset;
            }
        }
        
        /// <summary>
        /// Order units by type priority (Pike > Infantry > Archer)
        /// </summary>
        private List<UnitModel> OrderUnitsByTypePriority()
        {
            List<UnitModel> ordered = new List<UnitModel>();
            
            // Add pikes first
            ordered.AddRange(_unitModelsByType[UnitType.Pike]);
            
            // Add infantry next
            ordered.AddRange(_unitModelsByType[UnitType.Infantry]);
            
            // Add archers last
            ordered.AddRange(_unitModelsByType[UnitType.Archer]);
            
            return ordered;
        }
        
        /// <summary>
        /// Order units by formation position from SquadData
        /// </summary>
        private List<UnitModel> OrderUnitsByFormationPosition()
        {
            // Create a list to hold units in each position
            Dictionary<FormationPosition, List<UnitModel>> positionGroups = new Dictionary<FormationPosition, List<UnitModel>>
            {
                { FormationPosition.Front, new List<UnitModel>() },
                { FormationPosition.Middle, new List<UnitModel>() },
                { FormationPosition.Back, new List<UnitModel>() },
                { FormationPosition.Left, new List<UnitModel>() },
                { FormationPosition.Right, new List<UnitModel>() },
                { FormationPosition.Auto, new List<UnitModel>() }
            };
            
            // Examine the unit compositions from squad data to determine positions
            if (_squadData != null)
            {
                // Create a map of unit type to formation position
                Dictionary<UnitType, FormationPosition> typePositions = new Dictionary<UnitType, FormationPosition>();
                
                foreach (var composition in _squadData.UnitCompositions)
                {
                    if (composition.UnitData != null && composition.FormationPosition != FormationPosition.Auto)
                    {
                        typePositions[composition.UnitData.UnitType] = composition.FormationPosition;
                    }
                }
                
                // Assign units to position groups
                foreach (var unit in _unitModels)
                {
                    FormationPosition position;
                    
                    // Use the position from squad data if available
                    if (typePositions.TryGetValue(unit.UnitType, out position))
                    {
                        positionGroups[position].Add(unit);
                    }
                    else
                    {
                        // Otherwise, use default positions based on unit type
                        switch (unit.UnitType)
                        {
                            case UnitType.Pike:
                                positionGroups[FormationPosition.Front].Add(unit);
                                break;
                            case UnitType.Infantry:
                                positionGroups[FormationPosition.Middle].Add(unit);
                                break;
                            case UnitType.Archer:
                                positionGroups[FormationPosition.Back].Add(unit);
                                break;
                            default:
                                positionGroups[FormationPosition.Auto].Add(unit);
                                break;
                        }
                    }
                }
            }
            else
            {
                // Without squad data, use default positions based on unit type
                foreach (var unit in _unitModels)
                {
                    switch (unit.UnitType)
                    {
                        case UnitType.Pike:
                            positionGroups[FormationPosition.Front].Add(unit);
                            break;
                        case UnitType.Infantry:
                            positionGroups[FormationPosition.Middle].Add(unit);
                            break;
                        case UnitType.Archer:
                            positionGroups[FormationPosition.Back].Add(unit);
                            break;
                        default:
                            positionGroups[FormationPosition.Auto].Add(unit);
                            break;
                    }
                }
            }
            
            // Combine groups in formation order
            List<UnitModel> ordered = new List<UnitModel>();
            ordered.AddRange(positionGroups[FormationPosition.Front]);
            ordered.AddRange(positionGroups[FormationPosition.Middle]);
            ordered.AddRange(positionGroups[FormationPosition.Back]);
            ordered.AddRange(positionGroups[FormationPosition.Left]);
            ordered.AddRange(positionGroups[FormationPosition.Right]);
            ordered.AddRange(positionGroups[FormationPosition.Auto]);
            
            return ordered;
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
                // if (!unitModel.HasReachedDestination)
                // {
                //     movingCount++;
                // }
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
                // if (unitModel.IsInCombat())
                // {
                //     anyEngaged = true;
                //     _lastCombatTime = Time.time;
                //     break;
                // }
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
            if (_unitModelsByType.TryGetValue(unitType, out var units))
            {
                return new List<UnitModel>(units);
            }
            return new List<UnitModel>();
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
            return _unitModelsByType.TryGetValue(unitType, out var units) && units.Count > 0;
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
        /// Update the squad's data
        /// </summary>
        /// <param name="newData">New squad data configuration</param>
        public void UpdateData(SquadDataSO newData)
        {
            if (newData == null) return;
            
            _squadData = newData;
            
            // Update formation settings
            _currentFormation = newData.DefaultFormationType;
            _formationSpacing = newData.SpacingMultiplier;
            
            // Recalculate formation offsets
            RecalculateAllFormationOffsets();
            
            Debug.Log($"SquadModel: Updated data for squad {_squadId} to {newData.DisplayName}");
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
        /// </summary>
        public override string ToString()
        {
            return $"SquadModel(ID: {_squadId}, Units: {_unitModels.Count}, Formation: {_currentFormation}, " +
                   $"Position: {_currentPosition}, Moving: {_isMoving}, Engaged: {_isEngaged})";
        }
    }
}