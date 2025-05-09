using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    public class CombatSystem : BaseSystem
    {
        public override void Execute()
        {
            // Get all entities with combat components
            var entities = EntityRegistry.GetEntitiesWithComponent<CombatComponent>();
            
            foreach (var entity in entities)
            {
                // Get required components
                var combatComponent = entity.GetComponent<CombatComponent>();
                var aggroDetectionComponent = entity.GetComponent<AggroDetectionComponent>();
                
                // Combat logic is mostly handled by the behavior system
                // This system could be used for additional combat-related logic
                
                // For example, updating cooldowns or handling special combat events
            }
        }
    }
}