using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Units.Components;

namespace VikingRaven.Core.StateMachine
{
    public class KnockbackState : BaseUnitState
    {
        private float _knockbackDuration;
        private float _knockbackTimer;
        private Vector3 _knockbackDirection;
        private float _knockbackForce;

        public KnockbackState(IEntity entity, IStateMachine stateMachine) : base(entity, stateMachine)
        {
        }

        public void SetKnockbackParams(Vector3 direction, float force, float duration)
        {
            _knockbackDirection = direction.normalized;
            _knockbackForce = force;
            _knockbackDuration = duration;
        }

        public override void Enter()
        {
            Debug.Log($"Entity {Entity.Id} entered Knockback state");
            
            _knockbackTimer = 0f;
            
            // Set animation if available
            var animationComponent = Entity.GetComponent<AnimationComponent>();
            if (animationComponent != null)
            {
                animationComponent.PlayAnimation("Knockback");
            }
        }

        public override void Execute()
        {
            _knockbackTimer += Time.deltaTime;
            
            // Apply knockback force
            var transformComponent = Entity.GetComponent<TransformComponent>();
            if (transformComponent != null)
            {
                float remainingForce = Mathf.Lerp(_knockbackForce, 0, _knockbackTimer / _knockbackDuration);
                transformComponent.Move(_knockbackDirection * (remainingForce * Time.deltaTime));
            }
            
            // Check if knockback is complete
            if (_knockbackTimer >= _knockbackDuration)
            {
                StateMachine.RevertToPreviousState();
            }
        }

        public override void Exit()
        {
            Debug.Log($"Entity {Entity.Id} exited Knockback state");
        }
    }
}