using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.Data;
using VikingRaven.Core.ECS;
using VikingRaven.Core.ObjectPooling;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;
using VikingRaven.Units.Models;

namespace VikingRaven.Core.Factory
{
    /// <summary>
    /// Enhanced factory responsible for creating and managing units and their models
    /// Uses object pooling for efficient unit management
    /// </summary>
    public class UnitFactory : MonoBehaviour, IEntityFactory
    {
        // References to unit prefabs
        [SerializeField] private GameObject _infantryPrefab;
        [SerializeField] private GameObject _archerPrefab;
        [SerializeField] private GameObject _pikePrefab;
        
        // Initial pool sizes for each unit type
        [SerializeField] private int _infantryPoolSize = 20;
        [SerializeField] private int _archerPoolSize = 10;
        [SerializeField] private int _pikePoolSize = 10;
        
        [SerializeField] private bool _preloadOnAwake = true;
        
        // Object pools for each unit type
        private ObjectPool<BaseEntity> _infantryPool;
        private ObjectPool<BaseEntity> _archerPool;
        private ObjectPool<BaseEntity> _pikePool;
        
        // Dictionary to track all created units by ID
        private Dictionary<int, BaseEntity> _activeEntities = new Dictionary<int, BaseEntity>();
        
        // Dictionary to track unit models by entity ID
        private Dictionary<int, UnitModel> _unitModels = new Dictionary<int, UnitModel>();
        
        // Data cache for performance
        private Dictionary<UnitType, List<UnitDataSO>> _unitDataCache = new Dictionary<UnitType, List<UnitDataSO>>();
        
        // Next entity ID to assign
        private int _nextEntityId = 1000;
        
        // Reference to EntityRegistry
        private EntityRegistry EntityRegistry => EntityRegistry.Instance;
        private DataManager DataManager => DataManager.Instance;
        
        // Events
        public delegate void UnitEvent(UnitModel unitModel);
        public event UnitEvent OnUnitCreated;
        public event UnitEvent OnUnitReturned;

        /// <summary>
        /// Initialize the factory and object pools
        /// </summary>
        private void Awake()
        {
            // Initialize object pools
            InitializeObjectPools();
            
            // Preload data cache
            if (_preloadOnAwake)
            {
                PreloadDataCache();
            }
        }
        
        /// <summary>
        /// Initialize object pools for each unit type
        /// </summary>
        private void InitializeObjectPools()
        {
            // Create parent transform for each pool
            Transform infantryPoolParent = CreatePoolParent("InfantryPool");
            Transform archerPoolParent = CreatePoolParent("ArcherPool");
            Transform pikePoolParent = CreatePoolParent("PikePool");
            
            // Create the object pools
            if (_infantryPrefab != null)
            {
                _infantryPool = new ObjectPool<BaseEntity>(
                    _infantryPrefab.GetComponent<BaseEntity>(),
                    _infantryPoolSize,
                    infantryPoolParent,
                    true,
                    OnReturnUnit,
                    OnGetUnit
                );
                Debug.Log($"EnhancedUnitFactory: Created infantry pool with {_infantryPoolSize} units");
            }
            
            if (_archerPrefab != null)
            {
                _archerPool = new ObjectPool<BaseEntity>(
                    _archerPrefab.GetComponent<BaseEntity>(),
                    _archerPoolSize,
                    archerPoolParent,
                    true,
                    OnReturnUnit,
                    OnGetUnit
                );
                Debug.Log($"EnhancedUnitFactory: Created archer pool with {_archerPoolSize} units");
            }
            
            if (_pikePrefab != null)
            {
                _pikePool = new ObjectPool<BaseEntity>(
                    _pikePrefab.GetComponent<BaseEntity>(),
                    _pikePoolSize,
                    pikePoolParent,
                    true,
                    OnReturnUnit,
                    OnGetUnit
                );
                Debug.Log($"EnhancedUnitFactory: Created pike pool with {_pikePoolSize} units");
            }
        }
        
        /// <summary>
        /// Preload unit data from DataManager
        /// </summary>
        private void PreloadDataCache()
        {
            if (DataManager == null || !DataManager.IsInitialized)
            {
                Debug.LogWarning("EnhancedUnitFactory: DataManager not available for preloading data");
                return;
            }
            
            // Clear existing cache
            _unitDataCache.Clear();
            
            // Initialize cache for each unit type
            _unitDataCache[UnitType.Infantry] = new List<UnitDataSO>();
            _unitDataCache[UnitType.Archer] = new List<UnitDataSO>();
            _unitDataCache[UnitType.Pike] = new List<UnitDataSO>();
            
            // Load all unit data
            List<UnitDataSO> allUnitData = DataManager.GetAllUnitData();
            
            foreach (var unitData in allUnitData)
            {
                if (_unitDataCache.TryGetValue(unitData.UnitType, out var list))
                {
                    list.Add(unitData);
                }
            }
            
            Debug.Log($"EnhancedUnitFactory: Preloaded {allUnitData.Count} unit data templates");
        }
        
        /// <summary>
        /// Create a parent transform for a pool
        /// </summary>
        private Transform CreatePoolParent(string name)
        {
            GameObject parent = new GameObject(name);
            parent.transform.SetParent(transform);
            return parent.transform;
        }
        
        /// <summary>
        /// Callback when a unit is returned to the pool
        /// </summary>
        private void OnReturnUnit(BaseEntity entity)
        {
            if (entity == null) return;
            
            // Get the unit model
            UnitModel unitModel = null;
            if (_unitModels.TryGetValue(entity.Id, out unitModel))
            {
                // Clean up model
                unitModel.Cleanup();
                
                // Trigger event
                OnUnitReturned?.Invoke(unitModel);
                
                // Remove from tracking dictionaries
                _unitModels.Remove(entity.Id);
            }
            
            // Reset the unit's state
            var healthComponent = entity.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                healthComponent.Revive();
            }
            
            // Deactivate components that should be inactive
            var navigationComponent = entity.GetComponent<NavigationComponent>();
            if (navigationComponent != null)
            {
                navigationComponent.DisablePathfinding();
            }
            
            // Remove from active entities
            _activeEntities.Remove(entity.Id);
            
            // Unregister from EntityRegistry if needed
            if (EntityRegistry != null)
            {
                // We don't actually destroy the entity, just unregister it
                // EntityRegistry.UnregisterEntity(entity);
            }
            
            Debug.Log($"EnhancedUnitFactory: Returned entity {entity.Id} to pool");
        }
        
        /// <summary>
        /// Callback when a unit is taken from the pool
        /// </summary>
        private void OnGetUnit(BaseEntity entity)
        {
            if (entity == null) return;
            
            // Assign a new entity ID
            AssignEntityId(entity);
            
            // Register with EntityRegistry
            if (EntityRegistry != null)
            {
                EntityRegistry.RegisterEntity(entity);
            }
            
            // Add to active entities
            _activeEntities[entity.Id] = entity;
            
            // Initialize components
            InitializeEntityComponents(entity);
            
            Debug.Log($"EnhancedUnitFactory: Got entity {entity.Id} from pool");
        }

        /// <summary>
        /// Create a generic entity (default implementation for IEntityFactory)
        /// </summary>
        public IEntity CreateEntity(Vector3 position, Quaternion rotation)
        {
            return CreateUnit(UnitType.Infantry, position, rotation);
        }
        
        /// <summary>
        /// Create a unit of a specific type without a UnitModel
        /// </summary>
        public IEntity CreateUnit(UnitType unitType, Vector3 position, Quaternion rotation)
        {
            // Get unit data
            UnitDataSO unitData = GetUnitData(unitType);
            
            // Create entity with the data
            return CreateUnitFromData(unitData, position, rotation);
        }
        
        /// <summary>
        /// Create a unit from a specific UnitData with UnitModel
        /// </summary>
        public IEntity CreateUnitFromData(UnitDataSO unitData, Vector3 position, Quaternion rotation)
        {
            if (unitData == null)
            {
                Debug.LogError("dUnitFactory: Cannot create unit - unitData is null");
                return null;
            }
            
            // Get the appropriate pool based on unit type
            ObjectPool<BaseEntity> pool = GetPoolForUnitType(unitData.UnitType);
            
            if (pool == null)
            {
                Debug.LogError($"EnhancedUnitFactory: No pool available for unit type {unitData.UnitType}");
                return null;
            }
            
            // Get a unit from the pool
            BaseEntity entity = pool.Get();
            if (entity == null)
            {
                Debug.LogError($"EnhancedUnitFactory: Failed to get unit from pool for type {unitData.UnitType}");
                return null;
            }
            
            // Set position and rotation
            entity.transform.position = position;
            entity.transform.rotation = rotation;
            
            // Configure unit type if component exists
            var unitTypeComponent = entity.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent != null)
            {
                unitTypeComponent.SetUnitType(unitData.UnitType);
            }
            
            // Create UnitModel
            UnitModel unitModel = new UnitModel(entity, unitData);
            
            // Store in dictionary
            _unitModels[entity.Id] = unitModel;
            
            // Trigger event
            OnUnitCreated?.Invoke(unitModel);
            
            Debug.Log($"EnhancedUnitFactory: Created unit ID {entity.Id} of type {unitData.UnitType} at position {position}");
            
            return entity;
        }
        
        /// <summary>
        /// Get the appropriate pool for a unit type
        /// </summary>
        private ObjectPool<BaseEntity> GetPoolForUnitType(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Infantry:
                    return _infantryPool;
                case UnitType.Archer:
                    return _archerPool;
                case UnitType.Pike:
                    return _pikePool;
                default:
                    return _infantryPool; // Default to infantry
            }
        }
        
        /// <summary>
        /// Get unit data for a specific unit type
        /// </summary>
        private UnitDataSO GetUnitData(UnitType unitType)
        {
            // Check cache first
            if (_unitDataCache.TryGetValue(unitType, out var dataList) && dataList.Count > 0)
            {
                // Return a random unit data of the requested type
                return dataList[Random.Range(0, dataList.Count)];
            }
            
            // Otherwise, try to get from DataManager
            if (DataManager != null && DataManager.IsInitialized)
            {
                var unitDataList = DataManager.GetUnitDataByType(unitType);
                if (unitDataList.Count > 0)
                {
                    // Cache for future use
                    if (!_unitDataCache.ContainsKey(unitType))
                    {
                        _unitDataCache[unitType] = new List<UnitDataSO>();
                    }
                    
                    _unitDataCache[unitType].AddRange(unitDataList);
                    
                    // Return a random unit data
                    return unitDataList[Random.Range(0, unitDataList.Count)];
                }
            }
            
            Debug.LogWarning($"EnhancedUnitFactory: No unit data found for type {unitType}");
            return null;
        }
        
        /// <summary>
        /// Return a unit to its pool
        /// </summary>
        public void ReturnUnit(IEntity entity)
        {
            if (entity == null) return;
            
            var baseEntity = entity as BaseEntity;
            if (baseEntity == null) return;
            
            // Determine which pool this unit belongs to
            var unitTypeComponent = baseEntity.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent == null) return;
            
            UnitType unitType = unitTypeComponent.UnitType;
            ObjectPool<BaseEntity> pool = GetPoolForUnitType(unitType);
            
            if (pool != null)
            {
                // Return to the appropriate pool
                pool.Return(baseEntity);
                Debug.Log($"EnhancedUnitFactory: Returned unit ID {entity.Id} of type {unitType} to pool");
            }
        }
        
        /// <summary>
        /// Return all units to their pools
        /// </summary>
        public void ReturnAllUnits()
        {
            // Create a copy of the keys to avoid modification during iteration
            List<int> entityIds = new List<int>(_activeEntities.Keys);
            
            foreach (int id in entityIds)
            {
                if (_activeEntities.TryGetValue(id, out BaseEntity entity))
                {
                    ReturnUnit(entity);
                }
            }
            
            Debug.Log($"EnhancedUnitFactory: Returned all {entityIds.Count} units to pool");
        }
        
        /// <summary>
        /// Get a unit model by entity ID
        /// </summary>
        public UnitModel GetUnitModel(int entityId)
        {
            if (_unitModels.TryGetValue(entityId, out UnitModel model))
            {
                return model;
            }
            return null;
        }
        
        /// <summary>
        /// Get a unit model from an entity
        /// </summary>
        public UnitModel GetUnitModel(IEntity entity)
        {
            if (entity == null) return null;
            
            return GetUnitModel(entity.Id);
        }
        
        /// <summary>
        /// Get all unit models
        /// </summary>
        public List<UnitModel> GetAllUnitModels()
        {
            return new List<UnitModel>(_unitModels.Values);
        }
        
        /// <summary>
        /// Get all unit models of a specific type
        /// </summary>
        public List<UnitModel> GetUnitModelsByType(UnitType unitType)
        {
            List<UnitModel> result = new List<UnitModel>();
            
            foreach (var model in _unitModels.Values)
            {
                if (model.UnitType == unitType)
                {
                    result.Add(model);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Initialize components on a unit entity
        /// </summary>
        private void InitializeEntityComponents(IEntity entity)
        {
            // Get the MonoBehaviour
            var entityTransform = entity as MonoBehaviour;
            if (entityTransform == null) return;
            
            // Ensure all components are properly initialized
            var components = entityTransform.GetComponents<IComponent>();
            foreach (var component in components)
            {
                if (component.Entity == null)
                {
                    component.Entity = entity;
                }
                
                component.Initialize();
            }
        }
        
        /// <summary>
        /// Assign a unique entity ID
        /// </summary>
        private void AssignEntityId(BaseEntity entity)
        {
            if (entity == null) return;
            
            // Use reflection to set the ID field
            var idField = typeof(BaseEntity).GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (idField != null)
            {
                idField.SetValue(entity, _nextEntityId++);
            }
            else
            {
                Debug.LogError("EnhancedUnitFactory: Cannot find _id field in BaseEntity");
            }
        }
        
        /// <summary>
        /// Clear all pools and reset the factory
        /// </summary>
        public void ClearAllPools()
        {
            // Return all active units first
            ReturnAllUnits();
            
            // Clear pools
            _infantryPool?.Clear();
            _archerPool?.Clear();
            _pikePool?.Clear();
            
            // Clear dictionaries
            _activeEntities.Clear();
            _unitModels.Clear();
            
            Debug.Log("EnhancedUnitFactory: Cleared all unit pools");
        }
        
        /// <summary>
        /// Get statistics about the pools
        /// </summary>
        public string GetPoolStats()
        {
            return $"EnhancedUnitFactory Stats:\n" +
                   $"Infantry Pool: {_infantryPool?.CountInactive}/{_infantryPool?.CountAll} inactive/total\n" +
                   $"Archer Pool: {_archerPool?.CountInactive}/{_archerPool?.CountAll} inactive/total\n" +
                   $"Pike Pool: {_pikePool?.CountInactive}/{_pikePool?.CountAll} inactive/total\n" +
                   $"Active Entities: {_activeEntities.Count}\n" +
                   $"Unit Models: {_unitModels.Count}";
        }
        
        /// <summary>
        /// Refresh all unit models by reapplying their data
        /// </summary>
        public void RefreshAllUnits()
        {
            foreach (var unitModel in _unitModels.Values)
            {
                unitModel.ApplyData();
            }
            
            Debug.Log($"EnhancedUnitFactory: Refreshed all {_unitModels.Count} unit models");
        }
        
        /// <summary>
        /// Handle unit model cleanup on destroy
        /// </summary>
        private void OnDestroy()
        {
            // Clean up all unit models
            foreach (var unitModel in _unitModels.Values)
            {
                unitModel.Cleanup();
            }
            
            // Clear pools
            ClearAllPools();
        }
    }
}