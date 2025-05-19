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
    /// Enhanced factory for creating and managing squads of units
    /// Uses the EnhancedUnitFactory to create units and SquadModel to manage them
    /// </summary>
    public class SquadFactory : Singleton<SquadFactory>
    {
        [Header("Squad Settings")]
        [SerializeField] private int _nextSquadId = 1;
        
        [Header("References")]
        [SerializeField] private UnitFactory _unitFactory;
        
        // Dictionary to store active squads by ID
        private Dictionary<int, SquadModel> _activeSquads = new Dictionary<int, SquadModel>();
        
        // Categorized squad lists
        private List<int> _playerSquadIds = new List<int>();
        private List<int> _enemySquadIds = new List<int>();
        private List<int> _neutralSquadIds = new List<int>();
        
        // Data cache
        private Dictionary<string, SquadDataSO> _squadDataCache = new Dictionary<string, SquadDataSO>();
        
        // References to other systems
        private DataManager DataManager => DataManager.Instance;
        
        // Events
        public delegate void SquadEvent(SquadModel squadModel);
        public event SquadEvent OnSquadCreated;
        public event SquadEvent OnSquadDisbanded;
        
        
        [Obsolete("Obsolete")]
        protected override void OnInitialize()
        {
            base.OnInitialize();
            Debug.Log("EnhancedSquadFactory initialized as singleton");
            
            // Auto-initialize unit factory reference if not set
            if (_unitFactory == null)
            {
                _unitFactory = FindObjectOfType<UnitFactory>();
                if (_unitFactory == null)
                {
                    Debug.LogError("EnhancedSquadFactory: UnitFactory reference is not set and couldn't be found");
                }
            }
            
            // Preload data cache
            PreloadDataCache();
        }
        
        /// <summary>
        /// Preload squad data from DataManager
        /// </summary>
        private void PreloadDataCache()
        {
            if (DataManager == null || !DataManager.IsInitialized)
            {
                Debug.LogWarning("EnhancedSquadFactory: DataManager not available for preloading data");
                return;
            }
            
            // Clear existing cache
            _squadDataCache.Clear();
            
            // Load all squad data
            List<SquadDataSO> allSquadData = DataManager.GetAllSquadData();
            
            foreach (var squadData in allSquadData)
            {
                if (!string.IsNullOrEmpty(squadData.SquadId))
                {
                    _squadDataCache[squadData.SquadId] = squadData;
                }
            }
            
            Debug.Log($"EnhancedSquadFactory: Preloaded {_squadDataCache.Count} squad data templates");
        }
        
        /// <summary>
        /// Create a squad based on SquadData
        /// </summary>
        /// <param name="squadData">Data defining the squad composition</param>
        /// <param name="position">Position in world space</param>
        /// <param name="rotation">Rotation in world space</param>
        /// <param name="isEnemy">Whether this is an enemy squad</param>
        /// <returns>The created squad model</returns>
        public SquadModel CreateSquadFromData(SquadDataSO squadData, Vector3 position, Quaternion rotation, bool isEnemy = false)
        {
            if (squadData == null)
            {
                Debug.LogError("EnhancedSquadFactory: Cannot create squad - squadData is null");
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
                    
                for (int i = 0; i < composition.Count; i++)
                {
                    // Add a small random offset to avoid units spawning at exactly the same position
                    Vector3 spawnOffset = new Vector3(
                        Random.Range(-0.5f, 0.5f),
                        0,
                        Random.Range(-0.5f, 0.5f)
                    );
                    
                    // Create unit from data
                    IEntity unitEntity = _unitFactory.CreateUnitFromData(
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
            
            Debug.Log($"EnhancedSquadFactory: Created {(isEnemy ? "enemy" : "player")} squad {squadId} with {squadUnits.Count} units based on {squadData.DisplayName}");
            
            return squadModel;
        }
        
        /// <summary>
        /// Create a squad with a single unit type
        /// </summary>
        /// <param name="unitType">Type of unit</param>
        /// <param name="unitCount">Number of units to create</param>
        /// <param name="position">Position in world space</param>
        /// <param name="rotation">Rotation in world space</param>
        /// <param name="isEnemy">Whether this is an enemy squad</param>
        /// <returns>The created squad model</returns>
        public SquadModel CreateSquad(UnitType unitType, int unitCount, Vector3 position, Quaternion rotation, bool isEnemy = false)
        {
            // Try to find a matching squad data first
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
            
            Debug.Log($"EnhancedSquadFactory: Created {(isEnemy ? "enemy" : "player")} squad {squadId} with {squadUnits.Count} units of type {unitType}");
            
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
        /// <param name="unitCounts">Dictionary mapping unit types to counts</param>
        /// <param name="position">Position in world space</param>
        /// <param name="rotation">Rotation in world space</param>
        /// <param name="isEnemy">Whether this is an enemy squad</param>
        /// <returns>The created squad model</returns>
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
            
            Debug.Log($"EnhancedSquadFactory: Created {(isEnemy ? "enemy" : "player")} mixed squad {squadId} with {squadUnits.Count} units");
            
            return squadModel;
        }
        
        /// <summary>
        /// Create a squad by ID from DataManager
        /// </summary>
        /// <param name="squadDataId">ID of the squad data in DataManager</param>
        /// <param name="position">Position in world space</param>
        /// <param name="rotation">Rotation in world space</param>
        /// <param name="isEnemy">Whether this is an enemy squad</param>
        /// <returns>The created squad model</returns>
        public SquadModel CreateNamedSquad(string squadDataId, Vector3 position, Quaternion rotation, bool isEnemy = false)
        {
            // Check cache first
            if (_squadDataCache.TryGetValue(squadDataId, out SquadDataSO squadData))
            {
                return CreateSquadFromData(squadData, position, rotation, isEnemy);
            }
            
            // Not in cache, try to get from DataManager
            if (DataManager != null && DataManager.IsInitialized)
            {
                squadData = DataManager.GetSquadData(squadDataId);
                if (squadData != null)
                {
                    // Cache for future use
                    _squadDataCache[squadDataId] = squadData;
                    
                    return CreateSquadFromData(squadData, position, rotation, isEnemy);
                }
            }
            
            Debug.LogError($"EnhancedSquadFactory: Cannot create named squad - SquadData not found for ID: {squadDataId}");
            return null;
        }
        
        /// <summary>
        /// Disband a squad (return all its units to pools)
        /// </summary>
        /// <param name="squadId">ID of the squad to disband</param>
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
                
                Debug.Log($"EnhancedSquadFactory: Disbanded squad {squadId} with {unitEntities.Count} units");
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
            
            Debug.Log($"EnhancedSquadFactory: Disbanded all {squadIds.Count} squads");
        }
        
        /// <summary>
        /// Get a squad model by ID
        /// </summary>
        /// <param name="squadId">Squad ID</param>
        /// <returns>Squad model or null if not found</returns>
        public SquadModel GetSquad(int squadId)
        {
            if (_activeSquads.TryGetValue(squadId, out SquadModel squadModel))
            {
                return squadModel;
            }
            return null;
        }
        
        /// <summary>
        /// Get all squad models
        /// </summary>
        /// <returns>List of all active squad models</returns>
        public List<SquadModel> GetAllSquads()
        {
            return new List<SquadModel>(_activeSquads.Values);
        }
        
        /// <summary>
        /// Get all player squad models
        /// </summary>
        /// <returns>List of player squad models</returns>
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
        /// <returns>List of enemy squad models</returns>
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
        /// <returns>List of all active squad IDs</returns>
        public List<int> GetAllSquadIds()
        {
            return new List<int>(_activeSquads.Keys);
        }
        
        /// <summary>
        /// Get all player squad IDs
        /// </summary>
        /// <returns>List of player squad IDs</returns>
        public List<int> GetPlayerSquadIds()
        {
            return new List<int>(_playerSquadIds);
        }
        
        /// <summary>
        /// Get all enemy squad IDs
        /// </summary>
        /// <returns>List of enemy squad IDs</returns>
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