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

        // private void Start()
        // {
        //     GameObject unitObject = Instantiate(test);
        //     _listCache.Add(unitObject);
        // }

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
            
            var entityComponent = unitObject.GetComponent<BaseEntity>();
            // if (entityComponent == null)
            // {
            //     entityComponent = unitObject.AddComponent<BaseEntity>();
            // }
            //
            // var unitTypeComponent = unitObject.GetComponent<UnitTypeComponent>();
            // if (unitTypeComponent != null)
            // {
            //     unitTypeComponent.SetUnitType(unitType);
            // }
            // else
            // {
            //     Debug.LogError($"UnitFactory: UnitTypeComponent missing from prefab {prefab.name}");
            // }
            _listCache.Add(unitObject);
            Debug.Log($"Created unit at position {position}, active: {unitObject.activeSelf}, in scene: {unitObject.scene.name}, hierarchyPath: {GameObjectPath(unitObject)}");
            return entityComponent;
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