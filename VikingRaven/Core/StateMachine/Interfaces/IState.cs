namespace VikingRaven.Core.StateMachine
{
    public interface IState
    {
        void Enter();
        void Execute();
        void Exit();
    }
}