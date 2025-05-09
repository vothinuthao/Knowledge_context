using Core.Utils;
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Core.Factory
{
    public class UnitFactory : Singleton<UnitFactory>, IEntityFactory
    {
        [SerializeField] private GameObject _infantryPrefab;
        [SerializeField] private GameObject _archerPrefab;
        [SerializeField] private GameObject _pikePrefab;
        

        private void Awake()
        {
            base.Awake();
            
            Debug.Log("UnitFactory.Awake() - Starting");
            
            Debug.Log($"UnitFactory has prefabs - Infantry: {_infantryPrefab != null}, " +
                     $"Archer: {_archerPrefab != null}, Pike: {_pikePrefab != null}");
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            ValidatePrefabs();
        }

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

        // Method to ensure all required prefabs are set up
        public bool ValidatePrefabs()
        {
            bool valid = true;
            
            if (_infantryPrefab == null)
            {
                Debug.LogError("UnitFactory: Infantry prefab is missing!");
                valid = false;
            }
            
            if (_archerPrefab == null)
            {
                Debug.LogError("UnitFactory: Archer prefab is missing!");
                valid = false;
            }
            
            if (_pikePrefab == null)
            {
                Debug.LogError("UnitFactory: Pike prefab is missing!");
                valid = false;
            }
            
            return valid;
        }
    }
}