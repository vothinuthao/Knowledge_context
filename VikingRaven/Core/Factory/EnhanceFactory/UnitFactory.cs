using System.Collections.Generic;
using UnityEngine;
using VikingRaven.Core.Data;
using VikingRaven.Core.ECS;
using VikingRaven.Core.Factory;
using VikingRaven.Core.ObjectPooling;
using VikingRaven.Units.Components;
using VikingRaven.Units.Data;

namespace VikingRaven.EnhanceFactory
{
    /// <summary>
    /// Factory responsible for creating and managing units using object pooling
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
        
        // Object pools for each unit type
        private ObjectPool<BaseEntity> _infantryPool;
        private ObjectPool<BaseEntity> _archerPool;
        private ObjectPool<BaseEntity> _pikePool;
        
        // Dictionary to track all created units by ID
        private Dictionary<int, BaseEntity> _activeUnits = new Dictionary<int, BaseEntity>();
        
        // Next entity ID to assign
        private int _nextEntityId = 1000;
        
        // Cache of unit data by type (for quick access)
        private Dictionary<UnitType, UnitDataSO> _unitDataCache = new Dictionary<UnitType, UnitDataSO>();
        
        // Reference to EntityRegistry
        private EntityRegistry EntityRegistry => EntityRegistry.Instance;
        private DataManager DataManager => DataManager.Instance;

        /// <summary>
        /// Initialize the factory and object pools
        /// </summary>
        private void Awake()
        {
            InitializeObjectPools();
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
                Debug.Log($"UnitFactory: Created infantry pool with {_infantryPoolSize} units");
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
                Debug.Log($"UnitFactory: Created archer pool with {_archerPoolSize} units");
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
                Debug.Log($"UnitFactory: Created pike pool with {_pikePoolSize} units");
            }
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
        private void OnReturnUnit(BaseEntity unit)
        {
            if (unit == null) return;
            
            // Reset the unit's state
            var healthComponent = unit.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                healthComponent.Revive();
            }
            
            // Deactivate components that should be inactive
            var navigationComponent = unit.GetComponent<NavigationComponent>();
            if (navigationComponent != null)
            {
                navigationComponent.DisablePathfinding();
            }
            
            // Remove from active units
            if (_activeUnits.ContainsKey(unit.Id))
            {
                _activeUnits.Remove(unit.Id);
            }
            
            // Unregister from EntityRegistry
            if (EntityRegistry != null)
            {
                // We don't actually destroy the entity, just unregister it
                // EntityRegistry.UnregisterEntity(unit);
            }
        }
        
        /// <summary>
        /// Callback when a unit is taken from the pool
        /// </summary>
        private void OnGetUnit(BaseEntity unit)
        {
            if (unit == null) return;
            
            // Assign a new entity ID
            AssignEntityId(unit);
            
            // Register with EntityRegistry
            if (EntityRegistry != null)
            {
                EntityRegistry.RegisterEntity(unit);
            }
            
            // Add to active units
            _activeUnits[unit.Id] = unit;
            
            // Initialize components
            InitializeEntityComponents(unit);
        }

        /// <summary>
        /// Create a generic entity (default implementation)
        /// </summary>
        public IEntity CreateEntity(Vector3 position, Quaternion rotation)
        {
            return CreateUnit(UnitType.Infantry, position, rotation);
        }
        
        /// <summary>
        /// Create a unit of a specific type
        /// </summary>
        public IEntity CreateUnit(UnitType unitType, Vector3 position, Quaternion rotation)
        {
            // Get the appropriate pool based on unit type
            ObjectPool<BaseEntity> pool = null;
            switch (unitType)
            {
                case UnitType.Infantry:
                    pool = _infantryPool;
                    break;
                case UnitType.Archer:
                    pool = _archerPool;
                    break;
                case UnitType.Pike:
                    pool = _pikePool;
                    break;
            }
            
            if (pool == null)
            {
                Debug.LogError($"UnitFactory: No pool available for unit type {unitType}");
                return null;
            }
            
            // Get a unit from the pool
            BaseEntity entity = pool.Get();
            if (entity == null)
            {
                Debug.LogError($"UnitFactory: Failed to get unit from pool for type {unitType}");
                return null;
            }
            
            // Set position and rotation
            entity.transform.position = position;
            entity.transform.rotation = rotation;
            
            // Configure unit type if component exists
            var unitTypeComponent = entity.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent != null)
            {
                unitTypeComponent.SetUnitType(unitType);
            }
            
            // Apply data from UnitData if available
            ApplyUnitData(entity, unitType);
            
            Debug.Log($"UnitFactory: Created unit ID {entity.Id} of type {unitType} at position {position}");
            
            return entity;
        }
        
        /// <summary>
        /// Create a unit from a specific UnitData
        /// </summary>
        public IEntity CreateUnitFromData(UnitDataSO unitData, Vector3 position, Quaternion rotation)
        {
            if (unitData == null)
            {
                Debug.LogError("UnitFactory: Cannot create unit - unitData is null");
                return null;
            }
            
            // Create the basic unit first
            IEntity entity = CreateUnit(unitData.UnitType, position, rotation);
            
            if (entity == null) 
                return null;
                
            // Apply the data directly
            var entityGameObject = (entity as MonoBehaviour)?.gameObject;
            if (entityGameObject != null)
            {
                unitData.ApplyToUnit(entityGameObject);
            }
            
            return entity;
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
            ObjectPool<BaseEntity> pool = null;
            
            switch (unitType)
            {
                case UnitType.Infantry:
                    pool = _infantryPool;
                    break;
                case UnitType.Archer:
                    pool = _archerPool;
                    break;
                case UnitType.Pike:
                    pool = _pikePool;
                    break;
            }
            
            if (pool != null)
            {
                // Return to the appropriate pool
                pool.Return(baseEntity);
                Debug.Log($"UnitFactory: Returned unit ID {entity.Id} of type {unitType} to pool");
            }
        }
        
        /// <summary>
        /// Apply data from UnitData to a unit entity
        /// </summary>
        private void ApplyUnitData(BaseEntity entity, UnitType unitType)
        {
            // Try to get cached data first
            if (!_unitDataCache.TryGetValue(unitType, out UnitDataSO unitData))
            {
                // Load data from DataManager
                if (DataManager != null && DataManager.IsInitialized)
                {
                    // Get all unit data of this type
                    var unitDataList = DataManager.GetUnitDataByType(unitType);
                    if (unitDataList.Count > 0)
                    {
                        // Use the first one found (could be improved to select specific variants)
                        unitData = unitDataList[0];
                        _unitDataCache[unitType] = unitData;
                    }
                }
            }
            
            if (unitData != null)
            {
                unitData.ApplyToUnit(entity.gameObject);
            }
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
            idField.SetValue(entity, _nextEntityId++);
        }
        
        /// <summary>
        /// Clear all pools and reset the factory
        /// </summary>
        public void ClearAllPools()
        {
            _infantryPool?.Clear();
            _archerPool?.Clear();
            _pikePool?.Clear();
            _activeUnits.Clear();
            Debug.Log("UnitFactory: Cleared all unit pools");
        }
        
        /// <summary>
        /// Get statistics about the pools
        /// </summary>
        public string GetPoolStats()
        {
            return $"UnitFactory Stats:\n" +
                   $"Infantry Pool: {_infantryPool?.CountInactive}/{_infantryPool?.CountAll} inactive/total\n" +
                   $"Archer Pool: {_archerPool?.CountInactive}/{_archerPool?.CountAll} inactive/total\n" +
                   $"Pike Pool: {_pikePool?.CountInactive}/{_pikePool?.CountAll} inactive/total\n" +
                   $"Active Units: {_activeUnits.Count}";
        }
        
        /// <summary>
        /// Get an active unit by ID
        /// </summary>
        public IEntity GetActiveUnit(int id)
        {
            if (_activeUnits.TryGetValue(id, out BaseEntity entity))
            {
                return entity;
            }
            return null;
        }
        
        /// <summary>
        /// Get all active units
        /// </summary>
        public List<IEntity> GetAllActiveUnits()
        {
            List<IEntity> result = new List<IEntity>();
            foreach (var entity in _activeUnits.Values)
            {
                result.Add(entity);
            }
            return result;
        }
    }
}