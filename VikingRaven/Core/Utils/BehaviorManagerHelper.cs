using VikingRaven.Core.ECS;
using VikingRaven.Units.Behaviors;
using VikingRaven.Units.Components;

namespace VikingRaven.Core.Utils
{
    public static class BehaviorManagerHelper
    {
        /// <summary>
        /// Helps initialize standard behaviors for a unit based on its type
        /// </summary>
        public static void InitializeStandardBehaviors(IEntity entity, UnitType unitType)
        {
            var behaviorComponent = entity.GetComponent<WeightedBehaviorComponent>();
            if (behaviorComponent == null || behaviorComponent.BehaviorManager == null)
                return;
                
            // Add standard behaviors that all units should have
            behaviorComponent.AddBehavior(new MoveBehavior(entity));
            behaviorComponent.AddBehavior(new AttackBehavior(entity));
            behaviorComponent.AddBehavior(new StrafeBehavior(entity));
            
            // Add unit-specific behaviors
            switch (unitType)
            {
                case UnitType.Infantry:
                    // Infantry specializes in protection and charging
                    behaviorComponent.AddBehavior(new ProtectBehavior(entity));
                    break;
                    
                case UnitType.Archer:
                    // Archers specialize in cover and ambush
                    behaviorComponent.AddBehavior(new CoverBehavior(entity));
                    behaviorComponent.AddBehavior(new AmbushMoveBehavior(entity));
                    break;
                    
                case UnitType.Pike:
                    // Pike units specialize in formation fighting
                    // Formation behaviors are added by the SpecializedBehaviorSystem
                    break;
            }
        }
        
        /// <summary>
        /// Helps initialize standard steering behaviors
        /// </summary>
        public static void InitializeSteeringBehaviors(IEntity entity)
        {
            var steeringComponent = entity.GetComponent<SteeringComponent>();
            if (steeringComponent == null || steeringComponent.SteeringManager == null)
                return;
                
            // Add standard steering behaviors
            steeringComponent.AddBehavior(new SeekBehavior());
            steeringComponent.AddBehavior(new FleeBehavior());
            steeringComponent.AddBehavior(new ObstacleAvoidanceBehavior());
            steeringComponent.AddBehavior(new SeparationBehavior());
            steeringComponent.AddBehavior(new CohesionBehavior());
            steeringComponent.AddBehavior(new FormationFollowingBehavior());
        }
    }
}