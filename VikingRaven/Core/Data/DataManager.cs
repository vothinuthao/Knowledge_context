using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using Core.Utils;
using VikingRaven.Core.Factory;
using VikingRaven.Units.Data;
using VikingRaven.Units.Components;

namespace VikingRaven.Core.Data
{
    /// <summary>
    /// Trung tâm quản lý tất cả dữ liệu game - UnitData, SquadData, và các loại dữ liệu khác
    /// Singleton pattern để đảm bảo chỉ có một instance duy nhất
    /// </summary>
    public class DataManager : Singleton<DataManager>
    {
        #region Inspector Fields
        
        [TitleGroup("Data Loading Settings")]
        [Tooltip("Tự động load dữ liệu khi khởi tạo")]
        [SerializeField, ToggleLeft] 
        private bool _autoLoadOnStart = true;
        
        [Tooltip("Hiển thị log chi tiết khi load dữ liệu")]
        [SerializeField, ToggleLeft] 
        private bool _verboseLogging = true;
        
        [TitleGroup("Resource Paths")]
        [Tooltip("Đường dẫn đến thư mục chứa UnitDataSO")]
        [SerializeField, FolderPath] 
        private string _unitDataPath = "UnitData";
        
        [Tooltip("Đường dẫn đến thư mục chứa SquadDataSO")]
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

        // Cache dữ liệu UnitData theo ID
        private Dictionary<string, UnitDataSO> _unitDataCache = new Dictionary<string, UnitDataSO>();
        
        // Cache dữ liệu SquadData theo ID  
        private Dictionary<string, SquadDataSO> _squadDataCache = new Dictionary<string, SquadDataSO>();
        
        // Cache dữ liệu UnitData theo loại đơn vị
        private Dictionary<UnitType, List<UnitDataSO>> _unitDataByType = new Dictionary<UnitType, List<UnitDataSO>>();
        
        // Danh sách tất cả UnitData và SquadData
        private List<UnitDataSO> _allUnitData = new List<UnitDataSO>();
        private List<SquadDataSO> _allSquadData = new List<SquadDataSO>();

        #endregion

        #region Properties

        /// <summary>
        /// Kiểm tra xem dữ liệu đã được load chưa
        /// </summary>
        public bool IsInitialized => _isDataLoaded;
        
        /// <summary>
        /// Tiến độ load dữ liệu (0-100)
        /// </summary>
        public float LoadProgress => _loadProgress;

        #endregion

        #region Events

        /// <summary>
        /// Event được gọi khi hoàn thành load dữ liệu
        /// </summary>
        public event Action OnDataLoaded;
        
        /// <summary>
        /// Event được gọi khi bắt đầu load dữ liệu
        /// </summary>
        public event Action OnDataLoadStarted;

        #endregion

        #region Unity Lifecycle

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            if (_verboseLogging)
                Debug.Log("DataManager: Initializing...");
            
            // Khởi tạo dictionaries
            InitializeCaches();
            
            // Auto load nếu được bật
            if (_autoLoadOnStart)
            {
                LoadAllData();
            }
        }

        #endregion

        #region Initialization Methods

        /// <summary>
        /// Khởi tạo các cache dictionaries
        /// </summary>
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

        /// <summary>
        /// Load tất cả dữ liệu từ Resources
        /// </summary>
        [Button("Load All Data"), TitleGroup("Debug Tools")]
        public void LoadAllData()
        {
            if (_verboseLogging)
                Debug.Log("DataManager: Starting to load all data...");
                
            OnDataLoadStarted?.Invoke();
            _loadProgress = 0f;
            
            try
            {
                // Load UnitData
                LoadUnitData();
                _loadProgress = 50f;
                
                // Load SquadData  
                LoadSquadData();
                _loadProgress = 100f;
                
                // Đánh dấu đã load xong
                _isDataLoaded = true;
                
                // Notify factories
                NotifyFactories();
                
                // Trigger event
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
        
        /// <summary>
        /// Load tất cả UnitDataSO từ Resources
        /// </summary>
        private void LoadUnitData()
        {
            // Clear existing data
            _unitDataCache.Clear();
            _allUnitData.Clear();
            foreach (var list in _unitDataByType.Values)
            {
                list.Clear();
            }
            
            // Load từ Resources
            UnitDataSO[] unitDataArray = Resources.LoadAll<UnitDataSO>(_unitDataPath);
            
            if (unitDataArray == null || unitDataArray.Length == 0)
            {
                Debug.LogWarning($"DataManager: No UnitDataSO found in path '{_unitDataPath}'");
                return;
            }
            
            // Process mỗi UnitData
            foreach (var unitData in unitDataArray)
            {
                if (unitData == null) continue;
                
                // Validate data
                if (string.IsNullOrEmpty(unitData.UnitId))
                {
                    Debug.LogWarning($"DataManager: UnitDataSO '{unitData.name}' has empty UnitId, skipping");
                    continue;
                }
                
                // Check duplicate ID
                if (_unitDataCache.ContainsKey(unitData.UnitId))
                {
                    Debug.LogWarning($"DataManager: Duplicate UnitId '{unitData.UnitId}' found, overwriting");
                }
                
                // Add to caches
                _unitDataCache[unitData.UnitId] = unitData;
                _allUnitData.Add(unitData);
                
                // Add to type-specific cache
                if (_unitDataByType.ContainsKey(unitData.UnitType))
                {
                    _unitDataByType[unitData.UnitType].Add(unitData);
                }
                
                if (_verboseLogging)
                    Debug.Log($"DataManager: Loaded UnitData '{unitData.DisplayName}' (ID: {unitData.UnitId}, Type: {unitData.UnitType})");
            }
        }
        
        /// <summary>
        /// Load tất cả SquadDataSO từ Resources
        /// </summary>
        private void LoadSquadData()
        {
            // Clear existing data
            _squadDataCache.Clear();
            _allSquadData.Clear();
            
            // Load từ Resources
            SquadDataSO[] squadDataArray = Resources.LoadAll<SquadDataSO>(_squadDataPath);
            
            if (squadDataArray == null || squadDataArray.Length == 0)
            {
                Debug.LogWarning($"DataManager: No SquadDataSO found in path '{_squadDataPath}'");
                return;
            }
            
            // Process mỗi SquadData
            foreach (var squadData in squadDataArray)
            {
                if (squadData == null) continue;
                
                // Validate data
                if (string.IsNullOrEmpty(squadData.SquadId))
                {
                    Debug.LogWarning($"DataManager: SquadDataSO '{squadData.name}' has empty SquadId, skipping");
                    continue;
                }
                
                // Check duplicate ID
                if (_squadDataCache.ContainsKey(squadData.SquadId))
                {
                    Debug.LogWarning($"DataManager: Duplicate SquadId '{squadData.SquadId}' found, overwriting");
                }
                
                // Add to caches
                _squadDataCache[squadData.SquadId] = squadData;
                _allSquadData.Add(squadData);
                
                if (_verboseLogging)
                    Debug.Log($"DataManager: Loaded SquadData '{squadData.DisplayName}' (ID: {squadData.SquadId})");
            }
        }

        #endregion

        #region Factory Notification

        /// <summary>
        /// Thông báo các Factory về việc data đã được load
        /// </summary>
        private void NotifyFactories()
        {
            // Notify UnitFactory
            var unitFactory = FindObjectOfType<VikingRaven.Core.Factory.UnitFactory>();
            if (unitFactory != null)
            {
                // Gọi method để UnitFactory tạo templates từ data
                NotifyUnitFactory(unitFactory);
            }
            else
            {
                Debug.LogWarning("DataManager: UnitFactory not found in scene");
            }
            
            // Notify SquadFactory
            var squadFactory = SquadFactory.Instance;
            if (squadFactory != null)
            {
                // Gọi method để SquadFactory tạo templates từ data
                NotifySquadFactory(squadFactory);
            }
            else
            {
                Debug.LogWarning("DataManager: SquadFactory not found");
            }
        }
        
        /// <summary>
        /// Thông báo UnitFactory để tạo các template models
        /// </summary>
        private void NotifyUnitFactory(VikingRaven.Core.Factory.UnitFactory unitFactory)
        {
            try
            {
                // Gọi reflection để call private method
                var method = typeof(VikingRaven.Core.Factory.UnitFactory)
                    .GetMethod("OnDataManagerLoaded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (method != null)
                {
                    method.Invoke(unitFactory, new object[] { _allUnitData });
                    if (_verboseLogging)
                        Debug.Log("DataManager: Notified UnitFactory successfully");
                }
                else
                {
                    Debug.LogWarning("DataManager: OnDataManagerLoaded method not found in UnitFactory");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"DataManager: Error notifying UnitFactory - {e.Message}");
            }
        }
        
        /// <summary>
        /// Thông báo SquadFactory để tạo các template models
        /// </summary>
        private void NotifySquadFactory(VikingRaven.Core.Factory.SquadFactory squadFactory)
        {
            try
            {
                // Gọi reflection để call private method
                var method = typeof(VikingRaven.Core.Factory.SquadFactory)
                    .GetMethod("OnDataManagerLoaded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (method != null)
                {
                    method.Invoke(squadFactory, new object[] { _allSquadData });
                    if (_verboseLogging)
                        Debug.Log("DataManager: Notified SquadFactory successfully");
                }
                else
                {
                    Debug.LogWarning("DataManager: OnDataManagerLoaded method not found in SquadFactory");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"DataManager: Error notifying SquadFactory - {e.Message}");
            }
        }

        #endregion

        #region Public API Methods

        /// <summary>
        /// Lấy UnitDataSO theo ID
        /// </summary>
        public UnitDataSO GetUnitData(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
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
        /// Lấy SquadDataSO theo ID
        /// </summary>
        public SquadDataSO GetSquadData(string squadId)
        {
            if (string.IsNullOrEmpty(squadId))
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
        /// Lấy tất cả UnitDataSO
        /// </summary>
        public List<UnitDataSO> GetAllUnitData()
        {
            return new List<UnitDataSO>(_allUnitData);
        }
        
        /// <summary>
        /// Lấy tất cả SquadDataSO
        /// </summary>
        public List<SquadDataSO> GetAllSquadData()
        {
            return new List<SquadDataSO>(_allSquadData);
        }
        
        /// <summary>
        /// Lấy UnitDataSO theo loại đơn vị
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
        /// Lấy UnitDataSO ngẫu nhiên theo loại
        /// </summary>
        public UnitDataSO GetRandomUnitDataByType(UnitType unitType)
        {
            var unitDataList = GetUnitDataByType(unitType);
            
            if (unitDataList.Count == 0)
                return null;
                
            return unitDataList[UnityEngine.Random.Range(0, unitDataList.Count)];
        }
        
        /// <summary>
        /// Kiểm tra xem có UnitData với ID cụ thể không
        /// </summary>
        public bool HasUnitData(string unitId)
        {
            return !string.IsNullOrEmpty(unitId) && _unitDataCache.ContainsKey(unitId);
        }
        
        /// <summary>
        /// Kiểm tra xem có SquadData với ID cụ thể không
        /// </summary>
        public bool HasSquadData(string squadId)
        {
            return !string.IsNullOrEmpty(squadId) && _squadDataCache.ContainsKey(squadId);
        }

        #endregion

        #region Debug and Utility Methods

        /// <summary>
        /// Màu cho thanh progress bar
        /// </summary>
        private Color GetLoadProgressColor()
        {
            if (_loadProgress < 50f) return Color.red;
            if (_loadProgress < 100f) return Color.yellow;
            return Color.green;
        }
        
        /// <summary>
        /// In thống kê dữ liệu đã load
        /// </summary>
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
        
        /// <summary>
        /// Reload tất cả dữ liệu
        /// </summary>
        [Button("Reload All Data"), TitleGroup("Debug Tools")]
        public void ReloadAllData()
        {
            _isDataLoaded = false;
            LoadAllData();
        }
        
        /// <summary>
        /// Clear tất cả cache
        /// </summary>
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