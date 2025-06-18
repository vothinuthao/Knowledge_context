using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using Core.Utils;
using VikingRaven.Core.Factory;
using VikingRaven.Units;
using VikingRaven.Units.Data;
using VikingRaven.Units.Components;

namespace VikingRaven.Core.Data
{
    /// <summary>
    /// Central data management for all game data - UnitData, SquadData
    /// Updated to use uint for IDs instead of string
    /// </summary>
    public class DataManager : Singleton<DataManager>
    {
        #region Inspector Fields
        
        [TitleGroup("Data Loading Settings")]
        [Tooltip("Auto load data on start")]
        [SerializeField, ToggleLeft] 
        private bool _autoLoadOnStart = true;
        
        [Tooltip("Enable verbose logging")]
        [SerializeField, ToggleLeft] 
        private bool _verboseLogging = true;
        
        [TitleGroup("Resource Paths")]
        [Tooltip("Path to UnitDataSO folder")]
        [SerializeField, FolderPath] 
        private string _unitDataPath = "UnitData";
        
        [Tooltip("Path to SquadDataSO folder")]
        [SerializeField, FolderPath] 
        private string _squadDataPath = "SquadData";
        
        [TitleGroup("Debug Information")]
        [ShowInInspector, ReadOnly, ProgressBar(0, 100, ColorGetter = "GetLoadProgressColor")]
        private float _loadProgress = 0f;
        
        [ShowInInspector, ReadOnly]
        private int _totalUnitDataLoaded => _unitDataCache?.Count ?? 0;
        
        [ShowInInspector, ReadOnly]
        private int _totalSquadDataLoaded => _squadDataCache?.Count ?? 0;
        
        [ShowInInspector, ReadOnly]
        private bool _isDataLoaded = false;

        #endregion

        #region Private Fields

        // Cache data using uint IDs
        private Dictionary<uint, UnitDataSO> _unitDataCache = new Dictionary<uint, UnitDataSO>();
        private Dictionary<uint, SquadDataSO> _squadDataCache = new Dictionary<uint, SquadDataSO>();
        
        // Cache by unit type
        private Dictionary<UnitType, List<UnitDataSO>> _unitDataByType = new Dictionary<UnitType, List<UnitDataSO>>();
        
        // All data lists
        private List<UnitDataSO> _allUnitData = new List<UnitDataSO>();
        private List<SquadDataSO> _allSquadData = new List<SquadDataSO>();

        #endregion

        #region Properties

        public bool IsInitialized => _isDataLoaded;
        public float LoadProgress => _loadProgress;

        #endregion

        #region Events

        public event Action OnDataLoaded;
        public event Action OnDataLoadStarted;

        #endregion

        #region Unity Lifecycle

        public void Initialize()
        {
            if (_verboseLogging)
                Debug.Log("DataManager: Initializing...");
            
            InitializeCaches();
            
            if (_autoLoadOnStart)
            {
                LoadAllData();
            }
        }

        #endregion

        #region Initialization Methods

        private void InitializeCaches()
        {
            _unitDataCache.Clear();
            _squadDataCache.Clear();
            _unitDataByType.Clear();
            _allUnitData.Clear();
            _allSquadData.Clear();
            
            foreach (UnitType unitType in Enum.GetValues(typeof(UnitType)))
            {
                _unitDataByType[unitType] = new List<UnitDataSO>();
            }
            
            if (_verboseLogging)
                Debug.Log("DataManager: Caches initialized");
        }

        #endregion

        #region Data Loading Methods

        [Button("Load All Data"), TitleGroup("Debug Tools")]
        public void LoadAllData()
        {
            if (_verboseLogging)
                Debug.Log("DataManager: Starting to load all data...");
                
            OnDataLoadStarted?.Invoke();
            _loadProgress = 0f;
            
            try
            {
                LoadUnitData();
                _loadProgress = 50f;
                
                LoadSquadData();
                _loadProgress = 100f;
                
                _isDataLoaded = true;
                
                OnDataLoaded?.Invoke();
                
                if (_verboseLogging)
                    Debug.Log($"DataManager: Successfully loaded {_totalUnitDataLoaded} UnitData and {_totalSquadDataLoaded} SquadData");
            }
            catch (Exception e)
            {
                Debug.LogError($"DataManager: Error loading data - {e.Message}");
                _loadProgress = 0f;
                _isDataLoaded = false;
            }
        }
        
        private void LoadUnitData()
        {
            _unitDataCache.Clear();
            _allUnitData.Clear();
            foreach (var list in _unitDataByType.Values)
            {
                list.Clear();
            }
            
            UnitDataSO[] unitDataArray = Resources.LoadAll<UnitDataSO>(_unitDataPath);
            
            if (unitDataArray == null || unitDataArray.Length == 0)
            {
                Debug.LogWarning($"DataManager: No UnitDataSO found in path '{_unitDataPath}'");
                return;
            }
            
            foreach (var unitData in unitDataArray)
            {
                if (unitData == null) continue;
                
                // Validate data - now using uint
                if (unitData.UnitId == 0)
                {
                    Debug.LogWarning($"DataManager: UnitDataSO '{unitData.name}' has invalid UnitId (0), skipping");
                    continue;
                }
                
                if (_unitDataCache.ContainsKey(unitData.UnitId))
                {
                    Debug.LogWarning($"DataManager: Duplicate UnitId '{unitData.UnitId}' found, overwriting");
                }
                
                _unitDataCache[unitData.UnitId] = unitData;
                _allUnitData.Add(unitData);
                
                if (_unitDataByType.ContainsKey(unitData.UnitType))
                {
                    _unitDataByType[unitData.UnitType].Add(unitData);
                }
                
                if (_verboseLogging)
                    Debug.Log($"DataManager: Loaded UnitData '{unitData.DisplayName}' (ID: {unitData.UnitId}, Type: {unitData.UnitType})");
            }
        }
        
        private void LoadSquadData()
        {
            _squadDataCache.Clear();
            _allSquadData.Clear();
            
            SquadDataSO[] squadDataArray = Resources.LoadAll<SquadDataSO>(_squadDataPath);
            
            if (squadDataArray == null || squadDataArray.Length == 0)
            {
                Debug.LogWarning($"DataManager: No SquadDataSO found in path '{_squadDataPath}'");
                return;
            }
            
            foreach (var squadData in squadDataArray)
            {
                if (squadData == null) continue;
                
                // Validate data - now using uint
                if (squadData.SquadId == 0)
                {
                    Debug.LogWarning($"DataManager: SquadDataSO '{squadData.name}' has invalid SquadId (0), skipping");
                    continue;
                }
                
                if (_squadDataCache.ContainsKey(squadData.SquadId))
                {
                    Debug.LogWarning($"DataManager: Duplicate SquadId '{squadData.SquadId}' found, overwriting");
                }
                
                _squadDataCache[squadData.SquadId] = squadData;
                _allSquadData.Add(squadData);
                
                if (_verboseLogging)
                    Debug.Log($"DataManager: Loaded SquadData '{squadData.DisplayName}' (ID: {squadData.SquadId})");
            }
        }

        #endregion

        #region Public API Methods

        /// <summary>
        /// Get UnitDataSO by ID
        /// </summary>
        public UnitDataSO GetUnitData(uint unitId)
        {
            if (unitId == 0)
                return null;
                
            if (_unitDataCache.TryGetValue(unitId, out UnitDataSO unitData))
            {
                return unitData;
            }
            
            if (_verboseLogging)
                Debug.LogWarning($"DataManager: UnitData with ID '{unitId}' not found");
            return null;
        }
        
        /// <summary>
        /// Get SquadDataSO by ID
        /// </summary>
        public SquadDataSO GetSquadData(uint squadId)
        {
            if (squadId == 0)
                return null;
                
            if (_squadDataCache.TryGetValue(squadId, out SquadDataSO squadData))
            {
                return squadData;
            }
            
            if (_verboseLogging)
                Debug.LogWarning($"DataManager: SquadData with ID '{squadId}' not found");
            return null;
        }
        
        /// <summary>
        /// Get all UnitDataSO
        /// </summary>
        public List<UnitDataSO> GetAllUnitData()
        {
            return new List<UnitDataSO>(_allUnitData);
        }
        
        /// <summary>
        /// Get all SquadDataSO
        /// </summary>
        public List<SquadDataSO> GetAllSquadData()
        {
            return new List<SquadDataSO>(_allSquadData);
        }
        
        /// <summary>
        /// Get UnitDataSO by unit type
        /// </summary>
        public List<UnitDataSO> GetUnitDataByType(UnitType unitType)
        {
            if (_unitDataByType.TryGetValue(unitType, out List<UnitDataSO> unitDataList))
            {
                return new List<UnitDataSO>(unitDataList);
            }
            
            return new List<UnitDataSO>();
        }
        
        /// <summary>
        /// Check if unit data exists
        /// </summary>
        public bool HasUnitData(uint unitId)
        {
            return unitId != 0 && _unitDataCache.ContainsKey(unitId);
        }
        
        /// <summary>
        /// Check if squad data exists
        /// </summary>
        public bool HasSquadData(uint squadId)
        {
            return squadId != 0 && _squadDataCache.ContainsKey(squadId);
        }

        #endregion

        #region Debug Methods

        private Color GetLoadProgressColor()
        {
            if (_loadProgress < 50f) return Color.red;
            if (_loadProgress < 100f) return Color.yellow;
            return Color.green;
        }
        
        [Button("Show Data Statistics"), TitleGroup("Debug Tools")]
        public void ShowDataStatistics()
        {
            string stats = "=== DATA MANAGER STATISTICS ===\n";
            stats += $"Total UnitData loaded: {_totalUnitDataLoaded}\n";
            stats += $"Total SquadData loaded: {_totalSquadDataLoaded}\n";
            stats += $"Load Progress: {_loadProgress}%\n";
            stats += $"Is Initialized: {_isDataLoaded}\n\n";
            
            stats += "UnitData by Type:\n";
            foreach (var kvp in _unitDataByType)
            {
                stats += $"  {kvp.Key}: {kvp.Value.Count} units\n";
            }
            
            Debug.Log(stats);
        }
        
        [Button("Reload All Data"), TitleGroup("Debug Tools")]
        public void ReloadAllData()
        {
            _isDataLoaded = false;
            LoadAllData();
        }
        
        [Button("Clear All Caches"), TitleGroup("Debug Tools")]
        public void ClearAllCaches()
        {
            InitializeCaches();
            _isDataLoaded = false;
            _loadProgress = 0f;
            Debug.Log("DataManager: All caches cleared");
        }

        #endregion
    }
}