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
    /// Simplified Unit Factory - Only creates units by ID
    /// Follows Single Responsibility Principle
    /// </summary>
    public class UnitFactory : MonoBehaviour
    {
        #region Configuration
        
        [Title("Pool Configuration")]
        [Tooltip("Default pool size for each unit type")]
        [SerializeField, Range(5, 50)] 
        private int _defaultPoolSize = 20;
        
        [Tooltip("Allow pools to expand when empty")]
        [SerializeField, ToggleLeft] 
        private bool _expandablePool = true;

        #endregion

        #region Dependencies
        
        [Title("Dependencies")]
        [Tooltip("Data manager reference")]
        [SerializeField, Required]
        private DataManager _dataManager;
        
        [Tooltip("Entity registry reference")]
        [SerializeField, Required]
        private EntityRegistry _entityRegistry;

        #endregion

        #region Runtime Data
        
        [Title("Runtime Information")]
        [ShowInInspector, ReadOnly]
        private int ActiveUnitsCount => _activeUnits?.Count ?? 0;
        
        [ShowInInspector, ReadOnly]
        private int PoolsCount => _unitPools?.Count ?? 0;

        #endregion

        #region Private Fields
        
        // Object pools by unit ID
        private Dictionary<uint, ObjectPool<BaseEntity>> _unitPools = new Dictionary<uint, ObjectPool<BaseEntity>>();
        
        // Active units tracking
        private Dictionary<int, IEntity> _activeUnits = new Dictionary<int, IEntity>();
        private Dictionary<int, UnitModel> _unitModels = new Dictionary<int, UnitModel>();
        
        // ID generation
        private int _nextEntityId = 1000;

        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            ValidateDependencies();
        }
        
        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Core Factory Methods
        
        /// <summary>
        /// Create unit by unit data ID
        /// </summary>
        /// <param name="unitDataId">Unit data identifier</param>
        /// <param name="position">Spawn position</param>
        /// <param name="rotation">Spawn rotation</param>
        /// <returns>Created entity or null if failed</returns>
        public IEntity CreateUnit(uint unitDataId, Vector3 position, Quaternion rotation)
        {
            // Get unit data
            UnitDataSO unitData = _dataManager.GetUnitData(unitDataId);
            if (unitData == null)
            {
                Debug.LogError($"UnitFactory: Unit data not found for ID: {unitDataId}");
                return null;
            }
            
            // Get or create pool
            var pool = GetOrCreatePool(unitDataId, unitData);
            if (pool == null)
            {
                Debug.LogError($"UnitFactory: Failed to create pool for: {unitDataId}");
                return null;
            }
            
            // Get entity from pool
            BaseEntity entity = pool.Get();
            if (entity == null)
            {
                Debug.LogError($"UnitFactory: Failed to get entity from pool: {unitDataId}");
                return null;
            }
            
            // Setup entity
            SetupEntity(entity, unitData, position, rotation);
            
            return entity;
        }
        
        /// <summary>
        /// Return unit to pool
        /// </summary>
        /// <param name="entity">Entity to return</param>
        public void ReturnUnit(IEntity entity)
        {
            if (entity == null) return;
            
            // Find unit model
            if (!_unitModels.TryGetValue(entity.Id, out UnitModel unitModel))
            {
                Debug.LogWarning($"UnitFactory: Unit model not found for entity: {entity.Id}");
                return;
            }
            
            // Find pool
            if (!_unitPools.TryGetValue(unitModel.UnitId, out var pool))
            {
                Debug.LogWarning($"UnitFactory: Pool not found for unit: {unitModel.UnitId}");
                return;
            }
            
            // Clean up tracking
            _activeUnits.Remove(entity.Id);
            _unitModels.Remove(entity.Id);
            
            // Return to pool
            var baseEntity = entity as BaseEntity;
            if (baseEntity != null)
            {
                pool.Return(baseEntity);
            }
        }

        #endregion

        #region Public Queries
        
        /// <summary>
        /// Get unit model for entity
        /// </summary>
        public UnitModel GetUnitModel(IEntity entity)
        {
            if (entity == null) return null;
            _unitModels.TryGetValue(entity.Id, out UnitModel model);
            return model;
        }
        
        /// <summary>
        /// Check if entity is managed by this factory
        /// </summary>
        public bool IsManaged(IEntity entity)
        {
            return entity != null && _activeUnits.ContainsKey(entity.Id);
        }

        #endregion

        #region Private Methods
        
        private void ValidateDependencies()
        {
            if (_dataManager == null)
            {
                _dataManager = DataManager.Instance;
                if (_dataManager == null)
                {
                    Debug.LogError("UnitFactory: DataManager dependency is missing!");
                }
            }
            
            if (_entityRegistry == null)
            {
                _entityRegistry = EntityRegistry.Instance;
                if (_entityRegistry == null)
                {
                    Debug.LogError("UnitFactory: EntityRegistry dependency is missing!");
                }
            }
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        private ObjectPool<BaseEntity> GetOrCreatePool(uint unitDataId, UnitDataSO unitData)
        {
            if (_unitPools.TryGetValue(unitDataId, out var existingPool))
                return existingPool;
            
            if (!unitData.Prefab)
            {
                Debug.LogError($"UnitFactory: Prefab is null for unit: {unitDataId}");
                return null;
            }
            
            var prefabEntity = unitData.Prefab.GetComponent<BaseEntity>();
            if (!prefabEntity)
            {
                Debug.LogError($"UnitFactory: Prefab missing BaseEntity component: {unitDataId}");
                return null;
            }
            
            
            GameObject poolParent = new GameObject($"Pool_{unitDataId}");
            poolParent.transform.SetParent(transform);
            
            // Create pool
            var newPool = new ObjectPool<BaseEntity>(
                prefabEntity,
                _defaultPoolSize,
                poolParent.transform,
                _expandablePool,
                OnReturnToPool,
                OnTakeFromPool
            );
            
            _unitPools[unitDataId] = newPool;
            return newPool;
        }
        
        private void SetupEntity(BaseEntity entity, UnitDataSO unitData, Vector3 position, Quaternion rotation)
        {
            entity.SetId(_nextEntityId++);
            entity.transform.position = position;
            entity.transform.rotation = rotation;
            
            if (_entityRegistry != null)
            {
                _entityRegistry.RegisterEntity(entity);
            }
            
            var unitModel = new UnitModel(entity, unitData);
            
            // Track
            _activeUnits[entity.Id] = entity;
            _unitModels[entity.Id] = unitModel;
            InitializeComponents(entity, unitData);
        }
        
        private void InitializeComponents(BaseEntity entity, UnitDataSO unitData)
        {
            var healthComponent = entity.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                healthComponent.SetMaxHealth(unitData.HitPoints);
                healthComponent.SetHealth(unitData.HitPoints);
            }
            var combatComponent = entity.GetComponent<CombatComponent>();
            if (combatComponent)
            {
                combatComponent.SetAttackDamage(unitData.Damage);
                combatComponent.SetAttackRange(unitData.Range);
                combatComponent.SetAttackCooldown(unitData.HitSpeed);
                combatComponent.SetMoveSpeed(unitData.MoveSpeed);
            }
            var unitTypeComponent = entity.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent != null)
            {
                unitTypeComponent.SetUnitType(unitData.UnitType);
            }
        }
        
        private void OnReturnToPool(BaseEntity entity)
        {
            // Reset components
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
        
        private void OnTakeFromPool(BaseEntity entity)
        {
            // Entity will be set up by SetupEntity method
        }
        
        private void Cleanup()
        {
            // Return all active units
            var activeIds = new List<int>(_activeUnits.Keys);
            foreach (int id in activeIds)
            {
                if (_activeUnits.TryGetValue(id, out var entity))
                {
                    ReturnUnit(entity);
                }
            }
            
            // Clear pools
            foreach (var pool in _unitPools.Values)
            {
                pool.Clear();
            }
            _unitPools.Clear();
        }

        #endregion
    }
}