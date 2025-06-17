using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;
using VikingRaven.Units.Models;

namespace VikingRaven.Units.Models
{
    public class SquadModel
    {
        #region Formation Index Configuration
        private static readonly int[] FORMATION_PRIORITY_ORDER = new int[]
        {
            4, // Unit 0 (Leader) -> Center position (slot 4)
            1, // Unit 1 -> Front center (slot 1) - support leader from front
            7, // Unit 2 -> Back center (slot 7) - support leader from back
            3, // Unit 3 -> Middle left (slot 3) - protect leader's left flank
            5, // Unit 4 -> Middle right (slot 5) - protect leader's right flank
            0, // Unit 5 -> Front left (slot 0) - front line corner
            2, // Unit 6 -> Front right (slot 2) - front line corner
            6, // Unit 7 -> Back left (slot 6) - rear guard corner
            8  // Unit 8 -> Back right (slot 8) - rear guard corner
        };

        #endregion

        private int _squadId;
        private SquadDataSO _squadData;
        
        private List<UnitModel> _unitModels = new List<UnitModel>();
        private Dictionary<int, UnitModel> _unitModelsById = new Dictionary<int, UnitModel>();
        private Dictionary<UnitType, List<UnitModel>> _unitModelsByType = new Dictionary<UnitType, List<UnitModel>>();
        private FormationType _currentFormation;
        private Vector3 _currentPosition;
        private Quaternion _currentRotation;
        private float _formationSpacing;
        private FormationSpacingConfig _formationSpacingConfig;
        
        private Dictionary<int, Vector3> _formationOffsets = new Dictionary<int, Vector3>();
        private bool _formationDirty = true;
        
        private bool _isMoving = false;
        private bool _isEngaged = false;
        private bool _isReforming = false;
        private bool _isInitialized = false;
        
        private int _totalKills = 0;
        private float _totalDamageDealt = 0f;
        private float _totalDamageReceived = 0f;
        private int _combatStreak = 0;
        private float _lastCombatTime = 0f;
        
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
            if (targetHealth != null && !targetHealth.IsAlive)
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
            Debug.Log($"SquadModel: Calculating Normal formation for {_unitModels.Count} units with spacing {spacing:F2}");
            
            for (int unitIndex = 0; unitIndex < _unitModels.Count; unitIndex++)
            {
                UnitModel unit = _unitModels[unitIndex];
                if (unit?.Entity == null) continue;
                
                int entityId = unit.Entity.Id;
                
                // FIXED: Use logical formation slot assignment
                int formationSlot = GetFormationSlotForUnit(unitIndex);
                
                // Calculate offset from formation slot
                Vector3 offset = CalculateOffsetFromSlot(formationSlot, spacing);
                
                // Store offset
                _formationOffsets[entityId] = offset;
                
                // Update formation component with correct data
                UpdateUnitFormationData(unit, formationSlot, offset);
                
                Debug.Log($"SquadModel: Unit {unitIndex} (ID:{entityId}) -> Formation Slot {formationSlot} -> Offset {offset}");
            }
            
            Debug.Log($"SquadModel: Completed Normal formation calculation with {_formationOffsets.Count} offsets");
        }

        private int GetFormationSlotForUnit(int unitIndex)
        {
            // Use priority order for first 9 units
            if (unitIndex < FORMATION_PRIORITY_ORDER.Length)
            {
                return FORMATION_PRIORITY_ORDER[unitIndex];
            }
            
            // For additional units beyond 9, use remaining slots
            // Create set of used slots
            var usedSlots = new HashSet<int>();
            for (int i = 0; i < FORMATION_PRIORITY_ORDER.Length && i < _unitModels.Count; i++)
            {
                usedSlots.Add(FORMATION_PRIORITY_ORDER[i]);
            }
            
            // Find first available slot for extra units
            for (int slot = 0; slot < 9; slot++)
            {
                if (!usedSlots.Contains(slot))
                {
                    return slot;
                }
            }
            
            // Fallback: wrap around
            return unitIndex % 9;
        }

        private Vector3 CalculateOffsetFromSlot(int slotIndex, float spacing)
        {
            // Ensure slot index is valid
            slotIndex = Mathf.Clamp(slotIndex, 0, 8);
            
            // Convert slot index to grid coordinates
            // Grid layout: [0][1][2]
            //              [3][4][5]
            //              [6][7][8]
            int row = slotIndex / 3;    // 0, 1, 2
            int col = slotIndex % 3;    // 0, 1, 2
            
            // Convert grid coordinates to world offset (centered around origin)
            float x = (col - 1) * spacing;  // -spacing, 0, +spacing
            float z = (row - 1) * spacing;  // -spacing, 0, +spacing
            
            return new Vector3(x, 0, z);
        }

        private void UpdateUnitFormationData(UnitModel unit, int formationSlot, Vector3 offset)
        {
            var formationComponent = unit.Entity.GetComponent<FormationComponent>();
            if (formationComponent == null) return;
            
            // Set formation slot index - THIS IS THE KEY FIX!
            formationComponent.SetFormationSlot(formationSlot);
            
            // Set formation offset
            formationComponent.SetFormationOffset(offset, true); // Use smooth transition
            
            formationComponent.SetSquadId(_squadId);
            formationComponent.SetFormationType(_currentFormation);
            
            FormationRole role = DetermineFormationRole(formationSlot);
            formationComponent.SetFormationRole(role);
            
            Debug.Log($"SquadModel: Updated FormationComponent for Unit {unit.Entity.Id}: " +
                     $"Slot={formationSlot}, Role={role}, Offset={offset}");
        }

        private FormationRole DetermineFormationRole(int slotIndex)
        {
            return slotIndex switch
            {
                4 => FormationRole.Leader,
                1 or 7 => FormationRole.FrontLine,
                3 or 5 => FormationRole.Flanker,
                0 or 2 => FormationRole.Support,
                6 or 8 => FormationRole.Reserve, 
                _ => FormationRole.Follower
            };
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
                
                float x = (col - (width - 1) * 0.5f) * spacing;
                float z = (row - (height - 1) * 0.5f) * spacing;
                
                Vector3 offset = new Vector3(x, 0, z);
                _formationOffsets[entityId] = offset;
                
                // Update formation component
                var formationComponent = unit.Entity.GetComponent<FormationComponent>();
                if (formationComponent != null)
                {
                    formationComponent.SetFormationSlot(i);
                    formationComponent.SetFormationOffset(offset, false);
                    formationComponent.SetSquadId(_squadId);
                    formationComponent.SetFormationType(_currentFormation, false);
                    
                    FormationRole role = i == 0 ? FormationRole.Leader : 
                                       (i < width) ? FormationRole.FrontLine : FormationRole.Support;
                    formationComponent.SetFormationRole(role);
                }
            }
        }
        
        private void CalculateTestudoFormationOffsets(float spacing)
        {
            CalculatePhalanxFormationOffsets(spacing);
        }
        
        private void ApplyFormationOffsetsToUnits()
        {
            foreach (var unitModel in _unitModels)
            {
                int entityId = unitModel.Entity.Id;
                
                if (_formationOffsets.TryGetValue(entityId, out Vector3 offset))
                {
                    var formationComponent = unitModel.Entity.GetComponent<FormationComponent>();
                    if (formationComponent != null)
                    {
                        formationComponent.SetFormationOffset(offset, true); // Use smooth transition
                        formationComponent.SetSquadId(_squadId);
                        formationComponent.SetFormationType(_currentFormation);
                    }
                    
                    var navigationComponent = unitModel.Entity.GetComponent<NavigationComponent>();
                    if (navigationComponent != null)
                    {
                        navigationComponent.SetFormationInfo(
                            _currentPosition,
                            offset,
                            NavigationCommandPriority.High
                        );
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
            int movingCount = 0;
            foreach (var unitModel in _unitModels)
            {
                var navigationComponent = unitModel.Entity.GetComponent<NavigationComponent>();
                if (navigationComponent != null && !navigationComponent.HasReachedDestination)
                {
                    movingCount++;
                }
            }
            
            bool isMoving = movingCount > _unitModels.Count * 0.2f;
            
            // Update moving state
            if (_isMoving != isMoving)
            {
                _isMoving = isMoving;
            }
        }
        
        private void UpdateEngagedState()
        {
            if (_unitModels.Count == 0)
            {
                _isEngaged = false;
                return;
            }
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
        
        public List<IEntity> GetAllUnitEntities()
        {
            List<IEntity> result = new List<IEntity>();
            foreach (var unitModel in _unitModels)
            {
                result.Add(unitModel.Entity);
            }
            return result;
        }
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
        public bool IsViable()
        {
            return _unitModels.Count > 0;
        }
        public void Cleanup()
        {
            foreach (var unitModel in _unitModels)
            {
                UnsubscribeFromUnitEvents(unitModel);
            }
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
        public List<UnitModel> GetAllUnits()
        {
            return new List<UnitModel>(_unitModels);
        }
    }
}