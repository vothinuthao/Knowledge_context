// UnitFactory.cs - phiên bản cập nhật để làm việc tốt hơn với Zenject
using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;
using Zenject;

namespace VikingRaven.Core.Factory
{
    public class UnitFactory : MonoBehaviour, IEntityFactory
    {
        // Thay vì SerializeField, làm cho các prefab public để dễ truy cập
        // từ ZenjectSceneInstaller
        [SerializeField] private GameObject _infantryPrefab;
        [SerializeField] private GameObject _archerPrefab;
        [SerializeField] private GameObject _pikePrefab;
        
        // Thêm properties để truy cập các prefab từ bên ngoài
        public GameObject InfantryPrefab => _infantryPrefab;
        public GameObject ArcherPrefab => _archerPrefab;
        public GameObject PikePrefab => _pikePrefab;

        [Inject] private DiContainer _container; // Zenject container để inject các dependency vào GameObject mới
        [Inject] private IEntityRegistry _entityRegistry;

        private void Awake()
        {
            Debug.Log("UnitFactory.Awake() - Starting");
            
            // Kiểm tra và log prefabs
            Debug.Log($"UnitFactory has prefabs - Infantry: {_infantryPrefab != null}, " +
                     $"Archer: {_archerPrefab != null}, Pike: {_pikePrefab != null}");
        }

        [Inject]
        public void Construct(IEntityRegistry entityRegistry, DiContainer container)
        {
            Debug.Log($"UnitFactory: Dependencies injected - EntityRegistry: {entityRegistry != null}, Container: {container != null}");
            _entityRegistry = entityRegistry;
            _container = container;
        }

        private void Start()
        {
            if (_entityRegistry == null)
            {
                Debug.LogError("UnitFactory: EntityRegistry is null after Start!");
            }
            
            if (_container == null)
            {
                Debug.LogError("UnitFactory: DiContainer is null after Start!");
            }
        }

        public IEntity CreateEntity(Vector3 position, Quaternion rotation)
        {
            // Default to infantry
            return CreateUnit(UnitType.Infantry, position, rotation);
        }

        public IEntity CreateUnit(UnitType unitType, Vector3 position, Quaternion rotation)
        {
            // Select prefab based on unit type
            GameObject prefab = GetPrefabForUnitType(unitType);
            
            if (prefab == null)
            {
                Debug.LogError($"UnitFactory: No prefab for unit type {unitType}");
                return null;
            }

            // Instantiate the unit using Zenject
            GameObject unitObject;
            if (_container != null)
            {
                // Use Zenject to instantiate and inject the GameObject
                unitObject = _container.InstantiatePrefab(prefab, position, rotation, null);
                Debug.Log($"Created unit using Zenject: {unitObject.name}");
            }
            else
            {
                // Fallback to standard instantiation
                Debug.LogWarning("UnitFactory: DiContainer is null, using standard Instantiate");
                unitObject = Instantiate(prefab, position, rotation);
                
                // Try to find container in scene to inject manually
                var sceneContext = FindObjectOfType<SceneContext>();
                if (sceneContext != null)
                {
                    sceneContext.Container.InjectGameObject(unitObject);
                    Debug.Log($"Manually injected unit: {unitObject.name}");
                }
                else
                {
                    Debug.LogError("UnitFactory: No SceneContext found for manual injection!");
                }
            }

            // Get or add BaseEntity component
            var entityComponent = unitObject.GetComponent<BaseEntity>();
            if (entityComponent == null)
            {
                entityComponent = unitObject.AddComponent<BaseEntity>();
            }

            // Set unit type
            var unitTypeComponent = unitObject.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent != null)
            {
                unitTypeComponent.SetUnitType(unitType);
            }
            else
            {
                Debug.LogError($"UnitFactory: UnitTypeComponent missing from prefab {prefab.name}");
            }

            // Add entity to registry
            if (_entityRegistry != null)
            {
                // EntityRegistry doesn't have a RegisterEntity method
                // Instead we need to check if entity is already in registry
                var existingEntity = _entityRegistry.GetEntity(entityComponent.Id);
                if (existingEntity == null)
                {
                    // The entity is not in registry yet - it will be tracked by EntityRegistry
                    // when GetEntity is called with its ID
                    Debug.Log($"Entity {entityComponent.Id} with type {unitType} will be tracked by EntityRegistry");
                }
                else
                {
                    Debug.Log($"Entity {entityComponent.Id} already exists in EntityRegistry");
                }
            }
            else
            {
                Debug.LogError("UnitFactory: Cannot track entity - EntityRegistry is null!");
            }

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

        // Phương thức để đảm bảo tất cả các prefab cần thiết đã được thiết lập
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