using VikingRaven.Core.ECS;
using VikingRaven.Units.Behaviors;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    public class AIDecisionSystem : BaseSystem
    {
        public override void Execute()
        {
            // Get all entities with weighted behavior components
            var entities = EntityRegistry.GetEntitiesWithComponent<WeightedBehaviorComponent>();
            
            foreach (var entity in entities)
            {
                // Get the behavior component
                var behaviorComponent = entity.GetComponent<WeightedBehaviorComponent>();
                
                // The actual behavior selection and execution is handled by the WeightedBehaviorManager
                // This system could be used for higher-level decision making or behavior coordination
                
                // For example, ensuring certain behaviors are available based on the unit type
                var unitTypeComponent = entity.GetComponent<UnitTypeComponent>();
                if (unitTypeComponent != null && behaviorComponent.BehaviorManager != null)
                {
                    // Add behaviors if they don't exist yet - this could be moved to initialization
                    // but is shown here for demonstration
                    
                    // Ensure basic behaviors are registered
                    // Note: In a real implementation, you would check if behaviors already exist
                    
                    // Move behavior for all unit types
                    var moveBehavior = new MoveBehavior(entity);
                    behaviorComponent.AddBehavior(moveBehavior);
                    
                    // Attack behavior for all unit types
                    var attackBehavior = new AttackBehavior(entity);
                    behaviorComponent.AddBehavior(attackBehavior);
                    
                    // Strafe behavior for archers
                    if (unitTypeComponent.UnitType == UnitType.Archer)
                    {
                        var strafeBehavior = new StrafeBehavior(entity);
                        behaviorComponent.AddBehavior(strafeBehavior);
                    }
                    
                    // Add more unit-specific behaviors as needed
                }
            }
        }
    }
}