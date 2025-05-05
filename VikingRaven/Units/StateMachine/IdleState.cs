using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.StateMachine;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.StateMachine
{
    public class IdleState : BaseUnitState
    {
        public IdleState(IEntity entity, IStateMachine stateMachine) : base(entity, stateMachine)
        {
        }

        public override void Enter()
        {
            Debug.Log($"Entity {Entity.Id} entered Idle state");
            
            // Set animation if available
            var animationComponent = Entity.GetComponent<AnimationComponent>();
            if (animationComponent != null)
            {
                animationComponent.PlayAnimation("Idle");
            }
        }

        public override void Execute()
        {
            var aggroDetectionComponent = Entity.GetComponent<AggroDetectionComponent>();
            if (aggroDetectionComponent != null && aggroDetectionComponent.HasEnemyInRange())
            {
                StateMachine.ChangeState<AggroState>();
                return;
            }
        }

        public override void Exit()
        {
            Debug.Log($"Entity {Entity.Id} exited Idle state");
        }
    }
}