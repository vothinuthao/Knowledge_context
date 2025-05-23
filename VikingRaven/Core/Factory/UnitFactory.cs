using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using VikingRaven.Core.ObjectPooling;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Data;
using VikingRaven.Units.Data;
using VikingRaven.Units.Models;
using VikingRaven.Units.Components;

namespace VikingRaven.Core.Factory
{
    /// <summary>
    /// Enhanced Factory for creating and managing units with dynamic data caching
    /// Uses UnitDataSO cache and dynamic prefab loading
    /// </summary>
    public class UnitFactory : MonoBehaviour
    {
        #region Inspector Fields

        [TitleGroup("Cache Settings")]
        [Tooltip("Tự động tải cache UnitDataSO khi khởi động")]
        [SerializeField, ToggleLeft] 
        private bool _autoLoadCache = true;
        
        [Tooltip("Kích thước pool mặc định cho mỗi loại unit")]
        [SerializeField, Range(5, 50), ProgressBar(5, 50)] 
        private int _defaultPoolSize = 20;
        
        [TitleGroup("Pool Settings")]
        [Tooltip("Pool có thể mở rộng khi hết unit")]
        [SerializeField, ToggleLeft] 
        private bool _expandPoolsWhenEmpty = true;
        
        [Tooltip("Số lượng unit tối đa mỗi pool có thể chứa")]
        [SerializeField, Range(50, 200)] 
        private int _maxPoolSize = 100;
        
        [TitleGroup("Debug Information")]
        [ShowInInspector, ReadOnly, ProgressBar(0, 1000)] 
        private int ActiveUnitsCount => _activeEntities?.Count ?? 0;
        
        [ShowInInspector, ReadOnly] 
        private int CachedUnitDataCount => _unitDataCache?.Count ?? 0;
        
        [ShowInInspector, ReadOnly] 
        private int UnitModelTemplatesCount => _unitModelTemplates?.Count ?? 0;
        
        [ShowInInspector, ReadOnly] 
        private int ActivePoolsCount => _unitPools?.Count ?? 0;

        #endregion

        #region Private Fields

        // Cache hệ thống
        private Dictionary<string, UnitDataSO> _unitDataCache = new Dictionary<string, UnitDataSO>();
        private Dictionary<UnitType, List<UnitDataSO>> _unitDataByType = new Dictionary<UnitType, List<UnitDataSO>>();
        private Dictionary<string, UnitModel> _unitModelTemplates = new Dictionary<string, UnitModel>();
        
        // Pool hệ thống động
        private Dictionary<string, ObjectPool<BaseEntity>> _unitPools = new Dictionary<string, ObjectPool<BaseEntity>>();
        private Dictionary<string, Transform> _poolParents = new Dictionary<string, Transform>();
        
        // Tracking hệ thống
        private Dictionary<int, BaseEntity> _activeEntities = new Dictionary<int, BaseEntity>();
        private Dictionary<int, UnitModel> _unitModelsById = new Dictionary<int, UnitModel>();
        private Dictionary<string, List<int>> _unitsByDataId = new Dictionary<string, List<int>>();
        
        private int _nextEntityId = 1000;

        #endregion

        #region Properties

        private EntityRegistry EntityRegistry => EntityRegistry.Instance;
        private DataManager DataManager => DataManager.Instance;
        
        // Events
        public delegate void UnitEvent(UnitModel unitModel);
        public event UnitEvent OnUnitCreated;
        public event UnitEvent OnUnitReturned;
        public event UnitEvent OnUnitDestroyed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeUnitTypeCollections();
            
            if (_autoLoadCache)
            {
                LoadUnitDataCache();
                CreateUnitModelTemplates();
            }
        }

        private void OnDestroy()
        {
            CleanupAllResources();
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// Khởi tạo collections cho các loại unit
        /// </summary>
        private void InitializeUnitTypeCollections()
        {
            _unitDataByType[UnitType.Infantry] = new List<UnitDataSO>();
            _unitDataByType[UnitType.Archer] = new List<UnitDataSO>();
            _unitDataByType[UnitType.Pike] = new List<UnitDataSO>();
        }

        /// <summary>
        /// Tải cache UnitDataSO từ DataManager
        /// </summary>
        [Button("Load Unit Data Cache"), TitleGroup("Cache Management")]
        private void LoadUnitDataCache()
        {
            if (DataManager == null || !DataManager.IsInitialized)
            {
                Debug.LogWarning("EnhancedUnitFactory: DataManager không khả dụng cho việc tải cache");
                return;
            }

            // Clear existing cache
            _unitDataCache.Clear();
            foreach (var typeList in _unitDataByType.Values)
            {
                typeList.Clear();
            }

            // Load all unit data
            List<UnitDataSO> allUnitData = DataManager.GetAllUnitData();
            
            foreach (var unitData in allUnitData)
            {
                if (unitData != null && !string.IsNullOrEmpty(unitData.UnitId))
                {
                    // Add to main cache
                    _unitDataCache[unitData.UnitId] = unitData;
                    
                    // Add to type-specific cache
                    if (_unitDataByType.ContainsKey(unitData.UnitType))
                    {
                        _unitDataByType[unitData.UnitType].Add(unitData);
                    }
                }
            }

            Debug.Log($"EnhancedUnitFactory: Đã tải {_unitDataCache.Count} UnitDataSO vào cache");
        }

        /// <summary>
        /// Tạo UnitModel templates từ cache data
        /// </summary>
        [Button("Create Unit Model Templates"), TitleGroup("Cache Management")]
        private void CreateUnitModelTemplates()
        {
            _unitModelTemplates.Clear();
            
            foreach (var kvp in _unitDataCache)
            {
                string unitId = kvp.Key;
                UnitDataSO unitData = kvp.Value;
                
                // Tạo template UnitModel (không có entity thật)
                UnitModel templateModel = new UnitModel(null, unitData);
                _unitModelTemplates[unitId] = templateModel;
            }
            
            Debug.Log($"EnhancedUnitFactory: Đã tạo {_unitModelTemplates.Count} UnitModel templates");
        }

        #endregion

        #region Dynamic Pool Management

        /// <summary>
        /// Lấy hoặc tạo pool cho một UnitDataSO cụ thể
        /// </summary>
        private ObjectPool<BaseEntity> GetOrCreatePool(UnitDataSO unitData)
        {
            if (unitData == null || unitData.Prefab == null)
            {
                Debug.LogError("EnhancedUnitFactory: UnitData hoặc Prefab là null");
                return null;
            }

            string poolKey = unitData.UnitId;
            
            // Kiểm tra pool đã tồn tại
            if (_unitPools.TryGetValue(poolKey, out ObjectPool<BaseEntity> existingPool))
            {
                return existingPool;
            }

            // Tạo pool mới
            return CreateNewPool(unitData);
        }

        /// <summary>
        /// Tạo pool mới cho UnitDataSO
        /// </summary>
        private ObjectPool<BaseEntity> CreateNewPool(UnitDataSO unitData)
        {
            string poolKey = unitData.UnitId;
            
            // Tạo parent transform cho pool
            Transform poolParent = CreatePoolParent($"Pool_{unitData.DisplayName}");
            _poolParents[poolKey] = poolParent;
            
            // Lấy BaseEntity component từ prefab
            BaseEntity prefabEntity = unitData.Prefab.GetComponent<BaseEntity>();
            if (prefabEntity == null)
            {
                Debug.LogError($"EnhancedUnitFactory: Prefab {unitData.Prefab.name} không có BaseEntity component");
                return null;
            }

            // Tạo object pool
            ObjectPool<BaseEntity> newPool = new ObjectPool<BaseEntity>(
                prefabEntity,
                _defaultPoolSize,
                poolParent,
                _expandPoolsWhenEmpty,
                entity => OnReturnUnit(entity, unitData),
                entity => OnGetUnit(entity, unitData)
            );

            _unitPools[poolKey] = newPool;
            
            Debug.Log($"EnhancedUnitFactory: Đã tạo pool cho {unitData.DisplayName} với {_defaultPoolSize} units");
            
            return newPool;
        }

        /// <summary>
        /// Tạo parent transform cho pool
        /// </summary>
        private Transform CreatePoolParent(string name)
        {
            GameObject parent = new GameObject(name);
            parent.transform.SetParent(transform);
            return parent.transform;
        }

        #endregion

        #region Pool Callbacks

        /// <summary>
        /// Callback khi unit được trả về pool
        /// </summary>
        private void OnReturnUnit(BaseEntity entity, UnitDataSO unitData)
        {
            if (entity == null) return;

            int entityId = entity.Id;
            
            // Lấy unit model
            if (_unitModelsById.TryGetValue(entityId, out UnitModel unitModel))
            {
                // Trigger event
                OnUnitReturned?.Invoke(unitModel);
                
                // Remove from tracking
                _unitModelsById.Remove(entityId);
                
                // Remove from units by data tracking
                if (_unitsByDataId.TryGetValue(unitData.UnitId, out List<int> unitList))
                {
                    unitList.Remove(entityId);
                    if (unitList.Count == 0)
                    {
                        _unitsByDataId.Remove(unitData.UnitId);
                    }
                }
            }

            // Reset entity state
            ResetEntityState(entity);
            
            // Remove from active entities
            _activeEntities.Remove(entityId);
            
            Debug.Log($"EnhancedUnitFactory: Đã trả unit {entityId} về pool {unitData.UnitId}");
        }

        /// <summary>
        /// Callback khi unit được lấy từ pool
        /// </summary>
        private void OnGetUnit(BaseEntity entity, UnitDataSO unitData)
        {
            if (entity == null) return;

            // Assign new entity ID
            AssignEntityId(entity);
            
            // Register with EntityRegistry
            if (EntityRegistry != null)
            {
                EntityRegistry.RegisterEntity(entity);
            }
            
            // Add to active entities
            _activeEntities[entity.Id] = entity;
            
            // Initialize components with unit data
            InitializeEntityWithData(entity, unitData);
            
            Debug.Log($"EnhancedUnitFactory: Đã lấy unit {entity.Id} từ pool {unitData.UnitId}");
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Tạo unit từ UnitType (random từ available data)
        /// </summary>
        public IEntity CreateUnit(UnitType unitType, Vector3 position, Quaternion rotation)
        {
            UnitDataSO unitData = GetRandomUnitDataByType(unitType);
            if (unitData == null)
            {
                Debug.LogError($"EnhancedUnitFactory: Không tìm thấy UnitData cho type {unitType}");
                return null;
            }

            return CreateUnitFromData(unitData, position, rotation);
        }

        /// <summary>
        /// Tạo unit từ UnitDataSO ID
        /// </summary>
        public IEntity CreateUnitFromId(string unitId, Vector3 position, Quaternion rotation)
        {
            if (!_unitDataCache.TryGetValue(unitId, out UnitDataSO unitData))
            {
                Debug.LogError($"EnhancedUnitFactory: Không tìm thấy UnitData với ID {unitId}");
                return null;
            }

            return CreateUnitFromData(unitData, position, rotation);
        }

        /// <summary>
        /// Tạo unit từ UnitDataSO (main factory method)
        /// </summary>
        public IEntity CreateUnitFromData(UnitDataSO unitData, Vector3 position, Quaternion rotation)
        {
            if (unitData == null)
            {
                Debug.LogError("EnhancedUnitFactory: UnitData là null");
                return null;
            }

            // Bước 1: Tạo UnitModel từ template
            UnitModel unitModel = CreateUnitModelFromData(unitData);
            if (unitModel == null)
            {
                Debug.LogError($"EnhancedUnitFactory: Không thể tạo UnitModel cho {unitData.UnitId}");
                return null;
            }

            // Bước 2: Lấy pool và entity
            ObjectPool<BaseEntity> pool = GetOrCreatePool(unitData);
            if (pool == null)
            {
                Debug.LogError($"EnhancedUnitFactory: Không thể tạo pool cho {unitData.UnitId}");
                return null;
            }

            BaseEntity entity = pool.Get();
            if (entity == null)
            {
                Debug.LogError($"EnhancedUnitFactory: Không thể lấy entity từ pool {unitData.UnitId}");
                return null;
            }

            // Bước 3: Setup entity
            entity.transform.position = position;
            entity.transform.rotation = rotation;

            // Bước 4: Kết nối UnitModel với Entity
            unitModel = new UnitModel(entity, unitData); // Tạo lại với entity thật
            _unitModelsById[entity.Id] = unitModel;

            // Bước 5: Tracking
            if (!_unitsByDataId.ContainsKey(unitData.UnitId))
            {
                _unitsByDataId[unitData.UnitId] = new List<int>();
            }
            _unitsByDataId[unitData.UnitId].Add(entity.Id);

            // Bước 6: Trigger event
            OnUnitCreated?.Invoke(unitModel);

            Debug.Log($"EnhancedUnitFactory: Đã tạo unit {entity.Id} từ data {unitData.UnitId} tại {position}");

            return entity;
        }

        /// <summary>
        /// Tạo multiple units cùng loại
        /// </summary>
        public List<IEntity> CreateMultipleUnits(UnitType unitType, int count, Vector3 basePosition, float spacing = 1f)
        {
            List<IEntity> createdUnits = new List<IEntity>();

            for (int i = 0; i < count; i++)
            {
                // Tính toán vị trí với spacing
                Vector3 offset = new Vector3(
                    (i % 3) * spacing - spacing,
                    0,
                    (i / 3) * spacing
                );
                Vector3 position = basePosition + offset;

                IEntity unit = CreateUnit(unitType, position, Quaternion.identity);
                if (unit != null)
                {
                    createdUnits.Add(unit);
                }
            }

            Debug.Log($"EnhancedUnitFactory: Đã tạo {createdUnits.Count}/{count} units loại {unitType}");

            return createdUnits;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Tạo UnitModel từ UnitDataSO (sử dụng template nếu có)
        /// </summary>
        private UnitModel CreateUnitModelFromData(UnitDataSO unitData)
        {
            if (_unitModelTemplates.TryGetValue(unitData.UnitId, out UnitModel template))
            {
                // Clone template
                return template.Clone();
            }

            // Tạo mới nếu không có template
            return new UnitModel(null, unitData);
        }

        /// <summary>
        /// Lấy random UnitDataSO theo type
        /// </summary>
        private UnitDataSO GetRandomUnitDataByType(UnitType unitType)
        {
            if (_unitDataByType.TryGetValue(unitType, out List<UnitDataSO> dataList) && dataList.Count > 0)
            {
                return dataList[Random.Range(0, dataList.Count)];
            }
            return null;
        }

        /// <summary>
        /// Khởi tạo entity với data
        /// </summary>
        private void InitializeEntityWithData(BaseEntity entity, UnitDataSO unitData)
        {
            // Đảm bảo tất cả components được khởi tạo đúng
            var components = entity.GetComponents<IComponent>();
            foreach (var component in components)
            {
                if (component.Entity == null)
                {
                    component.Entity = entity;
                }
                component.Initialize();
            }

            // Set unit type nếu có component
            var unitTypeComponent = entity.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent != null)
            {
                // unitTypeComponent.SetUnitType(unitData.UnitType);
            }
        }

        /// <summary>
        /// Reset trạng thái entity khi trả về pool
        /// </summary>
        private void ResetEntityState(BaseEntity entity)
        {
            var healthComponent = entity.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                healthComponent.Revive();
            }
            var navigationComponent = entity.GetComponent<NavigationComponent>();
            if (navigationComponent != null)
            {
                navigationComponent.DisablePathfinding();
            }
        }

        /// <summary>
        /// Assign entity ID
        /// </summary>
        private void AssignEntityId(BaseEntity entity)
        {
            var idField = typeof(BaseEntity).GetField("_id", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            idField?.SetValue(entity, _nextEntityId++);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Trả unit về pool
        /// </summary>
        public void ReturnUnit(IEntity entity)
        {
            if (entity == null) return;

            var baseEntity = entity as BaseEntity;
            if (baseEntity == null) return;

            // Tìm pool tương ứng
            UnitModel unitModel = GetUnitModel(entity.Id);
            if (unitModel == null)
            {
                Debug.LogError($"EnhancedUnitFactory: Không tìm thấy UnitModel cho entity {entity.Id}");
                return;
            }

            // Tìm UnitData để xác định pool
            UnitDataSO unitData = GetUnitDataById(unitModel.UnitId);
            if (unitData == null)
            {
                Debug.LogError($"EnhancedUnitFactory: Không tìm thấy UnitData cho {unitModel.UnitId}");
                return;
            }

            // Trả về pool
            if (_unitPools.TryGetValue(unitData.UnitId, out ObjectPool<BaseEntity> pool))
            {
                pool.Return(baseEntity);
            }
        }

        /// <summary>
        /// Lấy UnitModel theo entity ID
        /// </summary>
        public UnitModel GetUnitModel(int entityId)
        {
            _unitModelsById.TryGetValue(entityId, out UnitModel model);
            return model;
        }

        /// <summary>
        /// Lấy UnitModel từ entity
        /// </summary>
        public UnitModel GetUnitModel(IEntity entity)
        {
            return entity != null ? GetUnitModel(entity.Id) : null;
        }

        /// <summary>
        /// Lấy UnitDataSO theo ID
        /// </summary>
        public UnitDataSO GetUnitDataById(string unitId)
        {
            _unitDataCache.TryGetValue(unitId, out UnitDataSO unitData);
            return unitData;
        }

        /// <summary>
        /// Lấy tất cả UnitData theo type
        /// </summary>
        public List<UnitDataSO> GetUnitDataByType(UnitType unitType)
        {
            return _unitDataByType.TryGetValue(unitType, out List<UnitDataSO> dataList) 
                ? new List<UnitDataSO>(dataList) 
                : new List<UnitDataSO>();
        }

        /// <summary>
        /// Lấy tất cả units đang active theo data ID
        /// </summary>
        public List<IEntity> GetActiveUnitsByDataId(string unitId)
        {
            List<IEntity> result = new List<IEntity>();
            
            if (_unitsByDataId.TryGetValue(unitId, out List<int> entityIds))
            {
                foreach (int id in entityIds)
                {
                    if (_activeEntities.TryGetValue(id, out BaseEntity entity))
                    {
                        result.Add(entity);
                    }
                }
            }
            
            return result;
        }

        #endregion

        #region Debug Tools

        [Button("Return All Units"), TitleGroup("Debug Tools")]
        public void ReturnAllUnits()
        {
            List<int> entityIds = new List<int>(_activeEntities.Keys);
            
            foreach (int id in entityIds)
            {
                if (_activeEntities.TryGetValue(id, out BaseEntity entity))
                {
                    ReturnUnit(entity);
                }
            }
            
            Debug.Log($"EnhancedUnitFactory: Đã trả {entityIds.Count} units về pool");
        }

        [Button("Clear All Pools"), TitleGroup("Debug Tools")]
        public void ClearAllPools()
        {
            ReturnAllUnits();
            
            foreach (var pool in _unitPools.Values)
            {
                pool.Clear();
            }
            
            _unitPools.Clear();
            _poolParents.Clear();
            
            Debug.Log("EnhancedUnitFactory: Đã xóa tất cả pools");
        }

        [Button("Show Cache Stats"), TitleGroup("Debug Tools")]
        public void ShowCacheStats()
        {
            string stats = "=== Enhanced Unit Factory Stats ===\n";
            stats += $"Cached Unit Data: {_unitDataCache.Count}\n";
            stats += $"Unit Model Templates: {_unitModelTemplates.Count}\n";
            stats += $"Active Pools: {_unitPools.Count}\n";
            stats += $"Active Units: {_activeEntities.Count}\n";
            
            stats += "\n=== Units by Type ===\n";
            foreach (var kvp in _unitDataByType)
            {
                stats += $"{kvp.Key}: {kvp.Value.Count} data entries\n";
            }
            
            Debug.Log(stats);
        }

        /// <summary>
        /// Cleanup tất cả resources
        /// </summary>
        private void CleanupAllResources()
        {
            ReturnAllUnits();
            ClearAllPools();
            
            _unitDataCache.Clear();
            _unitModelTemplates.Clear();
            _unitModelsById.Clear();
            _unitsByDataId.Clear();
        }

        #endregion
    }
}