using System;
using System.Collections.Generic;
using Core.Utils;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.Data;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;
using VikingRaven.Units.Models;
using VikingRaven.Core.Factory;
using Random = UnityEngine.Random;

namespace VikingRaven.Core.Factory
{
    public class SquadFactory : Singleton<SquadFactory>
    {
        #region Inspector Fields
        
        [TitleGroup("Squad Configuration")]
        [Tooltip("Số lượng units trong mỗi squad")]
        [SerializeField, Range(3, 15), ProgressBar(3, 15)]
        private int _unitsPerSquad = 9;
        
        [Tooltip("Khoảng cách spawn giữa các units trong squad")]
        [SerializeField, Range(0.5f, 3f)]
        private float _unitSpacing = 1.5f;
        
        [Tooltip("Tự động assign SquadController cho squad mới")]
        [SerializeField, ToggleLeft]
        private bool _autoAssignController = true;
        
        [TitleGroup("Cache Settings")]
        [Tooltip("Tự động load cache SquadDataSO khi khởi động")]
        [SerializeField, ToggleLeft]
        private bool _autoLoadSquadCache = true;
        
        [Tooltip("Số lượng squad template tối đa lưu trong cache")]
        [SerializeField, Range(10, 100)]
        private int _maxCacheSize = 50;
        
        [TitleGroup("Debug Information")]
        [ShowInInspector, ReadOnly, ProgressBar(0, 100)]
        private int ActiveSquadsCount => _activeSquads?.Count ?? 0;
        
        [ShowInInspector, ReadOnly]
        private int CachedSquadDataCount => _squadDataCache?.Count ?? 0;
        
        [ShowInInspector, ReadOnly]
        private int SquadModelTemplatesCount => _squadModelTemplates?.Count ?? 0;
        
        [ShowInInspector, ReadOnly]
        [PropertySpace(SpaceBefore = 10)]
        private string FactoryStatus => _isInitialized ? "Initialized" : "Not Initialized";

        #endregion

        #region Private Fields

        // Cache system
        private Dictionary<string, SquadDataSO> _squadDataCache = new Dictionary<string, SquadDataSO>();
        private Dictionary<string, SquadModel> _squadModelTemplates = new Dictionary<string, SquadModel>();
        private Dictionary<UnitType, List<SquadDataSO>> _squadDataByPrimaryType = new Dictionary<UnitType, List<SquadDataSO>>();
        
        // Squad tracking
        private Dictionary<int, SquadModel> _activeSquads = new Dictionary<int, SquadModel>();
        private Dictionary<string, List<int>> _squadsByTemplateId = new Dictionary<string, List<int>>();
        
        // Categorized squads
        private List<int> _playerSquadIds = new List<int>();
        private List<int> _enemySquadIds = new List<int>();
        private List<int> _neutralSquadIds = new List<int>();
        
        // ID management
        private int _nextSquadId = 1;
        private bool _isInitialized = false;

        #endregion

        #region Properties

        // References
        private DataManager DataManager => DataManager.Instance;
        private UnitFactory UnitFactory => FindObjectOfType<UnitFactory>();
        
        // Events
        public delegate void SquadEvent(SquadModel squadModel);
        public event SquadEvent OnSquadCreated;
        public event SquadEvent OnSquadDisbanded;

        #endregion

        #region Unity Lifecycle

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            InitializeSquadTypeCollections();
            
            if (_autoLoadSquadCache)
            {
                LoadSquadDataCache();
                CreateSquadModelTemplates();
            }
            
            _isInitialized = true;
            Debug.Log("EnhancedSquadFactory: Đã khởi tạo thành công");
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// Khởi tạo collections cho các loại squad theo unit type chính
        /// </summary>
        private void InitializeSquadTypeCollections()
        {
            _squadDataByPrimaryType[UnitType.Infantry] = new List<SquadDataSO>();
            _squadDataByPrimaryType[UnitType.Archer] = new List<SquadDataSO>();
            _squadDataByPrimaryType[UnitType.Pike] = new List<SquadDataSO>();
        }

        /// <summary>
        /// Load cache SquadDataSO từ DataManager
        /// </summary>
        [Button("Load Squad Data Cache"), TitleGroup("Cache Management")]
        private void LoadSquadDataCache()
        {
            if (DataManager == null || !DataManager.IsInitialized)
            {
                Debug.LogWarning("EnhancedSquadFactory: DataManager không khả dụng");
                return;
            }

            // Clear existing cache
            _squadDataCache.Clear();
            foreach (var typeList in _squadDataByPrimaryType.Values)
            {
                typeList.Clear();
            }

            // Load all squad data
            List<SquadDataSO> allSquadData = DataManager.GetAllSquadData();
            
            foreach (var squadData in allSquadData)
            {
                if (squadData != null && !string.IsNullOrEmpty(squadData.SquadId))
                {
                    // Add to main cache
                    _squadDataCache[squadData.SquadId] = squadData;
                    
                    // Determine primary unit type and add to type-specific cache
                    UnitType primaryType = DeterminePrimaryUnitType(squadData);
                    if (_squadDataByPrimaryType.ContainsKey(primaryType))
                    {
                        _squadDataByPrimaryType[primaryType].Add(squadData);
                    }
                }
            }

            Debug.Log($"EnhancedSquadFactory: Đã load {_squadDataCache.Count} SquadDataSO vào cache");
        }

        /// <summary>
        /// Xác định loại unit chính của squad dựa trên composition
        /// </summary>
        private UnitType DeterminePrimaryUnitType(SquadDataSO squadData)
        {
            Dictionary<UnitType, int> typeCounts = squadData.GetUnitTypeCounts();
            
            UnitType primaryType = UnitType.Infantry; // Default
            int maxCount = 0;
            
            foreach (var kvp in typeCounts)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    primaryType = kvp.Key;
                }
            }
            
            return primaryType;
        }

        /// <summary>
        /// Tạo SquadModel templates từ cache data
        /// </summary>
        [Button("Create Squad Model Templates"), TitleGroup("Cache Management")]
        private void CreateSquadModelTemplates()
        {
            _squadModelTemplates.Clear();
            
            foreach (var kvp in _squadDataCache)
            {
                string squadId = kvp.Key;
                SquadDataSO squadData = kvp.Value;
                
                // Tạo template SquadModel (với ID âm để đánh dấu là template)
                SquadModel templateModel = new SquadModel(-1, squadData, Vector3.zero, Quaternion.identity);
                _squadModelTemplates[squadId] = templateModel;
            }
            
            Debug.Log($"EnhancedSquadFactory: Đã tạo {_squadModelTemplates.Count} SquadModel templates");
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Tạo squad từ template ID
        /// </summary>
        public SquadModel CreateSquadFromTemplate(string squadTemplateId, Vector3 position, Quaternion rotation, bool isEnemy = false)
        {
            // Kiểm tra template trong cache
            if (!_squadModelTemplates.TryGetValue(squadTemplateId, out SquadModel templateModel))
            {
                // Nếu không có trong cache, thử lấy từ DataManager
                SquadDataSO squadData = DataManager?.GetSquadData(squadTemplateId);
                if (squadData == null)
                {
                    Debug.LogError($"EnhancedSquadFactory: Không tìm thấy squad template {squadTemplateId}");
                    return null;
                }
                
                // Tạo template mới
                templateModel = new SquadModel(-1, squadData, Vector3.zero, Quaternion.identity);
                _squadModelTemplates[squadTemplateId] = templateModel;
            }

            return CreateSquadFromData(templateModel.Data, position, rotation, isEnemy);
        }

        /// <summary>
        /// Tạo squad từ SquadDataSO
        /// </summary>
        public SquadModel CreateSquadFromData(SquadDataSO squadData, Vector3 position, Quaternion rotation, bool isEnemy = false)
        {
            if (squadData == null)
            {
                Debug.LogError("EnhancedSquadFactory: SquadData là null");
                return null;
            }

            if (UnitFactory == null)
            {
                Debug.LogError("EnhancedSquadFactory: Không tìm thấy UnitFactory");
                return null;
            }

            // Bước 1: Tạo SquadModel
            int squadId = _nextSquadId++;
            SquadModel squadModel = new SquadModel(squadId, squadData, position, rotation);

            // Bước 2: Tạo units theo composition
            List<UnitModel> squadUnits = CreateUnitsFromComposition(squadData, position, rotation);
            
            if (squadUnits.Count == 0)
            {
                Debug.LogError($"EnhancedSquadFactory: Không thể tạo units cho squad {squadId}");
                return null;
            }

            // Bước 3: Assign squad ID cho tất cả units
            foreach (var unitModel in squadUnits)
            {
                unitModel.SetSquadId(squadId);
                
                // Set formation component nếu có
                var formationComponent = unitModel.Entity?.GetComponent<FormationComponent>();
                if (formationComponent != null)
                {
                    formationComponent.SetFormationType(squadData.DefaultFormationType);
                }
            }
            squadModel.AddUnits(squadUnits);
            _activeSquads[squadId] = squadModel;
            CategorizeSquad(squadId, squadData.Faction, isEnemy);
            if (!_squadsByTemplateId.ContainsKey(squadData.SquadId))
            {
                _squadsByTemplateId[squadData.SquadId] = new List<int>();
            }
            _squadsByTemplateId[squadData.SquadId].Add(squadId);

            OnSquadCreated?.Invoke(squadModel);

            Debug.Log($"EnhancedSquadFactory: Đã tạo {(isEnemy ? "enemy" : "player")} squad {squadId} " +
                     $"với {squadUnits.Count} units từ template {squadData.DisplayName}");

            return squadModel;
        }
        public SquadModel CreateSimpleSquad(UnitType unitType, Vector3 position, Quaternion rotation, bool isEnemy = false)
        {
            if (UnitFactory == null)
            {
                Debug.LogError("EnhancedSquadFactory: Không tìm thấy UnitFactory");
                return null;
            }

            // Tìm template phù hợp
            SquadDataSO matchingData = FindBestSquadTemplateForUnitType(unitType);
            
            if (matchingData != null)
            {
                return CreateSquadFromData(matchingData, position, rotation, isEnemy);
            }

            // Tạo squad generic nếu không tìm thấy template
            return CreateGenericSquad(unitType, position, rotation, isEnemy);
        }

        private SquadModel CreateGenericSquad(UnitType unitType, Vector3 position, Quaternion rotation, bool isEnemy)
        {
            int squadId = _nextSquadId++;
            SquadModel squadModel = new SquadModel(squadId, null, position, rotation);

            List<UnitModel> squadUnits = new List<UnitModel>();
            
            for (int i = 0; i < _unitsPerSquad; i++)
            {
                Vector3 spawnPosition = CalculateUnitSpawnPosition(position, i);
                
                IEntity unitEntity = UnitFactory.CreateUnit(unitType, spawnPosition, rotation);
                if (unitEntity != null)
                {
                    UnitModel unitModel = UnitFactory.GetUnitModel(unitEntity);
                    if (unitModel != null)
                    {
                        unitModel.SetSquadId(squadId);
                        squadUnits.Add(unitModel);
                        
                    }
                }
            }

            // Add units to squad
            squadModel.AddUnits(squadUnits);

            // Store squad
            _activeSquads[squadId] = squadModel;

            // Categorize
            CategorizeSquad(squadId, isEnemy ? "Enemy" : "Player", isEnemy);


            // Trigger event
            OnSquadCreated?.Invoke(squadModel);

            Debug.Log($"EnhancedSquadFactory: Đã tạo generic squad {squadId} với {squadUnits.Count} {unitType} units");

            return squadModel;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Tạo units từ squad composition
        /// </summary>
        private List<UnitModel> CreateUnitsFromComposition(SquadDataSO squadData, Vector3 position, Quaternion rotation)
        {
            List<UnitModel> squadUnits = new List<UnitModel>();
            int unitIndex = 0;

            foreach (var composition in squadData.UnitCompositions)
            {
                if (composition.UnitData == null || composition.Count <= 0)
                    continue;

                for (int i = 0; i < composition.Count; i++)
                {
                    Vector3 spawnPosition = CalculateUnitSpawnPosition(position, unitIndex);
                    
                    IEntity unitEntity = UnitFactory.CreateUnitFromData(
                        composition.UnitData, 
                        spawnPosition, 
                        rotation
                    );

                    if (unitEntity != null)
                    {
                        UnitModel unitModel = UnitFactory.GetUnitModel(unitEntity);
                        if (unitModel != null)
                        {
                            squadUnits.Add(unitModel);
                        }
                    }

                    unitIndex++;
                }
            }

            return squadUnits;
        }

        /// <summary>
        /// Tính toán vị trí spawn cho unit trong squad
        /// </summary>
        private Vector3 CalculateUnitSpawnPosition(Vector3 basePosition, int unitIndex)
        {
            // Tạo pattern grid 3x3 cho squad
            int row = unitIndex / 3;
            int col = unitIndex % 3;
            
            Vector3 offset = new Vector3(
                (col - 1) * _unitSpacing,
                0,
                row * _unitSpacing
            );
            
            // Add small random để tránh overlap hoàn toàn
            offset += new Vector3(
                Random.Range(-0.1f, 0.1f),
                0,
                Random.Range(-0.1f, 0.1f)
            );

            return basePosition + offset;
        }

        /// <summary>
        /// Phân loại squad theo faction
        /// </summary>
        private void CategorizeSquad(int squadId, string faction, bool isEnemy)
        {
            if (isEnemy || faction == "Enemy")
            {
                _enemySquadIds.Add(squadId);
            }
            else if (faction == "Neutral")
            {
                _neutralSquadIds.Add(squadId);
            }
            else
            {
                _playerSquadIds.Add(squadId);
            }
        }

        /// <summary>
        /// Tìm squad template tốt nhất cho unit type
        /// </summary>
        private SquadDataSO FindBestSquadTemplateForUnitType(UnitType unitType)
        {
            if (_squadDataByPrimaryType.TryGetValue(unitType, out List<SquadDataSO> templates) && templates.Count > 0)
            {
                // Trả về template đầu tiên hoặc random
                return templates[Random.Range(0, templates.Count)];
            }
            return null;
        }


        #endregion

        #region Public API

        /// <summary>
        /// Disband squad và return units về pool
        /// </summary>
        public void DisbandSquad(int squadId)
        {
            if (!_activeSquads.TryGetValue(squadId, out SquadModel squadModel))
            {
                Debug.LogWarning($"EnhancedSquadFactory: Squad {squadId} không tồn tại");
                return;
            }

            // Trigger event trước khi disband
            OnSquadDisbanded?.Invoke(squadModel);

            // Return all units
            List<IEntity> unitEntities = squadModel.GetAllUnitEntities();
            foreach (var entity in unitEntities)
            {
                if (UnitFactory != null && entity != null)
                {
                    UnitFactory.ReturnUnit(entity);
                }
            }
            // Cleanup squad model
            squadModel.Cleanup();

            // Remove from tracking
            _activeSquads.Remove(squadId);
            _playerSquadIds.Remove(squadId);
            _enemySquadIds.Remove(squadId);
            _neutralSquadIds.Remove(squadId);

            // Remove from template tracking
            foreach (var templateList in _squadsByTemplateId.Values)
            {
                templateList.Remove(squadId);
            }

            Debug.Log($"EnhancedSquadFactory: Đã disband squad {squadId} với {unitEntities.Count} units");
        }

        /// <summary>
        /// Lấy squad model theo ID
        /// </summary>
        public SquadModel GetSquad(int squadId)
        {
            _activeSquads.TryGetValue(squadId, out SquadModel squadModel);
            return squadModel;
        }

        /// <summary>
        /// Lấy tất cả active squads
        /// </summary>
        public List<SquadModel> GetAllSquads()
        {
            return new List<SquadModel>(_activeSquads.Values);
        }

        /// <summary>
        /// Lấy player squads
        /// </summary>
        public List<SquadModel> GetPlayerSquads()
        {
            List<SquadModel> result = new List<SquadModel>();
            foreach (int squadId in _playerSquadIds)
            {
                if (_activeSquads.TryGetValue(squadId, out SquadModel squad))
                {
                    result.Add(squad);
                }
            }
            return result;
        }

        /// <summary>
        /// Lấy enemy squads
        /// </summary>
        public List<SquadModel> GetEnemySquads()
        {
            List<SquadModel> result = new List<SquadModel>();
            foreach (int squadId in _enemySquadIds)
            {
                if (_activeSquads.TryGetValue(squadId, out SquadModel squad))
                {
                    result.Add(squad);
                }
            }
            return result;
        }

        #endregion

        #region Debug Tools

        [Button("Create Test Squad"), TitleGroup("Debug Tools")]
        public void CreateTestSquad()
        {
            Vector3 position = Vector3.zero;
            CreateSimpleSquad(UnitType.Infantry, position, Quaternion.identity, false);
        }

        [Button("Disband All Squads"), TitleGroup("Debug Tools")]
        public void DisbandAllSquads()
        {
            List<int> squadIds = new List<int>(_activeSquads.Keys);
            foreach (int squadId in squadIds)
            {
                DisbandSquad(squadId);
            }
            Debug.Log($"EnhancedSquadFactory: Đã disband {squadIds.Count} squads");
        }

        [Button("Show Factory Stats"), TitleGroup("Debug Tools")]
        public void ShowFactoryStats()
        {
            string stats = "=== Enhanced Squad Factory Stats ===\n";
            stats += $"Cached Squad Data: {_squadDataCache.Count}\n";
            stats += $"Squad Model Templates: {_squadModelTemplates.Count}\n";
            stats += $"Active Squads: {_activeSquads.Count}\n";
            stats += $"Player Squads: {_playerSquadIds.Count}\n";
            stats += $"Enemy Squads: {_enemySquadIds.Count}\n";
            stats += $"Neutral Squads: {_neutralSquadIds.Count}\n";
            
            Debug.Log(stats);
        }

        #endregion
    }
}