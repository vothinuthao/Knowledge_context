using System.Collections.Generic;
using UnityEngine;
using Core.Utils;
using VikingRaven.Units.Data;

namespace VikingRaven.Core.Data
{
    /// <summary>
    /// Manages loading and accessing game data from ScriptableObjects
    /// </summary>
    public class DataManager : PureSingleton<DataManager>
    {
        // Dictionary for caching unit data, accessed by unit ID
        private Dictionary<string, UnitDataSO> _unitDataDict = new Dictionary<string, UnitDataSO>();
        
        // Dictionary for caching squad data, accessed by squad ID
        private Dictionary<string, SquadDataSO> _squadDataDict = new Dictionary<string, SquadDataSO>();
        
        // Flag to track if data has been loaded
        private bool _dataLoaded = false;
        
        /// <summary>
        /// Load all game data from Resources folders
        /// </summary>
        /// <returns>True if data was loaded successfully</returns>
        public override bool Initialize()
        {
            if (_dataLoaded) 
                return true;
                
            // Call base implementation first
            base.Initialize();
            
            // Load unit data
            LoadUnitData();
            
            // Load squad data
            LoadSquadData();
            
            // Set flag to indicate data is loaded
            _dataLoaded = true;
            
            Debug.Log($"DataManager: Initialized with {_unitDataDict.Count} units and {_squadDataDict.Count} squads");
            return true;
        }
        
        /// <summary>
        /// Load all unit data from Resources
        /// </summary>
        private void LoadUnitData()
        {
            // Clear existing cache
            _unitDataDict.Clear();
            
            // Load all UnitData assets from Resources/Units folder
            UnitDataSO[] unitDataArray = Resources.LoadAll<UnitDataSO>("Units");
            
            foreach (var unitData in unitDataArray)
            {
                if (!string.IsNullOrEmpty(unitData.UnitId))
                {
                    // Create a clone to avoid shared references
                    _unitDataDict[unitData.UnitId] = unitData.Clone();
                    Debug.Log($"DataManager: Loaded UnitData [{unitData.UnitId}] - {unitData.DisplayName}");
                }
                else
                {
                    Debug.LogWarning($"DataManager: Skipping UnitData with empty ID: {unitData.name}");
                }
            }
            
            Debug.Log($"DataManager: Loaded {_unitDataDict.Count} unit data assets");
        }
        
        /// <summary>
        /// Load all squad data from Resources
        /// </summary>
        private void LoadSquadData()
        {
            // Clear existing cache
            _squadDataDict.Clear();
            
            // Load all SquadData assets from Resources/Squads folder
            SquadDataSO[] squadDataArray = Resources.LoadAll<SquadDataSO>("Squads");
            
            foreach (var squadData in squadDataArray)
            {
                if (!string.IsNullOrEmpty(squadData.SquadId))
                {
                    // Create a clone to avoid shared references
                    _squadDataDict[squadData.SquadId] = squadData.Clone();
                    Debug.Log($"DataManager: Loaded SquadData [{squadData.SquadId}] - {squadData.DisplayName}");
                }
                else
                {
                    Debug.LogWarning($"DataManager: Skipping SquadData with empty ID: {squadData.name}");
                }
            }
            
            Debug.Log($"DataManager: Loaded {_squadDataDict.Count} squad data assets");
        }
        
        /// <summary>
        /// Get unit data by ID
        /// </summary>
        /// <param name="unitId">Unit ID</param>
        /// <returns>UnitData or null if not found</returns>
        public UnitDataSO GetUnitData(string unitId)
        {
            if (_unitDataDict.TryGetValue(unitId, out UnitDataSO unitData))
            {
                return unitData;
            }
            
            Debug.LogWarning($"DataManager: UnitData not found for ID: {unitId}");
            return null;
        }
        
        /// <summary>
        /// Get squad data by ID
        /// </summary>
        /// <param name="squadId">Squad ID</param>
        /// <returns>SquadData or null if not found</returns>
        public SquadDataSO GetSquadData(string squadId)
        {
            if (_squadDataDict.TryGetValue(squadId, out SquadDataSO squadData))
            {
                return squadData;
            }
            
            Debug.LogWarning($"DataManager: SquadData not found for ID: {squadId}");
            return null;
        }
        
        /// <summary>
        /// Get all available unit data
        /// </summary>
        /// <returns>List of all unit data</returns>
        public List<UnitDataSO> GetAllUnitData()
        {
            List<UnitDataSO> result = new List<UnitDataSO>();
            foreach (var unitData in _unitDataDict.Values)
            {
                result.Add(unitData);
            }
            return result;
        }
        
        /// <summary>
        /// Get all available squad data
        /// </summary>
        /// <returns>List of all squad data</returns>
        public List<SquadDataSO> GetAllSquadData()
        {
            List<SquadDataSO> result = new List<SquadDataSO>();
            foreach (var squadData in _squadDataDict.Values)
            {
                result.Add(squadData);
            }
            return result;
        }
        
        /// <summary>
        /// Get all unit data of a specific type
        /// </summary>
        /// <param name="unitType">Type of unit to filter by</param>
        /// <returns>List of unit data matching the type</returns>
        public List<UnitDataSO> GetUnitDataByType(Units.Components.UnitType unitType)
        {
            List<UnitDataSO> result = new List<UnitDataSO>();
            foreach (var unitData in _unitDataDict.Values)
            {
                if (unitData.UnitType == unitType)
                {
                    result.Add(unitData);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Reload all data from resources
        /// </summary>
        public void ReloadData()
        {
            _dataLoaded = false;
            Initialize();
            Debug.Log("DataManager: All data reloaded");
        }
        
        /// <summary>
        /// Clean up resources
        /// </summary>
        public override void Cleanup()
        {
            _unitDataDict.Clear();
            _squadDataDict.Clear();
            _dataLoaded = false;
            base.Cleanup();
        }
    }
}