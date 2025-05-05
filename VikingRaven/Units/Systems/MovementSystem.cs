using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    public class MovementSystem : BaseSystem
    {
        public override void Execute()
        {
            // Get all entities with transform and navigation components
            var entities = EntityRegistry.GetEntitiesWithComponent<TransformComponent>();
            
            foreach (var entity in entities)
            {
                // Get required components
                var transformComponent = entity.GetComponent<TransformComponent>();
                var navigationComponent = entity.GetComponent<NavigationComponent>();
                
                if (navigationComponent != null)
                {
                    // Update pathfinding for entities with navigation
                    navigationComponent.UpdatePathfinding();
                }
                
                // Additional movement logic can be added here
                // For example, handling formation movement
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent != null)
                {
                    // Formation logic
                }
            }
        }
    }
}