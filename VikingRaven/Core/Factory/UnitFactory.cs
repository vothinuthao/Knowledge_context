using System;
using System.Collections.Generic;
using Core.Utils;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Core.Factory
{
    public class UnitFactory : MonoBehaviour, IEntityFactory
    {
        [SerializeField] private GameObject _infantryPrefab;
        [SerializeField] private GameObject _archerPrefab;
        [SerializeField] private GameObject _pikePrefab;

        [SerializeField] private List<GameObject> _listCache = new List<GameObject>();
        private int _nextEntityId = 1000; 

        private EntityRegistry EntityRegistry => EntityRegistry.Instance;

        public IEntity CreateEntity(Vector3 position, Quaternion rotation)
        {
            return CreateUnit(UnitType.Infantry, position, rotation);
        }

        public IEntity CreateUnit(UnitType unitType, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = GetPrefabForUnitType(unitType);
            
            if (prefab == null)
            {
                Debug.LogError($"UnitFactory: No prefab for unit type {unitType}");
                return null;
            }
            
            GameObject unitObject = Instantiate(prefab, position, rotation);
            _listCache.Add(unitObject);
            
            // Get or add a BaseEntity component
            var entityComponent = unitObject.GetComponent<BaseEntity>();
            if (entityComponent == null)
            {
                Debug.LogWarning($"UnitFactory: Prefab {prefab.name} doesn't have BaseEntity component, adding one");
                entityComponent = unitObject.AddComponent<BaseEntity>();
            }
            
            // Set the entity ID using reflection if it's not already set
            var idField = typeof(BaseEntity).GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            int currentId = (int)idField.GetValue(entityComponent);
            
            if (currentId <= 0)
            {
                // Assign a new ID
                int newId = _nextEntityId++;
                idField.SetValue(entityComponent, newId);
                Debug.Log($"UnitFactory: Assigned ID {newId} to entity");
            }
            
            // Manually register the entity with EntityRegistry
            if (EntityRegistry != null)
            {
                EntityRegistry.RegisterEntity(entityComponent);
                Debug.Log($"UnitFactory: Registered entity {entityComponent.Id} with EntityRegistry");
            }
            else
            {
                Debug.LogError("UnitFactory: EntityRegistry is null, cannot register entity");
            }
            
            // Configure unit type if component exists
            var unitTypeComponent = unitObject.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent != null)
            {
                unitTypeComponent.SetUnitType(unitType);
            }
            else
            {
                Debug.LogError($"UnitFactory: UnitTypeComponent missing from prefab {prefab.name}");
            }
            
            // Initialize core components
            InitializeEntityComponents(entityComponent);
            
            Debug.Log($"UnitFactory: Created unit with ID {entityComponent.Id} at position {position}, active: {unitObject.activeSelf}, in scene: {unitObject.scene.name}, hierarchyPath: {GameObjectPath(unitObject)}");
            
            return entityComponent;
        }

        private void InitializeEntityComponents(IEntity entity)
        {
            // Make sure all essential components are initialized
            var entityTransform = entity as MonoBehaviour;
            if (entityTransform == null) return;
            
            // Ensure that all components are properly initialized
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

        private GameObject GetPrefabForUnitType(UnitType unitType)
        {
            switch (unitType)
            {
                case UnitType.Infantry:
                    return _infantryPrefab;
                    
                case UnitType.Archer:
                    return _archerPrefab;
                    
                case UnitType.Pike:
                    return _pikePrefab;
                    
                default:
                    Debug.LogWarning($"Unknown unit type: {unitType}, defaulting to Infantry");
                    return _infantryPrefab;
            }
        }

        private string GameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}