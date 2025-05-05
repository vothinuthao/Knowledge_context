using UnityEngine;
using VikingRaven.Core.DI;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Core.Factory
{
    public class UnitFactory : MonoBehaviour, IEntityFactory
    {
        [SerializeField] private GameObject _infantryPrefab;
        [SerializeField] private GameObject _archerPrefab;
        [SerializeField] private GameObject _pikePrefab;
        
        [Inject] private IEntityRegistry _entityRegistry;

        public IEntity CreateEntity(Vector3 position, Quaternion rotation)
        {
            // Default to infantry
            return CreateUnit(UnitType.Infantry, position, rotation);
        }

        public IEntity CreateUnit(UnitType unitType, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = GetPrefabForUnitType(unitType);
            
            if (prefab == null)
            {
                Debug.LogError($"No prefab defined for unit type {unitType}");
                return null;
            }
            
            // Instantiate the prefab
            GameObject unitObject = Instantiate(prefab, position, rotation);
            
            // Get or add the BaseEntity component
            BaseEntity baseEntity = unitObject.GetComponent<BaseEntity>();
            if (baseEntity == null)
            {
                baseEntity = unitObject.AddComponent<BaseEntity>();
            }
            
            // Add basic components if they don't exist
            EnsureBasicComponents(unitObject, baseEntity);
            
            // Set the unit type
            var unitTypeComponent = unitObject.GetComponent<UnitTypeComponent>();
            if (unitTypeComponent != null)
            {
                unitTypeComponent.SetUnitType(unitType);
            }
            
            // Register the entity
            _entityRegistry.GetEntity(baseEntity.Id);
            
            return baseEntity;
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
                    return _infantryPrefab;
            }
        }

        private void EnsureBasicComponents(GameObject unitObject, BaseEntity baseEntity)
        {
            // Ensure all required components are added
            
            // Transform Component
            if (unitObject.GetComponent<TransformComponent>() == null)
            {
                unitObject.AddComponent<TransformComponent>();
            }
            
            // Health Component
            if (unitObject.GetComponent<HealthComponent>() == null)
            {
                unitObject.AddComponent<HealthComponent>();
            }
            
            // Navigation Component
            if (unitObject.GetComponent<NavigationComponent>() == null)
            {
                unitObject.AddComponent<NavigationComponent>();
            }
            
            // Combat Component
            if (unitObject.GetComponent<CombatComponent>() == null)
            {
                unitObject.AddComponent<CombatComponent>();
            }
            
            // Animation Component
            if (unitObject.GetComponent<AnimationComponent>() == null)
            {
                unitObject.AddComponent<AnimationComponent>();
            }
            
            // Aggro Detection Component
            if (unitObject.GetComponent<AggroDetectionComponent>() == null)
            {
                unitObject.AddComponent<AggroDetectionComponent>();
            }
            
            // Unit Type Component
            if (unitObject.GetComponent<UnitTypeComponent>() == null)
            {
                unitObject.AddComponent<UnitTypeComponent>();
            }
            
            // Formation Component
            if (unitObject.GetComponent<FormationComponent>() == null)
            {
                unitObject.AddComponent<FormationComponent>();
            }
            
            // State Component
            if (unitObject.GetComponent<StateComponent>() == null)
            {
                unitObject.AddComponent<StateComponent>();
            }
            
            // Weighted Behavior Component
            if (unitObject.GetComponent<WeightedBehaviorComponent>() == null)
            {
                unitObject.AddComponent<WeightedBehaviorComponent>();
            }
        }
    }
}