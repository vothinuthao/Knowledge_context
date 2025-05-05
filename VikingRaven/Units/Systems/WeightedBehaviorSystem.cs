using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    public class WeightedBehaviorSystem : BaseSystem
    {
        public override void Execute()
        {
            // Get all entities with weighted behavior components
            var entities = EntityRegistry.GetEntitiesWithComponent<WeightedBehaviorComponent>();
            
            foreach (var entity in entities)
            {
                // Get the behavior component
                var behaviorComponent = entity.GetComponent<WeightedBehaviorComponent>();
                
                // Update behavior is handled in the WeightedBehaviorManager's Update method
                // This system could be used for additional behavior coordination
            }
        }
    }
}