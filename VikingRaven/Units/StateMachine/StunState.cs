using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Core.StateMachine
{
    public class StunState : BaseUnitState
    {
        private float _stunDuration;
        private float _stunTimer;

        public StunState(IEntity entity, IStateMachine stateMachine) : base(entity, stateMachine)
        {
        }

        public void SetStunDuration(float duration)
        {
            _stunDuration = duration;
        }

        public override void Enter()
        {
            Debug.Log($"Entity {Entity.Id} entered Stun state");
            
            _stunTimer = 0f;
            
            // Set animation if available
            var animationComponent = Entity.GetComponent<AnimationComponent>();
            if (animationComponent != null)
            {
                animationComponent.PlayAnimation("Stun");
            }
        }

        public override void Execute()
        {
            _stunTimer += Time.deltaTime;
            
            // Check if stun is complete
            if (_stunTimer >= _stunDuration)
            {
                StateMachine.RevertToPreviousState();
            }
        }

        public override void Exit()
        {
            Debug.Log($"Entity {Entity.Id} exited Stun state");
        }
    }
}