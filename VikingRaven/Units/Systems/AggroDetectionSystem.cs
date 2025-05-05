using VikingRaven.Core.ECS;
using VikingRaven.Core.StateMachine;
using VikingRaven.Units.Components;
using VikingRaven.Units.StateMachine;

namespace VikingRaven.Units.Systems
{
    public class AggroDetectionSystem : BaseSystem
    {
        public override void Execute()
        {
            // Get all entities with aggro detection components
            var entities = EntityRegistry.GetEntitiesWithComponent<AggroDetectionComponent>();
            
            foreach (var entity in entities)
            {
                // Get required components
                var aggroDetectionComponent = entity.GetComponent<AggroDetectionComponent>();
                var stateComponent = entity.GetComponent<StateComponent>();
                
                // Aggro detection is handled in the component's Update method
                
                // However, we can handle state transitions here
                if (stateComponent != null && stateComponent.StateMachineInGame != null)
                {
                    // Check if we detected an enemy and are currently in Idle state
                    if (aggroDetectionComponent.HasEnemyInRange() && 
                        stateComponent.CurrentState is IdleState)
                    {
                        stateComponent.StateMachineInGame.ChangeState<AggroState>();
                    }
                    
                    // Check if we lost all enemies and are currently in Aggro state
                    else if (!aggroDetectionComponent.HasEnemyInRange() && 
                             stateComponent.CurrentState is AggroState)
                    {
                        // Transition back to Idle state
                        stateComponent.StateMachineInGame.ChangeState<IdleState>();
                    }
                }
            }
        }
    }
}