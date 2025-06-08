using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using Core.Utils;
using VikingRaven.Core.Data;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Data;
using VikingRaven.Units.Models;
using VikingRaven.Units.Components;
using Random = UnityEngine.Random;

namespace VikingRaven.Core.Factory
{
    public class SquadFactory : Singleton<SquadFactory>
    {
        #region Configuration - Simplified

        [TitleGroup("Formation Spawn Settings")]
        [Tooltip("Enable direct formation positioning on spawn")]
        [SerializeField, ToggleLeft]
        private bool _useDirectFormationSpawn = true;
        
        [Tooltip("Formation spacing for spawn positioning")]
        [SerializeField, Range(1.5f, 4f)]
        private float _baseFormationSpacing = 2.5f;
        
        [Tooltip("Random offset for natural positioning")]
        [SerializeField, Range(0f, 0.5f)]
        private float _spawnRandomOffset = 0.2f;
        
        [Tooltip("Parent container for all squads")]
        [SerializeField]
        private Transform _squadParentContainer;

        #endregion

        #region Dependencies
        
        [TitleGroup("Dependencies")]
        [SerializeField, Required]
        private UnitFactory _unitFactory;
        
        [SerializeField, Required]
        private DataManager _dataManager;

        #endregion

        #region Runtime Data - Simplified
        
        [TitleGroup("Runtime Information")]
        [ShowInInspector, ReadOnly]
        private int ActiveSquadsCount => _activeSquads?.Count ?? 0;
        
        private Dictionary<int, SquadModel> _activeSquads = new Dictionary<int, SquadModel>();
        private Dictionary<int, GameObject> _squadContainers = new Dictionary<int, GameObject>();
        private int _nextSquadId = 1;

        #endregion

        #region Events
        
        public event System.Action<SquadModel> OnSquadCreated;
        public event System.Action<SquadModel> OnSquadDisbanded;

        #endregion

        #region Initialization

        protected override void OnInitialize()
        {
            base.OnInitialize();
            ValidateDependencies();
            SetupSquadParentContainer();
        }

        [Obsolete("Obsolete")]
        private void ValidateDependencies()
        {
            if (_unitFactory == null)
                _unitFactory = FindObjectOfType<UnitFactory>();
            
            if (_dataManager == null)
                _dataManager = DataManager.Instance;
        }

        private void SetupSquadParentContainer()
        {
            if (_squadParentContainer == null)
            {
                GameObject container = new GameObject("Squad Containers");
                container.transform.SetParent(transform);
                _squadParentContainer = container.transform;
            }
        }

        #endregion

        #region Core Factory Methods - Simplified
        public SquadModel CreateSquad(uint squadDataId, Vector3 position, Quaternion rotation)
        {
            SquadDataSO squadData = _dataManager.GetSquadData(squadDataId);
            if (squadData == null)
            {
                return null;
            }
            
            int squadId = _nextSquadId++;
            
            SquadModel squadModel = new SquadModel(squadId, squadData, position, rotation);
            GameObject squadContainer = CreateSquadContainer(squadId, squadData, position, rotation);
            List<UnitModel> squadUnits = CreateUnitsWithFormationIndex(
                squadData, 
                position, 
                rotation, 
                squadId, 
                squadContainer
            );
            
            if (squadUnits.Count == 0)
            {
                Debug.LogError($"SquadFactory: Failed to create units for squad: {squadDataId}");
                CleanupSquadContainer(squadId);
                return null;
            }
            
            // Add units to squad
            squadModel.AddUnits(squadUnits);
            
            // Track squad
            _activeSquads[squadId] = squadModel;
            _squadContainers[squadId] = squadContainer;
            
            // Trigger events
            OnSquadCreated?.Invoke(squadModel);
            
            Debug.Log($"SquadFactory: Created squad {squadId} with {squadUnits.Count} units " +
                     $"in {squadData.DefaultFormationType} formation");
            
            return squadModel;
        }
        
        public void DisbandSquad(int squadId)
        {
            if (!_activeSquads.TryGetValue(squadId, out SquadModel squadModel))
            {
                Debug.LogWarning($"SquadFactory: Squad not found: {squadId}");
                return;
            }
            
            OnSquadDisbanded?.Invoke(squadModel);
            
            // Return units to pool
            foreach (var entity in squadModel.GetAllUnitEntities())
            {
                if (entity != null)
                    _unitFactory.ReturnUnit(entity);
            }
            
            // Cleanup squad
            CleanupSquadContainer(squadId);
            squadModel.Cleanup();
            
            _activeSquads.Remove(squadId);
            
            Debug.Log($"SquadFactory: Disbanded squad {squadId}");
        }

        #endregion

        #region Squad Container Management

        private GameObject CreateSquadContainer(int squadId, SquadDataSO squadData, Vector3 position, Quaternion rotation)
        {
            GameObject container = new GameObject($"Squad_{squadId}_{squadData.DisplayName}");
            container.transform.SetParent(_squadParentContainer);
            container.transform.position = position;
            container.transform.rotation = rotation;
            
            // Add squad identifier
            var identifier = container.AddComponent<SquadIdentifier>();
            identifier.SetSquadData(squadId, squadData);
            
            return container;
        }

        private void CleanupSquadContainer(int squadId)
        {
            if (_squadContainers.TryGetValue(squadId, out GameObject container))
            {
                if (container != null)
                    DestroyImmediate(container);
                _squadContainers.Remove(squadId);
            }
        }

        #endregion

        #region Formation-Based Unit Creation - SIMPLIFIED CORE LOGIC

        /// <summary>
        /// SIMPLIFIED: Create units with direct formation index assignment
        /// Each unit gets correct formation index, slot, and position immediately
        /// </summary>
        private List<UnitModel> CreateUnitsWithFormationIndex(SquadDataSO squadData, Vector3 squadPosition, 
            Quaternion squadRotation, int squadId, GameObject squadContainer)
        {
            List<UnitModel> squadUnits = new List<UnitModel>();
            
            if (_unitFactory == null) return squadUnits;
            
            // Calculate total units for formation planning
            int totalUnits = CalculateTotalUnits(squadData);
            if (totalUnits == 0) return squadUnits;
            
            // Get formation spacing for this squad type
            float spacing = GetFormationSpacing(squadData.DefaultFormationType, squadData);
            
            // Generate formation positions based on formation type
            Vector3[] formationPositions = GenerateFormationPositions(
                squadData.DefaultFormationType, 
                totalUnits, 
                spacing, 
                squadPosition, 
                squadRotation
            );
            
            int unitIndex = 0;
            
            // Create units with formation data
            foreach (var composition in squadData.UnitCompositions)
            {
                if (!composition.UnitData) continue;
                
                for (int i = 0; i < composition.Count; i++)
                {
                    if (unitIndex >= formationPositions.Length) break;
                    
                    // Get spawn position with small random offset
                    Vector3 spawnPosition = formationPositions[unitIndex];
                    if (_spawnRandomOffset > 0)
                    {
                        spawnPosition += GetRandomOffset();
                    }
                    
                    // Create unit
                    IEntity unitEntity = _unitFactory.CreateUnit(
                        composition.UnitData.UnitId, 
                        spawnPosition, 
                        squadRotation
                    );
                    
                    if (unitEntity != null)
                    {
                        // Set unit parent
                        if (squadContainer != null)
                        {
                            var unitObj = unitEntity as MonoBehaviour;
                            if (unitObj != null)
                                unitObj.transform.SetParent(squadContainer.transform);
                        }
                        
                        // Setup unit with formation data immediately
                        UnitModel unitModel = _unitFactory.GetUnitModel(unitEntity);
                        if (unitModel != null)
                        {
                            unitModel.SetSquadId(squadId);
                            squadUnits.Add(unitModel);
                            
                            // CORE: Setup formation component with correct index
                            SetupFormationComponentDirect(
                                unitEntity, 
                                squadId, 
                                unitIndex, 
                                squadData.DefaultFormationType,
                                formationPositions[unitIndex] - squadPosition // Local offset
                            );
                        }
                    }
                    
                    unitIndex++;
                }
            }
            
            Debug.Log($"SquadFactory: Created {squadUnits.Count} units with {squadData.DefaultFormationType} " +
                     $"formation, spacing: {spacing}");
            
            return squadUnits;
        }

        #endregion

        #region Formation Position Generation - CORE LOGIC
        private Vector3[] GenerateFormationPositions(FormationType formationType, int unitCount, 
            float spacing, Vector3 squadCenter, Quaternion squadRotation)
        {
            Vector3[] localPositions = new Vector3[unitCount];
            
            // Generate local formation positions
            switch (formationType)
            {
                case FormationType.Normal:
                    GenerateNormalFormationPositions(localPositions, spacing);
                    break;
                    
                case FormationType.Phalanx:
                    GeneratePhalanxFormationPositions(localPositions, spacing);
                    break;
                    
                case FormationType.Testudo:
                    GenerateTestudoFormationPositions(localPositions, spacing);
                    break;
                    
                default:
                    GenerateNormalFormationPositions(localPositions, spacing);
                    break;
            }
            
            // Convert to world positions
            Vector3[] worldPositions = new Vector3[unitCount];
            for (int i = 0; i < unitCount; i++)
            {
                Vector3 rotatedPosition = squadRotation * localPositions[i];
                worldPositions[i] = squadCenter + rotatedPosition;
            }
            
            return worldPositions;
        }

        /// <summary>
        /// Generate Normal formation (3x3 grid)
        /// </summary>
        private void GenerateNormalFormationPositions(Vector3[] positions, float spacing)
        {
            int count = positions.Length;
            const int gridWidth = 3;
            
            for (int i = 0; i < count; i++)
            {
                int row = i / gridWidth;
                int col = i % gridWidth;
                
                float x = (col - 1) * spacing;  // -1, 0, 1
                float z = (row - 1) * spacing;  // -1, 0, 1
                
                positions[i] = new Vector3(x, 0, z);
            }
        }

        /// <summary>
        /// Generate Phalanx formation
        /// </summary>
        private void GeneratePhalanxFormationPositions(Vector3[] positions, float spacing)
        {
            int count = positions.Length;
            int width = Mathf.CeilToInt(Mathf.Sqrt(count));
            
            for (int i = 0; i < count; i++)
            {
                int row = i / width;
                int col = i % width;
                
                float x = (col - (width - 1) * 0.5f) * spacing;
                float z = (row - (Mathf.CeilToInt((float)count / width) - 1) * 0.5f) * spacing;
                
                positions[i] = new Vector3(x, 0, z);
            }
        }

        /// <summary>
        /// Generate Testudo formation
        /// </summary>
        private void GenerateTestudoFormationPositions(Vector3[] positions, float spacing)
        {
            GeneratePhalanxFormationPositions(positions, spacing);
        }

        #endregion

        #region Formation Component Setup
        private void SetupFormationComponentDirect(IEntity unitEntity, int squadId, int formationIndex, 
            FormationType formationType, Vector3 localOffset)
        {
            var formationComponent = unitEntity.GetComponent<FormationComponent>();
            if (formationComponent == null) return;
            
            formationComponent.SetSquadId(squadId);
            formationComponent.SetFormationSlot(formationIndex);
            formationComponent.SetFormationType(formationType, false);
            formationComponent.SetFormationOffset(localOffset );
            
            FormationRole role = DetermineFormationRole(formationIndex, formationType);
            formationComponent.SetFormationRole(role);
            
            Debug.Log($"SquadFactory: Unit {unitEntity.Id} assigned formation index {formationIndex}, " +
                     $"offset {localOffset}, role {role}");
        }
        private FormationRole DetermineFormationRole(int index, FormationType formationType)
        {
            if (index == 0) return FormationRole.Leader;
            
            switch (formationType)
            {
                case FormationType.Normal:
                    return index <= 2 ? FormationRole.FrontLine : FormationRole.Follower;
                    
                case FormationType.Phalanx:
                    return index <= 3 ? FormationRole.FrontLine : FormationRole.Support;
                    
                case FormationType.Testudo:
                    return FormationRole.Support;
                    
                default:
                    return FormationRole.Follower;
            }
        }

        #endregion

        #region Helper Methods

        private int CalculateTotalUnits(SquadDataSO squadData)
        {
            int total = 0;
            foreach (var composition in squadData.UnitCompositions)
            {
                if (composition.UnitData != null)
                    total += composition.Count;
            }
            return total;
        }

        private float GetFormationSpacing(FormationType formationType, SquadDataSO squadData)
        {
            // Use squad data spacing if available
            if (squadData != null)
                return squadData.GetFormationSpacing(formationType);
            
            // Fallback to base spacing
            return formationType switch
            {
                FormationType.Normal => _baseFormationSpacing,
                FormationType.Phalanx => _baseFormationSpacing * 0.8f,
                FormationType.Testudo => _baseFormationSpacing * 0.6f,
                _ => _baseFormationSpacing
            };
        }

        private Vector3 GetRandomOffset()
        {
            return new Vector3(
                Random.Range(-_spawnRandomOffset, _spawnRandomOffset),
                0,
                Random.Range(-_spawnRandomOffset, _spawnRandomOffset)
            );
        }

        #endregion

        #region Public Interface

        public SquadModel GetSquad(int squadId)
        {
            _activeSquads.TryGetValue(squadId, out SquadModel squad);
            return squad;
        }

        public List<SquadModel> GetAllSquads()
        {
            return new List<SquadModel>(_activeSquads.Values);
        }

        public bool HasSquad(int squadId)
        {
            return _activeSquads.ContainsKey(squadId);
        }

        #endregion

        #region Debug Tools

        [Button("Show Formation Spawn Stats"), TitleGroup("Debug Tools")]
        public void ShowFormationSpawnStats()
        {
            string stats = "=== Formation Spawn Statistics ===\n";
            stats += $"Active Squads: {ActiveSquadsCount}\n";
            stats += $"Use Direct Formation Spawn: {_useDirectFormationSpawn}\n";
            stats += $"Base Formation Spacing: {_baseFormationSpacing}\n";
            stats += $"Spawn Random Offset: {_spawnRandomOffset}\n";
            
            Debug.Log(stats);
        }

        [Button("Test Formation Generation"), TitleGroup("Debug Tools")]
        public void TestFormationGeneration()
        {
            Debug.Log("=== Testing Formation Generation ===");
            
            int testUnitCount = 9;
            Vector3 testPosition = Vector3.zero;
            Quaternion testRotation = Quaternion.identity;
            
            foreach (FormationType formationType in System.Enum.GetValues(typeof(FormationType)))
            {
                Vector3[] positions = GenerateFormationPositions(
                    formationType, 
                    testUnitCount, 
                    _baseFormationSpacing, 
                    testPosition, 
                    testRotation
                );
                
                Debug.Log($"Formation {formationType}: Generated {positions.Length} positions");
                for (int i = 0; i < positions.Length; i++)
                {
                    Debug.Log($"  Unit {i}: {positions[i]}");
                }
            }
        }

        #endregion

        #region Cleanup

        protected override void OnDestroy()
        {
            var squadIds = new List<int>(_activeSquads.Keys);
            foreach (int squadId in squadIds)
            {
                DisbandSquad(squadId);
            }
            
            _activeSquads.Clear();
            _squadContainers.Clear();
        }

        #endregion
    }
}