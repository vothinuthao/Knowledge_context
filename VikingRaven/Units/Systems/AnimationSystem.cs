using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.Systems
{
    public class AnimationSystem : BaseSystem
    {
        public override void Execute()
        {
            // Get all entities with animation components
            var entities = EntityRegistry.GetEntitiesWithComponent<AnimationComponent>();
            
            foreach (var entity in entities)
            {
                // Get required components
                var animationComponent = entity.GetComponent<AnimationComponent>();
                var transformComponent = entity.GetComponent<TransformComponent>();
                var navigationComponent = entity.GetComponent<NavigationComponent>();
                
                if (transformComponent != null && navigationComponent != null)
                {
                    // Set speed parameter based on movement
                    bool isMoving = !navigationComponent.HasReachedDestination;
                    float speed = isMoving ? 1.0f : 0.0f;
                    
                    animationComponent.SetFloat("Speed", speed);
                }
                
                // State-based animations are handled in the state machine
            }
        }
    }
}