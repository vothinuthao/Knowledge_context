using UnityEngine;
using VikingRaven.Core.ECS;
using VikingRaven.Core.StateMachine;
using VikingRaven.Units.Components;

namespace VikingRaven.Units.StateMachine
{
    public class AggroState : BaseUnitState
    {
        public AggroState(IEntity entity, IStateMachine stateMachine) : base(entity, stateMachine)
        {
        }

        public override void Enter()
        {
            Debug.Log($"Entity {Entity.Id} entered Aggro state");
            
            // Set animation if available
            var animationComponent = Entity.GetComponent<AnimationComponent>();
            if (animationComponent != null)
            {
                animationComponent.PlayAnimation("Aggro");
            }
        }

        public override void Execute()
        {
            // Check if enemy is still in range
            var aggroDetectionComponent = Entity.GetComponent<AggroDetectionComponent>();
            if (aggroDetectionComponent != null && !aggroDetectionComponent.HasEnemyInRange())
            {
                StateMachine.ChangeState<IdleState>();
                return;
            }
            
            // Get target and face it
            var targetEntity = aggroDetectionComponent.GetClosestEnemy();
            if (targetEntity != null)
            {
                // Face target logic
                var transformComponent = Entity.GetComponent<TransformComponent>();
                var targetTransform = targetEntity.GetComponent<TransformComponent>();
                
                if (transformComponent != null && targetTransform != null)
                {
                    transformComponent.LookAt(targetTransform.Position);
                }
                
                // Check if in attack range
                var combatComponent = Entity.GetComponent<CombatComponent>();
                if (combatComponent != null && combatComponent.IsInAttackRange(targetEntity))
                {
                    // Attack logic
                    // combatComponent.Attack(targetEntity);
                }
            }
        }

        public IState IdleState { get; set; }

        public override void Exit()
        {
            Debug.Log($"Entity {Entity.Id} exited Aggro state");
        }
    }
}