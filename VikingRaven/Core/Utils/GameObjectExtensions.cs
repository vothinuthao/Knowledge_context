using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Core.Utils
{
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Extension method to get or add a component to a GameObject
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }
        
        /// <summary>
        /// Extension method to get the BaseEntity from a GameObject
        /// </summary>
        public static IEntity GetEntity(this GameObject gameObject)
        {
            return gameObject.GetComponent<BaseEntity>();
        }
        
        /// <summary>
        /// Extension method to initialize a GameObject as a unit with required components
        /// </summary>
        public static IEntity InitializeAsUnit(this GameObject gameObject, UnitType unitType)
        {
            // Get or add BaseEntity
            var entityComponent = gameObject.GetOrAddComponent<BaseEntity>();
            
            // Add essential components
            var transformComponent = gameObject.GetOrAddComponent<TransformComponent>();
            transformComponent.Initialize();
            
            var healthComponent = gameObject.GetOrAddComponent<HealthComponent>();
            healthComponent.Initialize();
            
            var unitTypeComponent = gameObject.GetOrAddComponent<UnitTypeComponent>();
            unitTypeComponent.SetUnitType(unitType);
            
            var stateComponent = gameObject.GetOrAddComponent<StateComponent>();
            stateComponent.Initialize();
            
            var formationComponent = gameObject.GetOrAddComponent<FormationComponent>();
            formationComponent.Initialize();
            
            var navigationComponent = gameObject.GetOrAddComponent<NavigationComponent>();
            navigationComponent.Initialize();
            
            var aggroComponent = gameObject.GetOrAddComponent<AggroDetectionComponent>();
            aggroComponent.Initialize();
            
            var combatComponent = gameObject.GetOrAddComponent<CombatComponent>();
            combatComponent.Initialize();
            
            return entityComponent;
        }
    }
}