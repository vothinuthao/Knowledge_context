using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    public class MovementSystem : BaseSystem
    {
        public override void Execute()
        {
            var entities = EntityRegistry.GetEntitiesWithComponent<TransformComponent>();
            
            foreach (var entity in entities)
            {
                var navigationComponent = entity.GetComponent<NavigationComponent>();
                
                if (navigationComponent != null)
                {
                    // navigationComponent.UpdatePathfinding();
                }
                
                // Additional movement logic can be added here
                // For example, handling formation movement
                var formationComponent = entity.GetComponent<FormationComponent>();
                if (formationComponent)
                {
                    // Formation logic
                }
            }
        }
    }
}