using VikingRaven.Core.ECS;

namespace VikingRaven.Core.StateMachine
{
    public abstract class BaseUnitState : IState
    {
        protected readonly IEntity Entity;
        protected readonly IStateMachine StateMachine;
        
        public BaseUnitState(IEntity entity, IStateMachine stateMachine)
        {
            Entity = entity;
            StateMachine = stateMachine;
        }
        
        public virtual void Enter() { }
        public virtual void Execute() { }
        public virtual void Exit() { }
    }
}