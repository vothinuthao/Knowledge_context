using System;
using System.Collections.Generic;
using Core.Utils;
using UnityEngine;
using VikingRaven.Core.Data;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;
using VikingRaven.Units.Models;
using Random = UnityEngine.Random;

namespace VikingRaven.Core.Factory
{
    /// <summary>
    /// Factory cho việc tạo và quản lý Squad, lưu trữ SquadModel cache
    /// </summary>
    public class SquadFactory : Singleton<SquadFactory>
    {
        [Header("Squad Settings")]
        [SerializeField] private int _nextSquadId = 1;
        
        [Header("References")]
        [SerializeField] private UnitFactory _unitFactory;
        
        // Dictionary to store active squads by ID
        private Dictionary<int, SquadModel> _activeSquads = new Dictionary<int, SquadModel>();
        
        // Dictionary to cache squad templates by ID
        private Dictionary<string, SquadModel> _squadModelTemplates = new Dictionary<string, SquadModel>();
        
        // Categorized squad lists
        private List<int> _playerSquadIds = new List<int>();
        private List<int> _enemySquadIds = new List<int>();
        private List<int> _neutralSquadIds = new List<int>();
        
        // References to other systems
        private DataManager DataManager => DataManager.Instance;
        
        // Events
        public delegate void SquadEvent(SquadModel squadModel);
        public event SquadEvent OnSquadCreated;
        public event SquadEvent OnSquadDisbanded;
        
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            Debug.Log("SquadFactory initialized as singleton");
            
            // Auto-initialize unit factory reference if not set
            if (_unitFactory == null)
            {
                _unitFactory = FindObjectOfType<UnitFactory>();
                if (_unitFactory == null)
                {
                    Debug.LogError("SquadFactory: UnitFactory reference is not set and couldn't be found");
                }
            }
            
            // Preload data cache
            PreloadSquadModels();
        }
        
        /// <summary>
        /// Preload squad models from DataManager
        /// </summary>
        private void PreloadSquadModels()
        {
            if (DataManager == null || !DataManager.IsInitialized)
            {
                Debug.LogWarning("SquadFactory: DataManager not available for preloading data");
                return;
            }
            
            // Clear existing cache
            _squadModelTemplates.Clear();
            
            // Load all squad data
            List<SquadDataSO> allSquadData = DataManager.GetAllSquadData();
            
            foreach (var squadData in allSquadData)
            {
                if (!string.IsNullOrEmpty(squadData.SquadId))
                {
                    // Create a template SquadModel (without actual units)
                    SquadModel templateModel = new SquadModel(-1, squadData, Vector3.zero, Quaternion.identity);
                    _squadModelTemplates[squadData.SquadId] = templateModel;
                }
            }
            
            Debug.Log($"SquadFactory: Preloaded {_squadModelTemplates.Count} squad templates");
        }
        
        /// <summary>
        /// Create a squad based on a template ID from cache
        /// </summary>
        public SquadModel CreateSquadFromTemplate(string squadTemplateId, Vector3 position, Quaternion rotation, bool isEnemy = false)
        {
            // Check if template exists
            if (!_squadModelTemplates.TryGetValue(squadTemplateId, out SquadModel templateModel))
            {
                // If not in cache, try to get from DataManager
                SquadDataSO squadDataSo = DataManager.GetSquadData(squadTemplateId);
                if (squadDataSo == null)
                {
                    Debug.LogError($"SquadFactory: Cannot create squad - template with ID {squadTemplateId} not found");
                    return null;
                }
                
                // Create a new template
                templateModel = new SquadModel(-1, squadDataSo, Vector3.zero, Quaternion.identity);
                _squadModelTemplates[squadTemplateId] = templateModel;
                
                Debug.Log($"SquadFactory: Created new template for squad ID {squadTemplateId}");
            }
            
            // Get squad data from template
            SquadDataSO squadData = templateModel.Data;
            
            // Create an actual squad using the template's data
            return CreateSquadFromData(squadData, position, rotation, isEnemy);
        }
        
        /// <summary>
        /// Create a squad based on SquadData
        /// </summary>
        public SquadModel CreateSquadFromData(SquadDataSO squadData, Vector3 position, Quaternion rotation, bool isEnemy = false)
        {
            if (squadData == null)
            {
                Debug.LogError("SquadFactory: Cannot create squad - squadData is null");
                return null;
            }
            
            // Generate a new squad ID
            int squadId = _nextSquadId++;
            
            // Create squad model
            SquadModel squadModel = new SquadModel(squadId, squadData, position, rotation);
            
            // Create units based on the squad composition
            List<UnitModel> squadUnits = new List<UnitModel>();
            
            foreach (var composition in squadData.UnitCompositions)
            {
                if (composition.UnitData == null || composition.Count <= 0)
                    continue;
                    
                string unitId = composition.UnitData.UnitId;
                
                for (int i = 0; i < composition.Count; i++)
                {
                    // Add a small random offset to avoid units spawning at exactly the same position
                    Vector3 spawnOffset = new Vector3(
                        Random.Range(-0.5f, 0.5f),
                        0,
                        Random.Range(-0.5f, 0.5f)
                    );
                    
                    // Create unit from template or data
                    IEntity unitEntity;
                    unitEntity = _unitFactory.CreateUnitFromData(
                        composition.UnitData,
                        position + spawnOffset,
                        rotation
                    );
                    
                    if (unitEntity != null)
                    {
                        // Get the unit model
                        UnitModel unitModel = _unitFactory.GetUnitModel(unitEntity.Id);
                        if (unitModel != null)
                        {
                            squadUnits.Add(unitModel);
                        }
                    }
                }
            }
            
            // Add all units to the squad
            squadModel.AddUnits(squadUnits);
            
            // Store the squad in active squads
            _activeSquads[squadId] = squadModel;
            
            // Add to appropriate list
            if (isEnemy)
            {
                _enemySquadIds.Add(squadId);
            }
            else if (squadData.Faction == "Neutral")
            {
                _neutralSquadIds.Add(squadId);
            }
            else
            {
                _playerSquadIds.Add(squadId);
            }
            
            // Trigger event
            OnSquadCreated?.Invoke(squadModel);
            
            Debug.Log($"SquadFactory: Created {(isEnemy ? "enemy" : "player")} squad {squadId} with {squadUnits.Count} units based on {squadData.DisplayName}");
            
            return squadModel;
        }
        
        /// <summary>
        /// Create a squad with a single unit type
        /// </summary>
        public SquadModel CreateSquad(UnitType unitType, int unitCount, Vector3 position, Quaternion rotation, bool isEnemy = false)
        {
            // Try to find a matching squad template first
            SquadDataSO matchingData = FindMatchingSquadData(unitType);
            
            if (matchingData != null)
            {
                // Create from the matching data but override the unit count
                return CreateCustomizedSquad(matchingData, unitCount, position, rotation, isEnemy);
            }
            
            // If no matching data found, create a generic squad
            int squadId = _nextSquadId++;
            
            // Create an empty squad model
            SquadModel squadModel = new SquadModel(squadId, null, position, rotation);
            
            // Create the units
            List<UnitModel> squadUnits = new List<UnitModel>();
            
            for (int i = 0; i < unitCount; i++)
            {
                // Add a small random offset
                Vector3 spawnOffset = new Vector3(
                    Random.Range(-0.5f, 0.5f),
                    0,
                    Random.Range(-0.5f, 0.5f)
                );
                
                // Create unit
                IEntity unitEntity = _unitFactory.CreateUnit(unitType, position + spawnOffset, rotation);
                
                if (unitEntity != null)
                {
                    // Get the unit model
                    UnitModel unitModel = _unitFactory.GetUnitModel(unitEntity.Id);
                    if (unitModel != null)
                    {
                        squadUnits.Add(unitModel);
                    }
                }
            }
            
            // Add units to the squad
            squadModel.AddUnits(squadUnits);
            
            // Store the squad
            _activeSquads[squadId] = squadModel;
            
            // Add to appropriate list
            if (isEnemy)
            {
                _enemySquadIds.Add(squadId);
            }
            else
            {
                _playerSquadIds.Add(squadId);
            }
            
            // Trigger event
            OnSquadCreated?.Invoke(squadModel);
            
            Debug.Log($"SquadFactory: Created {(isEnemy ? "enemy" : "player")} squad {squadId} with {squadUnits.Count} units of type {unitType}");
            
            return squadModel;
        }
        
        /// <summary>
        /// Create a customized version of a squad data with different unit count
        /// </summary>
        private SquadModel CreateCustomizedSquad(SquadDataSO baseData, int totalUnitCount, Vector3 position, Quaternion rotation, bool isEnemy)
        {
            if (baseData == null) return null;
            
            // Clone the data so we don't modify the original
            SquadDataSO customData = baseData.Clone();
            
            // Get the original unit type ratio
            Dictionary<UnitType, float> typeRatios = new Dictionary<UnitType, float>();
            int originalTotalCount = 0;
            
            foreach (var composition in baseData.UnitCompositions)
            {
                if (composition.UnitData != null)
                {
                    UnitType type = composition.UnitData.UnitType;
                    if (!typeRatios.ContainsKey(type))
                    {
                        typeRatios[type] = 0;
                    }
                    
                    typeRatios[type] += composition.Count;
                    originalTotalCount += composition.Count;
                }
            }
            
            // Convert counts to ratios
            if (originalTotalCount > 0)
            {
                foreach (var type in typeRatios.Keys)
                {
                    typeRatios[type] /= originalTotalCount;
                }
            }
            
            // Create a mixed unit squad with the same ratio but different total count
            Dictionary<UnitType, int> unitCounts = new Dictionary<UnitType, int>();
            int remainingUnits = totalUnitCount;
            
            // First pass: calculate counts based on ratios
            foreach (var type in typeRatios.Keys)
            {
                int count = Mathf.FloorToInt(totalUnitCount * typeRatios[type]);
                unitCounts[type] = count;
                remainingUnits -= count;
            }
            
            // Second pass: distribute any remaining units
            var typeList = new List<UnitType>(typeRatios.Keys);
            while (remainingUnits > 0 && typeList.Count > 0)
            {
                int index = Random.Range(0, typeList.Count);
                UnitType type = typeList[index];
                
                unitCounts[type]++;
                remainingUnits--;
                
                // Remove this type to ensure even distribution
                typeList.RemoveAt(index);
                
                // If we've gone through all types, reset the list
                if (typeList.Count == 0 && remainingUnits > 0)
                {
                    typeList = new List<UnitType>(typeRatios.Keys);
                }
            }
            
            // Now create a mixed squad with these counts
            return CreateMixedSquad(unitCounts, position, rotation, isEnemy);
        }
        
        /// <summary>
        /// Create a mixed squad with different unit types
        /// </summary>
        public SquadModel CreateMixedSquad(Dictionary<UnitType, int> unitCounts, Vector3 position, Quaternion rotation, bool isEnemy = false)
        {
            // Try to find a matching squad data first
            SquadDataSO matchingData = FindMatchingSquadData(unitCounts);
            
            if (matchingData != null)
            {
                return CreateSquadFromData(matchingData, position, rotation, isEnemy);
            }
            
            // If no matching data found, create a generic mixed squad
            int squadId = _nextSquadId++;
            
            // Create an empty squad model
            SquadModel squadModel = new SquadModel(squadId, null, position, rotation);
            
            // Create units of each type
            List<UnitModel> squadUnits = new List<UnitModel>();
            
            foreach (var kvp in unitCounts)
            {
                UnitType unitType = kvp.Key;
                int count = kvp.Value;
                
                for (int i = 0; i < count; i++)
                {
                    // Add a small random offset
                    Vector3 spawnOffset = new Vector3(
                        Random.Range(-0.5f, 0.5f),
                        0,
                        Random.Range(-0.5f, 0.5f)
                    );
                    
                    // Create unit
                    IEntity unitEntity = _unitFactory.CreateUnit(unitType, position + spawnOffset, rotation);
                    
                    if (unitEntity != null)
                    {
                        // Get the unit model
                        UnitModel unitModel = _unitFactory.GetUnitModel(unitEntity.Id);
                        if (unitModel != null)
                        {
                            squadUnits.Add(unitModel);
                        }
                    }
                }
            }
            
            // Add units to the squad
            squadModel.AddUnits(squadUnits);
            
            // Store the squad
            _activeSquads[squadId] = squadModel;
            
            // Add to appropriate list
            if (isEnemy)
            {
                _enemySquadIds.Add(squadId);
            }
            else
            {
                _playerSquadIds.Add(squadId);
            }
            
            // Trigger event
            OnSquadCreated?.Invoke(squadModel);
            
            Debug.Log($"SquadFactory: Created {(isEnemy ? "enemy" : "player")} mixed squad {squadId} with {squadUnits.Count} units");
            
            return squadModel;
        }
        
        /// <summary>
        /// Disband a squad (return all its units to pools)
        /// </summary>
        public void DisbandSquad(int squadId)
        {
            if (_activeSquads.TryGetValue(squadId, out SquadModel squadModel))
            {
                // Trigger event
                OnSquadDisbanded?.Invoke(squadModel);
                
                // Get all unit entities
                List<IEntity> unitEntities = squadModel.GetAllUnitEntities();
                
                // Return all units to the factory
                foreach (var entity in unitEntities)
                {
                    if (_unitFactory != null && entity != null)
                    {
                        _unitFactory.ReturnUnit(entity);
                    }
                }
                
                // Clean up squad model
                squadModel.Cleanup();
                
                // Remove from tracking collections
                _activeSquads.Remove(squadId);
                _playerSquadIds.Remove(squadId);
                _enemySquadIds.Remove(squadId);
                _neutralSquadIds.Remove(squadId);
                
                Debug.Log($"SquadFactory: Disbanded squad {squadId} with {unitEntities.Count} units");
            }
        }
        
        /// <summary>
        /// Disband all active squads
        /// </summary>
        public void DisbandAllSquads()
        {
            // Create a copy of squad IDs to avoid modification during iteration
            List<int> squadIds = new List<int>(_activeSquads.Keys);
            
            foreach (int squadId in squadIds)
            {
                DisbandSquad(squadId);
            }
            
            Debug.Log($"SquadFactory: Disbanded all {squadIds.Count} squads");
        }
        
        /// <summary>
        /// Get a squad model by ID
        /// </summary>
        public SquadModel GetSquad(int squadId)
        {
            if (_activeSquads.TryGetValue(squadId, out SquadModel squadModel))
            {
                return squadModel;
            }
            return null;
        }
        
        /// <summary>
        /// Get a squad template by ID
        /// </summary>
        public SquadModel GetSquadTemplate(string squadId)
        {
            if (_squadModelTemplates.TryGetValue(squadId, out SquadModel template))
            {
                return template;
            }
            return null;
        }
        
        /// <summary>
        /// Get all squad templates
        /// </summary>
        public Dictionary<string, SquadModel> GetAllSquadTemplates()
        {
            return new Dictionary<string, SquadModel>(_squadModelTemplates);
        }
        
        /// <summary>
        /// Get all squad models
        /// </summary>
        public List<SquadModel> GetAllSquads()
        {
            return new List<SquadModel>(_activeSquads.Values);
        }
        
        /// <summary>
        /// Get all player squad models
        /// </summary>
        public List<SquadModel> GetPlayerSquads()
        {
            List<SquadModel> result = new List<SquadModel>();
            
            foreach (int squadId in _playerSquadIds)
            {
                if (_activeSquads.TryGetValue(squadId, out SquadModel squadModel))
                {
                    result.Add(squadModel);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Get all enemy squad models
        /// </summary>
        public List<SquadModel> GetEnemySquads()
        {
            List<SquadModel> result = new List<SquadModel>();
            
            foreach (int squadId in _enemySquadIds)
            {
                if (_activeSquads.TryGetValue(squadId, out SquadModel squadModel))
                {
                    result.Add(squadModel);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Get all squad IDs
        /// </summary>
        public List<int> GetAllSquadIds()
        {
            return new List<int>(_activeSquads.Keys);
        }
        
        /// <summary>
        /// Get all player squad IDs
        /// </summary>
        public List<int> GetPlayerSquadIds()
        {
            return new List<int>(_playerSquadIds);
        }
        
        /// <summary>
        /// Get all enemy squad IDs
        /// </summary>
        public List<int> GetEnemySquadIds()
        {
            return new List<int>(_enemySquadIds);
        }
        
        /// <summary>
        /// Find a matching squad data for a specific unit type
        /// </summary>
        private SquadDataSO FindMatchingSquadData(UnitType unitType)
        {
            if (DataManager == null || !DataManager.IsInitialized)
                return null;
                
            // Get all squad data
            List<SquadDataSO> allSquadData = DataManager.GetAllSquadData();
            
            foreach (var squadData in allSquadData)
            {
                // Count units by type
                Dictionary<UnitType, int> typeCounts = squadData.GetUnitTypeCounts();
                
                // Check if this squad only contains the requested unit type
                if (typeCounts.Count == 1 && typeCounts.ContainsKey(unitType))
                {
                    return squadData;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Find a matching squad data for a specific unit composition
        /// </summary>
        private SquadDataSO FindMatchingSquadData(Dictionary<UnitType, int> unitCounts)
        {
            if (DataManager == null || !DataManager.IsInitialized)
                return null;
                
            // Get all squad data
            List<SquadDataSO> allSquadData = DataManager.GetAllSquadData();
            
            // Calculate total unit count
            int totalRequestedUnits = 0;
            foreach (var count in unitCounts.Values)
            {
                totalRequestedUnits += count;
            }
            
            // Find a squad with a similar composition
            foreach (var squadData in allSquadData)
            {
                // Get squad composition
                Dictionary<UnitType, int> squadComposition = squadData.GetUnitTypeCounts();
                
                // Check if the composition matches
                if (squadComposition.Count != unitCounts.Count)
                    continue;
                    
                bool isMatch = true;
                float ratioMatch = 0f;
                
                // Calculate total units in squad
                int totalSquadUnits = 0;
                foreach (var count in squadComposition.Values)
                {
                    totalSquadUnits += count;
                }
                
                // Compare each unit type
                foreach (var kvp in unitCounts)
                {
                    UnitType type = kvp.Key;
                    int requestedCount = kvp.Value;
                    
                    // Check if the squad has this unit type
                    if (!squadComposition.TryGetValue(type, out int squadCount))
                    {
                        isMatch = false;
                        break;
                    }
                    
                    // Check if the ratio is similar
                    float requestedRatio = (float)requestedCount / totalRequestedUnits;
                    float squadRatio = (float)squadCount / totalSquadUnits;
                    
                    // Consider a 20% variance as acceptable
                    float ratioVariance = Mathf.Abs(requestedRatio - squadRatio);
                    if (ratioVariance > 0.2f)
                    {
                        isMatch = false;
                        break;
                    }
                    
                    // Track how closely this matches (lower is better)
                    ratioMatch += ratioVariance;
                }
                
                if (isMatch)
                {
                    return squadData;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Update all squads
        /// </summary>
        public void UpdateAllSquads()
        {
            foreach (var squad in _activeSquads.Values)
            {
                squad.Update();
            }
        }
        
        /// <summary>
        /// Clean up all squads when destroyed
        /// </summary>
        private void OnDestroy()
        {
            DisbandAllSquads();
        }
    }
}