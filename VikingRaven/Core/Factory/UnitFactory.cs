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
    /// Factory for creating and managing units from pools
    /// Uses unit data to dynamically create and configure units
    /// </summary>
    public class UnitFactory : MonoBehaviour
    {
        #region Inspector Fields

        [TitleGroup("Prefab References")]
        [Tooltip("Infantry unit prefab reference")]
        [SerializeField, PreviewField(50)] 
        private GameObject _infantryPrefab;
        
        [Tooltip("Archer unit prefab reference")]
        [SerializeField, PreviewField(50)] 
        private GameObject _archerPrefab;
        
        [Tooltip("Pike unit prefab reference")]
        [SerializeField, PreviewField(50)] 
        private GameObject _pikePrefab;
        
        [TitleGroup("Pool Settings")]
        [Tooltip("Initial pool size for Infantry units")]
        [SerializeField, Range(5, 50), ProgressBar(5, 50, 0, 1, Height = 15)] 
        private int _infantryPoolSize = 20;
        
        [Tooltip("Initial pool size for Archer units")]
        [SerializeField, Range(5, 50), ProgressBar(5, 50, 0, 1, Height = 15)] 
        private int _archerPoolSize = 10;
        
        [Tooltip("Initial pool size for Pike units")]
        [SerializeField, Range(5, 50), ProgressBar(5, 50, 0, 1, Height = 15)] 
        private int _pikePoolSize = 10;
        
        [TitleGroup("Initialization Settings")]
        [Tooltip("Whether to preload unit models on Awake")]
        [SerializeField, ToggleLeft] 
        private bool _preloadOnAwake = true;
        
        [Tooltip("Whether pools can expand when empty")]
        [SerializeField, ToggleLeft] 
        private bool _expandPoolsWhenEmpty = true;
        
        [TitleGroup("Debug Information")]
        [ShowInInspector, ReadOnly] 
        private int _activeEntitiesCount => _activeEntities?.Count ?? 0;
        
        [ShowInInspector, ReadOnly] 
        private int _unitModelsCount => _unitModelsById?.Count ?? 0;
        
        [ShowInInspector, ReadOnly] 
        private int _templateModelsCount => _unitModelTemplates?.Count ?? 0;

        #endregion

        #region Private Fields

        // Object pools for each unit type
        private ObjectPool<BaseEntity> _infantryPool;
        private ObjectPool<BaseEntity> _archerPool;
        private ObjectPool<BaseEntity> _pikePool;
        
        // Dictionary to track all created units by ID
        private Dictionary<int, BaseEntity> _activeEntities = new Dictionary<int, BaseEntity>();
        
        // Dictionary to track unit models by entity ID
        private Dictionary<int, UnitModel> _unitModelsById = new Dictionary<int, UnitModel>();
        
        // Dictionary to cache unit models by unit data ID
        private Dictionary<string, UnitModel> _unitModelTemplates = new Dictionary<string, UnitModel>();
        
        // Next entity ID to assign
        private int _nextEntityId = 1000;
        
        // Parent transforms for pool organization
        private Transform _infantryPoolParent;
        private Transform _archerPoolParent;
        private Transform _pikePoolParent;

        #endregion

        #region Properties

        // References to other managers
        private EntityRegistry EntityRegistry => EntityRegistry.Instance;
        private DataManager DataManager => DataManager.Instance;
        
        // Events
        public delegate void UnitEvent(UnitModel unitModel);
        public event UnitEvent OnUnitCreated;
        public event UnitEvent OnUnitReturned;

        #endregion

        #region Unity Lifecycle Methods

        private void Awake()
        {
            // Initialize object pools
            InitializeObjectPools();
            
            // Preload data cache
            if (_preloadOnAwake)
            {
                PreloadUnitModels();
            }
        }

        private void OnDestroy()
        {
            // Clean up all unit models
            foreach (var unitModel in _unitModelsById.Values)
            {
                unitModel.Cleanup();
            }
            
            // Clear pools
            ClearAllPools();
        }

        #endregion

        #region Initialization Methods

        /// <summary>
        /// Initialize object pools for each unit type
        /// </summary>
        private void InitializeObjectPools()
        {
            // Create parent transforms for each pool
            _infantryPoolParent = CreatePoolParent("InfantryPool");
            _archerPoolParent = CreatePoolParent("ArcherPool");
            _pikePoolParent = CreatePoolParent("PikePool");
            
            // Create the object pools
            if (_infantryPrefab != null)
            {
                _infantryPool = new ObjectPool<BaseEntity>(
                    _infantryPrefab.GetComponent<BaseEntity>(),
                    _infantryPoolSize,
                    _infantryPoolParent,
                    _expandPoolsWhenEmpty,
                    OnReturnUnit,
                    OnGetUnit
                );
                Debug.Log($"UnitFactory: Created infantry pool with {_infantryPoolSize} units");
            }
            
            if (_archerPrefab != null)
            {
                _archerPool = new ObjectPool<BaseEntity>(
                    _archerPrefab.GetComponent<BaseEntity>(),
                    _archerPoolSize,
                    _archerPoolParent,
                    _expandPoolsWhenEmpty,
                    OnReturnUnit,
                    OnGetUnit
                );
                Debug.Log($"UnitFactory: Created archer pool with {_archerPoolSize} units");
            }
            
            if (_pikePrefab != null)
            {
                _pikePool = new ObjectPool<BaseEntity>(
                    _pikePrefab.GetComponent<BaseEntity>(),
                    _pikePoolSize,
                    _pikePoolParent,
                    _expandPoolsWhenEmpty,
                    OnReturnUnit,
                    OnGetUnit
                );
                Debug.Log($"UnitFactory: Created pike pool with {_pikePoolSize} units");
            }
        }
        
        /// <summary>
        /// Preload unit models from DataManager
        /// </summary>
        private void PreloadUnitModels()
        {
            if (DataManager == null || !DataManager.IsInitialized)
            {
                Debug.LogWarning("UnitFactory: DataManager not available for preloading data");
                return;
            }
            
            // Clear existing cache
            _unitModelTemplates.Clear();
            
            // Get all unit data from DataManager
            List<UnitDataSO> allUnitData = DataManager.GetAllUnitData();
            
            foreach (var unitData in allUnitData)
            {
                if (!string.IsNullOrEmpty(unitData.UnitId))
                {
                    // Create a template UnitModel (without a real entity)
                    UnitModel templateModel = new UnitModel(null, unitData);
                    _unitModelTemplates[unitData.UnitId] = templateModel;
                }
            }
            
            Debug.Log($"UnitFactory: Preloaded {_unitModelTemplates.Count} unit model templates");
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

        #endregion

        #region Pool Callbacks

        /// <summary>
        /// Callback when a unit is returned to the pool
        /// </summary>
        private void OnReturnUnit(BaseEntity entity)
        {
            if (entity == null) return;
            
            // Get the unit model
            if (_unitModelsById.TryGetValue(entity.Id, out UnitModel unitModel))
            {
                // Clean up model
                unitModel.Cleanup();
                
                // Trigger event
                OnUnitReturned?.Invoke(unitModel);
                
                // Remove from tracking dictionaries
                _unitModelsById.Remove(entity.Id);
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
                // EntityRegistry.UnregisterEntity(entity);
            }
            
            Debug.Log($"UnitFactory: Returned entity {entity.Id} to pool");
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
            
            Debug.Log($"UnitFactory: Got entity {entity.Id} from pool");
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Create a unit of a specific type
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
                Debug.LogError("UnitFactory: Cannot create unit - unitData is null");
                return null;
            }
            
            // Get the appropriate pool based on unit type
            ObjectPool<BaseEntity> pool = GetPoolForUnitType(unitData.UnitType);
            
            if (pool == null)
            {
                Debug.LogError($"UnitFactory: No pool available for unit type {unitData.UnitType}");
                return null;
            }
            
            // Get a unit from the pool
            BaseEntity entity = pool.Get();
            if (entity == null)
            {
                Debug.LogError($"UnitFactory: Failed to get unit from pool for type {unitData.UnitType}");
                return null;
            }
            
            // Set position and rotation
            entity.transform.position = position;
            entity.transform.rotation = rotation;
            
            // Apply the unit data
            unitData.ApplyToUnit(entity.gameObject);
            
            // Create UnitModel
            UnitModel unitModel = new UnitModel(entity, unitData);
            
            // Store in dictionary
            _unitModelsById[entity.Id] = unitModel;
            
            // Trigger event
            OnUnitCreated?.Invoke(unitModel);
            
            Debug.Log($"UnitFactory: Created unit ID {entity.Id} of type {unitData.UnitType} at position {position}");
            
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
            // Get data from DataManager if available
            if (DataManager != null && DataManager.IsInitialized)
            {
                var unitDataList = DataManager.GetUnitDataByType(unitType);
                if (unitDataList.Count > 0)
                {
                    // Return a random unit data from the available options
                    return unitDataList[Random.Range(0, unitDataList.Count)];
                }
            }
            
            Debug.LogWarning($"UnitFactory: No unit data found for type {unitType}");
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
            if (unitTypeComponent == null) 
            {
                Debug.LogError($"UnitFactory: Entity {entity.Id} has no UnitTypeComponent, cannot return to pool");
                return;
            }
            
            UnitType unitType = unitTypeComponent.UnitType;
            ObjectPool<BaseEntity> pool = GetPoolForUnitType(unitType);
            
            if (pool != null)
            {
                // Return to the appropriate pool
                pool.Return(baseEntity);
                Debug.Log($"UnitFactory: Returned unit ID {entity.Id} of type {unitType} to pool");
            }
            else
            {
                Debug.LogError($"UnitFactory: No pool found for unit type {unitType}, cannot return entity {entity.Id}");
            }
        }

        #endregion

        #region Helper Methods

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
                Debug.LogError("UnitFactory: Cannot find _id field in BaseEntity");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get a unit model by entity ID
        /// </summary>
        public UnitModel GetUnitModel(int entityId)
        {
            if (_unitModelsById.TryGetValue(entityId, out UnitModel model))
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
        /// Get a unit template from cache by ID
        /// </summary>
        public UnitModel GetUnitTemplate(string unitId)
        {
            if (_unitModelTemplates.TryGetValue(unitId, out UnitModel template))
            {
                return template;
            }
            return null;
        }
        
        /// <summary>
        /// Get all unit templates
        /// </summary>
        public Dictionary<string, UnitModel> GetAllUnitTemplates()
        {
            return new Dictionary<string, UnitModel>(_unitModelTemplates);
        }
        
        /// <summary>
        /// Get all unit models
        /// </summary>
        public List<UnitModel> GetAllUnitModels()
        {
            return new List<UnitModel>(_unitModelsById.Values);
        }
        
        /// <summary>
        /// Get all unit models of a specific type
        /// </summary>
        public List<UnitModel> GetUnitModelsByType(UnitType unitType)
        {
            List<UnitModel> result = new List<UnitModel>();
            foreach (var unitModel in _unitModelsById.Values)
            {
                if (unitModel.UnitType == unitType)
                {
                    result.Add(unitModel);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Return all units to their pools
        /// </summary>
        [Button("Return All Units"), TitleGroup("Debug Tools")]
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
            
            Debug.Log($"UnitFactory: Returned all {entityIds.Count} units to pool");
        }
        
        /// <summary>
        /// Clear all pools and reset the factory
        /// </summary>
        [Button("Clear All Pools"), TitleGroup("Debug Tools")]
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
            _unitModelsById.Clear();
            
            Debug.Log("UnitFactory: Cleared all unit pools");
        }
        
        /// <summary>
        /// Get statistics about the pools
        /// </summary>
        [Button("Show Pool Stats"), TitleGroup("Debug Tools")]
        public string GetPoolStats()
        {
            string stats = "UnitFactory Stats:\n";
            
            stats += $"Infantry Pool: {_infantryPool?.CountInactive}/{_infantryPool?.CountAll} inactive/total\n";
            stats += $"Archer Pool: {_archerPool?.CountInactive}/{_archerPool?.CountAll} inactive/total\n";
            stats += $"Pike Pool: {_pikePool?.CountInactive}/{_pikePool?.CountAll} inactive/total\n";
            stats += $"Active Entities: {_activeEntities.Count}\n";
            stats += $"Unit Models: {_unitModelsById.Count}\n";
            stats += $"Unit Templates: {_unitModelTemplates.Count}";
            
            Debug.Log(stats);
            return stats;
        }
        
        /// <summary>
        /// Refresh all unit models by reapplying their data
        /// </summary>
        [Button("Refresh All Units"), TitleGroup("Debug Tools")]
        public void RefreshAllUnits()
        {
            foreach (var unitModel in _unitModelsById.Values)
            {
                unitModel.ApplyData();
            }
            
            Debug.Log($"UnitFactory: Refreshed all {_unitModelsById.Count} unit models");
        }

        #endregion
    }
}