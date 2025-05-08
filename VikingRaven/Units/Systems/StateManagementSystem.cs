using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    public class StateManagementSystem : BaseSystem
    {
        public override void Execute()
        {
            // Get all entities with a state component
            var entities = EntityRegistry.GetEntitiesWithComponent<StateComponent>();
            
            foreach (var entity in entities)
            {
                var stateComponent = entity.GetComponent<StateComponent>();
                
                // State machine update is handled in the component's Update method
                // This system could be used for additional state management logic
                
                // For example, check if entity is dead and change state accordinglysd
                var healthComponent = entity.GetComponent<HealthComponent>();
                if (healthComponent && healthComponent.IsDead)
                {
                    // Handle death state if needed
                }
                
                // Or check if stunned or knocked back based on other conditions
                // ...
            }
        }
    }
}